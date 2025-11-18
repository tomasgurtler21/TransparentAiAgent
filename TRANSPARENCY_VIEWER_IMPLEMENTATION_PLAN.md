# TransparencyViewer Implementation Plan

**Created**: 2025-11-18
**Status**: Ready for Implementation
**Session-Based**: Yes - Each step can be done in independent sessions

---

## Overview

This document provides a detailed implementation plan for three improvements to the TransparencyViewer, following Lean TDD principles. Each step is designed to be completable in an independent session.

**Related Documents:**
- [TRANSPARENCY_IMPROVEMENTS_DISCUSSION.md](TRANSPARENCY_IMPROVEMENTS_DISCUSSION.md) - Full context and decisions
- [.claude/skills/tdd/SKILL.md](.claude/skills/tdd/SKILL.md) - TDD workflow guidelines

**Implementation Order:**
1. Event Type Cleanup (simplest, enables cleaner work for other steps)
2. Export to JSON (medium complexity, independent feature)
3. Multi-Filter UI (most complex, benefits from cleaner event types)

---

## Step 1: Event Type Cleanup

**Goal**: Remove 17 unused event types from TransparencyEventType enum

**Can be completed in**: Single session (~30-45 minutes)

**Prerequisites**: None

### Phase 1.1: Update Tests (RED → GREEN)

**RED Phase:**

1. **Identify all test files** that reference unused event types:
   ```bash
   # Search for test usage of unused types
   grep -r "ToolDiscoveryStarted\|ToolCallStarted\|MCPServerConnecting" TransparentAiAgentCore_Tests/
   ```

2. **Review each test** to understand what it's testing:
   - If testing meaningful behavior → rewrite to use existing event type
   - If testing trivial enum value → delete the test (Lean TDD principle)

3. **Expected test failures**: Tests referencing removed types won't compile (setup phase)

**GREEN Phase:**

4. **Fix or delete tests** identified above
5. **Run all tests** - ensure they pass:
   ```bash
   dotnet test TransparentAiAgentCore_Tests
   ```

**Validation**: All existing tests pass with no compilation errors

### Phase 1.2: Remove Unused Event Types (RED → GREEN)

**RED Phase:**

1. **Remove 17 unused event types** from `TransparencyEventType.cs`:

   **Types to REMOVE:**
   - UserInput
   - ConfigurationChange
   - Warning
   - Debug
   - ToolDiscoveryStarted
   - ToolDiscoveryCompleted
   - ToolDiscoveryFailed
   - ToolRegistered
   - ToolCallStarted
   - ToolCallCompleted
   - ToolCallFailed
   - ToolCallTimeout
   - MCPServerConnecting
   - MCPServerConnected
   - MCPServerDisconnected
   - MCPServerConnectionFailed

   **Types to KEEP (13 currently used):**
   - ToolCall
   - ToolResult
   - Error
   - SystemState
   - ContextChange
   - AssistantResponse
   - RawLLMRequest
   - RawLLMResponse
   - MessageParsingError
   - ToolArgumentValidationFailed
   - ToolStreamingDataCorrupted
   - Info
   - UIControlAction

2. **Expected failures**: Any production code referencing removed types won't compile

**GREEN Phase:**

3. **Search for production code usage**:
   ```bash
   # Check each removed type
   grep -r "TransparencyEventType.UserInput" TransparentAiAgentCore/
   grep -r "TransparencyEventType.ToolDiscoveryStarted" TransparentAiAgentCore/
   # ... (repeat for all removed types)
   ```

4. **Fix any references** found (unlikely based on earlier analysis)

5. **Verify UI still works**:
   - Open TransparencyViewer.razor
   - Check that dropdown event filter still renders correctly
   - Verify icon mapping in TransparencyEventDisplay.razor handles all remaining types

6. **Run all tests**:
   ```bash
   dotnet test TransparentAiAgentCore_Tests
   ```

**Validation**:
- All tests pass
- Application compiles without errors
- TransparencyViewer dropdown shows only 13 event types

### Phase 1.3: Cleanup UI Mappings

**Files to check:**
- `TransparencyEventDisplay.razor` - Remove icon mappings for deleted types
- `TransparencyViewer.razor` - Verify dropdown generation works with fewer types

**No tests needed**: UI mappings are presentation logic without business rules (Lean TDD principle)

### Deliverables

- [ ] TransparencyEventType.cs contains only 13 used event types
- [ ] All tests pass
- [ ] No compilation errors in production code
- [ ] TransparencyViewer UI displays correctly with reduced event types

---

## Step 2: Export to JSON

**Goal**: Add "Save" button that exports all transparency events to JSON file with privacy warning

