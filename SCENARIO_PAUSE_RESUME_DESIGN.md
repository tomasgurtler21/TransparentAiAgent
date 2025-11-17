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
private string? _pauseMessage = null;

public ScenarioExecutionState State { get; private set; }
public bool IsPaused => State == ScenarioExecutionState.Paused;
```

### 2. Pause Mechanism

**Single Pause Mechanism with Two Triggers:**

The pause mechanism is the same regardless of how it's triggered. The only difference is whether a pause message is provided.

#### A. User-Initiated Pause (via UI Button)
New method on `IScenarioExecutor`:
```csharp
void PauseScenario(string? message = null);
```

Implementation:
```csharp
public void PauseScenario(string? message = null)
{
    lock (_pauseLock)
    {
        if (State != ScenarioExecutionState.Running)
            return; // Can only pause if running

        State = ScenarioExecutionState.Paused;
        _pauseMessage = message;
        ScenarioPaused?.Invoke(this, new ScenarioPausedEventArgs(CurrentScenario!, message));
    }
}
```

#### B. Scenario-Initiated Pause (via PauseForUser Step)
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
/// Used in PauseForUser steps. Supports localization.
/// </summary>
public string? PauseMessage { get; }

/// <summary>
/// Localization key for pause message.
/// Used in PauseForUser steps.
/// </summary>
public string? PauseMessageKey { get; }
```

**JSON Schema:**
```json
{
  "type": "pause_for_user",
  "pauseMessageKey": "scenarios.demo.step5.pauseMessage",
  "pauseMessage": "Take a moment to examine the context indicator in the top right. Click Resume when ready.",
  "annotation": "Pausing to let user explore the UI"
}
```

**Note:** Both `pauseMessageKey` (for localization) and `pauseMessage` (fallback) supported, consistent with other scenario content.

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
    ExecutePauseForUserStep(step);
    break;
```

Implementation:
```csharp
private void ExecutePauseForUserStep(ScenarioStep step)
{
    // The step itself pauses the scenario with the provided message
    // Pause happens AFTER this step completes (in the main execution loop)
    var message = step.PauseMessage ?? "Scenario paused. Click Resume to continue.";
    PauseScenario(message);
}
```

**Note:** The pause message is displayed to the UI, then the scenario pauses. The pause check happens in the main execution loop after the step completes.

### 6. Event System

New events on `IScenarioExecutor`:

```csharp
/// <summary>
/// Event fired when scenario is paused.
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
    public string? PauseMessage { get; }

    public ScenarioPausedEventArgs(
        ScenarioDefinition scenario,
        string? pauseMessage)
    {
        Scenario = scenario;
        PauseMessage = pauseMessage;
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
    background-color: rgba(255, 255, 255, 0.2);
    border-radius: 0.25rem;
    font-size: 0.9rem;
}

