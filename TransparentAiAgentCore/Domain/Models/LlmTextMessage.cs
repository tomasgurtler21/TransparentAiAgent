using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Standard text response from LLM.
/// </summary>
public class LlmTextMessage : LlmMessage
{
    public override string MessageTypeDiscriminator => "Llm.Text";

    public LlmTextMessage(string content)
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
