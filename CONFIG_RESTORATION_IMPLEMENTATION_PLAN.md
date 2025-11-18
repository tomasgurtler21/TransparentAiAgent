# Configuration Restoration Bugfixes - Implementation Plan

**Created**: 2025-11-18
**Based On**: CONFIG_RESTORATION_ANALYSIS.md
**Methodology**: Lean TDD (Test-Driven Development)
**Branch**: `claude/config-restoration-bugfixes-015W6rVaW5CFfWkqStj76ivi`

---

## Overview

This plan implements fixes for critical configuration restoration bugs identified in the CONFIG_RESTORATION_ANALYSIS.md. The implementation follows Lean TDD principles: tests are created first to detect bugs, then fixes are implemented to make tests pass.

### Bugs Being Fixed

1. **Bug #1**: Base configuration is empty - messageLimit from appsettings.json never loaded
2. **Bug #2**: No event-driven architecture - ConversationManager unaware of overlay changes
3. **Bug #3**: Asymmetric truncation logic - messages truncate but never restore
4. **Bug #4**: No notification after overlay pop/push - changes are silent

### Success Criteria

- ✅ Configuration overlays trigger immediate truncation/restoration
- ✅ Scenario completion restores original context window size
- ✅ Truncated messages are restored when limit increases
- ✅ Manual overlay changes take effect immediately
- ✅ All tests pass (Red → Green cycle completed)

---

## Implementation Approach

### Lean TDD Discipline

Each step follows the **Red-Green-Refactor** cycle:

1. **RED**: Write failing test that compiles and runs but fails assertion
2. **GREEN**: Implement minimum code to make test pass
3. **REFACTOR**: Improve code quality while keeping tests green

**CRITICAL**: Always investigate unexpected results:
- If test passes when expected to fail → STOP and investigate
- If test fails for wrong reason → STOP and investigate
- Never continue with lazy assumptions

### Session Independence

Each step is designed to be completed in an independent session:
- Clear prerequisites and deliverables
- Self-contained scope
- Explicit verification criteria
- No assumptions about previous sessions

---

## Phase 1: Test Creation (Detect Bugs)

**Goal**: Create comprehensive tests that expose all four bugs. Tests should fail initially (RED phase), confirming bugs exist.

---

### Step 1: Create ConfigurationOverlayService Event Tests

**Session ID**: Step 1
**File**: `TransparentAiAgentCore_Tests/Infrastructure/Configuration/ConfigurationOverlayServiceTests.cs`
**Duration Estimate**: 30-45 minutes

#### Prerequisites
- [ ] Read CONFIG_RESTORATION_ANALYSIS.md (lines 80-128)
- [ ] Read .claude/skills/tdd/SKILL.md (Lean TDD principles)
- [ ] Understand ConfigurationOverlayService.cs implementation

#### Objective
Create tests for event-driven overlay change notifications. These tests will FAIL initially because the event system doesn't exist yet.

#### Test Cases to Implement

```csharp
[TestClass]
public class ConfigurationOverlayServiceTests
{
    // Test 1: PushOverlay should fire OverlayChanged event
    [TestMethod]
    public void PushOverlay_WithValidOverlay_FiresOverlayChangedEvent()
    {
        // Arrange
        var service = new ConfigurationOverlayService(new Dictionary<string, object>());
        bool eventFired = false;
        service.OverlayChanged += (sender, args) => eventFired = true;

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });

        // Assert
        Assert.IsTrue(eventFired, "OverlayChanged event should fire when overlay is pushed");
    }

    // Test 2: PopOverlay should fire OverlayChanged event
    [TestMethod]
    public void PopOverlay_WithExistingOverlay_FiresOverlayChangedEvent()
    {
        // Arrange
        var service = new ConfigurationOverlayService(new Dictionary<string, object>());
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });
        bool eventFired = false;
        service.OverlayChanged += (sender, args) => eventFired = true;

        // Act
        service.PopOverlay();

        // Assert
        Assert.IsTrue(eventFired, "OverlayChanged event should fire when overlay is popped");
    }

    // Test 3: Event args contain correct change type
    [TestMethod]
    public void PushOverlay_EventArgs_ContainsPushChangeType()
    {
        // Arrange
        var service = new ConfigurationOverlayService(new Dictionary<string, object>());
        ConfigurationChangedEventArgs? capturedArgs = null;
        service.OverlayChanged += (sender, args) => capturedArgs = args;

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });

        // Assert
        Assert.IsNotNull(capturedArgs, "Event args should not be null");
        Assert.AreEqual(ChangeType.Push, capturedArgs.Type, "Change type should be Push");
    }

    // Test 4: Multiple subscribers receive event
    [TestMethod]
    public void PushOverlay_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var service = new ConfigurationOverlayService(new Dictionary<string, object>());
        int callCount = 0;
        service.OverlayChanged += (sender, args) => callCount++;
        service.OverlayChanged += (sender, args) => callCount++;

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });

        // Assert
        Assert.AreEqual(2, callCount, "Both subscribers should receive event");
    }

    // Test 5: GetValue works correctly with base config
    [TestMethod]
    public void GetValue_NoOverlay_ReturnsBaseConfigValue()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { { "messageLimit", 200 } };
        var service = new ConfigurationOverlayService(baseConfig);

        // Act
        var value = service.GetValue<int>("messageLimit", 100);

        // Assert
        Assert.AreEqual(200, value, "Should return value from base config");
    }

    // Test 6: GetValue prioritizes overlay over base
    [TestMethod]
    public void GetValue_WithOverlay_ReturnsOverlayValue()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { { "messageLimit", 200 } };
        var service = new ConfigurationOverlayService(baseConfig);
        service.PushOverlay(new Dictionary<string, object> { { "messageLimit", 8 } });

        // Act
        var value = service.GetValue<int>("messageLimit", 100);

        // Assert
        Assert.AreEqual(8, value, "Should return value from overlay, not base config");
    }
}
```

