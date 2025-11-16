# Built-in Tools

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 6-8
**Layer**: Infrastructure

---

## Overview

Built-in Tools are tools implemented directly in the agent codebase (not from external MCP servers). This includes **UI Control Tools** for Teaching Mode, **Knowledge Library Tool** for accessing teaching guardrails, and **Long-Term Memory Tools** for persistent context across sessions.

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

### [Knowledge Library Tool](knowledge-library-tool.md)

Tool that allows the agent to query curated knowledge entries for teaching guidance.

**Key Features**:
- Query knowledge entries by ID
- List available topics
- Knowledge gap likelihood awareness
- Guardrails for teaching

**Tools**:
- `knowledge_library_query` - Query knowledge entry by ID

**Status**: ✅ Implemented (Phase 9)

---

### [Long-Term Memory Tools](long-term-memory-tools.md)

Tools that enable the agent to read and update persistent memory across conversation sessions.

**Key Features**:
- Read mode-specific memory
- Update memory with validation
- Automatic mode awareness
- Security guardrails

**Tools**:
- `long_term_memory_read` - Read current mode's memory
- `long_term_memory_update` - Update current mode's memory

**Status**: ✅ Implemented (Phase 10)

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
| **Examples** | UI control, Knowledge Library, Memory | Filesystem, web search, databases |
| **SourceType** | `ToolSourceType.BuiltInUIControl`<br/>`ToolSourceType.BuiltInKnowledgeLibrary`<br/>`ToolSourceType.BuiltInLongTermMemory` | `ToolSourceType.MCP` |

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
- [Knowledge Library Tool](knowledge-library-tool.md) - Teaching guardrails tool
- [Long-Term Memory Tools](long-term-memory-tools.md) - Persistent memory tools
- [MCP Tools](../mcp/README.md) - External tool subsystem
- [Tool Manager](../tool-manager.md) - Execution routing
- [Teaching Mode](../../../03-concepts/teaching-mode/) - Conceptual motivation
- [Knowledge Library Concept](../../../03-concepts/knowledge-library.md) - Guardrails philosophy
- [Long-Term Memory Concept](../../../03-concepts/long-term-memory.md) - Memory overview

---

**See Also**: [Tools Overview](../README.md)
