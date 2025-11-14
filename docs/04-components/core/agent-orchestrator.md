# Agent Orchestrator

**Last Updated**: 2025-11-14
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

- Process user input messages (via ProcessUserInputAsync/ProcessUserInputStreamingAsync)
- Process application-originated messages (via ProcessApplicationMessageAsync)
- Process tool-originated messages (via ProcessToolMessageAsync)
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

**Methods**:
- `ProcessUserInputAsync(UserMessage)` - Non-streaming version
- `ProcessUserInputStreamingAsync(UserMessage)` - Streaming version

**Flow**:
1. Validate user message
2. Add UserMessage (typically DirectUserMessage) to conversation
3. Enter tool call loop (depth 0)
4. Return final assistant message (LlmTextMessage)

### Application Message Processing

**Method**: `ProcessApplicationMessageAsync(ApplicationMessage)` - Streaming

**Flow**:
1. Add ApplicationMessage to conversation
2. Route based on message subtype:
   - **ScenarioUserMessage**: Trigger LLM processing (like user input)
   - **ScenarioAssistantMessage**: Skip LLM, just yield completion chunk
3. Return streaming response chunks

### Tool Message Processing

**Method**: `ProcessToolMessageAsync(ToolMessage)` - Streaming

**Flow**:
1. Add ToolMessage (ToolResultMessage or ToolErrorMessage) to conversation
2. Trigger LLM processing to continue tool loop
3. Return streaming response chunks

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

**Method**: `ExecuteToolCallsAsync`

**Flow** (for each tool call):
1. Log tool call to transparency
2. Execute via ToolManager
3. Create appropriate message:
   - **Success**: ToolResultMessage with result
   - **Error**: ToolErrorMessage with error details
4. Add to conversation
5. Log result

## Message Architecture

**Four-Tier Message Hierarchy** (as of Phase 6.2):

The system uses a four-tier message hierarchy based on message **origin**:

1. **UserMessage** (abstract) - Messages from human user input
   - `DirectUserMessage` - Standard user-typed messages

2. **LlmMessage** (abstract) - Messages from LLM responses
   - `LlmTextMessage` - Text responses from LLM
   - `LlmToolCallMessage` - Tool call requests from LLM

3. **ApplicationMessage** (abstract) - Messages from application logic
   - `ScenarioUserMessage` - Scenario-generated user messages
   - `ScenarioAssistantMessage` - Scenario-generated assistant messages

4. **ToolMessage** (abstract) - Messages from tool execution
   - `ToolResultMessage` - Successful tool execution results
   - `ToolErrorMessage` - Tool execution errors

**Why This Matters**: The orchestrator has stable APIs (`ProcessUserInputAsync`, `ProcessApplicationMessageAsync`, `ProcessToolMessageAsync`) that accept abstract base types, allowing unlimited extensibility without API changes.

**See**: MESSAGE_ARCHITECTURE_REDESIGN.md for full design rationale.

## Critical Logic: Tool Loop

```csharp
// Recursive tool loop with depth protection
private async IAsyncEnumerable<ProcessingChunk> ProcessStreamingToolLoopAsync(
    int depth,
    [EnumeratorCancellation] CancellationToken ct)
{
    if (depth >= MaxToolCallDepth)
    {
        var errorMsg = new LlmTextMessage("Maximum tool call depth reached");
        _conversationManager.AddMessage(errorMsg);
        yield return new CompletionChunk(errorMsg.Content);
        yield break;
    }

    var llmResponse = await _llmProvider.SendRequestAsync(BuildLLMRequest());

    if (llmResponse.ToolCalls != null && llmResponse.ToolCalls.Count > 0)
    {
        var toolCallMsg = new LlmToolCallMessage(llmResponse.Content ?? "", llmResponse.ToolCalls);
        _conversationManager.AddMessage(toolCallMsg);

        await ExecuteToolCallsAsync(llmResponse.ToolCalls); // Creates ToolResultMessage or ToolErrorMessage

        await foreach (var chunk in ProcessStreamingToolLoopAsync(depth + 1, ct))
        {
            yield return chunk;
        }
    }
    else
    {
        var textMsg = new LlmTextMessage(llmResponse.Content ?? "");
        _conversationManager.AddMessage(textMsg);
        yield return new CompletionChunk(textMsg.Content);
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
// Process user-typed message (streaming)
var userMessage = new DirectUserMessage(userInput);
await foreach (var chunk in _orchestrator.ProcessUserInputStreamingAsync(userMessage))
{
    // Handle streaming chunks
}

// Process scenario message (streaming)
var scenarioMsg = new ScenarioUserMessage("Hello", "Teaching step 1");
await foreach (var chunk in _orchestrator.ProcessApplicationMessageAsync(scenarioMsg))
{
    // Handle streaming chunks
}

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
