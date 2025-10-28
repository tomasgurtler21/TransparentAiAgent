using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentGui.Models;

/// <summary>
/// View model for displaying messages in UI
/// </summary>
public class UIMessage
{
    public Guid Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MessageContextStatus ContextStatus { get; set; }

    /// <summary>
    /// CSS class for styling based on role
    /// </summary>
    public string CssClass => Role switch
    {
        MessageRole.User => "message-user",
        MessageRole.Assistant => "message-assistant",
        MessageRole.System => "message-system",
        MessageRole.Tool => "message-tool",
        _ => "message-default"
    };

    /// <summary>
    /// Icon to display for context status
    /// </summary>
    public string ContextStatusIcon => ContextStatus switch
    {
        MessageContextStatus.InContext => "✅",
        MessageContextStatus.TruncatedFromContext => "⚠️",
        _ => ""
    };

    /// <summary>
    /// Tooltip text for context status
    /// </summary>
    public string ContextStatusTooltip => ContextStatus switch
    {
        MessageContextStatus.InContext => "In LLM Context",
        MessageContextStatus.TruncatedFromContext => "Not in LLM Context (truncated)",
        _ => ""
    };

    /// <summary>
    /// Create UIMessage from domain message
    /// </summary>
    public static UIMessage FromDomainMessage(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return new UIMessage
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            ContextStatus = message.ContextStatus
        };
    }
}
