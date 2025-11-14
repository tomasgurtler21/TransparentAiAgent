using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Standard text response from LLM.
/// </summary>
public class LlmTextMessage : LlmMessage
{
    public override string MessageTypeDiscriminator => "Llm.Text";

    /// <summary>
    /// Extended thinking content from the LLM (e.g., Anthropic's thinking blocks).
    /// This represents the model's internal reasoning process.
    /// </summary>
    public string? Thinking { get; set; }

    public LlmTextMessage(string content, string? thinking = null)
    {
        // Allow empty content, but not null (empty content is valid for responses that only contain tool calls)
        if (content == null)
            throw new ArgumentException("Content cannot be null", nameof(content));

        Id = Guid.NewGuid();
        Content = content;
        Thinking = thinking;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }
}
