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
    Error,

    // Tool discovery events (Phase 5)
    ToolDiscoveryStarted,
    ToolDiscoveryCompleted,
    ToolDiscoveryFailed,
    ToolRegistered,

    // Tool execution events (Phase 5)
    ToolCallStarted,
    ToolCallCompleted,
    ToolCallFailed,
    ToolCallTimeout,

    // MCP server lifecycle events (Phase 5)
    MCPServerConnecting,
    MCPServerConnected,
    MCPServerDisconnected,
    MCPServerConnectionFailed,

    // UI Control events (Phase 9)
    UIControlAction
}
