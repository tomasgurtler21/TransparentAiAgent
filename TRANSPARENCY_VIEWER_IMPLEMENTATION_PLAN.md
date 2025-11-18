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

## Code Investigation Results

### File Structure (VERIFIED)

**Project Names:**
- Core: `TransparentAiAgentCore`
- GUI: `TransparentAiAgentGui` (NOT WebUI)
- Core Tests: `TransparentAiAgentCore_Tests`
- GUI Tests: `TransparentAiAgentGui_Tests`

**Key Files:**
- Event Type Enum: `/TransparentAiAgentCore/Domain/Transparency/TransparencyEventType.cs` (30 types defined)
- Viewer Component: `/TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`
- Service: `/TransparentAiAgentCore/Infrastructure/Transparency/TransparencyService.cs`
- JavaScript: `/TransparentAiAgentGui/wwwroot/js/site.js`
- Viewer Tests: `/TransparentAiAgentGui_Tests/Components/Transparency/TransparencyViewerTests.cs`
- Service Tests: `/TransparentAiAgentCore_Tests/Infrastructure/Transparency/TransparencyServiceTests.cs`

**Note:** There's also an OLD enum at `/TransparentAiAgentCore/Domain/Enums/TransparencyEventType.cs` with only 8 types. This appears to be deprecated/unused. Investigation needed to determine if it should be deleted.

### Event Type Usage (VERIFIED)

**Total defined**: 30 types in `Domain/Transparency/TransparencyEventType.cs`

**Actually USED in production code (12 types):**
1. SystemState (14 uses)
2. Error (9 uses)
3. RawLLMResponse (7 uses)
4. RawLLMRequest (4 uses)
5. AssistantResponse (2 uses)
6. ToolStreamingDataCorrupted (1 use)
7. ToolResult (1 use)
8. ToolCall (1 use)
9. ToolArgumentValidationFailed (1 use)
10. MessageParsingError (1 use)
11. Info (1 use)
12. ContextChange (1 use)

**Used ONLY in tests (3 types):**
- UserInput (TransparencyViewerTests.cs, TransparencyServiceTests.cs)
- ToolCallCompleted (TransparencyViewerTests.cs line 161)
- UIControlAction (appears in test expectations but not actually logged)

**UNUSED - Never referenced (15 types):**
- ConfigurationChange
- Warning
- Debug
- ToolDiscoveryStarted
- ToolDiscoveryCompleted
- ToolDiscoveryFailed
- ToolRegistered
- ToolCallStarted
- ToolCallFailed
- ToolCallTimeout
- MCPServerConnecting
- MCPServerConnected
- MCPServerDisconnected
- MCPServerConnectionFailed

**Decision needed**: Should we keep the 3 test-only types (UserInput, ToolCallCompleted, UIControlAction)? Or remove them too?

### Current Component State (VERIFIED)

**TransparencyViewer.razor:**
- Line 8: Already has `@inject IJSRuntime JSRuntime`
- Lines 26-32: Single dropdown filter (`SelectedEventType`)
- Lines 83-87: Has UI state filter logic that discussion doc says to remove
- Line 104: Loads only 1000 recent events
- Filter logic: Lines 76-99 (computed property `FilteredEvents`)

**TransparencyService.cs:**
- Has `GetEvents()` - returns ALL events (line 26-32)
- Has `GetRecentEvents(int count)` - returns limited events (line 34-40)
- NO `ExportToJson()` method exists
- Already thread-safe with locks

**site.js:**
- Has `scrollToBottom` function
- Has `overlayResize` functionality
- NO `downloadFile` function

---

## Step 1: Event Type Cleanup

**Goal**: Remove unused event types from TransparencyEventType enum

**Can be completed in**: Single session (~45-60 minutes)

**Prerequisites**: None

### Phase 1.1: Decide on Test-Only Types

**Question for implementer**: Should we keep types that are only used in tests?

**Option A - Keep test-only types (3 types):**
- UserInput
- ToolCallCompleted
- UIControlAction

Rationale: Tests might be preparing for future features, or testing filter behavior

**Option B - Remove test-only types:**

Rationale: Lean TDD says don't keep unused code. If needed later, add back when implementing feature.

