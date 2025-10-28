# Phase 3: Agent Core - Detailed Implementation Plan

**Status**: Implementation Ready
**Last Updated**: 2025-10-28
**Dependencies**: Phase 1 and Phase 2 must be complete

## Overview

This document provides a comprehensive, class-by-class implementation plan for Phase 3 using Test-Driven Development (TDD) with **Lean TDD** principles.

## Goals

Build the core agent orchestration logic that manages conversations and coordinates LLM interactions:
- Conversation management with full chat history
- Context window management with truncation tracking
- Agent orchestration loop for multi-turn conversations
- Message pipeline for transformations between domain models and LLM models
- Integration with Transparency System for complete visibility
- Multi-turn conversation capability

## Architecture Layer: Domain + Application

Phase 3 focuses on:
- **Domain Layer**: Conversation interfaces, orchestration abstractions
- **Application Layer**: Conversation Manager, Agent Orchestrator, Message Pipeline

## Lean TDD Approach

**IMPORTANT**: We follow **Lean TDD**, not pedantic testing.

### What We DO Test:
✅ **Business Logic & Behavior**
- Conversation history management (add, retrieve, truncate)
- Context status tracking (InContext vs TruncatedFromContext)
- Truncation logic (when and which messages are truncated)
- Message transformation logic (domain models → LLM models)
- Agent orchestration loop behavior
- State management during conversations
- Error handling and edge cases
- Integration between components

### What We DON'T Test:
❌ **Compiler-Enforced or Trivial Features**
- Simple property getters/setters with no logic
- Interface definitions without logic
- Framework features

---

## Components & Implementation Order

### 1. Conversation Management (Application Layer)

#### 1.1 Conversation Manager Interface

##### `IConversationManager` Interface
```csharp
namespace TransparentAiAgentCore.Application.Conversation;

using TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Manages conversation history and context window
/// </summary>
public interface IConversationManager
{
    /// <summary>
    /// Add a message to conversation history
    /// </summary>
    void AddMessage(IMessage message);

    /// <summary>
    /// Get all messages in conversation history (including truncated ones)
    /// </summary>
    IReadOnlyList<IMessage> GetAllMessages();

    /// <summary>
    /// Get only messages currently in LLM context window
    /// </summary>
    IReadOnlyList<IMessage> GetInContextMessages();

    /// <summary>
    /// Get conversation ID
    /// </summary>
    Guid ConversationId { get; }

    /// <summary>
    /// Get context window size limit
    /// </summary>
    int ContextWindowSize { get; }

    /// <summary>
    /// Get current count of messages in context
    /// </summary>
    int InContextMessageCount { get; }

    /// <summary>
    /// Clear all messages from conversation
    /// </summary>
    void ClearConversation();

    /// <summary>
    /// Event raised when a message's context status changes
    /// </summary>
    event EventHandler<ContextStatusChangedEventArgs>? ContextStatusChanged;
}
```

**Tests**: ❌ NO TESTS NEEDED (interface definition)

---

##### `ContextStatusChangedEventArgs` Class
```csharp
namespace TransparentAiAgentCore.Application.Conversation;

using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;

/// <summary>
/// Event args for context status changes
/// </summary>
public class ContextStatusChangedEventArgs : EventArgs
{
    public Guid MessageId { get; }
    public MessageContextStatus OldStatus { get; }
    public MessageContextStatus NewStatus { get; }

    public ContextStatusChangedEventArgs(
        Guid messageId,
        MessageContextStatus oldStatus,
        MessageContextStatus newStatus)
    {
        MessageId = messageId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
```

**Tests**: ❌ NO TESTS NEEDED (simple data holder)

---

#### 1.2 Conversation Manager Implementation

