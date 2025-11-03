using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.Client;
using Anthropic.Client.Core;
using Anthropic.Client.Models.Messages;
using Anthropic.Client.Models.Messages.MessageCreateParamsProperties;
using Anthropic.Client.Models.Messages.MessageParamProperties;
using Anthropic.Client.Models.Messages.ToolProperties;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Anthropic Claude LLM provider implementation
/// </summary>
public class AnthropicProvider : ILLMProvider
{
    private readonly AnthropicClient _client;
    private readonly string _modelName;
    private readonly ITransparencyService _transparencyService;
    private readonly AppConfiguration _appConfig;

    public string ProviderName => "Anthropic";

    /// <summary>
    /// Initializes a new instance of the AnthropicProvider
    /// </summary>
    /// <param name="authProvider">Authentication provider for API credentials</param>
    /// <param name="modelName">Claude model name (e.g., claude-3-5-sonnet-20241022)</param>
    /// <param name="transparencyService">Service for logging transparency events</param>
    /// <param name="appConfig">Application configuration</param>
    public AnthropicProvider(
        IAuthenticationProvider authProvider,
        string modelName,
        ITransparencyService transparencyService,
        AppConfiguration appConfig)
    {
        // Validate parameters
        if (authProvider == null)
            throw new ArgumentNullException(nameof(authProvider));

        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));

        if (transparencyService == null)
            throw new ArgumentNullException(nameof(transparencyService));

        if (appConfig == null)
            throw new ArgumentNullException(nameof(appConfig));

        // Store dependencies
        _modelName = modelName;
        _transparencyService = transparencyService;
        _appConfig = appConfig;

        // Initialize Anthropic client
        var apiKey = authProvider.GetApiKey("Anthropic");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Anthropic API key is required");

        _client = new AnthropicClient { APIKey = apiKey };
    }

    /// <summary>
    /// Send a request to Claude and get a complete response
    /// </summary>
    public async Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Build request parameters
            var messageParams = BuildMessageRequest(request, null);

            // Log transparency event - simplified for now, can be enhanced later
            _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.SystemState,
                $"Anthropic LLM Request: Model={_modelName}, Messages={request.Messages.Count}, MaxTokens={request.MaxTokens}, Tools={request.Tools?.Count ?? 0}",
                "LLM Request"));

            // Call Anthropic API
            var response = await _client.Messages.Create(messageParams);

            // Convert response
            var llmResponse = ConvertResponse(response);

            // Log transparency event
            _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.AssistantResponse,
                $"Anthropic LLM Response: Content length={llmResponse.Content.Length}, FinishReason={llmResponse.FinishReason}",
                "LLM Response"));

            return llmResponse;
        }
        catch (Exception ex)
        {
            _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.Error,
                $"Anthropic API error: {ex.Message}",
                "LLM Error"));
            throw new Domain.Exceptions.LLMException($"Anthropic request failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Send a request to Claude and stream the response
    /// </summary>
    public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Log transparency event
        _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
            Domain.Transparency.TransparencyEventType.SystemState,
            $"Anthropic LLM Streaming Request: Model={_modelName}, Messages={request.Messages.Count}, MaxTokens={request.MaxTokens}, Tools={request.Tools?.Count ?? 0}",
            "LLM Streaming Request"));

        IAsyncEnumerable<Anthropic.Client.Models.Messages.RawMessageStreamEvent>? streamingResponse = null;

        // Get streaming response outside of try-catch to allow yield
        try
        {
            // Build request parameters
            var messageParams = BuildMessageRequest(request, null);

            // Get streaming response
            streamingResponse = _client.Messages.CreateStreaming(messageParams);
        }
        catch (Exception ex)
        {
            _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.Error,
                $"Anthropic streaming request setup failed: {ex.Message}",
                "LLM Error"));
            throw new Domain.Exceptions.LLMException($"Anthropic streaming request failed: {ex.Message}", ex);
        }

        // Stream chunks (cannot be in try-catch due to yield)
        if (streamingResponse != null)
        {
            await foreach (var streamEvent in streamingResponse.WithCancellation(cancellationToken))
            {
                var chunk = ConvertStreamingEvent(streamEvent);
                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
    }

    /// <summary>
    /// Convert Anthropic Message to LLMResponse
    /// </summary>
    private LLMResponse ConvertResponse(Message response)
    {
        // Extract text content from content blocks
        var textContent = string.Empty;
        List<LLMToolCall>? toolCalls = null;

        foreach (var contentBlock in response.Content)
        {
            // Check if this is a text block
            if (contentBlock.TryPickText(out var textBlock))
            {
                textContent += textBlock.Text;
            }
            // Check if this is a tool use block
            else if (contentBlock.TryPickToolUse(out var toolUseBlock))
            {
                if (toolCalls == null)
                    toolCalls = new List<LLMToolCall>();

                // Convert tool use to LLMToolCall
                var toolCall = new LLMToolCall(
                    toolUseBlock.ID,
                    toolUseBlock.Name,
                    System.Text.Json.JsonSerializer.Serialize(toolUseBlock.Input)
                );
                toolCalls.Add(toolCall);
            }
        }

        // Map stop reason to finish reason
        var finishReason = response.StopReason?.ToString() ?? "unknown";

        return new LLMResponse(
            content: textContent,
            toolCalls: toolCalls,
            finishReason: finishReason,
            usage: null  // TODO: Extract usage info from response
        );
    }

    /// <summary>
    /// Convert Anthropic streaming event to StreamingLLMChunk
    /// </summary>
    private StreamingLLMChunk? ConvertStreamingEvent(Anthropic.Client.Models.Messages.RawMessageStreamEvent streamEvent)
    {
        // Handle content block delta events (text chunks)
        if (streamEvent.TryPickContentBlockDelta(out var deltaEvent))
        {
            // Check if this is a text delta
            if (deltaEvent.Delta.TryPickText(out var textDelta))
            {
                return new StreamingLLMChunk(
                    contentDelta: textDelta.Text,
                    toolCallDelta: null,
                    isComplete: false,
                    finishReason: null
                );
            }
        }
        // Handle message stop event (end of stream)
        else if (streamEvent.TryPickStop(out var stopEvent))
        {
            return new StreamingLLMChunk(
                contentDelta: string.Empty,
                toolCallDelta: null,
                isComplete: true,
                finishReason: "stop"
            );
        }

        // Skip other event types (message_start, content_block_start, content_block_stop, message_delta)
        return null;
    }

    /// <summary>
    /// Build Anthropic MessageCreateParams from LLMRequest
    /// </summary>
    internal MessageCreateParams BuildMessageRequest(LLMRequest request, string? systemPrompt)
    {
        // Convert messages
        var (extractedSystem, anthropicMessages) = ConvertToAnthropicMessages(request.Messages);

        // Use provided system prompt or extracted one
        var finalSystemPrompt = systemPrompt ?? extractedSystem;

        // Build request
        var messageParams = new MessageCreateParams
        {
            Model = _modelName,
            Messages = anthropicMessages,
            MaxTokens = request.MaxTokens
        };

        // Set optional parameters - only set if they have values
        if (request.Temperature.HasValue)
        {
            messageParams.Temperature = request.Temperature.Value;
        }

        if (request.TopP.HasValue)
        {
            messageParams.TopP = request.TopP.Value;
        }

        if (finalSystemPrompt != null)
        {
            messageParams.System = new SystemModel(finalSystemPrompt);
        }

        // Add tools if present
        if (request.Tools != null && request.Tools.Count > 0)
        {
            messageParams.Tools = new List<ToolUnion>();
            foreach (var tool in request.Tools)
            {
                messageParams.Tools.Add(ConvertToAnthropicTool(tool));
            }
        }

        return messageParams;
    }

    /// <summary>
    /// Convert LLMTool to Anthropic Tool format
    /// </summary>
    private ToolUnion ConvertToAnthropicTool(LLMTool tool)
    {
        // Parse the JSON schema string into a JsonElement
        var schemaDocument = System.Text.Json.JsonDocument.Parse(tool.ParametersSchema);
        var schemaElement = schemaDocument.RootElement;

        // Extract properties and required fields from the schema
        System.Text.Json.JsonElement? properties = null;
        List<string>? required = null;

        if (schemaElement.TryGetProperty("properties", out var propsElement))
        {
            properties = propsElement;
        }

        if (schemaElement.TryGetProperty("required", out var reqElement))
        {
            required = System.Text.Json.JsonSerializer.Deserialize<List<string>>(reqElement.GetRawText());
        }

        // Create the Anthropic Tool
        var anthropicTool = new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new InputSchema
            {
                Type = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("\"object\""),
                Properties1 = properties,
                Required = required
            }
        };

        return new ToolUnion(anthropicTool);
    }

    /// <summary>
    /// Convert LLMMessage list to Anthropic MessageParam format
    /// Extracts system message separately as required by Anthropic API
    /// </summary>
    internal (string? systemPrompt, List<MessageParam> messages) ConvertToAnthropicMessages(List<LLMMessage> llmMessages)
    {
        var messages = new List<MessageParam>();
        string? systemPrompt = null;

        foreach (var llmMsg in llmMessages)
        {
            if (llmMsg.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                // Extract system message - Anthropic wants it separate
                systemPrompt = llmMsg.Content;
                continue;
            }

            // Convert role
            ApiEnum<string, Role> role = llmMsg.Role.ToLowerInvariant() switch
            {
                "user" => Role.User,
                "assistant" => Role.Assistant,
                _ => throw new ArgumentException($"Unknown role: {llmMsg.Role}")
            };

            // Create message param
            var messageParam = new MessageParam
            {
                Role = role,
                Content = new Content(llmMsg.Content)
            };

            messages.Add(messageParam);
        }

        return (systemPrompt, messages);
    }
}