**RECOMMENDATION**: Option A (keep test-only types for now)
- UserInput is a logical event type that might be used soon
- Tests would need significant updates to remove these
- Low cost to keep them (only 3 types)

**After decision, proceed with removal count:**
- **If Option A**: Remove 15 unused types
- **If Option B**: Remove 18 unused types (15 + 3 test-only)

### Phase 1.2: Investigate Duplicate Enum File

**Files:**
- `/TransparentAiAgentCore/Domain/Enums/TransparencyEventType.cs` (8 types - old?)
- `/TransparentAiAgentCore/Domain/Transparency/TransparencyEventType.cs` (30 types - current)

**Task:**
1. Search entire solution for references to `Domain.Enums.TransparencyEventType`
2. If no references found → DELETE the old file
3. If references found → Migrate to new namespace first, then delete

**Command to check:**
```bash
grep -r "Domain.Enums.TransparencyEventType" . --include="*.cs"
grep -r "using.*Domain.Enums" . --include="*.cs"
```

### Phase 1.3: Update Tests for Removed Types

**If Option A (keep test-only types):**

No test changes needed - UserInput, ToolCallCompleted, UIControlAction stay

**If Option B (remove test-only types):**

**Files to update:**
- `TransparentAiAgentCore_Tests/Infrastructure/Transparency/TransparencyServiceTests.cs`
  - Lines 21, 36, 59, 80, 96, 98, 104, 118, 120, 122, 141, 167: Replace `UserInput` with `SystemState` or `Info`

- `TransparentAiAgentGui_Tests/Components/Transparency/TransparencyViewerTests.cs`
  - Line 96: Replace `UserInput` with `SystemState`
  - Line 161: Replace `ToolCallCompleted` with different event type (e.g., `ToolCall`)

**Run tests after changes:**
```bash
dotnet test TransparentAiAgentCore_Tests
dotnet test TransparentAiAgentGui_Tests
```

### Phase 1.4: Remove Unused Event Types

**File**: `TransparentAiAgentCore/Domain/Transparency/TransparencyEventType.cs`

**Types to REMOVE (15 types if Option A, 18 if Option B):**

Remove these lines from enum:
```csharp
ConfigurationChange,    // Line 10 - REMOVE
Warning,                // Line 13 - REMOVE
Debug,                  // Line 14 - REMOVE

// Tool discovery events (Phase 5)
ToolDiscoveryStarted,        // Line 18 - REMOVE
ToolDiscoveryCompleted,      // Line 19 - REMOVE
ToolDiscoveryFailed,         // Line 20 - REMOVE
ToolRegistered,              // Line 21 - REMOVE

// Tool execution events (Phase 5)
ToolCallStarted,        // Line 24 - REMOVE
ToolCallCompleted,      // Line 25 - REMOVE (if Option B)
ToolCallFailed,         // Line 26 - REMOVE
ToolCallTimeout,        // Line 27 - REMOVE

// MCP server lifecycle events (Phase 5)
MCPServerConnecting,         // Line 30 - REMOVE
MCPServerConnected,          // Line 31 - REMOVE
MCPServerDisconnected,       // Line 32 - REMOVE
MCPServerConnectionFailed,   // Line 33 - REMOVE

// UI Control events (Phase 9)
UIControlAction,        // Line 36 - REMOVE (if Option B)

// If Option B, also remove:
UserInput,              // Line 5 - REMOVE (if Option B)
```

**After removal:**
- Verify file compiles
- Check line spacing/formatting
- Keep comments for remaining sections

### Phase 1.5: Run All Tests

```bash
dotnet build
dotnet test
```

**Expected**: All tests pass, no compilation errors

### Phase 1.6: Manual Verification

1. Run the application
2. Open TransparencyViewer (via UI control or direct navigation)
3. Click event type dropdown
4. Verify only used types appear (12-15 types depending on option chosen)
5. Verify events display correctly

### Deliverables

- [ ] Duplicate enum file investigated (deleted if unused)
- [ ] Decision made on test-only types (Option A or B)
- [ ] Tests updated if needed
- [ ] TransparencyEventType.cs cleaned up
- [ ] All tests pass
- [ ] No compilation errors
- [ ] TransparencyViewer UI dropdown shows correct types
- [ ] Events display correctly in viewer

