# TransparentAiAgent Documentation

**Last Updated**: 2025-11-08
**Project Status**: Phase 8 Complete (95%) | Phase 9 Ready

Welcome to the TransparentAiAgent documentation! This is your central navigation hub.

---

## 🎯 Quick Start

**New to the project?** Start here:

1. **[Getting Started](01-getting-started/)** - Quick onboarding (coming soon)
2. **[Requirements](07-planning/requirements.md)** - What we're building
3. **[Architecture Overview](02-architecture/overview.md)** - How it's structured

**Developers:** See [Development Guides](05-guides/development/)

---

## 📚 Documentation Structure

Our documentation is organized into 9 categories:

### 1. [Getting Started](01-getting-started/) 🚀
Quick onboarding for new users and developers
- Installation and setup
- First conversation
- Quick start guide

### 2. [Architecture](02-architecture/) 🏗️
System design and structure
- [Overview](02-architecture/overview.md) - Clean Architecture layers, component organization
- [Design Decisions](02-architecture/design-decisions.md) - DD-001 through DD-XXX with rationale

### 3. [Concepts](03-concepts/) 💡
Cross-cutting features and ideas
- **[Teaching Mode](03-concepts/teaching-mode/)** - Interactive UI control and teaching
  - [Vision](03-concepts/teaching-mode/vision.md)
  - [Architecture](03-concepts/teaching-mode/architecture.md)
  - [Implementation Roadmap](03-concepts/teaching-mode/implementation-roadmap.md)
- [Tool System](03-concepts/tool-system.md) - MCP integration and tool abstraction

### 4. [Components](04-components/) 🧩
Detailed component documentation
- [Core](04-components/core/) - Agent Orchestrator, Conversation Manager
- [LLM](04-components/llm/) - Provider abstractions and implementations
- [Tools](04-components/tools/) - MCP client, tool registry, UI control tools
- [Infrastructure](04-components/infrastructure/) - Config, auth, transparency
- [UI](04-components/ui/) - Blazor components and state management

### 5. [Guides](05-guides/) 📖
Step-by-step how-to documentation
- [Development](05-guides/development/) - Adding components, providers, testing
- [Deployment](05-guides/deployment/) - [Configuration](05-guides/deployment/configuration-guide.md), [Troubleshooting](05-guides/deployment/troubleshooting.md)
- [Integration](05-guides/integration/) - MCP servers, custom tools, OAuth

**Note**: TDD workflow covered by `.claude/skills/tdd/` skill

### 6. [Reference](06-reference/) 📚
API specs and technical references
- **[Providers](06-reference/providers/)**
  - [Anthropic](06-reference/providers/anthropic/) - Complete Claude API reference (7 docs)
  - [Azure OpenAI](06-reference/providers/azure-openai/) - Authentication and API
- [Blazor](06-reference/blazor/) - Hosting models, examples
- [Quick Reference](06-reference/quick-reference.md) - One-page cheatsheet

### 7. [Planning](07-planning/) 📋
Project roadmap and requirements
- [Requirements](07-planning/requirements.md) - Core values, features, goals
- [Roadmap](07-planning/roadmap.md) - 9-phase implementation plan
- [Phases](07-planning/phases/) - Detailed phase documentation

### 8. [Contributing](08-contributing/) 📝
Documentation standards and templates
- [Templates](08-contributing/templates/) - Component, concept, guide templates
- Style guide and documentation standards

### 9. [Archive](09-archive/) 🗄️
Historical and session-specific docs
- [Sessions](09-archive/sessions/) - Development session summaries
- [Implementation Notes](09-archive/implementation-notes/) - Historical fixes

---

## 🔍 Find What You Need

### I want to...

**Understand the project**
→ [Requirements](07-planning/requirements.md) and [Architecture Overview](02-architecture/overview.md)

**Get started developing**
→ [Development Guides](05-guides/development/)

**Learn about Teaching Mode**
→ [Teaching Mode Vision](03-concepts/teaching-mode/vision.md)

**Configure the application**
→ [Configuration Guide](05-guides/deployment/configuration-guide.md)

**Understand a component**
→ [Components](04-components/)

**Look up Anthropic API details**
→ [Anthropic Reference](06-reference/providers/anthropic/)

**Troubleshoot an issue**
→ [Troubleshooting Guide](05-guides/deployment/troubleshooting.md)

**See the implementation plan**
→ [Roadmap](07-planning/roadmap.md)

---

## 🎓 Learning Paths

### For New Users
1. [Requirements](07-planning/requirements.md) - What is this?
2. [Architecture Overview](02-architecture/overview.md) - How does it work?
3. [Teaching Mode Vision](03-concepts/teaching-mode/vision.md) - What makes this special?

### For Developers
1. [Architecture Overview](02-architecture/overview.md) - System design
2. [Components](04-components/) - Component details
3. [Development Guides](05-guides/development/) - How to contribute
4. `.claude/skills/tdd/` - TDD workflow

### For LLM Integration
1. [LLM Components](04-components/llm/) - Provider abstractions
2. [Anthropic Reference](06-reference/providers/anthropic/) - API details
3. [Azure OpenAI](06-reference/providers/azure-openai/) - Authentication

---

## 📊 Project Status

**Completed Phases** (1-8):
- ✅ Foundation, LLM Integration, Agent Core
- ✅ Basic UI, MCP Integration, Enhanced UI
- ✅ Configuration UI, Anthropic Provider (95%)

**Current Phase** (9):
- 🚧 Interactive Teaching Mode Layer
- Status: Planning complete, ready for implementation

**Next Phase** (10):
- 📋 Polish & Refinement

See [Roadmap](07-planning/roadmap.md) for details.

---

## 🔧 Technology Stack

- **.NET 8.0** - Framework
- **C#** - Language
- **Blazor Server** - UI with SignalR
- **MSTest** - Testing (following Lean TDD via `.claude/skills/tdd/`)
- **MCP Protocol** - Tool integration

---

## 📝 Documentation Standards

All documentation follows strict standards to prevent misuse:

### Scope Headers
Every document includes a **scope header** defining:
- ✅ What belongs in this document
- ❌ What does NOT belong (with redirects)

**Why?** Prevents AI agents and humans from putting content in wrong places.

### Templates
Use templates from [08-contributing/templates/](08-contributing/templates/) for consistency:
- Component documentation
- Concept documentation
- How-to guides
- Design decisions

---

## 🤝 Contributing to Documentation

1. Choose appropriate template from [templates](08-contributing/templates/)
2. Keep the scope header - it's critical
3. Follow naming conventions (kebab-case)
4. Update this README if adding new categories

---

## 📞 Need Help?

- **Architecture questions**: See [02-architecture/](02-architecture/)
- **How-to questions**: See [05-guides/](05-guides/)
- **API questions**: See [06-reference/](06-reference/)
- **Can't find something**: Check the category READMEs above

---

## 🎉 Welcome!

This documentation is comprehensive, well-organized, and designed to help you understand and work with the TransparentAiAgent project efficiently.

**Happy exploring!** 🚀

---

**Documentation Design**: See [DOCUMENTATION_DESIGN_PROPOSAL.md](DOCUMENTATION_DESIGN_PROPOSAL.md) for the design rationale and migration details.
