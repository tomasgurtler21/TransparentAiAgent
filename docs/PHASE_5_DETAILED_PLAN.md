# Phase 5: Tool Integration - Detailed Plan

**Status**: Approved - Ready for Implementation
**Created**: 2025-11-01
**Reviewed**: 2025-11-01
**Phase**: 5 of 9
**Prerequisites**: Phases 1-4 complete (Basic agent functional with Azure OpenAI)

---

## Overview

This phase implements a comprehensive tool system that supports multiple tool sources while maintaining clean architecture and transparency. The key insight is that we need **tool source abstraction** - not just MCP tools, but a unified system that can handle MCP, built-in tools, and future protocols.

### Key Architectural Decision

**Problem**: We may have tools from multiple sources:
- MCP tools (external servers via MCP protocol)
- Built-in tools (native .NET implementations)
- Future protocols (other standards)

**Solution**: Create a clean **Tool Abstraction Layer** that:
1. Presents a unified interface to the LLM (same format regardless of source)
2. Routes execution to appropriate handlers based on tool metadata
3. Does NOT forward everything to MCP client immediately
4. Separates concerns: discovery, registration, routing, execution

**Benefits**:
- LLM sees consistent tool format (simplifies prompting)
- Easy to add new tool sources without changing core logic
- Clean separation between tool routing and tool execution
- Better testability (can mock different tool sources)
- Aligns with Clean Architecture (domain defines abstractions, infrastructure implements)

---

## Architecture Overview

### Layered Architecture for Tools

```
┌─────────────────────────────────────────────────────┐
│  Agent Orchestrator (Application Layer)            │
│  - Detects tool calls from LLM response            │
│  - Delegates to Tool Manager                       │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│  Tool Manager (Application Layer) - NEW            │
│  - Routes tool calls to correct executor           │
│  - Orchestrates tool execution flow                │
│  - Handles errors and logging                      │
└────────────────┬────────────────────────────────────┘
                 │
    ┌────────────┼────────────┐
    │            │            │
┌───▼─────┐ ┌───▼─────┐ ┌───▼─────────┐
│ MCP     │ │ Built-in│ │ Future      │
│ Executor│ │ Executor│ │ Protocol    │
│         │ │         │ │ Executor    │
└─────────┘ └─────────┘ └─────────────┘
(Infrastructure Layer - Implementations)
```

### Key Components

```
Domain Layer (Abstractions):
├── ITool                      - Tool definition abstraction
├── IToolRegistry              - Tool discovery & registration
├── IToolExecutor              - Tool execution abstraction
└── IToolManager               - High-level tool orchestration

Application Layer (Orchestration):
├── ToolManager                - Routes & orchestrates tool execution
├── AgentOrchestrator updates  - Integrate tool calling loop
└── ToolCallResult             - Standardized result format

Infrastructure Layer (Implementations):
├── MCP/
│   ├── MCPClientWrapper       - C# MCP SDK wrapper
│   ├── MCPToolDiscovery       - Discovers tools from MCP servers
│   ├── MCPToolExecutor        - Executes MCP tool calls
│   └── MCPToolRegistry        - Registers MCP tools
├── BuiltIn/
│   ├── BuiltInToolExecutor    - Executes built-in tools
│   └── BuiltInToolRegistry    - Registers built-in tools
└── ToolRegistryComposite      - Aggregates all tool sources
```

---

## Detailed Component Design

### 1. Domain Layer - Tool Abstractions

#### ITool Interface

```csharp
namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Represents a tool that can be called by the LLM
/// Agnostic to the underlying implementation (MCP, built-in, etc.)
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique tool name (used for routing)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// JSON Schema for parameters
    /// </summary>
    string ParametersSchema { get; }

    /// <summary>
    /// Tool source type (for routing and transparency)
    /// </summary>
    ToolSourceType SourceType { get; }

    /// <summary>
    /// Source-specific metadata (e.g., MCP server name, built-in assembly)
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}

public enum ToolSourceType
{
    MCP,
    BuiltIn,
    // Future: ExternalAPI, CustomProtocol, etc.
}
```

#### IToolExecutor Interface

