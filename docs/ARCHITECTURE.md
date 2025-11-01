# TransparentAiAgent - Architecture Document

## Overview

This document describes the high-level architecture of the TransparentAiAgent framework.

**Status**: Architecture design complete - ready for implementation planning

## Architectural Goals

1. **Separation of Concerns**: Clear boundaries between agent logic, LLM integration, and UI
2. **Testability**: Design for easy unit and integration testing (TDD approach)
3. **Extensibility**: MCP-based architecture for adding capabilities
4. **Maintainability**: Clean code following SOLID principles
5. **Transparency**: All actions, tool calls, and context visible in real-time
6. **Streaming**: Real-time streaming of LLM responses and events to UI

## Architectural Style

**Clean Architecture with Layered Design**

The system follows Clean Architecture principles with clear dependency rules:
- Dependencies point inward (UI → Application → Domain → Infrastructure)
- Domain layer has no external dependencies
- Infrastructure implements interfaces defined in inner layers

## High-Level Architecture

### System Layers (Updated for Phase 5 - Tool Integration)

```
┌───────────────────────────────────────────────────────┐
│              Presentation Layer                        │
│         (TransparentAiAgentGui - Blazor)              │
│                                                        │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │ Chat         │  │ Configuration│  │ Tools View │ │
│  │ Component    │  │ Component    │  │ Component  │ │
│  └──────────────┘  └──────────────┘  └────────────┘ │
│                                                        │
│  ┌──────────────────────────────────────────────────┐│
│  │      State Management & SSE Client               ││
│  └──────────────────────────────────────────────────┘│
└────────────────────────┬──────────────────────────────┘
                         │ HTTP/SSE
                         │
┌────────────────────────▼──────────────────────────────┐
│              Application Layer                         │
│         (TransparentAiAgentCore - Services)           │
│                                                        │
│  ┌──────────────────────────────────────────────────┐│
│  │         Agent Orchestrator                       ││
│  │  (Main loop, coordinates all operations)         ││
│  │  + Tool calling loop integration                 ││
│  └──────────────────────────────────────────────────┘│
│                         │                             │
│       ┌─────────────────┼─────────────────┬──────────┤
│       │                 │                 │          │
│  ┌────▼─────┐  ┌────────▼──────┐  ┌──────▼──────┐  │
│  │ Message  │  │ Conversation  │  │ Transparency│  │
│  │ Pipeline │  │ Manager       │  │ Service     │  │
│  └──────────┘  └───────────────┘  └─────────────┘  │
│                                                       │
│  ┌──────────────────────────────────────────────────┐│
│  │         Tool Manager (NEW - Phase 5)             ││
│  │  - Routes tool calls by source type              ││
│  │  - Orchestrates tool execution                   ││
│  │  - Integrates with Transparency System           ││
│  └──────────────────────────────────────────────────┘│
└────────────────────────┬──────────────────────────────┘
                         │
┌────────────────────────▼──────────────────────────────┐
│               Domain Layer                             │
│         (Core abstractions & interfaces)              │
│                                                        │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │ ILLMProvider │  │ ITool        │  │ IMessage   │ │
│  │ Interface    │  │ Interface    │  │ Models     │ │
│  └──────────────┘  └──────────────┘  └────────────┘ │
│                                                        │
│  ┌──────────────────────────────────────────────────┐│
│  │  Tool Abstractions (NEW - Phase 5)               ││
│  │  - IToolExecutor   - IToolRegistry               ││
│  │  - IToolManager    - ToolExecutionResult         ││
│  └──────────────────────────────────────────────────┘│
└────────────────────────┬──────────────────────────────┘
                         │
┌────────────────────────▼──────────────────────────────┐
│           Infrastructure Layer                         │
│      (External integrations & implementations)        │
│                                                        │
│  ┌──────────────────┐        ┌──────────────────┐    │
│  │  LLM Providers   │        │  Tool System     │    │
│  │  - AzureOpenAI   │        │  (NEW - Phase 5) │    │
│  │  - Anthropic     │        │                  │    │
│  │  - Streaming     │        │  MCP Tools:      │    │
│  └──────────────────┘        │  - MCPClient     │    │
│                               │  - MCPDiscovery  │    │
│  ┌──────────────────┐        │  - MCPExecutor   │    │
│  │  Configuration   │        │  - MCPRegistry   │    │
│  │  Manager         │        │                  │    │
│  └──────────────────┘        │  (Future):       │    │
│                               │  - BuiltInExec   │    │
│  ┌──────────────────┐        │  - BuiltInReg    │    │
│  │  Authentication  │        │                  │    │
│  │  Manager         │        │  Composite:      │    │
│  └──────────────────┘        │  - ToolRegistry  │    │
│                               │    Composite     │    │
│                               └──────────────────┘    │
└───────────────────────────────────────────────────────┘
```

