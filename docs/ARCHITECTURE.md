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

### System Layers

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
│  └──────────────────────────────────────────────────┘│
│                         │                             │
│       ┌─────────────────┼─────────────────┐          │
│       │                 │                 │          │
│  ┌────▼─────┐  ┌────────▼──────┐  ┌──────▼──────┐  │
│  │ Message  │  │ Conversation  │  │ Transparency│  │
│  │ Pipeline │  │ Manager       │  │ Service     │  │
│  └──────────┘  └───────────────┘  └─────────────┘  │
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
└────────────────────────┬──────────────────────────────┘
                         │
┌────────────────────────▼──────────────────────────────┐
│           Infrastructure Layer                         │
│      (External integrations & implementations)        │
│                                                        │
│  ┌──────────────────┐        ┌──────────────────┐    │
│  │  LLM Providers   │        │   MCP Client     │    │
│  │  - AzureOpenAI   │        │   - Tool Registry│    │
│  │  - Anthropic     │        │   - Tool Executor│    │
│  │  - Streaming     │        │                  │    │
│  └──────────────────┘        └──────────────────┘    │
│                                                        │
│  ┌──────────────────┐        ┌──────────────────┐    │
│  │  Configuration   │        │  Authentication  │    │
│  │  Manager         │        │  Manager         │    │
│  └──────────────────┘        └──────────────────┘    │
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

**Last Updated**: 2025-10-28
