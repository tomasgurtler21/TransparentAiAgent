# Scenario Pause/Resume Feature - Design & Implementation Plan

**Status**: Design Phase
**Created**: 2025-11-17
**Target**: Phase 10c (Teaching Mode Enhancement)

---

## Executive Summary

This document outlines the design and implementation plan for adding pause/resume functionality to teaching scenarios. The feature enables scenarios to pause themselves to prompt users to check something, and allows users to manually pause/resume scenarios at will.

---

## Use Cases

### Primary Use Case: Scenario-Initiated Pause
A scenario runs several steps, then pauses to let the user examine something (e.g., UI state, configuration, conversation history) before continuing.

**Example Flow:**
1. Scenario sends messages demonstrating a concept
2. Scenario pauses with message: "Notice the context indicator in the top right. Click Resume when you've examined it."
3. User examines the UI at their own pace
4. User clicks "Resume" button
5. Scenario continues with explanation

### Secondary Use Case: User-Initiated Pause
User wants to pause a running scenario to:
- Take a break and resume later
- Examine the current state more carefully
- Read annotations or messages without the scenario continuing

**Example Flow:**
1. Scenario is auto-playing through steps
2. User clicks "Pause" button in ScenarioIndicator
3. Scenario stops at current step
4. User examines state, reads messages, etc.
5. User clicks "Resume" to continue

---

## Current Architecture Analysis

### Scenario Execution Flow

**Current Implementation** (ScenarioExecutor.cs:44-110):
```csharp
public async Task ExecuteScenarioAsync(ScenarioDefinition scenario, CancellationToken cancellationToken)
{
    // ... validation and setup ...

    ScenarioStarted?.Invoke(this, new ScenarioExecutionEventArgs(scenario));

    // Sequential loop through steps
    for (int i = 0; i < scenario.Steps.Count; i++)
    {
        _cts.Token.ThrowIfCancellationRequested();

        var step = scenario.Steps[i];
        CurrentStepIndex = i;

        if (step.DelayMs > 0)
            await Task.Delay(step.DelayMs, _cts.Token);

        await ExecuteStepAsync(step, scenario, i, _cts.Token);

        StepExecuted?.Invoke(this, new ScenarioStepEventArgs(scenario, step, i));
    }

    ScenarioCompleted?.Invoke(this, new ScenarioExecutionEventArgs(scenario));
}
```

**Key Observations:**
- Uses a simple `for` loop to iterate through steps
- Uses `CancellationTokenSource` for stopping scenarios
- Fires events at scenario start, step completion, and scenario end
- `StopScenario()` method cancels via `_cts.Cancel()`
- No current mechanism to pause/resume

### UI Component (ScenarioIndicator.razor)

**Current Display:**
- Shows scenario name and step progress (e.g., "Step 3/10")
- Only visible when scenario is executing
- Subscribes to scenario events (ScenarioStarted, StepExecuted, ScenarioCompleted)

**Location**: TransparentAiAgentGui/Components/Scenarios/ScenarioIndicator.razor:5-18

---

## Design: Pause/Resume Architecture

### 1. State Management

Add a new state to track pause status in `ScenarioExecutor`:

```csharp
public enum ScenarioExecutionState
{
    NotRunning,
    Running,
    Paused,
    Completed,
    Failed
}
```

**New Properties:**
```csharp
private ScenarioExecutionState _state = ScenarioExecutionState.NotRunning;
private SemaphoreSlim _pauseSemaphore = new SemaphoreSlim(0);
private readonly object _pauseLock = new object();

public ScenarioExecutionState State { get; private set; }
public bool IsPaused => State == ScenarioExecutionState.Paused;
```

### 2. Pause Mechanism

**Two Ways to Pause:**

#### A. User-Initiated Pause
New method on `IScenarioExecutor`:
```csharp
void PauseScenario();
```

Implementation:
```csharp
public void PauseScenario()
{
    lock (_pauseLock)
    {
        if (State != ScenarioExecutionState.Running)
            return; // Can only pause if running

        State = ScenarioExecutionState.Paused;
        ScenarioPaused?.Invoke(this, new ScenarioExecutionEventArgs(CurrentScenario!));
    }
}
```