## Core Component Details

For detailed component documentation, see [COMPONENTS.md](COMPONENTS.md)

### Key Components Summary

1. **Agent Orchestrator**: Main coordination loop
   - Receives user input
   - Manages conversation flow
   - Coordinates LLM calls and tool execution
   - Publishes transparency events

2. **LLM Provider System**:
   - Abstraction: `ILLMProvider` interface
   - Implementations: Azure OpenAI, Anthropic
   - Handles streaming responses
   - Manages tool call execution

3. **MCP Integration**:
   - MCP Client: Protocol implementation
   - Tool Registry: Discovers and registers tools
   - Tool Executor: Executes tool calls from LLM

4. **Transparency System**:
   - Event-based logging of all actions
   - Real-time streaming to UI via SSE
   - Structured event format (JSON)

5. **Configuration System**:
   - File-based configuration (JSON)
   - Runtime configuration via UI
   - Hot-reload support

## Data Flow

### Main Conversation Flow

```
1. User Input
   │
   ▼
2. Chat Component → State Management
   │
   ▼
3. Agent Orchestrator
   │
   ▼
4. Conversation Manager (add to context)
   │
   ▼
5. Message Pipeline (prepare LLM request)
   │
   ▼
6. LLM Provider (streaming call)
   │
   ├─→ Transparency System (log request)
   │
   ▼
7. Streaming Handler
   │
   ├─→ SSE to UI (real-time display)
   ├─→ Transparency System (log chunks)
   │
   ▼
8. Tool Calls Detected?
   │
   ├─→ YES: Tool Executor → MCP Client
   │   │
   │   ├─→ Transparency System (log tool call)
   │   │
   │   ▼
   │   Tool Result → back to step 4 (continue conversation)
   │
   └─→ NO: Conversation complete
```

### Transparency Event Flow

```
Any Component
   │
   ▼
Transparency System
   │
   ├─→ Structured Event (JSON)
   │
   ▼
Event Store (in-memory for now)
   │
   ▼
SSE Stream
   │
   ▼
UI Components (real-time display)
```

### Tool Execution Flow (Phase 5 - NEW)

**Detailed flow for tool calling loop:**

```
1. LLM Response received with tool calls
   │
   ▼
2. Agent Orchestrator detects tool calls
   │
   ▼
3. For each tool call (sequential in Phase 5):
   │
   ├─→ Tool Manager: ExecuteToolCallAsync(toolCall)
   │    │
   │    ├─→ Lookup tool in registry by name
   │    │    │
   │    │    ├─→ Tool found? Get SourceType
   │    │    │
   │    │    └─→ Tool not found? Return error
   │    │
   │    ├─→ Find executor for SourceType
   │    │    │
   │    │    ├─→ MCP? → MCPToolExecutor
   │    │    ├─→ BuiltIn? → BuiltInToolExecutor (future)
   │    │    └─→ Not found? Return error
   │    │
   │    ├─→ Log tool call started (Transparency)
   │    │
   │    ├─→ Executor.ExecuteAsync(tool, arguments)
   │    │    │
   │    │    ├─→ MCP: Send to MCP server via MCPClient
   │    │    │         │
   │    │    │         ├─→ Timeout after 180 seconds
   │    │    │         └─→ Return result
   │    │    │
   │    │    └─→ BuiltIn: Direct .NET method call (future)
   │    │
   │    ├─→ Log tool call completed/failed (Transparency)
   │    │
   │    └─→ Return ToolExecutionResult
   │
   ├─→ Create ToolResultMessage with result
   │
   └─→ Add to conversation
   │
   ▼
4. Check tool call depth limit (max 10)
   │
   ├─→ Depth > 10? Return error to user
   │
   └─→ Depth OK? Continue
   │
   ▼
5. Back to LLM with tool results (repeat from step 1)
   │
   ▼
6. LLM responds without tool calls → Done
```