**Can be completed in**: Single session (~1-2 hours)

**Prerequisites**: Step 1 completed (optional but recommended)

### Phase 2.1: Add JavaScript Download Function

**Location**: `wwwroot/js/site.js`

**Implementation** (no tests needed - JavaScript not testable in MSTest):

```javascript
// Add to existing site.js file
window.downloadFile = function(filename, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
};
```

**Validation**: Visual inspection only (will be tested in Phase 2.3)

### Phase 2.2: Create Export Service Logic (TDD)

**Goal**: Test JSON serialization of transparency events

**File**: `TransparentAiAgentCore/Infrastructure/Transparency/TransparencyService.cs`

**RED Phase:**

1. **Create test file**: `TransparentAiAgentCore_Tests/Infrastructure/Transparency/TransparencyServiceExportTests.cs`

2. **Write first test** - Export creates valid JSON structure:

```csharp
[TestClass]
public class TransparencyServiceExportTests
{
    [TestMethod]
    public void ExportToJson_WithEvents_ReturnsValidJsonStructure()
    {
        // Arrange
        var service = new TransparencyService();
        service.LogEvent(TransparencyEventType.Info, "test data", "additional");

        // Act
        var json = service.ExportToJson();

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("\"exportedAt\""));
        Assert.IsTrue(json.Contains("\"events\""));
        Assert.IsTrue(json.Contains("\"eventType\""));
    }
}
```

3. **Add minimal stub** to TransparencyService.cs:

```csharp
public string ExportToJson()
{
    throw new NotImplementedException();
}
```

4. **Run test** - should fail with NotImplementedException

**GREEN Phase:**

5. **Implement ExportToJson**:

```csharp
public string ExportToJson()
{
    var export = new
    {
        exportedAt = DateTime.UtcNow,
        events = GetEvents() // Uses existing method that returns ALL events
    };

    return JsonSerializer.Serialize(export, new JsonSerializerOptions
    {
        WriteIndented = true
    });
}
```

6. **Run test** - should pass

**REFACTOR Phase:**

7. **Add more tests** for edge cases:

```csharp
[TestMethod]
public void ExportToJson_NoEvents_ReturnsEmptyEventsArray()
{
    // Arrange
    var service = new TransparencyService();

    // Act
    var json = service.ExportToJson();

    // Assert
    Assert.IsTrue(json.Contains("\"events\": []"));
}

[TestMethod]
public void ExportToJson_ContainsExportTimestamp()
{
    // Arrange
    var service = new TransparencyService();
    var beforeExport = DateTime.UtcNow;

    // Act
    var json = service.ExportToJson();
    var afterExport = DateTime.UtcNow;

    // Assert - timestamp should be between before and after
    Assert.IsTrue(json.Contains("\"exportedAt\""));
    // Note: Not testing exact timestamp value (fragile test)
}
```

8. **Run all tests** - should pass

**Validation**: All export tests pass

### Phase 2.3: Add UI Components (Manual Testing)

**File**: `TransparentAiAgentCore.WebUI/Components/Transparency/TransparencyViewer.razor`

**Implementation Steps:**

1. **Add IJSRuntime injection** at top of file:

```razor
@inject IJSRuntime JSRuntime
```

2. **Add Save button** to controls section (around line 12-15):

```razor
<div class="transparency-controls">
    <input type="text" @bind="SearchQuery" @bind:event="oninput"
           placeholder="Search events..." class="search-box" />

    <select @bind="SelectedEventType" class="filter-select">
        <option value="">All Event Types</option>
        @foreach (var eventType in Enum.GetValues<TransparencyEventType>())
        {
            <option value="@eventType.ToString()">@eventType</option>
        }
    </select>

    <button @onclick="HandleExportClick" class="btn-save">Save</button>
</div>
```

3. **Add export handler** in @code block:

```csharp
private async Task HandleExportClick()
{
    var confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
        "Logs contain all message content. Ensure no sensitive information before exporting. Continue?");

    if (confirmed)
    {
        await ExportToJson();
    }
}

private async Task ExportToJson()
{
    var json = TransparencyService.ExportToJson();
    var filename = $"transparency-events-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
    await JSRuntime.InvokeVoidAsync("downloadFile", filename, json);
}
```

4. **Add CSS styling** to `TransparencyViewer.razor.css`:

```css
.btn-save {
    padding: 8px 16px;
    background-color: #0066cc;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
}

.btn-save:hover {
    background-color: #0052a3;
}
```

**Manual Validation:**