#### Expected Results (RED Phase)
- ❌ Tests should FAIL with "Member 'OverlayChanged' not found" (compilation error initially)
- ❌ After adding minimal event stub, tests should RUN and FAIL with "Event not fired"
- ❌ Test 5 should FAIL because base config is currently ignored
- ✅ This confirms bugs #1 and #2 exist

#### Deliverables
- [ ] ConfigurationOverlayServiceTests.cs created with all 6 tests
- [ ] Tests compile (minimal stubs added to production code)
- [ ] Tests run and FAIL as expected
- [ ] Documented failure reasons in test run output

#### Verification
```bash
dotnet test --filter "FullyQualifiedName~ConfigurationOverlayServiceTests"
```
All tests should FAIL (RED phase confirmed).

---

### Step 2: Create ConversationManager Restoration Tests

**Session ID**: Step 2
**File**: `TransparentAiAgentCore_Tests/Application/Conversation/ConversationManagerTests.cs`
**Duration Estimate**: 45-60 minutes

#### Prerequisites
- [ ] Read CONFIG_RESTORATION_ANALYSIS.md (lines 130-214)
- [ ] Read .claude/skills/tdd/SKILL.md (Lean TDD principles)
- [ ] Understand ConversationManager.cs TruncateIfNeeded() method

#### Objective
Create tests for bidirectional truncation logic (both truncate AND restore). These tests will FAIL initially because restoration logic doesn't exist.

#### Test Cases to Implement

