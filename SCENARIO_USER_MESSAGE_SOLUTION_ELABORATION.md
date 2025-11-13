# ScenarioUserMessage as Distinct Domain Type - Detailed Analysis

**Date**: 2025-11-13
**Proposed Solution**: Create `ScenarioUserMessage : IMessage` in domain layer

---

## Solution Overview

Instead of adding optional metadata to `UserMessage`, create a **new message type** specifically for scenario-generated user messages. This maintains type safety and makes the distinction explicit at the domain level.

---

## Proposed Implementation

### 1. New Domain Type

**File**: `TransparentAiAgentCore/Domain/Models/ScenarioUserMessage.cs`

```csharp
using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Represents a user message sent by a teaching scenario.
/// Sent to LLM as a regular user message (annotation excluded),
/// but displayed differently in UI to indicate scenario origin.
/// </summary>
public class ScenarioUserMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.User;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    /// <summary>
    /// Optional annotation explaining the scenario step to the user.
    /// UI-only, never sent to LLM.
    /// </summary>
    public string? Annotation { get; }

    /// <summary>
    /// Type of scenario step that generated this message (for tracking).
    /// </summary>
    public string ScenarioStepType { get; }

    public ScenarioUserMessage(
        string content,
        string? annotation = null,
        string scenarioStepType = "scenario_user_message")
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = Guid.NewGuid();
        Content = content;
        Annotation = annotation;
        ScenarioStepType = scenarioStepType;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }
}
```

---

### 2. Orchestrator Changes

**File**: `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs`

#### New Method

```csharp
/// <summary>
/// Processes a user message from a teaching scenario with optional annotation.
/// The message is sent to the LLM as a regular user message (annotation excluded),
/// but the UI can display it differently.
/// </summary>
public async IAsyncEnumerable<StreamingResponseChunk> ProcessScenarioUserMessageAsync(
    string userInput,
    string? annotation = null,
    string scenarioStepType = "scenario_user_message",
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(userInput))
        throw new ArgumentException("User input cannot be null or whitespace", nameof(userInput));

    // 1. Add scenario user message to conversation
    var scenarioUserMessage = new ScenarioUserMessage(userInput, annotation, scenarioStepType);
    _conversationManager.AddMessage(scenarioUserMessage);
    LogEvent("ScenarioUserInput", $"Scenario user input (streaming): {userInput}");

    // 2. Start streaming tool loop from depth 0
    await foreach (var chunk in ProcessStreamingToolLoopAsync(0, cancellationToken))
    {
        yield return chunk;
    }
}
```

#### Non-Streaming Version (if needed)

```csharp
public async Task<IMessage> ProcessScenarioUserInputAsync(
    string userInput,
    string? annotation = null,
    string scenarioStepType = "scenario_user_message",
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(userInput))
        throw new ArgumentException("User input cannot be null or whitespace", nameof(userInput));

    try
    {
        // 1. Add scenario user message to conversation
        var scenarioUserMessage = new ScenarioUserMessage(userInput, annotation, scenarioStepType);
        _conversationManager.AddMessage(scenarioUserMessage);
        LogEvent("ScenarioUserInput", $"Scenario user input: {userInput}");

        // 2. Tool calling loop (with max depth protection)
        IMessage finalMessage = await ProcessWithToolLoopAsync(0, cancellationToken);

        return finalMessage;
    }
    catch (Exception ex) when (ex is not AgentException and not LLMException)
    {
        LogEvent("Error", $"Unexpected error: {ex.Message}");
        throw new AgentException($"Error processing scenario user input: {ex.Message}", ex);
    }
}
```

---

### 3. ScenarioExecutor Changes

**File**: `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs:257-265`

**Before**:
```csharp
private async Task ExecuteScenarioUserMessageStepAsync(ScenarioStep step, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(step.Content))
        return;

    // ScenarioUserMessage behaves like AutoMessage but with annotation support
    // The annotation is handled by the UI layer
    await ExecuteAutoMessageStepAsync(step, cancellationToken);
}
```

