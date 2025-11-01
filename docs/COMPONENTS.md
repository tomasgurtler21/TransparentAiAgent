# Component Overview

This document provides a high-level overview of all major components in the TransparentAiAgent system.

## Component Organization

Components are organized into logical layers following Clean Architecture principles:

```
┌─────────────────────────────────────────────┐
│         UI/Presentation Layer               │
│  (Blazor Components, State Management)      │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│           Application Layer                  │
│  (Agent Orchestrator, Message Pipeline)     │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│            Domain Layer                      │
│  (Conversation, Tools, Transparency)        │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         Infrastructure Layer                 │
│  (LLM Providers, MCP Client, Config, Auth)  │
└─────────────────────────────────────────────┘
```

## Component Categories

### 1. Core Domain Components
Core business logic and domain models.

- **Agent Orchestrator** → [components/core/AgentOrchestrator.md](components/core/AgentOrchestrator.md)
- **Conversation Manager** → [components/core/ConversationManager.md](components/core/ConversationManager.md)
- **Message Pipeline** → [components/core/MessagePipeline.md](components/core/MessagePipeline.md)

### 2. LLM Integration Layer
Abstractions and implementations for LLM providers.

- **LLM Provider Abstraction** → [components/llm/ProviderAbstraction.md](components/llm/ProviderAbstraction.md)
- **Azure OpenAI Provider** → [components/llm/AzureOpenAIProvider.md](components/llm/AzureOpenAIProvider.md)
- **Anthropic Provider** → [components/llm/AnthropicProvider.md](components/llm/AnthropicProvider.md)
- **Streaming Handler** → [components/llm/StreamingHandler.md](components/llm/StreamingHandler.md)
- **Token Counter** → [components/llm/TokenCounter.md](components/llm/TokenCounter.md)

### 3. Tool Management Layer (NEW - Phase 5)
Source-agnostic tool system with routing and execution.

**Domain Abstractions**:
- **ITool Interface** → Tool definition abstraction (name, description, schema, source type)
- **IToolExecutor Interface** → Tool execution abstraction (executes tools for specific source type)
- **IToolRegistry Interface** → Tool discovery and management abstraction
- **IToolManager Interface** → High-level tool orchestration and routing

**Application Layer**:
- **Tool Manager** → [components/tools/ToolManager.md](components/tools/ToolManager.md)
  - Routes tool calls by source type
  - Orchestrates tool execution
  - Integrates with Transparency System

**Infrastructure Layer - MCP Implementation**:
- **MCP Client Wrapper** → [components/tools/mcp/MCPClientWrapper.md](components/tools/mcp/MCPClientWrapper.md)
  - Wraps C# MCP SDK
  - Manages MCP server connections
- **MCP Tool Discovery** → [components/tools/mcp/MCPToolDiscovery.md](components/tools/mcp/MCPToolDiscovery.md)
  - Discovers tools from MCP servers
  - Converts to ITool format
- **MCP Tool Executor** → [components/tools/mcp/MCPToolExecutor.md](components/tools/mcp/MCPToolExecutor.md)
  - Executes MCP tool calls
  - Implements IToolExecutor for SourceType.MCP
- **MCP Tool Registry** → [components/tools/mcp/MCPToolRegistry.md](components/tools/mcp/MCPToolRegistry.md)
  - Manages MCP tool catalog
  - Implements IToolRegistry

**Infrastructure Layer - Composite**:
- **Tool Registry Composite** → [components/tools/ToolRegistryComposite.md](components/tools/ToolRegistryComposite.md)
  - Aggregates all tool sources (MCP, built-in, future)
  - Implements Composite pattern

**Integration Points for Future Tool Sources**:
- **Built-In Tools** (Future) → See [TOOL_INTEGRATION_POINTS.md](TOOL_INTEGRATION_POINTS.md)
  - BuiltInToolExecutor (implements IToolExecutor for SourceType.BuiltIn)
  - BuiltInToolRegistry (implements IToolRegistry)
  - Examples: reset_conversation, export_conversation, get_system_info

### 4. Infrastructure Components
Cross-cutting infrastructure concerns.

- **Configuration Manager** → [components/infrastructure/ConfigurationManager.md](components/infrastructure/ConfigurationManager.md)
- **Authentication Manager** → [components/infrastructure/AuthenticationManager.md](components/infrastructure/AuthenticationManager.md)
- **Transparency System** → [components/infrastructure/TransparencySystem.md](components/infrastructure/TransparencySystem.md)
- **Serialization Service** → [components/infrastructure/SerializationService.md](components/infrastructure/SerializationService.md)

### 5. UI/Presentation Layer
Blazor components and UI logic.