```csharp
namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Executes tool calls for a specific source type
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Source type this executor handles
    /// </summary>
    ToolSourceType SourceType { get; }

    /// <summary>
    /// Execute a tool call and return result
    /// </summary>
    /// <param name="tool">Tool definition</param>
    /// <param name="arguments">JSON arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of tool execution
/// </summary>
public class ToolExecutionResult
{
    public bool Success { get; }
    public string Content { get; }  // Result content (JSON or text)
    public string? ErrorMessage { get; }
    public TimeSpan ExecutionTime { get; }

    // Factory methods
    public static ToolExecutionResult Success(string content, TimeSpan executionTime);
    public static ToolExecutionResult Failure(string errorMessage, TimeSpan executionTime);
}
```

#### IToolRegistry Interface

```csharp
namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Registry for discovering and managing tools
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Get all registered tools
    /// </summary>
    IReadOnlyList<ITool> GetAllTools();

    /// <summary>
    /// Get a tool by name (returns null if not found)
    /// </summary>
    ITool? GetTool(string toolName);

    /// <summary>
    /// Check if a tool is registered
    /// </summary>
    bool HasTool(string toolName);

    /// <summary>
    /// Refresh tool registry (re-discover from sources)
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
```

#### IToolManager Interface

```csharp
namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// High-level tool management and orchestration
/// </summary>
public interface IToolManager
{
    /// <summary>
    /// Get tool registry
    /// </summary>
    IToolRegistry Registry { get; }

    /// <summary>
    /// Execute a tool call from the LLM
    /// </summary>
    /// <param name="toolCall">Tool call from LLM</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<ToolExecutionResult> ExecuteToolCallAsync(
        LLMToolCall toolCall,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert registered tools to LLM tool format
    /// </summary>
    List<LLMTool> GetLLMToolDefinitions();
}
```

---

### 2. Application Layer - Tool Manager Implementation

#### ToolManager Class

**Location**: `TransparentAiAgentCore/Application/Tools/ToolManager.cs`

**Responsibilities**:
1. Route tool calls to appropriate executor based on tool source type
2. Log tool execution to Transparency System
3. Handle errors gracefully
4. Coordinate between registry and executors

**Key Logic**:
```csharp
public async Task<ToolExecutionResult> ExecuteToolCallAsync(
    LLMToolCall toolCall,
    CancellationToken cancellationToken)
{
    // 1. Lookup tool in registry
    var tool = _registry.GetTool(toolCall.Name);
    if (tool == null)
        return ToolExecutionResult.Failure($"Tool '{toolCall.Name}' not found");

    // 2. Find executor for this tool's source type
    var executor = _executors.FirstOrDefault(e => e.SourceType == tool.SourceType);
    if (executor == null)
        return ToolExecutionResult.Failure($"No executor for {tool.SourceType}");

    // 3. Log to Transparency System
    _transparencyService.LogEvent(new TransparencyEvent(
        TransparencyEventType.ToolCallStarted,
        new { ToolName = tool.Name, SourceType = tool.SourceType, Arguments = toolCall.Arguments }
    ));

    // 4. Execute (with error handling)
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var result = await executor.ExecuteAsync(tool, toolCall.Arguments, cancellationToken);

        // 5. Log result
        _transparencyService.LogEvent(new TransparencyEvent(
            result.Success ? TransparencyEventType.ToolCallCompleted : TransparencyEventType.ToolCallFailed,
            new { ToolName = tool.Name, Result = result }
        ));

        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _transparencyService.LogEvent(new TransparencyEvent(
            TransparencyEventType.ToolCallFailed,
            new { ToolName = tool.Name, Error = ex.Message }
        ));
        return ToolExecutionResult.Failure(ex.Message, stopwatch.Elapsed);
    }
}
```

---

### 3. Infrastructure Layer - MCP Implementation

#### MCPClientWrapper

**Location**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPClientWrapper.cs`

**Purpose**: Thin wrapper around C# MCP SDK client

**Responsibilities**:
- Connect to MCP servers from configuration
- Manage server lifecycle (start, stop, health check)
- Provide clean interface to MCP operations
- Abstract SDK-specific details

**Key Points**:
- Uses `ModelContextProtocol` NuGet package (C# MCP SDK)
- Manages stdio transport connections
- Handles server process lifecycle

#### MCPToolDiscovery

**Location**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPToolDiscovery.cs`

