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
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = Guid.NewGuid();
        Content = content;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }
}
