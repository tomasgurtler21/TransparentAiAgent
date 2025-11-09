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
    Warning,
    Debug,
    Info,

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
    UIControlAction,

    // Raw LLM request/response events (Phase 9a - Transparency Enhancement)
    RawLLMRequest,          // Complete request JSON before sending to LLM
    RawLLMResponse,         // Complete response JSON as received from LLM
    MessageParsingError,    // Error during message/response parsing

    // Tool execution safety events (Phase 9b - Tool Execution Safety)
    ToolArgumentValidationFailed,   // Tool arguments failed schema validation
    ToolStreamingDataCorrupted      // JSON accumulation failed during streaming
}
