namespace TransparentAiAgentCore.Domain.ConversationHistory;

/// <summary>
/// Repository interface for persisting and retrieving conversations.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Saves a conversation to persistent storage.
    /// Creates a new conversation if it doesn't exist, or updates if it does.
    /// </summary>
    Task SaveAsync(Conversation conversation);

    /// <summary>
    /// Loads a conversation by its ID.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when conversation doesn't exist</exception>
    Task<Conversation> LoadAsync(Guid conversationId);

    /// <summary>
    /// Gets metadata for all conversations, sorted by most recent first.
    /// Does not load full message history for performance.
    /// </summary>
    Task<List<ConversationMetadata>> ListAllAsync();

    /// <summary>
    /// Deletes a conversation by its ID.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when conversation doesn't exist</exception>
    Task DeleteAsync(Guid conversationId);

    /// <summary>
    /// Checks if a conversation exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid conversationId);
}
