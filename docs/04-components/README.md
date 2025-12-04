# Component Overview

**Last Updated**: 2025-12-04
**Status**: Active - Complete Catalog

This document provides a high-level overview of all major components in the TransparentAiAgent system.

---

## Component Organization

Components are organized into logical layers following Clean Architecture principles:

```
┌─────────────────────────────────────────────┐
│         UI/Presentation Layer               │
│  (Blazor Server, UI Services, Components)   │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│           Application Layer                  │
│  (Agent Orchestrator, Conversation, Tools)  │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│            Domain Layer                      │
│  (Models, Interfaces, Business Rules)       │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         Infrastructure Layer                 │
│  (LLM Providers, MCP, Config, Auth, etc.)   │
└─────────────────────────────────────────────┘
```

---

## Component Categories

### 1. [Core Application Components](core/)
**Layer**: Application
**Files**: 3 component docs

Coordinate agent behavior and conversation flow.

- **[Agent Orchestrator](core/agent-orchestrator.md)** - Main entry point, tool loop coordination
- **[Conversation Manager](core/conversation-manager.md)** - Conversation history and context
- **[Message Pipeline](core/message-pipeline.md)** - Domain/LLM message transformation

**Key Feature**: Tool call loop with depth protection (max 10 iterations)

---

### 2. [LLM Integration Components](llm/)
**Layer**: Domain + Infrastructure
**Files**: 6 component docs

Provider-agnostic LLM abstraction with multiple implementations.

- **[Provider Abstraction](llm/provider-abstraction.md)** - ILLMProvider interface + domain models
- **[LLM Provider Manager](llm/llm-provider-manager.md)** - Dynamic provider switching and lifecycle management
- **[Anthropic Provider](llm/anthropic-provider.md)** - Claude API implementation
- **[Azure OpenAI Provider](llm/azure-openai-provider.md)** - Azure OpenAI with OAuth support
- **[OpenAI Provider](llm/openai-provider.md)** - OpenAI API implementation
- **[Streaming Utilities](llm/streaming.md)** - Markdown buffering for streaming

**Key Features**:
- Dynamic provider switching at runtime without restart
- Multiple providers: Anthropic Claude, Azure OpenAI, OpenAI
- Streaming and non-streaming support
- Tool calling abstraction
- Multiple auth modes (API key, OAuth)

---

### 3. [Tool System](tools/)
**Layer**: Domain + Application + Infrastructure
**Files**: 15+ component docs (MCP: 5, Built-in: 4, Scenario Mock: 1, Application: 2, Overview: 3+)

Source-agnostic tool system with MCP, built-in, and scenario mock implementations.

**Application Layer**:
- **[Tool Manager](tools/tool-manager.md)** - Routes tool calls, logs to transparency
- **[Tool Schema Validator](tools/tool-schema-validator.md)** - Validates tool arguments for safety

**MCP Tools** ([mcp/](tools/mcp/)):
- **[MCP Client Wrapper](tools/mcp/mcp-client-wrapper.md)** - MCP SDK wrapper
- **[MCP Tool Registry](tools/mcp/mcp-tool-registry.md)** - Tool caching
- **[MCP Tool Executor](tools/mcp/mcp-tool-executor.md)** - MCP execution
- **[MCP Tool Discovery](tools/mcp/mcp-tool-discovery.md)** - Tool discovery

**Built-in Tools** ([builtin/](tools/builtin/)):
- **[UI Control Tools](tools/builtin/ui-control-tools.md)** - Dynamic UI manipulation (7 tools)
- **[Knowledge Library Tool](tools/builtin/knowledge-library-tool.md)** - Access curated teaching concepts
- **[Long-Term Memory Tools](tools/builtin/long-term-memory-tools.md)** - Persistent agent memory across sessions

**Scenario Mock Tools** ([scenariomock/](tools/scenariomock/)):
- **[Scenario Mock Tools](tools/scenariomock/README.md)** - Temporary mock tools for teaching scenarios

**Key Features**:
- Composite pattern for multiple tool sources
- Tool routing by SourceType
- Schema validation before execution
- Teaching Mode enablement via UI control
- Deterministic teaching scenarios via mock tools

**Cross-Reference**: See `docs/03-concepts/teaching-mode/` for Teaching Mode concept

---

### 4. [Infrastructure Components](infrastructure/)
**Layer**: Infrastructure
**Files**: 6 component docs

Cross-cutting concerns and external integrations.

- **[Configuration Service](infrastructure/configuration-service.md)** - Config loading/saving
- **[Authentication](infrastructure/authentication.md)** - API credentials provider
- **[Transparency Service](infrastructure/transparency-service.md)** - Event logging
- **[Serialization Service](infrastructure/serialization-service.md)** - JSON handling
- **[Long-Term Memory Service](infrastructure/long-term-memory-service.md)** - Persistent agent memory across sessions

