using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Tool execution error.
/// Part of the 4-tier message hierarchy - inherits from ToolMessage.
/// </summary>
public class ToolErrorMessage : ToolMessage
{
    public override string MessageTypeDiscriminator => "Tool.Error";

    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Optional exception that caused the error.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Creates a new tool error message.
    /// </summary>
    /// <param name="toolCallId">ID of the tool call this message is responding to</param>
    /// <param name="toolName">Name of the tool that was executed</param>
    /// <param name="errorMessage">Error message describing what went wrong</param>
    /// <param name="exception">Optional exception that caused the error</param>
    public ToolErrorMessage(string toolCallId, string toolName, string errorMessage, Exception? exception = null)
    {
        // Validate parameters
        if (toolCallId == null)
            throw new ArgumentNullException(nameof(toolCallId));
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be empty or whitespace", nameof(toolCallId));
        if (toolName == null)
            throw new ArgumentNullException(nameof(toolName));
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be empty or whitespace", nameof(toolName));
        if (errorMessage == null)
            throw new ArgumentNullException(nameof(errorMessage));
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty or whitespace", nameof(errorMessage));

        // Set properties from base class
        Id = Guid.NewGuid();
        ToolCallId = toolCallId;
        ToolName = toolName;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;

        // Set properties specific to this class
        ErrorMessage = errorMessage;
        Exception = exception;
        Content = $"Error executing {toolName}: {errorMessage}";
    }
}
