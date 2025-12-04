# UI/Presentation Layer

**Last Updated**: 2025-12-04
**Status**: Active
**Layer**: Presentation

---

## Overview

The Presentation layer provides a Blazor Server web interface for interacting with the AI agent. It features an **overlay system** for chat, configuration, tools discovery, and transparency viewing with independent toggle controls.

## Key Documentation

### [UI Architecture](architecture.md)
Complete UI architecture, services, state management, overlay system, and Teaching Mode integration.

**Topics**:
- Blazor Server framework with mixed render modes
- SignalR communication
- UI services (ConversationUIService, UIControlService)
- State management (UIState)
- Overlay system architecture
- Component categories (Chat, Transparency, Tools)

---

## Component Categories

**Pages** (7 files):
- Home - Main chat interface with overlay system
- Configuration - Settings editor (rendered in overlay)
- Tools - Tool discovery (rendered in overlay)
- Transparency - Event viewer (rendered in overlay)

**Chat Components** (6 files):
- ChatInput, MessageList, MessageDisplay, MessageFilterControls, JsonDisplay, MarkdownDisplay

**Tool Components** (3 files):
- ToolsOverview, ToolCard, ToolDetailsModal

**Transparency Components** (2 files):
- TransparencyViewer, TransparencyEventDisplay

**Layout Components** (3 files):
- MainLayout - Static SSR layout container
- NavMenu - Interactive navigation with overlay toggles
- OverlayContainer - Interactive overlay manager (NEW)

---

## Services

**ConversationUIService**: Bridges UI and agent orchestrator

**UIControlService**: Singleton service enabling agent-driven UI control (Teaching Mode) and overlay management

---

## Overlay System

The UI features a **slide-in overlay system** for Transparency, Tools, and Configuration panels:
- Independent toggle controls in navigation menu
- Smooth animations (CSS transitions)
- State-driven visibility (UIControlService)
- All overlays start closed by default

**Architecture**: Mixed render modes (Static SSR + InteractiveServer) to handle MainLayout's Body parameter constraint.

**See**: [UI Architecture - Overlay System](architecture.md#overlay-system)

---

## Teaching Mode

UI Control Tools + UIState + UIControlService enable dynamic UI manipulation by the agent, including overlay visibility control.

**Cross-Reference**: See `docs/03-concepts/teaching-mode/`

---

## Known Issues

- Overlay resize functionality not working (see `docs/KnownIssues.md`)

---

## Related Documentation

- [UI Architecture](architecture.md) - Complete architecture
- [UI Control Tools](../tools/builtin/ui-control-tools.md) - Agent UI control
- [Blazor Reference](../../06-reference/blazor/) - Blazor Server details
- [Known Issues](../../KnownIssues.md) - Current limitations

---

**See Also**: [Component Overview](../README.md)
