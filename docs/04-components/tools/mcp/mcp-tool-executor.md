# MCP Tool Executor

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 5
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- MCPToolExecutor implementation
- Tool execution via MCP servers
- IToolExecutor implementation for MCP

**What does NOT belong here**:
- ❌ Tool discovery → See [mcp-tool-discovery.md](mcp-tool-discovery.md)
- ❌ Tool routing logic → See [../tool-manager.md](../tool-manager.md)
- ❌ Built-in tool execution → See [../builtin/](../builtin/)

---

## Overview

MCPToolExecutor implements IToolExecutor for ToolSourceType.MCP. It routes tool calls to appropriate MCP servers and returns standardized results.

## Purpose

- **Execute** tools from MCP servers
- **Implement** IToolExecutor for MCP source type
- **Handle** tool call routing to correct server
- **Convert** MCP responses to ToolExecutionResult
- **Handle** execution timeouts and errors

## Responsibilities

- Validate tool is from MCP source
- Extract server name from tool metadata
- Connect to appropriate MCP server
- Execute tool with arguments
- Parse and return result
- Handle errors and timeouts

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPToolExecutor.cs`

```csharp
public class MCPToolExecutor : IToolExecutor
{
    public ToolSourceType SourceType => ToolSourceType.MCP;

    public async Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        // Validation, execution, result conversion
    }
}
```

### Dependencies

**Depends on**:
- `Domain/Tools/IToolExecutor` - Interface contract
- `Domain/Tools/ToolSourceType` - MCP source type
- `Domain/Configuration/MCPConfiguration` - Server config
- `Infrastructure/Tools/MCP/MCPClientWrapper` - Server communication

**Used by**:
- `Application/Tools/ToolManager` - Routes MCP tools to this executor

## Key Execution Flow

1. **Validate** tool source is MCP
2. **Extract** server name from tool metadata
3. **Get** server config for that server
4. **Create** MCPClientWrapper
5. **Connect** to server
6. **Call** tool via `wrapper.CallToolAsync()`
7. **Convert** response to ToolExecutionResult
8. **Disconnect** from server
9. **Return** result

**Timeout**: 180 seconds default (via cancellation token).

## Error Handling

- Throws `ArgumentException` if tool is not MCP source
- Wraps MCP SDK exceptions in ToolExecutionResult with error
- Logs errors for debugging

## Usage

### Integration Point (Tool Manager)

```csharp
// Tool manager routes based on source type
var executor = tool.SourceType switch
{
    ToolSourceType.MCP => mcpExecutor,
    ToolSourceType.BuiltIn => builtInExecutor,
    _ => throw new NotSupportedException()
};

var result = await executor.ExecuteAsync(tool, arguments);
```

## Related Documentation

- [MCP Client Wrapper](mcp-client-wrapper.md) - Server communication
- [Tool Manager](../tool-manager.md) - Execution routing
- [Tools Overview](../README.md) - Tool system

---

**See Also**: [MCP Tools Overview](README.md)
