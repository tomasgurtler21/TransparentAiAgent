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

### 3. MCP Integration Layer
Model Context Protocol client implementation.

- **MCP Client** → [components/mcp/MCPClient.md](components/mcp/MCPClient.md)
- **Tool Registry** → [components/mcp/ToolRegistry.md](components/mcp/ToolRegistry.md)
- **Tool Executor** → [components/mcp/ToolExecutor.md](components/mcp/ToolExecutor.md)

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

### Tool Call Flow

```
LLM Response with Tool Call
             ↓
       Tool Executor
             ↓
       MCP Client (calls external tool)
             ↓
       Transparency System (logs tool call & result)
             ↓
       Message Pipeline (adds result to context)
             ↓
       LLM Provider (continues conversation)
```

## Component Documentation

Each component has its own detailed documentation file (to be created as needed):
- Responsibilities
- Interfaces/Contracts
- Dependencies
- Implementation notes
- Testing strategy

**Note**: Component detail docs will be created during implementation phase. This overview provides the structure and relationships.

---

**Status**: Component structure defined - ready for architecture design
**Last Updated**: 2025-10-28