##### `ConversationManager` Class
```csharp
namespace TransparentAiAgentCore.Application.Conversation;

using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Domain.Transparency;

/// <summary>
/// Manages conversation history and context window with simple truncation strategy
/// </summary>
public class ConversationManager : IConversationManager
{
    private readonly List<IMessage> _messages = new();
    private readonly object _lock = new();
    private readonly ITransparencyService _transparencyService;

    public Guid ConversationId { get; }
    public int ContextWindowSize { get; }
    public int InContextMessageCount => GetInContextMessages().Count;

    public event EventHandler<ContextStatusChangedEventArgs>? ContextStatusChanged;

    public ConversationManager(
        int contextWindowSize,
        ITransparencyService transparencyService)
    {
        if (contextWindowSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(contextWindowSize),
                "Context window size must be greater than 0");

        _transparencyService = transparencyService
            ?? throw new ArgumentNullException(nameof(transparencyService));

        ConversationId = Guid.NewGuid();
        ContextWindowSize = contextWindowSize;

        LogEvent("ConversationStarted", $"Conversation {ConversationId} started with context window size {contextWindowSize}");
    }

    public void AddMessage(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        lock (_lock)
        {
            _messages.Add(message);

            // After adding, check if we need to truncate
            TruncateIfNeeded();

            LogEvent("MessageAdded", $"Message {message.Id} added to conversation. Total: {_messages.Count}, In context: {InContextMessageCount}");
        }
    }

    public IReadOnlyList<IMessage> GetAllMessages()
    {
        lock (_lock)
        {
            return _messages.ToList(); // Return defensive copy
        }
    }

    public IReadOnlyList<IMessage> GetInContextMessages()
    {
        lock (_lock)
        {
            return _messages
                .Where(m => m.ContextStatus == MessageContextStatus.InContext)
                .ToList();
        }
    }

    public void ClearConversation()
    {
        lock (_lock)
        {
            _messages.Clear();
            LogEvent("ConversationCleared", $"Conversation {ConversationId} cleared");
        }
    }

    /// <summary>
    /// Truncate oldest messages if we exceed context window size.
    /// Strategy: Remove oldest InContext messages first, keeping system messages if possible.
    /// </summary>
    private void TruncateIfNeeded()
    {
        var inContextMessages = _messages
            .Where(m => m.ContextStatus == MessageContextStatus.InContext)
            .ToList();

        if (inContextMessages.Count <= ContextWindowSize)
            return; // No truncation needed

        // How many messages to truncate
        int toTruncate = inContextMessages.Count - ContextWindowSize;

        // Get messages to truncate (oldest first, but prefer non-system messages)
        var messagesToTruncate = inContextMessages
            .OrderBy(m => m.Role == MessageRole.System ? 1 : 0) // System messages last priority for truncation
            .ThenBy(m => m.Timestamp) // Oldest first
            .Take(toTruncate)
            .ToList();

        foreach (var message in messagesToTruncate)
        {
            var oldStatus = message.ContextStatus;
            message.ContextStatus = MessageContextStatus.TruncatedFromContext;

            // Raise event
            ContextStatusChanged?.Invoke(
                this,
                new ContextStatusChangedEventArgs(message.Id, oldStatus, message.ContextStatus));

            LogEvent("MessageTruncated", $"Message {message.Id} truncated from context");
        }

        LogEvent("ContextTruncated", $"{toTruncate} messages truncated. In context: {InContextMessageCount}/{ContextWindowSize}");
    }

    private void LogEvent(string eventType, string details)
    {
        _transparencyService?.LogEvent(
            new TransparencyEvent(
                TransparencyEventType.ContextChange,
                System.Text.Json.JsonSerializer.Serialize(new { ConversationId, EventType = eventType }),
                details));
    }
}
```

**Tests** (Lean - Focus on Business Logic):

**Constructor Tests:**
- ✅ Constructor throws ArgumentOutOfRangeException for contextWindowSize <= 0
- ✅ Constructor throws ArgumentNullException for null transparencyService
- ✅ Constructor generates unique ConversationId
- ✅ Constructor logs conversation started event

