namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Registry for discovering and managing tools.
/// Provides access to all available tools from various sources.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    /// <returns>A read-only list of all tools.</returns>
    IReadOnlyList<ITool> GetAllTools();

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to retrieve.</param>
    /// <returns>The tool if found, otherwise null.</returns>
    ITool? GetTool(string toolName);

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    /// <param name="toolName">The name of the tool to check.</param>
    /// <returns>True if the tool is registered, otherwise false.</returns>
    bool HasTool(string toolName);

    /// <summary>
    /// Refreshes the tool registry by re-discovering from sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
