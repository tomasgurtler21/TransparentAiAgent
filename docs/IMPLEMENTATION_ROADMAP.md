# Implementation Roadmap

This document outlines the proposed implementation order for the TransparentAiAgent framework.

**Status**: Planning phase - ready for user review and approval

## Key Decisions Reflected in This Roadmap

- **LLM Provider**: Start with Azure OpenAI (unlimited access)
- **MCP Integration**: Use existing C# MCP SDK library
- **Context Management**: Simple truncation with visual indicators
- **UI Timing**: Basic UI before MCP (enables early end-to-end testing)
- **Transparency Focus**: Infrastructure transparency first (context, tools, config)

## Implementation Phases

### Phase 1: Foundation (Infrastructure)

**Goal**: Build the foundational infrastructure components that other components depend on.

**Components**:
1. **Configuration Manager**
   - Load and parse configuration files
   - Layered configuration support
   - Configuration models

2. **Domain Models**
   - Message models (`IMessage`, `UserMessage`, `AssistantMessage`, `ToolCallMessage`, etc.)
   - Configuration models
   - Basic interfaces (`ILLMProvider`, `ITool`, etc.)

3. **Exception Hierarchy**
   - Base `AgentException`
   - Derived exceptions (`LLMException`, `MCPException`, etc.)

4. **Transparency System (Basic)**
   - Event model
   - Event logger
   - In-memory event store
   - (SSE streaming added in Phase 3)

**Deliverables**:
- Configuration loads successfully
- Domain models defined and tested
- Exceptions can be thrown and caught
- Events can be logged and retrieved

**Tests**: Unit tests for all components

---

### Phase 2: LLM Integration

**Goal**: Implement LLM provider abstraction and at least one provider.

**Components**:
1. **LLM Provider Abstraction**
   - `ILLMProvider` interface
   - Provider factory
   - Request/response models

2. **Azure OpenAI Provider**:
   - Implement Azure OpenAI API calls
   - Handle Azure-specific authentication
   - Support streaming responses
   - Tool calling support (function calling)

3. **Streaming Handler**
   - `IAsyncEnumerable<T>` support
   - Stream buffering
   - Error handling mid-stream

4. **Authentication Manager**
   - `IAuthenticationProvider` interface
   - API key provider implementation
   - Configuration-based credentials

**Deliverables**:
- Can make LLM calls to chosen provider
- Streaming responses work
- Authentication functional
- Errors handled gracefully

**Tests**: Unit tests with mocked HTTP responses

---

### Phase 3: Agent Core

**Goal**: Implement the core agent orchestration logic.

**Components**:
1. **Conversation Manager**
   - Maintain conversation history (full chat history)
   - Context window management with truncation
   - Track message context status (`InContext` vs `TruncatedFromContext`)
   - Add/retrieve messages
   - Truncate oldest messages when limit reached

2. **Agent Orchestrator**
   - Main agent loop
   - Coordinate LLM calls
   - Handle user input/output
   - Integrate with Transparency System

3. **Message Pipeline**
   - Message validation
   - Message transformation for LLM
   - Response formatting

**Deliverables**:
- Can have multi-turn conversation with LLM
- Context maintained across turns
- All actions logged to Transparency System
- Basic agent loop working

**Tests**: Integration tests with mock LLM provider

---

### Phase 4: Basic UI

**Goal**: Create minimal working Blazor UI for end-to-end testing.

**Components**:
1. **Blazor Server Setup**
   - Project configuration
   - Basic layout and routing

2. **Chat Component**
   - Display chat history (all messages)
   - Show context status for each message
   - Visual indicators: "In Context" vs "Not in Context"
   - User input field
   - Send messages to agent

3. **State Management**
   - AppState service
   - Connect UI to Agent Orchestrator
   - Handle real-time updates

4. **Message Display**
   - User messages
   - Assistant messages
   - Basic formatting
   - Context status badges