```csharp
[TestClass]
public class ConversationManagerRestorationTests
{
    // Test 1: Messages are restored when limit increases
    [TestMethod]
    public void TruncateIfNeeded_LimitIncrease_RestoresTruncatedMessages()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var mockOverlay = new Mock<IConfigurationOverlay>();
        var manager = new ConversationManager(10, mockTransparency.Object, mockOverlay.Object);

        // Add 20 messages (first 10 will be truncated when limit is 10)
        for (int i = 0; i < 20; i++)
        {
            manager.AddMessage(new UserMessage($"Message {i}"));
        }

        // Verify initial truncation
        Assert.AreEqual(10, manager.InContextMessageCount, "Should have 10 messages in context");

        // Act: Increase limit to 15
        mockOverlay.Setup(o => o.GetValue<int>("messageLimit", 10)).Returns(15);
        manager.UpdateContextWindowSize(15); // Manually trigger truncation check

        // Assert: 5 messages should be restored
        Assert.AreEqual(15, manager.InContextMessageCount, "Should restore 5 messages to reach limit of 15");
    }

    // Test 2: Restoration respects FIFO order (oldest truncated messages restored first)
    [TestMethod]
    public void TruncateIfNeeded_Restoration_RestoresOldestFirst()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(5, mockTransparency.Object, null);

        // Add 10 messages
        var messages = new List<UserMessage>();
        for (int i = 0; i < 10; i++)
        {
            var msg = new UserMessage($"Message {i}");
            messages.Add(msg);
            manager.AddMessage(msg);
        }

        // First 5 should be truncated
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, messages[0].ContextStatus);
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, messages[1].ContextStatus);
        Assert.AreEqual(MessageContextStatus.InContext, messages[5].ContextStatus);

        // Act: Increase limit to 7
        manager.UpdateContextWindowSize(7);

        // Assert: Messages 0 and 1 should be restored (oldest truncated first)
        Assert.AreEqual(MessageContextStatus.InContext, messages[0].ContextStatus, "Oldest message should be restored first");
        Assert.AreEqual(MessageContextStatus.InContext, messages[1].ContextStatus, "Second oldest should be restored next");
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, messages[2].ContextStatus, "This should still be truncated");
    }

    // Test 3: Restoration fires ContextStatusChanged events
    [TestMethod]
    public void TruncateIfNeeded_Restoration_FiresContextStatusChangedEvents()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(5, mockTransparency.Object, null);

        // Add 10 messages (5 will be truncated)
        for (int i = 0; i < 10; i++)
        {
            manager.AddMessage(new UserMessage($"Message {i}"));
        }

        int eventCount = 0;
        manager.ContextStatusChanged += (sender, args) => eventCount++;

        // Act: Increase limit to 8 (should restore 3 messages)
        manager.UpdateContextWindowSize(8);

        // Assert: 3 ContextStatusChanged events should fire
        Assert.AreEqual(3, eventCount, "Should fire 3 events for 3 restored messages");
    }

    // Test 4: Restoration works with overlay changes
    [TestMethod]
    public void TruncateIfNeeded_OverlayPop_RestoresMessages()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var mockOverlay = new Mock<IConfigurationOverlay>();
        mockOverlay.Setup(o => o.GetValue<int>("messageLimit", 200)).Returns(8); // Initially 8

        var manager = new ConversationManager(200, mockTransparency.Object, mockOverlay.Object);

        // Add 20 messages with overlay limit of 8
        for (int i = 0; i < 20; i++)
        {
            manager.AddMessage(new UserMessage($"Message {i}"));
        }

        Assert.AreEqual(8, manager.InContextMessageCount, "Overlay limit should be active");

        // Act: Simulate overlay pop (limit returns to 200)
        mockOverlay.Setup(o => o.GetValue<int>("messageLimit", 200)).Returns(200);
        mockOverlay.Raise(o => o.OverlayChanged += null, new ConfigurationChangedEventArgs(ChangeType.Pop, null));

        // Assert: All 20 messages should be restored
        Assert.AreEqual(20, manager.InContextMessageCount, "All messages should be restored after overlay pop");
    }

    // Test 5: No restoration needed when already at limit
    [TestMethod]
    public void TruncateIfNeeded_AlreadyAtLimit_NoChanges()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(10, mockTransparency.Object, null);

        // Add exactly 10 messages
        for (int i = 0; i < 10; i++)
        {
            manager.AddMessage(new UserMessage($"Message {i}"));
        }

        int eventCount = 0;
        manager.ContextStatusChanged += (sender, args) => eventCount++;

        // Act: Update to same limit
        manager.UpdateContextWindowSize(10);

        // Assert: No events fired (no changes needed)
        Assert.AreEqual(0, eventCount, "No events should fire when already at limit");
    }

    // Test 6: Partial restoration when not enough truncated messages
    [TestMethod]
    public void TruncateIfNeeded_LimitExceedsTruncated_RestoresAllAvailable()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(5, mockTransparency.Object, null);

        // Add 7 messages (2 will be truncated)
        for (int i = 0; i < 7; i++)
        {
            manager.AddMessage(new UserMessage($"Message {i}"));
        }

        Assert.AreEqual(5, manager.InContextMessageCount);

        // Act: Increase limit to 100 (but only 7 messages total)
        manager.UpdateContextWindowSize(100);

        // Assert: All 7 messages should be in context
        Assert.AreEqual(7, manager.InContextMessageCount, "Should restore all available messages");
    }
}
```

#### Expected Results (RED Phase)
- ❌ Tests should FAIL because restoration logic is missing
- ❌ Test 1: "Expected 15 but was 10" (no restoration happens)
- ❌ Test 2: "Expected InContext but was TruncatedFromContext"
- ❌ Test 3: "Expected 3 but was 0" (no events fired)
- ❌ Test 4: "Expected 20 but was 8" (overlay change ignored)
- ✅ This confirms bugs #3 and #4 exist

#### Deliverables
- [ ] ConversationManagerRestorationTests.cs created with all 6 tests
- [ ] Tests compile and run
- [ ] Tests FAIL as expected (RED phase)
- [ ] Documented failure reasons

#### Verification
```bash
dotnet test --filter "FullyQualifiedName~ConversationManagerRestorationTests"
```
All tests should FAIL (RED phase confirmed).

---

### Step 3: Create Base Configuration Population Tests

**Session ID**: Step 3
**File**: `TransparentAiAgentCore_Tests/Infrastructure/Configuration/ConfigurationOverlayServiceIntegrationTests.cs`
**Duration Estimate**: 20-30 minutes

#### Prerequisites
- [ ] Read CONFIG_RESTORATION_ANALYSIS.md (lines 397-418)
- [ ] Understand Program.cs ConfigurationOverlayService registration

#### Objective
Create integration tests verifying base configuration is populated from AppConfiguration. These tests will FAIL initially because base config is currently empty.

#### Test Cases to Implement

