# Scenario User Message Display Issue - Analysis Report

**Date**: 2025-11-13
**Issue**: `scenario_user_message` displays as regular user message (blue color, no visual distinction) instead of having different UI display
**Goal**: Send as regular user message to LLM (without annotation), but display differently in UI

---

## Executive Summary

The root cause is that **domain messages (UserMessage) contain no metadata indicating they originated from a scenario**. When `scenario_user_message` steps execute, they call the same orchestrator method as regular user input, which creates a plain `UserMessage` object. The UI cannot distinguish scenario messages from regular user messages when rebuilding from domain messages.

**Impact on Architecture**: This issue reveals a fundamental gap in the message architecture where UI-only metadata cannot be preserved through the domain layer.

---

## Message Flow Analysis

### 1. Scenario Execution Flow

**File**: `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`

```
ExecuteScenarioUserMessageStepAsync (line 257-265)
  └─> Calls ExecuteAutoMessageStepAsync(step, cancellationToken)
      └─> Calls _orchestrator.ProcessUserInputStreamingAsync(step.Content, cancellationToken)
```

**Problem**: `scenario_user_message` and `auto_message` use the SAME execution path. No distinction is made at orchestrator level.

---

### 2. Orchestrator Processing

**File**: `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs:315-332`

```csharp
public async IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
    string userInput,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // 1. Add user message to conversation
    var userMessage = new UserMessage(userInput);  // ← Creates plain UserMessage
    _conversationManager.AddMessage(userMessage);

    // 2. Start streaming tool loop from depth 0
    await foreach (var chunk in ProcessStreamingToolLoopAsync(0, cancellationToken))
    {
        yield return chunk;
    }
}
```

**Problem**:
- Orchestrator receives only string content, no context about message source
- Creates plain `UserMessage` with no scenario metadata
- **This is where the scenario context is lost**

---

### 3. Domain Message Model

**File**: `TransparentAiAgentCore/Domain/Models/UserMessage.cs:1-24`

```csharp
public class UserMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.User;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    // NO SCENARIO METADATA PROPERTIES
}
```

**Problem**: Domain model has no properties to indicate:
- Message is from a scenario
- Message should display differently in UI
- Message has an annotation

---

### 4. UI Service Workaround Attempt

**File**: `TransparentAiAgentGui/Services/ConversationUIService.cs`

#### Annotation Tracking (lines 408-421)

```csharp
private void OnScenarioStepExecuted(object? sender, ScenarioStepEventArgs e)
{
    var step = e.Step;

    if (step.Type == ScenarioStepType.ScenarioUserMessage &&
        !string.IsNullOrWhiteSpace(step.Annotation))
    {
        lock (_annotationLock)
        {
            _nextAutoMessageAnnotation = step.Annotation;
        }
    }
}
```

**Problem**: This event fires but the message has already been added to conversation by orchestrator.

#### Auto-Message Tracking (lines 342-349)

```csharp
private void OnAutoMessageSent(object? sender, AutoMessageSentEventArgs e)
{
    lock (_autoMessageLock)
    {
        _pendingAutoMessages.Add(e.MessageContent);
    }
}
```

**Problem**: This tracks auto-messages by content, but:
1. Relies on content matching (fragile)
2. Only works for `auto_message` not `scenario_user_message`
3. Doesn't survive `RefreshMessages()` calls

#### RefreshMessages Logic (lines 280-320)

```csharp
private void RefreshMessages()
{
    var domainMessages = _conversationManager.GetAllMessages();
    var newMessages = new List<UIMessage>();

    foreach (var domainMessage in domainMessages)
    {
        var uiMessage = UIMessage.FromDomainMessage(domainMessage);

        // Check if this user message is an auto-message
        if (uiMessage.Role == MessageRole.User)
        {
            lock (_autoMessageLock)
            {
                if (_pendingAutoMessages.Contains(uiMessage.Content))
                {
                    uiMessage.IsAutoMessage = true;
                    _pendingAutoMessages.Remove(uiMessage.Content);
                }
            }

            // Restore annotation if one exists for this content
            lock (_annotationLock)
            {
                if (_contentToAnnotation.TryGetValue(uiMessage.Content, out var annotation))
                {
                    uiMessage.Annotation = annotation;
                }
            }
        }

        newMessages.Add(uiMessage);
    }
}
```

