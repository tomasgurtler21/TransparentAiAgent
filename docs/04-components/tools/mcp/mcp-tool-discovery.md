# MCP Tool Discovery

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 5
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- MCPToolDiscovery implementation
- Tool discovery from MCP servers
- Conversion from MCP SDK format to ITool

**What does NOT belong here**:
- ❌ Tool caching → See [mcp-tool-registry.md](mcp-tool-registry.md)
- ❌ Tool execution → See [mcp-tool-executor.md](mcp-tool-executor.md)
- ❌ MCP client lifecycle → See [mcp-client-wrapper.md](mcp-client-wrapper.md)

---

## Overview

MCPToolDiscovery connects to configured MCP servers, retrieves tool definitions, and converts them to ITool instances for use in the tool system.

## Purpose

- **Connect** to all configured MCP servers
- **Retrieve** tool definitions via tools/list
- **Convert** MCP tool format to ITool domain model
- **Aggregate** tools from multiple servers
- **Include** server metadata for routing

## Responsibilities

- Iterate through MCPServerConfiguration list
- Create MCPClientWrapper for each server
- Connect and list tools
- Convert McpClientTool to MCPTool (ITool implementation)
- Add server name to tool metadata
- Handle connection failures gracefully

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPToolDiscovery.cs`

```csharp
public class MCPToolDiscovery
{
    private readonly MCPConfiguration _mcpConfiguration;

    public async Task<IReadOnlyList<ITool>> DiscoverToolsAsync(
        CancellationToken cancellationToken = default)
    {
        // Discovery logic
    }
}
```

### Dependencies

**Depends on**:
- `Domain/Configuration/MCPConfiguration` - Server list
- `Infrastructure/Tools/MCP/MCPClientWrapper` - Server communication
- `Infrastructure/Tools/MCP/MCPTool` - ITool implementation

**Used by**:
- `Infrastructure/Tools/MCP/MCPToolRegistry` - Calls DiscoverToolsAsync()

## Discovery Flow

1. **Initialize** empty tool list
2. **For each** server in MCPConfiguration.Servers:
   - Create MCPClientWrapper(serverConfig)
   - Connect to server
   - List tools via ListToolsAsync()
   - Convert each McpClientTool to MCPTool
   - Add server name to metadata
   - Disconnect from server
3. **Handle** connection errors (log, continue to next server)
4. **Return** aggregated tool list

**Error Handling**: Continues discovery even if individual servers fail.

## Tool Conversion

**From**: `McpClientTool` (MCP SDK)
**To**: `MCPTool` (ITool implementation)

**Mapping**:
- Name → ITool.Name
- Description → ITool.Description
- InputSchema (JSON) → ITool.ParametersSchema
- SourceType → ToolSourceType.MCP
- Metadata["ServerName"] → Server name for routing

## Usage

### Integration Point (Registry Refresh)

```csharp
var discovery = new MCPToolDiscovery(mcpConfiguration);
var tools = await discovery.DiscoverToolsAsync();

// tools is IReadOnlyList<ITool> with all MCP tools from all servers
```

## Related Documentation

- [MCP Tool Registry](mcp-tool-registry.md) - Uses discovery
- [MCP Client Wrapper](mcp-client-wrapper.md) - Server communication
- [MCP Tools Overview](README.md) - MCP subsystem

---

**See Also**: [Tools Overview](../README.md)
