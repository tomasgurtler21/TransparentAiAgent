# UI Overlay System Refactor Plan

**Created:** 2025-11-08
**Status:** Planning
**Related to:** Phase 9C - Teaching Mode UI Control
**Issue:** UI tools don't work because components only exist on specific pages

---

## 🎯 Problem Statement

**Current Issue:**
- Agent calls `ui_control_transparency_viewer(visible: true)` from Home page (`/`)
- Tool executes successfully, UIState updates, events fire
- **BUT** TransparencyViewer component only exists on `/transparency` page
- Result: Nothing happens in UI, user sees no change

**Root Cause:**
Page-based component architecture doesn't match agent's mental model of "show this UI element from anywhere"

---

## 💡 Solution: Modal/Overlay System (Option 3)

### Core Concept

Render controllable UI components as **floating overlays/panels** in MainLayout:
- Always present in DOM
- Hidden by default (CSS transforms)
- Slide in/out when agent controls them
- Don't disrupt page content
- Available globally on all pages

### Why This Solution?

1. **Teaching Mode is fundamentally about overlays**
   - Agent explains → Panel overlays current view
   - User maintains context (no page navigation)

2. **Matches Use Cases**
   ```
   "Let me show you the transparency events while you look at this conversation"
   → TransparencyViewer slides in from right
   → Chat stays visible on left
   ```

3. **Performance is acceptable**
   - Blazor doesn't re-render hidden components
   - Minimal DOM overhead (~100-200 nodes per component)
   - Modern browsers handle `position: fixed` well

4. **Scalable**
   - Easy to add more overlay panels
   - Can stack multiple overlays
   - Clean separation of concerns

---

## 📋 Implementation Plan

### Phase 1: Infrastructure Setup
- [ ] Create overlay container system in MainLayout
- [ ] Add CSS for overlay panel positioning and animations
- [ ] Define z-index hierarchy for stacking
- [ ] Add responsive breakpoints for mobile

### Phase 2: Move Components to Overlays
- [ ] Move TransparencyViewer to MainLayout as overlay
- [ ] Update TransparencyViewer styles for overlay mode
- [ ] Test visibility toggling from different pages
- [ ] Verify event subscriptions still work

### Phase 3: Additional Components (Future)
- [ ] Consider ToolsPanel as overlay
- [ ] MessageFilterControls (keep inline or overlay?)
- [ ] Context indicators positioning

### Phase 4: Polish
- [ ] Smooth slide-in/out animations
- [ ] Handle overlay stacking (multiple visible)
- [ ] Mobile responsiveness
- [ ] Accessibility (focus management, ESC to close)

---

## 🔍 Current Codebase Research

### CSS Architecture

**Current Approach:**
- Uses Blazor **scoped CSS** (`.razor.css` files)
- Each component has isolated styles
- Bootstrap 5 for base styling
- Global styles in `wwwroot/app.css`

**Z-Index Hierarchy:**
```css
MainLayout.razor.css:
  .top-row: z-index: 1  (sticky header)

app.css:
  #blazor-error-ui: z-index: 1000  (error banner)

ToolDetailsModal.razor.css:
  .modal-backdrop: z-index: 1000  (existing modal)
```

**Responsive Breakpoints:**
```css
@media (max-width: 640.98px)  /* Mobile */
@media (min-width: 641px)      /* Desktop */
```

### Existing Modal Pattern

**File:** `ToolDetailsModal.razor` (already implemented)

**Structure:**
```html
<div class="modal-backdrop">  <!-- Fullscreen overlay, rgba(0,0,0,0.5) -->
  <div class="modal-content">  <!-- Centered modal, max-width: 800px -->
    <div class="modal-header">
      <h3>Title</h3>
      <button class="close-button">×</button>
    </div>
    <div class="modal-body">
      Content...
    </div>
  </div>
</div>
```

**Key Features:**
- Fixed positioning (`position: fixed`)
- Click backdrop to close (`@onclick="OnClose"`)
- Stop propagation on content (`@onclick:stopPropagation`)
- Smooth scrolling (`overflow-y: auto`)

### TransparencyViewer Current Structure

**File:** `TransparencyViewer.razor`

**Current Layout:**
- Full-height flex container (`height: 100%`)
- Three sections: header, stats, content
- Border/rounded corners design
- Scrollable content area

**CSS Characteristics:**
- Designed for full-page display (not overlay-ready)
- No fixed positioning
- Expects parent container for sizing

### Layout Structure