- **Chat Component** → [components/ui/ChatComponent.md](components/ui/ChatComponent.md)
- **Configuration Component** → [components/ui/ConfigurationComponent.md](components/ui/ConfigurationComponent.md)
- **Tools Overview Component** → [components/ui/ToolsOverviewComponent.md](components/ui/ToolsOverviewComponent.md)
- **Transparency Viewer** → [components/ui/TransparencyViewer.md](components/ui/TransparencyViewer.md)
- **State Management** → [components/ui/StateManagement.md](components/ui/StateManagement.md)

## Component Interaction Flow

### Typical Request Flow

```
User Input → Chat Component
             ↓
          State Management
             ↓
       Agent Orchestrator
             ↓
       Conversation Manager (adds to context)
             ↓
       Message Pipeline (prepares request)
             ↓
       LLM Provider (streams response)
             ↓
       Streaming Handler
             ↓
       Transparency System (logs everything)
             ↓
       Chat Component (displays streaming response)
```

### Tool Call Flow (Updated for Phase 5)

```
LLM Response with Tool Call(s)
             ↓
       Agent Orchestrator (detects tool calls)
             ↓
       Tool Manager
             ↓
   ┌─────────┴──────────────────┐
   │  Routing Logic:            │
   │  1. Lookup tool by name    │
   │  2. Check SourceType       │
   │  3. Find executor          │
   │  4. Log to Transparency    │
   └─────────┬──────────────────┘
             ↓
    ┌────────┴─────────┐
    │                  │
    ▼                  ▼
MCP Tool Executor   Built-In Executor (future)
    │
    ├─→ MCP Client (calls MCP server)
    │
    ├─→ Tool Result
    │
    └─→ Transparency System (logs execution)
             ↓
       Agent Orchestrator
             ↓
       Conversation Manager (adds ToolResultMessage)
             ↓
       LLM Provider (continues with tool results)
             ↓
       LLM Response (final answer or more tool calls)
```

**Tool Call Loop** (Sequential in Phase 5):
- Agent checks for tool calls in LLM response
- If found: Execute tools → Add results → Call LLM again
- If not found: Conversation complete
- Maximum depth: 10 iterations (prevents infinite loops)

## Component Documentation

Each component has its own detailed documentation file (to be created as needed):
- Responsibilities
- Interfaces/Contracts
- Dependencies
- Implementation notes
- Testing strategy

**Note**: Component detail docs will be created during implementation phase. This overview provides the structure and relationships.

---

## Tool System Component Details (Phase 5)

### Key Interfaces

**ITool** - Tool definition abstraction:
- `string Name` - Unique tool name
- `string Description` - Human-readable description
- `string ParametersSchema` - JSON Schema for parameters
- `ToolSourceType SourceType` - Source type (MCP, BuiltIn, etc.)
- `IReadOnlyDictionary<string, string> Metadata` - Source-specific metadata

**IToolExecutor** - Execution abstraction:
- `ToolSourceType SourceType` - What source type this executor handles
- `Task<ToolExecutionResult> ExecuteAsync(ITool tool, string arguments, CancellationToken ct)`

**IToolRegistry** - Discovery and management:
- `IReadOnlyList<ITool> GetAllTools()` - Get all registered tools
- `ITool? GetTool(string toolName)` - Get tool by name
- `bool HasTool(string toolName)` - Check if tool exists
- `Task RefreshAsync(CancellationToken ct)` - Re-discover tools

**IToolManager** - High-level orchestration:
- `IToolRegistry Registry` - Access to tool registry
- `Task<ToolExecutionResult> ExecuteToolCallAsync(LLMToolCall toolCall, CancellationToken ct)` - Execute a tool call
- `List<LLMTool> GetLLMToolDefinitions()` - Get tools in LLM format

### Component Responsibilities

**ToolManager** (Application Layer):
- Routes tool calls to appropriate executor based on SourceType
- Coordinates between registry and executors
- Logs all tool operations to Transparency System
- Handles errors and timeouts
- Returns standardized ToolExecutionResult

**MCPClientWrapper** (Infrastructure):
- Thin wrapper around C# MCP SDK
- Manages MCP server connections (stdio transport)
- Handles server lifecycle (start, stop, health check)
- Provides clean interface to MCP operations

**MCPToolDiscovery** (Infrastructure):
- Connects to MCP servers from configuration
- Sends tools/list request
- Parses tool definitions
- Converts to ITool instances with SourceType=MCP
- Includes metadata (server name, etc.)

**MCPToolExecutor** (Infrastructure):
- Implements IToolExecutor for SourceType.MCP
- Validates tool is from MCP source
- Sends tools/call request to MCP server
- Handles timeout (180 seconds default)
- Parses and returns result

**MCPToolRegistry** (Infrastructure):
- Implements IToolRegistry for MCP tools
- Caches discovered tools
- Supports refresh on demand
- Tracks server connection status

**ToolRegistryComposite** (Infrastructure):
- Aggregates multiple IToolRegistry implementations
- Uses Composite pattern
- Provides unified view of all tools
- Routes queries to appropriate registry

---

**Status**: Component structure updated for Phase 5 - Tool Integration
**Last Updated**: 2025-11-01