**Purpose**: Discovers tools from MCP servers

**Flow**:
1. Connect to each configured MCP server
2. Send `tools/list` request
3. Parse tool definitions
4. Convert to `ITool` instances with `SourceType = MCP`
5. Include metadata (server name, server version)

#### MCPToolExecutor

**Location**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPToolExecutor.cs`

**Purpose**: Executes MCP tool calls

**Implementation**: `IToolExecutor` for `SourceType.MCP`

**Flow**:
1. Validate tool is from MCP source
2. Get MCP server connection from metadata
3. Send `tools/call` request with arguments
4. Parse result
5. Return `ToolExecutionResult`

#### MCPToolRegistry

**Location**: `TransparentAiAgentCore/Infrastructure/Tools/MCP/MCPToolRegistry.cs`

**Purpose**: Maintains MCP tool catalog

**Responsibilities**:
- Cache discovered MCP tools
- Refresh on demand
- Track server connection status

---

### 4. Infrastructure Layer - Built-In Tools (Future)

**Note**: Initial implementation focuses on MCP. Built-in tools architecture is designed here for future extensibility.

#### BuiltInToolExecutor

**Location**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltIn/BuiltInToolExecutor.cs`

**Purpose**: Executes built-in .NET tools

**Why Separate from MCP**:
- No network overhead
- Direct .NET method invocation
- Can use reflection or explicit registration
- Useful for system operations (reset conversation, export history, etc.)

**Example Built-In Tools** (future):
- `reset_conversation` - Clear conversation history
- `export_conversation` - Export chat to JSON/Markdown
- `get_system_info` - Get transparency system stats

---

### 5. Tool Registry Composite

**Location**: `TransparentAiAgentCore/Infrastructure/Tools/ToolRegistryComposite.cs`

**Purpose**: Aggregates tools from all sources (MCP, built-in, future)

**Pattern**: Composite Pattern

**Implementation**:
```csharp
public class ToolRegistryComposite : IToolRegistry
{
    private readonly List<IToolRegistry> _registries;

    public ToolRegistryComposite(IEnumerable<IToolRegistry> registries)
    {
        _registries = registries.ToList();
    }

    public IReadOnlyList<ITool> GetAllTools()
    {
        // Aggregate from all registries
        return _registries
            .SelectMany(r => r.GetAllTools())
            .ToList()
            .AsReadOnly();
    }

    public ITool? GetTool(string toolName)
    {
        // Search all registries (first match wins)
        foreach (var registry in _registries)
        {
            var tool = registry.GetTool(toolName);
            if (tool != null) return tool;
        }
        return null;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        // Refresh all registries in parallel
        await Task.WhenAll(_registries.Select(r => r.RefreshAsync(cancellationToken)));
    }
}
```

---

### 6. Agent Orchestrator Updates

**Location**: `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs`

**New Logic**: Tool calling loop

**Flow**:
```
1. User sends message
2. Add to conversation
3. Prepare LLM request (include tool definitions)
   ↓
4. Call LLM (streaming)
   ↓
5. Receive response
   ├─→ Text only: Done, return to user
   │
   └─→ Contains tool calls:
       ├─→ For each tool call:
       │   ├─→ Execute via ToolManager
       │   └─→ Create ToolResultMessage
       ├─→ Add tool results to conversation
       └─→ Back to step 3 (continue loop with tool results)
```

**Implementation Considerations**:
- Maximum tool call depth (prevent infinite loops)
- Parallel vs sequential tool execution
- Error handling (tool failures)
- Streaming: How to handle tool calls mid-stream?

**Streaming Challenge**:
- LLM streams text, then tool calls at end
- Must buffer full response to detect tool calls
- Then execute tools
- Then continue conversation

**Solution**:
- Stream text to UI as it arrives
- Buffer complete response
- When stream ends, check for tool calls
- If tool calls present, execute and continue loop

---

## Configuration Updates

### MCPConfiguration Updates