#### B. Scenario-Initiated Pause
New step type: `PauseForUser`

**ScenarioStepType enum addition:**
```csharp
/// <summary>
/// Pauses scenario execution and waits for user to click Resume.
/// </summary>
PauseForUser
```

**ScenarioStep properties for this type:**
```csharp
/// <summary>
/// Message to display to user explaining why scenario is paused.
/// Used in PauseForUser steps.
/// </summary>
public string? PauseMessage { get; }
```

**JSON Schema:**
```json
{
  "type": "pause_for_user",
  "pauseMessage": "Take a moment to examine the context indicator in the top right. Click Resume when ready.",
  "annotation": "Pausing to let user explore the UI"
}
```

### 3. Resume Mechanism

New method on `IScenarioExecutor`:
```csharp
void ResumeScenario();
```

Implementation:
```csharp
public void ResumeScenario()
{
    lock (_pauseLock)
    {
        if (State != ScenarioExecutionState.Paused)
            return; // Can only resume if paused

        State = ScenarioExecutionState.Running;
        _pauseSemaphore.Release(); // Unblock the waiting task
        ScenarioResumed?.Invoke(this, new ScenarioExecutionEventArgs(CurrentScenario!));
    }
}
```

### 4. Modified Execution Loop

Update `ExecuteScenarioAsync` to check for pause after each step:

```csharp
public async Task ExecuteScenarioAsync(ScenarioDefinition scenario, CancellationToken cancellationToken)
{
    // ... validation and setup ...

    State = ScenarioExecutionState.Running;
    ScenarioStarted?.Invoke(this, new ScenarioExecutionEventArgs(scenario));

    try
    {
        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            _cts.Token.ThrowIfCancellationRequested();

            var step = scenario.Steps[i];
            CurrentStepIndex = i;

            // Apply delay if specified
            if (step.DelayMs > 0)
                await Task.Delay(step.DelayMs, _cts.Token);

            // Execute the step
            await ExecuteStepAsync(step, scenario, i, _cts.Token);

            // Fire step executed event
            StepExecuted?.Invoke(this, new ScenarioStepEventArgs(scenario, step, i));

            // Check if we should pause (either manually or via PauseForUser step)
            if (IsPaused)
            {
                // Wait for resume signal
                await _pauseSemaphore.WaitAsync(_cts.Token);
            }
        }

        State = ScenarioExecutionState.Completed;
        ScenarioCompleted?.Invoke(this, new ScenarioExecutionEventArgs(scenario));
    }
    catch (OperationCanceledException)
    {
        State = ScenarioExecutionState.NotRunning;
        // Scenario was cancelled/stopped
    }
    catch (Exception ex)
    {
        State = ScenarioExecutionState.Failed;
        ScenarioFailed?.Invoke(this, new ScenarioExecutionEventArgs(scenario, ex.Message));
        throw;
    }
    finally
    {
        // Cleanup
        lock (_pauseLock)
        {
            State = ScenarioExecutionState.NotRunning;
            CurrentScenario = null;
            CurrentStepIndex = -1;
            _cts?.Dispose();
            _cts = null;
        }
    }
}
```

### 5. PauseForUser Step Execution

Add to `ExecuteStepAsync` switch statement:

```csharp
case ScenarioStepType.PauseForUser:
    await ExecutePauseForUserStepAsync(step, cancellationToken);
    break;
```

Implementation:
```csharp
private async Task ExecutePauseForUserStepAsync(ScenarioStep step, CancellationToken cancellationToken)
{
    // Set pause state
    lock (_pauseLock)
    {
        State = ScenarioExecutionState.Paused;
    }

    // Fire pause event with message
    ScenarioPaused?.Invoke(this, new ScenarioPausedEventArgs(
        CurrentScenario!,
        step.PauseMessage ?? "Scenario paused. Click Resume to continue.",
        isUserInitiated: false));

    // Wait for resume (happens in main loop after this method returns)
    // No need to wait here, the main loop will handle it
}
```

### 6. Event System

New events on `IScenarioExecutor`:

```csharp
/// <summary>
/// Event fired when scenario is paused (user-initiated or scenario-initiated).
/// </summary>
event EventHandler<ScenarioPausedEventArgs>? ScenarioPaused;

/// <summary>
/// Event fired when scenario is resumed.
/// </summary>
event EventHandler<ScenarioExecutionEventArgs>? ScenarioResumed;
```

