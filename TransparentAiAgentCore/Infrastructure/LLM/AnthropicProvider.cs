using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
using TransparentAiAgentCore.Domain.Transparency.EventData;
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

            // Generate correlation ID for request/response tracking
            var correlationId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            // Log raw request
            LogRawRequest(request, messageParams, correlationId);

            // Call Anthropic API
            var response = await _client.Messages.Create(messageParams);

            // Calculate latency
            var latency = DateTime.UtcNow - startTime;

            // Convert response
            var llmResponse = ConvertResponse(response);

            // Log raw response
            LogRawResponse(response, correlationId, latency);

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

        // Accumulators for proper tool call handling
        var textAccumulators = new Dictionary<int, System.Text.StringBuilder>();
        var toolCallInfo = new Dictionary<int, (string Id, string Name)>();
        var jsonAccumulators = new Dictionary<int, System.Text.StringBuilder>();
        string? stopReason = null;

        IAsyncEnumerable<Anthropic.Client.Models.Messages.RawMessageStreamEvent>? streamingResponse = null;
        string correlationId = Guid.NewGuid().ToString();
        DateTime startTime = DateTime.UtcNow;

        // Get streaming response outside of try-catch to allow yield
        try
        {
            // Build request parameters
            var messageParams = BuildMessageRequest(request, null);

            // Log raw request before streaming starts
            LogRawRequest(request, messageParams, correlationId);

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
                // Handle content_block_start
                if (streamEvent.TryPickContentBlockStart(out var blockStart))
                {
                    int index = (int)blockStart.Index;

                    if (blockStart.ContentBlock.TryPickText(out _))
                    {
                        textAccumulators[index] = new System.Text.StringBuilder();
                    }
                    else if (blockStart.ContentBlock.TryPickToolUse(out var toolBlock))
                    {
                        toolCallInfo[index] = (toolBlock.ID, toolBlock.Name);
                        jsonAccumulators[index] = new System.Text.StringBuilder();
                    }
                }
                // Handle content_block_delta
                else if (streamEvent.TryPickContentBlockDelta(out var deltaEvent))
                {
                    int index = (int)deltaEvent.Index;

                    if (deltaEvent.Delta.TryPickText(out var textDelta))
                    {
                        if (textAccumulators.ContainsKey(index))
                        {
                            textAccumulators[index].Append(textDelta.Text);
                        }
                    }
                    // Accumulate tool call input JSON deltas
                    else
                    {
                        // Try to extract input JSON delta - SDK uses TryPickInputJSON
                        var deltaType = deltaEvent.Delta.GetType();
                        var tryPickMethod = deltaType.GetMethod("TryPickInputJSON");
                        if (tryPickMethod != null)
                        {
                            var parameters = new object?[] { null };
                            var result = (bool?)tryPickMethod.Invoke(deltaEvent.Delta, parameters);
                            if (result == true && parameters[0] != null)
                            {
                                var jsonDelta = parameters[0]!;
                                var jsonDeltaType = jsonDelta.GetType();

                                // Try common property names for the partial JSON
                                var partialJsonProp = jsonDeltaType.GetProperty("PartialJson")
                                    ?? jsonDeltaType.GetProperty("PartialJSON")
                                    ?? jsonDeltaType.GetProperty("Input")
                                    ?? jsonDeltaType.GetProperty("Json")
                                    ?? jsonDeltaType.GetProperty("JSON");

                                if (partialJsonProp != null && jsonAccumulators.ContainsKey(index))
                                {
                                    var partialJson = partialJsonProp.GetValue(jsonDelta) as string;
                                    if (partialJson != null)
                                    {
                                        jsonAccumulators[index].Append(partialJson);
                                    }
                                }
                            }
                        }
                    }
                }
                // Handle content_block_stop
                else if (streamEvent.TryPickContentBlockStop(out var blockStop))
                {
                    int index = (int)blockStop.Index;

                    // Validate JSON for tool calls
                    if (jsonAccumulators.ContainsKey(index))
                    {
                        var jsonString = jsonAccumulators[index].ToString();
                        try
                        {
                            System.Text.Json.JsonDocument.Parse(jsonString);
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                                Domain.Transparency.TransparencyEventType.Error,
                                $"Invalid tool call JSON at index {index}: {jsonString}\nError: {ex.Message}",
                                "Tool Call Error"));
                        }
                    }
                }
                // Handle message_delta (FIXED: Capture stop_reason from here!)
                // Note: SDK beta may not have TryPickMessageDelta - will need to capture from Stop event
                // This is a workaround for beta SDK limitations
                // else if (streamEvent.TryPickMessageDelta(out var messageDelta))
                // {
                //     stopReason = messageDelta.Delta.StopReason?.ToString();
                // }
                // Handle message_stop
                else if (streamEvent.TryPickStop(out var stopEvent))
                {
                    // Stream is complete - build tool calls if any
                    List<LLMToolCall>? toolCalls = null;

                    if (toolCallInfo.Count > 0)
                    {
                        toolCalls = new List<LLMToolCall>();
                        foreach (var kvp in toolCallInfo)
                        {
                            int index = kvp.Key;
                            var (id, name) = kvp.Value;
                            var jsonString = jsonAccumulators.ContainsKey(index)
                                ? jsonAccumulators[index].ToString()
                                : "";

                            // If JSON is empty, default to empty object (validation layer should catch if required params missing)
                            if (string.IsNullOrWhiteSpace(jsonString))
                            {
                                _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                                    Domain.Transparency.TransparencyEventType.ToolStreamingDataCorrupted,
                                    $"Tool call '{name}' JSON accumulation failed during streaming. Arguments will be empty {{\u007D}}. " +
                                    $"Schema validation will fail if tool has required parameters.",
                                    "Streaming Tool Call Build"));
                                jsonString = "{}";
                            }

                            toolCalls.Add(new LLMToolCall(id, name, jsonString));
                        }

                        // Set stop_reason to tool_use if we have tool calls
                        if (stopReason == null)
                        {
                            stopReason = "tool_use";
                        }
                    }

                    // Yield final chunk with tool calls
                    yield return new StreamingLLMChunk(
                        contentDelta: string.Empty,
                        toolCallDelta: null,
                        isComplete: true,
                        finishReason: stopReason ?? "unknown",
                        accumulatedToolCalls: toolCalls
                    );

                    // Log complete accumulated response
                    var latency = DateTime.UtcNow - startTime;
                    var fullText = string.Join("", textAccumulators.Values.Select(sb => sb.ToString()));
                    LogStreamingResponse(fullText, toolCalls, stopReason, correlationId, latency);

                    break;
                }

                // Convert and yield current event
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

        // FIXED: Extract usage information
        LLMUsage? usage = null;
        if (response.Usage != null)
        {
            usage = new LLMUsage(
                promptTokens: (int)response.Usage.InputTokens,
                completionTokens: (int)response.Usage.OutputTokens
            );
        }

        return new LLMResponse(
            content: textContent,
            toolCalls: toolCalls,
            finishReason: finishReason,
            usage: usage
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
            // Build the tools list FIRST, then assign it to messageParams
            // (The SDK's Tools property may return a new list on each access)
            var toolsList = new List<ToolUnion>();

            foreach (var tool in request.Tools)
            {
                try
                {
                    var anthropicTool = ConvertToAnthropicTool(tool);
                    toolsList.Add(anthropicTool);
                }
                catch (Exception ex)
                {
                    _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                        Domain.Transparency.TransparencyEventType.Error,
                        $"Failed to convert tool {tool.Name}: {ex.Message}",
                        $"Tool conversion error: {tool.Name}"));
                }
            }

            // NOW assign the complete list to messageParams
            messageParams.Tools = toolsList;
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

        // Group consecutive tool result messages into a single user message
        var i = 0;
        while (i < llmMessages.Count)
        {
            var llmMsg = llmMessages[i];

            if (llmMsg.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                // Extract system message - Anthropic wants it separate
                systemPrompt = llmMsg.Content;
                i++;
                continue;
            }

            // Handle consecutive tool result messages - GROUP them into ONE user message
            if (llmMsg.Role.Equals("tool", StringComparison.OrdinalIgnoreCase))
            {
                var toolResultBlocks = new List<ContentBlockParam>();

                // Collect ALL consecutive tool result messages
                while (i < llmMessages.Count && llmMessages[i].Role.Equals("tool", StringComparison.OrdinalIgnoreCase))
                {
                    var toolMsg = llmMessages[i];

                    if (string.IsNullOrEmpty(toolMsg.ToolCallId))
                        throw new ArgumentException("Tool result message must have ToolCallId");

                    var toolResultBlock = new ToolResultBlockParam(toolMsg.ToolCallId)
                    {
                        Content = new Anthropic.Client.Models.Messages.ToolResultBlockParamProperties.Content(toolMsg.Content),
                        IsError = false  // Assuming success; adjust based on your error handling
                    };

                    toolResultBlocks.Add(new ContentBlockParam(toolResultBlock));
                    i++;
                }

                // Create ONE user message with ALL tool results
                var messageParam = new MessageParam
                {
                    Role = Role.User,
                    Content = new Content(toolResultBlocks)
                };

                messages.Add(messageParam);
                continue;
            }

            // Convert role
            ApiEnum<string, Role> role = llmMsg.Role.ToLowerInvariant() switch
            {
                "user" => Role.User,
                "assistant" => Role.Assistant,
                _ => throw new ArgumentException($"Unknown role: {llmMsg.Role}")
            };

            // Handle assistant messages with tool calls
            if (llmMsg.ToolCalls != null && llmMsg.ToolCalls.Count > 0)
            {
                var contentBlocks = new List<ContentBlockParam>();

                // Add text content if present
                if (!string.IsNullOrEmpty(llmMsg.Content))
                {
                    var textBlock = new TextBlockParam { Text = llmMsg.Content };
                    contentBlocks.Add(new ContentBlockParam(textBlock));
                }

                // Add tool use blocks
                foreach (var toolCall in llmMsg.ToolCalls)
                {
                    // Handle empty/whitespace arguments (tools with optional parameters)
                    var arguments = string.IsNullOrWhiteSpace(toolCall.Arguments) ? "{}" : toolCall.Arguments;

                    var inputJson = System.Text.Json.JsonDocument.Parse(arguments).RootElement;
                    var toolUseBlock = new ToolUseBlockParam
                    {
                        ID = toolCall.Id,
                        Name = toolCall.Name,
                        Input = inputJson
                    };
                    contentBlocks.Add(new ContentBlockParam(toolUseBlock));
                }

                var messageParam = new MessageParam
                {
                    Role = role,
                    Content = new Content(contentBlocks)
                };

                messages.Add(messageParam);
            }
            else
            {
                // Regular message without tool calls
                var messageParam = new MessageParam
                {
                    Role = role,
                    Content = new Content(llmMsg.Content)
                };

                messages.Add(messageParam);
            }

            i++;
        }

        return (systemPrompt, messages);
    }

    /// <summary>
    /// Log raw LLM request for transparency
    /// </summary>
    private void LogRawRequest(LLMRequest request, MessageCreateParams messageParams, string correlationId)
    {
        var rawData = new RawLLMRequestData(
            correlationId,
            ProviderName,
            SerializeRawRequest(request, messageParams),
            request.Messages.Count);

        var eventData = JsonSerializer.Serialize(rawData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.RawLLMRequest,
                eventData,
                $"Raw request to {ProviderName} (Correlation: {correlationId})"));
    }

    /// <summary>
    /// Log raw LLM response for transparency
    /// </summary>
    private void LogRawResponse(Message response, string correlationId, TimeSpan latency)
    {
        var rawData = new RawLLMResponseData(
            correlationId,
            ProviderName,
            SerializeRawResponse(response),
            null, // Status code not available for SDK calls
            latency);

        var eventData = JsonSerializer.Serialize(rawData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.RawLLMResponse,
                eventData,
                $"Raw response from {ProviderName} (Correlation: {correlationId}, Latency: {latency.TotalMilliseconds}ms)"));
    }

    /// <summary>
    /// Log complete accumulated streaming response for transparency (includes tool calls)
    /// </summary>
    private void LogStreamingResponse(string accumulatedContent, List<LLMToolCall>? toolCalls, string? stopReason, string correlationId, TimeSpan latency)
    {
        // Build response data with accumulated content and tool calls
        var contentItems = new List<object>();

        // Add text content if present
        if (!string.IsNullOrEmpty(accumulatedContent))
        {
            contentItems.Add(new { Type = "text", Text = accumulatedContent });
        }

        // Add tool calls if present
        if (toolCalls != null && toolCalls.Count > 0)
        {
            foreach (var toolCall in toolCalls)
            {
                contentItems.Add(new
                {
                    Type = "tool_use",
                    Id = toolCall.Id,
                    Name = toolCall.Name,
                    Arguments = toolCall.Arguments
                });
            }
        }

        var responseData = new
        {
            Provider = ProviderName,
            Content = contentItems,
            StopReason = stopReason ?? "unknown",
            Timestamp = DateTime.UtcNow,
            IsStreaming = true
        };

        var responseJson = JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true });

        var rawData = new RawLLMResponseData(
            correlationId,
            ProviderName,
            responseJson,
            null, // Status code not available for SDK calls
            latency);

        var eventData = JsonSerializer.Serialize(rawData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.RawLLMResponse,
                eventData,
                $"Complete streaming response from {ProviderName} (Correlation: {correlationId}, Latency: {latency.TotalMilliseconds}ms)"));
    }

    /// <summary>
    /// Serialize raw request to JSON for transparency logging
    /// </summary>
    private string SerializeRawRequest(LLMRequest request, MessageCreateParams messageParams)
    {
        var requestData = new
        {
            Provider = ProviderName,
            Model = _modelName,
            Messages = messageParams.Messages.Select(m => new
            {
                Role = m.Role.ToString(),
                Content = ExtractMessageContent(m)
            }).ToList(),
            Parameters = new
            {
                MaxTokens = messageParams.MaxTokens,
                Temperature = messageParams.Temperature,
                TopP = messageParams.TopP,
                System = messageParams.System != null ? ExtractSystemPrompt(messageParams.System) : null,
                Tools = messageParams.Tools?.Select(t => ExtractToolInfo(t)).ToList()
            },
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Serialize raw response to JSON for transparency logging
    /// </summary>
    private string SerializeRawResponse(Message response)
    {
        var responseData = new
        {
            Provider = ProviderName,
            Id = response.ID,
            Model = response.Model,
            Role = response.Role.ToString(),
            Content = response.Content.Select(c => ExtractContentBlock(c)).ToList(),
            StopReason = response.StopReason?.ToString(),
            Usage = response.Usage != null
                ? new
                {
                    InputTokens = response.Usage.InputTokens,
                    OutputTokens = response.Usage.OutputTokens
                }
                : null,
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Extract content from a MessageParam
    /// </summary>
    private object ExtractMessageContent(MessageParam messageParam)
    {
        if (messageParam.Content.TryPickString(out var stringContent))
        {
            return stringContent;
        }
        // If not a string, serialize the content as-is
        return JsonSerializer.Serialize(messageParam.Content);
    }

    /// <summary>
    /// Extract content from a content block (dynamic type from Anthropic SDK)
    /// </summary>
    private object ExtractContentBlock(dynamic contentBlock)
    {
        try
        {
            if (contentBlock.TryPickText(out dynamic textBlock))
            {
                return new { Type = "text", Text = textBlock.Text };
            }
            else if (contentBlock.TryPickToolUse(out dynamic toolUseBlock))
            {
                return new
                {
                    Type = "tool_use",
                    Id = toolUseBlock.ID,
                    Name = toolUseBlock.Name,
                    Input = toolUseBlock.Input
                };
            }
        }
        catch
        {
            // Fallback if TryPick methods aren't available
        }

        return new { Type = "unknown", Raw = contentBlock?.ToString() };
    }

    /// <summary>
    /// Extract system prompt from SystemModel
    /// </summary>
    private string ExtractSystemPrompt(SystemModel systemModel)
    {
        if (systemModel.TryPickString(out var systemString))
        {
            return systemString;
        }
        // If not a string, serialize as JSON
        return JsonSerializer.Serialize(systemModel);
    }

    /// <summary>
    /// Extract tool information from ToolUnion
    /// </summary>
    private object ExtractToolInfo(ToolUnion toolUnion)
    {
        if (toolUnion.TryPickTool(out var tool))
        {
            // Extract type from JsonElement
            string? typeValue = null;
            try
            {
                if (tool.InputSchema.Type.ValueKind == JsonValueKind.String)
                {
                    typeValue = tool.InputSchema.Type.GetString();
                }
                else
                {
                    typeValue = tool.InputSchema.Type.ToString();
                }
            }
            catch
            {
                typeValue = tool.InputSchema.Type.ToString();
            }

            return new
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = new
                {
                    Type = typeValue,
                    Properties = tool.InputSchema.Properties1,
                    Required = tool.InputSchema.Required
                }
            };
        }
        return new { Raw = toolUnion?.ToString() };
    }
}
