using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Application.Conversation;

namespace TransparentAiAgentCore.Application.Agent;

/// <summary>
/// Orchestrates agent interactions and conversation flow
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Process user message and generate response.
    /// Accepts UserMessage (abstract base) - use DirectUserMessage for direct user input from UI.
    /// </summary>
    /// <param name="message">The user message to process (e.g., DirectUserMessage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IMessage> ProcessUserInputAsync(UserMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process user message and stream response chunks.
    /// Accepts UserMessage (abstract base) - use DirectUserMessage for direct user input from UI.
    /// </summary>
    /// <param name="message">The user message to process (e.g., DirectUserMessage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
        UserMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversation manager
    /// </summary>
    IConversationManager ConversationManager { get; }

    /// <summary>
    /// Start a new conversation (clears history)
    /// </summary>
    void StartNewConversation();

    /// <summary>
    /// Process an application-originated message (e.g., from scenario playback) and stream response chunks
    /// </summary>
    /// <param name="message">The application message to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Streaming chunks including text content and status updates</returns>
    IAsyncEnumerable<StreamingResponseChunk> ProcessApplicationMessageAsync(
        ApplicationMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a tool-originated message (e.g., tool result or error) and stream response chunks
    /// </summary>
    /// <param name="message">The tool message to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Streaming chunks including text content and status updates</returns>
    IAsyncEnumerable<StreamingResponseChunk> ProcessToolMessageAsync(
        ToolMessage message,
        CancellationToken cancellationToken = default);
}
