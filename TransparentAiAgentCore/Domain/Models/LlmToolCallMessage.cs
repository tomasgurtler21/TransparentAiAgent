using System.Text.Json.Serialization;
using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// LLM response that includes tool calls.
/// </summary>
public class LlmToolCallMessage : LlmMessage
{
    public override string MessageTypeDiscriminator => "Llm.ToolCall";

    /// <summary>
    /// Tool calls requested by the LLM.
    /// Always contains at least one tool call.
    /// Immutable after creation.
    /// </summary>
    public List<ToolCall> ToolCalls { get; init; } = new List<ToolCall>();

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private LlmToolCallMessage()
    {
        // For deserialization only
    }

    /// <summary>
    /// Creates a new LLM tool call message
    /// </summary>
    /// <param name="content">Message content (can be empty when LLM only requests tools)</param>
    /// <param name="toolCalls">List of tool calls (must contain at least one)</param>
    /// <exception cref="ArgumentException">Thrown when toolCalls is null or empty</exception>
    public LlmToolCallMessage(string content, List<ToolCall> toolCalls)
    {
        if (toolCalls == null || toolCalls.Count == 0)
            throw new ArgumentException("Must have at least one tool call", nameof(toolCalls));

        // Create defensive copy
        ToolCalls = toolCalls.ToList();

        Id = Guid.NewGuid();
        Content = content ?? string.Empty;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }
}