**MainLayout Structure:**
```
.page (flex container)
  ├─ .sidebar (250px wide, sticky)
  ├─ main
  │   ├─ .top-row (sticky header)
  │   └─ article.content (@Body - page content)
  └─ #blazor-error-ui
```

**Sidebar:**
- Desktop: 250px wide, 100vh height, sticky
- Mobile: Full width (flex-direction: column)

---

## 🎨 Design Specifications

### Overlay Panel Design

**Visual Design:**
```
┌────────────────────────────────────────┐
│  Main Content Area                     │ ← Page content (chat, config, etc.)
│                                        │
│                  ┌─────────────────────┤
│                  │ TransparencyViewer  │ ← Overlay (slides from right)
│                  │ [Header]            │
│                  │ ─────────────────── │
│                  │ [Stats]             │
│                  │ ─────────────────── │
│                  │ [Content - scroll]  │
│                  │                     │
│                  │                     │
└──────────────────┴─────────────────────┘
```

**Overlay Characteristics:**
- Position: Fixed right side
- Width: 400px (desktop), 90vw (mobile)
- Height: 100vh
- Background: Semi-transparent backdrop optional
- Animation: Slide in/out (transform: translateX)
- Shadow: `box-shadow: -2px 0 10px rgba(0,0,0,0.1)`

### Z-Index Strategy

```css
/* Proposed z-index hierarchy */
.page: z-index: 0              /* Base layer */
.top-row: z-index: 10          /* Sticky header (increased from 1) */
.overlay-panel: z-index: 100   /* Overlay panels */
.modal-backdrop: z-index: 1000 /* Modals (keep existing) */
#blazor-error-ui: z-index: 2000 /* Error UI (increased)  */
```

**Rationale:**
- Overlays sit above page content but below modals
- Multiple overlays can stack (z-index: 100, 101, 102...)
- Errors always on top

### Animation Specifications

**Slide In/Out:**
```css
.overlay-panel {
  transition: transform 0.3s ease-in-out;
  transform: translateX(100%); /* Hidden: off-screen right */
}

.overlay-panel.visible {
  transform: translateX(0); /* Visible: on-screen */
}
```

**Alternative (with backdrop fade):**
```css
.overlay-backdrop {
  opacity: 0;
  transition: opacity 0.3s ease-in-out;
  pointer-events: none;
}

.overlay-backdrop.visible {
  opacity: 1;
  pointer-events: auto;
}
```

---

## 🔨 Detailed Implementation Steps

### Step 1: Create Overlay Infrastructure

**File:** `MainLayout.razor.css`

Add new styles:
```css
/* Overlay System */
.overlay-container {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  pointer-events: none; /* Allow clicks through when no overlays */
  z-index: 100;
}

.overlay-panel {
  position: fixed;
  top: 0;
  right: 0;
  height: 100vh;
  background: white;
  box-shadow: -2px 0 10px rgba(0, 0, 0, 0.1);
  transition: transform 0.3s ease-in-out;
  pointer-events: auto; /* Capture clicks on panel */
  overflow-y: auto;
}

.overlay-panel-right {
  width: 400px;
  transform: translateX(100%);
}

.overlay-panel-right.visible {
  transform: translateX(0);
}

/* Responsive */
@media (max-width: 640.98px) {
  .overlay-panel-right {
    width: 90vw;
  }
}

/* Optional backdrop */
.overlay-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.3);
  opacity: 0;
  transition: opacity 0.3s ease-in-out;
  pointer-events: none;
  z-index: 99; /* Below overlay panels */
}

.overlay-backdrop.visible {
  opacity: 1;
  pointer-events: auto;
}
```

**File:** `MainLayout.razor`

Add overlay container:
```razor
@using TransparentAiAgentGui.Components.Transparency
@inherits LayoutComponentBase

<div class="page">
  <div class="sidebar">
    <NavMenu />
  </div>

  <main>
    <div class="top-row px-4">
      <a href="https://learn.microsoft.com/aspnet/core/" target="_blank">About</a>
    </div>

    <article class="content px-4">
      @Body
    </article>
  </main>

  <!-- NEW: Overlay System -->
  <div class="overlay-container">
    <!-- Optional backdrop (comment out if not needed) -->
    @* <div class="overlay-backdrop @(IsAnyOverlayVisible() ? "visible" : "")" @onclick="CloseAllOverlays"></div> *@

    <!-- TransparencyViewer as overlay -->
    <div class="overlay-panel overlay-panel-right @(GetOverlayVisibilityClass())">
      <TransparencyViewer />
    </div>
  </div>
</div>

<div id="blazor-error-ui">
  An unhandled error has occurred.
  <a href="" class="reload">Reload</a>
  <a class="dismiss">🗙</a>
</div>

@code {
  // Will be implemented in Step 2
}
```

