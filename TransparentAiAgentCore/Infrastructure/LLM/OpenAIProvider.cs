using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Transparency.EventData;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// LLM provider implementation for OpenAI
/// </summary>
public class OpenAIProvider : ILLMProvider
{
    private readonly ChatClient _chatClient;
    private readonly ITransparencyService _transparencyService;
    private readonly bool _isReasoningModel;

    public string ProviderName => "OpenAI";

    public OpenAIProvider(
        IAuthenticationProvider authProvider,
        string model,
        ITransparencyService transparencyService,
        AppConfiguration appConfig)
    {
        if (authProvider == null)
            throw new ArgumentNullException(nameof(authProvider));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model name cannot be null or whitespace", nameof(model));
        if (appConfig == null)
            throw new ArgumentNullException(nameof(appConfig));

        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _isReasoningModel = appConfig.LLM.OpenAI?.IsReasoningModel ?? false;

        var apiKey = authProvider.GetApiKey("OpenAI");
        var openAIConfig = appConfig.LLM.OpenAI;

        // Create OpenAIClient
        OpenAIClient openAIClient;

        if (!string.IsNullOrWhiteSpace(openAIConfig?.Endpoint))
        {
            // Use custom endpoint if provided
            openAIClient = new OpenAIClient(
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions()
                {
                    Endpoint = new Uri(openAIConfig.Endpoint)
                });
        }
        else
        {
            // Use default endpoint (api.openai.com)
            openAIClient = new OpenAIClient(apiKey);
        }

        // Get ChatClient for the specific model
        _chatClient = openAIClient.GetChatClient(model);
    }

