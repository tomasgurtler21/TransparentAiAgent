using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore.Infrastructure.Tools.MCP;

/// <summary>
/// Wraps the MCP SDK client to provide a clean interface for MCP server interactions.
/// Manages the lifecycle of the MCP client connection.
/// </summary>
public class MCPClientWrapper : IMCPClientWrapper
{
    private readonly MCPServerConfiguration _serverConfig;
    private StdioClientTransport? _transport;
    private McpClient? _client;
    private bool _isConnected;
    private bool _disposed;

    public MCPClientWrapper(MCPServerConfiguration serverConfig)
    {
        _serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
        _serverConfig.Validate();
    }

    /// <summary>
    /// Gets the server name this client is connected to.
    /// </summary>
    public string ServerName => _serverConfig.Name;

    /// <summary>
    /// Gets whether the client is connected to the server.
    /// </summary>
    public bool IsConnected => _isConnected && _client != null;

    /// <summary>
    /// Connects to the MCP server.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            return; // Already connected
        }

        try
        {
            // Create transport options
            var transportOptions = new StdioClientTransportOptions
            {
                Name = _serverConfig.Name,
                Command = _serverConfig.Command,
                Arguments = _serverConfig.Args.ToArray()
            };

            // Note: Environment variables would be set here if API supports it
            // For now, env vars in server config may not be used

            // Create transport and client
            _transport = new StdioClientTransport(transportOptions);
            _client = await McpClient.CreateAsync(_transport);

            _isConnected = true;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _client = null;
            _transport = null;
            throw new InvalidOperationException(
                $"Failed to connect to MCP server '{_serverConfig.Name}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Lists all available tools from the MCP server.
    /// </summary>
    public async Task<IReadOnlyList<McpClientTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var tools = await _client!.ListToolsAsync();
            return tools.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to list tools from MCP server '{_serverConfig.Name}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calls a tool on the MCP server.
    /// </summary>
    public async Task<CallToolResult> CallToolAsync(
        string toolName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));

        EnsureConnected();

        try
        {
            var result = await _client!.CallToolAsync(toolName, arguments);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to call tool '{toolName}' on MCP server '{_serverConfig.Name}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Disconnects from the MCP server.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            return; // Already disconnected
        }

        try
        {
            _isConnected = false;

            if (_client != null)
            {
                await _client.DisposeAsync();
                _client = null;
            }

            // Transport cleanup
            _transport = null;
        }
        catch (Exception ex)
        {
            // Log but don't throw during cleanup
            System.Diagnostics.Debug.WriteLine(
                $"Error disconnecting from MCP server '{_serverConfig.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes the client wrapper and disconnects from the server.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisconnectAsync();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    private void EnsureConnected()
    {
        if (!_isConnected || _client == null)
        {
            throw new InvalidOperationException(
                $"Not connected to MCP server '{_serverConfig.Name}'. Call ConnectAsync first.");
        }
    }
}