New event args class:
```csharp
public class ScenarioPausedEventArgs : EventArgs
{
    public ScenarioDefinition Scenario { get; }
    public string PauseMessage { get; }
    public bool IsUserInitiated { get; }

    public ScenarioPausedEventArgs(
        ScenarioDefinition scenario,
        string pauseMessage,
        bool isUserInitiated)
    {
        Scenario = scenario;
        PauseMessage = pauseMessage;
        IsUserInitiated = isUserInitiated;
    }
}
```

### 7. UI Component Changes

**ScenarioIndicator.razor enhancements:**

```razor
@using TransparentAiAgentCore.Application.Scenarios
@inject IScenarioExecutor ScenarioExecutor
@implements IDisposable

@if (ScenarioExecutor.IsExecuting && ScenarioExecutor.CurrentScenario != null)
{
    <div class="scenario-indicator">
        <div class="indicator-content">
            <span class="indicator-icon">🎬</span>
            <div class="indicator-info">
                <strong>@ScenarioExecutor.CurrentScenario.Name</strong>
                <span class="text-muted">
                    Step @currentStepNumber/@totalSteps
                </span>
                @if (ScenarioExecutor.IsPaused && !string.IsNullOrEmpty(pauseMessage))
                {
                    <div class="pause-message">
                        <span class="pause-icon">⏸️</span>
                        <span>@pauseMessage</span>
                    </div>
                }
            </div>
            <div class="indicator-actions">
                @if (ScenarioExecutor.IsPaused)
                {
                    <button class="btn btn-success btn-sm" @onclick="ResumeScenario">
                        ▶️ Resume
                    </button>
                }
                else if (ScenarioExecutor.State == ScenarioExecutionState.Running)
                {
                    <button class="btn btn-warning btn-sm" @onclick="PauseScenario">
                        ⏸️ Pause
                    </button>
                }
                <button class="btn btn-danger btn-sm" @onclick="StopScenario">
                    ⏹️ Stop
                </button>
            </div>
        </div>
    </div>
}

@code {
    private int currentStepNumber = 0;
    private int totalSteps = 0;
    private string pauseMessage = string.Empty;

    protected override void OnInitialized()
    {
        ScenarioExecutor.ScenarioStarted += OnScenarioStarted;
        ScenarioExecutor.StepExecuted += OnStepExecuted;
        ScenarioExecutor.ScenarioCompleted += OnScenarioCompleted;
        ScenarioExecutor.ScenarioPaused += OnScenarioPaused;
        ScenarioExecutor.ScenarioResumed += OnScenarioResumed;

        UpdateStepInfo();
    }

    private void OnScenarioStarted(object? sender, ScenarioExecutionEventArgs e)
    {
        pauseMessage = string.Empty;
        UpdateStepInfo();
        InvokeAsync(StateHasChanged);
    }

    private void OnStepExecuted(object? sender, ScenarioStepEventArgs e)
    {
        UpdateStepInfo();
        InvokeAsync(StateHasChanged);
    }

    private void OnScenarioCompleted(object? sender, ScenarioExecutionEventArgs e)
    {
        UpdateStepInfo();
        InvokeAsync(StateHasChanged);
    }

    private void OnScenarioPaused(object? sender, ScenarioPausedEventArgs e)
    {
        pauseMessage = e.PauseMessage;
        InvokeAsync(StateHasChanged);
    }

    private void OnScenarioResumed(object? sender, ScenarioExecutionEventArgs e)
    {
        pauseMessage = string.Empty;
        InvokeAsync(StateHasChanged);
    }

    private void UpdateStepInfo()
    {
        if (ScenarioExecutor.CurrentScenario != null)
        {
            currentStepNumber = ScenarioExecutor.CurrentStepIndex + 1;
            totalSteps = ScenarioExecutor.CurrentScenario.Steps.Count;
        }
        else
        {
            currentStepNumber = 0;
            totalSteps = 0;
        }
    }

    private void PauseScenario()
    {
        ScenarioExecutor.PauseScenario();
    }

    private void ResumeScenario()
    {
        ScenarioExecutor.ResumeScenario();
    }

    private void StopScenario()
    {
        ScenarioExecutor.StopScenario();
    }

    public void Dispose()
    {
        if (ScenarioExecutor != null)
        {
            ScenarioExecutor.ScenarioStarted -= OnScenarioStarted;
            ScenarioExecutor.StepExecuted -= OnStepExecuted;
            ScenarioExecutor.ScenarioCompleted -= OnScenarioCompleted;
            ScenarioExecutor.ScenarioPaused -= OnScenarioPaused;
            ScenarioExecutor.ScenarioResumed -= OnScenarioResumed;
        }
    }
}
```

