namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Represents a tool that can be called by the LLM.
/// Agnostic to the underlying implementation (MCP, built-in, etc.).
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the unique tool name (used for routing).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the human-readable description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the JSON Schema for the tool's parameters.
    /// </summary>
    string ParametersSchema { get; }

    /// <summary>
    /// Gets the tool source type (for routing and transparency).
    /// </summary>
    ToolSourceType SourceType { get; }

    /// <summary>
    /// Gets the source-specific metadata (e.g., MCP server name, built-in assembly).
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