---

## Step 2: Export to JSON

**Goal**: Add "Save" button that exports all transparency events to JSON file with privacy warning

**Can be completed in**: Single session (~1-2 hours)

**Prerequisites**: Step 1 completed (optional but recommended)

### Phase 2.1: Add JavaScript Download Function (TDD - Manual Verification)

**File**: `TransparentAiAgentGui/wwwroot/js/site.js`

**Implementation** (add after existing functions):

```javascript
// File export utility for Transparency Viewer
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

**No automated tests** (JavaScript not testable in MSTest, Lean TDD principle)

**Manual verification**: Will test in Phase 2.4

### Phase 2.2: Create Export Service Method (TDD)

**Goal**: Add `ExportToJson()` method to TransparencyService with tests

**File**: `TransparentAiAgentCore/Infrastructure/Transparency/TransparencyService.cs`

**RED Phase:**

1. **Create test** in `TransparentAiAgentCore_Tests/Infrastructure/Transparency/TransparencyServiceTests.cs`:

```csharp
[TestMethod]
public void ExportToJson_WithEvents_ReturnsValidJsonStructure()
{
    // Arrange
    _service.LogEvent(new TransparencyEvent(TransparencyEventType.Info, "test data", "additional"));
    _service.LogEvent(new TransparencyEvent(TransparencyEventType.Error, "error data", null));

    // Act
    var json = _service.ExportToJson();

    // Assert
    Assert.IsNotNull(json);
    Assert.IsTrue(json.Contains("\"exportedAt\""));
    Assert.IsTrue(json.Contains("\"events\""));
    Assert.IsTrue(json.Contains("\"eventType\""));
    Assert.IsTrue(json.Contains("\"Info\""));
    Assert.IsTrue(json.Contains("\"Error\""));
}

[TestMethod]
public void ExportToJson_NoEvents_ReturnsEmptyEventsArray()
{
    // Arrange - no events logged

    // Act
    var json = _service.ExportToJson();

    // Assert
    Assert.IsNotNull(json);
    Assert.IsTrue(json.Contains("\"exportedAt\""));
    Assert.IsTrue(json.Contains("\"events\""));
    // Should have empty array (may be "events":[] or "events": [])
    Assert.IsTrue(json.Contains("\"events\": []") || json.Contains("\"events\":[]"));
}

[TestMethod]
public void ExportToJson_ReturnsIndentedJson()
{
    // Arrange
    _service.LogEvent(new TransparencyEvent(TransparencyEventType.Info, "test"));

    // Act
    var json = _service.ExportToJson();

    // Assert - indented JSON has newlines
    Assert.IsTrue(json.Contains("\n") || json.Contains(Environment.NewLine));
}
```

2. **Add minimal stub** to TransparencyService.cs:

```csharp
public string ExportToJson()
{
    throw new NotImplementedException();
}
```

3. **Run test** - should fail with NotImplementedException:
```bash
dotnet test TransparentAiAgentCore_Tests --filter "ExportToJson"
```

**Expected**: 3 tests fail with NotImplementedException

**GREEN Phase:**

4. **Implement ExportToJson** in TransparencyService.cs:

```csharp
using System.Text.Json;