5. **Context Window Display**
   - Show context usage (e.g., "15/20 messages in context")
   - Visual indicator of context limit

**Deliverables**:
- Can chat with agent via browser
- Messages display with context status
- Full transparency: user sees what LLM sees
- Basic agent loop functional through UI
- **End-to-end testing possible without MCP**

**Tests**: Manual testing (E2E testing later)

**Why This Phase is Here**:
- Enables functional testing earlier
- Validates architecture before adding MCP complexity
- User can interact with agent sooner
- Proves basic agent works before adding tools

---

### Phase 5: Tool Integration

**Goal**: Implement source-agnostic tool system with MCP integration (MOVED FROM PHASE 4).

**Note**: Now that basic UI works, we can add tools as an enhancement.

**Architecture**: Tool Abstraction Layer (supports MCP, built-in, future protocols)

**Components**:

1. **Domain Abstractions** (Tool System)
   - `ITool` - Source-agnostic tool definition
   - `IToolExecutor` - Execution abstraction
   - `IToolRegistry` - Discovery and management
   - `IToolManager` - High-level orchestration
   - `ToolExecutionResult` - Standardized result format
   - `ToolSourceType` enum (MCP, BuiltIn, etc.)

2. **Application Layer**
   - `ToolManager` - Routes tool calls by source type
   - Orchestrates tool execution
   - Integrates with Transparency System
   - Error handling and timeout management

3. **MCP Implementation** (Infrastructure)
   - `MCPClientWrapper` - Wraps C# MCP SDK
   - `MCPToolDiscovery` - Discovers tools from MCP servers
   - `MCPToolExecutor` - Executes MCP tool calls (implements IToolExecutor)
   - `MCPToolRegistry` - Manages MCP tool catalog (implements IToolRegistry)
   - `ToolRegistryComposite` - Aggregates all tool sources

4. **Agent Orchestrator Updates**
   - Detect tool calls from LLM
   - Execute tools via ToolManager (sequential in Phase 5)
   - Continue conversation with tool results
   - Track tool call depth (max 10)

5. **Configuration Updates**
   - `MCPConfiguration`: AutoDiscoverTools, ToolExecutionTimeoutSeconds (180s), MaxToolCallDepth (10)
   - `AgentConfiguration`: EnableTools, ToolExecutionMode (Sequential/Parallel)

6. **Transparency Integration**
   - New event types: Tool discovery, execution, timeout, MCP lifecycle
   - Log all tool operations

**Key Design Decisions**:
- **Tool Routing**: By `SourceType` (metadata-driven, not hardcoded)
- **Execution Mode**: Sequential (Phase 5), designed for parallel (future)
- **Timeout**: 180 seconds default
- **Depth Limit**: 10 levels max
- **Built-In Tools**: Deferred to post-Phase 5 (architecture ready)

**Deliverables**:
- Tool abstraction layer implemented
- MCP servers connect successfully
- Tools discovered from MCP servers
- LLM can call MCP tools
- Tool results returned to LLM
- Full tool-calling loop works
- Tool execution visible in Transparency System
- Tool calls displayed in UI (basic)
- Easy to add new tool sources in future

**Tests**:
- Unit tests (TDD approach) for all components
- Integration tests with real MCP servers (todo-list, context7)
- End-to-end functional tests

**Time Estimate**: ~40-50 hours (1-1.5 weeks)

**Note**: Using C# MCP SDK library (user has server-side experience, client-side is new)

---

### Phase 6: Enhanced UI & Transparency

**Goal**: Add streaming, transparency view, and better formatting (UNCHANGED - UI enhancements).

**Components**:
1. **SSE Integration**
   - SSE endpoint for Transparency events
   - SSE client in Blazor
   - Real-time event updates

2. **Enhanced Chat Display**
   - Streaming LLM responses in real-time
   - Tool call formatting (JSON)
   - Distinguished user/assistant styling