**Key Features**:
- Centralized configuration management
- Multiple auth modes
- Real-time transparency events
- Standardized JSON serialization
- Mode-aware persistent memory storage

---

### 5. [UI/Presentation Layer](ui/)
**Layer**: Presentation
**Files**: 3 docs (architecture + overview + old)

Blazor Server web interface with SignalR real-time communication.

- **[UI Architecture](ui/architecture.md)** - Complete UI architecture, services, state
- **[UI README](ui/README.md)** - Component categories and overview

**Services**:
- ConversationUIService - UI/Agent bridge
- UIControlService - Dynamic UI state management

**Component Categories**:
- Pages (Home, Configuration, Tools, Transparency)
- Chat Components (Input, List, Display, Filters)
- Tool Components (Overview, Card, Modal)
- Transparency Components (Viewer, Event Display)

**Key Feature**: Agent-driven UI control via UIControlService (Teaching Mode)

---

## Complete Component Catalog

### Domain Layer
- **Models**: IMessage (+ 5 implementations), LLMRequest, LLMResponse, ToolCall, etc.
- **Interfaces**: ILLMProvider, ITool, IToolRegistry, IToolExecutor, IToolManager, IUIControlService, IAuthenticationProvider
- **Configuration**: AppConfiguration hierarchy (Agent, LLM, MCP)
- **Enums**: MessageRole, ToolSourceType, TransparencyEventType

### Application Layer
- **Agent Orchestrator** (`core/agent-orchestrator.md`)
- **Conversation Manager** (`core/conversation-manager.md`)
- **Message Pipeline** (`core/message-pipeline.md`)
- **Tool Manager** (`tools/tool-manager.md`)

### Infrastructure Layer
- **LLM Providers** (`llm/anthropic-provider.md`, `llm/azure-openai-provider.md`)
- **MCP Tools** (`tools/mcp/*` - 4 components)
- **Built-in Tools** (`tools/builtin/*` - 2 components)
- **Configuration** (`infrastructure/configuration-service.md`)
- **Authentication** (`infrastructure/authentication.md`)
- **Transparency** (`infrastructure/transparency-service.md`)
- **Serialization** (`infrastructure/serialization-service.md`)

### Presentation Layer
- **UI Services** (ConversationUIService, UIControlService)
- **Blazor Components** (23 Razor components across 4 categories)
- **UI State** (UIState domain model)

---

## Component Interaction Flows

### User Message Processing

```
User Input → ChatInput (UI)
    ↓
ConversationUIService
    ↓
AgentOrchestrator.ProcessUserInputAsync()
    ├─ Add to ConversationManager
    ├─ Build LLMRequest via MessagePipeline
    ├─ Send to ILLMProvider
    ├─ Handle Response:
    │  ├─ If tool calls → ToolManager → Executors → Results
    │  └─ If content → Parse message
    ├─ Log to TransparencyService
    └─ Return AssistantMessage
    ↓
UI updates via SignalR
```

### Tool Execution

```
LLM Response with ToolCalls
    ↓
AgentOrchestrator detects tool calls
    ↓
ToolManager.ExecuteToolCallAsync()
    ├─ Get tool from Registry (composite)
    ├─ Match executor by SourceType
    ├─ Execute:
    │  ├─ MCPToolExecutor → MCP server
    │  └─ UIControlToolExecutor → UIControlService
    └─ Return ToolExecutionResult
    ↓
Add ToolResultMessage to conversation
    ↓
Continue conversation with results
```

---

## Statistics

**Total Documentation Files**: ~35+ component docs
**Layers**: 4 (Domain, Application, Infrastructure, Presentation)
**Component Groups**: 5 major categories
**Files Documented**: 120+ source files analyzed

---

## Navigation

**By Layer**:
- [Core (Application)](core/) - 3 docs
- [LLM](llm/) - 6 docs
- [Tools](tools/) - 15+ docs
- [Infrastructure](infrastructure/) - 6 docs
- [UI](ui/) - 3 docs

**By Feature**:
- [Conversation Flow](core/agent-orchestrator.md)
- [LLM Integration](llm/)
- [Tool Execution](tools/)
- [Teaching Mode](tools/builtin/ui-control-tools.md) + `docs/03-concepts/teaching-mode/`
- [Transparency](infrastructure/transparency-service.md)

---

## Related Documentation

- [Architecture Overview](../02-architecture/overview.md) - System design
- [Concepts](../03-concepts/) - Cross-cutting features
- [Guides](../05-guides/) - How-to documentation
- [Reference](../06-reference/) - API references

---

**Status**: Component catalog complete. All major components documented.
**Documentation Design**: Follows two-phase approach (components first, architecture last) to avoid circular dependency.
