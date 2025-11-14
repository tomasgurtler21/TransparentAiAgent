using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Messages originating from tool execution infrastructure.
/// Represents results from MCP servers and built-in tools.
/// Tools are external operations (file system, git, network, etc.) - distinct from app-internal logic.
/// </summary>
public abstract class ToolMessage : IMessage
{
    public Guid Id { get; protected set; }
    public MessageRole Role => MessageRole.Tool;
    public string Content { get; protected set; } = string.Empty;
    public DateTime Timestamp { get; protected set; }
    public MessageContextStatus ContextStatus { get; set; }
    public abstract string MessageTypeDiscriminator { get; }

    /// <summary>
    /// ID of the tool call this message is responding to.
    /// Links tool result back to the LLM's tool call request.
    /// String format matches LLM provider's tool call ID format.
    /// </summary>
    public string ToolCallId { get; protected set; } = string.Empty;

    /// <summary>
    /// Name of the tool that was executed.
    /// </summary>
    public string ToolName { get; protected set; } = string.Empty;
}
