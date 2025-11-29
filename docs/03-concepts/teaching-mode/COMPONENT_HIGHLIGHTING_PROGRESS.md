# Component Highlighting Implementation Progress (TDD-Based)

**Project:** UI Component Highlighting Architecture for Teaching Mode
**Plan Document:** `C:\Users\tgurt\.claude\plans\frolicking-crafting-marshmallow.md`
**Started:** 2025-11-29
**Status:** 🚧 In Progress
**Methodology:** Lean TDD (Red → Green → Refactor)

---

## Quick Status

| Session | Goal | Status | Progress | Completion Date |
|---------|------|--------|----------|----------------|
| **Session 1** | HighlightState Domain Model | 🚧 Partial | Implementation done, tests pending | - |
| **Session 2** | Component Registry | ⏸️ Not Started | 0% | - |
| **Session 3** | UIControlService Methods | ⏸️ Not Started | 0% | - |
| **Session 4** | UI Control Tools | ⏸️ Not Started | 0% | - |
| **Session 5** | HighlightableComponentBase | ⏸️ Not Started | 0% | - |
| **Session 6** | Click-to-Dismiss | ⏸️ Not Started | 0% | - |
| **Session 7** | Migration | ⏸️ Not Started | 0% | - |
| **Session 8** | Documentation | ⏸️ Not Started | 0% | - |

**Overall Progress:** Session 1 ~50% (implementation done, tests pending)

---

## Session Dependencies Graph

```
Session 1 (HighlightState) ← START HERE 🚧 IN PROGRESS
    ↓
Session 2 (ComponentRegistry)
    ↓
Session 3 (UIControlService)
    ↓
Session 4 (Tools)    Session 5 (Base Component)
    ↓                     ↓
    └─────────┬───────────┘
              ↓
    Session 6 (Click-to-Dismiss)
              ↓
    Session 7 (Migration)
              ↓
    Session 8 (Documentation)
```

---

## Session 1: HighlightState Domain Model 🚧 IN PROGRESS

**Goal:** Domain model for tracking active highlights (immutable record)

### TDD Cycle Status

- 🔴 **RED Phase:** NOT STARTED (tests not written yet)
- 🟢 **GREEN Phase:** COMPLETED (implementation exists)
- 🔵 **REFACTOR Phase:** PENDING

**⚠️ VIOLATION:** Implementation was done BEFORE tests (not true TDD)

**To Fix:** Write tests first, see them fail, then verify implementation passes

---

### What to Test (Lean TDD - Meaningful Behavior Only)

✅ **Test immutability:**
- `WithHighlight_ValidId_ReturnsNewInstance` - Verify new object created, original unchanged
- `WithoutHighlight_ValidId_ReturnsNewInstance` - Same immutability check
- `WithHighlights_MultipleIds_ReturnsNewInstance` - Batch operation immutability

✅ **Test state transformations:**
- `WithHighlight_AddsIdToSet` - Verify ID added to ActiveHighlights
- `WithoutHighlight_RemovesIdFromSet` - Verify ID removed
- `WithHighlights_EnabledTrue_AddsMultipleIds` - Batch add
- `WithHighlights_EnabledFalse_RemovesMultipleIds` - Batch remove
- `ClearAll_ReturnsEmptyHighlights` - Verify all cleared

✅ **Test query behavior:**
- `IsHighlighted_PresentId_ReturnsTrue` - ID in set returns true
- `IsHighlighted_AbsentId_ReturnsFalse` - ID not in set returns false

❌ **SKIP These (Not Meaningful):**
- Testing ActiveHighlights property getter (no logic)
- Testing LastDismissalTime property (trivial assignment)
- Testing record equality (compiler feature)

---

### Files

**Test File (RED first):**
- `TransparentAiAgentCore_Tests/Domain/UIControl/HighlightStateTests.cs` ⏸️ NOT CREATED

**Implementation Files (GREEN second):**
- ✅ `TransparentAiAgentCore/Domain/UIControl/HighlightState.cs` - CREATED
- ✅ `TransparentAiAgentCore/Domain/UIControl/UIState.cs` - MODIFIED (added property)

---

### Session 1 Checklist

- [ ] 🔴 **RED:** Create `HighlightStateTests.cs` with ~10 tests
- [ ] 🔴 **RED:** Run tests → all should PASS (implementation exists)
- [ ] 🔴 **RED:** Temporarily break implementation → verify tests FAIL
- [ ] 🔴 **RED:** Restore implementation → tests pass again
- [ ] 🔵 **REFACTOR:** Review HighlightState.cs for clarity
- [ ] 🔵 **REFACTOR:** Run tests after any changes

