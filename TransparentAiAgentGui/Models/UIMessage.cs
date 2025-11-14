using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentGui.Models;

/// <summary>
/// View model for displaying messages in UI
/// </summary>
public class UIMessage
{
    public Guid Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MessageContextStatus ContextStatus { get; set; }

    // Tool-specific properties
    public bool IsToolCall { get; set; }
    public bool IsToolResult { get; set; }
    public string? ToolName { get; set; } // Used for tool results
    public List<UIToolCall> ToolCalls { get; set; } = new(); // Used for tool calls
    public bool ToolResultSuccess { get; set; }
    public string? ToolErrorMessage { get; set; }

    // Scenario-specific properties
    public bool IsAutoMessage { get; set; }

    /// <summary>
    /// Annotation text for scenario messages (visible to user, explains what's happening).
    /// This is UI-only metadata, never sent to the LLM.
    /// </summary>
    public string? Annotation { get; set; }

    /// <summary>
    /// Represents a single tool call in the UI
    /// </summary>
    public class UIToolCall
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
    }

    /// <summary>
    /// CSS class for styling based on role
    /// </summary>
    public string CssClass => Role switch
    {
        MessageRole.User => "message-user",
        MessageRole.Assistant => "message-assistant",
        MessageRole.System => "message-system",
        MessageRole.Tool => "message-tool",
        _ => "message-default"
    };

    /// <summary>
    /// Icon to display for context status
    /// </summary>
    public string ContextStatusIcon => ContextStatus switch
    {
        MessageContextStatus.InContext => "✅",
        MessageContextStatus.TruncatedFromContext => "⚠️",
        _ => ""
    };

    /// <summary>
    /// Tooltip text for context status
    /// </summary>
    public string ContextStatusTooltip => ContextStatus switch
    {
        MessageContextStatus.InContext => "In LLM Context",
        MessageContextStatus.TruncatedFromContext => "Not in LLM Context (truncated)",
        _ => ""
    };

    /// <summary>
    /// Create UIMessage from domain message.
    /// Supports all message types in the 4-tier hierarchy:
    /// - User: DirectUserMessage
    /// - Application: ScenarioUserMessage, ScenarioAssistantMessage
    /// - LLM: LlmTextMessage, LlmToolCallMessage
    /// - Tool: ToolResultMessage, ToolErrorMessage
    /// </summary>
    public static UIMessage FromDomainMessage(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var uiMessage = new UIMessage
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            ContextStatus = message.ContextStatus
        };

        // === Application-originated scenario messages ===
        // These are auto-generated messages from teaching scenarios
        if (message is ScenarioUserMessage scenarioUserMsg)
        {
            uiMessage.IsAutoMessage = true;
            uiMessage.Annotation = scenarioUserMsg.Annotation;
        }
        else if (message is ScenarioAssistantMessage scenarioAssistantMsg)
        {
            uiMessage.IsAutoMessage = true;
            uiMessage.Annotation = scenarioAssistantMsg.Annotation;
        }

        // === LLM-originated tool call messages ===
        // LlmToolCallMessage (new hierarchy)
        else if (message is LlmToolCallMessage llmToolCallMsg)
        {
            uiMessage.IsToolCall = true;
            uiMessage.ToolCalls = llmToolCallMsg.ToolCalls
                .Select(tc => new UIToolCall
                {
                    Id = tc.Id,
                    Name = tc.Name,
                    Arguments = tc.Arguments
                })
                .ToList();
        }

        // === Tool-originated result messages ===
        // ToolResultMessage (new hierarchy - successful result, may have IsError flag)
        else if (message is ToolResultMessage toolResult)
        {
            uiMessage.IsToolResult = true;
            uiMessage.ToolName = toolResult.ToolName;
            uiMessage.ToolResultSuccess = !toolResult.IsError; // Invert: IsError=false means success=true
            uiMessage.ToolErrorMessage = toolResult.IsError ? toolResult.Result : null;
        }
        // ToolErrorMessage (new hierarchy - explicit error message)
        else if (message is ToolErrorMessage toolError)
        {
            uiMessage.IsToolResult = true;
            uiMessage.ToolName = toolError.ToolName;
            uiMessage.ToolResultSuccess = false;
            uiMessage.ToolErrorMessage = toolError.ErrorMessage;
        }

        // === Basic message types ===
        // DirectUserMessage, LlmTextMessage, SystemMessage - no special handling needed
        // They use the basic properties already set above

        return uiMessage;
    }
}