**After**:
```csharp
private async Task ExecuteScenarioUserMessageStepAsync(ScenarioStep step, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(step.Content))
        return;

    // Process using specialized orchestrator method that creates ScenarioUserMessage
    await foreach (var chunk in _orchestrator.ProcessScenarioUserMessageAsync(
        step.Content,
        step.Annotation,
        "scenario_user_message",
        cancellationToken))
    {
        // Forward streaming chunks as events for UI to consume
        StreamingUpdate?.Invoke(this, new ScenarioStreamingUpdateEventArgs(
            chunk.ContentDeltaSafe,
            chunk.IsComplete));

        if (chunk.IsComplete)
            break;
    }

    // Fire event to notify that an auto-message was sent
    AutoMessageSent?.Invoke(this, new AutoMessageSentEventArgs(step.Content, DateTime.UtcNow));
}
```

---

### 4. MessagePipeline Changes

**File**: `TransparentAiAgentCore/Application/Pipeline/MessagePipeline.cs`

**Critical**: ScenarioUserMessage must convert to regular LLM user message (annotation excluded).

```csharp
public List<LLMMessage> ConvertToLLMMessages(List<IMessage> domainMessages)
{
    var llmMessages = new List<LLMMessage>();

    foreach (var message in domainMessages)
    {
        LLMMessage? llmMessage = message switch
        {
            UserMessage userMsg => ConvertUserMessage(userMsg),
            ScenarioUserMessage scenarioUserMsg => ConvertScenarioUserMessage(scenarioUserMsg),  // ← New case
            AssistantMessage assistantMsg => ConvertAssistantMessage(assistantMsg),
            AssistantToolCallMessage toolCallMsg => ConvertAssistantToolCallMessage(toolCallMsg),
            SystemMessage systemMsg => ConvertSystemMessage(systemMsg),
            ToolResultMessage toolResultMsg => ConvertToolResultMessage(toolResultMsg),
            _ => throw new InvalidOperationException($"Unknown message type: {message.GetType().Name}")
        };

        if (llmMessage != null)
            llmMessages.Add(llmMessage);
    }

    return llmMessages;
}

private LLMMessage ConvertScenarioUserMessage(ScenarioUserMessage scenarioUserMessage)
{
    // Convert to regular user message - annotation is UI-only
    return new LLMMessage(
        LLMRole.User,
        new List<LLMContent>
        {
            new LLMTextContent(scenarioUserMessage.Content)
        });
}
```

---

### 5. UI Model Changes

**File**: `TransparentAiAgentGui/Models/UIMessage.cs:79-117`

**Before**:
```csharp
public static UIMessage FromDomainMessage(IMessage message)
{
    if (message == null)
        throw new ArgumentNullException(nameof(message));

    var uiMessage = new UIMessage
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        Timestamp = message.Timestamp,
        ContextStatus = message.ContextStatus
    };

    // Handle tool-specific messages
    if (message is AssistantToolCallMessage toolCallMsg)
    {
        // ... tool call handling ...
    }
    else if (message is ToolResultMessage toolResult)
    {
        // ... tool result handling ...
    }

    return uiMessage;
}
```

**After**:
```csharp
public static UIMessage FromDomainMessage(IMessage message)
{
    if (message == null)
        throw new ArgumentNullException(nameof(message));

    var uiMessage = new UIMessage
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        Timestamp = message.Timestamp,
        ContextStatus = message.ContextStatus
    };

    // Handle scenario user messages
    if (message is ScenarioUserMessage scenarioUserMsg)
    {
        uiMessage.IsAutoMessage = true;
        uiMessage.Annotation = scenarioUserMsg.Annotation;
    }
    // Handle tool-specific messages
    else if (message is AssistantToolCallMessage toolCallMsg)
    {
        // ... tool call handling ...
    }
    else if (message is ToolResultMessage toolResult)
    {
        // ... tool result handling ...
    }

    return uiMessage;
}
```

---

### 6. ConversationUIService Simplification

**File**: `TransparentAiAgentGui/Services/ConversationUIService.cs`

**Remove**:
- `_nextAutoMessageAnnotation` field
- `_contentToAnnotation` dictionary
- `OnScenarioStepExecuted()` event handler
- Content-based annotation matching in `RefreshMessages()`
- All annotation-related locking code

**Simplify RefreshMessages()**:

