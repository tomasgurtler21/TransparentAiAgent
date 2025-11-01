namespace TransparentAiAgentGui.Models;

/// <summary>
/// Represents a streaming message update event
/// </summary>
public class StreamingMessageUpdate
{
    /// <summary>
    /// ID of the message being streamed
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Current accumulated content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether streaming is complete for this message
    /// </summary>
    public bool IsComplete { get; set; }
}
