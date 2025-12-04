# TransparentAiAgent

A production-ready framework for building transparent, configurable, and LLM-agnostic AI agents with interactive teaching capabilities.

## Overview

TransparentAiAgent is a complete framework featuring:
- **Full Transparency**: Every decision, action, and reasoning step is visible and auditable
- **Multi-Provider Support**: Anthropic Claude, Azure OpenAI, and OpenAI with dynamic switching
- **Interactive Teaching Mode**: Agent-controlled UI for guided learning experiences
- **MCP Integration**: Extensible tool system via Model Context Protocol
- **Long-Term Memory**: Persistent agent memory across conversation sessions
- **Knowledge Library**: Curated teaching concepts and guardrails

## Key Features

- **Blazor Server UI** with real-time streaming
- **Dynamic provider switching** without application restart
- **Teaching scenarios** for interactive onboarding
- **UI control tools** enabling agent-driven interface manipulation
- **Comprehensive transparency logging** for debugging and education
- **Clean Architecture** with full test coverage

## Technology Stack

- **.NET 8.0** - Framework
- **C#** - Language
- **Blazor Server** - Interactive UI with SignalR
- **MCP Protocol** - Tool integration
- **MSTest** - Testing (Lean TDD approach)

## Getting Started

See the [Installation Guide](docs/05-guides/installation/installation-guide.md) for complete setup instructions.

**Quick Start**:
1. Configure your LLM provider (Anthropic, Azure OpenAI, or OpenAI)
2. Optional: Set up MCP servers for additional tools
3. Run the application and start chatting

## Documentation

- **[Documentation Hub](docs/README.md)** - Complete documentation index
- [Requirements](docs/07-planning/requirements.md) - Project vision and goals
- [Architecture](docs/02-architecture/overview.md) - System design
- [Components](docs/04-components/README.md) - Component documentation
- [Guides](docs/05-guides/README.md) - How-to guides and tutorials

## Project Structure

```
TransparentAiAgent/
├── TransparentAiAgentCore/          # Core agent logic (Clean Architecture)
├── TransparentAiAgentGui/           # Blazor Server UI
├── TransparentAiAgentCore_Tests/    # Comprehensive test suite
├── docs/                            # Complete documentation (35+ docs)
└── .claude/                         # Claude Code workspace config
```

## Status

**Beta Testing** - Core features complete, entering first public beta

See the [Roadmap](docs/07-planning/roadmap.md) for planned enhancements.

---

**Last Updated**: 2025-12-04
