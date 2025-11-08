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

**Status**: ✅ 95% Complete (Core implementation done, optional tasks remaining)
**Completed**: 2025-11-03

**Goal**: Add Anthropic Claude provider and validate abstraction.

**Components**:
1. ✅ **Anthropic Provider Implementation**
   - ✅ Implement Anthropic API calls
   - ✅ Handle Anthropic-specific authentication
   - ⏳ Support streaming responses (partial - null validation done)
   - ✅ Tool calling support (conversion implemented)
   - ✅ Handle Anthropic-specific format differences (system messages, content blocks)

2. ✅ **Provider Selection**
   - ✅ Factory supports provider switching
   - ✅ Provider switching via configuration only
   - ✅ Configuration per provider

**Deliverables**:
- ✅ AnthropicProvider.cs implemented (~245 lines)
- ✅ 17/17 unit tests passing (100% success rate)
- ✅ Factory integration complete (15/15 tests passing)
- ✅ Can switch between providers via config
- ✅ Abstraction validated (ILLMProvider works for both providers)
- ⏳ Full streaming implementation (optional)
- ⏳ Integration testing with live API (optional)

**Tests**:
- ✅ 17 AnthropicProvider unit tests passing
- ✅ 15 LLMProviderFactory tests passing (including Anthropic)
- ⏳ Integration tests pending (optional)

**Detailed Documentation**: See [PHASE_8_DETAILED_PLAN.md](./PHASE_8_DETAILED_PLAN.md)

---

### Phase 9: Interactive Teaching Mode Layer

**Status**: 📋 Planning Complete - Ready for Implementation

**Goal**: Build a transformational layer that enables the agent to teach users about transparency features through interactive UI control.

**This is NOT just another incremental phase** - this represents a new conceptual layer on top of the transparent agent foundation.

**Overview**:
The Interactive Teaching Mode Layer enables agents to:
- Dynamically control UI components via built-in tools
- Progressively reveal features in Teaching Mode
- Teach users about transparency concepts interactively
- Provide context-aware learning experiences

**Key Components**:
1. **Built-In UI Control Tools** (7 tools appearing as MCP tools to agent)
   - `ui_control_chat_filter` - Show/hide message types
   - `ui_control_filter_visibility` - Reveal filter controls
   - `ui_get_state` - Query current UI state
   - `ui_control_transparency_viewer` - Control event logging panel
   - `ui_control_tools_panel` - Highlight and expand tools
   - `ui_control_context_indicators` - Teach about context windows
   - `ui_control_configuration` - Guide through configuration

2. **UI Control Service** - Manages UI state and coordinates updates

3. **Teaching Mode System** - Mode switching, teaching-focused system prompt

4. **Enhanced UI Components** - Filter controls, state-aware rendering

**Sub-Phases**:
- **Phase 9a**: Core Infrastructure (UIState, Services, Tool Registry) - 2-3 days ✅ **Complete**
- **Phase 9b**: Chat History Control Tools - 2 days ✅ **Complete**
- **Phase 9c**: Additional UI Component Tools - 3-4 days ✅ **Complete**
- **Phase 9d**: Teaching Mode System - 2-3 days
- **Phase 9e**: Polish & Documentation - 1-2 days

**Deliverables**:
- All 7 UI control tools functional
- Agent can control UI components
- Teaching Mode with progressive reveal
- Normal Mode with full transparency
- Comprehensive documentation (Vision, Architecture, Roadmap)
- >80% test coverage
- Production-ready teaching system

**Time Estimate**: 10-15 development days

**Detailed Documentation**:
- 📘 **[Interactive Teaching Mode Vision](./INTERACTIVE_TEACHING_MODE_VISION.md)** - Philosophy, use cases, and user journeys
- 🏗️ **[Agent UI Control Architecture](./AGENT_UI_CONTROL_ARCHITECTURE.md)** - Technical specifications and designs
- 🗺️ **[Teaching Mode Implementation Roadmap](./TEACHING_MODE_IMPLEMENTATION_ROADMAP.md)** - Detailed step-by-step implementation guide

**Tests**: Comprehensive unit, component (bUnit), and integration tests following Lean TDD

---

### Phase 10: Polish & Refinement

**Goal**: Improve user experience and robustness across the entire system.

**Improvements**:
- Better error messages
- Loading states in UI
- Context summarization (if needed)
- Token counting (if desired)
- Performance optimization
- Documentation updates
- User guide updates for Teaching Mode
- Accessibility improvements

**Deliverables**:
- Production-ready agent with teaching capabilities
- Complete documentation
- Comprehensive user guide

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
9. **Teaching Mode Layer**: Transformational layer built on solid foundation (Phases 1-8)
10. **Polish last**: Refine once core features are solid

**Key Changes**:
- Basic UI moved before MCP (Phase 4 instead of Phase 5) to enable early functional testing
- Teaching Mode Layer added as Phase 9 (distinct from incremental phases - new conceptual layer)

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

**Status**: Phases 1-7 Complete | Phase 8 (Anthropic API) 95% Complete | Phase 9 (Teaching Mode) Ready
**Last Updated**: 2025-11-03 (Phase 8 Anthropic provider core implementation completed)
