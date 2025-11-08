# TransparentAiAgent - Requirements Document

## Project Vision

Build a fully transparent, highly configurable, and LLM-agnostic AI agent framework that serves as a template for easily creating custom agents.

## Core Principles

### 1. Transparency
- Every decision made by the agent must be visible
- Reasoning chains should be logged and auditable
- User should be able to inspect agent state at any point
- Actions and their rationale should be clearly communicated

### 2. Configurability
- Flexible configuration system
- Easy to customize agent behavior
- Plugin-based architecture for extensibility

### 3. LLM Agnostic
- Support multiple LLM providers (OpenAI, Anthropic, local models, etc.)
- Unified interface for LLM interaction
- Easy to switch between providers

### 4. Template Nature
- Clear patterns for extending functionality
- Well-documented codebase
- Examples and guides for common use cases

## Technology Stack

- **Language**: C# (.NET 8.0)
- **GUI**: Blazor (Web-based UI)
- **Testing**: TDD approach with xUnit/NUnit (TBD)
- **Architecture**: Clean Architecture / Layered approach

## Detailed Requirements

### Transparency Requirements

**Primary Focus: Infrastructure Transparency**

This is the CORE focus of the project. Infrastructure transparency includes:

1. **Context Management Transparency**:
   - Always visible: What messages are in the current LLM context
   - Context truncation clearly indicated in UI
   - Context window size and usage visible
   - When messages are truncated, they remain in chat history but marked as "not in context"

2. **Tool System Transparency**:
   - All available tools visible with schemas
   - Tool calls in real-time as they happen
   - Tool call parameters (JSON formatted)
   - Tool results
   - Tool execution time/status

3. **Configuration Transparency**:
   - Current system prompt visible
   - LLM parameters (temperature, top-p) visible
   - Active provider visible
   - All settings available for inspection

4. **System State Transparency**:
   - What the agent is doing at any moment
   - Request/response flow
   - LLM output streaming (SSE - Server-Sent Events)
   - Errors and warnings

**Secondary Focus: LLM Reasoning Transparency** (Later Phase)

- LLM internal reasoning chains (provider-specific)
- Chain-of-thought visibility
- Decision-making transparency
- Note: This is harder to implement in LLM-agnostic way
- Will be addressed in later phase, possibly via system prompts

**UI Presentation**:
- Real-time streaming for everything
- Structured display: JSON-formatted tool calls
- Distinguished formatting for user vs LLM messages
- Clear visual indicators for context status
- Chat history shows ALL messages, with context status marked

### LLM Integration Requirements

**Initial Providers** (Priority order):
1. Azure OpenAI (unlimited access on other station)
2. Anthropic Claude (limited access on current station)
3. Future: Other providers (OpenAI, local models, etc.)

**Required Features**:
- Multi-turn conversation support (CRITICAL)
- Tool/function calling support (CRITICAL)
- Streaming responses (CRITICAL)
- Token counting and cost tracking (LOW PRIORITY initially)

**Excluded for Initial Version**:
- Vision capabilities
- Non-text modalities

### Agent Capabilities & Tool System

**Philosophy**: Everything via MCP server tools, NO built-in functions
- Agent should be purely orchestration + MCP client
- All capabilities exposed through MCP tools:
  - File system operations
  - Web scraping/browsing
  - Code execution
  - API access
  - Any other functionality

**Tool Integration**:
- Classic MCP client behavior
- Configuration-based tool discovery and registration
- MCP protocol compliance

### Configuration System

**Configuration Methods**:
1. Configuration files (primary)
2. UI-based configuration (everything visible)

**Configurable Parameters** (Initial set):
- System prompts
- Available tools
- LLM sampling parameters:
  - Temperature
  - Top-p
- Future: More parameters as needed

### User Interface Requirements

**Core Components**:
1. **Chat Interface**:
   - Primary interaction point
   - Tool calls integrated into chat history
   - Real-time streaming display

2. **Configuration Panel** (possibly separate tab):
   - Edit system prompts
   - Configure LLM parameters
   - Manage tool availability

3. **Tools Overview**:
   - List all available tools
   - Display tool schemas
   - Show tool usage history

**Hosting Model**:
- Local deployment required
- Blazor WebAssembly vs Hybrid: TBD (need technical comparison)

### Testing Requirements

**Framework**: MSTest (user familiarity)

**Test Types** (Priority):
1. Unit tests (HIGH PRIORITY)
2. Integration tests (future)
3. End-to-end tests (future)

**Approach**: Test-Driven Development (TDD)
- Write tests first
- Implement to pass tests
- Refactor with test safety net

### Technical Constraints & Lessons Learned

**Known Pain Points from Previous Agent**:
1. **Streaming LLM Output**: Complex to implement correctly
   - User has ideas for solutions
   - Needs special attention in design

2. **Authentication**: Multiple endpoint types require different auth
   - Azure OpenAI: API key + endpoint
   - Anthropic: API key
   - MCP servers: Various auth methods
   - Solution: Flexible authentication abstraction

**Successful Patterns**:
- MCP tools integration worked well in previous agent
- Keep and refine MCP client approach

### Non-Functional Requirements

**Modularity**:
- Clean component boundaries
- Easy to extend and modify
- No tight coupling

**Maintainability**:
- Clear documentation
- SOLID principles
- Clean Architecture patterns

**Performance**:
- Real-time streaming without UI blocking
- Efficient context management
- Responsive user experience

---

**Status**: Requirements gathered from user - ready for architecture design
**Last Updated**: 2025-10-28
