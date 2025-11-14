using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Messages originating from LLM responses.
/// Represents AI agent outputs.
/// </summary>
public abstract class LlmMessage : IMessage
{
    public Guid Id { get; protected set; }
    public MessageRole Role => MessageRole.Assistant;
    public string Content { get; protected set; } = string.Empty;
    public DateTime Timestamp { get; protected set; }
    public MessageContextStatus ContextStatus { get; set; }
    public abstract string MessageTypeDiscriminator { get; }
}
