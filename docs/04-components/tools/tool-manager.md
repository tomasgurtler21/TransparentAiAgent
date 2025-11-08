# Tool Manager

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 5
**Layer**: Application

---

## Document Scope

**What belongs in this document**:
- ToolManager implementation
- Tool orchestration and routing logic
- Integration between registry and executors
- Transparency logging for tool calls

**What does NOT belong here**:
- ❌ Tool registries → See [mcp/](mcp/), [builtin/](builtin/)
- ❌ Tool executors → See [mcp/mcp-tool-executor.md](mcp/mcp-tool-executor.md), [builtin/ui-control-tools.md](builtin/ui-control-tools.md)
- ❌ Agent orchestration → See [../core/agent-orchestrator.md](../core/agent-orchestrator.md)

---

## Overview

ToolManager is the Application layer coordinator for tool execution. It routes LLM tool calls to appropriate executors based on tool source type, logs to transparency system, and tracks usage statistics.

## Purpose

- **Coordinate** tool discovery (via registry) and execution (via executors)
- **Route** tool calls to appropriate executor based on SourceType
- **Log** all tool calls and results to transparency system
- **Track** tool usage statistics
- **Provide** unified tool execution interface for AgentOrchestrator

## Responsibilities

- Look up tools in registry by name
- Match tools to executors by source type
- Execute tools via appropriate executor
- Log tool calls and results for transparency
- Record timing and success/failure statistics
- Handle tool not found and executor not found errors

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Application/Tools/ToolManager.cs:15`

```csharp
public class ToolManager : IToolManager
{
    private readonly IToolRegistry _registry;
    private readonly IEnumerable<IToolExecutor> _executors;
    private readonly ITransparencyService _transparencyService;
    private readonly IToolUsageStatistics _statistics;

    public IToolRegistry Registry => _registry;
}
```

### Dependencies

**Depends on**:
- `Domain/Tools/IToolRegistry` - Tool lookup
- `Domain/Tools/IToolExecutor` - Tool execution (collection)
- `Infrastructure/Transparency/ITransparencyService` - Event logging
- `Infrastructure/Tools/IToolUsageStatistics` - Usage tracking

**Used by**:
- `Application/Agent/AgentOrchestrator` - Main tool consumer

## Execution Flow

### ExecuteToolCallAsync

**Method**: `ToolManager.cs:42`

**Steps**:
1. **Lookup** tool in registry by name
   - If not found → Return failure result
2. **Find** executor matching tool.SourceType
   - If not found → Return failure result
3. **Log** tool call start to transparency system
4. **Execute** tool via executor.ExecuteAsync()
5. **Record** statistics (success/failure, duration)
6. **Log** result to transparency system
7. **Return** ToolExecutionResult

**Timing**: Tracked via Stopwatch throughout execution.

## Tool Routing Logic

```csharp
// Registry lookup
var tool = _registry.GetTool(toolCall.Name);

// Executor selection
var executor = _executors.FirstOrDefault(e => e.SourceType == tool.SourceType);

// Execution
var result = await executor.ExecuteAsync(tool, toolCall.Arguments, cancellationToken);
```

**Source Type Mapping**:
- `ToolSourceType.MCP` → MCPToolExecutor
- `ToolSourceType.BuiltInUIControl` → UIControlToolExecutor

## Transparency Logging

**Tool Call Event**:
```json
{
  "ToolName": "ui_control_chat_filter",
  "SourceType": "BuiltInUIControl",
  "Arguments": "{...}",
  "CallId": "call_abc123"
}
```

**Tool Result Event**:
```json
{
  "ToolName": "ui_control_chat_filter",
  "CallId": "call_abc123",
  "Success": true,
  "Result": "{...}",
  "ExecutionTimeMs": 45.2
}
```

## Statistics Tracking

Tracks per tool:
- Call count
- Success count
- Failure count
- Average execution time

**Used for**: Performance monitoring, debugging, analytics.

## Error Handling

**Tool not found**:
```
ToolExecutionResult.Failure("Tool 'unknown_tool' not found in registry")
```

**No executor**:
```
ToolExecutionResult.Failure("No executor found for source type MCP")
```

**Executor throws exception**: Caught by executor, returned as failure ToolExecutionResult.

## Usage

### Integration Point (Agent Orchestrator)

```csharp
// Agent detects tool calls in LLM response
foreach (var toolCall in llmResponse.ToolCalls)
{
    var result = await _toolManager.ExecuteToolCallAsync(toolCall);

    // Add result to conversation
    conversationManager.AddToolResult(toolCall.Id, result.Result);
}

// Continue conversation with tool results
var nextResponse = await llmProvider.SendRequestAsync(request);
```

## Testing

**File**: `TransparentAiAgentCore_Tests/Application/Tools/ToolManagerTests.cs`

**Tests**:
- Tool execution routing
- Error handling (tool not found, executor not found)
- Transparency logging
- Statistics recording

## Related Documentation

- [MCP Tool Executor](mcp/mcp-tool-executor.md) - MCP execution
- [UI Control Tools](builtin/ui-control-tools.md) - Built-in execution
- [Agent Orchestrator](../core/agent-orchestrator.md) - Main consumer
- [Tools Overview](README.md) - Complete tool system

---

**See Also**: [Component Overview](../README.md)