### Step 2: Make MainLayout Reactive to UIState

**File:** `MainLayout.razor` (add code block)

```csharp
@using TransparentAiAgentCore.Domain.UIControl
@inject IUIControlService UIControlService
@implements IDisposable

@code {
  private UIState _uiState = UIState.DefaultNormalMode();

  protected override void OnInitialized()
  {
    UIControlService.UIStateChanged += OnUIStateChanged;
    _uiState = UIControlService.GetCurrentState();
  }

  private void OnUIStateChanged(object? sender, UIState newState)
  {
    InvokeAsync(() =>
    {
      _uiState = newState;
      StateHasChanged();
    });
  }

  private string GetOverlayVisibilityClass()
  {
    return _uiState.TransparencyViewer.Visible ? "visible" : "";
  }

  public void Dispose()
  {
    UIControlService.UIStateChanged -= OnUIStateChanged;
  }
}
```

### Step 3: Refactor TransparencyViewer for Overlay Mode

**File:** `TransparencyViewer.razor.css`

Update styles:
```css
/* Remove height: 100% - overlay handles sizing now */
.transparency-viewer {
  display: flex;
  flex-direction: column;
  min-height: 100vh; /* NEW: Fill overlay height */
  border: none;      /* NEW: No border in overlay mode */
  border-radius: 0;  /* NEW: No rounded corners in overlay mode */
  background-color: #ffffff;
}

/* Add close button area (optional) */
.viewer-header {
  padding: 16px;
  border-bottom: 1px solid #dee2e6;
  background-color: #f8f9fa;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.viewer-close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #6c757d;
}

.viewer-close-btn:hover {
  color: #212529;
}

/* Rest of styles remain unchanged */
```

**File:** `TransparencyViewer.razor`

Add close button (optional):
```razor
<div class="viewer-header">
  <h3>Transparency Events</h3>
  <div class="viewer-controls">
    <!-- existing controls -->
  </div>
  <button class="viewer-close-btn" @onclick="HandleClose">×</button>
</div>

@code {
  private async Task HandleClose()
  {
    // Close via UIControlService
    await Task.Run(() =>
      UIControlService.UpdateTransparencyViewer(visible: false));
  }
}
```

### Step 4: Remove TransparencyViewer from Transparency.razor Page

**File:** `Components/Pages/Transparency.razor`

**BEFORE:**
```razor
@page "/transparency"
@using TransparentAiAgentGui.Components.Transparency

<PageTitle>Transparency Events</PageTitle>

<h1>Transparency Events</h1>

<div class="transparency-page">
    <TransparencyViewer />
</div>
```

**AFTER:**
```razor
@page "/transparency"
@using TransparentAiAgentCore.Domain.UIControl
@inject IUIControlService UIControlService
@implements IDisposable

<PageTitle>Transparency Events</PageTitle>

<h1>Transparency Events</h1>

<div class="transparency-page">
    <p>The transparency viewer is now available as an overlay on all pages.</p>
    <button class="btn btn-primary" @onclick="ShowViewer">Show Transparency Viewer</button>

    @* Alternative: Auto-show when navigating to this page *@
</div>

@code {
    protected override void OnInitialized()
    {
        // Auto-show viewer when user navigates to transparency page
        UIControlService.UpdateTransparencyViewer(visible: true);
    }

    private void ShowViewer()
    {
        UIControlService.UpdateTransparencyViewer(visible: true);
    }

    public void Dispose()
    {
        // Optional: Auto-hide when leaving page
        // UIControlService.UpdateTransparencyViewer(visible: false);
    }
}
```

### Step 5: Testing Plan

**Test Cases:**

1. **Visibility Toggle from Home Page**
   - Navigate to `/` (Home)
   - Agent calls `ui_control_transparency_viewer(visible: true)`
   - Expected: Panel slides in from right

2. **Visibility Toggle from Different Pages**
   - Test from `/tools`, `/configuration`, `/transparency`
   - Expected: Consistent behavior

3. **Event Subscription**
   - Verify UIStateChanged events fire
   - Verify TransparencyViewer updates when state changes

4. **Multiple Overlays** (Future)
   - Test with multiple panels visible
   - Verify z-index stacking works

5. **Responsive Design**
   - Test on mobile (< 641px)
   - Verify overlay width adapts (90vw)