**Problem**:
- Matches by content, which is fragile
- Race conditions between event firing and message being added
- Content-based matching breaks if same content is sent twice

---

### 5. UI Display

**File**: `TransparentAiAgentGui/Models/UIMessage.cs:47-54`

```csharp
public string CssClass => Role switch
{
    MessageRole.User => "message-user",     // ← All user messages get this
    MessageRole.Assistant => "message-assistant",
    MessageRole.System => "message-system",
    MessageRole.Tool => "message-tool",
    _ => "message-default"
};
```

**File**: `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor.css:8-11`

```css
.message-user {
    background-color: #e3f2fd;  /* Light blue */
    border-left-color: #2196f3; /* Blue */
}
```

**Result**: All user messages (regular and scenario) display with same blue styling.

---

## Root Cause Summary

The issue occurs at **AgentOrchestrator.ProcessUserInputStreamingAsync()** where a plain `UserMessage` is created without any scenario metadata. The chain of issues:

1. **ScenarioExecutor** has scenario context (step type, annotation) ✅
2. **ScenarioExecutor** calls orchestrator with only string content ❌
3. **Orchestrator** creates plain UserMessage ❌
4. **ConversationManager** stores plain UserMessage ❌
5. **UI Service** tries to track metadata separately (fragile) ⚠️
6. **UI** has no reliable way to distinguish scenario messages ❌

---

## Why Previous Fixes Failed

Looking at the attempted workarounds in `ConversationUIService`:

1. **Content-based matching** (`_contentToAnnotation`, `_pendingAutoMessages`):
   - Breaks if same content is sent multiple times
   - Race condition between event and message being added
   - Doesn't survive RefreshMessages() reliably

2. **Event-based tracking** (`OnScenarioStepExecuted`, `OnAutoMessageSent`):
   - Events fire at wrong time (before or after message is added)
   - No way to match event to specific message ID
   - State gets out of sync

3. **No domain-level support**:
   - UserMessage has no scenario metadata
   - Can't distinguish messages at domain level
   - UI layer trying to add metadata after the fact

---

## Architectural Implications

This issue highlights a fundamental architectural challenge:

### Current Architecture (Clean Architecture)

```
┌─────────────────┐
│   UI Layer      │ ← Needs scenario metadata for display
├─────────────────┤
│ Application     │ ← Orchestrator creates plain UserMessages
├─────────────────┤
│   Domain        │ ← UserMessage has no scenario metadata
└─────────────────┘
```

### The Conflict

- **Domain Layer**: Should be pure, no UI concerns
- **UI Layer**: Needs metadata to display messages differently
- **Application Layer**: Bridge between them, but currently loses metadata

### Key Question

**Where should scenario metadata live?**

**Option A**: Domain (UserMessage has IsScenario, Annotation properties)
- ✅ Persists through all layers
- ❌ Pollutes domain with UI concerns
- ❌ Breaks Clean Architecture principles

**Option B**: Application (Orchestrator accepts metadata, stores separately)
- ✅ Keeps domain clean
- ❌ Complex metadata management
- ❌ Still need to pass through to UI

**Option C**: UI only (Track separately in ConversationUIService)
- ✅ No domain changes
- ❌ Fragile, race conditions
- ❌ Current approach - already failing

**Option D**: Enriched messages (Domain has metadata, but as extension/marker)
- ✅ Balances concerns
- ✅ Domain remains mostly clean
- ✅ UI can distinguish messages
- ❌ Requires new patterns

---

## Proposed Solutions

### Solution 1: Add Metadata to IMessage (Least Invasive)

**Changes**:
1. Add optional metadata dictionary to `IMessage` interface
2. ScenarioExecutor passes metadata to orchestrator
3. Orchestrator attaches metadata to UserMessage
4. UI reads metadata from domain messages

**Pros**:
- Minimal changes to existing code
- Preserves Clean Architecture
- Metadata is optional, doesn't affect non-scenario messages

**Cons**:
- Adds complexity to domain model
- Dictionary-based metadata is less type-safe

---

### Solution 2: Specialized Message Type

**Changes**:
1. Create `ScenarioUserMessage : IMessage` in domain
2. Orchestrator accepts message type hint
3. ConversationManager handles both types
4. UI distinguishes by message type

**Pros**:
- Type-safe
- Clear separation
- Follows existing pattern (AssistantToolCallMessage, etc.)

