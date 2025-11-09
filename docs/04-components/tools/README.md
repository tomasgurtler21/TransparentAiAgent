# Tool System

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 5-8
**Layer**: Domain + Application + Infrastructure

---

## Overview

The Tool System enables the agent to execute external capabilities via tool calls. It supports multiple tool sources (MCP servers, built-in tools) through a unified abstraction, allowing the LLM to invoke tools and receive results within conversations.

## Architecture

```
┌──────────────────────────────────────────────┐
│         Application Layer                    │
│                                               │
│  ┌────────────────────────────────────────┐ │
│  │         Tool Manager                    │ │
│  │  - Routes tool calls to executors      │ │
│  │  - Logs to transparency               │ │
│  │  - Tracks statistics                   │ │
│  └────────────────────────────────────────┘ │
└──────────────────┬──────────┬────────────────┘
                   │          │
         ┌─────────┘          └─────────┐
         │                               │
         ▼                               ▼
┌────────────────┐              ┌────────────────┐
│ MCP Subsystem  │              │ Built-in Tools │
├────────────────┤              ├────────────────┤
│ - MCPToolReg   │              │ - UIControlReg │
│ - MCPToolExec  │              │ - UIControlEx  │
│ - MCPDiscovery │              │                │
│ - MCPClient    │              │                │
└────────────────┘              └────────────────┘
```

---

## Components

### [Tool Manager](tool-manager.md)
**Layer**: Application

Coordinates tool execution. Routes LLM tool calls to appropriate executors based on source type.

**Key Features**:
- Tool routing by SourceType
- Schema validation before execution
- Transparency logging
- Usage statistics tracking

---

### [Tool Schema Validator](tool-schema-validator.md)
**Layer**: Infrastructure
**Phase**: Phase 9b (Tool Execution Safety)

Validates tool arguments against JSON Schema before execution to prevent unsafe tool calls.

**Key Features**:
- Required field validation
- Type validation (string, number, boolean, etc.)
- Enum validation
- Clear error messages for LLM to retry

**Purpose**: Critical safety layer ensuring no tool executes with invalid arguments.

---

### [MCP Tools](mcp/README.md)
**Layer**: Infrastructure

External tools from Model Context Protocol servers.

**Components**:
- [MCP Client Wrapper](mcp/mcp-client-wrapper.md) - Server communication
- [MCP Tool Registry](mcp/mcp-tool-registry.md) - Tool caching
- [MCP Tool Executor](mcp/mcp-tool-executor.md) - Tool execution
- [MCP Tool Discovery](mcp/mcp-tool-discovery.md) - Tool discovery

**Example Tools**: Filesystem operations, web search, database queries

---

### [Built-in Tools](builtin/README.md)
**Layer**: Infrastructure

Tools implemented directly in the agent codebase.

**Components**:
- [UI Control Tools](builtin/ui-control-tools.md) - Dynamic UI manipulation

**Example Tools**: `ui_control_chat_filter`, `ui_get_state`, `ui_control_transparency_viewer`

**Purpose**: Enable Teaching Mode (agent-guided UI interactions)

---

## Tool Flow

### Discovery (Startup)

```
1. Application startup
2. MCPToolRegistry.RefreshAsync()
   └─ Discovers tools from all MCP servers
3. BuiltInUIControlToolRegistry.GetAllTools()
   └─ Registers UI control tools
4. ToolRegistryComposite aggregates both
5. ToolManager initialized with composite registry
```

### Execution (Runtime)

```
1. LLM generates tool call in response
2. AgentOrchestrator detects tool calls
3. For each tool call:
   a. ToolManager.ExecuteToolCallAsync(toolCall)
   b. Lookup tool in registry
   c. Match executor by SourceType
   d. Log tool call to transparency
   e. **Validate arguments against schema (Phase 9b)**
      - If validation fails → Return error to LLM
   f. Execute via executor
   g. Log result to transparency
   h. Return ToolExecutionResult
4. Add tool results to conversation
5. Send follow-up request to LLM with results
6. LLM generates final response
```

---

## Tool Sources Comparison