```csharp
private void RefreshMessages()
{
    var domainMessages = _conversationManager.GetAllMessages();
    var newMessages = new List<UIMessage>();

    foreach (var domainMessage in domainMessages)
    {
        var uiMessage = UIMessage.FromDomainMessage(domainMessage);
        // ScenarioUserMessage handling is now inside FromDomainMessage()
        newMessages.Add(uiMessage);
    }

    lock (_messagesLock)
    {
        _messages.Clear();
        _messages.AddRange(newMessages);
    }
    OnMessagesChanged();
}
```

**Simplification**: ~80 lines of fragile tracking code removed!

---

### 7. Optional: CSS Styling Enhancement

**File**: `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor.css`

Add distinct styling for scenario user messages:

```css
/* Scenario user message - lighter/different than regular user */
.message-scenario-user {
    background-color: #f0f7ff;  /* Lighter blue */
    border-left-color: #64b5f6; /* Lighter blue border */
    opacity: 0.95;
}
```

**File**: `TransparentAiAgentGui/Models/UIMessage.cs:47-54`

```csharp
public string CssClass
{
    get
    {
        // Scenario user messages get distinct styling
        if (Role == MessageRole.User && IsAutoMessage)
            return "message-scenario-user";

        return Role switch
        {
            MessageRole.User => "message-user",
            MessageRole.Assistant => "message-assistant",
            MessageRole.System => "message-system",
            MessageRole.Tool => "message-tool",
            _ => "message-default"
        };
    }
}
```

---

## Complete List of File Changes

### New Files (1)
1. `TransparentAiAgentCore/Domain/Models/ScenarioUserMessage.cs` - New domain type

### Modified Files (6)
1. `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs` - Add new method
2. `TransparentAiAgentCore/Application/Agent/IAgentOrchestrator.cs` - Add interface method
3. `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs` - Update step execution
4. `TransparentAiAgentCore/Application/Pipeline/MessagePipeline.cs` - Add conversion logic
5. `TransparentAiAgentGui/Models/UIMessage.cs` - Add pattern matching
6. `TransparentAiAgentGui/Services/ConversationUIService.cs` - Remove workarounds

### Optional Files (2)
1. `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor.css` - Add styling
2. `TransparentAiAgentGui/Models/UIMessage.cs` - Update CssClass logic

### Test Files to Update (~4)
1. `UserMessage` tests might need companion `ScenarioUserMessage` tests
2. Pipeline tests need cases for new message type
3. Orchestrator tests need cases for new method
4. UI tests need cases for scenario message display

**Total Impact**: ~10 files modified/created

---

## Comprehensive Pros and Cons

### ✅ PROS

#### 1. **Type Safety** ⭐⭐⭐
- Compiler enforces correct usage
- Pattern matching: `if (message is ScenarioUserMessage)` is explicit and safe
- No runtime type checking of dictionaries or metadata
- IDE autocomplete and refactoring support

#### 2. **Readability** ⭐⭐⭐
- Intent is crystal clear: `ScenarioUserMessage` vs `UserMessage`
- No magic strings in metadata dictionaries
- Code is self-documenting
- New developers immediately understand the distinction

#### 3. **Maintainability** ⭐⭐⭐
- Changes to scenario message behavior are localized
- No fragile content-based matching or race conditions
- Future scenario-specific properties can be added without affecting UserMessage
- Clean separation of concerns

#### 4. **Domain Model Clarity** ⭐⭐
- Domain explicitly models the concept of scenario-generated messages
- Annotation is a first-class property, not hidden in metadata
- Follows Domain-Driven Design principles
- Type system enforces business rules

#### 5. **No Workarounds** ⭐⭐⭐
- Eliminates all the fragile tracking code in ConversationUIService
- Removes ~80 lines of complex synchronization and matching logic
- No race conditions between events and message creation
- No content-based matching that breaks with duplicate messages

#### 6. **Testability** ⭐⭐
- Easy to create test scenarios with ScenarioUserMessage
- Clear test cases: "when scenario message, expect X display"
- No mocking of complex tracking dictionaries

#### 7. **Consistency with Existing Pattern** ⭐⭐
- Follows same pattern as `AssistantToolCallMessage`, `ToolResultMessage`
- System already handles multiple message types
- UI already patterns matches on message types

#### 8. **Future-Proof** ⭐⭐
- Easy to add scenario-specific behavior later
- Could add: scenario ID, step index, scenario metadata
- Could distinguish different scenario step types (auto_message vs scenario_user_message)