**Cons**:
- More code changes
- Need to update all message handling code
- May affect LLM serialization

---

### Solution 3: Message Context Service

**Changes**:
1. Create `IMessageContextService` in application layer
2. ScenarioExecutor registers message context before sending
3. Orchestrator looks up context, attaches to message
4. UI queries context service for display metadata

**Pros**:
- Keeps domain clean
- Centralized metadata management
- Extensible for future needs

**Cons**:
- New service to maintain
- Requires message ID coordination
- Complex threading/timing

---

### Solution 4: Enriched Orchestrator API

**Changes**:
1. Add `ProcessScenarioUserMessageAsync()` method to orchestrator
2. Accepts content + metadata (annotation, display hints)
3. Creates UserMessage with embedded metadata
4. UI extracts metadata for display

**Pros**:
- Clear API separation
- Metadata flows naturally
- No content-based matching

**Cons**:
- Orchestrator API grows
- Need to decide where metadata lives in domain

---

## Recommended Solution

**Hybrid Approach (Solution 4 + Metadata in Domain)**:

1. **Add metadata property to UserMessage**:
   ```csharp
   public class UserMessage : IMessage
   {
       // ... existing properties ...
       public Dictionary<string, object>? Metadata { get; init; }
   }
   ```

2. **Add specialized orchestrator method**:
   ```csharp
   public async IAsyncEnumerable<StreamingResponseChunk> ProcessScenarioUserMessageAsync(
       string userInput,
       string? annotation = null,
       CancellationToken cancellationToken = default)
   {
       var metadata = new Dictionary<string, object>
       {
           ["IsScenarioMessage"] = true,
           ["Annotation"] = annotation ?? string.Empty
       };

       var userMessage = new UserMessage(userInput) { Metadata = metadata };
       _conversationManager.AddMessage(userMessage);

       // ... rest of processing ...
   }
   ```

3. **ScenarioExecutor calls new method**:
   ```csharp
   private async Task ExecuteScenarioUserMessageStepAsync(ScenarioStep step, CancellationToken cancellationToken)
   {
       await foreach (var chunk in _orchestrator.ProcessScenarioUserMessageAsync(
           step.Content,
           step.Annotation,
           cancellationToken))
       {
           // ... streaming handling ...
       }
   }
   ```

4. **UI extracts metadata**:
   ```csharp
   public static UIMessage FromDomainMessage(IMessage message)
   {
       var uiMessage = new UIMessage { /* ... */ };

       if (message is UserMessage userMsg &&
           userMsg.Metadata?.TryGetValue("IsScenarioMessage", out var isScenario) == true &&
           (bool)isScenario)
       {
           uiMessage.IsAutoMessage = true;
           if (userMsg.Metadata.TryGetValue("Annotation", out var annotation))
           {
               uiMessage.Annotation = annotation?.ToString();
           }
       }

       return uiMessage;
   }
   ```

5. **Update CSS for scenario messages** (optional):
   ```csharp
   public string CssClass
   {
       get
       {
           if (Role == MessageRole.User && IsAutoMessage)
               return "message-scenario-user";

           return Role switch
           {
               MessageRole.User => "message-user",
               // ... rest ...
           };
       }
   }
   ```

---

## Alternative: Minimal Change Solution

If we want to avoid domain changes entirely:

1. **Track by message ID** instead of content:
   ```csharp
   private readonly Dictionary<Guid, string> _messageIdToAnnotation = new();
   ```

2. **ScenarioExecutor generates ID before calling orchestrator**:
   - Not possible - orchestrator creates the message and ID

This won't work cleanly without domain changes.

---

## Testing Considerations

Any solution must handle:

1. **Multiple scenario messages with same content**
2. **RefreshMessages() rebuilding UI from domain**
3. **Scenario messages persisting after scenario ends**
4. **Regular user messages sent during scenarios**
5. **Annotations displaying correctly**
6. **CSS styling applied correctly**

---

## Conclusion

The issue is architectural: **metadata about message source cannot flow from ScenarioExecutor → Orchestrator → Domain → UI** without domain model support.

The recommended solution adds minimal metadata support to the domain layer while keeping it clean and optional. This preserves Clean Architecture principles while solving the practical UI display problem.

**Next Step**: Choose solution approach and implement.
