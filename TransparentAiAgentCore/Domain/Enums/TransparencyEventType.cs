namespace TransparentAiAgentCore.Domain.Enums;

/// <summary>
/// Types of transparency events that can be logged
/// </summary>
public enum TransparencyEventType
{
    UserInput,
    AssistantResponse,
    ToolCall,
    ToolResult,
    ContextChange,
    ConfigurationChange,
    SystemState,
    Error
}
