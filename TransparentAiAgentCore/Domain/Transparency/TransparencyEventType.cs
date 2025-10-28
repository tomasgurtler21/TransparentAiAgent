namespace TransparentAiAgentCore.Domain.Transparency;

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