1. Run the application
2. Navigate to TransparencyViewer
3. Click "Save" button
4. Verify confirmation dialog appears with privacy warning
5. Click "Yes" → verify browser download dialog opens
6. Open downloaded JSON file → verify structure:
   ```json
   {
     "exportedAt": "2025-11-18T10:30:45Z",
     "events": [...]
   }
   ```

**No automated UI tests**: Blazor component rendering and JS interop not tested (Lean TDD - would be integration tests, out of scope)

### Deliverables

- [ ] TransparencyService.ExportToJson() method implemented with tests
- [ ] JavaScript downloadFile() function added to site.js
- [ ] Save button added to TransparencyViewer UI
- [ ] Privacy confirmation dialog works
- [ ] JSON export downloads successfully with correct structure
- [ ] All tests pass

---

## Step 3: Multi-Filter UI

**Goal**: Replace single event type dropdown with dynamic multi-filter (OR logic)

**Can be completed in**: Single session (~2-3 hours)

**Prerequisites**: Step 1 completed (recommended for cleaner event type list)

### Phase 3.1: Update Component State and Logic (TDD)

**File**: `TransparentAiAgentCore.WebUI/Components/Transparency/TransparencyViewer.razor`

**RED Phase:**

1. **Plan the logic change**:
   - Current: `SelectedEventType` (string) - single filter
   - New: `SelectedEventTypes` (List<string>) - multiple filters
   - Filter logic: Show events matching ANY selected type (OR logic)

