# Message Hierarchy - Four-Tier Architecture

**Last Updated**: 2025-11-14
**Status**: Implemented (Phase 6.2)
**Related**: MESSAGE_ARCHITECTURE_REDESIGN.md, MESSAGE_ARCHITECTURE_IMPLEMENTATION_PLAN.md

---

## Document Scope

**What belongs in this document**:
- Overview of the four-tier message hierarchy
- Message origin categories
- Design rationale and benefits
- Common usage patterns

**What does NOT belong here**:
- ❌ Implementation details → See component documentation
- ❌ Serialization specifics → See [Serialization Service](../04-components/infrastructure/serialization-service.md)
- ❌ Full design document → See MESSAGE_ARCHITECTURE_REDESIGN.md in project root

---

## Overview

The TransparentAiAgent uses a **four-tier message hierarchy** that categorizes all messages by their **origin** rather than just their LLM role. This creates a stable, extensible architecture that scales as new features are added.

## The Four Message Origins

### 1. UserMessage (Abstract)
**Origin**: Human user input
**Purpose**: Represents direct human interaction with the agent

**Concrete Types**:
- `DirectUserMessage` - Standard user-typed messages

**Rule**: Use ONLY when user typed text, used voice input, or pasted text into chat. DO NOT use for system-generated messages even if user-initiated.

**Discriminator Pattern**: `"User.{ConcreteType}"` (e.g., `"User.Direct"`)

---

### 2. LlmMessage (Abstract)
**Origin**: LLM responses
**Purpose**: Represents AI agent outputs

**Concrete Types**:
- `LlmTextMessage` - Text responses from LLM
- `LlmToolCallMessage` - Tool call requests from LLM

**Discriminator Pattern**: `"Llm.{ConcreteType}"` (e.g., `"Llm.Text"`, `"Llm.ToolCall"`)

---

### 3. ApplicationMessage (Abstract)
**Origin**: Application logic
**Purpose**: Represents system-driven actions, scenarios, automation, workflows

**Concrete Types**:
- `ScenarioUserMessage` - Scenario-generated messages with user role
- `ScenarioAssistantMessage` - Scenario-generated messages with assistant role

**Note**: "Application" is used instead of "System" to avoid confusion with LLM system prompts.

**Rule**: Use when system translated user action into message, or application logic generated content.

**Discriminator Pattern**: `"Application.{ConcreteType}"` (e.g., `"Application.ScenarioUser"`)

---

### 4. ToolMessage (Abstract)
**Origin**: Tool execution infrastructure
**Purpose**: Represents results from MCP servers and built-in tools

**Concrete Types**:
- `ToolResultMessage` - Successful tool execution results
- `ToolErrorMessage` - Tool execution errors

**Note**: Tools are external operations (file system, git, network, etc.) - distinct from app-internal logic.

**Discriminator Pattern**: `"Tool.{ConcreteType}"` (e.g., `"Tool.Result"`, `"Tool.Error"`)

---

## Design Benefits

### 1. Stable Orchestrator API
The `AgentOrchestrator` has only **three processing methods** that accept abstract base types:
- `ProcessUserInputAsync(UserMessage)` - Accepts any UserMessage subtype
- `ProcessApplicationMessageAsync(ApplicationMessage)` - Accepts any ApplicationMessage subtype
- `ProcessToolMessageAsync(ToolMessage)` - Accepts any ToolMessage subtype

**This API never grows**, no matter how many new message subtypes are added.

### 2. Unlimited Extensibility
New features add message subtypes, not API methods:
- ✅ Add `VoiceUserMessage : UserMessage` → Works with existing API
- ✅ Add `WorkflowMessage : ApplicationMessage` → Works with existing API
- ✅ Add `LlmReasoningMessage : LlmMessage` → Works with existing API

### 3. Clear Separation
Message origin is explicit in the type system:
- `DirectUserMessage` → Clearly from human
- `ScenarioUserMessage` → Clearly from application
- Both have "user" role, but different origins are obvious

