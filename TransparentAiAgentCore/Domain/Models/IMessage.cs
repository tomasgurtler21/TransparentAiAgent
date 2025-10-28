using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

public interface IMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Role of the message sender
    /// </summary>
    MessageRole Role { get; }

    /// <summary>
    /// Text content of the message
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Timestamp when message was created
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Whether message is currently in LLM context window
    /// </summary>
    MessageContextStatus ContextStatus { get; set; }
}
