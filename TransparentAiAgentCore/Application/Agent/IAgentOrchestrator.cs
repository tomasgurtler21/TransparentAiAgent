using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Application.Conversation;

namespace TransparentAiAgentCore.Application.Agent;

/// <summary>
/// Orchestrates agent interactions and conversation flow
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Process user input and generate response
    /// </summary>
    Task<IMessage> ProcessUserInputAsync(string userInput, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process user input and stream response chunks
    /// </summary>
    IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
        string userInput,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversation manager
    /// </summary>
    IConversationManager ConversationManager { get; }

    /// <summary>
    /// Start a new conversation (clears history)
    /// </summary>
    void StartNewConversation();
}