## Tool System Architecture (Phase 5 - NEW)

### Overview

The tool system is designed for **source-agnostic tool integration**. Tools can come from multiple sources (MCP servers, built-in .NET methods, future protocols), but all present a unified interface to the LLM.

### Key Design Principles

1. **Source Abstraction**: LLM sees same tool format regardless of source
2. **Routing Layer**: Tool Manager routes calls based on metadata, not hardcoded logic
3. **Clean Separation**: Discovery, registration, routing, and execution are separate concerns
4. **Extensibility**: Easy to add new tool sources without changing core logic

### Tool Abstraction Layer

```
┌─────────────────────────────────────────────────┐
│  LLM Provider                                   │
│  - Receives tool definitions (unified format)   │
│  - Returns tool calls (tool name + arguments)   │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│  Tool Manager (Application Layer)               │
│  ┌───────────────────────────────────────────┐  │
│  │ Routing Logic:                            │  │
│  │ 1. Lookup tool by name                    │  │
│  │ 2. Check tool.SourceType                  │  │
│  │ 3. Route to appropriate executor          │  │
│  │ 4. Log to Transparency System             │  │
│  └───────────────────────────────────────────┘  │
└────────────────┬────────────────────────────────┘
                 │
        ┌────────┴─────────┬──────────────┐
        │                  │              │
        ▼                  ▼              ▼
┌──────────────┐  ┌──────────────┐  ┌─────────┐
│ MCP Executor │  │BuiltIn Exec  │  │ Future  │
│ (Phase 5)    │  │ (Future)     │  │ Protocol│
└──────────────┘  └──────────────┘  └─────────┘
```

### Component Responsibilities

**Domain Layer** (Abstractions):
- `ITool` - Tool definition (name, description, schema, source type, metadata)
- `IToolExecutor` - Executes tools for specific source type
- `IToolRegistry` - Discovers and manages tools
- `IToolManager` - High-level orchestration and routing

**Application Layer** (Orchestration):
- `ToolManager` - Implements routing and orchestration logic
- Integrates with Transparency System for logging
- Handles errors and timeouts

**Infrastructure Layer** (Implementations):
- **MCP Implementation** (Phase 5):
  - `MCPClientWrapper` - Wraps C# MCP SDK
  - `MCPToolDiscovery` - Discovers tools from MCP servers
  - `MCPToolExecutor` - Executes MCP tool calls
  - `MCPToolRegistry` - Manages MCP tool catalog
- **Built-In Implementation** (Future):
  - `BuiltInToolExecutor` - Executes .NET methods directly
  - `BuiltInToolRegistry` - Manages built-in tool catalog
- **Composite**:
  - `ToolRegistryComposite` - Aggregates all tool sources

### Tool Routing Strategy

**Routing is metadata-driven**, not hardcoded:

```csharp
// Tool metadata includes SourceType
public interface ITool
{
    string Name { get; }
    ToolSourceType SourceType { get; }  // MCP, BuiltIn, etc.
    // ...
}

// Executors register themselves by SourceType
public interface IToolExecutor
{
    ToolSourceType SourceType { get; }
    Task<ToolExecutionResult> ExecuteAsync(...);
}

// ToolManager routes based on SourceType
public async Task<ToolExecutionResult> ExecuteToolCallAsync(LLMToolCall toolCall)
{
    var tool = _registry.GetTool(toolCall.Name);
    var executor = _executors.First(e => e.SourceType == tool.SourceType);
    return await executor.ExecuteAsync(tool, toolCall.Arguments);
}
```

### Tool Discovery and Registration

**Discovery** (at startup or on-demand):
1. Each tool source has a discovery component (e.g., `MCPToolDiscovery`)
2. Discovery connects to source and enumerates tools
3. Converts source-specific format to `ITool` instances
4. Registers tools with source-specific registry

**Registration**:
1. Source-specific registries (e.g., `MCPToolRegistry`) cache tools
2. `ToolRegistryComposite` aggregates all registries
3. Tools accessible via unified interface: `GetTool(name)`, `GetAllTools()`

