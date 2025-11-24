using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.MCP;

/// <summary>
/// Registry for MCP tools.
/// Caches discovered tools and provides access to them.
/// </summary>
public class MCPToolRegistry : IToolRegistry
{
    private readonly ToolsConfiguration _toolsConfiguration;
    private readonly MCPToolDiscovery _toolDiscovery;
    private readonly SemaphoreSlim _refreshLock;
    private IReadOnlyList<ITool> _cachedTools;
    private DateTime _lastRefresh;

    public MCPToolRegistry(ToolsConfiguration toolsConfiguration)
    {
        _toolsConfiguration = toolsConfiguration ?? throw new ArgumentNullException(nameof(toolsConfiguration));
        _toolDiscovery = new MCPToolDiscovery(toolsConfiguration);
        _refreshLock = new SemaphoreSlim(1, 1);
        _cachedTools = Array.Empty<ITool>();
        _lastRefresh = DateTime.MinValue;
    }

    /// <summary>
    /// Gets all registered MCP tools.
    /// </summary>
    public IReadOnlyList<ITool> GetAllTools()
    {
        return _cachedTools;
    }

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    public ITool? GetTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        return _cachedTools.FirstOrDefault(t =>
            t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    public bool HasTool(string toolName)
    {
        return GetTool(toolName) != null;
    }

    /// <summary>
    /// Refreshes the tool registry by re-discovering from MCP servers.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Discover tools from all configured MCP servers
            var discoveredTools = await _toolDiscovery.DiscoverAllToolsAsync(cancellationToken);

            // Update cache
            _cachedTools = discoveredTools;
            _lastRefresh = DateTime.UtcNow;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Gets the timestamp of the last refresh.
    /// </summary>
    public DateTime LastRefresh => _lastRefresh;

    /// <summary>
    /// Gets the number of cached tools.
    /// </summary>
    public int ToolCount => _cachedTools.Count;
}