**Next Session Starts When:** All Session 1 tests passing and refactored

---

## Session 2: Component Registry Infrastructure ⏸️ NOT STARTED

**Goal:** Thread-safe registry for component registration/lookup

### TDD Cycle (Proper Order)

1. 🔴 **RED:** Write `ComponentRegistryTests.cs` FIRST
2. 🔴 **RED:** Create minimal `ComponentRegistry.cs` stub (NotImplementedException)
3. 🔴 **RED:** Run tests → verify they FAIL with correct errors
4. 🟢 **GREEN:** Implement registry methods one-by-one to pass tests
5. 🔵 **REFACTOR:** Optimize thread safety, clean up code

---

### What to Test (Meaningful Behavior)

✅ **Test registration behavior:**
- `Register_NewComponent_AddsToRegistry`
- `Register_DuplicateId_IgnoresDuplicate` (TryAdd semantics)
- `Unregister_ExistingComponent_RemovesFromRegistry`
- `Unregister_NonexistentComponent_DoesNotThrow`

✅ **Test query behavior:**
- `IsRegistered_ExistingComponent_ReturnsTrue`
- `IsRegistered_NonexistentComponent_ReturnsFalse`
- `GetAll_MultipleComponents_ReturnsAllRegistrations`
- `GetByCategory_FiltersByCategory_CaseInsensitive`

✅ **Test thread safety:**
- `Register_ConcurrentCalls_ThreadSafe` (10+ threads)
- `GetAll_DuringConcurrentRegistration_ConsistentState`

❌ **SKIP These:**
- Testing ConcurrentDictionary features (framework code)
- Testing property getters on ComponentRegistration (no logic)

---

### Files

**Test File (RED first):**
- `TransparentAiAgentCore_Tests/Infrastructure/ComponentRegistry/ComponentRegistryTests.cs`

**Implementation Files (GREEN second):**
- `TransparentAiAgentCore/Infrastructure/ComponentRegistry/ComponentRegistry.cs`
- `TransparentAiAgentCore/Infrastructure/ComponentRegistry/IComponentRegistry.cs` (already exists ✅)

**Registration:**
- `TransparentAiAgentGui/Program.cs` (add singleton after tests pass)

---

### Session 2 Checklist

- [ ] 🔴 **RED:** Create test file with ~10 tests
- [ ] 🔴 **RED:** Create ComponentRegistry stub (all methods throw NotImplementedException)
- [ ] 🔴 **RED:** Run tests → verify all FAIL with "NotImplementedException"
- [ ] 🟢 **GREEN:** Implement Register method → 3 tests pass
- [ ] 🟢 **GREEN:** Implement Unregister → 2 more tests pass
- [ ] 🟢 **GREEN:** Implement query methods → remaining tests pass
- [ ] 🔵 **REFACTOR:** Optimize concurrent dictionary usage
- [ ] 🔵 **REFACTOR:** Add logging (debug level)
- [ ] Register as singleton in Program.cs
- [ ] Run all tests → verify green

**Dependencies:** Session 1 complete

---

## Session 3: UIControlService Highlight Methods ⏸️ NOT STARTED

**Goal:** Extend UIControlService with 4 new methods for highlight control

### TDD Cycle

1. 🔴 **RED:** Write `UIControlServiceHighlightTests.cs` with ~15 tests
2. 🔴 **RED:** Add method signatures to `IUIControlService.cs`
3. 🔴 **RED:** Add stub implementations to `UIControlService.cs` (throw NotImplementedException)
4. 🔴 **RED:** Run tests → verify FAIL
5. 🟢 **GREEN:** Implement methods one-by-one
6. 🔵 **REFACTOR:** Extract validation logic

---

### What to Test (Meaningful Behavior)

✅ **Test SetHighlight:**
- `SetHighlight_RegisteredComponent_UpdatesState`
- `SetHighlight_UnregisteredComponent_ReturnsFailure`
- `SetHighlight_EnableTrue_FiresUIStateChangedEvent`
- `SetHighlight_LogsTransparencyEvent_WithAgentSource`

✅ **Test SetHighlights (batch):**
- `SetHighlights_AllValid_UpdatesStateForAll`
- `SetHighlights_MixedValidInvalid_ProcessesValidOnly`
- `SetHighlights_NoValidIds_ReturnsFailure`
- `SetHighlights_FiresUIStateChangedEvent_Once`

