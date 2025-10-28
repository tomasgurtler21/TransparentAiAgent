using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Application.Conversation;

/// <summary>
/// Event args for context status changes
/// </summary>
public class ContextStatusChangedEventArgs : EventArgs
{
    public Guid MessageId { get; }
    public MessageContextStatus OldStatus { get; }
    public MessageContextStatus NewStatus { get; }

    public ContextStatusChangedEventArgs(
        Guid messageId,
        MessageContextStatus oldStatus,
        MessageContextStatus newStatus)
    {
        MessageId = messageId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
