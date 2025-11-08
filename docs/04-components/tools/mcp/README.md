# MCP Tools

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 5
**Layer**: Infrastructure

---

## Overview

The MCP Tools subsystem integrates Model Context Protocol (MCP) servers with the agent's tool system. It discovers, registers, and executes tools from external MCP servers.

## Components

### [MCP Client Wrapper](mcp-client-wrapper.md)
Wraps MCP SDK client for lifecycle management and clean API.

**Responsibilities**: Server connection via stdio transport, tool/resource listing.

---

### [MCP Tool Registry](mcp-tool-registry.md)
IToolRegistry implementation for MCP tools with caching.

**Responsibilities**: Tool caching, lookup, refresh.

---

### [MCP Tool Executor](mcp-tool-executor.md)
IToolExecutor implementation for executing MCP tools.

**Responsibilities**: Route tool calls to servers, execute, return results.

---

### [MCP Tool Discovery](mcp-tool-discovery.md)
Discovers tools from configured MCP servers.

**Responsibilities**: Connect to servers, retrieve tool definitions, convert to ITool.

---

## Architecture

```
MCPConfiguration (from config)
    ↓
MCPToolDiscovery
    ├─ Creates MCPClientWrapper per server
    ├─ Connects to each server
    ├─ Lists tools
    └─ Converts to ITool instances
    ↓
MCPToolRegistry (caches discovered tools)
    ├─ GetAllTools()
    ├─ GetTool(name)
    └─ RefreshAsync()
    ↓
ToolRegistryComposite (aggregates with built-in)
    ↓
ToolManager (routes execution)
    ↓
MCPToolExecutor (executes MCP tools)
    ├─ Creates MCPClientWrapper
    ├─ Connects to server
    ├─ Calls tool
    └─ Returns result
```

---

## Configuration

### appsettings.json

```json
{
  "MCP": {
    "Servers": [
      {
        "Name": "filesystem",
        "Command": "node",
        "Args": ["path/to/mcp-server.js"],
        "Env": {}
      }
    ]
  }
}
```

**See**: `docs/05-guides/deployment/configuration-guide.md`

---

## Tool Flow Example

### Discovery (Startup)

```
1. MCPToolRegistry.RefreshAsync()
2. MCPToolDiscovery.DiscoverToolsAsync()
3. For each server:
   - MCPClientWrapper.ConnectAsync()
   - MCPClientWrapper.ListToolsAsync()
   - Convert to ITool
   - MCPClientWrapper.DisconnectAsync()
4. Cache tools in registry
```

### Execution (Runtime)

```
1. LLM requests tool call
2. ToolManager receives request
3. Get tool from composite registry
4. Check tool.SourceType == MCP
5. MCPToolExecutor.ExecuteAsync()
   - Get server from metadata
   - Connect to server
   - Call tool
   - Return result
6. Result added to conversation
```

---

## Testing

### Unit Tests

**Files**:
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/MCP/MCPToolDiscoveryIntegrationTests.cs`

### Integration Tests

Require running MCP servers for real testing.

---

## Related Documentation

- [Tool Manager](../tool-manager.md) - Orchestrates execution
- [Built-in Tools](../builtin/README.md) - Alternative tool source
- [Tools Overview](../README.md) - Complete tool system

---

**See Also**: [Component Overview](../../README.md)
