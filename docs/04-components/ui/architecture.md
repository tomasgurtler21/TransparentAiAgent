# UI Architecture

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 4-8
**Layer**: Presentation

---

## Overview

The UI layer is implemented using **Blazor Server**, providing a real-time web interface for agent interactions with SignalR-based communication.

## Architecture

**Framework**: Blazor Server (.NET 8)
**Communication**: SignalR (WebSockets)
**State Management**: Scoped services per connection

## Key Components

### Services

**ConversationUIService** - Bridges UI and AgentOrchestrator
- `SendMessageAsync()` - Process user input
- `GetMessagesAsync()` - Retrieve conversation history

**UIControlService** - Implements IUIControlService for dynamic UI control
- Manages UIState
- Fires UIStateChanged events
- Enables Teaching Mode

### Pages

- **Home** (`/`) - Main chat interface
- **Configuration** (`/configuration`) - Settings editor
- **Tools** (`/tools`) - Tool discovery UI
- **Transparency** (`/transparency`) - Event log viewer

### Components

**Chat Components**:
- ChatInput - User input textbox
- MessageList - Scrollable message display
- MessageDisplay - Individual message rendering
- MessageFilterControls - Filter UI (user/assistant/tool messages)

**Transparency Components**:
- TransparencyViewer - Real-time event log
- TransparencyEventDisplay - Individual event rendering

**Tool Components**:
- ToolsOverview - Tool list
- ToolCard - Individual tool display
- ToolDetailsModal - Tool schema viewer

## State Management

**UIState** (domain model) - Represents UI state
- ChatFilterState
- TransparencyViewerState
- ToolsPanelState
- ContextIndicatorsState
- ConfigurationPageState
- AppMode (Normal/Teaching)

**UIControlService** - Manages state changes
- Per-connection scoped service
- SignalR automatic UI updates

## Teaching Mode Integration

UI Control Tools allow agent to manipulate UIState dynamically.

**Example**:
1. Agent calls `ui_control_chat_filter`
2. UIControlService updates UIState
3. UIStateChanged event fires
4. Blazor components re-render automatically

**Cross-Reference**: See `docs/03-concepts/teaching-mode/` for complete vision.

## Testing

**File**: `TransparentAiAgentGui_Tests/Services/UIControlServiceTests.cs`

---

## Related Documentation

- [UI Control Tools](../tools/builtin/ui-control-tools.md) - Agent UI manipulation
- [Teaching Mode](../../03-concepts/teaching-mode/) - Conceptual foundation
- [Blazor Hosting](../../06-reference/blazor/hosting-comparison.md) - Blazor Server details

---

**See Also**: [UI Overview](README.md)
