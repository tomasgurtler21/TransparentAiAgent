using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools;

/// <summary>
/// Composite registry that aggregates tools from multiple sources.
/// Implements the Composite pattern to combine MCP, built-in, and future tool sources.
/// </summary>
public class ToolRegistryComposite : IToolRegistry
{
    private readonly IReadOnlyList<IToolRegistry> _registries;

    public ToolRegistryComposite(IEnumerable<IToolRegistry> registries)
    {
        if (registries == null)
            throw new ArgumentNullException(nameof(registries));

        _registries = registries.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets all tools from all registries.
    /// </summary>
    public IReadOnlyList<ITool> GetAllTools()
    {
        // Aggregate from all registries
        return _registries
            .SelectMany(r => r.GetAllTools())
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets a tool by name (first match wins).
    /// Searches all registries in order.
    /// </summary>
    public ITool? GetTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        // Search all registries (first match wins)
        foreach (var registry in _registries)
        {
            var tool = registry.GetTool(toolName);
            if (tool != null)
                return tool;
        }

        return null;
    }

    /// <summary>
    /// Checks if a tool is registered in any registry.
    /// </summary>
    public bool HasTool(string toolName)
    {
        return GetTool(toolName) != null;
    }

    /// <summary>
    /// Refreshes all registries in parallel.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Refresh all registries in parallel for efficiency
        var refreshTasks = _registries
            .Select(r => r.RefreshAsync(cancellationToken))
            .ToArray();

        await Task.WhenAll(refreshTasks);
    }

    /// <summary>
    /// Gets the number of registries in this composite.
    /// </summary>
    public int RegistryCount => _registries.Count;
}
