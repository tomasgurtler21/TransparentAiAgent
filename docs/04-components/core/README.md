# Core Application Components

**Last Updated**: 2025-12-04
**Status**: Active
**Layer**: Application

---

## Overview

Core Application components coordinate agent behavior and conversation flow. They orchestrate interactions between domain models, infrastructure services, and the LLM.

## Components

### [Agent Orchestrator](agent-orchestrator.md)
Main entry point for agent interactions.

**Responsibilities**:
- Process user input
- Coordinate LLM requests
- Execute tool call loops
- Integrate all subsystems

**Key Features**:
- Tool loop with depth protection (max 10)
- Transparency logging
- Error handling

---

### [Conversation Manager](conversation-manager.md)
Manages conversation history and context window.

**Responsibilities**:
- Maintain message history
- Track in-context vs archived messages
- Provide message retrieval

**Key Features**:
- Chronological message storage
- Context status events
- Future: Context summarization

---

### [Message Pipeline](message-pipeline.md)
Transforms messages between domain and LLM formats.

**Responsibilities**:
- Convert IMessage to LLMMessage
- Handle all message types
- Standardize format for providers

**Key Features**:
- Type-safe conversions
- Support for all message types (user, assistant, system, tool)

---

## Interaction Flow

```
User Input → Agent Orchestrator
    ├─ Add to Conversation Manager
    ├─ Get messages from Conversation Manager
    ├─ Convert via Message Pipeline
    ├─ Send to LLM Provider
    ├─ Handle response:
    │  ├─ If tool calls → Execute tools → Loop
    │  └─ If content → Add to Conversation Manager → Return
    └─ Log to Transparency Service
```

---

## Testing

**Files**:
- `TransparentAiAgentCore_Tests/Application/Agent/AgentOrchestratorTests.cs`
- `TransparentAiAgentCore_Tests/Application/Conversation/ConversationManagerTests.cs`
- `TransparentAiAgentCore_Tests/Application/Pipeline/MessagePipelineTests.cs`

---

## Related Documentation

- [LLM Providers](../llm/README.md) - LLM integration
- [Tools](../tools/README.md) - Tool execution
- [Infrastructure](../infrastructure/README.md) - Supporting services

---

**See Also**: [Component Overview](../README.md)
