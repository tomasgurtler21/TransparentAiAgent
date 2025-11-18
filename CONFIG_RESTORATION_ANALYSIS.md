# Configuration Restoration Investigation - Deep Analysis

**⚠️ SESSION CRASH RESISTANCE NOTICE**
This file was created to preserve findings across potential session crashes. If the session crashes before completion, this file contains all investigation findings and open questions. Please review this file first before requesting new analysis.

**Investigation Date**: 2025-11-18
**Branch**: `claude/investigate-config-restoration-01H6pkPK6x66wqVK6PPfcnnK`
**Status**: 🔴 **CRITICAL BUG CONFIRMED** - Config restoration broken in multiple scenarios

---

## Executive Summary

Configuration restoration after scenarios is fundamentally broken. The message content limit does not restore to its original value after a scenario completes, leaving users stuck with the scenario's reduced limit (e.g., 8 messages instead of 200). Additionally, configuration overlays fail to work during normal conversation, and the limit set at conversation start persists regardless of subsequent changes.

**Root Cause**: Missing event-driven architecture for configuration changes. ConversationManager has no way to know when overlays change.

---

## Issue Description (From User)

### Observed Behavior

1. **Scenario Context Limit Stuck**:
   - User starts with `messageLimit: 200` (set in local appsettings.json)
   - User runs the "Context Limits (Advanced)" scenario
   - Scenario applies overlay with `messageLimit: 8`
   - Scenario completes successfully (all steps executed, UI indicator gone)
   - **BUG**: Context limit remains at 8 messages instead of restoring to 200
   - When continuing conversation, old messages are truncated immediately using the stuck limit of 8

2. **Configuration Overlay Doesn't Work**:
   - User tries to change context limit using Configuration Overlay during normal conversation
   - **BUG**: Limit does not change
   - "It looks like limit from start of the conversation always applies, whatever is set afterwards"

