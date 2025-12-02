# UI Overlay System - User Controls

**Purpose:** Document how users control overlays (independent of AI teaching mode)

---

## 🎮 User Control Philosophy

**Core Principle:**
> Users must ALWAYS be in control. AI can suggest/show overlays, but users can override at any time.

**Control Methods:**
1. ✅ **Close Button (X)** - On every overlay
2. ✅ **Navigation Menu** - Toggle from sidebar
3. ✅ **Page Buttons** - Show/hide from relevant pages
4. ✅ **Keyboard Shortcuts** - ESC to close, Ctrl+T to toggle
5. ✅ **Click Outside** - Optional backdrop click to close

---

## 🔨 Implementation: User Controls

### 1. Close Button on Overlay (Required)

**File:** `TransparencyViewer.razor`

**Change:**
```razor
<div class="viewer-header">
  <h3>Transparency Events</h3>

  <div class="viewer-controls">
    <!-- Existing controls -->
    <input type="text" class="search-input"
           placeholder="Search events..."
           @bind="SearchQuery" @bind:event="oninput" />

    <select class="filter-select" @bind="SelectedEventType">
      <option value="">All Events</option>
      @foreach (TransparencyEventType eventType in Enum.GetValues<TransparencyEventType>())
      {
        <option value="@eventType">@eventType</option>
      }
    </select>
  </div>

  <!-- NEW: User close button -->
  <button class="viewer-close-btn"
          @onclick="HandleUserClose"
          title="Close transparency viewer (ESC)"
          aria-label="Close">
    ×
  </button>
</div>

@code {
    // ... existing code ...

    private void HandleUserClose()
    {
        // User manually closes overlay
        UIControlService.UpdateTransparencyViewer(visible: false);
    }
}
```

**CSS:** `TransparencyViewer.razor.css`

```css
.viewer-header {
  padding: 16px;
  border-bottom: 1px solid #dee2e6;
  background-color: #f8f9fa;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
}

.viewer-close-btn {
  background: none;
  border: none;
  font-size: 28px;
  line-height: 1;
  cursor: pointer;
  color: #6c757d;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: all 0.2s;
  flex-shrink: 0; /* Don't shrink when space is tight */
}

.viewer-close-btn:hover {
  color: #212529;
  background-color: #e9ecef;
}

.viewer-close-btn:active {
  background-color: #dee2e6;
}
```

---

### 2. Navigation Menu Toggle (Optional but Recommended)

**File:** `NavMenu.razor`

**Add:**
```razor
@using TransparentAiAgentCore.Domain.UIControl
@inject IUIControlService UIControlService
@implements IDisposable

<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
    <nav class="flex-column">
        <!-- Existing nav items -->
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="">
                <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
            </NavLink>
        </div>

        <div class="nav-item px-3">
            <NavLink class="nav-link" href="tools">
                <span class="bi bi-tools" aria-hidden="true"></span> Tools
            </NavLink>
        </div>

        <div class="nav-item px-3">
            <NavLink class="nav-link" href="configuration">
                <span class="bi bi-gear-fill" aria-hidden="true"></span> Configuration
            </NavLink>
        </div>

        <!-- NEW: Transparency Viewer Toggle -->
        <div class="nav-item px-3">
            <a class="nav-link" @onclick="ToggleTransparencyViewer" role="button" style="cursor: pointer;">
                <span class="bi bi-eye-fill" aria-hidden="true"></span>
                Transparency
                @if (_uiState.TransparencyViewer.Visible)
                {
                    <span class="badge bg-success ms-2">Visible</span>
                }
            </a>
        </div>
    </nav>
</div>

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

    private void ToggleTransparencyViewer()
    {
        var currentlyVisible = _uiState.TransparencyViewer.Visible;
        UIControlService.UpdateTransparencyViewer(visible: !currentlyVisible);
    }

    public void Dispose()
    {
        UIControlService.UIStateChanged -= OnUIStateChanged;
    }
}
```

---

### 3. Keyboard Shortcuts (Recommended for Power Users)

**File:** `MainLayout.razor`

**Add keyboard handling:**
```razor
@using TransparentAiAgentCore.Domain.UIControl
@using TransparentAiAgentGui.Components.Transparency
@inject IUIControlService UIControlService
@implements IDisposable

<!-- Make page focusable for keyboard events -->
<div class="page" @onkeydown="HandleKeyDown" tabindex="-1">
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

  <!-- Overlay System -->
  <div class="overlay-container">
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

  // NEW: Keyboard shortcut handling
  private void HandleKeyDown(KeyboardEventArgs e)
  {
    // ESC closes any open overlay
    if (e.Key == "Escape" && _uiState.TransparencyViewer.Visible)
    {
      UIControlService.UpdateTransparencyViewer(visible: false);
    }

    // Ctrl+Shift+T toggles transparency viewer
    // (Ctrl+T is usually "new tab" in browsers, so use Ctrl+Shift+T)
    if (e.CtrlKey && e.ShiftKey && e.Key == "T")
    {
      UIControlService.UpdateTransparencyViewer(
        visible: !_uiState.TransparencyViewer.Visible);
    }
  }

  public void Dispose()
  {
    UIControlService.UIStateChanged -= OnUIStateChanged;
  }
}
```

