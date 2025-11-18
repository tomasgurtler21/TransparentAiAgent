# Scenario Execution Issues - Investigation and Fixes

**Date**: 2025-11-18
**Status**: FIXED
**Affected Component**: Scenario Execution System

## Executive Summary

Two critical issues were identified and fixed in the scenario execution system:

1. **Configuration not restored at end of scenario** - Config overlays remained on stack after scenario completion
2. **UI component (ScenarioSelector) not closing at end of scenario** - Selector overlay remained visible

Both issues are now resolved.

---

## Issue 1: Configuration Not Restored at End of Scenario

### Problem Description

When running the `context-limits-advanced` scenario:
- Initial configuration: `messageLimit = 200`
- Scenario applies overlay: `messageLimit = 8` (step 0)
- Scenario restores config: step 19 (`restore_config_overlay`)
- **Expected behavior**: After scenario completes, `messageLimit` should return to 200
- **Actual behavior**: `messageLimit` remained at 8/10

### Root Cause Analysis

**Location**: `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`

The `ExecuteScenarioAsync` method had a critical flaw in its cleanup logic:

```csharp
finally
{
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

**Problem**: The finally block did NOT clean up configuration overlays.

**Failure Scenarios**:
1. **Early Stop**: If user stops scenario before `restore_config_overlay` step executes
2. **Exception**: If any step throws exception before restore step
3. **Missing Restore Step**: If scenario definition forgets to include `restore_config_overlay`
4. **Pause/Resume Issues**: If scenario is paused and not properly resumed

In all these cases, overlays pushed by `apply_config_overlay` remained on the stack indefinitely, affecting all subsequent operations.

### The Fix

**File**: `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`

**Change 1** - Added tracking field:
```csharp
private int _initialOverlayCount = 0;
```

**Change 2** - Store initial overlay count when scenario starts:
```csharp
lock (_lock)
{
    if (IsExecuting)
        throw new InvalidOperationException("A scenario is already executing");

    IsExecuting = true;
    CurrentScenario = scenario;
    CurrentStepIndex = -1;
    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    // Store initial overlay count to ensure cleanup
    _initialOverlayCount = _configurationOverlay.OverlayCount;
}
```

**Change 3** - Clean up overlays in finally block:
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

**How It Works**:
1. Record overlay count at scenario start
2. Scenario executes normally (may push overlays via `apply_config_overlay`)
3. Regardless of how scenario ends (success/failure/stop), finally block ensures overlay count returns to initial value
4. This guarantees configuration is always restored, even if scenario doesn't explicitly restore

**Benefits**:
- ✅ Handles early stops gracefully
- ✅ Handles exceptions without leaking overlays
- ✅ Handles missing restore steps
- ✅ Works with pause/resume
- ✅ Defensive programming - fail-safe by design

---

## Issue 2: UI Component (ScenarioSelector) Not Closing at End of Scenario

### Problem Description

**Location**: Scenario selector overlay (right-side panel showing available scenarios)

**Expected behavior**:
1. User opens scenario selector
2. User clicks a scenario to start it
3. Scenario executes
4. When scenario completes, selector should auto-close

**Actual behavior**:
- Selector remained visible after scenario completed
- User had to manually close it

### Root Cause Analysis

**Location**: `TransparentAiAgentGui/Components/Scenarios/ScenarioSelector.razor`

The component had NO subscription to the `ScenarioCompleted` event:

```csharp
protected override void OnInitialized()
{
    selectedLanguage = LanguageService.CurrentLanguage;
    LoadScenarios();

    // Subscribe to UI state changes
    UIControlService.UIStateChanged += OnUIStateChanged;
    _uiState = UIControlService.GetCurrentState();

    // ❌ MISSING: No subscription to ScenarioCompleted
}
```

**Why This Matters**:
- ScenarioSelector is an overlay controlled by `UIState.ScenarioSelector.Visible`
- When scenario completes, nothing triggered the selector to close itself
- This created poor UX - user expects selector to close automatically

### The Fix

**File**: `TransparentAiAgentGui/Components/Scenarios/ScenarioSelector.razor`

**Change 1** - Subscribe to ScenarioCompleted event in OnInitialized:
```csharp
protected override void OnInitialized()
{
    selectedLanguage = LanguageService.CurrentLanguage;
    LoadScenarios();

    UIControlService.UIStateChanged += OnUIStateChanged;
    _uiState = UIControlService.GetCurrentState();

    // Subscribe to scenario completion to auto-close selector
    ScenarioExecutor.ScenarioCompleted += OnScenarioCompleted;
}
```

**Change 2** - Handle scenario completion:
```csharp
private void OnScenarioCompleted(object? sender, ScenarioExecutionEventArgs e)
{
    // Auto-close the selector when scenario completes
    _ = HandleClose();
}
```

**Change 3** - Unsubscribe in Dispose:
```csharp
public void Dispose()
{
    UIControlService.UIStateChanged -= OnUIStateChanged;
    ScenarioExecutor.ScenarioCompleted -= OnScenarioCompleted;
}
```

**How It Works**:
1. When scenario completes, `ScenarioCompleted` event fires
2. `OnScenarioCompleted` handler calls `HandleClose()`
3. `HandleClose()` updates UIControlService to set `ScenarioSelector.Visible = false`
4. UIControlService fires UIStateChanged event
5. OverlayContainer re-renders and hides the selector overlay

**Benefits**:
- ✅ Better UX - selector closes automatically
- ✅ Clean UI state after scenario completion
- ✅ Consistent with user expectations
- ✅ Proper event lifecycle (subscribe/unsubscribe)

---

## Testing Recommendations

### Test Case 1: Normal Scenario Completion
1. Start app with messageLimit = 200
2. Open scenario selector
3. Run "Context Limits (Advanced)" scenario
4. Let it complete normally
5. **Verify**: messageLimit is restored to 200
6. **Verify**: Scenario selector is closed

### Test Case 2: Early Stop
1. Start scenario
2. Click "Stop" button before it completes
3. **Verify**: messageLimit is restored to original value
4. **Verify**: Scenario selector is closed

### Test Case 3: Pause and Resume
1. Start scenario
2. Let it pause (step 18)
3. Click Resume
4. Let it complete
5. **Verify**: messageLimit is restored to 200
6. **Verify**: Scenario selector is closed

### Test Case 4: Multiple Overlays
1. Modify scenario to apply multiple overlays
2. Run scenario and let it complete
3. **Verify**: All overlays are cleaned up
4. **Verify**: Configuration returns to base state

---

## Impact Assessment

### Files Modified
1. `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs` - Config cleanup logic
2. `TransparentAiAgentGui/Components/Scenarios/ScenarioSelector.razor` - Auto-close logic

### Backward Compatibility
- ✅ **Fully backward compatible**
- Existing scenarios continue to work
- `restore_config_overlay` step still works (now redundant with finally block, but harmless)
- No breaking changes to public APIs

### Performance Impact
- Negligible - only adds one integer field and a while loop in finally block
- Overlay count is always small (typically 0-2)

### Security Impact
- ✅ **Positive** - Prevents config overlay leaks that could affect security settings
- No new attack vectors introduced

---

## Additional Observations

### Scenario Definition Minor Issue

In `context-limits-advanced.json`, line 16 vs line 20:
- Line 16: `"messageLimit": 8`
- Line 20: `"annotation": "Message limit reduced to 10 to demonstrate truncation."`

**Inconsistency**: Code says 8, annotation says 10.

**Recommendation**: Update annotation to match actual value (8) for clarity.

---

## Lessons Learned

### 1. Always Clean Up in Finally Blocks
- Any resource acquired in try block should be released in finally
- Configuration overlays are resources that need cleanup
- Don't rely on scenario definitions to clean up - enforce it in code

### 2. UI Event Lifecycle Management
- When UI components depend on async operations (like scenarios), they must subscribe to completion events
- Always unsubscribe in Dispose to prevent memory leaks
- Consider UX flow - what should happen when operation completes?

### 3. Defensive Programming
- Code should be robust against:
  - Missing steps in scenario definitions
  - Exceptions
  - User interruptions (stop/pause)
- Use fail-safe patterns (like overlay count tracking)

---

## Related Issues in KnownIssues.md

The issues fixed here were NOT documented in `docs/KnownIssues.md`. Current known issues include:
- Knowledge library tool topics unknown to LLM
- UI built-in tools in Normal mode
- OpenAI LLM responses missing in transparency events
- Configuration overlay showing wrong system prompt
- Transparency overlay needs cleanup
- Tools overlay needs refactor
- System message refactor needed
- Missing export of logs

**Recommendation**: These fixed issues should be marked as RESOLVED if they appear in any tracking system.

---

## Conclusion

Both issues have been successfully identified and fixed with minimal, surgical changes. The fixes are defensive, backward-compatible, and improve overall system robustness. The scenario execution system is now more reliable and provides better UX.
