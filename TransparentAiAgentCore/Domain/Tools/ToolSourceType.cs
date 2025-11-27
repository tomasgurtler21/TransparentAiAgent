namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Represents the source type of a tool.
/// Used for routing tool execution to the appropriate executor.
/// </summary>
public enum ToolSourceType
{
    /// <summary>
    /// Tool provided by an MCP (Model Context Protocol) server
    /// </summary>
    MCP,

    /// <summary>
    /// Tool built into the application (native .NET implementation)
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Built-in UI control tool (Phase 9 - Teaching Mode)
    /// Used by agents to control UI components dynamically
    /// </summary>
    BuiltInUIControl,

    /// <summary>
    /// Built-in knowledge library tool
    /// Used by agents to query curated knowledge entries with guardrails
    /// </summary>
    BuiltInKnowledge,

    /// <summary>
    /// Built-in long-term memory tool
    /// Used by agents to read and update persistent user memory
    /// </summary>
    BuiltInLongTermMemory,

    /// <summary>
    /// Scenario-specific mock tool (temporary, scenario-scoped)
    /// Used in teaching scenarios to demonstrate tool behavior with controlled responses
    /// </summary>
    ScenarioMock,

    // Future: ExternalAPI, CustomProtocol, etc.
}
