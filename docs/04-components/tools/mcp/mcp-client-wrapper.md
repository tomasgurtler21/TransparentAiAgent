# MCP Client Wrapper

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 5
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- MCPClientWrapper implementation
- MCP SDK lifecycle management
- Server connection handling
- Tool and resource listing

**What does NOT belong here**:
- ❌ MCP protocol specification → See MCP SDK documentation
- ❌ Tool discovery logic → See [mcp-tool-discovery.md](mcp-tool-discovery.md)
- ❌ Tool execution → See [mcp-tool-executor.md](mcp-tool-executor.md)

---

## Overview

MCPClientWrapper provides a clean interface to the MCP SDK client, managing server connections via stdio transport and exposing tool/resource listing capabilities.

## Purpose

- **Wrap** MCP SDK client for cleaner API
- **Manage** connection lifecycle (connect, disconnect, dispose)
- **Provide** tool and resource listing from MCP servers
- **Handle** connection errors and retries

## Responsibilities

- Initialize stdio transport for MCP server processes
- Connect to MCP servers using command + arguments
- List tools and resources from connected servers
- Manage connection state
- Dispose of resources properly

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPClientWrapper.cs:12`

```csharp
public class MCPClientWrapper : IMCPClientWrapper
{
    private readonly MCPServerConfiguration _serverConfig;
    private StdioClientTransport? _transport;
    private McpClient? _client;
    private bool _isConnected;
}
```

### Dependencies

**Depends on**:
- `Domain/Configuration/MCPServerConfiguration` - Server config
- `ModelContextProtocol` SDK - MCP client library

**Used by**:
- `Infrastructure/Tools/MCP/MCPToolDiscovery.cs` - Tool discovery
- `Infrastructure/Tools/MCP/MCPToolExecutor.cs` - Tool execution

## Key Methods

### ConnectAsync

Establishes connection to MCP server using stdio transport.

**Flow**:
1. Check if already connected (idempotent)
2. Create StdioClientTransportOptions from config
3. Create StdioClientTransport
4. Create McpClient via `McpClient.CreateAsync()`
5. Mark as connected

**Error Handling**: Wraps exceptions in `InvalidOperationException` with server name context.

### ListToolsAsync

Retrieves all available tools from the connected server.

**Returns**: `IReadOnlyList<McpClientTool>` from MCP SDK.

### DisconnectAsync

Closes connection to server and disposes resources.

## Usage

### Integration Point (Discovery)

```csharp
var wrapper = new MCPClientWrapper(serverConfig);
await wrapper.ConnectAsync();
var tools = await wrapper.ListToolsAsync();
await wrapper.DisconnectAsync();
```

## Related Documentation

- [MCP Tool Discovery](mcp-tool-discovery.md) - Uses wrapper for discovery
- [MCP Tool Executor](mcp-tool-executor.md) - Uses wrapper for execution
- [MCP Tools Overview](README.md) - MCP subsystem

---

**See Also**: [Tools Overview](../README.md)
