# START HERE - TransparentAiAgent Project Guide

Welcome! This is your central navigation document for the TransparentAiAgent project.

**Last Updated**: 2025-10-28
**Status**: Planning Complete - Ready for Implementation

---

## 📋 What is This Project?

**TransparentAiAgent** is a fully transparent, highly configurable, and LLM-agnostic AI agent framework.

**Core Values**:
1. **Infrastructure Transparency** - Every tool call, context change, and config setting is visible
2. **LLM Agnostic** - Works with multiple LLM providers (Azure OpenAI, Anthropic, etc.)
3. **MCP-Only Tools** - All capabilities via MCP protocol (no built-in functions)
4. **Clean Architecture** - Maintainable, testable, extensible design
5. **TDD Approach** - Tests first, implementation second

---

## 🗺️ Documentation Map

### For Planning & Understanding (Read These First)

| Document | Purpose | Read When |
|----------|---------|-----------|
| **[REQUIREMENTS.md](REQUIREMENTS.md)** | What we're building | Start here |
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | How it's structured | After requirements |
| **[COMPONENTS.md](COMPONENTS.md)** | Component overview | After architecture |
| **[DESIGN_DECISIONS.md](DESIGN_DECISIONS.md)** | Why we made choices (18 decisions) | Reference as needed |
| **[IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md)** | 9-phase plan | Before coding |

### For Specific Topics (Reference as Needed)

| Document | Purpose | Read When |
|----------|---------|-----------|
| **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** | Quick lookup guide | During implementation |
| **[BLAZOR_HOSTING_DEEP_DIVE.md](BLAZOR_HOSTING_DEEP_DIVE.md)** | Blazor Server vs WebAssembly explained | If curious about hosting |
| **[BLAZOR_PRACTICAL_EXAMPLE.md](BLAZOR_PRACTICAL_EXAMPLE.md)** | Concrete examples of both approaches | If need more detail |
| **[DEPLOYMENT_TROUBLESHOOTING.md](DEPLOYMENT_TROUBLESHOOTING.md)** | Running and debugging guide | When running the app |
| **[SESSION_SUMMARY.md](SESSION_SUMMARY.md)** | What we accomplished today | For recap |

### Component Details (Created During Implementation)

| Directory | Purpose |
|-----------|---------|
| **[components/core/](components/core/)** | Agent Orchestrator, Conversation Manager, etc. |
| **[components/llm/](components/llm/)** | LLM provider abstractions and implementations |
| **[components/mcp/](components/mcp/)** | MCP client and tool system |
| **[components/infrastructure/](components/infrastructure/)** | Config, Auth, Transparency, etc. |
| **[components/ui/](components/ui/)** | Blazor components and UI logic |

**Note**: Component detail docs will be created as we implement each component (TDD).

---

## 🎯 Current Status

### ✅ Completed (Planning Phase - 2025-10-28)