**CSS additions (ScenarioIndicator.razor.css):**
```css
.indicator-actions {
    display: flex;
    gap: 0.5rem;
    align-items: center;
}

.pause-message {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-top: 0.5rem;
    padding: 0.5rem;
    background-color: #fff3cd;
    border-radius: 0.25rem;
    font-size: 0.9rem;
}

.pause-icon {
    font-size: 1.2rem;
}
```

---

## Implementation Plan

### Phase 1: Core Pause/Resume Logic (ScenarioExecutor)

**Files to Modify:**
1. `TransparentAiAgentCore/Application/Scenarios/IScenarioExecutor.cs`
2. `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`

**Changes:**
- [ ] Add `ScenarioExecutionState` enum
- [ ] Add pause/resume state management fields (`_pauseSemaphore`, `State`, etc.)
- [ ] Add `PauseScenario()` method to interface and implementation
- [ ] Add `ResumeScenario()` method to interface and implementation
- [ ] Add `ScenarioPaused` and `ScenarioResumed` events
- [ ] Create `ScenarioPausedEventArgs` class
- [ ] Modify `ExecuteScenarioAsync` to check for pause after each step
- [ ] Update `StopScenario()` to handle paused state properly
- [ ] Ensure proper cleanup in finally block

**Estimated Effort:** 2-3 hours

### Phase 2: PauseForUser Step Type

**Files to Modify:**
1. `TransparentAiAgentCore/Domain/Scenarios/ScenarioStepType.cs`
2. `TransparentAiAgentCore/Domain/Scenarios/ScenarioStep.cs`
3. `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`

**Changes:**
- [ ] Add `PauseForUser` to `ScenarioStepType` enum
- [ ] Add `PauseMessage` property to `ScenarioStep`
- [ ] Update `ScenarioStep` constructor to accept `pauseMessage` parameter
- [ ] Add validation for `PauseForUser` step type
- [ ] Implement `ExecutePauseForUserStepAsync` method
- [ ] Add case for `PauseForUser` in `ExecuteStepAsync` switch

**Estimated Effort:** 1-2 hours

### Phase 3: UI Component Updates

**Files to Modify:**
1. `TransparentAiAgentGui/Components/Scenarios/ScenarioIndicator.razor`
2. `TransparentAiAgentGui/Components/Scenarios/ScenarioIndicator.razor.css`

**Changes:**
- [ ] Add Pause/Resume buttons to UI
- [ ] Add pause message display area
- [ ] Subscribe to `ScenarioPaused` and `ScenarioResumed` events
- [ ] Implement button click handlers
- [ ] Update UI state on pause/resume
- [ ] Add CSS styling for buttons and pause message
- [ ] Ensure buttons are properly enabled/disabled based on state

**Estimated Effort:** 1-2 hours

### Phase 4: JSON Schema & Loader Updates

**Files to Modify:**
1. `TransparentAiAgentCore/Infrastructure/Scenarios/JsonScenarioLoader.cs`
2. `docs/03-concepts/teaching-mode/scenario-schema.md`

**Changes:**
- [ ] Update JSON loader to deserialize `pauseMessage` field
- [ ] Update JSON loader to handle `pause_for_user` step type
- [ ] Add validation for PauseForUser steps
- [ ] Update schema documentation with new step type
- [ ] Add examples of pause_for_user in documentation

**Estimated Effort:** 1 hour

### Phase 5: Testing

**Files to Create:**
1. `TransparentAiAgentCore_Tests/Application/Scenarios/ScenarioExecutorPauseTests.cs`

