# Design Decisions Log

This document tracks all major design decisions made during the development of TransparentAiAgent.

## Format

Each decision follows this structure:
- **Decision**: What was decided
- **Context**: Why this decision was needed
- **Options Considered**: Alternative approaches
- **Rationale**: Why this option was chosen
- **Consequences**: Implications of this decision
- **Date**: When the decision was made

---

## Decision Log

### DD-001: Use Blazor for GUI

**Date**: 2025-10-28

**Context**: Need to choose a GUI framework for the agent interface

**Decision**: Use Blazor for the GUI layer

**Options Considered**:
1. Blazor (WebAssembly or Server)
2. WPF
3. MAUI
4. Console-based interface

**Rationale**:
- User preference for Blazor
- Modern web-based UI with good tooling
- Can run as web app or desktop (with Blazor Hybrid)
- Good for real-time updates (SignalR integration)

**Consequences**:
- Requires web hosting (Blazor Server) or WebAssembly runtime
- Browser-based constraints
- Need to handle state synchronization

---

### DD-002: Target .NET 8.0

**Date**: 2025-10-28

**Context**: Need to choose .NET version

**Decision**: Use .NET 8.0 (LTS)

**Rationale**:
- Long-term support
- Latest stable features
- Good performance
- Wide adoption

**Consequences**:
- Users need .NET 8.0 runtime
- Can use latest C# 12 features

---

### DD-003: Follow TDD Approach

**Date**: 2025-10-28

**Context**: Development methodology

**Decision**: Use Test-Driven Development (TDD)

**Rationale**:
- User preference
- Better code quality
- Living documentation
- Easier refactoring

**Consequences**:
- Tests must be written before implementation
- Slower initial development but faster overall
- Better test coverage

---

---

### DD-004: Use Blazor Server for Hosting

**Date**: 2025-10-28

**Context**: Need to choose Blazor hosting model for local deployment

**Decision**: Use Blazor Server

**Options Considered**:
1. Blazor Server
2. Blazor WebAssembly
3. Blazor Hybrid (desktop app)

**Rationale**:
- Local deployment requirement fits Blazor Server model
- Real-time streaming is core requirement - SignalR built-in with Blazor Server
- Direct access to MCP servers from backend (no need for separate API layer)
- Simpler architecture for initial version
- Smaller client payload
- Can migrate to WebAssembly + API later if needed

**Consequences**:
- Requires persistent SignalR connection
- Server-side state management
- Minor network latency for UI updates (acceptable for local)
- Cannot work offline

---

### DD-005: LLM Provider Priority

**Date**: 2025-10-28

**Context**: Which LLM providers to support initially

**Decision**: Start with Azure OpenAI and Anthropic Claude

**Rationale**:
- Azure OpenAI: Unlimited access on user's other station
- Anthropic: Available on current station (limited)
- User has access credentials for both
- Covers major use cases
- Other providers can be added later

**Consequences**:
- Need to implement provider abstraction carefully
- Two different authentication mechanisms needed
- Different API formats to handle

---

### DD-006: MCP-Based Tool System (No Built-in Functions)

**Date**: 2025-10-28

**Context**: How to provide agent capabilities (tools/functions)

**Decision**: Use MCP protocol exclusively, NO built-in functions

**Rationale**:
- User explicitly dislikes built-in functions (non-modular)
- MCP provides standardized, extensible tool interface
- External tools via MCP servers
- Clean separation between agent core and capabilities
- MCP worked well in user's previous agent

**Consequences**:
- All capabilities must be provided by MCP servers
- Need robust MCP client implementation
- Dependency on external MCP servers
- More flexible and modular system

---

### DD-007: Configuration System Design

**Date**: 2025-10-28

**Context**: How to manage configuration

**Decision**: Layered configuration (files + UI)

**Options Considered**:
1. File-based only
2. UI-based only
3. Code-based configuration
4. Layered approach (files + UI)

**Rationale**:
- File-based allows easy editing and version control
- UI-based allows runtime changes without restart
- Layered approach provides flexibility
- Most config should be visible in UI

**Consequences**:
- Need configuration merge logic
- UI must reflect current configuration
- Need hot-reload support
- Increased complexity vs single config source

**Configuration Parameters (Initial)**:
- System prompts
- Available tools
- LLM parameters (temperature, top-p)

---

### DD-008: Transparency Focus - Infrastructure First

**Date**: 2025-10-28

**Context**: What to make transparent to users

**Decision**: **Primary focus on infrastructure transparency. LLM reasoning transparency is secondary (later phase).**

