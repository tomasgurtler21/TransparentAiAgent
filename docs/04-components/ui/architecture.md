# UI Architecture

**Last Updated**: 2025-11-09
**Status**: Active
**Phase**: Phase 4-9
**Layer**: Presentation

---

## Overview

The UI layer is implemented using **Blazor Server**, providing a real-time web interface for agent interactions with SignalR-based communication. The UI features an **overlay system** for Transparency, Tools, and Configuration panels that can be toggled independently.

## Architecture

**Framework**: Blazor Server (.NET 8)
**Communication**: SignalR (WebSockets)
**State Management**: Singleton UIControlService + Scoped services per connection
**Render Modes**: Mixed (Static SSR + Interactive Server components)

## Key Components

### Services

**ConversationUIService** - Bridges UI and AgentOrchestrator
- `SendMessageAsync()` - Process user input
- `GetMessagesAsync()` - Retrieve conversation history

**UIControlService** - Implements IUIControlService for dynamic UI control
- **Lifetime**: Singleton (shared across all render contexts)
- Manages UIState
- Fires UIStateChanged events
- Enables Teaching Mode and overlay control

### Layout Components

**MainLayout** - Static SSR layout container
- Hosts NavMenu and page content
- Delegates overlay rendering to OverlayContainer

**NavMenu** - Interactive navigation (InteractiveServer rendermode)
- Toggles overlay visibility via UIControlService
- Shows active overlay indicators

**OverlayContainer** - Interactive overlay manager (InteractiveServer rendermode)
- `TransparentAiAgentGui/Components/Layout/OverlayContainer.razor`
- Subscribes to UIControlService.UIStateChanged
- Renders all three overlays (Transparency, Tools, Configuration)
- Manages overlay visibility and animations
- Initializes JavaScript resize functionality

### Pages

- **Home** (`/`) - Main chat interface with overlay system
- **Configuration** - Settings editor (rendered in overlay)
- **Tools** - Tool discovery UI (rendered in overlay)
- **Transparency** - Event log viewer (rendered in overlay)

### Components

**Chat Components**:
- ChatInput - User input textbox
- MessageList - Scrollable message display
- MessageDisplay - Individual message rendering
- MessageFilterControls - Filter UI (user/assistant/tool messages)

**Transparency Components**:
- TransparencyViewer - Real-time event log (overlay)
- TransparencyEventDisplay - Individual event rendering

**Tool Components**:
- ToolsOverview - Tool list (overlay)
- ToolCard - Individual tool display
- ToolDetailsModal - Tool schema viewer

## Overlay System

### Architecture Decision

The overlay system uses a **mixed render mode architecture**:
- MainLayout: Static SSR (cannot have rendermode due to Body parameter)
- NavMenu: InteractiveServer (for button click handling)
- OverlayContainer: InteractiveServer (for state-driven re-rendering)

**Why**: MainLayout cannot use `@rendermode InteractiveServer` because it receives a `Body` parameter (RenderFragment), which cannot be serialized for interactive rendering. The solution is to extract overlay rendering into a separate interactive component.

### Component Structure

```
MainLayout (Static SSR)
├── NavMenu (InteractiveServer)
│   └── Toggle buttons update UIControlService
└── OverlayContainer (InteractiveServer)
    ├── Subscribes to UIControlService.UIStateChanged
    ├── Transparency Overlay (slide-in panel)
    ├── Tools Overlay (slide-in panel)
    └── Configuration Overlay (slide-in panel)
```

### UIControlService Lifetime

**Singleton** (not Scoped) to ensure NavMenu and OverlayContainer share the same instance across different render contexts.

**File**: `TransparentAiAgentGui/Program.cs:224`
```csharp
builder.Services.AddSingleton<IUIControlService, UIControlService>();
```

### Overlay CSS

Styles are in **global** `app.css` (not scoped CSS) to avoid bundling issues.

**File**: `TransparentAiAgentGui/wwwroot/app.css` (lines 53-117)

### Known Limitations

- **Overlay resize functionality**: Currently not working. See `docs/KnownIssues.md` for details.

## State Management

**UIState** (domain model) - Represents UI state
- ChatFilterState
- TransparencyViewerState (includes `Visible` property)
- ToolsPanelState (includes `Visible` property)
- ContextIndicatorsState
- ConfigurationPageState (includes `Visible` property)
- AppMode (Normal/Teaching)

**Default State**: All overlays start closed (`Visible = false`) to avoid stacking.

**UIControlService** - Manages state changes
- Singleton service (shared across render contexts)
- SignalR automatic UI updates for interactive components

## Teaching Mode Integration

UI Control Tools allow agent to manipulate UIState dynamically.

**Example**:
1. Agent calls `ui_control_transparency_viewer` with `visible: true`
2. UIControlService updates UIState
3. UIStateChanged event fires
4. OverlayContainer receives event and re-renders
5. Transparency overlay slides in

**Cross-Reference**: See `docs/03-concepts/teaching-mode/` for complete vision.

## Testing

**File**: `TransparentAiAgentGui_Tests/Services/UIControlServiceTests.cs`

---

## Related Documentation

- [UI Control Tools](../tools/builtin/ui-control-tools.md) - Agent UI manipulation
- [Teaching Mode](../../03-concepts/teaching-mode/) - Conceptual foundation
- [Blazor Hosting](../../06-reference/blazor/hosting-comparison.md) - Blazor Server details
- [Known Issues](../../KnownIssues.md) - Current limitations

---

**See Also**: [UI Overview](README.md)