**Test Cases:**
- [ ] Test user-initiated pause during scenario execution
- [ ] Test user-initiated resume after pause
- [ ] Test scenario-initiated pause via PauseForUser step
- [ ] Test resume after PauseForUser step
- [ ] Test stop scenario while paused
- [ ] Test cancellation while paused
- [ ] Test pause/resume state transitions
- [ ] Test pause events are fired correctly
- [ ] Test pause message is passed correctly
- [ ] Test multiple pause/resume cycles
- [ ] Test pause at first step, middle step, last step

**Estimated Effort:** 2-3 hours

### Phase 6: Example Scenario

**Files to Create:**
1. `TransparentAiAgentGui/data/scenarios/pause-demo.json`

**Content:**
Create a demo scenario that showcases pause/resume functionality:
- Auto-plays a few steps
- Pauses with message asking user to examine something
- Resumes when user clicks Resume
- Demonstrates both scenario-initiated and potential user-initiated pauses

**Estimated Effort:** 30 minutes

### Phase 7: Documentation

**Files to Update:**
1. `docs/03-concepts/teaching-mode/scenario-schema.md`
2. `docs/04-components/core/scenario-executor.md` (if exists)

**Changes:**
- [ ] Document pause/resume feature in scenario schema
- [ ] Document PauseForUser step type with examples
- [ ] Document pause message field
- [ ] Update component documentation with new methods and events
- [ ] Add usage examples and best practices

**Estimated Effort:** 1 hour

---

## Total Estimated Effort

**Development:** 8-12 hours
**Testing:** 2-3 hours
**Documentation:** 1 hour
**Total:** 11-16 hours (approximately 2-3 development sessions)

---

## Edge Cases & Considerations

### 1. Pause During Streaming Response
**Question:** What happens if user pauses while a streaming LLM response is in progress?

**Options:**
- **A. Complete current step first** - Wait for streaming to finish, then pause (RECOMMENDED)
- **B. Immediate pause** - Cancel streaming and pause immediately
- **C. Delay pause** - Queue the pause to happen after current step

**Recommendation:** Option A - Complete current step (including streaming) before pausing. This ensures message integrity.

**Implementation:**
Check pause state AFTER `ExecuteStepAsync` completes, not during.

### 2. Pause State Persistence
**Question:** Should pause state persist across page refreshes or app restarts?

**Answer:** NO for initial implementation. If page refreshes, scenario stops. This is acceptable behavior for v1.

**Future Enhancement:** Could save scenario state to localStorage/sessionStorage if needed.

### 3. Multiple Consecutive Pauses
**Question:** Can a scenario have multiple PauseForUser steps?

**Answer:** YES. A scenario can pause multiple times at different points.

**Example Use Case:**
1. Demonstrate feature A → Pause for examination
2. Resume → Demonstrate feature B → Pause for examination
3. Resume → Explain both features

### 4. Pause During Delay
**Question:** If a step has `delayMs: 5000`, can user pause during the delay?

**Current Implementation:** No - delay happens before ExecuteStepAsync, pause check happens after.

**Enhancement (Optional):** Could make delay pause-aware by chunking it:
```csharp
// Instead of:
await Task.Delay(step.DelayMs, _cts.Token);

// Do:
await DelayWithPauseCheckAsync(step.DelayMs, _cts.Token);

private async Task DelayWithPauseCheckAsync(int delayMs, CancellationToken ct)
{
    var chunks = delayMs / 100; // Check every 100ms
    for (int i = 0; i < chunks; i++)
    {
        if (IsPaused)
            await _pauseSemaphore.WaitAsync(ct);
        await Task.Delay(100, ct);
    }
}
```

**Recommendation:** Implement simple version first, add enhancement if needed.

### 5. StopScenario While Paused
**Question:** What happens if user clicks Stop while scenario is paused?

**Implementation:**
```csharp
public void StopScenario()
{
    lock (_pauseLock)
    {
        // If paused, release semaphore so cancellation can propagate
        if (IsPaused)
        {
            State = ScenarioExecutionState.NotRunning;
            _pauseSemaphore.Release();
        }

        _cts?.Cancel();
    }
}
```

### 6. PauseForUser with Timeout
**Question:** Should PauseForUser have an optional timeout (auto-resume after X seconds)?