6. **Accessibility**
   - Test keyboard navigation (Tab, Esc)
   - Test screen reader announcements

---

## 🚧 Known Limitations & Future Enhancements

### Current Limitations

1. **Single Overlay Support**
   - Only TransparencyViewer is overlay-ready
   - Other components (ToolsPanel, etc.) need similar refactoring

2. **No Backdrop**
   - Backdrop is optional (commented out)
   - May want to add for better focus management

3. **No Animation for Initial Render**
   - First render doesn't animate (appears immediately)
   - Could add delay or initial animation

### Future Enhancements

1. **Multi-Overlay Management**
   - Service to manage overlay stack
   - Auto-positioning (left, right, top)
   - Prevent overlaps

2. **Accessibility Improvements**
   - ESC key to close
   - Focus trap (keyboard navigation stays in overlay)
   - ARIA attributes (`role="dialog"`, `aria-modal`)

3. **Resize Handle**
   - Allow user to resize overlay width
   - Remember width preference (localStorage)

4. **Minimize/Maximize**
   - Minimize to floating button
   - Maximize to full-screen overlay

5. **Overlay State Persistence**
   - Remember which overlays were open
   - Restore on page reload (optional)

---

## 📝 Migration Checklist

### Pre-Implementation
- [ ] Review this plan with team
- [ ] Decide on backdrop (yes/no)
- [ ] Decide on close button placement
- [ ] Choose z-index values

### Implementation
- [ ] **Step 1:** Add overlay CSS to MainLayout.razor.css
- [ ] **Step 1:** Update MainLayout.razor markup
- [ ] **Step 2:** Add UIControlService injection to MainLayout
- [ ] **Step 2:** Implement UIStateChanged handler
- [ ] **Step 3:** Update TransparencyViewer.razor.css
- [ ] **Step 3:** (Optional) Add close button to TransparencyViewer
- [ ] **Step 4:** Refactor Transparency.razor page
- [ ] **Step 5:** Manual testing (all test cases)

### Post-Implementation
- [ ] Update documentation (component docs)
- [ ] Update Phase 9C roadmap (mark as complete)
- [ ] Create GitHub issue for future overlays (ToolsPanel, etc.)

---

## 🎓 Learning Resources

### Blazor Scoped CSS
- Scoped CSS applies only to component (automatic namespacing)
- Use `::deep` to style child components

### CSS Transforms vs Display:none
- `transform: translateX(100%)` keeps element in DOM (accessible)
- Better for animations than `display: none`
- Blazor still skips re-rendering if `@if (visible)` is false

### Z-Index Best Practices
- Use logical layers (10, 100, 1000)
- Leave room between layers for future additions
- Document z-index hierarchy

---

## 📞 Questions / Decisions Needed

1. **Backdrop:** Include semi-transparent backdrop behind overlays?
   - Pro: Better focus, clearer separation
   - Con: Darker UI, may feel modal-like

2. **Close Button:** Where to place?
   - Option A: Inside TransparencyViewer header
   - Option B: Outside (top-right of overlay panel)
   - Option C: Both (redundant but clear)

3. **Auto-Show on Navigation:** Should `/transparency` page auto-show overlay?
   - Pro: Backward compatible (page still "shows" transparency)
   - Con: Confusing (page and overlay are decoupled)

4. **Animation Duration:** 0.3s good balance?
   - Faster (0.2s): Snappier
   - Slower (0.4s): Smoother

---

## 🧪 Unit Test Impact Analysis

### Existing Tests That Will Be Affected

**Test File:** `TransparentAiAgentGui_Tests/Components/Transparency/TransparencyViewerTests.cs`

**Current Test Count:** 6 tests
- `TransparencyViewer_WhenVisibleFalse_HidesViewer()`
- `TransparencyViewer_WhenVisibleTrue_ShowsViewer()`
- `TransparencyViewer_WhenShowTimestampsFalse_HidesTimestamps()`
- `TransparencyViewer_WhenShowTimestampsTrue_ShowsTimestamps()`
- `TransparencyViewer_WithEventTypeFilters_FiltersEvents()`
- `TransparencyViewer_OnUIStateChanged_UpdatesDisplay()`

### Impact Assessment: ✅ **ZERO BREAKING CHANGES**

**Why? The tests are testing the RIGHT thing!**

