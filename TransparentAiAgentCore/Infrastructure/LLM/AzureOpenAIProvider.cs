using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// LLM provider implementation for Azure OpenAI
/// </summary>
public class AzureOpenAIProvider : ILLMProvider
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ITransparencyService _transparencyService;

    public string ProviderName => "AzureOpenAI";

    public AzureOpenAIProvider(
        IAuthenticationProvider authProvider,
        string deploymentName,
        ITransparencyService transparencyService,
        AppConfiguration appConfig)
    {
        if (authProvider == null)
            throw new ArgumentNullException(nameof(authProvider));
        if (string.IsNullOrWhiteSpace(deploymentName))
            throw new ArgumentException("Deployment name cannot be null or whitespace", nameof(deploymentName));
        if (appConfig == null)
            throw new ArgumentNullException(nameof(appConfig));

        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _deploymentName = deploymentName;

        var endpoint = new Uri(authProvider.GetEndpoint("AzureOpenAI"));
        var azureConfig = appConfig.LLM.AzureOpenAI;

        // Create client based on authentication mode
        if (azureConfig.AuthenticationMode == AuthenticationMode.DefaultAzureCredential)
        {
            // Use DefaultAzureCredential (OAuth/Microsoft Entra ID)
            TokenCredential credential;

            if (!string.IsNullOrWhiteSpace(azureConfig.TenantId))
            {
                // Use specific tenant if provided
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    TenantId = azureConfig.TenantId
                });
            }
            else
            {
                // Use default tenant discovery
                credential = new DefaultAzureCredential();
            }

            _client = new OpenAIClient(endpoint, credential);
        }
        else // AuthenticationMode.ApiKey
        {
            // Use API Key authentication
            var apiKey = authProvider.GetApiKey("AzureOpenAI");
            _client = new OpenAIClient(endpoint, new AzureKeyCredential(apiKey));
        }
    }

    // Constructor for testing with injected client
    internal AzureOpenAIProvider(
        OpenAIClient client,
        string deploymentName,
        ITransparencyService transparencyService)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
    }

    public async Task<LLMResponse> SendRequestAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Log request to transparency system
            LogRequest(request);

            // Convert to Azure OpenAI format
            var chatCompletionsOptions = BuildChatCompletionsOptions(request);

            // Send request
            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(
                chatCompletionsOptions,
                cancellationToken);

            // Convert response
            var llmResponse = ConvertResponse(response.Value);

            // Log response to transparency system
            LogResponse(llmResponse);

            return llmResponse;
        }
        catch (RequestFailedException ex)
        {
            throw new LLMException($"Azure OpenAI request failed: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not LLMException)
        {
            throw new LLMException($"Unexpected error during LLM request: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Log request to transparency system
        LogRequest(request);

        StreamingResponse<StreamingChatCompletionsUpdate>? streamingResponse = null;

        // Get streaming response outside of try-catch to allow yield
        try
        {
            // Convert to Azure OpenAI format
            var chatCompletionsOptions = BuildChatCompletionsOptions(request);

            // Get streaming response
            streamingResponse = await _client.GetChatCompletionsStreamingAsync(
                chatCompletionsOptions,
                cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            throw new LLMException($"Azure OpenAI streaming request failed: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not LLMException)
        {
            throw new LLMException($"Unexpected error during streaming LLM request: {ex.Message}", ex);
        }

        // Stream chunks (cannot be in try-catch due to yield)
        if (streamingResponse != null)
        {
            await foreach (StreamingChatCompletionsUpdate update in streamingResponse.EnumerateValues().WithCancellation(cancellationToken))
            {
                var chunk = ConvertStreamingUpdate(update);
                yield return chunk;
            }

            streamingResponse.Dispose();
        }
    }

    private ChatCompletionsOptions BuildChatCompletionsOptions(LLMRequest request)
    {
        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Temperature = (float)request.Temperature,
            NucleusSamplingFactor = (float)request.TopP,
            MaxTokens = request.MaxTokens
        };

        // Add messages
        foreach (var message in request.Messages)
        {
            options.Messages.Add(ConvertToAzureMessage(message));
        }

        // Add tools if present
        if (request.Tools != null && request.Tools.Count > 0)
        {
            foreach (var tool in request.Tools)
            {
                options.Tools.Add(ConvertToAzureTool(tool));
            }
        }

        return options;
    }

    private ChatRequestMessage ConvertToAzureMessage(LLMMessage message)
    {
        return message.Role.ToLowerInvariant() switch
        {
            "user" => new ChatRequestUserMessage(message.Content),
            "assistant" when message.ToolCalls != null && message.ToolCalls.Count > 0 =>
                CreateAssistantMessageWithToolCalls(message),
            "assistant" => new ChatRequestAssistantMessage(message.Content),
            "system" => new ChatRequestSystemMessage(message.Content),
            "tool" => new ChatRequestToolMessage(message.Content, message.ToolCallId!),
            _ => throw new LLMException($"Unknown message role: {message.Role}")
        };
    }

    private ChatRequestAssistantMessage CreateAssistantMessageWithToolCalls(LLMMessage message)
    {
        var assistantMessage = new ChatRequestAssistantMessage(message.Content);
        foreach (var tc in message.ToolCalls!)
        {
            var toolCall = new ChatCompletionsFunctionToolCall(tc.Id, tc.Name, tc.Arguments);
            assistantMessage.ToolCalls.Add(toolCall);
        }
        return assistantMessage;
    }

    private ChatCompletionsFunctionToolDefinition ConvertToAzureTool(LLMTool tool)
    {
        return new ChatCompletionsFunctionToolDefinition
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = BinaryData.FromString(tool.ParametersSchema)
        };
    }

    private LLMResponse ConvertResponse(ChatCompletions response)
    {
        var choice = response.Choices[0];
        var message = choice.Message;

        List<LLMToolCall>? toolCalls = null;
        if (message.ToolCalls != null && message.ToolCalls.Count > 0)
        {
            toolCalls = message.ToolCalls
                .OfType<ChatCompletionsFunctionToolCall>()
                .Select(tc => new LLMToolCall(tc.Id, tc.Name, tc.Arguments))
                .ToList();
        }

        var usage = response.Usage != null
            ? new LLMUsage(response.Usage.PromptTokens, response.Usage.CompletionTokens)
            : null;

        return new LLMResponse(
            message.Content ?? string.Empty,
            toolCalls,
            choice.FinishReason?.ToString(),
            usage);
    }

    private StreamingLLMChunk ConvertStreamingUpdate(StreamingChatCompletionsUpdate update)
    {
        var contentDelta = update.ContentUpdate ?? string.Empty;
        var isComplete = update.FinishReason != null;
        var finishReason = update.FinishReason?.ToString();

        // Handle tool calls in streaming (more complex, simplified here)
        LLMToolCall? toolCallDelta = null;
        if (update.ToolCallUpdate != null && update.ToolCallUpdate is StreamingFunctionToolCallUpdate ftc)
        {
            toolCallDelta = new LLMToolCall(
                ftc.Id ?? string.Empty,
                ftc.Name ?? string.Empty,
                ftc.ArgumentsUpdate ?? string.Empty);
        }

        return new StreamingLLMChunk(contentDelta, toolCallDelta, isComplete, finishReason);
    }

    private void LogRequest(LLMRequest request)
    {
        var eventData = System.Text.Json.JsonSerializer.Serialize(new
        {
            Provider = ProviderName,
            MessageCount = request.Messages.Count,
            Temperature = request.Temperature,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens,
            ToolCount = request.Tools?.Count ?? 0,
            Stream = request.Stream
        });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.SystemState,
                eventData,
                "LLM Request"));
    }

    private void LogResponse(LLMResponse response)
    {
        var eventData = System.Text.Json.JsonSerializer.Serialize(new
        {
            Provider = ProviderName,
            ContentLength = response.Content?.Length ?? 0,
            ToolCallCount = response.ToolCalls?.Count ?? 0,
            FinishReason = response.FinishReason,
            Usage = response.Usage
        });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.AssistantResponse,
                eventData,
                "LLM Response"));
    }
}