2. **Write test** for filtering logic in `TransparencyViewerTests.cs`:

   **NOTE**: If TransparencyViewerTests.cs doesn't exist, check if filtering logic should be in a separate service. If logic is complex enough, extract to testable service class. If simple UI logic, skip tests (Lean TDD - UI component tests often not worth the complexity).

   **Decision point**: Check if `TransparencyViewer.razor` has complex filtering logic worth testing. Based on TRANSPARENCY_IMPROVEMENTS_DISCUSSION.md lines 119-131, it's moderately complex.

   **Option A - Extract to Service (Recommended if logic grows):**

   Create `TransparencyFilterService` with testable methods:

   ```csharp
   // Test
   [TestClass]
   public class TransparencyFilterServiceTests
   {
       [TestMethod]
       public void ApplyEventTypeFilters_MultipleTypes_ReturnsEventsMatchingAny()
       {
           // Arrange
           var events = new[]
           {
               new TransparencyEvent(Guid.NewGuid(), DateTime.UtcNow,
                   TransparencyEventType.Info, "info data", null),
               new TransparencyEvent(Guid.NewGuid(), DateTime.UtcNow,
                   TransparencyEventType.Error, "error data", null),
               new TransparencyEvent(Guid.NewGuid(), DateTime.UtcNow,
                   TransparencyEventType.ToolCall, "tool data", null)
           };
           var filters = new List<string> { "Info", "Error" };
           var service = new TransparencyFilterService();

           // Act
           var result = service.ApplyEventTypeFilters(events, filters);

           // Assert
           Assert.AreEqual(2, result.Count());
           Assert.IsTrue(result.Any(e => e.EventType == TransparencyEventType.Info));
           Assert.IsTrue(result.Any(e => e.EventType == TransparencyEventType.Error));
           Assert.IsFalse(result.Any(e => e.EventType == TransparencyEventType.ToolCall));
       }

       [TestMethod]
       public void ApplyEventTypeFilters_EmptyFilters_ReturnsAllEvents()
       {
           // Arrange
           var events = new[]
           {
               new TransparencyEvent(Guid.NewGuid(), DateTime.UtcNow,
                   TransparencyEventType.Info, "info", null),
               new TransparencyEvent(Guid.NewGuid(), DateTime.UtcNow,
                   TransparencyEventType.Error, "error", null)
           };
           var filters = new List<string>();
           var service = new TransparencyFilterService();

           // Act
           var result = service.ApplyEventTypeFilters(events, filters);

           // Assert
           Assert.AreEqual(2, result.Count());
       }
   }
   ```

   **Option B - Keep in Component (Simpler):**

   Skip automated tests, rely on manual testing (Lean TDD - simple UI logic doesn't need tests if manually verifiable).

   **RECOMMENDATION**: Use Option B for this implementation (simpler, faster). The filtering logic is straightforward OR logic.

**GREEN Phase:**

3. **Update TransparencyViewer.razor state** (no test needed):

   Replace:
   ```csharp
   private string SelectedEventType { get; set; } = string.Empty;
   ```

   With:
   ```csharp
   private List<EventTypeFilter> EventTypeFilters { get; set; } = new()
   {
       new EventTypeFilter() // Start with one empty filter
   };

   private class EventTypeFilter
   {
       public string SelectedType { get; set; } = string.Empty;
   }
   ```

4. **Update filtering logic** in FilteredEvents computed property:

   Replace existing event type filter logic with:
   ```csharp
   // Apply multi-filter (OR logic)
   var selectedTypes = EventTypeFilters
       .Where(f => !string.IsNullOrEmpty(f.SelectedType))
       .Select(f => f.SelectedType)
       .ToList();

   if (selectedTypes.Any())
   {
       filtered = filtered.Where(e => selectedTypes.Contains(e.EventType.ToString()));
   }
   ```

5. **Remove UI state filter logic** (lines 83-87 based on discussion doc):
   ```csharp
   // DELETE THIS BLOCK:
   if (_uiState.TransparencyViewer.EventTypeFilters.Any())
   {
       filtered = filtered.Where(e =>
           _uiState.TransparencyViewer.EventTypeFilters.Contains(e.EventType.ToString()));
   }
   ```

**REFACTOR Phase:**

6. **Add filter management methods**:

```csharp
private void AddFilter()
{
    EventTypeFilters.Add(new EventTypeFilter());
}

private void RemoveFilter(EventTypeFilter filter)
{
    if (EventTypeFilters.Count > 1) // Keep at least one filter
    {
        EventTypeFilters.Remove(filter);
    }
}
```

### Phase 3.2: Update UI Markup

**File**: `TransparentAiAgentCore.WebUI/Components/Transparency/TransparencyViewer.razor`

**Implementation:**

1. **Replace single dropdown** with multi-filter UI:

```razor
<div class="transparency-controls">
    <input type="text" @bind="SearchQuery" @bind:event="oninput"
           placeholder="Search events..." class="search-box" />

    <div class="event-filters">
        @foreach (var filter in EventTypeFilters)
        {
            <div class="filter-item">
                <select @bind="filter.SelectedType" class="filter-select">
                    <option value="">All Event Types</option>
                    @foreach (var eventType in Enum.GetValues<TransparencyEventType>())
                    {
                        <option value="@eventType.ToString()">@eventType</option>
                    }
                </select>

                @if (EventTypeFilters.Count > 1)
                {
                    <button @onclick="() => RemoveFilter(filter)"
                            class="btn-remove-filter"
                            title="Remove filter">✕</button>
                }
            </div>
        }

        <button @onclick="AddFilter" class="btn-add-filter">+ Add Filter</button>
    </div>

    @if (/* Export button from Step 2 exists */)
    {
        <button @onclick="HandleExportClick" class="btn-save">Save</button>
    }
</div>
```

### Phase 3.3: Add CSS Styling

**File**: `TransparentAiAgentCore.WebUI/Components/Transparency/TransparencyViewer.razor.css`

**Implementation:**

```css
.event-filters {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
}

.filter-item {
    display: flex;
    align-items: center;
    gap: 4px;
}

.filter-select {
    padding: 8px;
    border: 1px solid #ccc;
    border-radius: 4px;
    font-size: 14px;
    background-color: white;
}

.btn-remove-filter {
    padding: 4px 8px;
    background-color: #dc3545;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 16px;
    line-height: 1;
}

.btn-remove-filter:hover {
    background-color: #c82333;
}

.btn-add-filter {
    padding: 8px 12px;
    background-color: #28a745;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
}

.btn-add-filter:hover {
    background-color: #218838;
}
```

### Manual Validation

1. **Run the application**
2. **Test default state**: One filter dropdown visible
3. **Test adding filters**: Click "+ Add Filter" → new dropdown appears
4. **Test removing filters**: Click ✕ → filter disappears (can't remove last one)
5. **Test OR logic**:
   - Filter 1: Select "Info"
   - Filter 2: Select "Error"
   - Verify events shown include BOTH Info AND Error types
6. **Test empty filters**: Set filter to "All Event Types" → should be ignored
7. **Test with search**: Combine filters with search box → both should work together

### Deliverables

- [ ] Multiple event type filters can be added/removed dynamically
- [ ] OR logic: Events matching ANY selected filter are shown
- [ ] Empty/"All Event Types" filters are ignored
- [ ] At least one filter always remains
- [ ] UI is clean and intuitive
- [ ] Filters work correctly with search box
- [ ] UI state filter logic removed from component
- [ ] Manual testing confirms all scenarios work

---

## Testing Strategy

### Automated Tests (Following Lean TDD)

**Test meaningful behavior only:**

✅ **Step 1**: No new tests needed (only deletion)
✅ **Step 2**: Test JSON serialization logic (business rule)
❌ **Step 3**: Skip UI filter tests (simple OR logic, manually verifiable)

**Why skip some tests?**
- UI component rendering → Not meaningful behavior (framework feature)
- Simple OR filtering → No complex business rules to test
- JavaScript interop → Can't test in MSTest
- CSS styling → Visual, not logical

### Manual Testing Checklist

After each step:

**Step 1 Validation:**
- [ ] Application compiles
- [ ] TransparencyViewer opens without errors
- [ ] Event type dropdown shows only 13 types
- [ ] All existing events display correctly

**Step 2 Validation:**
- [ ] Save button appears in UI
- [ ] Clicking Save shows confirmation dialog
- [ ] Privacy warning text is clear
- [ ] Clicking Yes downloads JSON file
- [ ] JSON file structure is correct
- [ ] All events included in export (not just visible 1000)

**Step 3 Validation:**
- [ ] One filter dropdown visible by default
- [ ] Add Filter button works
- [ ] Remove filter (✕) works
- [ ] Can't remove last filter
- [ ] OR logic: Multiple types shown simultaneously
- [ ] Empty filters ignored
- [ ] Works with search box

---

## Risk Assessment & Mitigation

### Step 1 Risks
- **Risk**: Accidentally removing used event type
- **Mitigation**: Double-check grep results before deleting
- **Mitigation**: Run full test suite after deletion

### Step 2 Risks
- **Risk**: Large exports (>10MB) slow or crash browser
- **Mitigation**: Document known limitation, acceptable for MVP
- **Risk**: JavaScript not loaded when Save clicked
- **Mitigation**: Blazor ensures JS loaded before interaction possible

### Step 3 Risks
- **Risk**: Complex filter state management introduces bugs
- **Mitigation**: Keep logic simple (List<EventTypeFilter>, basic OR)
- **Mitigation**: Thorough manual testing of all scenarios
- **Risk**: Removing UI state filter logic breaks chat component
- **Mitigation**: Verify chat component doesn't actually use this logic (per discussion doc)

---

## Session Boundaries

Each step is designed for independent sessions:

### Session 1: Event Type Cleanup
- **Time**: 30-45 minutes
- **Deliverable**: Cleaner enum with only used types
- **Blocking issues**: None (can be done anytime)

### Session 2: Export to JSON
- **Time**: 1-2 hours
- **Deliverable**: Working Save button with JSON export
- **Blocking issues**: None (independent feature)
- **Optional dependency**: Step 1 (cleaner event types in export)

### Session 3: Multi-Filter UI
- **Time**: 2-3 hours
- **Deliverable**: Dynamic multi-filter UI with OR logic
- **Blocking issues**: None
- **Recommended dependency**: Step 1 (fewer event types = cleaner UI)

**Total estimated time**: 4-6 hours across 3 sessions

---

## Success Criteria

**Step 1 Success:**
- ✅ Only 13 event types remain in enum
- ✅ All tests pass
- ✅ No compilation errors
- ✅ UI dropdown shows correct types

**Step 2 Success:**
- ✅ Save button functional in UI
- ✅ Privacy warning displays correctly
- ✅ JSON export downloads with correct structure
- ✅ All events exported (not limited to visible 1000)
- ✅ Export tests pass

**Step 3 Success:**
- ✅ Multiple filters can be added/removed
- ✅ OR logic works correctly
- ✅ UI is intuitive and clean
- ✅ No regressions in search functionality
- ✅ UI state filter logic removed

**Overall Success:**
- ✅ All automated tests pass
- ✅ Manual testing confirms all features work
- ✅ No breaking changes to existing functionality
- ✅ Code follows Lean TDD principles
- ✅ Documentation updated (this plan + discussion doc)

---

## Notes

- **TDD Approach**: Follow Red-Green-Refactor where applicable
- **Lean Testing**: Only test meaningful behavior, skip trivial tests
- **Manual Testing**: UI changes require thorough manual validation
- **Git Operations**: Not included (handled separately by implementer)
- **Environment**: Prone to crashes - save work frequently

---

## Related Documentation

- [TRANSPARENCY_IMPROVEMENTS_DISCUSSION.md](TRANSPARENCY_IMPROVEMENTS_DISCUSSION.md) - Full context and decisions
- [.claude/skills/tdd/SKILL.md](.claude/skills/tdd/SKILL.md) - TDD workflow
- [docs/04-components/infrastructure/transparency-service.md](docs/04-components/infrastructure/transparency-service.md) - Component docs
- [docs/04-components/ui/transparency-viewer.md](docs/04-components/ui/transparency-viewer.md) - UI component docs

---

**Ready for implementation!** Each step can be tackled independently in separate sessions.
