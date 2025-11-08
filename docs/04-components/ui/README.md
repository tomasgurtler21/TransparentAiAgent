# UI Components

This directory contains documentation for Blazor UI components and presentation logic.

## Blazor Hosting Model

**Selected**: Blazor Server

See [ARCHITECTURE.md](../../ARCHITECTURE.md#blazor-hosting-model-options) for rationale.

## Components

### Chat Component
**File**: `ChatComponent.md` (to be created during implementation)

Main chat interface for user interaction.

**Key Responsibilities**:
- Display chat history
- Show streaming LLM responses in real-time
- Display tool calls formatted in JSON
- Handle user input
- Distinguish user vs LLM messages visually
- Integrate with state management

---

### Configuration Component
**File**: `ConfigurationComponent.md` (to be created during implementation)

Configuration editor UI (possibly separate tab).

**Key Responsibilities**:
- Display current configuration
- Edit system prompts
- Adjust LLM parameters (temperature, top-p)
- Manage tool availability
- Save configuration changes

---

### Tools Overview Component
**File**: `ToolsOverviewComponent.md` (to be created during implementation)

Display available tools and their schemas.

**Key Responsibilities**:
- List all registered tools
- Show tool input schemas
- Display tool descriptions
- Show tool usage history
- Format schemas for readability

---

### Transparency Viewer
**File**: `TransparencyViewer.md` (to be created during implementation)

Real-time transparency information display.

**Key Responsibilities**:
- Show current context
- Display transparency events in real-time
- Format structured data (JSON)
- Provide filtering/search of events
- Show context summarization settings

---

### State Management
**File**: `StateManagement.md` (to be created during implementation)

Blazor state management and SSE client.

**Key Responsibilities**:
- Manage application state
- Connect to SSE endpoints
- Handle real-time updates
- Coordinate component updates
- Manage SignalR connections (Blazor Server)

---

## UI Architecture

```
Blazor Server App
├── Pages/
│   ├── Index.razor (main chat page)
│   ├── Configuration.razor (config page)
│   └── Tools.razor (tools overview page)
├── Components/
│   ├── Chat.razor
│   ├── MessageDisplay.razor
│   ├── ToolCallDisplay.razor
│   ├── TransparencyPanel.razor
│   └── ConfigEditor.razor
├── Services/
│   ├── AppStateService.cs
│   ├── SseClientService.cs
│   └── AgentService.cs (API wrapper)
└── Shared/
    ├── MainLayout.razor
    └── NavMenu.razor
```

---

**Status**: Structure defined - detailed docs to be created during implementation
**Last Updated**: 2025-10-28
