using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Application.ConversationHistory;

/// <summary>
/// Manages conversation history operations including saving, loading, and listing conversations.
/// </summary>
public interface IConversationHistoryManager
{
    /// <summary>
    /// Saves the current conversation with the given messages.
    /// Skips save if no messages provided. Generates name from messages if new conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="messages">The messages to save</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SaveCurrentConversationAsync(Guid conversationId, IReadOnlyList<IMessage> messages);

    /// <summary>
    /// Loads a conversation by its ID.
    /// </summary>
    /// <param name="conversationId">The conversation ID to load</param>
    /// <returns>The loaded conversation</returns>
    /// <exception cref="FileNotFoundException">Thrown if conversation not found</exception>
    Task<Domain.ConversationHistory.Conversation> LoadConversationAsync(Guid conversationId);

    /// <summary>
    /// Gets a list of all conversation metadata, sorted by most recent first.
    /// </summary>
    /// <returns>List of conversation metadata</returns>
    Task<IReadOnlyList<ConversationMetadata>> GetConversationListAsync();

    /// <summary>
    /// Creates a new conversation ID without saving.
    /// The conversation will be saved when first message is added.
    /// </summary>
    /// <returns>A new conversation ID</returns>
    Task<Guid> CreateNewConversationAsync();

    /// <summary>
    /// Deletes a conversation by its ID.
    /// </summary>
    /// <param name="conversationId">The conversation ID to delete</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="FileNotFoundException">Thrown if conversation not found</exception>
    Task DeleteConversationAsync(Guid conversationId);
}