#### 9. **LLM Transparency** ⭐⭐
- Clearly shows that LLM receives regular user message
- Annotation exclusion is explicit in MessagePipeline
- No confusion about what data reaches the LLM

#### 10. **Reliable Display** ⭐⭐⭐
- UI reliably shows scenario messages correctly
- RefreshMessages() always works (no state loss)
- Works with duplicate message content
- No timing issues

---

### ❌ CONS

#### 1. **More Code Changes Required** ⭐⭐⭐
- Must update 10+ files
- Every layer touches the new type
- More work upfront vs metadata approach
- Larger PR / code review

#### 2. **Orchestrator API Growth** ⭐⭐
- Adds `ProcessScenarioUserMessageAsync()` method
- Interface `IAgentOrchestrator` grows
- Could be seen as "special case" handling
- More methods to maintain

#### 3. **MessagePipeline Complexity** ⭐
- Adds another case in pattern matching
- Every new message type requires pipeline update
- Could forget to handle new types

#### 4. **Test Coverage Expansion** ⭐⭐
- Need tests for ScenarioUserMessage creation
- Need tests for orchestrator method
- Need tests for pipeline conversion
- Need tests for UI rendering
- More test code to write and maintain

#### 5. **Breaking Change Risk** ⭐
- If messages were persisted, need migration
- Existing code that assumes only UserMessage might break
- Need to check all pattern matching on messages
- Potential bugs in places we didn't anticipate

#### 6. **Future Message Type Proliferation** ⭐⭐
- Sets precedent: every new message variant = new type?
- Could end up with many message types (ScenarioSystemMessage, etc.)
- Need discipline to not over-engineer
- When to use new type vs metadata becomes a design decision

#### 7. **Interface Bloat** ⭐
- IAgentOrchestrator gets more methods over time
- Could become a "kitchen sink" interface
- Might need to refactor later if too many specialized methods

#### 8. **Domain Layer Coupling to Scenarios** ⭐
- Domain now knows about "scenarios" concept
- Is this business logic or UI concern?
- Philosophical question: does domain need to care?
- Could argue scenarios are infrastructure/UI concern

#### 9. **Migration Path** ⭐
- Old scenarios using AutoMessage might need updates
- Need to coordinate ScenarioExecutor changes with domain changes
- Can't do incremental rollout easily

#### 10. **Overhead for Simple Cases** ⭐
- If we only ever have one scenario message type, seems like overkill
- Metadata approach would be simpler for one-off cases
- Type hierarchy grows for rare use cases

---

## Comparison to Alternatives

### vs Metadata Dictionary Approach

| Aspect | ScenarioUserMessage Type | Metadata Dictionary |
|--------|-------------------------|---------------------|
| Type Safety | ✅ Compile-time | ❌ Runtime only |
| Readability | ✅ Explicit | ⚠️ Magic strings |
| Code Changes | ❌ Many (~10 files) | ✅ Few (~3 files) |
| Extensibility | ✅ Add properties | ⚠️ Dictionary grows |
| Testing | ✅ Clear test cases | ⚠️ Test dictionaries |
| Maintenance | ✅ Localized changes | ❌ Scattered logic |
| Performance | ✅ No dictionary lookup | ⚠️ Dictionary overhead |
| Domain Purity | ⚠️ Scenarios in domain | ✅ Keeps domain generic |

### vs UI-Only Tracking (Current Attempt)

| Aspect | ScenarioUserMessage Type | UI-Only Tracking |
|--------|-------------------------|------------------|
| Reliability | ✅ Always works | ❌ Race conditions |
| Complexity | ✅ Simple logic | ❌ Complex tracking |
| RefreshMessages | ✅ Always correct | ❌ State loss risk |
| Content Duplication | ✅ Handles perfectly | ❌ Breaks matching |
| LOC | ✅ Net reduction | ❌ +80 lines workarounds |

---

## Impact on Future Scenarios

### Positive Impacts

1. **Easy to Add More Properties**:
   ```csharp
   public class ScenarioUserMessage : IMessage
   {
       // Easy to add later:
       public string ScenarioId { get; }
       public int StepIndex { get; }
       public ScenarioMetadata Metadata { get; }
   }
   ```