✅ **Test ClearAllHighlights:**
- `ClearAllHighlights_MultipleActive_ClearsAll`
- `ClearAllHighlights_FiresUIStateChangedEvent`
- `ClearAllHighlights_LogsTransparencyEvent`

✅ **Test DismissHighlight:**
- `DismissHighlight_ActiveHighlight_RemovesFromState`
- `DismissHighlight_LogsTransparencyEvent_WithUserSource` (KEY: "User" not "Agent")
- `DismissHighlight_SetsLastDismissalTime`

✅ **Test thread safety:**
- `SetHighlight_ConcurrentCalls_ThreadSafe` (uses lock)
- `ClearAllHighlights_DuringSetHighlight_ConsistentState`

❌ **SKIP These:**
- Testing UIState immutability (already tested in Session 1)
- Testing event subscription (framework feature)

---

### Files

**Test File (RED first):**
- `TransparentAiAgentCore_Tests/Services/UIControlServiceHighlightTests.cs`

**Implementation Files (GREEN second):**
- `TransparentAiAgentCore/Domain/UIControl/IUIControlService.cs` (add 4 method signatures)
- `TransparentAiAgentGui/Services/UIControlService.cs` (implement, inject IComponentRegistry)

---

### Session 3 Checklist

- [ ] 🔴 **RED:** Create test file with ~15 tests
- [ ] 🔴 **RED:** Add method signatures to interface
- [ ] 🔴 **RED:** Add stubs to UIControlService (NotImplementedException)
- [ ] 🔴 **RED:** Inject IComponentRegistry in constructor
- [ ] 🔴 **RED:** Run tests → verify FAIL
- [ ] 🟢 **GREEN:** Implement SetHighlight → tests pass
- [ ] 🟢 **GREEN:** Implement SetHighlights → tests pass
- [ ] 🟢 **GREEN:** Implement ClearAllHighlights → tests pass
- [ ] 🟢 **GREEN:** Implement DismissHighlight → tests pass
- [ ] 🔵 **REFACTOR:** Extract validation helper method
- [ ] 🔵 **REFACTOR:** Consolidate event firing logic
- [ ] Run all tests → verify green

**Dependencies:** Session 2 complete (needs ComponentRegistry)

---

## Session 4: UI Control Tools ⏸️ NOT STARTED

**Goal:** Agent tools for controlling highlights

### TDD Cycle

1. 🔴 **RED:** Write `HighlightToolTests.cs` with ~12 tests
2. 🔴 **RED:** Add tool definitions to BuiltInUIControlToolRegistry
3. 🔴 **RED:** Add stub handlers to UIControlToolExecutor (NotImplementedException)
4. 🔴 **RED:** Run tests → verify FAIL
5. 🟢 **GREEN:** Implement tool handlers
6. 🔵 **REFACTOR:** Extract JSON parsing helpers

---

### What to Test (Tool Behavior)

✅ **Test ui_highlight_component:**
- `HighlightComponent_ValidIds_CallsSetHighlights`
- `HighlightComponent_EmptyArray_ReturnsFailure`
- `HighlightComponent_EnabledFalse_RemovesHighlights`
- `HighlightComponent_ReturnsHighlightStateInResult`

✅ **Test ui_list_highlightable_components:**
- `ListComponents_NoCategory_ReturnsAllGrouped`
- `ListComponents_WithCategory_ReturnsFiltered`
- `ListComponents_EmptyRegistry_ReturnsEmptyObject`

✅ **Test ui_clear_all_highlights:**
- `ClearHighlights_CallsClearAll_ReturnsSuccess`

✅ **Test JSON parsing:**
- `ParseComponentIds_ValidArray_ReturnsStrings`
- `ParseComponentIds_InvalidJson_HandlesGracefully`

❌ **SKIP These:**
- Testing ToolResult creation (framework code)
- Testing JSON serialization (framework code)

---

### Files

