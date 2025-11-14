namespace TransparentAiAgentCore.Domain.ConversationHistory;

/// <summary>
/// Lightweight metadata for a conversation, used for listing without loading full message history.
/// </summary>
public class ConversationMetadata
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Human-readable name for the conversation
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When the conversation was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification time
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Number of messages in the conversation
    /// </summary>
    public int MessageCount { get; set; }
}
