using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

public class ToolCallMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.Tool;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    /// <summary>
    /// Name of the tool being called
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// JSON string of tool parameters
    /// </summary>
    public string ToolParameters { get; }

    /// <summary>
    /// Unique ID for this tool call (from LLM)
    /// </summary>
    public string ToolCallId { get; }

    public ToolCallMessage(string toolName, string toolParameters, string toolCallId)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));

        Id = Guid.NewGuid();
        ToolName = toolName;
        ToolParameters = toolParameters ?? "{}";
        ToolCallId = toolCallId;
        Content = $"Tool Call: {toolName}({toolParameters ?? "{}"})";
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }
}