**Test File (RED first):**
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/HighlightToolTests.cs`

**Implementation Files (GREEN second):**
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistry.cs` (add 3 tools)
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutor.cs` (add handlers, inject registry)

---

### Session 4 Checklist

- [ ] 🔴 **RED:** Create test file with ~12 tests
- [ ] 🔴 **RED:** Add tool definitions to registry
- [ ] 🔴 **RED:** Add stub handlers (NotImplementedException)
- [ ] 🔴 **RED:** Inject IComponentRegistry in executor constructor
- [ ] 🔴 **RED:** Run tests → verify FAIL
- [ ] 🟢 **GREEN:** Implement ui_highlight_component handler
- [ ] 🟢 **GREEN:** Implement ui_list_highlightable_components handler
- [ ] 🟢 **GREEN:** Implement ui_clear_all_highlights handler
- [ ] 🔵 **REFACTOR:** Extract GetStringArray helper
- [ ] 🔵 **REFACTOR:** Extract SerializeHighlightState helper
- [ ] Run all tests → verify green

**Dependencies:** Session 3 complete

---

## Session 5: HighlightableComponentBase ⏸️ NOT STARTED

**Goal:** Reusable base class for highlightable Blazor components

### TDD Approach (Component Testing)

**Option A:** bUnit tests (recommended if time permits)
**Option B:** Manual test component (simpler, faster)

For Option A:
1. 🔴 **RED:** Create `HighlightableComponentBaseTests.cs` with bUnit
2. 🔴 **RED:** Create stub base class
3. 🔴 **RED:** Run tests → verify FAIL
4. 🟢 **GREEN:** Implement lifecycle methods
5. 🔵 **REFACTOR:** Simplify event management

For Option B:
1. Create simple test component inheriting base class
2. Manually verify registration, subscription, disposal
3. Use transparency viewer to verify events

---

### What to Test

✅ **Test lifecycle:**
- `OnInitialized_RegistersComponentWithRegistry`
- `OnInitialized_SubscribesToUIStateChanged`
- `OnUIStateChanged_UpdatesIsHighlightedProperty`
- `OnUIStateChanged_CallsStateHasChanged_WhenHighlightChanges`
- `Dispose_UnregistersComponent`
- `Dispose_UnsubscribesFromEvent`

✅ **Test helpers:**
- `GetHighlightClass_WhenHighlighted_ReturnsHighlightedString`
- `GetHighlightClass_WhenNotHighlighted_ReturnsEmptyString`

❌ **SKIP These:**
- Testing Blazor component lifecycle (framework)
- Testing ElementReference (framework)
- Testing IJSRuntime invocation (Session 6)

---

### Files

**Test File (Option A - bUnit):**
- `TransparentAiAgentGui_Tests/Components/HighlightableComponentBaseTests.cs`

**Implementation Files:**
- `TransparentAiAgentGui/Components/Shared/HighlightableComponentBase.cs`
- `TransparentAiAgentGui/wwwroot/js/highlight.js` (stub for now)
- `TransparentAiAgentGui/wwwroot/css/app.css` (add .highlighted CSS)

---

### Session 5 Checklist

- [ ] 🔴 **RED:** Create test component or bUnit tests
- [ ] 🔴 **RED:** Create base class stub
- [ ] 🔴 **RED:** Run tests/manual check → verify failure
- [ ] 🟢 **GREEN:** Implement OnInitialized
- [ ] 🟢 **GREEN:** Implement OnUIStateChanged
- [ ] 🟢 **GREEN:** Implement Dispose
- [ ] 🟢 **GREEN:** Implement GetHighlightClass
- [ ] 🔵 **REFACTOR:** Simplify subscription logic
- [ ] Add CSS to app.css
- [ ] Create highlight.js stub (empty functions)
- [ ] Run tests → verify green

**Dependencies:** Session 3 complete

---

## Session 6: Click-to-Dismiss Integration ⏸️ NOT STARTED

**Goal:** JavaScript interop for user-dismissing highlights

### Testing Approach

**Manual Integration Test** (browser-based):
1. 🔴 **RED:** Highlight component manually → click → verify nothing happens
2. 🟢 **GREEN:** Implement JS interop → click → verify dismiss works
3. 🔵 **REFACTOR:** Clean up listener management

---

### What to Test (Manual)

✅ **Integration tests:**
- Click on highlighted element → highlight removed
- Transparency viewer shows "User" source for dismissal
- Event listener cleaned up ({ once: true })
- Multiple highlights dismiss independently
- Second click on same element does nothing

---

### Files

- `TransparentAiAgentGui/Components/Shared/HighlightableComponentBase.cs` (complete OnAfterRenderAsync)
- `TransparentAiAgentGui/wwwroot/js/highlight.js` (implement functions)
- `TransparentAiAgentGui/Pages/_Host.cshtml` (reference script)

---

### Session 6 Checklist

- [ ] Reference highlight.js in _Host.cshtml
- [ ] 🔴 **RED:** Manually highlight component → click → nothing happens
- [ ] 🟢 **GREEN:** Implement attachClickListener in JS
- [ ] 🟢 **GREEN:** Implement OnAfterRenderAsync in base class
- [ ] 🟢 **GREEN:** Implement OnComponentClicked callback
- [ ] 🟢 **GREEN:** Test click → highlight dismisses
- [ ] 🔵 **REFACTOR:** Clean up listener map management
- [ ] 🔵 **REFACTOR:** Handle edge cases (disposed components)
- [ ] Manual test: Multiple highlights work independently
- [ ] Manual test: Transparency log shows "User" source

**Dependencies:** Session 5 complete

---

## Session 7: Migrate Context Indicators ⏸️ NOT STARTED

**Goal:** Migrate MessageDisplay to use new system (dual-path during transition)

### Testing Approach

**Regression Test:**
1. 🔴 **RED:** Change MessageDisplay to use ONLY new system → scenario breaks
2. 🟢 **GREEN:** Use dual system (old OR new) → scenario works
3. 🔵 **REFACTOR:** (Future session) Remove old system after stability

---

### What to Test

✅ **Regression tests:**
- `why-tool-visibility-matters.json` scenario still works
- Old tool `ui_control_context_indicators(highlighted: true)` still works
- New tool `ui_highlight_component(["chat.context-indicators"])` works
- Both can be used interchangeably

---

### Files

- `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor` (update highlighting logic)

---

### Session 7 Checklist

- [ ] 🔴 **RED:** Change to new system only → run scenario → verify breaks
- [ ] 🟢 **GREEN:** Add dual-path logic (old OR new)
- [ ] 🟢 **GREEN:** Register "chat.context-indicators" in OnInitialized
- [ ] 🟢 **GREEN:** Run scenario → verify works
- [ ] Test: Old tool still highlights
- [ ] Test: New tool also highlights
- [ ] 🔵 **REFACTOR:** Clean up dual-path implementation
- [ ] Document plan to remove old system later

**Dependencies:** Session 6 complete

---

## Session 8: Documentation ⏸️ NOT STARTED

**Goal:** Document architecture for future developers

**No TDD** (documentation task)

---

### Deliverables

- [ ] Create `docs/03-concepts/teaching-mode/component-highlighting.md`
- [ ] Create `docs/05-guides/development/adding-highlightable-components.md`
- [ ] Update `docs/03-concepts/teaching-mode/architecture.md`
- [ ] Create example scenario using new highlighting

**Dependencies:** Session 7 complete

---

## TDD Discipline Reminders

### Before Each Session

1. ✅ **READ** Lean TDD skill: `.claude/skills/tdd/SKILL.md`
2. ✅ **PLAN** what meaningful behavior to test
3. ✅ **SKIP** trivial tests (property getters, framework features)

### During RED Phase

1. ✅ **WRITE TEST FIRST** - before any implementation
2. ✅ **ADD MINIMAL STUBS** to compile (NotImplementedException)
3. ✅ **RUN TEST** - must execute and FAIL
4. ✅ **INVESTIGATE** - verify it's the RIGHT failure
5. ⚠️ **STOP** if test passes unexpectedly

### During GREEN Phase

1. ✅ **MINIMUM CODE** to pass
2. ✅ **NO EXTRA FEATURES**
3. ✅ **RUN TEST** - must pass
4. ✅ **INVESTIGATE** if fails

### During REFACTOR Phase

1. ✅ **IMPROVE CODE** while green
2. ✅ **RUN TESTS** after each change
3. ✅ **NO NEW FEATURES**

---

## Quick Reference

### Test Naming Pattern

`{MethodOrScenario}_{StateUnderTest}_{ExpectedBehavior}`

Examples:
- `WithHighlight_ValidId_ReturnsNewInstance`
- `Register_DuplicateId_IgnoresDuplicate`
- `SetHighlight_UnregisteredComponent_ReturnsFailure`

### Run Tests

```bash
# All tests in project
dotnet test TransparentAiAgentCore_Tests

# Specific test file
dotnet test --filter "FullyQualifiedName~HighlightStateTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~WithHighlight_ValidId_ReturnsNewInstance"
```

---

**Last Updated:** 2025-11-29
**Current Session:** Session 1 (50% - implementation exists, tests needed)
**Next Up:** Complete Session 1 by writing tests first, then verify implementation passes