**Answer:** Not for v1, but good future enhancement.

**Future Schema:**
```json
{
  "type": "pause_for_user",
  "pauseMessage": "Examine the UI...",
  "autoResumeAfterSeconds": 30
}
```

### 7. Pause at WaitForResponse
**Question:** If scenario is waiting for LLM response, can it be paused?

**Answer:** Yes - same as streaming case. Complete the wait, then pause.

---

## Open Questions for Clarification

Please review and provide guidance on the following:

### Question 1: Pause Button Visibility
Should the Pause button be visible:
- **A.** Always during scenario execution (even during delays and waits)
- **B.** Only when actively executing a step (not during waits)
- **C.** Have a setting to enable/disable user-initiated pause per scenario

**My Recommendation:** Option A for maximum user control

---

### Question 2: Pause Message Localization
The PauseForUser step has a `pauseMessage` field. Should this:
- **A.** Support localization keys like other scenario content (e.g., `pauseMessageKey`)
- **B.** Be plain text only for v1
- **C.** Support both key and fallback text

**My Recommendation:** Option C - Support both, consistent with existing localization approach

**Implementation:**
```csharp
public string? PauseMessage { get; }
public string? PauseMessageKey { get; } // For localization
```

---

### Question 3: Pause State Indicator
How should the UI indicate scenario is paused? Current design shows:
- Pause message below scenario name
- Resume button (green)
- Pause icon (⏸️)

**Additional Options:**
- Change indicator background color when paused
- Add animation or pulse effect
- Show time paused counter

**My Recommendation:** Keep it simple for v1 - just message, icon, and button

---

### Question 4: Resume Confirmation
Should resuming require confirmation if user has been paused for a long time?

**Options:**
- **A.** Always resume immediately (simple)
- **B.** Show confirmation if paused > 5 minutes
- **C.** No confirmation needed

**My Recommendation:** Option A - immediate resume. If user clicked pause, they can click resume.

---

### Question 5: Step Execution During Pause
When paused at step N, should we show that:
- **A.** Step N is completed (current step = N+1)
- **B.** Step N is in progress (current step = N)

**Current Implementation:** Step N is complete when paused.

**Question:** Is this the desired behavior, or should pause happen BEFORE step completion?

**Trade-offs:**
- Pause after: Cleaner state, step fully executed
- Pause before: More control, but step state is unclear

**My Recommendation:** Pause AFTER step completion (current design)

---

### Question 6: PauseForUser in Basic vs Advanced Scenarios
Should `PauseForUser` be:
- **A.** Available in both basic and advanced scenarios
- **B.** Advanced scenarios only
- **C.** Basic scenarios only

**My Recommendation:** Option A - useful for both. It's a simple step type that doesn't require advanced features like config overlays.

---

## API Summary

### New Interface Methods (IScenarioExecutor)

```csharp
public interface IScenarioExecutor
{
    // Existing members...

    // New members for pause/resume:

    /// <summary>
    /// Gets the current execution state of the scenario.
    /// </summary>
    ScenarioExecutionState State { get; }

    /// <summary>
    /// Gets whether the scenario is currently paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Pauses the currently executing scenario.
    /// Can only be called when state is Running.
    /// </summary>
    void PauseScenario();

    /// <summary>
    /// Resumes a paused scenario.
    /// Can only be called when state is Paused.
    /// </summary>
    void ResumeScenario();

    /// <summary>
    /// Event fired when scenario is paused.
    /// </summary>
    event EventHandler<ScenarioPausedEventArgs>? ScenarioPaused;

    /// <summary>
    /// Event fired when scenario is resumed.
    /// </summary>
    event EventHandler<ScenarioExecutionEventArgs>? ScenarioResumed;
}
```

### New Enums

```csharp
public enum ScenarioExecutionState
{
    NotRunning,
    Running,
    Paused,
    Completed,
    Failed
}
```

### New Step Type

```csharp
public enum ScenarioStepType
{
    // ... existing types ...

    /// <summary>
    /// Pauses scenario execution and waits for user to click Resume.
    /// </summary>
    PauseForUser
}
```

### New Event Args