.pause-icon {
    font-size: 1.2rem;
}
```

**CSS fix for step number visibility:**
```css
/* Fix: Make step numbers fully visible against gradient background */
.indicator-info .text-muted {
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 1); /* Changed from 0.8 to 1 for full opacity */
    font-weight: 500; /* Add slight weight for better readability */
}
```

---

## Implementation Plan (TDD Approach)

Each phase follows the TDD cycle: RED → GREEN → REFACTOR. Each phase can be executed independently in a new session, assuming previous phases are complete.

### Phase 1: ScenarioExecutionState Enum (Domain)

**Goal:** Add state tracking for scenario execution lifecycle.

**Prerequisites:** None

**Files to Create/Modify:**
- Create: `TransparentAiAgentCore/Domain/Scenarios/ScenarioExecutionState.cs`
- Test: `TransparentAiAgentCore_Tests/Domain/Scenarios/ScenarioExecutionStateTests.cs`

**TDD Steps:**

1. **RED:** Write tests for enum values
   ```csharp
   [TestMethod]
   public void ScenarioExecutionState_HasRequiredValues()
   {
       var values = Enum.GetValues<ScenarioExecutionState>();
       CollectionAssert.Contains(values.ToArray(), ScenarioExecutionState.NotRunning);
       CollectionAssert.Contains(values.ToArray(), ScenarioExecutionState.Running);
       CollectionAssert.Contains(values.ToArray(), ScenarioExecutionState.Paused);
       CollectionAssert.Contains(values.ToArray(), ScenarioExecutionState.Completed);
       CollectionAssert.Contains(values.ToArray(), ScenarioExecutionState.Failed);
   }
   ```

2. **GREEN:** Create enum
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

3. **REFACTOR:** Add XML documentation

**Exit Criteria:** Tests pass, enum is documented

**Estimated Effort:** 15 minutes

---

### Phase 2: Event Args Classes

**Goal:** Create event argument classes for pause/resume events.

**Prerequisites:** Phase 1 complete

**Files to Modify:**
- Modify: `TransparentAiAgentCore/Application/Scenarios/IScenarioExecutor.cs` (add event args classes)
- Test: `TransparentAiAgentCore_Tests/Application/Scenarios/ScenarioPausedEventArgsTests.cs`

**TDD Steps:**

1. **RED:** Write tests for ScenarioPausedEventArgs
   ```csharp
   [TestMethod]
   public void ScenarioPausedEventArgs_Constructor_InitializesProperties()
   {
       var scenario = new ScenarioDefinition(/* ... */);
       var message = "Test pause message";

       var eventArgs = new ScenarioPausedEventArgs(scenario, message);

       Assert.AreSame(scenario, eventArgs.Scenario);
       Assert.AreEqual(message, eventArgs.PauseMessage);
   }

   [TestMethod]
   public void ScenarioPausedEventArgs_Constructor_NullMessage_Allowed()
   {
       var scenario = new ScenarioDefinition(/* ... */);

       var eventArgs = new ScenarioPausedEventArgs(scenario, null);

       Assert.IsNull(eventArgs.PauseMessage);
   }
   ```

2. **GREEN:** Implement event args class
3. **REFACTOR:** Add documentation

**Exit Criteria:** Tests pass, event args classes documented

**Estimated Effort:** 30 minutes

---

### Phase 3: Core Pause/Resume Methods (Application Layer)

**Goal:** Implement pause/resume logic in ScenarioExecutor.

**Prerequisites:** Phases 1-2 complete

**Files to Modify:**
- Modify: `TransparentAiAgentCore/Application/Scenarios/IScenarioExecutor.cs`
- Modify: `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`
- Test: `TransparentAiAgentCore_Tests/Application/Scenarios/ScenarioExecutorPauseResumeTests.cs`

**TDD Steps:**

1. **RED:** Write test for user-initiated pause
   ```csharp
   [TestMethod]
   public void PauseScenario_WhileRunning_ChangesStateToPaused()
   {
       // Arrange: Start a scenario
       var executor = CreateExecutor();
       var scenario = CreateSimpleScenario();
       var task = executor.ExecuteScenarioAsync(scenario);

       // Wait for scenario to start
       await Task.Delay(100);

       // Act: Pause
       executor.PauseScenario();

       // Assert
       Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
   }
   ```

2. **GREEN:** Implement PauseScenario() method
3. **RED:** Write test for resume
4. **GREEN:** Implement ResumeScenario() method
5. **RED:** Write test for pause event firing
6. **GREEN:** Implement event firing
7. **REFACTOR:** Extract common logic, improve thread safety

**Additional Test Cases:**
- Pause when not running → no-op
- Resume when not paused → no-op
- Pause fires event with correct message
- Resume fires event
- Stop while paused → cleanup correctly

**Exit Criteria:** All pause/resume tests pass, events fire correctly

**Estimated Effort:** 2-3 hours

---

### Phase 4: Pause Check in Execution Loop

**Goal:** Make execution loop pause-aware.

**Prerequisites:** Phase 3 complete

**Files to Modify:**
- Modify: `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`
- Test: Add to `ScenarioExecutorPauseResumeTests.cs`

**TDD Steps:**

1. **RED:** Write test for pause between steps
   ```csharp
   [TestMethod]
   public async Task ExecuteScenario_PausedBetweenSteps_WaitsForResume()
   {
       var executor = CreateExecutor();
       var scenario = CreateScenarioWithMultipleSteps();

       executor.StepExecuted += (s, e) =>
       {
           if (e.StepIndex == 1)
               executor.PauseScenario();
       };

       var task = executor.ExecuteScenarioAsync(scenario);

       await Task.Delay(500); // Let first two steps execute

       Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
       Assert.AreEqual(1, executor.CurrentStepIndex); // Stopped at step 1

       executor.ResumeScenario();
       await task;

       Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
   }
   ```

2. **GREEN:** Add pause check in execution loop
3. **REFACTOR:** Ensure clean error handling

**Exit Criteria:** Scenario can pause/resume mid-execution

**Estimated Effort:** 1 hour

---

### Phase 5: PauseForUser Step Type (Domain)

**Goal:** Add PauseForUser step type to domain model.

**Prerequisites:** Phase 1 complete (independent of 2-4 for domain model)

**Files to Modify:**
- Modify: `TransparentAiAgentCore/Domain/Scenarios/ScenarioStepType.cs`
- Modify: `TransparentAiAgentCore/Domain/Scenarios/ScenarioStep.cs`
- Test: `TransparentAiAgentCore_Tests/Domain/Scenarios/ScenarioStepTests.cs`

**TDD Steps:**

1. **RED:** Write test for PauseForUser step creation
   ```csharp
   [TestMethod]
   public void ScenarioStep_PauseForUser_WithMessage_CreatesSuccessfully()
   {
       var step = new ScenarioStep(
           ScenarioStepType.PauseForUser,
           pauseMessage: "Examine the UI"
       );

       Assert.AreEqual(ScenarioStepType.PauseForUser, step.Type);
       Assert.AreEqual("Examine the UI", step.PauseMessage);
   }

   [TestMethod]
   public void ScenarioStep_PauseForUser_WithoutMessage_UsesDefault()
   {
       var step = new ScenarioStep(ScenarioStepType.PauseForUser);

       Assert.IsNotNull(step.PauseMessage); // Should have default
   }
   ```

2. **GREEN:** Add PauseForUser to enum, add PauseMessage + PauseMessageKey properties
3. **REFACTOR:** Update validation logic

**Exit Criteria:** PauseForUser step can be created with message

**Estimated Effort:** 45 minutes

---

### Phase 6: PauseForUser Step Execution

**Goal:** Execute PauseForUser steps during scenario execution.

**Prerequisites:** Phases 3, 4, 5 complete

**Files to Modify:**
- Modify: `TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs`
- Test: Add to `ScenarioExecutorPauseResumeTests.cs`

**TDD Steps:**

1. **RED:** Write test for PauseForUser execution
   ```csharp
   [TestMethod]
   public async Task ExecuteScenario_PauseForUserStep_PausesWithMessage()
   {
       var executor = CreateExecutor();
       var scenario = new ScenarioDefinition(
           "test",
           "Test",
           null,
           new[]
           {
               new ScenarioStep(ScenarioStepType.PauseForUser, pauseMessage: "Check this out")
           }
       );

       string? receivedMessage = null;
       executor.ScenarioPaused += (s, e) => receivedMessage = e.PauseMessage;

       var task = executor.ExecuteScenarioAsync(scenario);
       await Task.Delay(100);

       Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
       Assert.AreEqual("Check this out", receivedMessage);
   }
   ```

2. **GREEN:** Implement ExecutePauseForUserStep method
3. **REFACTOR:** Ensure message localization support

**Exit Criteria:** PauseForUser step pauses scenario with message

**Estimated Effort:** 45 minutes

---

### Phase 7: UI Component - Pause/Resume Buttons

**Goal:** Add pause/resume buttons to ScenarioIndicator.

**Prerequisites:** Phases 1-6 complete (needs executor functionality)

**Files to Modify:**
- Modify: `TransparentAiAgentGui/Components/Scenarios/ScenarioIndicator.razor`
- Modify: `TransparentAiAgentGui/Components/Scenarios/ScenarioIndicator.razor.css`

**Changes:**
1. Add button container with conditional rendering
2. Show Pause button when State == Running
3. Show Resume button when State == Paused
4. Wire up click handlers to call PauseScenario()/ResumeScenario()
5. Subscribe to ScenarioPaused/ScenarioResumed events
6. Display pause message when paused
7. **Fix:** Update `.text-muted` CSS for step number visibility

**Testing:** Manual testing in browser
- Start scenario → Pause button visible
- Click Pause → Resume button appears, pause message shows
- Click Resume → Pause button reappears
- Verify step numbers are clearly visible

**Exit Criteria:** UI buttons work correctly, step numbers visible

**Estimated Effort:** 1-1.5 hours

---

### Phase 8: JSON Loader Updates

**Goal:** Support PauseForUser in JSON scenario files.

**Prerequisites:** Phase 5 complete

**Files to Modify:**
- Modify: `TransparentAiAgentCore/Infrastructure/Scenarios/JsonScenarioLoader.cs`
- Test: `TransparentAiAgentCore_Tests/Infrastructure/Scenarios/JsonScenarioLoaderTests.cs`

**TDD Steps:**

1. **RED:** Write test for loading PauseForUser from JSON
   ```csharp
   [TestMethod]
   public void LoadFromJson_PauseForUserStep_LoadsCorrectly()
   {
       var json = @"{
           ""id"": ""test"",
           ""name"": ""Test"",
           ""steps"": [
               {
                   ""type"": ""pause_for_user"",
                   ""pauseMessageKey"": ""test.pause"",
                   ""pauseMessage"": ""Examine this""
               }
           ]
       }";

       var scenario = JsonScenarioLoader.LoadFromJson(json);

       Assert.AreEqual(ScenarioStepType.PauseForUser, scenario.Steps[0].Type);
       Assert.AreEqual("Examine this", scenario.Steps[0].PauseMessage);
   }
   ```

2. **GREEN:** Update JSON deserialization mapping
3. **REFACTOR:** Add validation

**Exit Criteria:** PauseForUser steps load from JSON correctly

**Estimated Effort:** 45 minutes

---

### Phase 9: Example Scenario & Documentation

**Goal:** Create example scenario and update documentation.

**Prerequisites:** Phases 1-8 complete

**Files to Create/Modify:**
- Create: `TransparentAiAgentGui/data/scenarios/pause-demo.json`
- Modify: `docs/03-concepts/teaching-mode/scenario-schema.md`

**Tasks:**
1. Create pause-demo.json with PauseForUser steps
2. Test manually in browser
3. Update scenario-schema.md with PauseForUser documentation
4. Add examples and best practices

**Exit Criteria:** Example works, documentation complete

**Estimated Effort:** 1 hour

---

## Total Estimated Effort

**Development & Testing:** 9-12 hours
**Documentation:** 1 hour
**Total:** 10-13 hours (approximately 2-3 development sessions)

---

## Phase Dependencies

```
Phase 1 (State Enum)
    ├─> Phase 2 (Event Args)
    │       └─> Phase 3 (Pause/Resume Methods)
    │               └─> Phase 4 (Execution Loop)
    │                       └─> Phase 6 (Step Execution)
    │                               └─> Phase 7 (UI)
    │
    └─> Phase 5 (Step Type)
            ├─> Phase 6 (Step Execution)
            └─> Phase 8 (JSON Loader)