### 4. Serialization Support
Every message has a `MessageTypeDiscriminator` property for polymorphic serialization:
```csharp
var json = MessageSerializer.Serialize(message);
var restored = MessageSerializer.Deserialize(json); // Returns correct concrete type
```

---

## Common Patterns

### Creating Messages

```csharp
// User-typed message
var userMsg = new DirectUserMessage("Hello, agent!");

// Scenario-generated message
var scenarioMsg = new ScenarioUserMessage("What are tool calls?", "Teaching step 3");

// LLM text response
var llmMsg = new LlmTextMessage("Tool calls allow me to...");

// Tool result
var toolResult = new ToolResultMessage(toolCallId, "get_weather", "{\"temp\":72}", false);

// Tool error
var toolError = new ToolErrorMessage(toolCallId, "get_weather", "API timeout", exception);
```

### Processing Messages

```csharp
// In UI layer
var userMessage = new DirectUserMessage(userInput);
await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync(userMessage))
{
    // Handle streaming chunks
}

// In scenario executor
var scenarioMessage = new ScenarioUserMessage(content, annotation);
await foreach (var chunk in orchestrator.ProcessApplicationMessageAsync(scenarioMessage))
{
    // Handle streaming chunks
}

// After tool execution
var toolMessage = new ToolResultMessage(toolCallId, toolName, result, false);
await foreach (var chunk in orchestrator.ProcessToolMessageAsync(toolMessage))
{
    // Handle streaming chunks
}
```

### Type Checking

```csharp
// Check message origin
if (message is UserMessage userMsg)
{
    // Handle any user-originated message
}

if (message is DirectUserMessage directMsg)
{
    // Handle specifically user-typed messages
}

if (message is ApplicationMessage appMsg)
{
    // Handle any application-generated message
    var annotation = appMsg is ScenarioUserMessage sMsg ? sMsg.Annotation : null;
}
```

---

## Message Role vs. Origin

**Key Concept**: Message **role** (for LLM) is separate from message **origin** (for system architecture).

| Message Type | Origin | Role |
|--------------|--------|------|
| DirectUserMessage | Human | User |
| ScenarioUserMessage | Application | User |
| LlmTextMessage | LLM | Assistant |
| LlmToolCallMessage | LLM | Assistant |
| ScenarioAssistantMessage | Application | Assistant |
| ToolResultMessage | Tool | Tool |
| ToolErrorMessage | Tool | Tool |
| SystemMessage | N/A | System |

**Why This Matters**:
- **Role**: What the LLM sees (`"user"`, `"assistant"`, `"tool"`, `"system"`)
- **Origin**: Where the message came from (Human, LLM, App, Tool)
- **UI Display**: Based on origin (show annotations for scenarios, etc.)
- **Orchestrator Routing**: Based on origin (different processing paths)

---

## Migration Notes

**Before (Phase < 6.2)**:
```csharp
// Old flat structure
var userMsg = new UserMessage("Hello");  // Ambiguous origin
var assistantMsg = new AssistantMessage("Hi!");  // Ambiguous origin
```

**After (Phase 6.2)**:
```csharp
// New hierarchical structure
var userMsg = new DirectUserMessage("Hello");  // Clear: from human
var assistantMsg = new LlmTextMessage("Hi!");  // Clear: from LLM
var scenarioMsg = new ScenarioUserMessage("Hello", "Step 1");  // Clear: from app
```

---

## Related Documentation

- [Agent Orchestrator](../04-components/core/agent-orchestrator.md) - Processing methods
- [Message Pipeline](../04-components/core/message-pipeline.md) - Role conversion
- [Serialization Service](../04-components/infrastructure/serialization-service.md) - Discriminator-based serialization
- MESSAGE_ARCHITECTURE_REDESIGN.md (project root) - Full design document
- MESSAGE_ARCHITECTURE_IMPLEMENTATION_PLAN.md (project root) - Implementation history

---

**See Also**: [Concepts Overview](README.md)