**Location**: `TransparentAiAgentCore/Domain/Configuration/MCPConfiguration.cs`

**Current** (from Phase 1-4):
```csharp
public class MCPConfiguration
{
    public List<MCPServerConfiguration> Servers { get; set; } = new();
}

public class MCPServerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string>? Arguments { get; set; }
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
}
```

**Enhancement** (for Phase 5):
```csharp
public class MCPConfiguration
{
    public List<MCPServerConfiguration> Servers { get; set; } = new();

    /// <summary>
    /// Auto-discover tools on startup (default: true)
    /// </summary>
    public bool AutoDiscoverTools { get; set; } = true;

    /// <summary>
    /// Timeout for tool execution (seconds)
    /// </summary>
    public int ToolExecutionTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Maximum tool call depth (prevent infinite loops)
    /// </summary>
    public int MaxToolCallDepth { get; set; } = 10;
}
```

### AgentConfiguration Updates

**Location**: `TransparentAiAgentCore/Domain/Configuration/AgentConfiguration.cs`

**Add**:
```csharp
public class AgentConfiguration
{
    // ... existing properties ...

    /// <summary>
    /// Enable tool calling (default: true)
    /// </summary>
    public bool EnableTools { get; set; } = true;

    /// <summary>
    /// Tool execution mode: Sequential or Parallel
    /// </summary>
    public ToolExecutionMode ToolExecutionMode { get; set; } = ToolExecutionMode.Sequential;
}

public enum ToolExecutionMode
{
    Sequential,  // Execute tools one at a time
    Parallel     // Execute all tool calls in parallel (when multiple in one response)
}
```

---

## Transparency Events

### New Event Types

**Add to**: `TransparentAiAgentCore/Domain/Transparency/TransparencyEventType.cs`

```csharp
public enum TransparencyEventType
{
    // ... existing events ...

    // Tool discovery
    ToolDiscoveryStarted,
    ToolDiscoveryCompleted,
    ToolDiscoveryFailed,
    ToolRegistered,

    // Tool execution
    ToolCallStarted,
    ToolCallCompleted,
    ToolCallFailed,
    ToolCallTimeout,

    // MCP server lifecycle
    MCPServerConnecting,
    MCPServerConnected,
    MCPServerDisconnected,
    MCPServerConnectionFailed,
}
```

### Event Data Examples

**Tool Discovery**:
```json
{
  "eventType": "ToolDiscoveryStarted",
  "timestamp": "2025-11-01T10:00:00Z",
  "data": {
    "serverName": "todo-list",
    "serverCommand": "npx",
    "serverArgs": ["-y", "@anthropic/mcp-server-todo-list"]
  }
}
```

**Tool Call**:
```json
{
  "eventType": "ToolCallStarted",
  "timestamp": "2025-11-01T10:00:05Z",
  "data": {
    "toolName": "add_todo",
    "sourceType": "MCP",
    "arguments": {
      "task": "Review Phase 5 plan"
    },
    "metadata": {
      "serverName": "todo-list"
    }
  }
}
```

**Tool Result**:
```json
{
  "eventType": "ToolCallCompleted",
  "timestamp": "2025-11-01T10:00:06Z",
  "data": {
    "toolName": "add_todo",
    "success": true,
    "executionTimeMs": 150,
    "result": {
      "id": "todo-123",
      "task": "Review Phase 5 plan",
      "created": "2025-11-01T10:00:06Z"
    }
  }
}
```

---

## Implementation Plan - Step by Step

### Step 1: Domain Abstractions (TDD)

**Goal**: Define tool abstractions

**Components**:
1. `ITool` interface
2. `IToolExecutor` interface
3. `IToolRegistry` interface
4. `IToolManager` interface
5. `ToolExecutionResult` class
6. `ToolSourceType` enum

**Tests**:
- `ToolExecutionResult` factory methods
- Interface contracts (using mocks)

**Time**: ~2-3 hours

---

### Step 2: Tool Manager (Application Layer)

**Goal**: Implement tool orchestration logic

**Components**:
1. `ToolManager` class implementing `IToolManager`
2. Tool routing logic
3. Integration with Transparency System

