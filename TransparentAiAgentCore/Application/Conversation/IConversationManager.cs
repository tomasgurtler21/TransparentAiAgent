using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Application.Conversation;

/// <summary>
/// Manages conversation history and context window
/// </summary>
public interface IConversationManager
{
    /// <summary>
    /// Add a message to conversation history
    /// </summary>
    void AddMessage(IMessage message);

    /// <summary>
    /// Get all messages in conversation history (including truncated ones)
    /// </summary>
    IReadOnlyList<IMessage> GetAllMessages();

    /// <summary>
    /// Get only messages currently in LLM context window
    /// </summary>
    IReadOnlyList<IMessage> GetInContextMessages();

    /// <summary>
    /// Get conversation ID
    /// </summary>
    Guid ConversationId { get; }

    /// <summary>
    /// Get context window size limit
    /// </summary>
    int ContextWindowSize { get; }

    /// <summary>
    /// Get current count of messages in context
    /// </summary>
    int InContextMessageCount { get; }

    /// <summary>
    /// Clear all messages from conversation
    /// </summary>
    void ClearConversation();

    /// <summary>
    /// Updates the system prompt by replacing the first SystemMessage.
    /// If no SystemMessage exists, creates a new one.
    /// </summary>
    void UpdateSystemPrompt(string newPrompt);

    /// <summary>
    /// Updates the context window size for the conversation.
    /// </summary>
    void UpdateContextWindowSize(int newSize);

    /// <summary>
    /// Event raised when a message's context status changes
    /// </summary>
    event EventHandler<ContextStatusChangedEventArgs>? ContextStatusChanged;
}