// Add to TransparencyService class:
public string ExportToJson()
{
    lock (_lock)
    {
        var export = new
        {
            exportedAt = DateTime.UtcNow,
            events = _events.ToList()
        };

        return JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
```

5. **Add using directive** at top of file:
```csharp
using System.Text.Json;
```

6. **Run tests** - should pass:
```bash
dotnet test TransparentAiAgentCore_Tests --filter "ExportToJson"
```

**Expected**: All 3 tests pass

**REFACTOR Phase:**

7. **Review implementation**:
   - Thread-safe? ✅ (uses existing `_lock`)
   - Returns ALL events? ✅ (uses `_events.ToList()`, not limited)
   - Pretty JSON? ✅ (WriteIndented = true)
   - Includes timestamp? ✅ (exportedAt)

8. **Run all service tests**:
```bash
dotnet test TransparentAiAgentCore_Tests/Infrastructure/Transparency/TransparencyServiceTests.cs
```

**Expected**: All tests pass (existing + new)

### Phase 2.3: Add UI Components (Manual Testing)

**File**: `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`

**Step 2.3.1: Add Save button to UI**

Find the viewer controls section (lines 19-42) and add Save button:

```razor
<div class="viewer-controls">
    <input type="text"
           class="search-input"
           placeholder="Search events..."
           @bind="SearchQuery"
           @bind:event="oninput" />

    <select class="filter-select" @bind="SelectedEventType">
        <option value="">All Events</option>
        @foreach (TransparencyEventType eventType in Enum.GetValues<TransparencyEventType>())
        {
            <option value="@eventType">@eventType</option>
        }
    </select>

    <label class="auto-scroll-label">
        <input type="checkbox" @bind="AutoScroll" />
        Auto-scroll
    </label>

    <button class="btn btn-sm btn-outline-secondary" @onclick="ClearEvents">
        Clear
    </button>

    <!-- ADD THIS BUTTON -->
    <button class="btn btn-sm btn-primary" @onclick="HandleExportClick">
        Save
    </button>
</div>
```

**Step 2.3.2: Add export handlers in @code block**

Add these methods to the @code section (after line 175):

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
    try
    {
        var json = TransparencyService.ExportToJson();
        var filename = $"transparency-events-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
        await JSRuntime.InvokeVoidAsync("downloadFile", filename, json);
    }
    catch (Exception ex)
    {
        // Log error but don't crash UI
        Console.WriteLine($"Export failed: {ex.Message}");
    }
}
```

**Note**: ITransparencyService interface needs ExportToJson() method signature

**Step 2.3.3: Update ITransparencyService interface**

**File**: Find ITransparencyService interface file:
```bash
find . -name "ITransparencyService.cs"
```

Add method signature:
```csharp
string ExportToJson();
```

### Phase 2.4: Manual Validation

**Run the application** and test:

1. **Open TransparencyViewer**
2. **Log some events** (interact with the app)
3. **Click "Save" button**
4. **Verify confirmation dialog appears** with message: "Logs contain all message content. Ensure no sensitive information before exporting. Continue?"
5. **Click "Cancel"** → nothing happens (good)
6. **Click "Save" again**, then **"OK"** → browser download dialog appears
7. **Save the file** to disk
8. **Open JSON file** → verify structure:

```json
{
  "exportedAt": "2025-11-18T10:30:45.123Z",
  "events": [
    {
      "id": "guid-here",
      "timestamp": "2025-11-18T10:30:00.123Z",
      "eventType": "SystemState",
      "data": "...",
      "additionalInfo": "..."
    },
    ...
  ]
}
```

9. **Verify ALL events exported** (not limited to 1000 visible in UI)
   - Log >1000 events if possible, or check count in JSON vs UI count
10. **Test with empty events** → should export empty array

### Deliverables

- [ ] downloadFile() JavaScript function added to site.js
- [ ] TransparencyService.ExportToJson() implemented with tests passing
- [ ] ITransparencyService interface updated
- [ ] Save button added to TransparencyViewer UI
- [ ] Privacy confirmation dialog works correctly
- [ ] JSON export downloads successfully
- [ ] JSON file has correct structure (exportedAt + events array)
- [ ] ALL events exported (not limited to visible 1000)
- [ ] Pretty-printed JSON (indented, readable)
- [ ] All automated tests pass

---

## Step 3: Multi-Filter UI

**Goal**: Replace single event type dropdown with dynamic multi-filter (OR logic)

**Can be completed in**: Single session (~2-3 hours)

**Prerequisites**: Step 1 completed (recommended for cleaner event type list)

### Phase 3.1: Remove UI State Filter Logic

**File**: `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`

**Current code (lines 83-87):**
```csharp
// Apply UI state event type filters if any
if (_uiState.TransparencyViewer.EventTypeFilters.Any())
{
    filtered = filtered.Where(e =>
        _uiState.TransparencyViewer.EventTypeFilters.Contains(e.EventType.ToString()));
}
```

**Action**: DELETE these 5 lines

**Rationale**: Per discussion doc, UI state filters are for chat component, not TransparencyViewer

**Verification**:
1. Remove the lines
2. Build the project
3. If compilation errors occur → investigate what depends on this
4. If chat component tests fail → revert and investigate further
5. Run all tests to ensure nothing breaks

**IMPORTANT**: Only remove if chat component doesn't actually use this. Verify first!

```bash
# Check if TransparencyViewerState.EventTypeFilters is used elsewhere
grep -r "EventTypeFilters" TransparentAiAgentCore/ TransparentAiAgentGui/ --include="*.cs"
```

### Phase 3.2: Update Component State (No Tests - UI Logic)

**File**: `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`

**Current state (line 72):**
```csharp
private string SelectedEventType { get; set; } = string.Empty;
```

**Replace with:**
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

### Phase 3.3: Update Filtering Logic (No Tests - Simple OR Logic)

**File**: `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`

**Current filtering logic (lines 89-95):**
```csharp
// Apply user search/filter
filtered = filtered.Where(e =>
    (string.IsNullOrEmpty(SelectedEventType) || e.EventType.ToString() == SelectedEventType) &&
    (string.IsNullOrEmpty(SearchQuery) ||
     e.EventType.ToString().Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
     e.AdditionalInfo?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true ||
     e.Data?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true));
```

**Replace with:**
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

// Apply search filter
filtered = filtered.Where(e =>
    string.IsNullOrEmpty(SearchQuery) ||
    e.EventType.ToString().Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
    e.AdditionalInfo?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true ||
    e.Data?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true);
```

### Phase 3.4: Add Filter Management Methods

**File**: `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`

Add these methods to @code section:

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

### Phase 3.5: Update UI Markup

**File**: `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`

**Current dropdown (lines 26-32):**
```razor
<select class="filter-select" @bind="SelectedEventType">
    <option value="">All Events</option>
    @foreach (TransparencyEventType eventType in Enum.GetValues<TransparencyEventType>())
    {
        <option value="@eventType">@eventType</option>
    }
</select>
```

**Replace with:**
```razor
<div class="event-filters">
    @foreach (var filter in EventTypeFilters)
    {
        <div class="filter-item">
            <select @bind="filter.SelectedType" class="filter-select">
                <option value="">All Events</option>
                @foreach (TransparencyEventType eventType in Enum.GetValues<TransparencyEventType>())
                {
                    <option value="@eventType">@eventType</option>
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

    <button @onclick="AddFilter" class="btn-add-filter">
        + Add Filter
    </button>
</div>
```

### Phase 3.6: Add CSS Styling

**File**: `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor.css`

Check if this file exists:
```bash
find . -name "TransparencyViewer.razor.css"
```

**If file exists**, add these styles:

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

**If file doesn't exist**, create it in the same directory as TransparencyViewer.razor

### Phase 3.7: Update Existing Tests

**File**: `TransparentAiAgentGui_Tests/Components/Transparency/TransparencyViewerTests.cs`

**Test that needs updating**: Line 155-185 - `TransparencyViewer_WithEventTypeFilters_FiltersEvents`

This test currently tests UI state filters which we removed. Options:

**Option A - Update test to verify new multi-filter UI:**

Replace test with:
```csharp
[TestMethod]
public void TransparencyViewer_WithMultipleFilters_ShowsEventsMatchingAny()
{
    // This test would be complex because it requires Blazor component interaction
    // Lean TDD principle: Skip complex UI tests, use manual testing instead
    // Mark test as Inconclusive or delete
}
```

**Option B - Delete the test:**

Delete lines 152-185

**RECOMMENDATION**: Option B (delete test)
- Lean TDD: Don't test complex UI interaction
- Multi-filter is simple OR logic, manual testing sufficient
- Test was testing UI state filters which we removed

### Phase 3.8: Manual Validation

**Run the application** and thoroughly test:

**Test 1: Default State**
- [ ] TransparencyViewer opens
- [ ] One filter dropdown visible
- [ ] "+ Add Filter" button visible
- [ ] No remove (✕) button on single filter

**Test 2: Adding Filters**
- [ ] Click "+ Add Filter"
- [ ] Second dropdown appears
- [ ] Both dropdowns now have remove (✕) button
- [ ] Click "+ Add Filter" again
- [ ] Third dropdown appears
- [ ] All three have remove button

**Test 3: Removing Filters**
- [ ] Click ✕ on second filter → it disappears
- [ ] Two filters remain
- [ ] Click ✕ on first filter → it disappears
- [ ] One filter remains
- [ ] Remove button disappears (can't remove last filter)
- [ ] Click "+ Add Filter" → can add again

**Test 4: OR Logic (Most Important)**
- [ ] Log events of multiple types (e.g., Info, Error, SystemState)
- [ ] Filter 1: Select "Info"
- [ ] Verify ONLY Info events shown
- [ ] Click "+ Add Filter"
- [ ] Filter 2: Select "Error"
- [ ] Verify BOTH Info AND Error events shown (OR logic)
- [ ] Filter 3: Add and select "SystemState"
- [ ] Verify Info, Error, AND SystemState events shown

**Test 5: Empty Filters Ignored**
- [ ] Filter 1: Select "Info"
- [ ] Filter 2: Leave as "All Events"
- [ ] Verify ONLY Info events shown (empty filter ignored)

**Test 6: All Filters Empty**
- [ ] Set all filters to "All Events"
- [ ] Verify ALL events shown

**Test 7: Search Box Interaction**
- [ ] Filter 1: Select "Info"
- [ ] Type text in search box
- [ ] Verify filters AND search both apply (Info events matching search text)
- [ ] Clear search
- [ ] Verify filter still works

**Test 8: Event Stats**
- [ ] Verify "Total" count shows all events
- [ ] Verify "Filtered" count updates correctly with filters
- [ ] Add/remove filters → counts update correctly

**Test 9: UI State Filters Removed**
- [ ] Verify chat component still works (if it exists)
- [ ] Verify no errors in browser console
- [ ] Verify EventTypeFilters removal didn't break anything

### Deliverables

- [ ] UI state filter logic removed (if safe to do so)
- [ ] Component state updated to multi-filter structure
- [ ] Filtering logic implements OR (show events matching ANY filter)
- [ ] Add/remove filter methods implemented
- [ ] UI markup updated with dynamic filters
- [ ] CSS styling added
- [ ] Old test deleted or updated
- [ ] All automated tests pass
- [ ] Manual testing confirms all scenarios work correctly
- [ ] No regressions in existing functionality

---

## Testing Strategy

### Automated Tests (Following Lean TDD)

**What to test:**
- ✅ Step 2: JSON serialization logic (business rule)
- ❌ Step 1: No new logic to test (deletion only)
- ❌ Step 3: Skip UI interaction tests (too complex, manually verifiable)

**What NOT to test:**
- UI component rendering → Framework feature
- Simple OR filtering → No complex business rules
- JavaScript download → Can't test in MSTest
- CSS styling → Visual, not logical
- Blazor data binding → Framework feature

### Manual Testing Priority

Each step has critical manual testing:
- **Step 1**: Verify dropdown shows correct types
- **Step 2**: Test entire export flow (button → dialog → download → JSON structure)
- **Step 3**: Extensive multi-filter testing (9 test scenarios)

---

## Implementation Notes

### Thread Safety

All steps preserve existing thread safety:
- TransparencyService uses locks
- UI updates on UI thread (Blazor handles this)
- Export creates snapshot within lock

### Performance Considerations

**Step 2 - Export:**
- Large exports (>10MB) might be slow
- Acceptable for MVP (transparency logs rarely this large)
- Could add progress indicator in future

**Step 3 - Multi-Filter:**
- Filtering recalculated on each state change
- Current implementation efficient (simple LINQ)
- No performance concerns for typical event counts (<10K)

### Browser Compatibility

**Step 2 - JavaScript Download:**
- Blob API supported in all modern browsers
- Object URL creation standard
- Browser must have JavaScript enabled (Blazor requires this anyway)

### Known Limitations

1. **Export size**: No chunking for very large exports
2. **Multi-filter UI**: No drag-to-reorder filters
3. **No filter presets**: Can't save common filter combinations

All acceptable for MVP, can enhance later if needed.

---

## Risk Assessment

### Step 1: Event Type Cleanup

**Risks:**
- Low: Accidentally removing used type
- Low: Breaking external code that references removed types

**Mitigation:**
- Verified usage with grep searches
- All tests pass before committing
- Compilation catches references

### Step 2: Export to JSON

**Risks:**
- Medium: Large exports crash browser
- Low: Privacy - user exports sensitive data
- Low: JavaScript not loaded when clicked

**Mitigation:**
- Document limitation (acceptable for MVP)
- Privacy warning dialog
- Blazor ensures JS loaded before interaction

### Step 3: Multi-Filter UI

**Risks:**
- Medium: Complex state management introduces bugs
- Medium: Removing UI state filters breaks chat component
- Low: Performance with many filters

**Mitigation:**
- Keep logic simple (basic List, simple OR)
- Thorough manual testing
- Verify chat component before removing
- LINQ efficient for typical event counts

---

## Session Boundaries

Each step designed for independent sessions:

### Session 1: Event Type Cleanup
**Time**: 45-60 minutes
**Deliverable**: Cleaner enum with only used types
**Blocking**: None (independent)
**Risk**: Low

### Session 2: Export to JSON
**Time**: 1-2 hours
**Deliverable**: Working Save button with JSON export
**Blocking**: None (independent feature)
**Risk**: Low-Medium
**Note**: Step 1 recommended first (cleaner event types in export)

### Session 3: Multi-Filter UI
**Time**: 2-3 hours
**Deliverable**: Dynamic multi-filter UI with OR logic
**Blocking**: None
**Risk**: Medium
**Note**: Step 1 recommended first (fewer event types = cleaner UI, easier testing)

**Total estimated time**: 4.5-6.5 hours across 3 sessions

---

## Success Criteria

### Step 1: Event Type Cleanup

- [ ] Duplicate enum file investigated and handled
- [ ] Decision made on test-only types
- [ ] 15-18 unused types removed from enum
- [ ] All tests pass
- [ ] No compilation errors
- [ ] UI dropdown shows only used types
- [ ] No regressions

### Step 2: Export to JSON

- [ ] JavaScript downloadFile() function works
- [ ] TransparencyService.ExportToJson() implemented
- [ ] All export tests pass (3 new tests)
- [ ] Save button appears in UI
- [ ] Privacy warning dialog works
- [ ] JSON downloads with correct structure
- [ ] ALL events exported (not limited to 1000)
- [ ] Pretty-printed JSON
- [ ] No exceptions during export

### Step 3: Multi-Filter UI

- [ ] UI state filter logic safely removed
- [ ] Multiple filters can be added/removed
- [ ] OR logic works correctly (events matching ANY filter shown)
- [ ] At least one filter always remains
- [ ] Empty filters ignored
- [ ] Works correctly with search box
- [ ] Event counts update correctly
- [ ] UI is clean and intuitive
- [ ] CSS styling applied
- [ ] All automated tests pass
- [ ] All 9 manual test scenarios pass
- [ ] No regressions

---

## Open Questions for Implementer

1. **Step 1.1**: Keep test-only types (Option A) or remove them (Option B)?
2. **Step 1.2**: Should duplicate enum file in Domain/Enums/ be deleted?
3. **Step 3.1**: Is UI state filter logic actually used by chat component? Verify before removing!
4. **Step 3.7**: Delete old test or mark inconclusive?

---

## Related Documentation

- [TRANSPARENCY_IMPROVEMENTS_DISCUSSION.md](TRANSPARENCY_IMPROVEMENTS_DISCUSSION.md) - Full context
- [.claude/skills/tdd/SKILL.md](.claude/skills/tdd/SKILL.md) - TDD workflow
- [docs/04-components/infrastructure/transparency-service.md](docs/04-components/infrastructure/transparency-service.md) - Component docs (if exists)
- [docs/04-components/ui/transparency-viewer.md](docs/04-components/ui/transparency-viewer.md) - UI docs (if exists)

---

**Plan Status**: ✅ Verified against actual codebase

**Code Investigation Date**: 2025-11-18

**Confidence Level**: High - All file paths, line numbers, and code references verified against actual source code
