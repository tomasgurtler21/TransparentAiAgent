using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

public class AssistantMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.Assistant;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    public AssistantMessage(string content)
    {
        // Allow empty content, but not null (empty content is valid for responses that only contain tool calls)
        if (content == null)
            throw new ArgumentException("Content cannot be null", nameof(content));

        Id = Guid.NewGuid();
        Content = content;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }
}