- [x] Requirements gathering and refinement
- [x] Architecture design (Clean Architecture with 4 layers)
- [x] Component identification (5 categories, 18 components)
- [x] Design decisions documented (18 decisions with rationale)
- [x] Implementation roadmap (9 phases)
- [x] Blazor hosting model decided (Blazor Server)
- [x] LLM provider priority (Azure OpenAI first)
- [x] MCP integration strategy (C# SDK library)
- [x] Context management strategy (simple truncation with visual indicators)
- [x] Phase ordering optimized (Basic UI before MCP)

### 🚀 Next: Phase 1 - Foundation (Tomorrow)

**Goal**: Build foundational infrastructure

**Components to Implement (TDD)**:
1. Domain Models (messages with context status)
2. Exception hierarchy
3. Configuration Manager
4. Basic Transparency System

**Approach**: Tests first, then implementation

---

## 📖 Reading Order for New Session Tomorrow

**Quick Start** (5 minutes):
1. This file (you're reading it!)
2. [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Key decisions and Phase 1 starting point

**If You Need Refresher** (15 minutes):
3. [REQUIREMENTS.md](REQUIREMENTS.md) - What we're building
4. [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md) - Phase 1 details

**If You Need Deep Understanding** (30+ minutes):
5. [ARCHITECTURE.md](ARCHITECTURE.md) - Complete system design
6. [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) - All decisions with rationale

**When You Start Coding**:
7. [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Keep this open for reference
8. [DEPLOYMENT_TROUBLESHOOTING.md](DEPLOYMENT_TROUBLESHOOTING.md) - When you run the app

---

## 🏗️ Architecture Summary

### Layers

```
┌─────────────────────────────────────┐
│  Presentation Layer                 │
│  - Blazor Server components         │
│  - Real-time updates via SignalR    │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│  Application Layer                  │
│  - Agent Orchestrator               │
│  - Conversation Manager             │
│  - Message Pipeline                 │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│  Domain Layer                       │
│  - Interfaces (ILLMProvider, ITool) │
│  - Models (Messages, Config)        │
│  - Abstractions                     │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│  Infrastructure Layer               │
│  - Azure OpenAI Provider            │
│  - MCP Client (C# SDK)              │
│  - Configuration Manager            │
│  - Authentication Manager           │
│  - Transparency System              │
└─────────────────────────────────────┘
```

**Dependency Rule**: Dependencies point inward only.

### Technology Stack

- **.NET 8.0** - Framework
- **C#** - Language
- **Blazor Server** - UI with SignalR for real-time
- **MSTest** - Testing framework
- **Azure OpenAI** - First LLM provider (Anthropic in Phase 8)
- **C# MCP SDK** - For MCP client

### Key Design Patterns

- **Strategy Pattern** - LLM provider selection
- **Observer Pattern** - Transparency events
- **Pipeline Pattern** - Message processing
- **Factory Pattern** - Provider creation

---

## 🎯 Core Concepts

### 1. Infrastructure Transparency (PRIMARY FOCUS)

**What's Visible**:
- ✅ Context management (what's in LLM context vs truncated)
- ✅ Tool calls and results (JSON formatted)
- ✅ Configuration (system prompt, LLM params)
- ✅ System state (what agent is doing)

**Not Focus** (Later Phase):
- ⏭️ LLM reasoning chains (provider-specific)

### 2. Context Management

**Critical Feature**:
- Full chat history always visible
- Messages have status: `InContext` | `TruncatedFromContext`
- When limit reached, oldest messages truncated
- Truncated messages stay visible with indicator
- User always knows what LLM can see

**UI Example**:
```
✅ User: Hi                      [In Context]
✅ Assistant: Hello!             [In Context]
⚠️  User: What's your name?      [Not in Context]
⚠️  Assistant: I'm an AI         [Not in Context]
✅ User: What can you do?        [In Context]
```

### 3. MCP-Only Tools

**Philosophy**: Everything via MCP, NO built-in functions

**Why**:
- Modular (add/remove tools easily)
- Standardized (MCP protocol)
- Flexible (external tool servers)
- Clean separation (agent = orchestration + MCP client)

### 4. Blazor Server Architecture

**How It Works**:
```
Browser (localhost:5000)
    ↕ SignalR WebSocket
Server Process (localhost:5000)
    ├─ All C# code runs here
    ├─ Direct MCP server access
    ├─ Direct LLM API calls
    └─ Only UI updates sent to browser
```

**Why This Works**:
- Localhost = ~1-5ms latency (imperceptible)
- SignalR = real-time streaming built-in
- MCP servers = direct stdio access
- API keys = stay secure on server
- Deployment = simple (one process)

**See [BLAZOR_HOSTING_DEEP_DIVE.md](BLAZOR_HOSTING_DEEP_DIVE.md) for detailed explanation.**

---

## 📅 Implementation Plan Summary

### Phase 1: Foundation (START HERE TOMORROW)

**Components**:
- Domain models (messages, config)
- Exception hierarchy
- Configuration Manager
- Basic Transparency System

**Deliverable**: Core abstractions and infrastructure

**Time**: ~1-2 days

---

### Phase 2: LLM Integration

**Components**:
- `ILLMProvider` interface
- Azure OpenAI provider
- Streaming handler
- Authentication manager

**Deliverable**: Can call Azure OpenAI with streaming

**Time**: ~2-3 days

---

### Phase 3: Agent Core

**Components**:
- Conversation Manager (with context tracking)
- Agent Orchestrator
- Message Pipeline

**Deliverable**: Multi-turn conversation works

**Time**: ~2-3 days

---

### Phase 4: Basic UI ⭐

**Components**:
- Blazor Server setup
- Chat component (with context status)
- State management
- Message display

**Deliverable**: **End-to-end functional test possible**

**Time**: ~2-3 days

**Why Here**: Enables functional testing before adding MCP complexity

---

### Phase 5: MCP Integration

**Components**:
- MCP Client (using C# SDK)
- Tool Registry
- Tool Executor
- Agent Orchestrator updates

**Deliverable**: Tools work end-to-end

**Time**: ~3-4 days

---

### Phases 6-9

- **Phase 6**: Enhanced UI & Transparency (SSE, streaming, tools view)
- **Phase 7**: Configuration UI
- **Phase 8**: Anthropic Provider
- **Phase 9**: Polish & Refinement

**See [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md) for full details.**

---

## 🔑 Key Decisions Reference

### Q: Why Blazor Server instead of WebAssembly?

**A**:
- ✅ Direct MCP server access (browser can't spawn processes)
- ✅ Real-time streaming built-in (SignalR)
- ✅ API keys stay secure
- ✅ Simple deployment (one process)
- ✅ Localhost = no latency concern (~1-5ms)

**See**: [DD-004 in DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) and [BLAZOR_HOSTING_DEEP_DIVE.md](BLAZOR_HOSTING_DEEP_DIVE.md)

### Q: Why Azure OpenAI first instead of Anthropic?

**A**:
- Unlimited access on other station (testing)
- Conserve Anthropic limits for development
- Can validate abstraction with second provider later (Phase 8)

**See**: [DD-015 in DESIGN_DECISIONS.md](DESIGN_DECISIONS.md)

### Q: Why MCP-only (no built-in functions)?

**A**:
- User explicitly dislikes built-in functions (non-modular)
- MCP provides standardized, extensible tool interface
- Clean separation of concerns
- Easy to add/remove capabilities

**See**: [DD-006 in DESIGN_DECISIONS.md](DESIGN_DECISIONS.md)

### Q: Why simple truncation instead of summarization?

**A**:
- Simpler to implement
- Easier to make transparent (clear what LLM sees)
- Can add summarization later as enhancement
- Transparency is key: user must know what's in context

**See**: [DD-017 in DESIGN_DECISIONS.md](DESIGN_DECISIONS.md)

### Q: Why Basic UI before MCP (Phase 4 vs 5)?

**A**:
- Enables end-to-end functional testing earlier
- Can validate architecture with real UI
- Proves basic agent works before adding tool complexity
- User can interact sooner

**See**: [DD-018 in DESIGN_DECISIONS.md](DESIGN_DECISIONS.md)

---

## 🚀 Tomorrow's Checklist

Before starting Phase 1:

### Prerequisites

- [ ] .NET 8.0 SDK installed (`dotnet --version`)
- [ ] Visual Studio 2022 or VS Code ready
- [ ] Solution builds (`dotnet build`)
- [ ] Tests run (`dotnet test`)
- [ ] Git repository initialized
- [ ] Read QUICK_REFERENCE.md

### Phase 1 Starting Point

**First Component**: Message Models

**TDD Process**:
1. Write failing test for `UserMessage`
2. Implement `UserMessage` to pass test
3. Refactor if needed
4. Move to next (e.g., `AssistantMessage`)

**Example First Test**:

```csharp
// In TransparentAiAgentCore_Tests/Models/MessageTests.cs

[TestClass]
public class MessageTests
{
    [TestMethod]
    public void UserMessage_ShouldHaveUserRole()
    {
        // Arrange & Act
        var message = new UserMessage("Hello");

        // Assert
        Assert.AreEqual(MessageRole.User, message.Role);
    }

    [TestMethod]
    public void Message_ShouldDefaultToInContext()
    {
        // Arrange & Act
        var message = new UserMessage("Hello");

        // Assert
        Assert.AreEqual(MessageContextStatus.InContext,
                       message.ContextStatus);
    }
}
```

**Then implement**:

```csharp
// In TransparentAiAgentCore/Models/Messages/UserMessage.cs

public class UserMessage : IMessage
{
    public MessageRole Role => MessageRole.User;
    public string Content { get; }
    public MessageContextStatus ContextStatus { get; set; }

    public UserMessage(string content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContextStatus = MessageContextStatus.InContext; // Default
    }
}
```

**See [QUICK_REFERENCE.md](QUICK_REFERENCE.md) for more Phase 1 details.**

---

## 🔍 Quick Problem Solving

### "I'm confused about X"

| Topic | See Document |
|-------|-------------|
| What we're building | [REQUIREMENTS.md](REQUIREMENTS.md) |
| How it's structured | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Why we chose X | [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) |
| What to build when | [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md) |
| How to start coding | [QUICK_REFERENCE.md](QUICK_REFERENCE.md) |
| Blazor Server vs WebAssembly | [BLAZOR_HOSTING_DEEP_DIVE.md](BLAZOR_HOSTING_DEEP_DIVE.md) |
| App won't run | [DEPLOYMENT_TROUBLESHOOTING.md](DEPLOYMENT_TROUBLESHOOTING.md) |

### "I need to find where we decided X"

All design decisions are logged in [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) with:
- Context (why we needed to decide)
- Decision made
- Options considered
- Rationale
- Consequences

**18 decisions documented** (DD-001 through DD-018).

---

## 📊 Project Metrics

### Documentation Created

- **Core docs**: 6 major documents
- **Component structure**: 5 categories, 18 components identified
- **Design decisions**: 18 decisions logged with full rationale
- **Implementation phases**: 9 phases planned
- **Total documentation**: ~70 pages (estimated)

### Planning Complete

- ✅ Requirements gathered and refined
- ✅ Architecture designed (Clean Architecture)
- ✅ Technology stack selected
- ✅ All major decisions made and documented
- ✅ Implementation roadmap created
- ✅ Component structure ready
- ✅ TDD approach defined

### Ready for Implementation

- ✅ Phase 1 components identified
- ✅ Starting point clear (Message models with TDD)
- ✅ Example test provided
- ✅ All questions answered
- ✅ No blockers

---

## 🎓 Learning Resources

### If You're New to:

**Clean Architecture**:
- Review [ARCHITECTURE.md](ARCHITECTURE.md) layers section
- Key principle: Dependencies point inward
- Domain has no external dependencies

**TDD (Test-Driven Development)**:
- Write test first (red)
- Implement minimum code to pass (green)
- Refactor with safety net (refactor)
- See [DD-014 in DESIGN_DECISIONS.md](DESIGN_DECISIONS.md)

**Blazor Server**:
- Read [BLAZOR_HOSTING_DEEP_DIVE.md](BLAZOR_HOSTING_DEEP_DIVE.md)
- SignalR provides real-time updates
- C# code runs on server, UI updates sent to browser

**MCP Protocol**:
- Will use existing C# MCP SDK
- Phase 5 implementation
- Stdio transport for local servers

---

## 🤝 Working with Claude Code

### Context for Claude

Claude Code has access to:
- All documentation in `docs/` folder
- `.claude/README.md` workspace context
- Full codebase

### Helpful Commands

When starting new session tomorrow:

```
"Let's start Phase 1. I want to begin with message models using TDD."

"Show me the first test for UserMessage."

"I'm stuck on X, can you check DESIGN_DECISIONS.md for context?"

"Summarize the architecture layers for me."
```

### Session Continuity

All decisions and context are documented, so future sessions can:
- Reference design decisions by number (e.g., "Per DD-008...")
- Look up component responsibilities
- Follow implementation roadmap
- Understand rationale for choices

---

## ✅ Final Pre-Implementation Checklist

Before you start coding tomorrow:

### Understanding
- [ ] Read this START_HERE.md
- [ ] Read QUICK_REFERENCE.md
- [ ] Understand Phase 1 goals
- [ ] Know what TDD means for this project
- [ ] Understand context management concept

### Environment
- [ ] .NET 8.0 SDK installed
- [ ] IDE ready (Visual Studio / VS Code)
- [ ] Solution opens and builds
- [ ] Can run tests (even if none yet)
- [ ] Git initialized

### Planning
- [ ] Know first component: Message models
- [ ] Know approach: TDD (test first)
- [ ] Know where to find help: This guide + QUICK_REFERENCE.md
- [ ] Know next steps: Write test for UserMessage

### Mindset
- [ ] Ready to write tests first (even if feels slow initially)
- [ ] Ready to ask questions if stuck
- [ ] Ready to reference design decisions when needed
- [ ] Excited to build! 🚀

---

## 🎉 You're Ready!

**Planning Phase**: ✅ Complete

**Next Session**: Phase 1 - Foundation (Message models with TDD)

**Documentation**: ✅ Complete and organized

**Questions**: All answered and documented

**Architecture**: ✅ Designed and documented

**Let's build this! 🚀**

---

## 📞 Quick Links

### Most Important Docs
- [REQUIREMENTS.md](REQUIREMENTS.md) - What we're building
- [ARCHITECTURE.md](ARCHITECTURE.md) - How it's structured
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Development reference
- [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md) - 9-phase plan

### When You Need Help
- [DEPLOYMENT_TROUBLESHOOTING.md](DEPLOYMENT_TROUBLESHOOTING.md) - Running & debugging
- [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) - Why we chose what we chose
- [BLAZOR_HOSTING_DEEP_DIVE.md](BLAZOR_HOSTING_DEEP_DIVE.md) - Blazor Server explained

### For Context
- [SESSION_SUMMARY.md](SESSION_SUMMARY.md) - What we accomplished today
- [components/](components/) - Component documentation structure

---

**Welcome to TransparentAiAgent development!**

**Date**: 2025-10-28
**Status**: Ready to implement Phase 1
**Approach**: TDD with Clean Architecture
**First Component**: Message models with context status

Let's make this the best transparent AI agent framework! 🎯

---