3. **Transparency Viewer Component**
   - Display transparency events
   - Show current context
   - Event filtering/search

4. **Tools Overview Component**
   - List available tools
   - Display tool schemas
   - Show usage history

**Deliverables**:
- Real-time streaming in UI
- Full transparency visible
- Tool calls beautifully formatted
- Tools overview functional

**Tests**: Manual testing + UI component tests (bUnit)

---

### Phase 7: Configuration UI

**Goal**: Allow configuration editing from UI.

**Components**:
1. **Configuration Component**
   - Display current configuration
   - Edit system prompts
   - Adjust LLM parameters
   - Manage tool availability

2. **Configuration API**
   - Endpoints to get/update config
   - Configuration validation
   - Hot-reload support

**Deliverables**:
- Can edit config from UI
- Changes apply immediately (where appropriate)
- Configuration persists

**Tests**: Unit tests for config API

---

### Phase 8: Anthropic Provider

**Goal**: Add Anthropic Claude provider and validate abstraction.

**Components**:
1. **Anthropic Provider Implementation**
   - Implement Anthropic API calls
   - Handle Anthropic-specific authentication
   - Support streaming responses
   - Tool calling support
   - Handle Anthropic-specific format differences

2. **Provider Selection**
   - UI to select active provider
   - Provider switching
   - Configuration per provider

**Deliverables**:
- Two providers working
- Can switch between providers
- Abstraction validated

**Tests**: Same tests pass for both providers

---

### Phase 9: Polish & Refinement

**Goal**: Improve user experience and robustness.

**Improvements**:
- Better error messages
- Loading states in UI
- Context summarization (if needed)
- Token counting (if desired)
- Performance optimization
- Documentation updates
- User guide

**Deliverables**:
- Production-ready agent
- Complete documentation
- User guide

---

## Implementation Order Rationale

1. **Foundation first**: Infrastructure components are needed by everything else
2. **LLM next**: Core functionality requires LLM integration (Azure OpenAI)
3. **Agent core**: Orchestration pulls together LLM and infrastructure
4. **Basic UI early**: Enables end-to-end functional testing before adding MCP complexity
5. **MCP tools**: Added as enhancement after basic agent works
6. **Enhanced UI**: Real-time streaming and transparency after tools work
7. **Configuration UI**: Nice-to-have, can be done after core functionality
8. **Anthropic provider**: Validates architecture works for multiple providers
9. **Polish last**: Refine once core features are solid

**Key Change**: Basic UI moved before MCP (Phase 4 instead of Phase 5) to enable early functional testing and validation.

## TDD Approach

For each component:
1. Write failing tests first
2. Implement minimum code to pass tests
3. Refactor with tests as safety net
4. Move to next component

## Decisions Made

All questions resolved:

1. **LLM Provider**: Azure OpenAI first (unlimited access on other station)
2. **MCP Library**: Use existing C# MCP SDK library (user has experience)
3. **MCP Servers**: Test with todo-list and context7 (already configured)
4. **Context Management**: Simple truncation with visual indicators in UI
5. **Phase Order**: Basic UI before MCP (Phase 4/5 swapped)

## Next Steps

**Ready for Implementation Phase Tomorrow**:

1. **Review this roadmap** - Ensure all phases make sense
2. **Set up development environment** - Ensure .NET 8.0, tools ready
3. **Begin Phase 1 tomorrow** - Foundation (Configuration, Models, Exceptions)
4. **TDD approach** - Tests first, then implementation

**Documentation Complete**:
- ✅ Requirements gathered and refined
- ✅ Architecture designed
- ✅ Components identified
- ✅ Design decisions documented (18 decisions logged)
- ✅ Implementation roadmap created and reordered
- ✅ All clarifications incorporated

**Tomorrow's Focus**: Begin Phase 1 - Foundation

---

**Status**: Planning complete - Ready for implementation
**Last Updated**: 2025-11-01 (Phase 5 updated with approved tool architecture)