**Message Management Tests:**
- ✅ AddMessage throws ArgumentNullException for null message
- ✅ AddMessage adds message to history
- ✅ AddMessage logs message added event
- ✅ GetAllMessages returns all messages including truncated ones
- ✅ GetAllMessages returns defensive copy (modifying result doesn't affect internal list)
- ✅ GetInContextMessages returns only messages with InContext status
- ✅ GetInContextMessages excludes messages with TruncatedFromContext status
- ✅ InContextMessageCount returns correct count

**Truncation Logic Tests (Critical Business Logic):**
- ✅ AddMessage does not truncate when under context window size
- ✅ AddMessage truncates oldest message when exceeding context window size
- ✅ Truncation updates message ContextStatus to TruncatedFromContext
- ✅ Truncation raises ContextStatusChanged event with correct details
- ✅ Truncation logs truncated events to transparency service
- ✅ Truncation prefers to keep system messages (truncates non-system first)
- ✅ Truncation respects timestamp order (oldest first among same priority)
- ✅ Multiple messages truncated when needed (e.g., context size = 3, add 5 messages)
- ✅ Truncated messages remain in GetAllMessages but not in GetInContextMessages

**Other Tests:**
- ✅ ClearConversation removes all messages
- ✅ ClearConversation logs cleared event
- ✅ Thread safety: concurrent AddMessage calls don't corrupt state

---

### 2. Message Transformation (Application Layer)

#### 2.1 Message Pipeline Interface

##### `IMessagePipeline` Interface
```csharp
namespace TransparentAiAgentCore.Application.Pipeline;

using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Transforms messages between domain models and LLM models
/// </summary>
public interface IMessagePipeline
{
    /// <summary>
    /// Convert domain message to LLM message
    /// </summary>
    LLMMessage ConvertToLLMMessage(IMessage message);

    /// <summary>
    /// Convert multiple domain messages to LLM messages
    /// </summary>
    List<LLMMessage> ConvertToLLMMessages(IEnumerable<IMessage> messages);

    /// <summary>
    /// Convert LLM response to domain message
    /// </summary>
    IMessage ConvertToDomainMessage(LLMResponse response);
}
```

**Tests**: ❌ NO TESTS NEEDED (interface definition)

---

#### 2.2 Message Pipeline Implementation

##### `MessagePipeline` Class
```csharp
namespace TransparentAiAgentCore.Application.Pipeline;

using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Exceptions;

/// <summary>
/// Transforms messages between domain models and LLM models
/// </summary>
public class MessagePipeline : IMessagePipeline
{
    public LLMMessage ConvertToLLMMessage(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return message switch
        {
            UserMessage userMsg => new LLMMessage("user", userMsg.Content),

            AssistantMessage assistantMsg => new LLMMessage("assistant", assistantMsg.Content),

            SystemMessage systemMsg => new LLMMessage("system", systemMsg.Content),

            ToolCallMessage toolCallMsg => new LLMMessage(
                "assistant",
                toolCallMsg.Content,
                new List<LLMToolCall>
                {
                    new LLMToolCall(toolCallMsg.ToolCallId, toolCallMsg.ToolName, toolCallMsg.ToolParameters)
                }),

            ToolResultMessage toolResultMsg => new LLMMessage(
                "tool",
                toolResultMsg.Result,
                toolResultMsg.ToolCallId),

            _ => throw new AgentException($"Unknown message type: {message.GetType().Name}")
        };
    }

    public List<LLMMessage> ConvertToLLMMessages(IEnumerable<IMessage> messages)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        return messages.Select(ConvertToLLMMessage).ToList();
    }

    public IMessage ConvertToDomainMessage(LLMResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        // If response has tool calls, create ToolCallMessage
        if (response.ToolCalls != null && response.ToolCalls.Count > 0)
        {
            // For simplicity, handle single tool call
            // Multi-tool call support can be added later
            var toolCall = response.ToolCalls[0];
            return new ToolCallMessage(
                toolCall.Name,
                toolCall.Arguments,
                toolCall.Id);
        }

        // Otherwise, create AssistantMessage
        return new AssistantMessage(response.Content);
    }
}
```

**Tests** (Lean - Focus on Transformation Logic):

**ConvertToLLMMessage Tests:**
- ✅ ConvertToLLMMessage throws ArgumentNullException for null message
- ✅ ConvertToLLMMessage converts UserMessage to LLMMessage with role="user"
- ✅ ConvertToLLMMessage converts AssistantMessage to LLMMessage with role="assistant"
- ✅ ConvertToLLMMessage converts SystemMessage to LLMMessage with role="system"
- ✅ ConvertToLLMMessage converts ToolCallMessage to LLMMessage with role="assistant" and tool calls
- ✅ ConvertToLLMMessage converts ToolResultMessage to LLMMessage with role="tool" and toolCallId
- ✅ ConvertToLLMMessage throws AgentException for unknown message type
- ✅ ConvertToLLMMessage preserves content correctly

**ConvertToLLMMessages Tests:**
- ✅ ConvertToLLMMessages throws ArgumentNullException for null messages
- ✅ ConvertToLLMMessages converts all messages in sequence
- ✅ ConvertToLLMMessages returns empty list for empty input

**ConvertToDomainMessage Tests:**
- ✅ ConvertToDomainMessage throws ArgumentNullException for null response
- ✅ ConvertToDomainMessage creates AssistantMessage for text response
- ✅ ConvertToDomainMessage creates ToolCallMessage for response with tool calls
- ✅ ConvertToDomainMessage preserves content and tool call details correctly

---

### 3. Agent Orchestration (Application Layer)

#### 3.1 Agent Orchestrator Interface

##### `IAgentOrchestrator` Interface
```csharp
namespace TransparentAiAgentCore.Application.Agent;

using TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Orchestrates agent interactions and conversation flow
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Process user input and generate response
    /// </summary>
    Task<IMessage> ProcessUserInputAsync(string userInput, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process user input and stream response chunks
    /// </summary>
    IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
        string userInput,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversation manager
    /// </summary>
    IConversationManager ConversationManager { get; }

    /// <summary>
    /// Start a new conversation (clears history)
    /// </summary>
    void StartNewConversation();
}
```

**Tests**: ❌ NO TESTS NEEDED (interface definition)

---

##### `StreamingResponseChunk` Class
```csharp
namespace TransparentAiAgentCore.Application.Agent;

/// <summary>
/// Represents a chunk of streaming response
/// </summary>
public class StreamingResponseChunk
{
    public string ContentDelta { get; }
    public bool IsComplete { get; }

    public StreamingResponseChunk(string contentDelta, bool isComplete = false)
    {
        ContentDelta = contentDelta ?? string.Empty;
        IsComplete = isComplete;
    }
}
```

**Tests**: ❌ NO TESTS NEEDED (simple data holder)

---

#### 3.2 Agent Orchestrator Implementation

##### `AgentOrchestrator` Class
```csharp
namespace TransparentAiAgentCore.Application.Agent;

using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Domain.Transparency;
using System.Runtime.CompilerServices;

/// <summary>
/// Orchestrates agent interactions and conversation flow
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ILLMProvider _llmProvider;
    private readonly IConversationManager _conversationManager;
    private readonly IMessagePipeline _messagePipeline;
    private readonly ITransparencyService _transparencyService;
    private readonly AgentConfiguration _agentConfig;
    private readonly LLMConfiguration _llmConfig;

    public IConversationManager ConversationManager => _conversationManager;

    public AgentOrchestrator(
        ILLMProvider llmProvider,
        IConversationManager conversationManager,
        IMessagePipeline messagePipeline,
        ITransparencyService transparencyService,
        AppConfiguration configuration)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _messagePipeline = messagePipeline ?? throw new ArgumentNullException(nameof(messagePipeline));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _agentConfig = configuration.Agent;
        _llmConfig = configuration.LLM;

        // Add system message to conversation
        var systemMessage = new SystemMessage(_agentConfig.SystemPrompt);
        _conversationManager.AddMessage(systemMessage);

        LogEvent("AgentInitialized", "Agent orchestrator initialized");
    }

    public async Task<IMessage> ProcessUserInputAsync(
        string userInput,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ArgumentException("User input cannot be null or whitespace", nameof(userInput));

        try
        {
            // 1. Add user message to conversation
            var userMessage = new UserMessage(userInput);
            _conversationManager.AddMessage(userMessage);
            LogEvent("UserInput", $"User input: {userInput}");

            // 2. Build LLM request from in-context messages
            var llmRequest = BuildLLMRequest();

            // 3. Send to LLM
            LogEvent("LLMRequestSent", "Sending request to LLM");
            var llmResponse = await _llmProvider.SendRequestAsync(llmRequest, cancellationToken);
            LogEvent("LLMResponseReceived", $"Received response from LLM. Content length: {llmResponse.Content?.Length ?? 0}");

            // 4. Convert LLM response to domain message
            var assistantMessage = _messagePipeline.ConvertToDomainMessage(llmResponse);

            // 5. Add assistant message to conversation
            _conversationManager.AddMessage(assistantMessage);

            return assistantMessage;
        }
        catch (Exception ex) when (ex is not AgentException and not LLMException)
        {
            LogEvent("Error", $"Unexpected error: {ex.Message}");
            throw new AgentException($"Error processing user input: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
        string userInput,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ArgumentException("User input cannot be null or whitespace", nameof(userInput));

        // 1. Add user message to conversation
        var userMessage = new UserMessage(userInput);
        _conversationManager.AddMessage(userMessage);
        LogEvent("UserInput", $"User input (streaming): {userInput}");

        // 2. Build LLM request from in-context messages
        var llmRequest = BuildLLMRequest();
        llmRequest = new LLMRequest(
            llmRequest.Messages,
            llmRequest.Temperature,
            llmRequest.TopP,
            llmRequest.MaxTokens,
            stream: true,
            llmRequest.Tools);

        // 3. Stream from LLM
        LogEvent("LLMStreamRequestSent", "Sending streaming request to LLM");

        var contentBuilder = new System.Text.StringBuilder();

        await foreach (var chunk in _llmProvider.StreamRequestAsync(llmRequest, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.ContentDelta))
            {
                contentBuilder.Append(chunk.ContentDelta);
            }

            yield return new StreamingResponseChunk(chunk.ContentDelta, chunk.IsComplete);

            if (chunk.IsComplete)
            {
                LogEvent("LLMStreamCompleted", $"Stream completed. Total content length: {contentBuilder.Length}");
            }
        }

        // 4. Create assistant message from accumulated content
        var assistantMessage = new AssistantMessage(contentBuilder.ToString());
        _conversationManager.AddMessage(assistantMessage);
    }

    public void StartNewConversation()
    {
        _conversationManager.ClearConversation();

        // Add system message to new conversation
        var systemMessage = new SystemMessage(_agentConfig.SystemPrompt);
        _conversationManager.AddMessage(systemMessage);

        LogEvent("ConversationRestarted", "Started new conversation");
    }

    private LLMRequest BuildLLMRequest()
    {
        // Get messages that are in context
        var inContextMessages = _conversationManager.GetInContextMessages();

        // Convert to LLM messages
        var llmMessages = _messagePipeline.ConvertToLLMMessages(inContextMessages);

        // Build request
        return new LLMRequest(
            llmMessages,
            _llmConfig.Temperature,
            _llmConfig.TopP,
            _llmConfig.MaxTokens,
            stream: false,
            tools: null); // Tools will be added in Phase 5
    }

    private void LogEvent(string eventType, string details)
    {
        _transparencyService?.LogEvent(
            new TransparencyEvent(
                TransparencyEventType.SystemState,
                System.Text.Json.JsonSerializer.Serialize(new { EventType = eventType }),
                details));
    }
}
```

**Tests** (Lean - Focus on Orchestration Logic):

**Constructor Tests:**
- ✅ Constructor throws ArgumentNullException for null llmProvider
- ✅ Constructor throws ArgumentNullException for null conversationManager
- ✅ Constructor throws ArgumentNullException for null messagePipeline
- ✅ Constructor throws ArgumentNullException for null transparencyService
- ✅ Constructor throws ArgumentNullException for null configuration
- ✅ Constructor adds system message to conversation
- ✅ Constructor logs initialization event

**ProcessUserInputAsync Tests (with mocked dependencies):**
- ✅ ProcessUserInputAsync throws ArgumentException for null/whitespace input
- ✅ ProcessUserInputAsync adds user message to conversation
- ✅ ProcessUserInputAsync builds LLM request from in-context messages
- ✅ ProcessUserInputAsync calls LLM provider with correct request
- ✅ ProcessUserInputAsync converts LLM response to domain message
- ✅ ProcessUserInputAsync adds assistant message to conversation
- ✅ ProcessUserInputAsync returns assistant message
- ✅ ProcessUserInputAsync logs appropriate events
- ✅ ProcessUserInputAsync wraps non-agent exceptions in AgentException
- ✅ ProcessUserInputAsync propagates LLMException and AgentException

**ProcessUserInputStreamingAsync Tests (with mocked dependencies):**
- ✅ ProcessUserInputStreamingAsync throws ArgumentException for null/whitespace input
- ✅ ProcessUserInputStreamingAsync adds user message to conversation
- ✅ ProcessUserInputStreamingAsync yields chunks from LLM provider
- ✅ ProcessUserInputStreamingAsync accumulates content correctly
- ✅ ProcessUserInputStreamingAsync adds complete assistant message to conversation
- ✅ ProcessUserInputStreamingAsync logs appropriate events

**StartNewConversation Tests:**
- ✅ StartNewConversation clears conversation
- ✅ StartNewConversation adds system message to new conversation
- ✅ StartNewConversation logs restart event

**Integration Tests (with mocked LLM provider):**
- ✅ Multi-turn conversation maintains context correctly
- ✅ Context truncation works during multi-turn conversation
- ✅ System message is preserved across truncation

---

## Implementation Order (TDD)

### Order of Implementation

1. **Conversation Management** (Foundation for orchestration)
   - ContextStatusChangedEventArgs
   - IConversationManager interface
   - ConversationManager implementation
   - Comprehensive tests for truncation logic

2. **Message Pipeline** (Transform between domain and LLM models)
   - IMessagePipeline interface
   - MessagePipeline implementation
   - Tests for all message type conversions

3. **Agent Orchestration** (Pulls everything together)
   - StreamingResponseChunk
   - IAgentOrchestrator interface
   - AgentOrchestrator implementation
   - Integration tests with mocked dependencies

---

## TDD Workflow

For each component:

1. **Write Test First (Red)**
   - Create test file in `TransparentAiAgentCore_Tests/Application/`
   - Write failing test(s) for component
   - Run test - should fail

2. **Implement Component (Green)**
   - Create implementation file in `TransparentAiAgentCore/Application/`
   - Write minimum code to pass test
   - Run test - should pass

3. **Refactor (Refactor)**
   - Improve code quality
   - Ensure tests still pass
   - Add more tests as needed

4. **Commit**
   - Commit tests + implementation together
   - Clear commit message

---

## Project Structure

```
TransparentAiAgentCore/
└── Application/
    ├── Conversation/
    │   ├── IConversationManager.cs
    │   ├── ConversationManager.cs
    │   └── ContextStatusChangedEventArgs.cs
    ├── Pipeline/
    │   ├── IMessagePipeline.cs
    │   └── MessagePipeline.cs
    └── Agent/
        ├── IAgentOrchestrator.cs
        ├── AgentOrchestrator.cs
        └── StreamingResponseChunk.cs

TransparentAiAgentCore_Tests/
└── Application/
    ├── Conversation/
    │   └── ConversationManagerTests.cs
    ├── Pipeline/
    │   └── MessagePipelineTests.cs
    └── Agent/
        └── AgentOrchestratorTests.cs
```

---

## Testing Strategy

### Mocking Dependencies

For testing `AgentOrchestrator`, mock these dependencies:
- `ILLMProvider` (mock LLM responses)
- `IConversationManager` (can use real implementation or mock)
- `IMessagePipeline` (can use real implementation or mock)
- `ITransparencyService` (mock or use real implementation)

**Recommendation**: Use real implementations for `ConversationManager` and `MessagePipeline` in orchestrator tests (integration-style), mock only `ILLMProvider`.

### Test Organization

**Unit Tests**:
- `ConversationManagerTests`: Test conversation management in isolation
- `MessagePipelineTests`: Test message transformations in isolation

**Integration Tests**:
- `AgentOrchestratorTests`: Test full orchestration flow with real conversation manager and pipeline, mocked LLM provider

---

## Dependencies

### Phase 1 Dependencies

Phase 3 depends on Phase 1 components:
- ✅ Domain models (`IMessage`, `UserMessage`, `AssistantMessage`, etc.)
- ✅ `AgentConfiguration` and `LLMConfiguration`
- ✅ `ITransparencyService`
- ✅ Exception hierarchy

### Phase 2 Dependencies

Phase 3 depends on Phase 2 components:
- ✅ `ILLMProvider` interface
- ✅ `LLMRequest`, `LLMResponse`, `LLMMessage` models
- ✅ `StreamingLLMChunk`
- ✅ At least one provider implementation (for integration testing)

**Ensure Phase 1 and Phase 2 are 100% complete before starting Phase 3.**

---

## Deliverables

At the end of Phase 3, we will have:

✅ **Conversation Management**:
- Full conversation history tracking
- Context window management with truncation
- Context status tracking for each message
- Event notifications for context changes

✅ **Message Transformation**:
- Bidirectional conversion between domain and LLM models
- Support for all message types

✅ **Agent Orchestration**:
- Multi-turn conversation capability
- Streaming and non-streaming response handling
- Integration with LLM providers
- Complete transparency logging

✅ **Capabilities**:
- Can have multi-turn conversations with LLM
- Context automatically managed (truncation when needed)
- All messages remain visible in history with status indicator
- User always knows what's in LLM context
- Complete event logging to transparency system

✅ **Test Coverage**:
- Unit tests for all components
- Integration tests for agent orchestrator
- ~95%+ code coverage
- All tests passing

✅ **Foundation Ready**:
- Phase 4 (Basic UI) can begin
- Can connect UI to agent orchestrator for chat functionality
- Ready for user interaction

---

## Next Phase

**Phase 4: Basic UI** will use these foundations to:
- Create Blazor chat component
- Display conversation with context status indicators
- Show "In Context" vs "Not in Context" badges
- Enable user to chat with agent through browser
- Prove end-to-end functionality before adding MCP tools

---

## Common Pitfalls & Solutions

### Pitfall 1: Not Testing Truncation Logic Thoroughly

❌ **Bad**: Only test truncation with simple scenarios
✅ **Good**: Test edge cases (context size = 1, multiple truncations, system message preservation)

### Pitfall 2: Forgetting Thread Safety

❌ **Bad**: Not locking when modifying conversation history
✅ **Good**: Use locks in ConversationManager for thread safety

### Pitfall 3: Not Preserving Truncated Messages

❌ **Bad**: Removing messages from history when truncated
✅ **Good**: Keep all messages, just update their context status

### Pitfall 4: Ignoring Event Notifications

❌ **Bad**: Changing context status without raising events
✅ **Good**: Always raise ContextStatusChanged event when truncating

### Pitfall 5: Over-Complicated Message Transformation

❌ **Bad**: Complex transformation logic with business rules
✅ **Good**: Simple 1:1 mapping, delegate business logic elsewhere

---

## Integration Points

### With Phase 1:
- Uses domain models (`IMessage` hierarchy)
- Uses `ITransparencyService` for logging
- Uses configuration models

### With Phase 2:
- Uses `ILLMProvider` for LLM communication
- Uses LLM domain models (`LLMRequest`, `LLMResponse`, etc.)
- Uses streaming support

### With Phase 4 (Next):
- `IAgentOrchestrator` will be injected into Blazor components
- `IConversationManager` will be accessed for displaying messages
- Context status will be displayed in UI with badges
- Streaming chunks will update UI in real-time

---

## Key Concepts

### Context Window Management

**Strategy**: Simple truncation
- When context limit reached, oldest messages are marked as `TruncatedFromContext`
- System messages are preserved when possible (truncated last)
- Truncated messages remain in full history (visible in UI with indicator)
- User always knows what LLM can and cannot see

**Visual Representation**:
```
Full History (20 messages):
[1] System: "You are a helpful assistant"     [In Context]
[2] User: "Hi"                                [Not in Context] ← Truncated
[3] Assistant: "Hello!"                       [Not in Context] ← Truncated
...
[18] User: "What can you do?"                 [In Context]
[19] Assistant: "I can help with..."          [In Context]
[20] User: "Tell me more"                     [In Context]

Context Window (15/15 messages in context)
```

### Message Flow

```
User Input
    ↓
UserMessage created
    ↓
Added to ConversationManager
    ↓
Truncation check (if needed)
    ↓
Get in-context messages
    ↓
Transform to LLM messages (Pipeline)
    ↓
Build LLM request
    ↓
Send to LLM Provider
    ↓
Receive LLM response
    ↓
Transform to domain message (Pipeline)
    ↓
AssistantMessage created
    ↓
Added to ConversationManager
    ↓
Return to caller (UI)
```

---

**Status**: Ready for Implementation
**Estimated Time**: 2-3 days
**Approach**: Strict TDD - Tests First!
**Dependencies**: Phase 1 and Phase 2 must be 100% complete

---

**Let's build the agent core! 🚀**