| Aspect | MCP Tools | Built-in Tools |
|--------|-----------|----------------|
| **Source** | External MCP servers | Agent codebase |
| **Discovery** | Runtime (via MCP protocol) | Compile-time (hardcoded) |
| **Execution** | MCP protocol over stdio | Direct C# method calls |
| **Examples** | Filesystem, web, databases | UI control |
| **SourceType** | `ToolSourceType.MCP` | `ToolSourceType.BuiltInUIControl` |
| **Registry** | MCPToolRegistry | BuiltInUIControlToolRegistry |
| **Executor** | MCPToolExecutor | UIControlToolExecutor |
| **Configuration** | `appsettings.json` MCP section | Hardcoded in registry |

---

## Domain Abstractions

### ITool
**File**: `Domain/Tools/ITool.cs`

Represents a tool (MCP or built-in).

**Properties**:
- `Name` - Unique tool name
- `Description` - Human-readable description
- `ParametersSchema` - JSON Schema for parameters
- `SourceType` - Tool source (MCP, BuiltInUIControl, etc.)
- `Metadata` - Source-specific metadata (e.g., server name)

### IToolRegistry
**File**: `Domain/Tools/IToolRegistry.cs`

Discovers and provides access to tools.

**Methods**:
- `GetAllTools()` - All registered tools
- `GetTool(name)` - Lookup by name
- `HasTool(name)` - Check existence
- `RefreshAsync()` - Re-discover tools

### IToolExecutor
**File**: `Domain/Tools/IToolExecutor.cs`

Executes tools for a specific source type.

**Properties**:
- `SourceType` - What source this handles

**Methods**:
- `ExecuteAsync(tool, arguments)` - Execute tool, return result

### IToolManager
**File**: `Domain/Tools/IToolManager.cs`

High-level tool orchestration.

**Properties**:
- `Registry` - Access to tool registry

**Methods**:
- `ExecuteToolCallAsync(toolCall)` - Execute LLM tool call

---

## Configuration

### MCP Tools

**appsettings.json**:
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

### Built-in Tools

Hardcoded in `BuiltInUIControlToolRegistry`. No external configuration needed.

---

## Adding New Tool Sources

To add a new tool source (e.g., database tools, API tools):

1. **Define SourceType**
   - Add to `Domain/Tools/ToolSourceType.cs` enum

2. **Implement ITool**
   - Create tool implementation (e.g., `DatabaseTool`)

3. **Implement IToolRegistry**
   - Create registry (e.g., `DatabaseToolRegistry`)
   - Implement discovery logic

4. **Implement IToolExecutor**
   - Create executor (e.g., `DatabaseToolExecutor`)
   - Implement execution logic

5. **Register in DI**
   - Add registry to `ToolRegistryComposite`
   - Add executor to DI container

6. **Document**
   - Create subfolder in `docs/04-components/tools/`
   - Follow MCP/Built-in structure

---

## Testing

### Unit Tests

**Application**:
- `TransparentAiAgentCore_Tests/Application/Tools/ToolManagerTests.cs`

**Infrastructure (Validation)**:
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/ToolSchemaValidatorTests.cs` (13 tests)

**Infrastructure (MCP)**:
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/MCP/MCPToolDiscoveryIntegrationTests.cs`

**Infrastructure (Built-in)**:
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistryTests.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutorTests.cs`

---

## Teaching Mode Integration

UI Control Tools enable **Teaching Mode** - a core differentiating feature where the agent guides users by dynamically controlling the UI.

**Example Flow**:
1. Agent explains a feature
2. Agent calls `ui_control_chat_filter` to hide distracting messages
3. UI updates in real-time
4. User sees simplified view
5. Agent continues teaching

**Cross-Reference**: See `docs/03-concepts/teaching-mode/` for complete Teaching Mode documentation.

---

## Related Documentation

- [MCP Tools](mcp/README.md) - External tool subsystem
- [Built-in Tools](builtin/README.md) - Internal tool subsystem
- [Tool Manager](tool-manager.md) - Application orchestrator
- [Tool Schema Validator](tool-schema-validator.md) - Safety and validation
- [Agent Orchestrator](../core/agent-orchestrator.md) - Main tool consumer
- [Teaching Mode Concept](../../03-concepts/teaching-mode/) - Motivation and vision
- [Tool Execution Safety Plan](../../TOOL_EXECUTION_SAFETY_PLAN.md) - Implementation details

---

**See Also**: [Component Overview](../README.md)
