# MCP Tool Registry

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 5
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- MCPToolRegistry implementation
- Tool caching strategy
- IToolRegistry implementation for MCP

**What does NOT belong here**:
- ❌ Tool discovery logic → See [mcp-tool-discovery.md](mcp-tool-discovery.md)
- ❌ Tool execution → See [mcp-tool-executor.md](mcp-tool-executor.md)
- ❌ Registry composite → See [../README.md](../README.md)

---

## Overview

MCPToolRegistry implements IToolRegistry for MCP tools. It caches discovered tools from MCP servers and provides lookup/refresh capabilities.

## Purpose

- **Implement** IToolRegistry for MCP source
- **Cache** discovered tools to avoid repeated server calls
- **Provide** efficient tool lookup by name
- **Support** refresh on demand

## Responsibilities

- Initialize with MCP configuration
- Discover and cache tools from all configured MCP servers
- Provide GetAllTools(), GetTool(), HasTool()
- Refresh tools via RefreshAsync()
- Thread-safe caching with SemaphoreSlim

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPToolRegistry.cs:10`

```csharp
public class MCPToolRegistry : IToolRegistry
{
    private readonly MCPConfiguration _mcpConfiguration;
    private readonly MCPToolDiscovery _toolDiscovery;
    private readonly SemaphoreSlim _refreshLock;
    private IReadOnlyList<ITool> _cachedTools;
    private DateTime _lastRefresh;
}
```

### Dependencies

**Depends on**:
- `Domain/Tools/IToolRegistry` - Interface contract
- `Domain/Configuration/MCPConfiguration` - Server list
- `Infrastructure/Tools/MCP/MCPToolDiscovery` - Discovery service

**Used by**:
- `Infrastructure/Tools/ToolRegistryComposite` - Aggregates registries
- `Application/Tools/ToolManager` - Via composite

## Key Methods

### GetAllTools()

Returns cached list of all MCP tools.

**Performance**: O(1) - returns cached list.

### GetTool(string toolName)

Case-insensitive lookup by tool name.

**Performance**: O(n) - linear search (acceptable for tool counts <100).

### RefreshAsync()

Re-discovers tools from all MCP servers and updates cache.

**Thread-Safety**: Uses `SemaphoreSlim` to prevent concurrent refreshes.

**Flow**:
1. Acquire refresh lock
2. Call `_toolDiscovery.DiscoverToolsAsync()`
3. Update `_cachedTools`
4. Update `_lastRefresh` timestamp
5. Release lock

## Usage

### Integration Point (Composite Registry)

```csharp
var mcpRegistry = new MCPToolRegistry(config.MCP);
await mcpRegistry.RefreshAsync(); // Initial discovery

var composite = new ToolRegistryComposite(new[] { mcpRegistry, builtInRegistry });
var allTools = composite.GetAllTools(); // Includes MCP tools
```

## Related Documentation

- [MCP Tool Discovery](mcp-tool-discovery.md) - Discovery implementation
- [Tool Registry Composite](../tool-registry-composite.md) - Aggregates registries
- [Tools Overview](../README.md) - Tool system

---

**See Also**: [MCP Tools Overview](README.md)
