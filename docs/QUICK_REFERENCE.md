# Quick Reference - TransparentAiAgent

Quick reference for key decisions and starting implementation.

## Core Principles

1. **Infrastructure Transparency First** - Context, tools, config visibility
2. **LLM Agnostic** - Provider abstraction supports multiple LLMs
3. **MCP-Only Tools** - No built-in functions, everything via MCP
4. **Lean TDD Approach** - Test meaningful behavior, not compiler features
5. **Clean Architecture** - Clear layer separation

## Technology Stack

- **.NET 8.0** - Framework
- **C#** - Language
- **Blazor Server** - UI (SignalR for real-time)
- **MSTest** - Testing framework
- **Azure OpenAI** - First LLM provider
- **C# MCP SDK** - MCP client library

## Architecture Layers

```
Presentation → Application → Domain → Infrastructure
   (UI)      (Orchestrator)  (Models)  (LLM/MCP/Config)
```

**Dependency Rule**: Dependencies point inward only.

## Key Components by Layer

### Domain (Core Abstractions)
- `ILLMProvider` - LLM provider interface
- `ITool` - Tool interface
- `IMessage` - Message models with context status
- Exception hierarchy

### Infrastructure
- `AzureOpenAIProvider : ILLMProvider`
- `MCPClient` (using C# MCP SDK)
- `ConfigurationManager`
- `AuthenticationManager`
- `TransparencySystem`

### Application
- `AgentOrchestrator` - Main coordination
- `ConversationManager` - Context + history
- `MessagePipeline` - Message processing
- `ToolExecutor` - Execute MCP tools

### Presentation
- Blazor Server components
- Chat component with context status
- Transparency viewer
- Configuration editor

## Context Management

**Critical Feature**:
- Full chat history (all messages)
- Message status: `InContext` | `TruncatedFromContext`
- Simple truncation: Remove oldest when limit reached
- Visual indicators in UI
- User always knows what LLM sees

## Streaming Architecture

```
LLM Provider (IAsyncEnumerable<T>)
    ↓
Streaming Handler (buffer, format)
    ↓
├─→ Conversation Manager (aggregate)
├─→ Transparency System (log)
└─→ SSE to UI (real-time)
```

## Transparency Events

**Infrastructure Events (Primary)**:
- Tool calls + results
- Context changes (truncation)
- Configuration changes
- LLM requests/responses
- System state changes
- Errors/warnings

**LLM Reasoning (Secondary - Later Phase)**:
- Chain-of-thought
- Decision reasoning

## Implementation Phases

1. **Foundation** - Config, Models, Exceptions, Transparency base
2. **LLM Integration** - Azure OpenAI, Streaming, Auth
3. **Agent Core** - Orchestrator, Conversation, Pipeline
4. **Basic UI** - Blazor setup, Chat, Context display ← Early testing
5. **MCP Integration** - C# SDK, Tool Registry, Executor
6. **Enhanced UI** - SSE streaming, Transparency viewer, Tools view
7. **Configuration UI** - Edit config from UI
8. **Anthropic Provider** - Second provider, validate abstraction
9. **Polish** - Refinement, docs, optimization

## Phase 1: Foundation (Start Here)

### Components to Implement (TDD)

1. **Domain Models**:
   ```csharp
   // Message with context status
   interface IMessage { }
   class UserMessage : IMessage { }
   class AssistantMessage : IMessage { }
   class AssistantToolCallMessage : AssistantMessage { }  // Phase 5 Refactoring: Derived class for tool calls
   class ToolResultMessage : IMessage { }

   // Phase 5: Supporting classes for tool calls
   class ToolCall { }  // Value object for single tool call (Id, Name, Arguments)

   enum MessageContextStatus {
       InContext,
       TruncatedFromContext
   }
   ```

2. **Exception Hierarchy**:
   ```csharp
   class AgentException : Exception { }
   class LLMException : AgentException { }
   class MCPException : AgentException { }
   class ConfigurationException : AgentException { }
   ```

3. **Configuration Manager**:
   - Load from `appsettings.json` + `agent-config.json`
   - Layered configuration
   - Configuration models

4. **Transparency System (Basic)**:
   - Event model
   - Event logger
   - In-memory event store

### Lean TDD Process

For each component:
1. Write test for **meaningful behavior** (validation, defaults, transformations)
2. Implement minimum code to pass
3. Refactor
4. Repeat

**What to Test** ✅:
- Validation logic (throws exceptions)
- Constructor initialization (ID generation, defaults)
- Transformations (formatting, calculations)
- Service behavior (add, retrieve, filter)

**What NOT to Test** ❌:
- Enum values exist
- Simple property getters/setters
- Trivial parameter-to-property assignment

### Tests First! (Lean Examples)

```csharp
[TestClass]
public class MessageTests
{
    [TestMethod]
    public void UserMessage_NullContent_ThrowsException() // ✅ GOOD - validation
    {
        Assert.ThrowsException<ArgumentException>(() => new UserMessage(null));
    }

    [TestMethod]
    public void UserMessage_GeneratesUniqueId() // ✅ GOOD - initialization behavior
    {
        var msg1 = new UserMessage("Hi");
        var msg2 = new UserMessage("Hello");
        Assert.AreNotEqual(msg1.Id, msg2.Id);
    }

    [TestMethod]
    public void UserMessage_DefaultsToInContext() // ✅ GOOD - business rule
    {
        var message = new UserMessage("Hello");
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    // ❌ BAD - Don't test trivial property access:
    // [TestMethod]
    // public void UserMessage_Role_IsUser() { ... } // Waste of time
}
```

## Key Interfaces

```csharp
// LLM Provider
interface ILLMProvider
{
    IAsyncEnumerable<StreamChunk> StreamCompletionAsync(
        IEnumerable<IMessage> messages,
        LLMParameters parameters,
        CancellationToken ct);
}

// Tool
interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonSchema InputSchema { get; }
    Task<ToolResult> ExecuteAsync(JsonObject parameters);
}

// Transparency
interface ITransparencyService
{
    void LogEvent(TransparencyEvent evt);
    IEnumerable<TransparencyEvent> GetEvents();
}
```

## Configuration Structure

```json
{
  "Agent": {
    "SystemPrompt": "You are a helpful assistant...",
    "ContextWindowSize": 20
  },
  "LLM": {
    "Provider": "AzureOpenAI",
    "Temperature": 0.7,
    "TopP": 1.0,
    "AzureOpenAI": {
      "Endpoint": "https://...",
      "ApiKey": "***",
      "DeploymentName": "gpt-4"
    }
  },
  "MCP": {
    "Servers": [
      {
        "Name": "todo-list",
        "Command": "C:\\path\\to\\server.exe",
        "Args": []
      }
    ]
  }
}
```

## Testing Strategy

- **Unit Tests**: Test each component in isolation with mocks
- **Integration Tests**: Test component interactions (later)
- **Manual Testing**: UI testing initially
- **E2E Tests**: Full flows (later)

## Git Workflow

Commits should be:
- **Per component**: One component per commit
- **Test + implementation**: Include tests in same commit
- **Clear messages**: "Add UserMessage model with context status"

## Commands to Remember

```bash
# Run tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~MessageTests"

# Build solution
dotnet build

# Run Blazor app
dotnet run --project TransparentAiAgentGui
```

## Blazor Server Notes

- **SignalR**: Built-in for real-time
- **State**: Server-side state management
- **SSE**: Can use SignalR or dedicated SSE endpoint
- **Port**: Default 5000 (HTTP) / 5001 (HTTPS)

## MCP SDK Notes

- **Library**: C# MCP SDK (specific version TBD in Phase 5)
- **Transport**: stdio primary
- **User has**: Server-side experience (learning client side)
- **Test with**: todo-list, context7 servers

## Common Patterns

### Strategy Pattern
```csharp
// For LLM providers
interface ILLMProvider { ... }
class AzureOpenAIProvider : ILLMProvider { ... }
class AnthropicProvider : ILLMProvider { ... }
```

### Observer Pattern
```csharp
// For transparency events
class TransparencySystem {
    event EventHandler<TransparencyEvent> EventLogged;
}
```

### Factory Pattern
```csharp
// For provider creation
class LLMProviderFactory {
    ILLMProvider Create(LLMConfiguration config) { ... }
}
```

## Authentication

Different providers need different auth:
- **Azure OpenAI**: API key + endpoint + deployment
- **Anthropic**: API key only
- **MCP**: Varies by server

Use `IAuthenticationProvider` abstraction.

## Streaming Considerations

**Key challenges** (known pain point):
- Multiple consumers of stream
- Error handling mid-stream
- Buffering strategy
- UI responsiveness

**Solution**:
- `IAsyncEnumerable<T>` from provider
- Streaming handler with buffering
- SSE to UI via SignalR
- Careful cancellation token handling

## Visual Indicators (UI)

Context status badges:
- ✅ **In Context** - Green badge
- ⚠️ **Not in Context** - Gray badge, faded text

Context usage:
- "15 / 20 messages in context"
- Progress bar or similar

Tool calls:
- JSON formatted
- Syntax highlighting
- Expandable/collapsible

## Document References

- **REQUIREMENTS.md** - What we're building
- **ARCHITECTURE.md** - How it's structured
- **COMPONENTS.md** - Component details
- **DESIGN_DECISIONS.md** - Why we made choices
- **IMPLEMENTATION_ROADMAP.md** - What to build when
- **SESSION_SUMMARY.md** - What we did today

## Ready to Code?

Start with Phase 1:
1. Set up MSTest project structure
2. Write first test for `UserMessage` model
3. Implement `UserMessage` to pass test
4. Continue with TDD for all domain models
5. Move to exceptions, then config, then transparency

**Remember**: Tests first, implementation second!

---

**Last Updated**: 2025-10-28