---

### 4. Optional: Click Outside to Close (Backdrop)

**File:** `MainLayout.razor`

**If you want backdrop click to close:**
```razor
<!-- Overlay System with backdrop -->
<div class="overlay-container">
  <!-- Optional backdrop -->
  @if (_uiState.TransparencyViewer.Visible)
  {
    <div class="overlay-backdrop visible" @onclick="HandleBackdropClick"></div>
  }

  <!-- Overlay panel -->
  <div class="overlay-panel overlay-panel-right @(GetOverlayVisibilityClass())"
       @onclick:stopPropagation>
    <TransparencyViewer />
  </div>
</div>

@code {
  // ... existing code ...

  private void HandleBackdropClick()
  {
    // User clicks outside overlay to close
    UIControlService.UpdateTransparencyViewer(visible: false);
  }
}
```

---

## 📋 User Control Summary

### All User Control Points:

| Control Method | Location | Action | Keyboard |
|----------------|----------|--------|----------|
| **Close Button (X)** | Overlay header | Close overlay | - |
| **ESC Key** | Anywhere | Close overlay | `ESC` |
| **Toggle Shortcut** | Anywhere | Show/hide | `Ctrl+Shift+T` |
| **Nav Menu** | Sidebar | Toggle visibility | - |
| **Backdrop Click** | Outside overlay | Close overlay | - |
| **Page Button** | `/transparency` page | Show overlay | - |

### User Flow Examples:

**Example 1: User wants to see transparency events**
```
User clicks "Transparency" in nav menu
  → Overlay slides in from right
  → User sees events
  → User clicks X to close
  → Overlay slides out
```

**Example 2: AI teaching scenario**
```
User asks: "How does this work?"
  → AI calls ui_control_transparency_viewer(visible: true)
  → Overlay slides in
  → AI explains: "These are the events happening..."
  → User can still close with X or ESC anytime
```

**Example 3: Power user**
```
User presses Ctrl+Shift+T
  → Overlay toggles
User presses ESC
  → Overlay closes
```

---

## 🎨 Visual Indicators

### Close Button States:

**Normal:**
- Gray color (#6c757d)
- No background

**Hover:**
- Darker color (#212529)
- Light gray background (#e9ecef)
- Subtle transition

**Active (clicked):**
- Darker background (#dee2e6)

### Nav Menu Badge:

When overlay is visible:
```html
<span class="badge bg-success ms-2">Visible</span>
```

Shows green "Visible" badge next to "Transparency" menu item.

---

## ♿ Accessibility

### Keyboard Navigation:
- ✅ ESC closes overlay (standard pattern)
- ✅ Close button is focusable (keyboard users can Tab to it)
- ✅ Overlay has proper ARIA attributes

### Screen Readers:
```html
<button aria-label="Close transparency viewer"
        title="Close (ESC)">
  ×
</button>
```

### Focus Management:
When overlay opens, focus should move to overlay (optional enhancement):
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (_uiState.TransparencyViewer.Visible && _previouslyHidden)
    {
        // Focus first interactive element in overlay
        await JSRuntime.InvokeVoidAsync("focusElement", ".viewer-controls input");
    }
    _previouslyHidden = !_uiState.TransparencyViewer.Visible;
}
```

---

## 🔧 Testing User Controls

### Manual Test Cases:

1. **Close Button**
   - [ ] Click X → Overlay closes
   - [ ] Hover X → Button highlights

2. **Keyboard Shortcuts**
   - [ ] ESC closes overlay
   - [ ] Ctrl+Shift+T toggles overlay
   - [ ] Works from any page

3. **Nav Menu Toggle**
   - [ ] Click toggles overlay
   - [ ] Badge shows when visible

4. **Backdrop (if enabled)**
   - [ ] Click outside closes overlay
   - [ ] Click on overlay doesn't close
   - [ ] Backdrop dims background

5. **Cross-Page Behavior**
   - [ ] Open overlay on Home → Navigate to Tools → Overlay stays open
   - [ ] Close overlay on one page → Stays closed on other pages

---

**End of User Controls Documentation**