**Example - MCP Discovery**:
```
1. Connect to MCP server (from config)
2. Send tools/list request (MCP protocol)
3. Parse tool definitions (JSON)
4. Create ITool instances:
   - Name: from MCP tool name
   - Description: from MCP tool description
   - ParametersSchema: from MCP tool schema
   - SourceType: MCP
   - Metadata: { "serverName": "todo-list", ... }
5. Register with MCPToolRegistry
```

### Integration with Agent Orchestrator

**Tool Calling Loop** (Phase 5 - Sequential):
```csharp
// In Agent Orchestrator
while (response.HasToolCalls && depth < MaxDepth)
{
    foreach (var toolCall in response.ToolCalls)
    {
        // Sequential execution
        var result = await _toolManager.ExecuteToolCallAsync(toolCall);
        conversation.Add(new ToolResultMessage(result));
    }

    depth++;
    response = await _llmProvider.SendRequestAsync(...);
}
```

**Future - Parallel Execution**:
```csharp
// When refactored to parallel
var tasks = response.ToolCalls.Select(tc =>
    _toolManager.ExecuteToolCallAsync(tc));
var results = await Task.WhenAll(tasks);
```

### Error Handling

**Timeout Protection**:
- Default: 180 seconds per tool execution
- Configurable in `MCPConfiguration.ToolExecutionTimeoutSeconds`
- Cancellation token passed to executors
- Timeout logged to Transparency System

**Depth Limit**:
- Default: 10 levels max
- Configurable in `MCPConfiguration.MaxToolCallDepth`
- Prevents infinite tool call loops
- Exceeded limit logged and error returned

**Error Scenarios**:
1. Tool not found → Return error to LLM
2. Executor not found → Return error to LLM
3. Execution timeout → Cancel execution, return timeout error
4. Execution exception → Catch, log, return error
5. Invalid arguments → Validation error to LLM
6. MCP server crash → Connection error, graceful degradation

All errors logged to Transparency System.

### Transparency Integration

**Events Logged**:
- Tool discovery (started, completed, failed)
- Tool registration (per tool)
- Tool call started (tool name, source type, arguments)
- Tool call completed (result, execution time)
- Tool call failed (error message, execution time)
- Tool call timeout
- MCP server lifecycle (connecting, connected, disconnected, failed)

**Event Format** (example):
```json
{
  "eventType": "ToolCallStarted",
  "timestamp": "2025-11-01T10:00:05Z",
  "data": {
    "toolName": "add_todo",
    "sourceType": "MCP",
    "arguments": { "task": "Review Phase 5 plan" },
    "metadata": { "serverName": "todo-list" }
  }
}
```

## Deployment Architecture

### Blazor Hosting Model Options

**Option 1: Blazor Server** (RECOMMENDED for local deployment)
- **How it works**: UI runs on server, DOM updates via SignalR
- **Pros**:
  - Smaller client payload
  - Full .NET runtime on server
  - Easy access to server resources
  - Perfect for local deployment
  - Real-time updates natural (already using SignalR)
- **Cons**:
  - Requires persistent connection
  - Server-side state management
  - Network latency affects UI

**Option 2: Blazor WebAssembly**
- **How it works**: Entire app runs in browser via WebAssembly
- **Pros**:
  - No server connection needed after load
  - Works offline
  - Client-side execution
- **Cons**:
  - Larger download size (includes .NET runtime)
  - Cannot directly access MCP servers (would need backend API)
  - More complex architecture for your use case

**Option 3: Blazor Hybrid** (Desktop app)
- **How it works**: Native desktop app with embedded web view
- **Pros**:
  - Desktop application
  - Full .NET capabilities
  - Native OS integration
- **Cons**:
  - Platform-specific packaging
  - Not web-based

**Decision: Start with Blazor Server** for these reasons:
- Local deployment requirement
- Real-time streaming is core requirement (SignalR built-in)
- Direct access to MCP servers from backend
- Simpler architecture
- Can migrate to WebAssembly + API later if needed

### Deployment Configuration

```
Local Machine:
┌─────────────────────────────────┐
│                                  │
│  Blazor Server App (Port 5000)  │
│     │                            │
│     ├─→ TransparentAiAgentCore  │
│     │                            │
│     ├─→ MCP Servers (external)  │
│     │   - todo-list             │
│     │   - context7              │
│     │   - others...             │
│     │                            │
│     └─→ LLM Providers (HTTP)    │
│         - Azure OpenAI           │
│         - Anthropic              │
│                                  │
└─────────────────────────────────┘
```