    // Constructor for testing with injected client
    internal OpenAIProvider(
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

        // Generate correlation ID for this request
        var correlationId = Guid.NewGuid().ToString();
        var requestStartTime = DateTime.UtcNow;

        try
        {
            // Log request to transparency system
            LogRequest(request);

            // Convert messages to OpenAI format
            var messages = ConvertToChatMessages(request.Messages);

            // Build options
            var options = BuildChatCompletionOptions(request);

            // Log RAW request BEFORE sending
            LogRawRequest(request, messages, options, correlationId);

            // Send request
            ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            // Calculate latency
            var latency = DateTime.UtcNow - requestStartTime;

            // Log RAW response IMMEDIATELY after receiving (before any processing)
            LogRawResponse(response.Value, correlationId, latency);

            // Convert response
            var llmResponse = ConvertResponse(response.Value);

            // Log response to transparency system
            LogResponse(llmResponse);

            return llmResponse;
        }
        catch (ClientResultException ex)
        {
            throw new LLMException($"OpenAI request failed: {ex.Message}", ex);
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

        AsyncCollectionResult<StreamingChatCompletionUpdate>? streamingResponse = null;
        string correlationId = Guid.NewGuid().ToString();
        DateTime startTime = DateTime.UtcNow;

        // Accumulate response for final logging
        var accumulatedContent = new System.Text.StringBuilder();
        // Use helper class to accumulate tool calls by index
        var toolCallAccumulator = new OpenAIStreamingToolCallAccumulator();
        string? finishReason = null;

        // Get streaming response outside of try-catch to allow yield
        try
        {
            // Convert messages to OpenAI format
            var messages = ConvertToChatMessages(request.Messages);

            // Build options
            var options = BuildChatCompletionOptions(request);

            // Log raw request before streaming starts
            LogRawRequest(request, messages, options, correlationId);

            // Get streaming response
            streamingResponse = _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);
        }
        catch (ClientResultException ex)
        {
            throw new LLMException($"OpenAI streaming request failed: {ex.Message}", ex);
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
                // LOG RAW STREAMING CHUNK for diagnostics
                LogStreamingChunk(update, correlationId);

                // Accumulate content from chunks
                if (update.ContentUpdate.Count > 0)
                {
                    foreach (var content in update.ContentUpdate)
                    {
                        accumulatedContent.Append(content.Text);
                    }
                }

                // Accumulate tool calls using helper
                if (update.ToolCallUpdates.Count > 0)
                {
                    foreach (var toolCallUpdate in update.ToolCallUpdates)
                    {
                        toolCallAccumulator.AddToolCallUpdate(
                            index: toolCallUpdate.Index,
                            toolCallId: toolCallUpdate.ToolCallId,
                            functionName: toolCallUpdate.FunctionName,
                            argumentsUpdate: SafeBinaryDataToString(toolCallUpdate.FunctionArgumentsUpdate));
                    }
                }

                // Capture finish reason
                if (update.FinishReason.HasValue)
                {
                    finishReason = update.FinishReason.Value.ToString();
                }

                // Only yield chunk if it's NOT the final chunk
                // Final chunk will be yielded after the loop with accumulated tool calls
                if (!update.FinishReason.HasValue)
                {
                    var chunk = ConvertStreamingUpdate(update);
                    yield return chunk;
                }
            }

            // Get accumulated tool calls from helper
            var accumulatedToolCalls = toolCallAccumulator.GetAccumulatedToolCalls();

            // ALWAYS yield final completion chunk with accumulated tool calls
            // This is the ONLY source of truth for complete tool calls after streaming
            var finalChunk = new StreamingLLMChunk(
                contentDelta: string.Empty,
                toolCallDelta: null,
                isComplete: true,
                finishReason: finishReason,
                accumulatedToolCalls: accumulatedToolCalls.Count > 0 ? accumulatedToolCalls : null);
            yield return finalChunk;

            // Log complete accumulated response after streaming finishes
            var latency = DateTime.UtcNow - startTime;

            // LOG ACCUMULATED RESULT for diagnostics
            LogAccumulatedToolCalls(accumulatedToolCalls, correlationId);

            LogStreamingResponse(
                accumulatedContent.ToString(),
                accumulatedToolCalls.Count > 0 ? accumulatedToolCalls : null,
                finishReason,
                correlationId,
                latency);
        }
    }

    private List<ChatMessage> ConvertToChatMessages(List<LLMMessage> messages)
    {
        var chatMessages = new List<ChatMessage>();

        foreach (var message in messages)
        {
            chatMessages.Add(ConvertToChatMessage(message));
        }

        return chatMessages;
    }

    private ChatMessage ConvertToChatMessage(LLMMessage message)
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
        var options = new ChatCompletionOptions();

        // ✅ Only set Temperature and TopP for non-reasoning models
        // Reasoning models (o1, o3, o4-mini) do not support these parameters
        if (!_isReasoningModel)
        {
            if (request.Temperature.HasValue)
            {
                options.Temperature = (float)request.Temperature.Value;
            }

            if (request.TopP.HasValue)
            {
                options.TopP = (float)request.TopP.Value;
            }
        }

        options.MaxOutputTokenCount = request.MaxTokens;

        // Add tools if present
        if (request.Tools != null && request.Tools.Count > 0)
        {
            foreach (var tool in request.Tools)
            {
                options.Tools.Add(ConvertToChatTool(tool));
            }
        }

        return options;
    }

    private ChatTool ConvertToChatTool(LLMTool tool)
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

            // Only create tool call delta if we have a valid ID
            // OpenAI streams tool calls in chunks, early chunks may not have ID yet
            if (!string.IsNullOrWhiteSpace(toolUpdate.ToolCallId))
            {
                toolCallDelta = new LLMToolCall(
                    toolUpdate.ToolCallId,
                    toolUpdate.FunctionName ?? string.Empty,
                    toolUpdate.FunctionArgumentsUpdate?.ToString() ?? string.Empty);
            }
        }

        return new StreamingLLMChunk(contentDelta, toolCallDelta, isComplete, finishReason);
    }

    private void LogRequest(LLMRequest request)
    {
        var eventData = JsonSerializer.Serialize(new
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
        var eventData = JsonSerializer.Serialize(new
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

    private void LogRawRequest(LLMRequest request, List<ChatMessage> messages, ChatCompletionOptions options, string correlationId)
    {
        var rawData = new RawLLMRequestData(
            correlationId,
            ProviderName,
            SerializeRawRequest(request, messages, options),
            request.Messages.Count);

        var eventData = JsonSerializer.Serialize(rawData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.RawLLMRequest,
                eventData,
                $"Raw request to {ProviderName} (Correlation: {correlationId})"));
    }

    private void LogRawResponse(ChatCompletion response, string correlationId, TimeSpan latency)
    {
        // Enhanced logging for diagnostics - log tool call details BEFORE any conversion
        var diagnosticData = new
        {
            Provider = ProviderName,
            CorrelationId = correlationId,
            ResponseId = response.Id,
            Model = response.Model,
            ContentParts = response.Content.Select(c => new
            {
                Type = c.Kind.ToString(),
                Text = c.Text
            }).ToList(),
            ToolCallsCount = response.ToolCalls.Count,
            ToolCallsRaw = response.ToolCalls.Select(tc => new
            {
                Id = tc.Id,
                Kind = tc.Kind.ToString(),
                FunctionName = tc.FunctionName,
                FunctionNameIsNullOrEmpty = string.IsNullOrEmpty(tc.FunctionName),
                FunctionArguments = tc.FunctionArguments.ToString(),
                FunctionArgumentsIsNullOrEmpty = string.IsNullOrEmpty(tc.FunctionArguments.ToString())
            }).ToList(),
            FinishReason = response.FinishReason.ToString(),
            Usage = response.Usage != null ? new
            {
                InputTokens = response.Usage.InputTokenCount,
                OutputTokens = response.Usage.OutputTokenCount,
                TotalTokens = response.Usage.TotalTokenCount
            } : null,
            Timestamp = DateTime.UtcNow
        };

        var diagnosticJson = JsonSerializer.Serialize(diagnosticData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.SystemState,
                diagnosticJson,
                $"[DIAGNOSTIC] Raw ChatCompletion object details (Correlation: {correlationId})"));

        // Standard raw response logging
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

    private void LogStreamingResponse(string content, List<LLMToolCall>? toolCalls, string? finishReason, string correlationId, TimeSpan latency)
    {
        var responseData = new
        {
            Provider = ProviderName,
            Content = content,
            ToolCalls = toolCalls?.Select(tc => new
            {
                Id = tc.Id,
                Name = tc.Name,
                Arguments = tc.Arguments
            }).ToList(),
            FinishReason = finishReason ?? "unknown",
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
                $"Raw streaming response from {ProviderName} (Correlation: {correlationId}, Latency: {latency.TotalMilliseconds}ms)"));
    }

    private string SerializeRawRequest(LLMRequest request, List<ChatMessage> messages, ChatCompletionOptions options)
    {
        var requestData = new
        {
            Provider = ProviderName,
            Messages = messages.Select(m => new
            {
                Role = GetMessageRole(m),
                Content = GetMessageContent(m),
                ToolCalls = m is AssistantChatMessage assistantMsg && assistantMsg.ToolCalls.Count > 0
                    ? assistantMsg.ToolCalls.Select(tc => new
                    {
                        Id = tc.Id,
                        Name = tc.FunctionName,
                        Arguments = tc.FunctionArguments.ToString()
                    }).ToList()
                    : null,
                ToolCallId = m is ToolChatMessage toolMsg ? toolMsg.ToolCallId : null
            }).ToList(),
            Parameters = new
            {
                Temperature = options.Temperature,
                TopP = options.TopP,
                MaxTokens = options.MaxOutputTokenCount,
                Tools = options.Tools.Select(t => new
                {
                    Name = t.FunctionName,
                    Description = t.FunctionDescription,
                    Parameters = t.FunctionParameters.ToString()
                }).ToList()
            },
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
    }

    private string SerializeRawResponse(ChatCompletion response)
    {
        var responseData = new
        {
            Provider = ProviderName,
            Id = response.Id,
            Model = response.Model,
            Created = response.CreatedAt,
            Content = response.Content.Select(c => c.Text).ToList(),
            ToolCalls = response.ToolCalls.Select(tc => new
            {
                Id = tc.Id,
                Type = tc.Kind.ToString(),
                Function = new
                {
                    Name = tc.FunctionName,
                    Arguments = tc.FunctionArguments.ToString()
                }
            }).ToList(),
            FinishReason = response.FinishReason.ToString(),
            Usage = response.Usage != null
                ? new
                {
                    PromptTokens = response.Usage.InputTokenCount,
                    CompletionTokens = response.Usage.OutputTokenCount,
                    TotalTokens = response.Usage.TotalTokenCount
                }
                : null,
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true });
    }

    private string GetMessageRole(ChatMessage message)
    {
        return message switch
        {
            UserChatMessage => "user",
            AssistantChatMessage => "assistant",
            SystemChatMessage => "system",
            ToolChatMessage => "tool",
            _ => "unknown"
        };
    }

    private string? GetMessageContent(ChatMessage message)
    {
        return message switch
        {
            UserChatMessage userMsg => userMsg.Content.FirstOrDefault()?.Text,
            AssistantChatMessage assistantMsg =>
                assistantMsg.Content.Count > 0 ? assistantMsg.Content[0].Text : null,
            SystemChatMessage systemMsg => systemMsg.Content.FirstOrDefault()?.Text,
            ToolChatMessage toolMsg => toolMsg.Content.FirstOrDefault()?.Text,
            _ => null
        };
    }

    /// <summary>
    /// Safely converts BinaryData to string, handling cases where internal bytes may be null.
    /// </summary>
    private static string? SafeBinaryDataToString(BinaryData? data)
    {
        if (data == null)
            return null;

        try
        {
            return data.ToString();
        }
        catch (ArgumentNullException)
        {
            // BinaryData has null internal bytes
            return null;
        }
    }

    private void LogStreamingChunk(StreamingChatCompletionUpdate update, string correlationId)
    {
        // Log each streaming chunk for diagnostics
        var chunkData = new
        {
            CorrelationId = correlationId,
            Provider = ProviderName,
            ContentUpdateCount = update.ContentUpdate.Count,
            ContentUpdates = update.ContentUpdate.Select(c => new
            {
                Kind = c.Kind.ToString(),
                Text = c.Text,
                TextIsNullOrEmpty = string.IsNullOrEmpty(c.Text)
            }).ToList(),
            ToolCallUpdateCount = update.ToolCallUpdates.Count,
            ToolCallUpdates = update.ToolCallUpdates.Select(tc => new
            {
                Index = tc.Index,
                ToolCallId = tc.ToolCallId,
                ToolCallIdIsNullOrEmpty = string.IsNullOrEmpty(tc.ToolCallId),
                FunctionName = tc.FunctionName,
                FunctionNameIsNullOrEmpty = string.IsNullOrEmpty(tc.FunctionName),
                FunctionArgumentsUpdate = SafeBinaryDataToString(tc.FunctionArgumentsUpdate),
                FunctionArgumentsUpdateIsNullOrEmpty = string.IsNullOrEmpty(SafeBinaryDataToString(tc.FunctionArgumentsUpdate))
            }).ToList(),
            FinishReason = update.FinishReason?.ToString(),
            Timestamp = DateTime.UtcNow
        };

        var chunkJson = JsonSerializer.Serialize(chunkData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.SystemState,
                chunkJson,
                $"[DIAGNOSTIC] Streaming chunk received (Correlation: {correlationId})"));
    }

    private void LogAccumulatedToolCalls(List<LLMToolCall> accumulatedToolCalls, string correlationId)
    {
        // Log accumulated tool calls BEFORE final response creation
        var accumulatedData = new
        {
            CorrelationId = correlationId,
            Provider = ProviderName,
            AccumulatedToolCallsCount = accumulatedToolCalls.Count,
            AccumulatedToolCalls = accumulatedToolCalls.Select(tc => new
            {
                Id = tc.Id,
                IdIsNullOrEmpty = string.IsNullOrEmpty(tc.Id),
                Name = tc.Name,
                NameIsNullOrEmpty = string.IsNullOrEmpty(tc.Name),
                Arguments = tc.Arguments,
                ArgumentsIsNullOrEmpty = string.IsNullOrEmpty(tc.Arguments)
            }).ToList(),
            Timestamp = DateTime.UtcNow
        };

        var accumulatedJson = JsonSerializer.Serialize(accumulatedData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new Domain.Transparency.TransparencyEvent(
                Domain.Transparency.TransparencyEventType.SystemState,
                accumulatedJson,
                $"[DIAGNOSTIC] Accumulated tool calls from streaming (Correlation: {correlationId})"));
    }
}
