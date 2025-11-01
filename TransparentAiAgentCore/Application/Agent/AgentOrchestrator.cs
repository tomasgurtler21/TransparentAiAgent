using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Domain.Transparency;
using System.Runtime.CompilerServices;
using System.Text;

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

        // Add system message to conversation
        var systemMessage = new SystemMessage(_agentConfig.SystemPrompt);
        _conversationManager.AddMessage(systemMessage);

        LogEvent("AgentInitialized", "Agent orchestrator initialized");
    }

    public async Task<IMessage> ProcessUserInputAsync(
        string userInput,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ArgumentException("User input cannot be null or whitespace", nameof(userInput));

        try
        {
            // 1. Add user message to conversation
            var userMessage = new UserMessage(userInput);
            _conversationManager.AddMessage(userMessage);
            LogEvent("UserInput", $"User input: {userInput}");

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
            var errorMessage = new AssistantMessage(
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
            // Convert LLMToolCall list to ToolCall list
            var toolCalls = llmResponse.ToolCalls
                .Select(tc => new ToolCall(tc.Id, tc.Name, tc.Arguments))
                .ToList();

            // Create ONE assistant message with ALL tool calls
            var assistantToolCallMessage = new AssistantToolCallMessage(
                llmResponse.Content ?? string.Empty,
                toolCalls);
            _conversationManager.AddMessage(assistantToolCallMessage);

            // Execute each tool and add result messages
            foreach (var toolCall in llmResponse.ToolCalls)
            {
                // Execute tool
                var toolResult = await _toolManager.ExecuteToolCallAsync(toolCall, cancellationToken);

                // Add tool result message to conversation
                var toolResultMessage = new ToolResultMessage(
                    toolCall.Id,
                    toolCall.Name,
                    toolResult.Content,
                    toolResult.IsSuccess,
                    toolResult.ErrorMessage);
                _conversationManager.AddMessage(toolResultMessage);
            }

            // Continue loop with tool results
            return await ProcessWithToolLoopAsync(depth + 1, cancellationToken);
        }
        else
        {
            // No tool calls - convert response to message and return
            var assistantMessage = _messagePipeline.ConvertToDomainMessage(llmResponse);
            _conversationManager.AddMessage(assistantMessage);
            return assistantMessage;
        }
    }

    public async IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
        string userInput,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ArgumentException("User input cannot be null or whitespace", nameof(userInput));

        // 1. Add user message to conversation
        var userMessage = new UserMessage(userInput);
        _conversationManager.AddMessage(userMessage);
        LogEvent("UserInput", $"User input (streaming): {userInput}");

        // 2. Build LLM request from in-context messages
        var llmRequest = BuildLLMRequest();
        llmRequest = new LLMRequest(
            llmRequest.Messages,
            llmRequest.Temperature,
            llmRequest.TopP,
            llmRequest.MaxTokens,
            stream: true,
            llmRequest.Tools);

        // 3. Stream from LLM
        LogEvent("LLMStreamRequestSent", "Sending streaming request to LLM");

        var contentBuilder = new StringBuilder();

        await foreach (var chunk in _llmProvider.StreamRequestAsync(llmRequest, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.ContentDelta))
            {
                contentBuilder.Append(chunk.ContentDelta);
            }

            yield return new StreamingResponseChunk(chunk.ContentDelta, chunk.IsComplete);

            if (chunk.IsComplete)
            {
                LogEvent("LLMStreamCompleted", $"Stream completed. Total content length: {contentBuilder.Length}");
            }
        }

        // 4. Create assistant message from accumulated content
        var assistantMessage = new AssistantMessage(contentBuilder.ToString());
        _conversationManager.AddMessage(assistantMessage);
    }

    public void StartNewConversation()
    {
        _conversationManager.ClearConversation();

        // Add system message to new conversation
        var systemMessage = new SystemMessage(_agentConfig.SystemPrompt);
        _conversationManager.AddMessage(systemMessage);

        LogEvent("ConversationRestarted", "Started new conversation");
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
                tools = _toolManager.GetLLMToolDefinitions();
                if (tools.Count > 0)
                {
                    LogEvent("ToolsIncluded", $"Including {tools.Count} tools in LLM request");
                }
            }
            catch (Exception ex)
            {
                LogEvent("ToolsError", $"Failed to get tool definitions: {ex.Message}");
            }
        }

        // Build request
        return new LLMRequest(
            llmMessages,
            _llmConfig.Temperature,
            _llmConfig.TopP,
            _llmConfig.MaxTokens,
            stream: false,
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
}