## Key Design Patterns

### 1. Strategy Pattern
- **Where**: LLM Provider selection
- **Why**: Easy to switch between providers
- **Implementation**: `ILLMProvider` interface with multiple implementations

### 2. Observer Pattern
- **Where**: Transparency System
- **Why**: Multiple subscribers to transparency events
- **Implementation**: Event-based transparency logging

### 3. Pipeline Pattern
- **Where**: Message processing
- **Why**: Flexible message transformation and validation
- **Implementation**: Message Pipeline with composable stages

### 4. Repository Pattern (Future)
- **Where**: Conversation persistence (future feature)
- **Why**: Abstract data storage
- **Implementation**: TBD when needed

### 5. Factory Pattern
- **Where**: LLM Provider creation, Tool creation
- **Why**: Complex object initialization
- **Implementation**: Provider factory based on configuration

## Cross-Cutting Concerns

### Streaming Strategy

**Challenge**: Streaming LLM responses to UI (known pain point)

**Solution**:
```
LLM Provider (IAsyncEnumerable<T>)
   │
   ▼
Streaming Handler (buffers, formats)
   │
   ├─→ Conversation Manager (aggregates full response)
   ├─→ Transparency System (logs chunks)
   └─→ SSE Stream to UI
```

- Use `IAsyncEnumerable<T>` for streaming from LLM
- Server-Sent Events (SSE) for UI updates
- Buffering strategy for efficient network usage

### Authentication Strategy

**Challenge**: Different auth methods for different services

**Solution**: `IAuthenticationProvider` interface
```csharp
interface IAuthenticationProvider
{
    Task<AuthCredentials> GetCredentialsAsync(string serviceId);
}
```

Implementations:
- `ApiKeyAuthProvider`: For OpenAI, Anthropic
- `AzureAuthProvider`: For Azure OpenAI (key + endpoint)
- `MCPAuthProvider`: For MCP servers (varies by server)

### Configuration Strategy

**Layered Configuration**:
1. Default configuration (embedded)
2. File-based configuration (`appsettings.json`, `agent-config.json`)
3. UI-based overrides (runtime)
4. Environment variables

**Configuration Sections**:
- LLM providers (endpoints, keys, parameters)
- MCP servers (connection info)
- Agent behavior (system prompt, tools)
- UI preferences

## Error Handling Strategy

**Principles**:
- Fail gracefully
- Log all errors to transparency system
- User-friendly error messages in UI
- Retry logic for transient failures

**Exception Hierarchy**:
```
AgentException (base)
├─ LLMException
│  ├─ StreamingException
│  └─ ProviderException
├─ MCPException
│  ├─ ToolNotFoundException
│  └─ ToolExecutionException
└─ ConfigurationException
```

## Testing Strategy

**TDD Approach** (MSTest):

1. **Unit Tests** (Priority):
   - Each component tested in isolation
   - Mock external dependencies
   - Test interfaces, not implementations

2. **Integration Tests** (Future):
   - Test component interactions
   - Use test doubles for LLM/MCP

3. **End-to-End Tests** (Future):
   - Full conversation flows
   - UI testing with Playwright/bUnit

## Scalability Considerations

**Current Scope**: Single user, local deployment

**Future Considerations**:
- Multi-user support
- Conversation persistence
- Cloud deployment
- Distributed MCP servers

## Technology Stack Summary

- **Framework**: .NET 8.0
- **UI**: Blazor Server
- **Testing**: MSTest
- **DI**: Built-in .NET DI
- **Configuration**: `Microsoft.Extensions.Configuration`
- **Logging**: `Microsoft.Extensions.Logging` (+ Transparency System)
- **HTTP**: `HttpClient` (for LLM providers)
- **Streaming**: `IAsyncEnumerable`, Server-Sent Events

## Next Steps

1. Review architecture with user
2. Update DESIGN_DECISIONS.md with choices made
3. Create component hierarchy in docs
4. Begin implementation planning (which component first?)

---

**Last Updated**: 2025-11-01 (Updated for Phase 5 - Tool Integration)
