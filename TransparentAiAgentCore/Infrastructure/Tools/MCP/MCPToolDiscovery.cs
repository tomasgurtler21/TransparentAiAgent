using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.MCP;

/// <summary>
/// Discovers tools from MCP servers and converts them to ITool instances.
/// </summary>
public class MCPToolDiscovery
{
    private readonly ToolsConfiguration _toolsConfiguration;

    public MCPToolDiscovery(ToolsConfiguration toolsConfiguration)
    {
        _toolsConfiguration = toolsConfiguration ?? throw new ArgumentNullException(nameof(toolsConfiguration));
    }

    /// <summary>
    /// Discovers all tools from all configured MCP servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of discovered tools.</returns>
    public async Task<IReadOnlyList<ITool>> DiscoverAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var allTools = new List<ITool>();

        foreach (var serverConfig in _toolsConfiguration.Servers)
        {
            try
            {
                var serverTools = await DiscoverToolsFromServerAsync(serverConfig, cancellationToken);
                allTools.AddRange(serverTools);
            }
            catch (Exception ex)
            {
                // Log error but continue with other servers
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to discover tools from MCP server '{serverConfig.Name}': {ex.Message}");
            }
        }

        return allTools.AsReadOnly();
    }

    /// <summary>
    /// Discovers tools from a specific MCP server.
    /// </summary>
    /// <param name="serverConfig">The server configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of tools from the server.</returns>
    public async Task<IReadOnlyList<ITool>> DiscoverToolsFromServerAsync(
        MCPServerConfiguration serverConfig,
        CancellationToken cancellationToken = default)
    {
        if (serverConfig == null)
            throw new ArgumentNullException(nameof(serverConfig));

        serverConfig.Validate();

        await using var clientWrapper = new MCPClientWrapper(serverConfig);

        try
        {
            // Connect to the MCP server
            await clientWrapper.ConnectAsync(cancellationToken);

            // List all tools
            var mcpTools = await clientWrapper.ListToolsAsync(cancellationToken);

            // Convert to ITool instances
            var tools = mcpTools
                .Select(mcpTool => (ITool)new MCPTool(mcpTool, serverConfig.Name))
                .ToList();

            return tools.AsReadOnly();
        }
        finally
        {
            // Ensure disconnection
            await clientWrapper.DisconnectAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets a client wrapper for a specific server (for external use).
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <returns>A new MCP client wrapper.</returns>
    /// <exception cref="InvalidOperationException">If server is not found in configuration.</exception>
    public IMCPClientWrapper GetClientWrapper(string serverName)
    {
        var serverConfig = _toolsConfiguration.Servers
            .FirstOrDefault(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));

        if (serverConfig == null)
        {
            throw new InvalidOperationException(
                $"MCP server '{serverName}' not found in configuration");
        }

        return new MCPClientWrapper(serverConfig);
    }
}