Phase 9 (Examples & Docs) depends on all previous phases
```

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

## Design Decisions (Confirmed)

The following design questions have been resolved:

### 1. Pause Button Visibility
**Decision:** ✅ Always visible during scenario execution

Pause button is always available when scenario is running, giving users maximum control.

---

### 2. Pause Message Localization
**Decision:** ✅ Support both localization keys and fallback text

Implementation includes both `pauseMessageKey` (for localization) and `pauseMessage` (fallback), consistent with other scenario content.

---

### 3. Pause State Indicator
**Decision:** ✅ Button state alone indicates pause status

- Running → Show "⏸️ Pause" button
- Paused → Show "▶️ Resume" button
- Pause message displayed when present
- No additional visual indicators needed

---

### 4. Resume Confirmation
**Decision:** ✅ Immediate resume, no confirmation

User clicks Resume button → scenario resumes immediately. Simple and predictable.

---

### 5. Step Execution During Pause
**Decision:** ✅ Pause happens AFTER step completes

PauseForUser step displays message, then scenario pauses. Cleaner state, step is fully executed before pause.

---

### 6. PauseForUser Availability
**Decision:** ✅ Available in all scenarios (basic and advanced)

PauseForUser is a simple step type that doesn't require advanced features, so it's available everywhere.

---

### 7. Pause Trigger Mechanism
**Decision:** ✅ Single pause mechanism, two triggers

No distinction between user-initiated and scenario-initiated pause. Both use the same `PauseScenario(message)` method. The only difference is whether a message is provided.

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
    /// <param name="message">Optional message explaining why scenario is paused.</param>
    void PauseScenario(string? message = null);

    /// <summary>
    /// Resumes a paused scenario.
    /// Can only be called when state is Paused.
    /// </summary>
    void ResumeScenario();

    /// <summary>
    /// Event fired when scenario is paused (user-initiated or step-initiated).
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
    public string? PauseMessage { get; }

    public ScenarioPausedEventArgs(
        ScenarioDefinition scenario,
        string? pauseMessage)
    {
        Scenario = scenario;
        PauseMessage = pauseMessage;
    }
}
```

**Note:** `PauseMessage` is null when user clicks Pause button, populated when PauseForUser step executes.

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