**Tests**:
- Route to correct executor based on source type
- Handle tool not found
- Handle executor not found
- Log events correctly
- Error handling

**Time**: ~3-4 hours

---

### Step 3: MCP Client Integration

**Goal**: Integrate C# MCP SDK

**Tasks**:
1. Add `ModelContextProtocol` NuGet package
2. Implement `MCPClientWrapper`
3. Connection management
4. Error handling

**Components**:
1. `MCPClientWrapper` class
2. MCP server lifecycle management

**Tests**:
- Connection to MCP server
- Send request / receive response
- Handle connection failures
- Server lifecycle (start, stop)

**Time**: ~4-5 hours

**Note**: This is a critical step - MCP SDK integration has specific patterns

---

### Step 4: MCP Tool Discovery

**Goal**: Discover tools from MCP servers

**Components**:
1. `MCPToolDiscovery` class
2. Tool definition parsing
3. Conversion to `ITool` instances

**Tests**:
- Discover tools from mock MCP server
- Parse tool definitions correctly
- Handle discovery errors
- Include correct metadata

**Time**: ~3-4 hours

---

### Step 5: MCP Tool Executor

**Goal**: Execute MCP tool calls

**Components**:
1. `MCPToolExecutor` implementing `IToolExecutor`
2. Tool call execution via MCP SDK
3. Result parsing

**Tests**:
- Execute tool call successfully
- Parse result correctly
- Handle execution errors
- Handle timeouts
- Measure execution time

**Time**: ~3-4 hours

---

### Step 6: MCP Tool Registry

**Goal**: Manage MCP tool catalog

**Components**:
1. `MCPToolRegistry` implementing `IToolRegistry`
2. Tool caching
3. Refresh logic

**Tests**:
- Register tools
- Retrieve tools by name
- Get all tools
- Refresh registry
- Handle duplicate tool names

**Time**: ~2-3 hours

---

### Step 7: Tool Registry Composite

**Goal**: Aggregate multiple tool sources

**Components**:
1. `ToolRegistryComposite` implementing `IToolRegistry`
2. Multi-registry aggregation

**Tests**:
- Aggregate tools from multiple registries
- Tool lookup across registries
- Refresh all registries

**Time**: ~2 hours

---

### Step 8: Agent Orchestrator Integration

**Goal**: Integrate tool calling into conversation loop

**Updates**:
1. Detect tool calls in LLM response
2. Execute tool calls via `ToolManager`
3. Add tool results to conversation
4. Continue loop with tool results
5. Handle maximum depth limit

**Tests**:
- Single tool call flow
- Multiple tool calls in one response
- Tool call loop (tool calls another tool)
- Maximum depth limit
- Error handling in tool execution

**Time**: ~4-5 hours

**Challenges**:
- Streaming + tool calls coordination
- Loop termination conditions
- Error recovery

---

### Step 9: LLM Request Updates

**Goal**: Include tool definitions in LLM requests

**Updates**:
1. `MessagePipeline` - add tool definitions to request
2. Convert `ITool` to `LLMTool` format

**Tests**:
- Tool definitions included in request
- Correct LLM tool format

**Time**: ~2 hours

---

### Step 10: Configuration Updates

**Goal**: Support tool configuration

**Updates**:
1. `MCPConfiguration` enhancements
2. `AgentConfiguration` tool settings
3. Configuration validation

**Tests**:
- Load tool configuration
- Validate tool settings
- Default values

**Time**: ~2 hours

---

### Step 11: Transparency Integration

**Goal**: Log all tool events

**Updates**:
1. New `TransparencyEventType` values
2. Event logging in `ToolManager`
3. Event logging in MCP components

**Tests**:
- Tool discovery events logged
- Tool execution events logged
- MCP server lifecycle events logged

**Time**: ~2-3 hours

---

### Step 12: End-to-End Testing

**Goal**: Validate complete tool flow

**Tests**:
1. Connect to real MCP server (todo-list)
2. Discover tools
3. Have LLM call a tool
4. Execute tool
5. Get result back to LLM
6. Verify transparency events

**Manual Testing Scenarios**:
1. "Add a todo: Review Phase 5 plan"
2. "List all my todos"
3. Test with context7 server (documentation search)