```csharp
public class ScenarioPausedEventArgs : EventArgs
{
    public ScenarioDefinition Scenario { get; }
    public string PauseMessage { get; }
    public bool IsUserInitiated { get; }

    public ScenarioPausedEventArgs(
        ScenarioDefinition scenario,
        string pauseMessage,
        bool isUserInitiated);
}
```

---

## Example Scenarios Using Pause/Resume

### Example 1: Context Limits with User Examination

```json
{
  "id": "context-limits-with-pause",
  "name": "Context Limits (Interactive)",
  "steps": [
    {
      "type": "scenario_user_message",
      "content": "Hi, my name is Alice."
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "pause_for_user",
      "pauseMessage": "Notice the model remembered the name. Look at the conversation history. Click Resume when ready.",
      "annotation": "Giving user time to examine current state"
    },
    {
      "type": "scenario_user_message",
      "content": "Demo message to fill context."
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "content": "What was my name?"
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "pause_for_user",
      "pauseMessage": "Notice the model now doesn't remember due to context truncation. Examine the context indicator. Click Resume to continue.",
      "annotation": "Letting user see the truncation effect"
    },
    {
      "type": "scenario_user_message",
      "content": "Why don't you remember?"
    },
    {
      "type": "wait_for_response"
    }
  ]
}
```

### Example 2: Tool Demonstration with Pauses

```json
{
  "id": "tool-demo-with-pause",
  "name": "Tool Usage Demo",
  "steps": [
    {
      "type": "scenario_user_message",
      "content": "Show me the current configuration"
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "pause_for_user",
      "pauseMessage": "The model just used the ui_control_configuration tool. Check the conversation to see the tool call details. Resume when ready.",
      "annotation": "Highlighting tool usage"
    },
    {
      "type": "scenario_user_message",
      "content": "Thank you! That was helpful."
    },
    {
      "type": "wait_for_response"
    }
  ]
}
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Deadlock in pause/resume logic | Low | High | Thorough testing with semaphore, use timeouts |
| UI not updating on pause/resume | Medium | Medium | Test event subscriptions, ensure InvokeAsync |
| Memory leak from semaphore | Low | Medium | Dispose semaphore in finally block |
| Race condition on state change | Low | High | Use locks consistently, test concurrent access |
| Pause during cleanup phase | Low | Low | Check state before allowing pause |

---

## Future Enhancements (Post-v1)

1. **Auto-resume timeout** - Automatically resume after N seconds if user doesn't click Resume
2. **Pause during delay** - Make delays pause-aware by chunking them
3. **Pause state persistence** - Save pause state to localStorage for page refresh recovery
4. **Pause reason tracking** - Track why scenario paused (user, step, timeout) for analytics
5. **Conditional pause** - Pause only if certain condition is met
6. **Multi-step pause** - Pause for multiple steps, not just one
7. **Pause history** - Track when scenario was paused/resumed for debugging

---

## Success Criteria

The pause/resume feature will be considered successful when:

1. ✅ User can click Pause button during scenario execution
2. ✅ Scenario stops executing new steps when paused
3. ✅ User can click Resume button to continue
4. ✅ Scenario continues from where it left off after resume
5. ✅ PauseForUser step type pauses scenario with custom message
6. ✅ UI clearly indicates paused state with message and buttons
7. ✅ Stop button works correctly when scenario is paused
8. ✅ All events fire correctly (ScenarioPaused, ScenarioResumed)
9. ✅ No memory leaks or resource issues from pause/resume cycles
10. ✅ Unit tests cover all pause/resume scenarios

---

## References

- **Current Executor Implementation**: TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs:44-110
- **Current UI Component**: TransparentAiAgentGui/Components/Scenarios/ScenarioIndicator.razor:5-18
- **Scenario Schema**: docs/03-concepts/teaching-mode/scenario-schema.md
- **Step Types**: TransparentAiAgentCore/Domain/Scenarios/ScenarioStepType.cs:6-68

---

## Next Steps

1. **Review this document** - Address open questions
2. **Approve design** - Confirm approach is sound
3. **Begin Phase 1** - Implement core pause/resume logic
4. **Iterate through phases** - Complete implementation systematically
5. **Test thoroughly** - Ensure no edge cases break functionality
6. **Update documentation** - Keep schema and component docs current

---

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Status:** Design Draft - Awaiting Review
