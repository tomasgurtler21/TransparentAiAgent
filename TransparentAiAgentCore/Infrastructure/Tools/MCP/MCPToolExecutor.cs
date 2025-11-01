using System.Diagnostics;
using System.Text.Json;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.MCP;

/// <summary>
/// Executes tool calls for MCP tools.
/// Implements IToolExecutor for ToolSourceType.MCP.
/// </summary>
public class MCPToolExecutor : IToolExecutor
{
    private readonly MCPToolDiscovery _toolDiscovery;
    private readonly Dictionary<string, IMCPClientWrapper> _activeClients;
    private readonly SemaphoreSlim _clientLock;

    public MCPToolExecutor(MCPToolDiscovery toolDiscovery)
    {
        _toolDiscovery = toolDiscovery ?? throw new ArgumentNullException(nameof(toolDiscovery));
        _activeClients = new Dictionary<string, IMCPClientWrapper>();
        _clientLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Gets the source type this executor handles (MCP).
    /// </summary>
    public ToolSourceType SourceType => ToolSourceType.MCP;

    /// <summary>
    /// Executes an MCP tool call.
    /// </summary>
    public async Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (tool.SourceType != ToolSourceType.MCP)
        {
            throw new ArgumentException(
                $"Tool '{tool.Name}' is not an MCP tool (SourceType: {tool.SourceType})",
                nameof(tool));
        }

        if (tool is not MCPTool mcpTool)
        {
            throw new ArgumentException(
                $"Tool '{tool.Name}' is not an instance of MCPTool",
                nameof(tool));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get server name from metadata
            if (!tool.Metadata.TryGetValue("ServerName", out var serverName) ||
                string.IsNullOrWhiteSpace(serverName))
            {
                throw new InvalidOperationException(
                    $"Tool '{tool.Name}' is missing ServerName in metadata");
            }

            // Get or create client connection
            var client = await GetOrCreateClientAsync(serverName, cancellationToken);

            // Parse arguments from JSON string to dictionary
            Dictionary<string, object?> argumentsDict;
            try
            {
                argumentsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(arguments)
                    ?? new Dictionary<string, object?>();
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                return ToolExecutionResult.Failure(
                    $"Invalid JSON arguments: {ex.Message}",
                    stopwatch.Elapsed);
            }

            // Execute tool via MCP client
            var result = await client.CallToolAsync(tool.Name, argumentsDict, cancellationToken);

            stopwatch.Stop();

            // Convert result to string
            // CallToolResult contains Content array with text/image/resource items
            var resultContent = ConvertCallToolResultToString(result);

            return ToolExecutionResult.Success(resultContent, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolExecutionResult.Failure(
                $"Tool execution failed: {ex.Message}",
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Gets or creates an MCP client connection for a server.
    /// </summary>
    private async Task<IMCPClientWrapper> GetOrCreateClientAsync(
        string serverName,
        CancellationToken cancellationToken)
    {
        await _clientLock.WaitAsync(cancellationToken);
        try
        {
            // Check if client already exists and is connected
            if (_activeClients.TryGetValue(serverName, out var existingClient) &&
                existingClient.IsConnected)
            {
                return existingClient;
            }

            // Create new client
            var newClient = _toolDiscovery.GetClientWrapper(serverName);
            await newClient.ConnectAsync(cancellationToken);

            // Store in active clients
            _activeClients[serverName] = newClient;

            return newClient;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    /// <summary>
    /// Converts CallToolResult to a string representation.
    /// </summary>
    private string ConvertCallToolResultToString(ModelContextProtocol.Protocol.CallToolResult result)
    {
        try
        {
            // Serialize the entire result to JSON
            // TODO: Extract text content specifically once ContentBlock API is clarified
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return $"{{\"error\": \"Failed to serialize result: {ex.Message}\"}}";
        }
    }

    /// <summary>
    /// Disposes all active MCP client connections.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _clientLock.WaitAsync();
        try
        {
            foreach (var client in _activeClients.Values)
            {
                try
                {
                    await client.DisposeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Error disposing MCP client: {ex.Message}");
                }
            }

            _activeClients.Clear();
        }
        finally
        {
            _clientLock.Release();
        }

        _clientLock.Dispose();
    }
}