**Time**: ~3-4 hours

---

### Step 13: Error Handling & Edge Cases

**Goal**: Robust error handling

**Scenarios**:
1. MCP server not responding
2. Tool execution timeout
3. Invalid tool arguments
4. Tool not found
5. Infinite tool call loops
6. MCP server crash mid-execution

**Tests**: For each scenario

**Time**: ~3-4 hours

---

### Step 14: UI Updates (Basic)

**Goal**: Display tool calls in UI

**Updates** (to Blazor GUI):
1. Display tool calls in conversation
2. Display tool results
3. Visual indicator for tool execution
4. Tool execution status (in progress, completed, failed)

**Basic UI Requirements**:
- Show tool calls as special message type
- Format JSON arguments readably
- Show tool results
- Indicate source type (MCP, built-in, etc.)

**Time**: ~3-4 hours

**Note**: Enhanced tool UI (tool browser, schemas, etc.) comes in Phase 6

---

## Testing Strategy

### Unit Tests (TDD)

**Components to Test**:
1. All domain interfaces (via mocks)
2. `ToolManager` logic
3. `ToolRegistryComposite` aggregation
4. `MCPToolDiscovery` parsing
5. `MCPToolExecutor` execution logic
6. `MCPToolRegistry` management
7. Agent orchestrator tool loop
8. Configuration validation

**Approach**: Write tests first, implement to pass

---

### Integration Tests

**Scenarios**:
1. Connect to mock MCP server
2. Discover tools
3. Execute tool call
4. Full conversation with tool calls

**Approach**: Use test doubles for MCP SDK where appropriate

---

### Functional Tests

**Requirements**:
- Real MCP servers (todo-list, context7)
- Real LLM calls (Azure OpenAI)
- Manual verification

**Test Cases**:
1. "Add a todo: Review Phase 5"
2. "What's in my todo list?"
3. "Search context7 docs for 'authentication'"

---

## Dependencies

### NuGet Packages

**New Dependency**:
```xml
<PackageReference Include="ModelContextProtocol" Version="<latest>" />
```

**Note**: Verify exact package name from Microsoft.Extensions.AI or Anthropic sources

### MCP Servers (for testing)

1. **todo-list**: `npx -y @anthropic/mcp-server-todo-list`
2. **context7**: (user already has configured)

---

## Deliverables

### Code Deliverables

1. **Domain Layer**:
   - `ITool`, `IToolExecutor`, `IToolRegistry`, `IToolManager` interfaces
   - `ToolExecutionResult` class
   - `ToolSourceType` enum

2. **Application Layer**:
   - `ToolManager` implementation
   - Agent orchestrator updates

3. **Infrastructure Layer**:
   - `MCPClientWrapper`
   - `MCPToolDiscovery`
   - `MCPToolExecutor`
   - `MCPToolRegistry`
   - `ToolRegistryComposite`

4. **Configuration**:
   - Updated `MCPConfiguration`
   - Updated `AgentConfiguration`

5. **Transparency**:
   - New event types
   - Event logging

### Functional Deliverable

**End-to-End Tool Flow**:
```
User: "Add a todo: Review Phase 5"
  ↓
Agent adds to conversation
  ↓
LLM called with tools=[add_todo, list_todos, ...]
  ↓
LLM responds with tool_call: add_todo(task="Review Phase 5")
  ↓
ToolManager routes to MCPToolExecutor
  ↓
MCPToolExecutor calls todo-list server
  ↓
Server returns: {id: "123", task: "Review Phase 5"}
  ↓
ToolManager logs to Transparency
  ↓
Agent adds ToolResultMessage to conversation
  ↓
LLM called again with tool result
  ↓
LLM responds: "I've added 'Review Phase 5' to your todo list."
  ↓
User sees response in UI
```

### Documentation Deliverables

1. Updated `COMPONENTS.md` with tool components
2. Tool architecture diagram
3. Configuration examples for MCP servers
4. Tool execution flow documentation

---

## Time Estimate

