using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
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
    private readonly AgentConfiguration _agentConfig;
    private readonly LLMConfiguration _llmConfig;

    public IConversationManager ConversationManager => _conversationManager;

    public AgentOrchestrator(
        ILLMProvider llmProvider,
        IConversationManager conversationManager,
        IMessagePipeline messagePipeline,
        ITransparencyService transparencyService,
        AppConfiguration configuration)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _messagePipeline = messagePipeline ?? throw new ArgumentNullException(nameof(messagePipeline));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));

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

            // 2. Build LLM request from in-context messages
            var llmRequest = BuildLLMRequest();

            // 3. Send to LLM
            LogEvent("LLMRequestSent", "Sending request to LLM");
            var llmResponse = await _llmProvider.SendRequestAsync(llmRequest, cancellationToken);
            LogEvent("LLMResponseReceived", $"Received response from LLM. Content length: {llmResponse.Content?.Length ?? 0}");

            // 4. Convert LLM response to domain message
            var assistantMessage = _messagePipeline.ConvertToDomainMessage(llmResponse);

            // 5. Add assistant message to conversation
            _conversationManager.AddMessage(assistantMessage);

            return assistantMessage;
        }
        catch (Exception ex) when (ex is not AgentException and not LLMException)
        {
            LogEvent("Error", $"Unexpected error: {ex.Message}");
            throw new AgentException($"Error processing user input: {ex.Message}", ex);
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

        // Build request
        return new LLMRequest(
            llmMessages,
            _llmConfig.Temperature,
            _llmConfig.TopP,
            _llmConfig.MaxTokens,
            stream: false,
            tools: null); // Tools will be added in Phase 5
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
