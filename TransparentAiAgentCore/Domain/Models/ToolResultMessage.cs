using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

public class ToolResultMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.Tool;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    /// <summary>
    /// ID of the tool call this is responding to
    /// </summary>
    public string ToolCallId { get; }

    /// <summary>
    /// Name of the tool that was called
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Result data from the tool (JSON string)
    /// </summary>
    public string Result { get; }

    /// <summary>
    /// Whether the tool execution was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Error message if tool execution failed
    /// </summary>
    public string? ErrorMessage { get; }

    public ToolResultMessage(string toolCallId, string toolName, string result, bool isSuccess, string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));

        Id = Guid.NewGuid();
        ToolCallId = toolCallId;
        ToolName = toolName;
        Result = result ?? "{}";
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Content = isSuccess ? $"Tool Result: {result ?? "{}"}" : $"Tool Error: {errorMessage}";
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }
}
