namespace TransparentAiAgentCore.Domain.Transparency;

public enum TransparencyEventType
{
    // Core events - actively used
    UserInput,              // Used in tests (future feature)
    AssistantResponse,
    ToolCall,
    ToolResult,
    ContextChange,
    SystemState,
    Error,
    Info,

    // Tool execution events
    ToolCallCompleted,      // Used in tests (future feature)

    // UI Control events
    UIControlAction,        // Used in tests (future feature)

    // Raw LLM request/response events (Phase 9a - Transparency Enhancement)
    RawLLMRequest,          // Complete request JSON before sending to LLM
    RawLLMResponse,         // Complete response JSON as received from LLM
    MessageParsingError,    // Error during message/response parsing

    // Tool execution safety events (Phase 9b - Tool Execution Safety)
    ToolArgumentValidationFailed,   // Tool arguments failed schema validation
    ToolStreamingDataCorrupted      // JSON accumulation failed during streaming
}
