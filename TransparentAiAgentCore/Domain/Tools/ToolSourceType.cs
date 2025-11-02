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

    // Future: ExternalAPI, CustomProtocol, etc.
}
