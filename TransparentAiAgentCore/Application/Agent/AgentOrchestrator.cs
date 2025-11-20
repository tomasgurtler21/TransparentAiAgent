using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Domain.Transparency.EventData;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace TransparentAiAgentCore.Application.Agent;

/// <summary>
/// Orchestrates agent interactions and conversation flow
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ILLMProvider _llmProvider;
    private readonly IConversationManager _conversationManager;
    private readonly IMessagePipeline _messagePipeline;
    private readonly ITransparencyService _transparencyService;
    private readonly IToolManager? _toolManager;
    private readonly AgentConfiguration _agentConfig;
    private readonly LLMConfiguration _llmConfig;
    private readonly bool _useRest;
    private const int MaxToolCallDepth = 10;

    public IConversationManager ConversationManager => _conversationManager;

    public AgentOrchestrator(
        ILLMProvider llmProvider,
        IConversationManager conversationManager,
        IMessagePipeline messagePipeline,
        ITransparencyService transparencyService,
        AppConfiguration configuration,
        IToolManager? toolManager = null)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _messagePipeline = messagePipeline ?? throw new ArgumentNullException(nameof(messagePipeline));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _toolManager = toolManager; // Optional - null if tools not configured

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _agentConfig = configuration.Agent;
        _llmConfig = configuration.LLM;
        _useRest = _llmConfig.UseRest;

        // Add system message to conversation
        var systemMessage = new SystemMessage(_agentConfig.SystemPrompt);
        _conversationManager.AddMessage(systemMessage);

        LogEvent("AgentInitialized", $"Agent orchestrator initialized (Mode: {(_useRest ? "REST" : "Streaming")})");
    }

    public async Task<IMessage> ProcessUserInputAsync(
        UserMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        try
        {
            // 1. Add user message to conversation
            _conversationManager.AddMessage(message);
            LogEvent("UserInput", $"User input: {message.Content}");

            // 2. Tool calling loop (with max depth protection)
            IMessage finalMessage = await ProcessWithToolLoopAsync(0, cancellationToken);

            return finalMessage;
        }
        catch (Exception ex) when (ex is not AgentException and not LLMException)
        {
            LogEvent("Error", $"Unexpected error: {ex.Message}");
            throw new AgentException($"Error processing user input: {ex.Message}", ex);
        }
    }

    private async Task<IMessage> ProcessWithToolLoopAsync(int depth, CancellationToken cancellationToken)
    {
        // Prevent infinite tool loops
        if (depth >= MaxToolCallDepth)
        {
            LogEvent("ToolLoopMaxDepthReached", $"Maximum tool call depth ({MaxToolCallDepth}) reached");
            var errorMessage = new LlmTextMessage(
                $"I've reached the maximum number of tool calls ({MaxToolCallDepth}). Please try rephrasing your request.");
            _conversationManager.AddMessage(errorMessage);
            return errorMessage;
        }

        // Build LLM request from in-context messages
        var llmRequest = BuildLLMRequest();

        // Send to LLM
        LogEvent("LLMRequestSent", $"Sending request to LLM (depth: {depth})");
        var llmResponse = await _llmProvider.SendRequestAsync(llmRequest, cancellationToken);
        LogEvent("LLMResponseReceived", $"Received response. Content length: {llmResponse.Content?.Length ?? 0}, Tool calls: {llmResponse.ToolCalls?.Count ?? 0}");

        // Check if LLM wants to call tools
        if (llmResponse.ToolCalls != null && llmResponse.ToolCalls.Count > 0 && _toolManager != null)
        {
            // Execute tools using extracted method
            await ExecuteToolCallsAsync(
                llmResponse.Content ?? string.Empty,
                llmResponse.ToolCalls,
                llmResponse.Thinking,
                cancellationToken);

            // Continue loop with tool results
            return await ProcessWithToolLoopAsync(depth + 1, cancellationToken);
        }
        else
        {
            try
            {
                // No tool calls - convert response to message and return
                var assistantMessage = _messagePipeline.ConvertToDomainMessage(llmResponse);
                _conversationManager.AddMessage(assistantMessage);
                return assistantMessage;
            }
            catch (Exception ex)
            {
                // Log parsing error
                LogParsingError(llmResponse, ex);
                throw; // Re-throw to maintain error handling behavior
            }
        }
    }

    /// <summary>
    /// Executes tool calls from LLM response and adds messages to conversation.
    /// Extracted method to be reused by both streaming and non-streaming paths.
    /// </summary>
    /// <param name="assistantContent">The text content from the LLM response</param>
    /// <param name="llmToolCalls">List of tool calls from the LLM</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ExecuteToolCallsAsync(
        string assistantContent,
        List<LLMToolCall> llmToolCalls,
        string? thinking,
        CancellationToken cancellationToken)
    {
        if (_toolManager == null)
        {
            LogEvent("ToolExecutionSkipped", "Tool manager not available");
            return;
        }

        try
        {
            // DIAGNOSTIC: Log LLMToolCalls BEFORE conversion
            LogToolCallsBeforeConversion(llmToolCalls);

            // Convert LLMToolCall list to ToolCall list
            var toolCalls = llmToolCalls
                .Select(tc => new ToolCall(tc.Id, tc.Name, tc.Arguments))
                .ToList();

            // Create ONE assistant message with ALL tool calls
            var assistantToolCallMessage = new LlmToolCallMessage(
                assistantContent,
                toolCalls,
                thinking);
            _conversationManager.AddMessage(assistantToolCallMessage);
        }
        catch (Exception ex)
        {
            // Log parsing error and re-throw
            LogEvent("ToolCallConversionError", $"Failed to convert tool calls: {ex.Message}");
            throw;
        }

        // Execute each tool and add result messages
        foreach (var toolCall in llmToolCalls)
        {
            try
            {
                // Execute tool
                LogEvent("ToolExecutionStarted", $"Executing tool: {toolCall.Name}");
                var toolResult = await _toolManager.ExecuteToolCallAsync(toolCall, cancellationToken);
                LogEvent("ToolExecutionCompleted", $"Tool {toolCall.Name} completed. Success: {toolResult.IsSuccess}");

                // Add tool result message to conversation
                var toolResultMessage = new ToolResultMessage(
                    toolCall.Id,
                    toolCall.Name,
                    toolResult.Content,
                    !toolResult.IsSuccess); // IsError = !IsSuccess
                _conversationManager.AddMessage(toolResultMessage);
            }
            catch (Exception ex)
            {
                // Log error but continue with other tools
                LogEvent("ToolExecutionError", $"Error executing tool {toolCall.Name}: {ex.Message}");

                // Add error result message with error description as content
                var errorResultMessage = new ToolErrorMessage(
                    toolCall.Id,
                    toolCall.Name,
                    ex.Message,
                    ex);
                _conversationManager.AddMessage(errorResultMessage);
            }
        }
    }

    /// <summary>
    /// Recursive streaming method that handles tool loops.
    /// Streams LLM response, accumulates tool calls, executes tools, and recursively continues if needed.
    /// </summary>
    /// <param name="depth">Current recursion depth (for infinite loop protection)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Streaming chunks including text content and status updates</returns>
    private async IAsyncEnumerable<StreamingResponseChunk> ProcessStreamingToolLoopAsync(
        int depth,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Prevent infinite tool loops
        if (depth >= MaxToolCallDepth)
        {
            LogEvent("ToolLoopMaxDepthReached", $"Maximum tool call depth ({MaxToolCallDepth}) reached in streaming");
            var errorMessage = $"I've reached the maximum number of tool calls ({MaxToolCallDepth}). Please try rephrasing your request.";

            // Add error message to conversation
            var errorAssistantMessage = new LlmTextMessage(errorMessage);
            _conversationManager.AddMessage(errorAssistantMessage);

            // Yield error chunk and complete
            yield return new StreamingResponseChunk(errorMessage, IsComplete: true, Status: StreamingStatus.Error);
            yield break;
        }

        // Build LLM request from current conversation state
        var llmRequest = BuildLLMRequest();

        var contentBuilder = new StringBuilder();
        List<LLMToolCall>? accumulatedToolCalls = null;
        string? accumulatedThinking = null;

        if (_useRest)
        {
            // REST mode: Use non-streaming API and convert to streaming chunks
            LogEvent("LLMRestRequestSent", $"Sending REST request to LLM (depth: {depth})");

            var response = await _llmProvider.SendRequestAsync(llmRequest, cancellationToken);

            // Convert response to streaming chunks
            if (!string.IsNullOrEmpty(response.Content))
            {
                contentBuilder.Append(response.Content);
                // Yield entire content as single chunk
                yield return new StreamingResponseChunk(response.Content, IsComplete: false, Status: StreamingStatus.Streaming);
            }

            accumulatedToolCalls = response.ToolCalls ?? new List<LLMToolCall>();
            accumulatedThinking = response.Thinking;

            LogEvent("LLMRestCompleted", $"REST request completed (depth: {depth}). Content length: {contentBuilder.Length}, Tool calls: {accumulatedToolCalls.Count}");
        }
        else
        {
            // Streaming mode: Use streaming API
            LogEvent("LLMStreamRequestSent", $"Sending streaming request to LLM (depth: {depth})");

            await foreach (var chunk in _llmProvider.StreamRequestAsync(llmRequest, cancellationToken))
            {
                // Accumulate text content
                if (!string.IsNullOrEmpty(chunk.ContentDelta))
                {
                    contentBuilder.Append(chunk.ContentDelta);
                    // Yield text content immediately for real-time streaming
                    yield return new StreamingResponseChunk(chunk.ContentDelta, IsComplete: false, Status: StreamingStatus.Streaming);
                }

                // Capture accumulated tool calls from final chunk
                if (chunk.AccumulatedToolCalls != null && chunk.AccumulatedToolCalls.Count > 0)
                {
                    accumulatedToolCalls = chunk.AccumulatedToolCalls;
                    LogEvent("LLMStreamCompleted", $"Stream completed (depth: {depth}). Content length: {contentBuilder.Length}, Tool calls: {accumulatedToolCalls.Count}");
                }

                // Capture accumulated thinking from final chunk
                if (!string.IsNullOrEmpty(chunk.AccumulatedThinking))
                {
                    accumulatedThinking = chunk.AccumulatedThinking;
                }

                // Check if stream is complete
                if (chunk.IsComplete && accumulatedToolCalls == null)
                {
                    LogEvent("LLMStreamCompleted", $"Stream completed (depth: {depth}). Content length: {contentBuilder.Length}, No tool calls");
                    break;
                }
            }
        }

        // Use accumulated tool calls from provider (or empty list if none)
        accumulatedToolCalls ??= new List<LLMToolCall>();

        // Check if LLM wants to call tools
        if (accumulatedToolCalls.Count > 0 && _toolManager != null)
        {
            // Yield status update: executing tools
            yield return new StreamingResponseChunk(null, IsComplete: false, Status: StreamingStatus.ExecutingTools);
            LogEvent("ToolExecutionStarting", $"Starting execution of {accumulatedToolCalls.Count} tool(s) in streaming mode");

            // Execute tools using extracted method
            await ExecuteToolCallsAsync(
                contentBuilder.ToString(),
                accumulatedToolCalls,
                accumulatedThinking,
                cancellationToken);

            LogEvent("ToolExecutionCompleted", $"Completed execution of {accumulatedToolCalls.Count} tool(s)");

            // Recursive call to continue tool loop
            await foreach (var chunk in ProcessStreamingToolLoopAsync(depth + 1, cancellationToken))
            {
                yield return chunk;
            }
        }
        else
        {
            // No tool calls - add assistant message and complete
            var assistantMessage = new LlmTextMessage(contentBuilder.ToString(), accumulatedThinking);
            _conversationManager.AddMessage(assistantMessage);

            // Yield final completion chunk
            yield return new StreamingResponseChunk(null, IsComplete: true, Status: StreamingStatus.Completed);
        }
    }

    public async IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
        UserMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        // 1. Add user message to conversation
        _conversationManager.AddMessage(message);
        LogEvent("UserInput", $"User input (streaming): {message.Content}");

        // 2. Start streaming tool loop from depth 0
        await foreach (var chunk in ProcessStreamingToolLoopAsync(0, cancellationToken))
        {
            yield return chunk;
        }
    }

    public void StartNewConversation()
    {
        _conversationManager.ClearConversation();

        // Add system message to new conversation
        var systemMessage = new SystemMessage(_agentConfig.SystemPrompt);
        _conversationManager.AddMessage(systemMessage);

        LogEvent("ConversationRestarted", "Started new conversation");
    }

    public async IAsyncEnumerable<StreamingResponseChunk> ProcessApplicationMessageAsync(
        ApplicationMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        // Add application message to conversation
        _conversationManager.AddMessage(message);
        LogEvent("ApplicationMessageAdded", $"Application message added: {message.GetType().Name}");

        // Route based on message type
        switch (message)
        {
            case ScenarioUserMessage scenarioUserMessage:
                // ScenarioUserMessage acts like user input - trigger LLM processing
                LogEvent("ScenarioUserMessageProcessing", $"Processing scenario user message: {scenarioUserMessage.Content}");
                await foreach (var chunk in ProcessStreamingToolLoopAsync(0, cancellationToken))
                {
                    yield return chunk;
                }
                break;

            case ScenarioAssistantMessage scenarioAssistantMessage:
                // ScenarioAssistantMessage is a scripted response - no LLM call needed
                LogEvent("ScenarioAssistantMessageAdded", $"Added scenario assistant message: {scenarioAssistantMessage.Content}");
                // Just yield completion chunk
                yield return new StreamingResponseChunk(null, IsComplete: true, Status: StreamingStatus.Completed);
                break;

            default:
                var errorMessage = $"Application message type {message.GetType().Name} is not supported";
                LogEvent("UnsupportedApplicationMessageType", errorMessage);
                throw new NotSupportedException(errorMessage);
        }
    }

    public async IAsyncEnumerable<StreamingResponseChunk> ProcessToolMessageAsync(
        ToolMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        // Add tool message to conversation
        _conversationManager.AddMessage(message);
        LogEvent("ToolMessageAdded", $"Tool message added: {message.GetType().Name}");

        // Route based on message type for detailed logging
        switch (message)
        {
            case ToolResultMessage toolResultMessage:
                LogEvent("ToolResultMessageProcessing",
                    $"Processing tool result from {toolResultMessage.ToolName} (success: {!toolResultMessage.IsError})");
                break;

            case ToolErrorMessage toolErrorMessage:
                LogEvent("ToolErrorMessageProcessing",
                    $"Processing tool error from {toolErrorMessage.ToolName}: {toolErrorMessage.ErrorMessage}");
                break;

            default:
                LogEvent("UnknownToolMessageType", $"Processing unknown tool message type: {message.GetType().Name}");
                break;
        }

        // Tool messages always trigger LLM processing (both results and errors need LLM response)
        await foreach (var chunk in ProcessStreamingToolLoopAsync(0, cancellationToken))
        {
            yield return chunk;
        }
    }

    private LLMRequest BuildLLMRequest()
    {
        // Get messages that are in context
        var inContextMessages = _conversationManager.GetInContextMessages();

        // Convert to LLM messages
        var llmMessages = _messagePipeline.ConvertToLLMMessages(inContextMessages);

        // Get tool definitions from tool manager (if available)
        List<LLMTool>? tools = null;
        if (_toolManager != null)
        {
            try
            {
                LogEvent("ToolManagerAvailable", $"Tool manager is available, attempting to get tool definitions");
                tools = _toolManager.GetLLMToolDefinitions();
                LogEvent("ToolsRetrieved", $"Retrieved {tools?.Count ?? 0} tools from tool manager");

                if (tools != null && tools.Count > 0)
                {
                    LogEvent("ToolsIncluded", $"Including {tools.Count} tools in LLM request: {string.Join(", ", tools.Select(t => t.Name))}");
                }
                else
                {
                    LogEvent("ToolsEmpty", "Tool manager returned 0 tools or null");
                }
            }
            catch (Exception ex)
            {
                LogEvent("ToolsError", $"Failed to get tool definitions: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
        else
        {
            LogEvent("ToolManagerNull", "Tool manager is NULL - tools will not be included in request");
        }

        // Build request
        return new LLMRequest(
            llmMessages,
            _llmConfig.Temperature,
            _llmConfig.TopP,
            _llmConfig.MaxTokens,
            stream: !_useRest,  // Use REST when _useRest is true, streaming otherwise
            tools: tools);
    }

    private void LogEvent(string eventType, string details)
    {
        _transparencyService?.LogEvent(
            new TransparencyEvent(
                Domain.Transparency.TransparencyEventType.SystemState,
                System.Text.Json.JsonSerializer.Serialize(new { EventType = eventType }),
                details));
    }

    private void LogParsingError(LLMResponse llmResponse, Exception exception)
    {
        var parsingEventData = new MessageParsingEventData(
            JsonSerializer.Serialize(llmResponse, new JsonSerializerOptions { WriteIndented = true }),
            null,
            false,
            $"{exception.GetType().Name}: {exception.Message}");

        var eventData = JsonSerializer.Serialize(parsingEventData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new TransparencyEvent(
                TransparencyEventType.MessageParsingError,
                eventData,
                $"Failed to parse LLM response: {exception.Message}"));
    }

    private void LogToolCallsBeforeConversion(List<LLMToolCall> toolCalls)
    {
        // Log detailed diagnostic information about each LLMToolCall BEFORE attempting conversion
        var diagnosticData = new
        {
            ToolCallCount = toolCalls.Count,
            ToolCalls = toolCalls.Select((tc, index) => new
            {
                Index = index,
                Id = tc.Id,
                IdIsNull = tc.Id == null,
                IdIsEmpty = string.IsNullOrEmpty(tc.Id),
                IdIsWhitespace = string.IsNullOrWhiteSpace(tc.Id),
                Name = tc.Name,
                NameIsNull = tc.Name == null,
                NameIsEmpty = string.IsNullOrEmpty(tc.Name),
                NameIsWhitespace = string.IsNullOrWhiteSpace(tc.Name),
                Arguments = tc.Arguments,
                ArgumentsIsNull = tc.Arguments == null,
                ArgumentsIsEmpty = string.IsNullOrEmpty(tc.Arguments),
                ArgumentsLength = tc.Arguments?.Length ?? 0
            }).ToList(),
            Timestamp = DateTime.UtcNow
        };

        var diagnosticJson = JsonSerializer.Serialize(diagnosticData, new JsonSerializerOptions { WriteIndented = true });

        _transparencyService.LogEvent(
            new TransparencyEvent(
                TransparencyEventType.SystemState,
                diagnosticJson,
                $"[DIAGNOSTIC] LLMToolCalls before conversion to ToolCalls (Count: {toolCalls.Count})"));
    }
}
