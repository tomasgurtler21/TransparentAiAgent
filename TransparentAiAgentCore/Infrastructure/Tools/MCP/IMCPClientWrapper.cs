using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TransparentAiAgentCore.Infrastructure.Tools.MCP;

/// <summary>
/// Wraps the MCP SDK client to provide a clean interface for MCP server interactions.
/// </summary>
public interface IMCPClientWrapper : IAsyncDisposable
{
    /// <summary>
    /// Gets the server name this client is connected to.
    /// </summary>
    string ServerName { get; }

    /// <summary>
    /// Gets whether the client is connected to the server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the MCP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available tools from the MCP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of tool definitions.</returns>
    Task<IReadOnlyList<McpClientTool>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a tool on the MCP server.
    /// </summary>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="arguments">The tool arguments as a dictionary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tool call result.</returns>
    Task<CallToolResult> CallToolAsync(
        string toolName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the MCP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
