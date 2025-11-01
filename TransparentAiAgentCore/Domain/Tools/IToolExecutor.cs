namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Executes tool calls for a specific source type.
/// Each tool source (MCP, built-in, etc.) provides its own implementation.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Gets the source type this executor handles.
    /// </summary>
    ToolSourceType SourceType { get; }

    /// <summary>
    /// Executes a tool call and returns the result.
    /// </summary>
    /// <param name="tool">The tool definition to execute.</param>
    /// <param name="arguments">The JSON-formatted arguments for the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tool execution result.</returns>
    Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default);
}