2. **Support Different Scenario Types**:
   - Could distinguish `auto_message` from `scenario_user_message`
   - Could add `ScenarioSystemMessage` for system-level scenario messages
   - Pattern is established

3. **Advanced Scenarios**:
   - Could track which scenario generated message
   - Could implement scenario-specific validation
   - Could add scenario lifecycle events

### Negative Impacts

1. **Pattern Proliferation**:
   - Temptation to create type for every variant
   - Need discipline to avoid over-engineering
   - Could end up with: `ScenarioSystemMessage`, `ScenarioAssistantMessage`, etc.

2. **Code Churn**:
   - Each new type requires pipeline updates
   - Each new type requires UI updates
   - Each new type requires tests

---

## Alternative: Middle Ground Approach

### Hybrid with Marker Interface

Could use a marker interface to reduce code changes:

```csharp
public interface IScenarioMessage : IMessage
{
    string? Annotation { get; }
    string ScenarioStepType { get; }
}

public class ScenarioUserMessage : IMessage, IScenarioMessage
{
    // ... implementation ...
}
```

Then in MessagePipeline:
```csharp
// Handle any scenario message generically
if (message is IScenarioMessage)
{
    // Strip annotation, convert underlying message
}
```

**Pros**: Less code duplication, extensible
**Cons**: Still same file changes, more abstraction

---

## Recommended Approach

### Implementation Strategy

**Phase 1: Core Domain Type**
1. Create `ScenarioUserMessage` domain type
2. Add orchestrator method
3. Update ScenarioExecutor
4. Add MessagePipeline conversion

**Phase 2: UI Updates**
1. Update `UIMessage.FromDomainMessage()`
2. Simplify `ConversationUIService`
3. Optional: Add CSS styling

**Phase 3: Testing**
1. Unit tests for new type
2. Integration tests for orchestrator
3. UI tests for display
4. Scenario execution tests

**Phase 4: Cleanup**
1. Remove old workaround code
2. Remove unused event handlers
3. Remove tracking dictionaries

---

## Risk Mitigation

### Risk: Breaking Changes

**Mitigation**:
- Run full test suite before/after
- Search codebase for all `is UserMessage` patterns
- Check all pattern matching on messages
- Review all message handling code

### Risk: Incomplete Updates

**Mitigation**:
- Checklist of all files to update
- Use compiler errors as guide
- Add `_ => throw` in pattern matches to catch missing cases

### Risk: Performance Regression

**Mitigation**:
- Profile before/after
- Ensure no degradation in message processing
- Check UI rendering performance

### Risk: Future Maintainer Confusion

**Mitigation**:
- Document the pattern clearly
- Add XML comments explaining why separate type
- Reference this analysis in code comments

---

## Decision Criteria

### Choose ScenarioUserMessage Type If:

✅ You value **type safety** over simplicity
✅ You expect to add more scenario-specific properties
✅ You want to **eliminate fragile workarounds**
✅ You have time for proper testing
✅ You want **clean, maintainable** code long-term
✅ You follow **DDD principles** (domain models concepts)

### Choose Metadata Approach If:

✅ You need **minimal changes** (quick fix)
✅ This is a **one-time** scenario use case
✅ You want to **keep domain generic**
✅ You're okay with **runtime type checking**
✅ You want to **avoid type proliferation**

---

## Conclusion

Your instinct is correct: **ScenarioUserMessage as a distinct type is the cleanest long-term solution** despite requiring more upfront work.

### Summary

**Pros (Strong)**:
- Type safety, readability, maintainability
- Eliminates all workarounds and race conditions
- Follows existing pattern in codebase
- Future-proof and extensible

**Cons (Manageable)**:
- More code changes required (~10 files)
- Orchestrator API grows
- Sets precedent for type proliferation

**Verdict**: The cons are **one-time costs** during implementation, while the pros are **ongoing benefits** for the lifetime of the codebase.

### Recommendation

✅ **Implement ScenarioUserMessage as distinct domain type**

The extensive code changes are **worth it** for:
1. Eliminating 80+ lines of fragile workaround code
2. Providing reliable, race-condition-free display
3. Making the codebase more maintainable
4. Following clean architecture and DDD principles

The approach is **not the simplest**, but it is **the cleanest and most sustainable**.
