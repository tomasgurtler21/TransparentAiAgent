# Agent Orchestrator

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 3
**Layer**: Application

---

## Document Scope

**What belongs in this document**:
- AgentOrchestrator implementation
- Agent conversation flow orchestration
- Tool call loop handling
- Integration of all major subsystems

**What does NOT belong here**:
- ❌ LLM provider details → See [../llm/](../llm/)
- ❌ Tool execution details → See [../tools/](../tools/)
- ❌ Conversation management → See [conversation-manager.md](conversation-manager.md)

---

## Overview

AgentOrchestrator is the main entry point for agent interactions. It coordinates conversation flow, LLM requests, tool execution loops, and integrates all major subsystems (conversation, LLM, tools, transparency).

## Purpose

- **Coordinate** end-to-end user interaction flow
- **Manage** tool call loops (LLM → tools → LLM)
- **Integrate** conversation manager, LLM provider, tool manager, transparency
- **Handle** system prompt initialization
- **Protect** against infinite tool loops

## Responsibilities

- Process user input messages
- Build LLM requests from conversation history
- Handle LLM responses (content + tool calls)
- Execute tool call loops with depth protection
- Add messages to conversation manager
- Log all operations to transparency service
- Handle errors and exceptions

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs:20`

```csharp
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ILLMProvider _llmProvider;
    private readonly IConversationManager _conversationManager;
    private readonly IMessagePipeline _messagePipeline;
    private readonly ITransparencyService _transparencyService;
    private readonly IToolManager? _toolManager;
    private const int MaxToolCallDepth = 10;
}
```

### Dependencies

**Depends on**:
- `Domain/LLM/ILLMProvider` - LLM communication
- `Application/Conversation/IConversationManager` - Conversation state
- `Application/Pipeline/IMessagePipeline` - Message transformations
- `Infrastructure/Transparency/ITransparencyService` - Event logging
- `Application/Tools/IToolManager` - Tool execution (optional)
- `Domain/Configuration/AppConfiguration` - Agent config

**Used by**:
- `TransparentAiAgentGui/Services/ConversationUIService.cs` - UI layer

## Key Flows

### User Input Processing

**Method**: `ProcessUserInputAsync` - `AgentOrchestrator.cs:60`

**Flow**:
1. Validate user input
2. Create UserMessage and add to conversation
3. Enter tool call loop (depth 0)
4. Return final assistant message

### Tool Call Loop

**Method**: `ProcessWithToolLoopAsync` - `AgentOrchestrator.cs:86`

**Flow**:
1. **Check depth** → If >= 10, return error message
2. **Build LLM request** from conversation history
3. **Send to LLM** via provider
4. **Check response**:
   - If tool calls present → Execute tools, recurse (depth+1)
   - If no tool calls → Parse content, add to conversation, return
5. **Handle errors** → Log and throw AgentException

**Depth Protection**: Prevents infinite loops by limiting to 10 tool calls per user input.

### Tool Execution

**Method**: `ExecuteToolCallsAsync` - `AgentOrchestrator.cs:~200`

**Flow** (for each tool call):
1. Log tool call to transparency
2. Execute via ToolManager
3. Create ToolResultMessage
4. Add to conversation
5. Log result

## Critical Logic: Tool Loop

```csharp
// Recursive tool loop with depth protection
private async Task<IMessage> ProcessWithToolLoopAsync(int depth, CancellationToken ct)
{
    if (depth >= MaxToolCallDepth)
        return ErrorMessage("Max depth reached");

    var llmResponse = await _llmProvider.SendRequestAsync(BuildLLMRequest());

    if (llmResponse.ToolCalls != null && llmResponse.ToolCalls.Count > 0)
    {
        await ExecuteToolCallsAsync(llmResponse.ToolCalls);
        return await ProcessWithToolLoopAsync(depth + 1, ct); // Recurse
    }
    else
    {
        return ParseAndAddAssistantMessage(llmResponse.Content);
    }
}
```

**Why Recursive**: Allows multiple tool call rounds (LLM → tool → LLM → tool → final response).

## Initialization

**Constructor**:
1. Inject all dependencies
2. Validate configuration
3. Add system prompt to conversation
4. Log initialization event

**System Prompt**: Added as first message in conversation (from `AgentConfiguration.SystemPrompt`).

## Error Handling

- Wraps unexpected exceptions in `AgentException`
- Preserves LLMException and AgentException as-is
- Logs all errors to transparency service
- Returns user-friendly error messages

## Transparency Logging

**Events logged**:
- AgentInitialized
- UserInput
- LLMRequestSent
- LLMResponseReceived
- ToolCallExecuted
- ToolLoopMaxDepthReached
- Error

**Purpose**: Complete audit trail of agent behavior.

## Testing

**File**: `TransparentAiAgentCore_Tests/Application/Agent/AgentOrchestratorTests.cs`

**Tests**:
- User input processing
- Tool call loop (single and multiple rounds)
- Max depth protection
- Error handling
- Message addition to conversation

## Usage

### Integration Point (UI Service)

```csharp
// UI service calls orchestrator
var response = await _orchestrator.ProcessUserInputAsync(userInput);

// Get conversation history
var messages = _orchestrator.ConversationManager.GetInContextMessages();
```

## Related Documentation

- [Conversation Manager](conversation-manager.md) - State management
- [Message Pipeline](message-pipeline.md) - Message transformations
- [Tool Manager](../tools/tool-manager.md) - Tool execution
- [LLM Providers](../llm/README.md) - LLM communication

---

**See Also**: [Core Components Overview](README.md)
