# UI/Presentation Layer

**Last Updated**: 2025-11-08
**Status**: Active
**Layer**: Presentation

---

## Overview

The Presentation layer provides a Blazor Server web interface for interacting with the AI agent. It includes chat, configuration, tools discovery, and transparency viewing.

## Key Documentation

### [UI Architecture](architecture.md)
Complete UI architecture, services, state management, and Teaching Mode integration.

**Topics**:
- Blazor Server framework
- SignalR communication
- UI services (ConversationUIService, UIControlService)
- State management (UIState)
- Component categories (Chat, Transparency, Tools)

---

## Component Categories

**Pages** (7 files):
- Home - Main chat interface
- Configuration - Settings editor
- Tools - Tool discovery
- Transparency - Event viewer

**Chat Components** (6 files):
- ChatInput, MessageList, MessageDisplay, MessageFilterControls, JsonDisplay, MarkdownDisplay

**Tool Components** (3 files):
- ToolsOverview, ToolCard, ToolDetailsModal

**Transparency Components** (2 files):
- TransparencyViewer, TransparencyEventDisplay

**Layout Components** (2 files):
- MainLayout, NavMenu

---

## Services

**ConversationUIService**: Bridges UI and agent orchestrator

**UIControlService**: Enables agent-driven UI control (Teaching Mode)

---

## Teaching Mode

UI Control Tools + UIState + UIControlService enable dynamic UI manipulation by the agent.

**Cross-Reference**: See `docs/03-concepts/teaching-mode/`

---

## Related Documentation

- [UI Architecture](architecture.md) - Complete architecture
- [UI Control Tools](../tools/builtin/ui-control-tools.md) - Agent UI control
- [Blazor Reference](../../06-reference/blazor/) - Blazor Server details

---

**See Also**: [Component Overview](../README.md)