| Step | Component | Estimated Time |
|------|-----------|----------------|
| 1 | Domain abstractions | 2-3 hours |
| 2 | ToolManager | 3-4 hours |
| 3 | MCP client integration | 4-5 hours |
| 4 | MCP tool discovery | 3-4 hours |
| 5 | MCP tool executor | 3-4 hours |
| 6 | MCP tool registry | 2-3 hours |
| 7 | Tool registry composite | 2 hours |
| 8 | Agent orchestrator updates | 4-5 hours |
| 9 | LLM request updates | 2 hours |
| 10 | Configuration updates | 2 hours |
| 11 | Transparency integration | 2-3 hours |
| 12 | End-to-end testing | 3-4 hours |
| 13 | Error handling | 3-4 hours |
| 14 | Basic UI updates | 3-4 hours |

**Total**: ~40-50 hours (~1-1.5 weeks with focused work)

---

## Success Criteria

### Functional

- [ ] Can connect to MCP servers from config
- [ ] Can discover tools from MCP servers
- [ ] LLM can call tools successfully
- [ ] Tool results returned to LLM
- [ ] Conversation continues after tool calls
- [ ] Multiple tool calls in single response work
- [ ] Tool execution visible in Transparency System
- [ ] Tool calls displayed in UI

### Technical

- [ ] Clean architecture maintained
- [ ] Tool source abstraction works (easy to add new sources)
- [ ] All components have unit tests
- [ ] Integration tests pass
- [ ] Error handling robust
- [ ] Configuration validation works
- [ ] Logging comprehensive

### User Experience

- [ ] User can see tool calls in conversation
- [ ] Tool execution status visible
- [ ] Tool results readable
- [ ] Errors user-friendly
- [ ] Transparency events helpful

---

## Risks & Mitigations

### Risk 1: MCP SDK Integration Complexity

**Risk**: C# MCP SDK client-side API may differ from server-side (user familiar with server-side)

**Mitigation**:
- Review SDK documentation first
- Create thin wrapper (`MCPClientWrapper`) to isolate SDK
- Test connection early (Step 3)
- Allocate extra time for SDK learning

### Risk 2: Streaming + Tool Call Coordination

**Risk**: Handling tool calls mid-stream is complex

**Mitigation**:
- Buffer complete response before processing tool calls
- Stream text to UI, but wait for completion
- Clear state machine for tool call loop
- Thorough testing of streaming scenarios

### Risk 3: Tool Execution Timeouts

**Risk**: Tool execution may hang or timeout

**Mitigation**:
- Implement timeout configuration
- Cancel tool execution on timeout
- Log timeout events
- Return error result to LLM

### Risk 4: Infinite Tool Call Loops

**Risk**: LLM may call tools infinitely

**Mitigation**:
- Maximum depth limit in configuration
- Track tool call depth in orchestrator
- Return error when limit exceeded
- Log loop detection to Transparency

### Risk 5: MCP Server Crashes

**Risk**: MCP servers may crash during operation

**Mitigation**:
- Health check mechanism
- Auto-reconnect logic
- Graceful degradation (tools unavailable)
- User notification via Transparency

---

## Future Enhancements (Post-Phase 5)

These are explicitly **not** in scope for Phase 5:

### Built-In Tools Implementation
- Implement `BuiltInToolExecutor`
- Implement `BuiltInToolRegistry`
- Create initial built-in tools (reset, export, etc.)

### Enhanced Tool UI (Phase 6)
- Tool browser (list all available tools)
- Tool schema viewer
- Tool execution history
- Tool usage statistics

### Advanced Features
- Parallel tool execution
- Tool call caching
- Tool permission system
- Tool call confirmation (ask user before executing)

### Other Tool Protocols
- REST API tools
- GraphQL tools
- Custom protocol support

---

## Review Decisions (Approved 2025-11-01)

1. **Architecture**: ✅ Approved - Tool abstraction layer meets requirements for clean separation

2. **Tool Sources**: ✅ Approved - Distinction between MCP, built-in, and future sources is clear

3. **Routing Logic**: ✅ Approved - Tool routing by `SourceType` and tool name works well

4. **Built-In Tools**: ⏭️ **Deferred to post-Phase 5** - Focus on MCP tools now, built-in tools later
   - **Action**: Document integration points for built-in tools in relevant docs

