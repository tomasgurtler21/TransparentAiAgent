using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
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
    private readonly ChatClient _chatClient;
    private readonly ITransparencyService _transparencyService;
    private readonly bool _isReasoningModel;
    private readonly string? _endpoint;
    private readonly string? _deploymentName;
    private readonly string? _apiKey;
    private readonly Azure.Core.TokenCredential? _tokenCredential;
    private readonly string? _apiVersion;

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
        _isReasoningModel = appConfig.LLM.AzureOpenAI?.IsReasoningModel ?? false;

        var endpoint = new Uri(authProvider.GetEndpoint("AzureOpenAI"));
        var azureConfig = appConfig.LLM.AzureOpenAI;

        // Store configuration for reasoning model HTTP requests
        _endpoint = endpoint.ToString().TrimEnd('/');
        _deploymentName = deploymentName;
        _apiVersion = azureConfig?.ApiVersion ?? "2024-02-15-preview";

        // Create AzureOpenAIClient based on authentication mode
        AzureOpenAIClient azureClient;

        switch (azureConfig.AuthenticationMode)
        {
            case AuthenticationMode.DefaultAzureCredential:
            {
                // Use DefaultAzureCredential (OAuth/Microsoft Entra ID)
                // Automatically discovers credentials from environment, CLI, managed identity, etc.
                if (!string.IsNullOrWhiteSpace(azureConfig.TenantId))
                {
                    // Use specific tenant if provided
                    _tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        TenantId = azureConfig.TenantId
                    });
                    azureClient = new AzureOpenAIClient(endpoint, _tokenCredential);
                }
                else
                {
                    // Use default tenant discovery
                    _tokenCredential = new DefaultAzureCredential();
                    azureClient = new AzureOpenAIClient(endpoint, _tokenCredential);
                }
                break;
            }
            case AuthenticationMode.InteractiveBrowserCredential:
            {
                // Use InteractiveBrowserCredential (OAuth/Microsoft Entra ID)
                // Opens browser popup for interactive user login
                if (!string.IsNullOrWhiteSpace(azureConfig.TenantId))
                {
                    // Use specific tenant if provided
                    _tokenCredential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                    {
                        TenantId = azureConfig.TenantId
                    });
                    azureClient = new AzureOpenAIClient(endpoint, _tokenCredential);
                }
                else
                {
                    // Use default tenant discovery
                    _tokenCredential = new InteractiveBrowserCredential();
                    azureClient = new AzureOpenAIClient(endpoint, _tokenCredential);
                }
                break;
            }
            case AuthenticationMode.ApiKey:
            {
                // Use API Key authentication
                _apiKey = authProvider.GetApiKey("AzureOpenAI");
                azureClient = new AzureOpenAIClient(endpoint, new ApiKeyCredential(_apiKey));
                break;
            }
            case AuthenticationMode.Unspecified:
            default:
                // Should never reach here due to validation, but fail defensively
                throw new ConfigurationException(
                    "Azure OpenAI AuthenticationMode is Unspecified or invalid. " +
                    "This should have been caught during configuration validation.");
        }

        // Get ChatClient for the specific deployment
        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    // Constructor for testing with injected client
    internal AzureOpenAIProvider(
        ChatClient chatClient,
        ITransparencyService transparencyService,
        bool isReasoningModel = false)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _isReasoningModel = isReasoningModel;
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

            // For reasoning models, use protocol method to send max_completion_tokens
            if (_isReasoningModel)
            {
                return await SendRequestAsync_ReasoningModel(request, cancellationToken);
            }

            // Standard path for traditional models
            // Convert messages to Azure OpenAI format
            var messages = ConvertToAzureMessages(request.Messages);

            // Build options
            var options = BuildChatCompletionOptions(request);

            // Send request
            ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            // Convert response
            var llmResponse = ConvertResponse(response.Value);

            // Log response to transparency system
            LogResponse(llmResponse);

            return llmResponse;
        }
        catch (ClientResultException ex)
        {
            throw new LLMException($"Azure OpenAI request failed: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not LLMException)
        {
            throw new LLMException($"Unexpected error during LLM request: {ex.Message}", ex);
        }
    }

    private async Task<LLMResponse> SendRequestAsync_ReasoningModel(LLMRequest request, CancellationToken cancellationToken)
    {
        // Convert messages
        var messages = ConvertToAzureMessages(request.Messages);

        // Build options (without MaxOutputTokenCount for reasoning models)
        var options = BuildChatCompletionOptions(request);

        // Build request JSON with max_completion_tokens
        var requestJson = BuildRequestJsonForReasoningModel(messages, options, request.MaxTokens);

        // Make HTTP request directly to Azure OpenAI API
        using var httpClient = new HttpClient();

        // Build URL
        var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";

        // Set authentication header
        if (_apiKey != null)
        {
            httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        }
        else if (_tokenCredential != null)
        {
            var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
            var token = await _tokenCredential.GetTokenAsync(tokenRequestContext, cancellationToken);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        }

        // Send request
        var content = new StringContent(requestJson.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LLMException($"Azure OpenAI request failed: HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) {errorContent}");
        }

        // Parse response
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument jsonDoc = JsonDocument.Parse(responseJson);
        var llmResponse = ParseLLMResponseFromJson(jsonDoc.RootElement);

        // Log response to transparency system
        LogResponse(llmResponse);

        return llmResponse;
    }

    private LLMResponse ParseLLMResponseFromJson(JsonElement root)
    {
        // Extract the first choice
        var choice = root.GetProperty("choices")[0];
        var message = choice.GetProperty("message");

        // Extract content
        var content = message.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? string.Empty : string.Empty;

        // Extract tool calls if present
        List<LLMToolCall>? toolCalls = null;
        if (message.TryGetProperty("tool_calls", out var toolCallsProp))
        {
            toolCalls = new List<LLMToolCall>();
            foreach (var tc in toolCallsProp.EnumerateArray())
            {
                var id = tc.GetProperty("id").GetString() ?? string.Empty;
                var function = tc.GetProperty("function");
                var name = function.GetProperty("name").GetString() ?? string.Empty;
                var arguments = function.GetProperty("arguments").GetString() ?? string.Empty;

                toolCalls.Add(new LLMToolCall(id, name, arguments));
            }
        }

        // Extract finish reason
        var finishReasonStr = choice.TryGetProperty("finish_reason", out var finishProp) ? finishProp.GetString() : null;

        // Extract usage if present
        LLMUsage? usage = null;
        if (root.TryGetProperty("usage", out var usageProp))
        {
            var inputTokens = usageProp.GetProperty("prompt_tokens").GetInt32();
            var outputTokens = usageProp.GetProperty("completion_tokens").GetInt32();
            usage = new LLMUsage(inputTokens, outputTokens);
        }

        return new LLMResponse(content, toolCalls, finishReasonStr, usage);
    }

    public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Log request to transparency system
        LogRequest(request);

        AsyncCollectionResult<StreamingChatCompletionUpdate>? streamingResponse = null;

        // Get streaming response outside of try-catch to allow yield
        try
        {
            // Convert messages to Azure OpenAI format
            var messages = ConvertToAzureMessages(request.Messages);

            // Build options
            var options = BuildChatCompletionOptions(request);

            // Get streaming response
            streamingResponse = _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);
        }
        catch (ClientResultException ex)
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
            await foreach (StreamingChatCompletionUpdate update in streamingResponse.WithCancellation(cancellationToken))
            {
                var chunk = ConvertStreamingUpdate(update);
                yield return chunk;
            }
        }
    }

    private List<ChatMessage> ConvertToAzureMessages(List<LLMMessage> messages)
    {
        var azureMessages = new List<ChatMessage>();

        foreach (var message in messages)
        {
            azureMessages.Add(ConvertToAzureMessage(message));
        }

        return azureMessages;
    }

    private ChatMessage ConvertToAzureMessage(LLMMessage message)
    {
        return message.Role.ToLowerInvariant() switch
        {
            "user" => new UserChatMessage(message.Content),
            "assistant" when message.ToolCalls != null && message.ToolCalls.Count > 0 =>
                CreateAssistantMessageWithToolCalls(message),
            "assistant" => new AssistantChatMessage(message.Content),
            "system" => new SystemChatMessage(message.Content),
            "tool" => new ToolChatMessage(message.ToolCallId!, message.Content),
            _ => throw new LLMException($"Unknown message role: {message.Role}")
        };
    }

    private AssistantChatMessage CreateAssistantMessageWithToolCalls(LLMMessage message)
    {
        var toolCalls = message.ToolCalls!
            .Select(tc => ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name, BinaryData.FromString(tc.Arguments)))
            .ToList();

        var assistantMessage = new AssistantChatMessage(toolCalls);

        // Add content if present
        if (!string.IsNullOrEmpty(message.Content))
        {
            assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(message.Content));
        }

        return assistantMessage;
    }

    private ChatCompletionOptions BuildChatCompletionOptions(LLMRequest request)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = (float)request.Temperature,
            TopP = (float)request.TopP
        };

        // For reasoning models, we'll use protocol method with BinaryContent to avoid SDK bug
        // So don't set MaxOutputTokenCount here for reasoning models
        if (!_isReasoningModel)
        {
            options.MaxOutputTokenCount = request.MaxTokens;
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

    private JsonObject BuildRequestJsonForReasoningModel(List<ChatMessage> messages, ChatCompletionOptions options, int maxTokens)
    {
        var requestJson = new JsonObject();

        // Add messages
        var messagesArray = new JsonArray();
        foreach (var message in messages)
        {
            var messageObj = new JsonObject();

            if (message is UserChatMessage userMsg)
            {
                messageObj["role"] = "user";
                messageObj["content"] = userMsg.Content[0].Text;
            }
            else if (message is AssistantChatMessage assistantMsg)
            {
                messageObj["role"] = "assistant";
                if (assistantMsg.Content.Count > 0)
                {
                    messageObj["content"] = assistantMsg.Content[0].Text;
                }
                if (assistantMsg.ToolCalls.Count > 0)
                {
                    var toolCallsArray = new JsonArray();
                    foreach (var tc in assistantMsg.ToolCalls)
                    {
                        var toolCallObj = new JsonObject
                        {
                            ["id"] = tc.Id,
                            ["type"] = "function",
                            ["function"] = new JsonObject
                            {
                                ["name"] = tc.FunctionName,
                                ["arguments"] = tc.FunctionArguments.ToString()
                            }
                        };
                        toolCallsArray.Add(toolCallObj);
                    }
                    messageObj["tool_calls"] = toolCallsArray;
                }
            }
            else if (message is SystemChatMessage systemMsg)
            {
                messageObj["role"] = "system";
                messageObj["content"] = systemMsg.Content[0].Text;
            }
            else if (message is ToolChatMessage toolMsg)
            {
                messageObj["role"] = "tool";
                messageObj["content"] = toolMsg.Content[0].Text;
                messageObj["tool_call_id"] = toolMsg.ToolCallId;
            }

            messagesArray.Add(messageObj);
        }
        requestJson["messages"] = messagesArray;

        // Add parameters
        requestJson["temperature"] = options.Temperature;
        requestJson["top_p"] = options.TopP;

        // CRITICAL: Use max_completion_tokens for reasoning models
        requestJson["max_completion_tokens"] = maxTokens;

        // Add tools if present
        if (options.Tools.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var tool in options.Tools)
            {
                var toolObj = new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = tool.FunctionName,
                        ["description"] = tool.FunctionDescription,
                        ["parameters"] = JsonNode.Parse(tool.FunctionParameters.ToString())
                    }
                };
                toolsArray.Add(toolObj);
            }
            requestJson["tools"] = toolsArray;
        }

        return requestJson;
    }

    private ChatTool ConvertToAzureTool(LLMTool tool)
    {
        return ChatTool.CreateFunctionTool(
            functionName: tool.Name,
            functionDescription: tool.Description,
            functionParameters: BinaryData.FromString(tool.ParametersSchema));
    }

    private LLMResponse ConvertResponse(ChatCompletion response)
    {
        var content = response.Content[0].Text ?? string.Empty;

        List<LLMToolCall>? toolCalls = null;
        if (response.ToolCalls.Count > 0)
        {
            toolCalls = response.ToolCalls
                .Select(tc => new LLMToolCall(tc.Id, tc.FunctionName, tc.FunctionArguments.ToString()))
                .ToList();
        }

        var usage = response.Usage != null
            ? new LLMUsage(response.Usage.InputTokenCount, response.Usage.OutputTokenCount)
            : null;

        return new LLMResponse(
            content,
            toolCalls,
            response.FinishReason.ToString(),
            usage);
    }

    private StreamingLLMChunk ConvertStreamingUpdate(StreamingChatCompletionUpdate update)
    {
        // Get content delta
        var contentDelta = string.Empty;
        if (update.ContentUpdate.Count > 0)
        {
            contentDelta = update.ContentUpdate[0].Text ?? string.Empty;
        }

        var isComplete = update.FinishReason != null;
        var finishReason = update.FinishReason?.ToString();

        // Handle tool calls in streaming
        LLMToolCall? toolCallDelta = null;
        if (update.ToolCallUpdates.Count > 0)
        {
            var toolUpdate = update.ToolCallUpdates[0];
            toolCallDelta = new LLMToolCall(
                toolUpdate.ToolCallId ?? string.Empty,
                toolUpdate.FunctionName ?? string.Empty,
                toolUpdate.FunctionArgumentsUpdate?.ToString() ?? string.Empty);
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
            Stream = request.Stream,
            IsReasoningModel = _isReasoningModel
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