**Rationale**:
- Infrastructure transparency is the CORE VALUE of this project
- Tool calls, context management, config are infrastructure-level
- LLM reasoning chains are provider-specific and hard to control in LLM-agnostic way
- Can add reasoning transparency later via system prompts if needed

**Primary Transparency (Initial Implementation)**:

1. **Context Management**:
   - What messages are in LLM context right now
   - Context window size and current usage
   - Which messages have been truncated (but still visible in chat)
   - Clear visual indicators for context status

2. **Tool System**:
   - Available tools with schemas
   - Tool calls in real-time
   - Tool parameters (JSON)
   - Tool results
   - Tool execution time/status

3. **Configuration**:
   - System prompt
   - LLM parameters (temperature, top-p)
   - Active provider
   - All settings

4. **System State**:
   - Current agent activity
   - Request/response flow
   - LLM output streaming
   - Errors and warnings

**Secondary Transparency (Later Phase)**:
- LLM internal reasoning chains
- Chain-of-thought
- Decision-making transparency

**Consequences**:
- Transparency system logs ALL infrastructure events
- NOT attempting to control LLM's internal reasoning initially
- Focus on what we can control (infrastructure)
- Clear separation: infrastructure (now) vs LLM reasoning (later)
- Must implement context status tracking from the start

---

### DD-009: Streaming Architecture

**Date**: 2025-10-28

**Context**: How to handle LLM streaming (known pain point)

**Decision**: `IAsyncEnumerable<T>` from providers + SSE to UI

**Rationale**:
- `IAsyncEnumerable<T>` is .NET standard for streaming
- Server-Sent Events (SSE) natural for browser streaming
- Blazor Server already uses SignalR (can use for SSE)
- Separates concerns: provider streaming vs UI streaming

**Implementation Strategy**:
```
LLM Provider → IAsyncEnumerable<T> → Streaming Handler
  ├─→ Conversation Manager (aggregate full response)
  ├─→ Transparency System (log chunks)
  └─→ SSE Stream to UI (real-time display)
```

**Consequences**:
- Need careful buffer management
- Multiple consumers of stream (fan-out pattern)
- Complexity in error handling mid-stream
- Need thorough testing of streaming path

---

### DD-010: Authentication System

**Date**: 2025-10-28

**Context**: Handle different auth mechanisms (known pain point)

**Decision**: `IAuthenticationProvider` abstraction with multiple implementations

**Rationale**:
- Different services need different auth (API keys, Azure-specific, MCP-specific)
- Strategy pattern allows flexible auth
- Easy to add new auth types

**Implementations Needed**:
- `ApiKeyAuthProvider`: Simple API key (Anthropic, OpenAI)
- `AzureAuthProvider`: Azure-specific (key + endpoint + resource)
- `MCPAuthProvider`: Per-MCP-server auth (varies)

**Consequences**:
- Need secure credential storage
- Configuration must support various auth types
- Credential validation logic per provider

---

### DD-011: Clean Architecture with Layered Design

**Date**: 2025-10-28

**Context**: Overall architectural style

**Decision**: Clean Architecture principles with clear layers

**Layers**:
1. Presentation (Blazor UI)
2. Application (Agent Orchestrator, services)
3. Domain (Interfaces, core abstractions)
4. Infrastructure (LLM providers, MCP client, config, auth)

**Rationale**:
- Clear separation of concerns
- Dependency rule: dependencies point inward
- Domain has no external dependencies
- Easy to test (mock infrastructure)
- Maintainable and extensible
- Aligns with SOLID principles

**Consequences**:
- More upfront design effort
- More interfaces and abstractions
- Clearer boundaries between components
- Easier to test and maintain long-term

---

### DD-012: Design Patterns

**Date**: 2025-10-28

**Context**: Which design patterns to use

**Decisions**:
- **Strategy Pattern**: LLM provider selection
- **Observer Pattern**: Transparency event system
- **Pipeline Pattern**: Message processing
- **Factory Pattern**: Provider and tool creation
- **Repository Pattern**: (Future) Conversation persistence

**Rationale**:
- Strategy: Need interchangeable providers
- Observer: Multiple transparency event subscribers
- Pipeline: Flexible message transformation
- Factory: Complex initialization logic
- Repository: Abstract data persistence (when needed)

**Consequences**:
- More interfaces and abstractions
- Clear patterns make code easier to understand
- Facilitates testing with mocks/stubs

---

### DD-013: Error Handling Strategy

**Date**: 2025-10-28

**Context**: How to handle errors consistently