```csharp
[TestClass]
public class ConfigurationOverlayServiceIntegrationTests
{
    // Test 1: Base config should contain messageLimit
    [TestMethod]
    public void BaseConfiguration_ShouldContainMessageLimit()
    {
        // Arrange
        var appConfig = new AppConfiguration
        {
            Agent = new AgentConfiguration { ContextWindowSize = 200 }
        };

        var baseConfig = new Dictionary<string, object>
        {
            { "messageLimit", appConfig.Agent.ContextWindowSize }
        };

        var service = new ConfigurationOverlayService(baseConfig);

        // Act
        var value = service.GetValue<int>("messageLimit", -1);

        // Assert
        Assert.AreEqual(200, value, "Base config should contain messageLimit from AppConfiguration");
        Assert.AreNotEqual(-1, value, "Should not fall back to default parameter");
    }

    // Test 2: Empty base config falls back to default (current buggy behavior)
    [TestMethod]
    public void EmptyBaseConfiguration_FallsBackToDefault()
    {
        // Arrange
        var service = new ConfigurationOverlayService(new Dictionary<string, object>());

        // Act
        var value = service.GetValue<int>("messageLimit", 100);

        // Assert
        Assert.AreEqual(100, value, "Empty base config should fall back to default parameter");
    }

    // Test 3: Base config should be overridden by overlay
    [TestMethod]
    public void BaseConfiguration_OverriddenByOverlay()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { { "messageLimit", 200 } };
        var service = new ConfigurationOverlayService(baseConfig);

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "messageLimit", 8 } });
        var value = service.GetValue<int>("messageLimit", -1);

        // Assert
        Assert.AreEqual(8, value, "Overlay should override base config");
    }

    // Test 4: After overlay pop, should return to base config value
    [TestMethod]
    public void PopOverlay_ReturnsToBaseConfiguration()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { { "messageLimit", 200 } };
        var service = new ConfigurationOverlayService(baseConfig);
        service.PushOverlay(new Dictionary<string, object> { { "messageLimit", 8 } });

        // Act
        service.PopOverlay();
        var value = service.GetValue<int>("messageLimit", -1);

        // Assert
        Assert.AreEqual(200, value, "Should return to base config value after overlay pop");
    }
}
```

#### Expected Results (RED Phase)
- ❌ Test 1: FAILS if run with actual DI container (base config is empty)
- ✅ Tests 2-4: These should PASS even with current implementation
- ✅ This documents expected vs actual behavior

