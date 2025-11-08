# UI Control Tools

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 6-8
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- UIControlTool implementation (ITool for UI control)
- UIControlToolExecutor implementation
- Built-in UI control tool definitions
- Tool routing to IUIControlService

**What does NOT belong here**:
- ❌ Teaching Mode concept/vision → See `docs/03-concepts/teaching-mode/`
- ❌ IUIControlService interface → See `docs/04-components/infrastructure/` (domain interface)
- ❌ UIState model → See domain models docs
- ❌ MCP tools → See [../mcp/](../mcp/)

---

## Overview

UI Control Tools are built-in tools that enable the agent to dynamically control UI state. These tools implement the technical foundation for **Teaching Mode** by allowing the LLM to manipulate UI components programmatically.

**Cross-Reference**: See `docs/03-concepts/teaching-mode/` for the conceptual motivation and vision behind UI control capabilities.

## Purpose

- **Enable** agent to control UI dynamically via tool calls
- **Implement** ITool interface for built-in UI control operations
- **Route** tool calls to IUIControlService methods
- **Support** Teaching Mode feature (agent-guided UI interactions)

## Components

### UIControlTool

**File**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlTool.cs:9`

Implements ITool for UI control operations.

**Properties**:
- `Name` - Tool name (e.g., "ui_control_chat_filter")
- `Description` - Human-readable description
- `ParametersSchema` - JSON schema for arguments
- `SourceType` - Always `ToolSourceType.BuiltInUIControl`
- `Metadata` - Category metadata

### UIControlToolExecutor

**File**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutor.cs:14`

Implements IToolExecutor for UI control tools.

**Responsibilities**:
- Parse JSON arguments from tool calls
- Route to appropriate IUIControlService method
- Convert Result<UIState> to ToolExecutionResult
- Log execution details

**Scoping**: Registered as **Scoped** to share IUIControlService instance with UI components.

## Available Tools

### ui_control_chat_filter
Control chat message filtering (show/hide user, assistant, tool messages).

### ui_control_filter_visibility
Control visibility of filter controls.

### ui_get_state
Get current UI state (read-only inspection).

### ui_control_transparency_viewer
Control transparency viewer visibility/filtering.

###ui_control_tools_panel
Control tools panel visibility.

### ui_control_context_indicators
Control context indicator visibility.

### ui_control_configuration
Control configuration page state.

**Full definitions**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistry.cs`

## Architecture

```
LLM generates tool call
    ↓
AgentOrchestrator → ToolManager
    ↓
ToolManager routes to UIControlToolExecutor (based on SourceType)
    ↓
UIControlToolExecutor parses arguments
    ↓
Routes to IUIControlService method (e.g., UpdateChatFilter)
    ↓
IUIControlService updates UIState
    ↓
UIStateChanged event fires
    ↓
Blazor components re-render with new state
    ↓
UIControlToolExecutor returns ToolExecutionResult
    ↓
Result added to conversation
```

## Tool Execution Example

**LLM Tool Call** (JSON):
```json
{
  "name": "ui_control_chat_filter",
  "arguments": "{\"showUser\": true, \"showAssistant\": false, \"showToolMessages\": false}"
}
```

**Execution Flow**:
1. ToolManager receives call
2. Looks up tool by name from registry
3. Checks `tool.SourceType == BuiltInUIControl`
4. Routes to UIControlToolExecutor
5. Executor parses arguments
6. Calls `_uiControlService.UpdateChatFilter(true, false, false)`
7. UIState updated
8. Returns success ToolExecutionResult

## Teaching Mode Integration

UI Control Tools enable **Teaching Mode** - allowing the agent to guide users through the UI.

**Example Teaching Flow**:
1. Agent says: "Let me hide tool messages to simplify the view"
2. Agent calls: `ui_control_chat_filter` with `showToolMessages: false`
3. UI updates in real-time
4. User sees simplified chat view
5. Agent continues teaching with adjusted UI

**See**: `docs/03-concepts/teaching-mode/architecture.md` for complete Teaching Mode design.

## Related Documentation

- [Built-in Tools Overview](README.md) - Built-in tools subsystem
- [Tool Manager](../tool-manager.md) - Execution routing
- [Teaching Mode Concept](../../../03-concepts/teaching-mode/) - Motivation and vision
- [IUIControlService](../../infrastructure/) - Service interface (domain)

---

**See Also**: [Tools Overview](../README.md)