**Decision**: Custom exception hierarchy + fail gracefully

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

**Principles**:
- Fail gracefully
- Log all errors to Transparency System
- User-friendly error messages in UI
- Retry logic for transient failures

**Consequences**:
- Need error recovery logic
- Errors visible in transparency view
- Retry logic adds complexity
- Better user experience

---

### DD-014: TDD with MSTest

**Date**: 2025-10-28

**Context**: Testing approach and framework

**Decision**: Test-Driven Development (TDD) using MSTest

**Rationale**:
- User preference for TDD approach
- User familiar with MSTest
- Better code quality from TDD
- Tests as living documentation

**Test Priorities**:
1. Unit tests (HIGH PRIORITY)
2. Integration tests (future)
3. End-to-end tests (future)

**Consequences**:
- Tests written before implementation
- Slower initial development
- Better test coverage
- Easier refactoring with safety net
- Forces thinking about interfaces and testability

---

---

### DD-015: Azure OpenAI First

**Date**: 2025-10-28

**Context**: Which LLM provider to implement first

**Decision**: Start with Azure OpenAI

**Rationale**:
- Unlimited access on user's other station
- Need to conserve Anthropic limits for development phase
- Will require separated development and functional testing stages

**Consequences**:
- Development done with one provider
- Functional testing on different station
- Anthropic provider added in Phase 8
- Must ensure abstraction works without having two providers to test against

---

### DD-016: Use C# MCP SDK Library

**Date**: 2025-10-28

**Context**: Build MCP client from scratch or use existing library

**Decision**: Use existing C# MCP SDK library

**Rationale**:
- User has experience with the SDK (server-side)
- SDK is well documented
- Faster implementation
- Standard compliance guaranteed
- No need to reinvent the wheel

**Consequences**:
- Dependency on external library
- Need to learn client-side SDK (user familiar with server-side)
- Faster Phase 4 implementation
- Standard MCP compliance

**Action Item**: Document which specific C# MCP SDK library during Phase 4 planning

---

### DD-017: Context Management - Simple Truncation with Visual Indicators

**Date**: 2025-10-28

**Context**: How to handle context window limits

**Decision**: Simple truncation strategy with full transparency

**Strategy**:
1. When context limit reached, remove oldest messages
2. Truncated messages remain visible in chat history
3. Clear visual indicator: "Not in context" or similar
4. User always sees full chat history
5. User always knows what LLM can see

**Rationale**:
- Simpler than summarization
- Can add summarization later if needed
- Transparency is key: user must know what's in context
- Chat history ≠ LLM context (important distinction)

**UI Requirements**:
- Visual badge/indicator on truncated messages
- Context window usage visible (e.g., "15/20 messages in context")
- Clear boundary between "in context" and "out of context"

**Consequences**:
- Need message state tracking: `InContext` vs `TruncatedFromContext`
- UI must handle both states
- Transparency System must log truncation events
- Future: Can add summarization as enhancement

---

### DD-018: Implementation Order - Basic UI Before MCP

**Date**: 2025-10-28

**Context**: Implementation phase ordering

**Decision**: Move Basic UI (Phase 5) before MCP Integration (Phase 4)

**New Order**:
1. Foundation
2. LLM Integration
3. Agent Core
4. **Basic UI** (moved up)
5. **MCP Integration** (moved down)
6. Enhanced UI & Transparency
7. Configuration UI
8. Second LLM Provider
9. Polish & Refinement

**Rationale**:
- Enables end-to-end functional testing earlier
- Can test agent with Azure OpenAI before adding MCP complexity
- Validates architecture with real UI feedback
- User can interact with agent earlier in development
- MCP tools are enhancement, not core requirement for basic agent

**Consequences**:
- Basic agent functional sooner
- Can test LLM interaction through real UI
- MCP added as enhancement after basic loop works
- May need to refactor UI when MCP added (acceptable tradeoff)

---

## Pending Decisions

The following decisions will be made during implementation:

- [ ] Specific C# MCP SDK library name and version
- [ ] Specific JSON libraries for serialization
- [ ] Logging framework details (though using Microsoft.Extensions.Logging)
- [ ] State persistence format (when implemented)
- [ ] Exact MCP transport mechanisms (stdio vs SSE vs both)
- [ ] Context limit threshold (number of messages or tokens)
- [ ] Specific HTTP client policies (retry, timeout, etc.)
- [ ] UI component library (if any, beyond base Blazor)
- [ ] Background job processing (if needed for MCP connections)

---

**Last Updated**: 2025-10-28