All tests use **mocked services** and test **component behavior**, NOT component location:
```csharp
// Tests check BEHAVIOR:
Assert.IsTrue(cut.Markup.Contains("transparency-viewer"));  // Component renders
Assert.IsFalse(cut.Markup.Contains("event-timestamp"));     // Conditional rendering

// Tests DON'T check:
// - Which page the component is on
// - Component position/layout
// - Parent container structure
```

### What Changes in the Refactor?

| Aspect | Before | After | Test Impact |
|--------|--------|-------|-------------|
| **Component Logic** | Same | Same | ✅ No change |
| **UIState Subscription** | Same | Same | ✅ No change |
| **Visibility Logic** | `@if (_uiState.Visible)` | `@if (_uiState.Visible)` | ✅ No change |
| **Event Filtering** | Same | Same | ✅ No change |
| **Timestamp Toggle** | Same | Same | ✅ No change |
| **Parent Container** | `<div class="transparency-page">` | `<div class="overlay-panel">` | ⚠️ Different, but **NOT tested** |
| **CSS Classes** | `.transparency-viewer` | `.transparency-viewer` | ✅ No change |

### Tests That Continue to Pass (No Changes Needed)

**All 6 tests will continue to pass** because:

1. ✅ **Tests are isolated** - Use bUnit to render component standalone
2. ✅ **Tests use mocks** - No dependency on MainLayout or routing
3. ✅ **Tests check markup** - Component still generates same HTML
4. ✅ **Tests check behavior** - Visibility, filtering, timestamps still work the same

### Example Test (No Changes Needed):

```csharp
[TestMethod]
public void TransparencyViewer_WhenVisibleFalse_HidesViewer()
{
    // Arrange
    UIState hiddenState = UIState.DefaultNormalMode() with
    {
        TransparencyViewer = new TransparencyViewerState { Visible = false }
    };
    _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(hiddenState);

    // Act
    IRenderedComponent<TransparencyViewer> cut = RenderComponent<TransparencyViewer>();

    // Assert
    Assert.IsFalse(cut.Markup.Contains("transparency-viewer"));
}
// ✅ This test will STILL PASS after refactor
// Why? Component logic unchanged, just parent container changed
```

### Other Test Files (No Impact)

**UIControlServiceTests.cs** (28 tests)
- ✅ Tests service methods (UpdateChatFilter, UpdateTransparencyViewer, etc.)
- ✅ No dependency on component rendering or layout
- ✅ **Zero changes needed**

**UIControlToolExecutorTests.cs** (7 tests)
- ✅ Tests tool execution logic
- ✅ No dependency on UI components
- ✅ **Zero changes needed**

**UIStateTests.cs** (tests)
- ✅ Tests domain models
- ✅ **Zero changes needed**

### New Tests to Add (Optional)

If you want to test the overlay system itself:

**Test File:** `TransparentAiAgentGui_Tests/Components/Layout/MainLayoutTests.cs` (NEW)

```csharp
[TestClass]
public class MainLayoutTests : Bunit.TestContext
{
    [TestMethod]
    public void MainLayout_WhenTransparencyViewerVisible_AppliesVisibleClass()
    {
        // Arrange
        var mockUIControl = new Mock<IUIControlService>();
        mockUIControl.Setup(s => s.GetCurrentState())
            .Returns(UIState.DefaultNormalMode() with
            {
                TransparencyViewer = new() { Visible = true }
            });
        Services.AddSingleton(mockUIControl.Object);

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        Assert.IsTrue(cut.Markup.Contains("overlay-panel-right visible"));
    }
}
```

But this is **optional** - not required for the refactor to work.

### Summary

**Test Impact:**
- ✅ **0 tests break**
- ✅ **0 tests need updates**
- ✅ **0 new tests required** (but optional to add MainLayout tests)

**Why This Works:**
The tests are well-designed! They test **component behavior** (what matters), not **component location** (implementation detail).

This is a perfect example of why testing behavior > testing implementation details.

---

## 🌐 Visual Examples Reference

For visualization, see these real-world examples:

### Most Similar: **Slack Thread Panel**
- Right-side overlay
- Slides in/out
- ~400px width
- Main content stays visible
- Example: https://slack.com (click any message thread)

### Also Similar: **GitHub File Preview**
- Right-side panel
- Smooth slide animation
- Semi-transparent backdrop
- Example: https://github.com (click any file in repository)

### Mobile Pattern: **Gmail Compose**
- Bottom overlay (desktop: corner, mobile: full-width)
- Multiple overlays can stack
- Minimize/maximize
- Example: https://mail.google.com (click Compose)

**Our Implementation = Slack Thread Panel + GitHub File Preview**

---

**End of Plan**

