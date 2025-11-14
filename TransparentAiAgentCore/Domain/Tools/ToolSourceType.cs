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

    // Future: ExternalAPI, CustomProtocol, etc.
}
