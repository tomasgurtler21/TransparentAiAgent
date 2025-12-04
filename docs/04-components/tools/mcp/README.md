# MCP Tools

**Last Updated**: 2025-12-04
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

### tools.json

MCP server configuration is stored in a separate `tools.json` file (not in `appsettings.json`).

```json
{
  "EnableTools": true,
  "ToolExecutionMode": "Sequential",
  "Servers": [
    {
      "Name": "todo-list",
      "Command": "npx",
      "Args": ["-y", "@anthropic/mcp-server-todo-list"],
      "Env": {}
    },
    {
      "Name": "filesystem",
      "Command": "node",
      "Args": ["path/to/mcp-server.js"],
      "Env": {
        "API_KEY": "your-api-key-here"
      }
    }
  ],
  "AutoDiscoverTools": true,
  "ToolExecutionTimeoutSeconds": 180,
  "MaxToolCallDepth": 10
}
```

**Configuration Options**:
- `EnableTools`: Enable/disable tool calling (default: true)
- `ToolExecutionMode`: `Sequential` or `Parallel` execution
- `Servers`: Array of MCP server configurations
  - `Name`: Unique server identifier
  - `Command`: Executable to start the server (e.g., `npx`, `node`, `python`, or **full path to .exe**)
  - `Args`: Command-line arguments
  - `Env`: Environment variables (augments inherited environment, set to `null` to remove)
- `AutoDiscoverTools`: Auto-discover tools on startup (default: true)
- `ToolExecutionTimeoutSeconds`: Maximum execution time (default: 180)
- `MaxToolCallDepth`: Max nested tool calls (default: 10, range: 1-50)

---

### Common Server Examples

**Using npx (Node package)**:
```json
{
  "Name": "todo-list",
  "Command": "npx",
  "Args": ["-y", "@anthropic/mcp-server-todo-list"],
  "Env": {}
}
```

**Using local binary/executable** (⭐ Many users don't know this is possible!):
```json
{
  "Name": "my-custom-tool",
  "Command": "C:\\Users\\YourName\\MCPServers\\my-tool.exe",
  "Args": ["--port", "8080"],
  "Env": {
    "LOG_LEVEL": "info"
  }
}
```

**Important for local binaries**:
- ✅ Use **full absolute path** in `Command` field
- ✅ On Windows, use double backslashes (`\\`) or forward slashes (`/`) in paths
- ✅ Works with `.exe`, Linux binaries, or any executable

**Using Python with full path** (useful for virtual environments):
```json
{
  "Name": "python-server",
  "Command": "C:\\Python311\\python.exe",
  "Args": ["C:\\MCPServers\\my_server.py"],
  "Env": {}
}
```

**See**: `tools.Example.json` for more detailed examples and comments

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
