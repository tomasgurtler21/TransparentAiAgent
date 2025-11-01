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

    // Future: ExternalAPI, CustomProtocol, etc.
}