5. **Tool Execution Mode**: 🔄 **Start Sequential, Design for Parallel**
   - Initial implementation: Sequential execution
   - Final goal: Parallel execution (configurable)
   - **Decision**: Start with sequential (simpler), refactor to parallel is minimal (see analysis below)
   - Configuration option added for future use

6. **Tool Call Depth**: ✅ **Max depth = 10** (increased from 5, easily configurable)

7. **Streaming Strategy**: ✅ Approved - Buffer complete response before processing tool calls
   - Note: LLM typically puts tool calls at end of stream anyway

8. **Error Handling**: ✅ Approved with adjustment
   - **Timeout increased to 180 seconds** (up from 30)

9. **Configuration**: ✅ Approved - Tool-related config settings are appropriate

10. **UI Requirements**: ✅ Approved - Basic tool display is sufficient for Phase 5
    - Enhanced tool UI will come later with significant rework

### Sequential vs Parallel Execution - Refactor Analysis

**Current Decision**: Start with **sequential** execution in Phase 5

**Rationale**:
- Simpler implementation and error handling
- Easier debugging and testing
- Refactor to parallel is minimal (analysis below)

**Refactor Impact Assessment**:

The interface design supports both modes without changes:
```csharp
// Sequential (Phase 5 implementation)
foreach (var toolCall in toolCalls)
{
    var result = await _toolManager.ExecuteToolCallAsync(toolCall);
    results.Add(result);
}

// Parallel (future refactor)
var tasks = toolCalls.Select(tc => _toolManager.ExecuteToolCallAsync(tc));
var results = await Task.WhenAll(tasks);
```

**Refactor scope**: Only affects Agent Orchestrator's tool execution loop (~20-30 lines of code)

**Interface stability**: `IToolManager.ExecuteToolCallAsync(LLMToolCall)` doesn't need to change

**Configuration**: `ToolExecutionMode` enum already in design, just switches between implementations

**Conclusion**: Refactor is minimal, safe to start with sequential and upgrade later

---

## Next Steps After Review

1. **Review & Discussion**: Discuss this plan, address questions, refine architecture
2. **Finalize Decisions**: Make final calls on open questions
3. **Update DESIGN_DECISIONS.md**: Log new decisions (DD-019, DD-020, etc.)
4. **Begin Implementation**: Start with Step 1 (Domain abstractions) using TDD

---

**Last Updated**: 2025-11-01
**Status**: Approved - Ready for Implementation

---

## Documentation Updates Required

Before implementation, update these documents with Phase 5 design:

### 1. ARCHITECTURE.md
- [ ] Add Tool System architecture section
- [ ] Update system layers diagram to include Tool Manager
- [ ] Add tool data flow diagram
- [ ] Document tool routing strategy

### 2. COMPONENTS.md
- [ ] Add Tool Management components section
- [ ] Document `IToolManager`, `IToolExecutor`, `IToolRegistry`
- [ ] Document MCP implementation components
- [ ] Document integration points for future built-in tools

### 3. DESIGN_DECISIONS.md
- [ ] **DD-019**: Tool Abstraction Layer Design
- [ ] **DD-020**: Tool Routing by Source Type
- [ ] **DD-021**: Sequential vs Parallel Tool Execution (start sequential)
- [ ] **DD-022**: Tool Execution Timeout (180 seconds)
- [ ] **DD-023**: Tool Call Depth Limit (10 levels)
- [ ] **DD-024**: Built-In Tools Deferred to Post-Phase 5

### 4. IMPLEMENTATION_ROADMAP.md
- [ ] Update Phase 5 description with approved architecture
- [ ] Add note about built-in tools in future phases
- [ ] Update time estimates based on approved scope

### 5. Create TOOL_INTEGRATION_POINTS.md (New Document)
**Purpose**: Document how to add new tool sources (for future built-in tools and protocols)

**Contents**:
- How to implement `IToolExecutor` for a new source
- How to implement `IToolRegistry` for a new source
- How to register with `ToolRegistryComposite`
- Configuration requirements for new sources
- Testing requirements
- Examples: MCP implementation as reference

This document will serve as a guide when implementing built-in tools later.
