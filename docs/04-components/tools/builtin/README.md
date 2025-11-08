# Built-in Tools

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 6-8
**Layer**: Infrastructure

---

## Overview

Built-in Tools are tools implemented directly in the agent codebase (not from external MCP servers). Currently includes **UI Control Tools** that enable agent-driven UI manipulation for Teaching Mode.

## Components

### [UI Control Tools](ui-control-tools.md)

Tools that allow the agent to dynamically control UI state.

**Key Features**:
- 7 UI control tools (chat filters, transparency viewer, etc.)
- Routing to IUIControlService
- Real-time UI updates
- Teaching Mode foundation

**Tools**:
- `ui_control_chat_filter` - Control message filtering
- `ui_control_filter_visibility` - Show/hide filters
- `ui_get_state` - Get current UI state
- `ui_control_transparency_viewer` - Control transparency viewer
- `ui_control_tools_panel` - Control tools panel
- `ui_control_context_indicators` - Control context indicators
- `ui_control_configuration` - Control configuration page

---

## Architecture

```
BuiltInUIControlToolRegistry (implements IToolRegistry)
    ↓
Registers 7 UI control tools (UIControlTool instances)
    ↓
ToolRegistryComposite aggregates with MCPToolRegistry
    ↓
ToolManager routes execution
    ↓
UIControlToolExecutor (implements IToolExecutor)
    ├─ Parses tool arguments
    ├─ Routes to IUIControlService
    └─ Returns ToolExecutionResult
```

---

## vs. MCP Tools

| Aspect | Built-in Tools | MCP Tools |
|--------|----------------|-----------|
| **Source** | Agent codebase | External MCP servers |
| **Discovery** | Registered at startup | Discovered from servers |
| **Execution** | Direct C# method calls | MCP protocol over stdio |
| **Examples** | UI control | Filesystem, web search, databases |
| **SourceType** | `ToolSourceType.BuiltInUIControl` | `ToolSourceType.MCP` |

---

## Future Built-in Tools

Potential future additions:
- `reset_conversation` - Clear conversation history
- `export_conversation` - Export chat to file
- `get_system_info` - Agent capabilities introspection
- `configure_llm` - Runtime LLM parameter adjustment

**Extension Point**: Add new tools by:
1. Create new BuiltInTool implementation
2. Register in appropriate registry
3. Implement executor logic
4. Document in this subsystem

---

## Teaching Mode Connection

Built-in UI Control Tools enable **Teaching Mode** - a core concept where the agent guides users through the UI by programmatically manipulating interface elements.

**Cross-Reference**: See `docs/03-concepts/teaching-mode/` for the complete vision and architecture of Teaching Mode.

---

## Testing

### Unit Tests

**Files**:
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistryTests.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutorTests.cs`

---

## Related Documentation

- [UI Control Tools](ui-control-tools.md) - Detailed UI tool docs
- [MCP Tools](../mcp/README.md) - External tool subsystem
- [Tool Manager](../tool-manager.md) - Execution routing
- [Teaching Mode](../../../03-concepts/teaching-mode/) - Conceptual motivation

---

**See Also**: [Tools Overview](../README.md)