#### Deliverables
- [ ] ConfigurationOverlayServiceIntegrationTests.cs created with all 4 tests
- [ ] Tests compile and run
- [ ] Test 1 FAILS (confirms bug #1)
- [ ] Tests 2-4 PASS (confirms workaround currently works)

#### Verification
```bash
dotnet test --filter "FullyQualifiedName~ConfigurationOverlayServiceIntegrationTests"
```
Test 1 should FAIL, others should PASS.

---

### Step 4: Verify All Tests Fail (RED Phase Confirmation)

**Session ID**: Step 4
**Duration Estimate**: 15-20 minutes

#### Prerequisites
- [ ] Steps 1-3 completed
- [ ] All test files created and compiling

#### Objective
Run complete test suite to confirm all bug-detecting tests fail as expected. This is final RED phase verification before implementing fixes.

#### Actions
1. Run all new tests together
2. Document each failure
3. Verify failures match expected bugs
4. Confirm no unexpected passes

#### Verification Commands

```bash
# Run all new tests
dotnet test --filter "FullyQualifiedName~ConfigurationOverlayServiceTests"
dotnet test --filter "FullyQualifiedName~ConversationManagerRestorationTests"
dotnet test --filter "FullyQualifiedName~ConfigurationOverlayServiceIntegrationTests"

# Run entire test suite to ensure no regressions
dotnet test TransparentAiAgentCore_Tests
```

#### Expected Results
- ❌ ConfigurationOverlayServiceTests: 4-5 failures (event system missing)
- ❌ ConversationManagerRestorationTests: 5-6 failures (restoration missing)
- ❌ ConfigurationOverlayServiceIntegrationTests: 1 failure (base config empty)
- ✅ Existing tests: All should still PASS (no regressions)

#### Deliverables
- [ ] Test run report showing all expected failures
- [ ] Confirmation that failures match bug descriptions
- [ ] No unexpected test failures
- [ ] Documentation of failure messages

#### Success Criteria
- All new tests FAIL for the RIGHT reasons
- No compilation errors
- Existing tests still pass
- Ready to proceed to GREEN phase

---

## Phase 2: Implementation (Fix Bugs)

**Goal**: Implement minimum code to make all tests pass (GREEN phase). Each step targets specific failing tests.

---

### Step 5: Implement Event System in ConfigurationOverlayService

**Session ID**: Step 5
**Files**:
- `TransparentAiAgentCore/Domain/Configuration/IConfigurationOverlay.cs`
- `TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationOverlayService.cs`
- `TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationChangedEventArgs.cs` (new)

**Duration Estimate**: 30-45 minutes

#### Prerequisites
- [ ] Step 1 completed (tests exist and fail)
- [ ] Read CONFIG_RESTORATION_ANALYSIS.md (lines 505-546)

#### Objective
Implement event-driven overlay change notifications to make ConfigurationOverlayServiceTests pass.

#### Implementation Tasks

**Task 5.1**: Create ConfigurationChangedEventArgs class

```csharp
// File: TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationChangedEventArgs.cs
namespace TransparentAiAgent.Core.Infrastructure.Configuration;

public enum ChangeType
{
    Push,
    Pop,
    Clear
}

public class ConfigurationChangedEventArgs : EventArgs
{
    public ChangeType Type { get; }
    public IReadOnlyDictionary<string, object>? ChangedValues { get; }

    public ConfigurationChangedEventArgs(ChangeType type, IReadOnlyDictionary<string, object>? changedValues)
    {
        Type = type;
        ChangedValues = changedValues;
    }
}
```

**Task 5.2**: Add event to IConfigurationOverlay interface

```csharp
// File: TransparentAiAgentCore/Domain/Configuration/IConfigurationOverlay.cs
public interface IConfigurationOverlay
{
    event EventHandler<ConfigurationChangedEventArgs>? OverlayChanged;

    // ... existing methods ...
}
```

**Task 5.3**: Implement event firing in ConfigurationOverlayService

```csharp
// File: TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationOverlayService.cs

public event EventHandler<ConfigurationChangedEventArgs>? OverlayChanged;

public void PushOverlay(IReadOnlyDictionary<string, object> overlayValues)
{
    lock (_lock)
    {
        _overlayStack.Push(new Dictionary<string, object>(overlayValues));

        // Fire event AFTER pushing
        OverlayChanged?.Invoke(this, new ConfigurationChangedEventArgs(
            ChangeType.Push,
            overlayValues));
    }
}

public void PopOverlay()
{
    lock (_lock)
    {
        if (_overlayStack.Count == 0)
            throw new InvalidOperationException("No configuration overlays to pop.");

        var popped = _overlayStack.Pop();

        // Fire event AFTER popping
        OverlayChanged?.Invoke(this, new ConfigurationChangedEventArgs(
            ChangeType.Pop,
            popped));
    }
}

public void ClearOverlays()
{
    lock (_lock)
    {
        _overlayStack.Clear();

        // Fire event AFTER clearing
        OverlayChanged?.Invoke(this, new ConfigurationChangedEventArgs(
            ChangeType.Clear,
            null));
    }
}
```

#### Verification
```bash
dotnet test --filter "FullyQualifiedName~ConfigurationOverlayServiceTests"
```

#### Expected Results (GREEN Phase)
- ✅ All ConfigurationOverlayServiceTests should now PASS
- ✅ Events fire correctly
- ✅ Multiple subscribers work
- ✅ Event args contain correct data

#### Deliverables
- [ ] ConfigurationChangedEventArgs.cs created
- [ ] IConfigurationOverlay.cs updated with event
- [ ] ConfigurationOverlayService.cs fires events
- [ ] All tests in Step 1 now PASS

---

### Step 6: Implement Bidirectional Truncation Logic

**Session ID**: Step 6
**Files**:
- `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs`

**Duration Estimate**: 45-60 minutes

#### Prerequisites
- [ ] Step 2 completed (tests exist and fail)
- [ ] Read CONFIG_RESTORATION_ANALYSIS.md (lines 574-646)

#### Objective
Implement restoration logic in TruncateIfNeeded() to make ConversationManagerRestorationTests pass.

#### Implementation Tasks

**Task 6.1**: Update TruncateIfNeeded() with bidirectional logic

```csharp
// File: TransparentAiAgentCore/Application/Conversation/ConversationManager.cs

private void TruncateIfNeeded()
{
    // Get effective context window size from overlay or use default
    var effectiveWindowSize = ContextWindowSize;
    if (_configurationOverlay != null)
    {
        effectiveWindowSize = _configurationOverlay.GetValue("messageLimit", ContextWindowSize);
        if (effectiveWindowSize != ContextWindowSize)
        {
            LogEvent("EffectiveContextWindowSize",
                $"Using overlay messageLimit: {effectiveWindowSize} (base: {ContextWindowSize})");
        }
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
            .OrderBy(m => m.Role == MessageRole.System ? 1 : 0) // System messages last
            .ThenBy(m => m.Timestamp) // Oldest first
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
            .OrderBy(m => m.Timestamp) // Restore oldest first (FIFO)
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

**Task 6.2**: Subscribe to ConfigurationOverlay events in constructor

```csharp
// File: TransparentAiAgentCore/Application/Conversation/ConversationManager.cs

public ConversationManager(
    int contextWindowSize,
    ITransparencyService transparencyService,
    IConfigurationOverlay? configurationOverlay = null)
{
    ContextWindowSize = contextWindowSize;
    _transparencyService = transparencyService;
    _configurationOverlay = configurationOverlay;

    LogEvent("ConversationManagerInitialized",
        $"ContextWindowSize: {contextWindowSize}, OverlayAvailable: {configurationOverlay != null}");

    // Subscribe to overlay changes
    if (_configurationOverlay != null)
    {
        _configurationOverlay.OverlayChanged += OnConfigurationOverlayChanged;
    }
}

private void OnConfigurationOverlayChanged(object? sender, ConfigurationChangedEventArgs e)
{
    lock (_lock)
    {
        LogEvent("ConfigurationOverlayChanged",
            $"Change type: {e.Type}, Re-evaluating truncation");

        // Re-evaluate truncation with new overlay values
        TruncateIfNeeded();
    }
}
```

**Task 6.3**: Implement IDisposable for cleanup

```csharp
// File: TransparentAiAgentCore/Application/Conversation/ConversationManager.cs

public void Dispose()
{
    if (_configurationOverlay != null)
    {
        _configurationOverlay.OverlayChanged -= OnConfigurationOverlayChanged;
    }
}
```

#### Verification
```bash
dotnet test --filter "FullyQualifiedName~ConversationManagerRestorationTests"
```

#### Expected Results (GREEN Phase)
- ✅ All ConversationManagerRestorationTests should now PASS
- ✅ Messages are restored when limit increases
- ✅ FIFO order is respected
- ✅ Events fire correctly
- ✅ Overlay changes trigger immediate truncation/restoration

#### Deliverables
- [ ] ConversationManager.cs updated with restoration logic
- [ ] Event subscription implemented
- [ ] IDisposable implemented
- [ ] All tests in Step 2 now PASS

---

### Step 7: Populate Base Configuration from AppConfiguration

**Session ID**: Step 7
**Files**:
- `TransparentAiAgentGui/Program.cs`

**Duration Estimate**: 20-30 minutes

#### Prerequisites
- [ ] Step 3 completed (tests exist)
- [ ] Read CONFIG_RESTORATION_ANALYSIS.md (lines 647-678)

#### Objective
Load base configuration from AppConfiguration during DI registration to make ConfigurationOverlayServiceIntegrationTests pass.

#### Implementation Tasks

**Task 7.1**: Update ConfigurationOverlayService registration

```csharp
// File: TransparentAiAgentGui/Program.cs (around line 450)

builder.Services.AddScoped<IConfigurationOverlay>(sp =>
{
    var appConfig = sp.GetRequiredService<AppConfiguration>();

    // Populate base configuration with values from appsettings.json
    var baseConfig = new Dictionary<string, object>
    {
        { "messageLimit", appConfig.Agent.ContextWindowSize },
        { "systemPrompt", appConfig.Agent.SystemPrompt ?? string.Empty },
        { "enableTools", appConfig.Agent.EnableTools },
        { "toolExecutionMode", appConfig.Agent.ToolExecutionMode.ToString() },
        // Future-proof: Add other config values as needed
    };

    return new ConfigurationOverlayService(baseConfig);
});
```

#### Verification
```bash
dotnet test --filter "FullyQualifiedName~ConfigurationOverlayServiceIntegrationTests"
```

#### Expected Results (GREEN Phase)
- ✅ Test 1 should now PASS (base config contains messageLimit)
- ✅ Tests 2-4 should still PASS
- ✅ GetValue() no longer relies solely on fallback parameters

#### Deliverables
- [ ] Program.cs updated with base config population
- [ ] All tests in Step 3 now PASS
- [ ] Base config includes messageLimit, systemPrompt, enableTools, toolExecutionMode

---

### Step 8: Integration Verification

**Session ID**: Step 8
**Duration Estimate**: 30-45 minutes

#### Prerequisites
- [ ] Steps 5-7 completed
- [ ] All unit tests passing

#### Objective
Verify all components work together correctly. Run full test suite and manual scenario testing.

#### Verification Tasks

**Task 8.1**: Run complete test suite

```bash
# Run all tests
dotnet test TransparentAiAgentCore_Tests

# Should see:
# ✅ ConfigurationOverlayServiceTests (all pass)
# ✅ ConversationManagerRestorationTests (all pass)
# ✅ ConfigurationOverlayServiceIntegrationTests (all pass)
# ✅ Existing tests (no regressions)
```

**Task 8.2**: Manual scenario testing

1. Start application
2. Create conversation with messageLimit: 200
3. Add 30 messages
4. Run "Context Limits (Advanced)" scenario
5. Verify:
   - ✅ Messages truncated during scenario (8 in context)
   - ✅ Messages restored after scenario (30 in context)
   - ✅ UI shows correct status
   - ✅ Transparency logs show restoration events

**Task 8.3**: Manual overlay testing

```csharp
// Test in development console or Blazor component
var overlay = serviceProvider.GetRequiredService<IConfigurationOverlay>();
var manager = serviceProvider.GetRequiredService<IConversationManager>();

// Add messages
for (int i = 0; i < 50; i++)
{
    manager.AddMessage(new UserMessage($"Message {i}"));
}
Console.WriteLine($"Initial: {manager.InContextMessageCount}"); // Should be 50

// Push overlay
overlay.PushOverlay(new Dictionary<string, object> { { "messageLimit", 10 } });
Console.WriteLine($"After push: {manager.InContextMessageCount}"); // Should be 10 (immediate)

// Pop overlay
overlay.PopOverlay();
Console.WriteLine($"After pop: {manager.InContextMessageCount}"); // Should be 50 (restored)
```

#### Expected Results
- ✅ All tests pass
- ✅ Scenario works correctly
- ✅ Manual overlay changes work immediately
- ✅ No regressions in existing functionality

#### Deliverables
- [ ] Test run report (all green)
- [ ] Scenario test results
- [ ] Manual overlay test results
- [ ] Documentation of any unexpected behavior

---

## Phase 3: Refactoring & Documentation

**Goal**: Improve code quality while keeping tests green. Update documentation to reflect fixes.

---

### Step 9: Code Refactoring

**Session ID**: Step 9
**Duration Estimate**: 30-45 minutes

#### Prerequisites
- [ ] All tests passing
- [ ] Step 8 completed

#### Objective
Improve code quality, add comments, ensure consistency. Keep tests green throughout.

#### Refactoring Tasks

**Task 9.1**: Add XML documentation comments

```csharp
// Add to all public methods in:
// - IConfigurationOverlay.cs
// - ConfigurationOverlayService.cs
// - ConversationManager.cs

/// <summary>
/// Fired when configuration overlay changes (push, pop, or clear).
/// Subscribers should re-evaluate their configuration-dependent behavior.
/// </summary>
public event EventHandler<ConfigurationChangedEventArgs>? OverlayChanged;

/// <summary>
/// Re-evaluates context window truncation based on current effective limit.
/// Truncates messages when count exceeds limit, restores when count is below limit.
/// </summary>
private void TruncateIfNeeded()
```

**Task 9.2**: Extract magic strings to constants

```csharp
// In ConversationManager.cs
private const string ConfigKey_MessageLimit = "messageLimit";

// Usage
effectiveWindowSize = _configurationOverlay.GetValue(ConfigKey_MessageLimit, ContextWindowSize);
```

**Task 9.3**: Improve logging messages

```csharp
LogEvent("ContextRestored",
    $"Restored {truncatedMessages.Count} message(s). " +
    $"Current: {InContextMessageCount}/{effectiveWindowSize} in context");
```

#### Verification
```bash
# Run after each refactoring
dotnet test TransparentAiAgentCore_Tests
```

All tests should remain GREEN.

#### Deliverables
- [ ] XML documentation added
- [ ] Constants extracted
- [ ] Logging improved
- [ ] All tests still passing

---

### Step 10: Update Documentation

**Session ID**: Step 10
**Duration Estimate**: 30-45 minutes

#### Prerequisites
- [ ] All previous steps completed
- [ ] All tests passing

#### Objective
Update documentation to reflect bug fixes and new behavior.

#### Documentation Updates

**Task 10.1**: Update CONFIG_RESTORATION_ANALYSIS.md

Add section at the end:

```markdown
---

## Resolution (2025-11-18)

All issues identified in this analysis have been fixed:

✅ **Issue #1: Event-Driven Architecture** - Implemented in Step 5
✅ **Issue #2: Bidirectional Truncation** - Implemented in Step 6
✅ **Issue #3: Base Configuration** - Implemented in Step 7
✅ **Issue #4: Immediate Feedback** - Addressed by Issues #1 & #2

### Verification

All fixes verified through comprehensive test suite:
- ConfigurationOverlayServiceTests (6 tests)
- ConversationManagerRestorationTests (6 tests)
- ConfigurationOverlayServiceIntegrationTests (4 tests)

### Implementation Details

See CONFIG_RESTORATION_IMPLEMENTATION_PLAN.md for complete implementation details.
```

**Task 10.2**: Update scenario documentation

```markdown
// File: docs/03-concepts/teaching-mode/reference-scenarios/context-limits-advanced.md

Add section:

## Configuration Restoration

This scenario temporarily reduces the message limit to demonstrate truncation.
The limit is automatically restored when the scenario completes.

**Technical Details:**
- Initial limit: 200 (from appsettings.json)
- Scenario limit: 8 (applied via config overlay)
- Restoration: Automatic on scenario completion
- Behavior: Messages truncated during scenario are restored afterward
```

**Task 10.3**: Create migration notes

```markdown
// File: docs/09-archive/implementation-notes/config-restoration-bugfixes.md

# Configuration Restoration Bugfixes

**Date**: 2025-11-18
**Branch**: claude/config-restoration-bugfixes-015W6rVaW5CFfWkqStj76ivi

## Summary

Fixed critical bugs in configuration overlay restoration that prevented
context window limits from restoring correctly after scenarios.

## Changes

1. **Event-Driven Overlays**: ConfigurationOverlayService now fires events
2. **Bidirectional Truncation**: Messages are restored when limit increases
3. **Base Configuration**: Populated from AppConfiguration at startup
4. **Immediate Effect**: Overlay changes take effect immediately

## Migration Guide

No breaking changes. Existing code continues to work.

**New Behavior:**
- Config overlay changes now trigger immediate truncation/restoration
- Truncated messages are automatically restored when limit increases
- ContextStatusChanged events fire for both truncation and restoration

## Testing

16 new tests added covering all scenarios:
- ConfigurationOverlayServiceTests: Event system
- ConversationManagerRestorationTests: Restoration logic
- ConfigurationOverlayServiceIntegrationTests: Base config integration
```

#### Deliverables
- [ ] CONFIG_RESTORATION_ANALYSIS.md updated
- [ ] Scenario documentation updated
- [ ] Migration notes created
- [ ] All documentation reviewed for accuracy

---

## Completion Checklist

### Phase 1: Test Creation ✅
- [ ] Step 1: ConfigurationOverlayServiceTests created (RED)
- [ ] Step 2: ConversationManagerRestorationTests created (RED)
- [ ] Step 3: ConfigurationOverlayServiceIntegrationTests created (RED)
- [ ] Step 4: All tests confirmed failing (RED phase verified)

### Phase 2: Implementation ✅
- [ ] Step 5: Event system implemented (GREEN)
- [ ] Step 6: Bidirectional truncation implemented (GREEN)
- [ ] Step 7: Base configuration populated (GREEN)
- [ ] Step 8: Integration verified (all tests GREEN)

### Phase 3: Refactoring ✅
- [ ] Step 9: Code refactored (tests remain GREEN)
- [ ] Step 10: Documentation updated

### Final Verification ✅
- [ ] All tests passing (16+ new tests)
- [ ] No regressions in existing tests
- [ ] Scenario testing successful
- [ ] Manual overlay testing successful
- [ ] Documentation complete and accurate

---

## Success Metrics

### Quantitative
- ✅ 16+ new tests added
- ✅ 100% test pass rate
- ✅ 0 regressions
- ✅ 4 bugs fixed

### Qualitative
- ✅ Configuration overlays work immediately
- ✅ Scenarios restore context correctly
- ✅ User expectations met
- ✅ Code is maintainable and well-documented

---

## Risk Mitigation

### Potential Issues

**Issue**: Tests pass but scenario still doesn't work
- **Mitigation**: Step 8 includes manual scenario testing
- **Action**: If fails, investigate integration points

**Issue**: Performance degradation with large message counts
- **Mitigation**: Restoration uses LINQ efficiently
- **Action**: Monitor performance in Step 8

**Issue**: Thread safety concerns with events
- **Mitigation**: ConfigurationOverlayService uses locks
- **Action**: Review threading in ConversationManager

**Issue**: Breaking changes to existing code
- **Mitigation**: Maintain backward compatibility
- **Action**: Run full test suite at each step

---

## Notes for Implementation Sessions

### Session Guidelines

1. **Read Prerequisites**: Always read listed prerequisites before starting
2. **Follow TDD Discipline**: Strict Red-Green-Refactor cycle
3. **Investigate Failures**: NEVER ignore unexpected results
4. **Run Tests Frequently**: After each meaningful change
5. **Document Issues**: Note any deviations from plan

### Common Pitfalls

- ❌ Skipping RED phase verification
- ❌ Implementing without failing test
- ❌ Not running tests after refactoring
- ❌ Ignoring existing test failures
- ❌ Batch committing multiple steps

### Session Independence

Each step is designed to be self-contained:
- Clear scope and deliverables
- Explicit verification criteria
- No hidden dependencies
- Rollback-safe (tests remain green)

---

## Appendix: File Modification Summary

### Core Domain
- `TransparentAiAgentCore/Domain/Configuration/IConfigurationOverlay.cs` - Add event

### Infrastructure
- `TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationOverlayService.cs` - Fire events
- `TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationChangedEventArgs.cs` - NEW

### Application
- `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs` - Restoration logic

### UI/DI
- `TransparentAiAgentGui/Program.cs` - Base config population

### Tests (NEW FILES)
- `TransparentAiAgentCore_Tests/Infrastructure/Configuration/ConfigurationOverlayServiceTests.cs`
- `TransparentAiAgentCore_Tests/Application/Conversation/ConversationManagerRestorationTests.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/Configuration/ConfigurationOverlayServiceIntegrationTests.cs`

### Documentation
- `CONFIG_RESTORATION_ANALYSIS.md` - Add resolution section
- `docs/03-concepts/teaching-mode/reference-scenarios/context-limits-advanced.md` - Update
- `docs/09-archive/implementation-notes/config-restoration-bugfixes.md` - NEW

---

**End of Implementation Plan**

This plan provides comprehensive, step-by-step guidance for fixing all configuration restoration bugs using Lean TDD principles. Each step is independent, verifiable, and follows strict Red-Green-Refactor discipline.