3. **Scenario CAN Set Limits (Partially)**:
   - Interestingly, scenarios CAN change the limit mid-conversation
   - User started conversation with limit 200, sent messages, then ran scenario
   - Got stuck back to 8/10 messages (scenario's limit)
   - This proves overlays work while active, but restoration is broken

4. **Verified Not a UI Issue**:
   - User inspected actual messages sent to LLM
   - UI truncation indicators match what's sent to the API
   - Problem is in the backend logic, not UI rendering

---

## Technical Investigation

### Architecture Overview

The system uses a layered configuration architecture:

```
┌─────────────────────────────────────────────────────┐
│ Scenario Execution                                   │
│ - Applies/Restores Config Overlays                  │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│ ConfigurationOverlayService (Stack-based)           │
│ - Maintains overlay stack                           │
│ - Provides GetValue() with fallback                 │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│ ConversationManager                                 │
│ - Stores ContextWindowSize (initial value)          │
│ - TruncateIfNeeded() reads overlay                  │
│ - Marks messages as InContext/TruncatedFromContext  │
└─────────────────────────────────────────────────────┘
```

### Key Components Analyzed

#### 1. ConfigurationOverlayService (`TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationOverlayService.cs`)

**Purpose**: Stack-based overlay system for temporary config changes

**Key Methods**:
- `PushOverlay(overlayValues)` - Pushes new overlay onto stack
- `PopOverlay()` - Removes most recent overlay
- `GetValue<T>(key, defaultValue)` - Gets effective value from stack

**Implementation**:
```csharp
// Line 60-79
public T? GetValue<T>(string key)
{
    lock (_lock)
    {
        // Search from most recent overlay to oldest
        foreach (var overlay in _overlayStack)
        {
            if (overlay.TryGetValue(key, out var value))
            {
                return ConvertValue<T>(value);
            }
        }

        // Fall back to base configuration
        if (_baseConfiguration.TryGetValue(key, out var baseValue))
        {
            return ConvertValue<T>(baseValue);
        }

        // Key not found anywhere
        return default;
    }
}
```

**Registration** (`TransparentAiAgentGui/Program.cs:450-455`):
```csharp
builder.Services.AddScoped<IConfigurationOverlay>(sp =>
{
    // Initialize with empty base configuration - could be expanded to load from appsettings if needed
    var baseConfig = new Dictionary<string, object>();
    return new ConfigurationOverlayService(baseConfig);
});
```

**🐛 BUG #1**: Base configuration is **EMPTY**. The messageLimit from appsettings.json is never loaded into the base configuration. The system relies entirely on the fallback parameter in GetValue() calls.

#### 2. ConversationManager (`TransparentAiAgentCore/Application/Conversation/ConversationManager.cs`)

**Purpose**: Manages conversation history and context window

**Key Properties**:
- `ContextWindowSize` (line 20) - Stored at initialization, has private setter
- Only updated via explicit call to `UpdateContextWindowSize()`

**Key Methods**:
- `AddMessage()` (line 46) - Adds message and calls `TruncateIfNeeded()`
- `UpdateContextWindowSize()` (line 150) - Updates stored size and calls `TruncateIfNeeded()`
- `TruncateIfNeeded()` (line 170) - **CRITICAL METHOD** for truncation logic

**Truncation Logic** (line 170-214):
```csharp
private void TruncateIfNeeded()
{
    // Get effective context window size from overlay or use default
    var effectiveWindowSize = ContextWindowSize;
    if (_configurationOverlay != null)
    {
        effectiveWindowSize = _configurationOverlay.GetValue("messageLimit", ContextWindowSize);
        if (effectiveWindowSize != ContextWindowSize)
        {
            LogEvent("EffectiveContextWindowSize", $"Using overlay messageLimit: {effectiveWindowSize} (base: {ContextWindowSize})");
        }
    }

    var inContextMessages = _messages
        .Where(m => m.ContextStatus == MessageContextStatus.InContext)
        .ToList();

    if (inContextMessages.Count <= effectiveWindowSize)
        return; // No truncation needed

    // How many messages to truncate
    int toTruncate = inContextMessages.Count - effectiveWindowSize;

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

    LogEvent("ContextTruncated", $"{toTruncate} messages truncated. In context: {InContextMessageCount}/{effectiveWindowSize}");
}
```

**🐛 BUG #2**: `TruncateIfNeeded()` is **ONLY called when messages are added** (line 56) or when `UpdateContextWindowSize()` is explicitly called. It is **NEVER called when overlays are pushed/popped**.

**🐛 BUG #3**: `TruncateIfNeeded()` has **ONE-WAY LOGIC**. It only marks messages as `TruncatedFromContext`. It **NEVER restores** truncated messages back to `InContext` when the limit increases.

**Registration** (`TransparentAiAgentGui/Program.cs:138-144`):
```csharp
builder.Services.AddScoped<IConversationManager>(sp =>
{
    var config = sp.GetRequiredService<AppConfiguration>();
    var transparencyService = sp.GetRequiredService<ITransparencyService>();
    var configurationOverlay = sp.GetRequiredService<IConfigurationOverlay>();
    return new ConversationManager(config.Agent.ContextWindowSize, transparencyService, configurationOverlay);
});
```

Note: Both `IConversationManager` and `IConfigurationOverlay` are Scoped, so they share the same instance within a user's session.

#### 3. ScenarioExecutor (`TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`)

**Purpose**: Executes scenario steps including config overlay management

**Overlay Cleanup** (line 156-172 finally block):
```csharp
finally
{
    // Ensure configuration overlays are cleaned up
    // Pop any overlays that were pushed during the scenario
    while (_configurationOverlay.OverlayCount > _initialOverlayCount)
    {
        _configurationOverlay.PopOverlay();
    }

    lock (_lock)
    {
        IsExecuting = false;
        CurrentScenario = null;
        CurrentStepIndex = -1;
        _cts?.Dispose();
        _cts = null;
    }
}
```

**Apply/Restore Steps**:
- `ExecuteApplyConfigOverlayStep()` (line 430-436) - Explicitly pushes overlay
- `ExecuteRestoreConfigOverlayStep()` (line 438-444) - Explicitly pops overlay

**🐛 BUG #4**: After popping the overlay (either via `restore_config_overlay` step or finally block), **NO notification** is sent to ConversationManager. The overlay is removed silently.

#### 4. Scenario Definition (`TransparentAiAgentGui/data/scenarios/context-limits-advanced.json`)

The scenario demonstrates the bug:

**Step 0** - Apply overlay with messageLimit=8:
```json
{
  "type": "apply_config_overlay",
  "overlay": {
    "messageLimit": 8,
    "systemPromptAddition": "Note: You are experiencing a teaching scenario about context windows."
  },
  "annotation": "Message limit reduced to 10 to demonstrate truncation."
}
```

**Step 109** - Restore (pop overlay):
```json
{
  "type": "restore_config_overlay",
  "annotation": "Message limit restored. Model can now access more context and teach effectively."
}
```

The annotation claims "Message limit restored" but this is **FALSE** in practice.

---

## Root Cause Analysis

### The Critical Flow (Scenario Execution)

1. **Conversation starts**:
   - `ContextWindowSize = 200` (from appsettings.json)
   - `_configurationOverlay` stack is empty
   - User sends 30 messages, all are `InContext`

2. **Scenario applies overlay** (step 0):
   - `_configurationOverlay.PushOverlay({ messageLimit: 8 })`
   - Overlay stack now has 1 overlay

3. **Next scenario step adds message**:
   - `ConversationManager.AddMessage()` is called
   - `TruncateIfNeeded()` is called
   - Reads `effectiveWindowSize = _configurationOverlay.GetValue("messageLimit", 200) = 8`
   - Truncates: 30 - 8 = 22 messages marked as `TruncatedFromContext`
   - Only 8 messages remain `InContext`

4. **Scenario continues** (steps 1-108):
   - More messages are added and truncated
   - Context window maintained at 8 messages

5. **Scenario restores overlay** (step 109):
   - `_configurationOverlay.PopOverlay()`
   - Overlay stack is now empty
   - **BUT NO MESSAGE IS ADDED YET**
   - `TruncateIfNeeded()` is **NOT CALLED**

6. **Finally block** (scenario completion):
   - Ensures overlays are popped (already done in step 109)
   - No additional action needed
   - **STILL NO CALL TO `TruncateIfNeeded()`**

7. **User continues conversation**:
   - User adds new message
   - `ConversationManager.AddMessage()` is called
   - `TruncateIfNeeded()` is called
   - Reads `effectiveWindowSize = _configurationOverlay.GetValue("messageLimit", 200) = 200`
   - Current `inContextMessages.Count = 9` (8 from scenario + 1 new)
   - Condition: `9 <= 200` → **TRUE**
   - `TruncateIfNeeded()` **RETURNS EARLY** (line 188)
   - **NO RESTORATION HAPPENS**
   - The 22 messages marked as `TruncatedFromContext` **STAY TRUNCATED FOREVER**

### Why Messages Stay Truncated

The `TruncateIfNeeded()` method has **asymmetric logic**:

**Truncation Path** (limit decreases):
```
inContextMessages.Count (30) > effectiveWindowSize (8)
→ Mark 22 oldest messages as TruncatedFromContext
→ Only 8 messages remain InContext
```

**Restoration Path** (limit increases):
```
inContextMessages.Count (8) <= effectiveWindowSize (200)
→ Early return, NO ACTION TAKEN
→ Truncated messages STAY truncated
```

**Expected Restoration Logic** (NOT IMPLEMENTED):
```
if (inContextMessages.Count < effectiveWindowSize)
{
    // Restore messages from TruncatedFromContext back to InContext
    var truncatedMessages = _messages
        .Where(m => m.ContextStatus == MessageContextStatus.TruncatedFromContext)
        .OrderBy(m => m.Timestamp)
        .Take(effectiveWindowSize - inContextMessages.Count)
        .ToList();

    foreach (var message in truncatedMessages)
    {
        message.ContextStatus = MessageContextStatus.InContext;
    }
}
```

### Why Configuration Overlay Changes Don't Work

When the user manually pushes an overlay during normal conversation:

1. User calls `_configurationOverlay.PushOverlay({ messageLimit: 100 })`
2. Overlay is pushed successfully
3. **BUT** `TruncateIfNeeded()` is not called
4. Next time a message is added, truncation will use the new limit
5. **BUT** if no message is added, the overlay has no effect
6. Even worse: if the user expects immediate effect, they won't see it

**Missing**: Event-driven notification when overlay changes.

---

## Critical Issues Summary

### 🔴 Issue #1: No Event-Driven Architecture for Config Changes

**Problem**: `ConversationManager` has no way to know when overlays are pushed/popped.

**Impact**:
- Config changes don't take effect until next message is added
- Overlay restoration after scenarios is silent and ineffective
- User expectations violated (config changes should be immediate)

**Required Fix**: Implement event system for overlay changes:
```csharp
public interface IConfigurationOverlay
{
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    // ... existing methods
}
```

### 🔴 Issue #2: Asymmetric Truncation Logic (One-Way Only)

**Problem**: `TruncateIfNeeded()` only marks messages as truncated, never restores them.

**Impact**:
- When overlay is popped and limit increases, truncated messages stay truncated
- User expects to see older messages when limit increases, but they never return
- Context window permanently damaged after scenario

**Required Fix**: Implement bidirectional logic:
1. If `inContextMessages.Count > effectiveWindowSize` → Truncate
2. If `inContextMessages.Count < effectiveWindowSize` → **Restore** truncated messages

### 🔴 Issue #3: Base Configuration Empty

**Problem**: `ConfigurationOverlayService` initialized with empty base config.

**Impact**:
- System relies entirely on fallback parameters in `GetValue()` calls
- If a caller forgets the fallback, they get `default(T)` (0 for int)
- Fragile design, prone to bugs

**Required Fix**: Load base configuration from `AppConfiguration` during initialization:
```csharp
builder.Services.AddScoped<IConfigurationOverlay>(sp =>
{
    var appConfig = sp.GetRequiredService<AppConfiguration>();
    var baseConfig = new Dictionary<string, object>
    {
        { "messageLimit", appConfig.Agent.ContextWindowSize },
        // Add other base config values as needed
    };
    return new ConfigurationOverlayService(baseConfig);
});
```

### 🟡 Issue #4: Missing Immediate Feedback for Config Changes

**Problem**: When config is changed (via UI or overlay), user doesn't see immediate effect.

**Impact**:
- Poor UX - user changes setting, nothing happens until next message
- Violates principle of least surprise
- Makes debugging difficult

**Required Fix**: Explicitly call `TruncateIfNeeded()` after config changes, AND implement restoration logic.

---

## Reproduction Steps

### Scenario 1: Scenario Config Not Restored

1. Start application with `ContextWindowSize: 200` in appsettings.json
2. Start new conversation
3. Send 30 messages (all should be InContext)
4. Run "Context Limits (Advanced)" scenario
5. Observe: Messages are truncated to 8 during scenario ✓
6. Wait for scenario to complete
7. Send new message
8. **BUG**: Only 9 messages are InContext (8 from scenario + 1 new)
9. **EXPECTED**: 31 messages should be InContext (200 limit restored)

### Scenario 2: Manual Overlay Doesn't Work

1. Start conversation with limit 200
2. Send 50 messages
3. Programmatically: `configOverlay.PushOverlay({ messageLimit: 10 })`
4. **BUG**: All 50 messages still InContext (no immediate effect)
5. Send new message (51st)
6. Observe: Now only 11 messages are InContext (overlay took effect)
7. **EXPECTED**: Truncation should happen immediately after PushOverlay()

### Scenario 3: Overlay Restoration Doesn't Restore Messages

1. Start conversation with limit 200
2. Send 100 messages (all InContext)
3. Push overlay: `configOverlay.PushOverlay({ messageLimit: 20 })`
4. Send message → 80 messages marked as TruncatedFromContext
5. Pop overlay: `configOverlay.PopOverlay()`
6. Send message
7. **BUG**: Still only 21 messages are InContext
8. **EXPECTED**: Should restore back to 101 messages InContext

---

## Questions for User

### Q1: User Workflow Clarification

You mentioned "When I use Configuration overlay to change context limit, it does not work either."

Could you clarify:
- Are you using the UI Configuration page (`/api/config/agent-config` endpoint)?
- Or are you programmatically calling `IConfigurationOverlay.PushOverlay()`?
- Or something else?

### Q2: Expected Behavior After Scenario

When the scenario ends and the overlay is popped, what do you expect to see?

**Option A**: Immediate restoration
- All truncated messages should immediately become InContext again
- Context window should immediately expand to 200

**Option B**: Gradual restoration
- Truncated messages stay truncated
- But NEW messages are not truncated (limit is back to 200)
- Old messages never come back

### Q3: Overlay Stacking

Have you tried scenarios where multiple overlays are stacked?
- Example: Start with 200, push overlay to 100, then push another to 50
- Do you expect popping to work correctly (50 → 100 → 200)?

---

## Proposed Solutions

### Solution 1: Implement Event-Driven Config Changes (RECOMMENDED)

**Changes Required**:

1. Add event to `IConfigurationOverlay`:
```csharp
public interface IConfigurationOverlay
{
    event EventHandler<ConfigurationChangedEventArgs>? OverlayChanged;
    // ... existing methods
}

public class ConfigurationChangedEventArgs : EventArgs
{
    public ChangeType Type { get; }  // Push, Pop, Clear
    public IReadOnlyDictionary<string, object>? ChangedValues { get; }
}
```

2. Fire events in `ConfigurationOverlayService`:
```csharp
public void PushOverlay(IReadOnlyDictionary<string, object> overlayValues)
{
    lock (_lock)
    {
        _overlayStack.Push(new Dictionary<string, object>(overlayValues));
        OverlayChanged?.Invoke(this, new ConfigurationChangedEventArgs(ChangeType.Push, overlayValues));
    }
}

public void PopOverlay()
{
    lock (_lock)
    {
        if (_overlayStack.Count == 0)
            throw new InvalidOperationException("No configuration overlays to pop.");

        var popped = _overlayStack.Pop();
        OverlayChanged?.Invoke(this, new ConfigurationChangedEventArgs(ChangeType.Pop, popped));
    }
}
```

3. Subscribe in `ConversationManager`:
```csharp
public ConversationManager(
    int contextWindowSize,
    ITransparencyService transparencyService,
    IConfigurationOverlay? configurationOverlay = null)
{
    // ... existing code ...

    if (_configurationOverlay != null)
    {
        _configurationOverlay.OverlayChanged += OnConfigurationOverlayChanged;
    }
}

private void OnConfigurationOverlayChanged(object? sender, ConfigurationChangedEventArgs e)
{
    lock (_lock)
    {
        // Re-evaluate truncation with new overlay values
        TruncateIfNeeded();
    }
}
```

### Solution 2: Implement Bidirectional Truncation Logic (REQUIRED)

**Changes Required**:

Update `TruncateIfNeeded()` to handle both truncation AND restoration:

```csharp
private void TruncateIfNeeded()
{
    // Get effective context window size from overlay or use default
    var effectiveWindowSize = ContextWindowSize;
    if (_configurationOverlay != null)
    {
        effectiveWindowSize = _configurationOverlay.GetValue("messageLimit", ContextWindowSize);
    }

    var inContextMessages = _messages
        .Where(m => m.ContextStatus == MessageContextStatus.InContext)
        .ToList();

    var currentCount = inContextMessages.Count;

    // CASE 1: Too many messages - need to truncate
    if (currentCount > effectiveWindowSize)
    {
        int toTruncate = currentCount - effectiveWindowSize;

        var messagesToTruncate = inContextMessages
            .OrderBy(m => m.Role == MessageRole.System ? 1 : 0)
            .ThenBy(m => m.Timestamp)
            .Take(toTruncate)
            .ToList();

        foreach (var message in messagesToTruncate)
        {
            var oldStatus = message.ContextStatus;
            message.ContextStatus = MessageContextStatus.TruncatedFromContext;
            ContextStatusChanged?.Invoke(this,
                new ContextStatusChangedEventArgs(message.Id, oldStatus, message.ContextStatus));
            LogEvent("MessageTruncated", $"Message {message.Id} truncated from context");
        }

        LogEvent("ContextTruncated",
            $"{toTruncate} messages truncated. In context: {InContextMessageCount}/{effectiveWindowSize}");
    }
    // CASE 2: Room for more messages - restore truncated ones
    else if (currentCount < effectiveWindowSize)
    {
        int toRestore = effectiveWindowSize - currentCount;

        var truncatedMessages = _messages
            .Where(m => m.ContextStatus == MessageContextStatus.TruncatedFromContext)
            .OrderBy(m => m.Timestamp)  // Restore oldest first (FIFO)
            .Take(toRestore)
            .ToList();

        if (truncatedMessages.Count > 0)
        {
            foreach (var message in truncatedMessages)
            {
                var oldStatus = message.ContextStatus;
                message.ContextStatus = MessageContextStatus.InContext;
                ContextStatusChanged?.Invoke(this,
                    new ContextStatusChangedEventArgs(message.Id, oldStatus, message.ContextStatus));
                LogEvent("MessageRestored", $"Message {message.Id} restored to context");
            }

            LogEvent("ContextRestored",
                $"{truncatedMessages.Count} messages restored. In context: {InContextMessageCount}/{effectiveWindowSize}");
        }
    }
    // CASE 3: Perfect fit - no action needed
}
```

### Solution 3: Populate Base Configuration (NICE-TO-HAVE)

**Changes Required**:

Update DI registration to load base config from `AppConfiguration`:

```csharp
builder.Services.AddScoped<IConfigurationOverlay>(sp =>
{
    var appConfig = sp.GetRequiredService<AppConfiguration>();

    // Populate base configuration with values from appsettings.json
    var baseConfig = new Dictionary<string, object>
    {
        { "messageLimit", appConfig.Agent.ContextWindowSize },
        { "systemPrompt", appConfig.Agent.SystemPrompt },
        { "enableTools", appConfig.Agent.EnableTools },
        { "toolExecutionMode", appConfig.Agent.ToolExecutionMode.ToString() },
        // Add more as needed
    };

    return new ConfigurationOverlayService(baseConfig);
});
```

**Benefits**:
- More robust fallback mechanism
- Less reliance on default parameters
- Explicit base configuration makes the system easier to understand
- Future-proof for additional configuration overlays

---

## Testing Strategy

### Unit Tests Required

1. **ConfigurationOverlayService**:
   - Test event firing on Push/Pop/Clear
   - Test GetValue() with multiple overlays
   - Test base config fallback

2. **ConversationManager**:
   - Test TruncateIfNeeded() with limit decrease (truncation)
   - Test TruncateIfNeeded() with limit increase (restoration)
   - Test TruncateIfNeeded() with overlay changes
   - Test ContextStatusChanged events fire correctly

3. **ScenarioExecutor**:
   - Test overlay cleanup in finally block
   - Test apply_config_overlay step
   - Test restore_config_overlay step
   - Test overlay restoration triggers ConversationManager update

### Integration Tests Required

1. **Scenario Execution**:
   - Run complete scenario with config overlay
   - Verify messages truncated during scenario
   - Verify messages restored after scenario
   - Verify correct message count at each step

2. **Manual Overlay**:
   - Push overlay mid-conversation
   - Verify immediate truncation
   - Pop overlay
   - Verify immediate restoration

3. **UI Config Changes**:
   - Change context window via UI
   - Verify immediate effect
   - Verify correct behavior with messages

---

## Implementation Priority

### Phase 1: Critical Fixes (MUST DO)
1. ✅ Implement event-driven config changes (Solution 1)
2. ✅ Implement bidirectional truncation logic (Solution 2)
3. ✅ Add comprehensive unit tests
4. ✅ Test with existing scenario

### Phase 2: Enhancements (SHOULD DO)
1. ✅ Populate base configuration (Solution 3)
2. ✅ Add integration tests
3. ✅ Update scenario documentation to reflect correct behavior
4. ✅ Add transparency logging for restoration

### Phase 3: Future Improvements (NICE TO HAVE)
1. ⬜ Add UI indicator when config overlay is active
2. ⬜ Add UI control to manually trigger restoration
3. ⬜ Add metrics/telemetry for truncation/restoration events
4. ⬜ Consider more sophisticated restoration strategies (e.g., restore by relevance, not just FIFO)

---

## Files Modified (Summary)

### Core Changes
- `TransparentAiAgentCore/Domain/Configuration/IConfigurationOverlay.cs` - Add event
- `TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationOverlayService.cs` - Fire events
- `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs` - Subscribe to events, implement restoration

### Registration
- `TransparentAiAgentGui/Program.cs` - Populate base configuration

### Tests
- `TransparentAiAgentCore_Tests/Infrastructure/Configuration/ConfigurationOverlayServiceTests.cs` - Add event tests
- `TransparentAiAgentCore_Tests/Application/Conversation/ConversationManagerTests.cs` - Add restoration tests
- `TransparentAiAgentCore_Tests/Application/Scenarios/ScenarioExecutorTests.cs` - Add overlay restoration tests

---

## Related Documentation

- [Configuration Guide](docs/05-guides/deployment/configuration-guide.md)
- [Scenario Schema](docs/03-concepts/teaching-mode/scenario-schema.md)
- [Context Limits Scenario](docs/03-concepts/teaching-mode/reference-scenarios/context-limits-advanced.md)
- [Config Overlay Service Concept](docs/03-concepts/teaching-mode/config-overlay-service.md)

---

## Next Steps

1. **IMMEDIATE**: Await user response to clarifying questions
2. **AFTER CONFIRMATION**: Implement Phase 1 critical fixes
3. **AFTER TESTING**: Implement Phase 2 enhancements
4. **FINAL**: Update documentation with new behavior

---

**End of Analysis**

*This investigation confirms a systemic issue in the configuration restoration architecture. The fixes are well-defined and should resolve all reported issues.*
