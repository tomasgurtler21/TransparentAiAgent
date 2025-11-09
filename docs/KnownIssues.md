# Known Issues

**Last Updated**: 2025-11-09

This document tracks known issues and limitations in the TransparentAiAgent project.

---

## UI - Overlay Resize Not Working

**Status**: Open
**Severity**: Low
**Affects**: Overlay system (Transparency, Tools, Configuration panels)
**Reported**: 2025-11-09

### Description

The overlay resize functionality does not work - users cannot drag the left edge of overlays to change their width. The resize cursor (double-arrow) does not appear when hovering over the overlay edge.

### Expected Behavior

- Hovering over the left 8px edge of any open overlay should show a horizontal resize cursor (`↔`)
- Clicking and dragging the edge should resize the overlay width
- Width should be constrained between 300px and 80vw
- Preferred width should persist in localStorage

### Current Behavior

- No resize cursor appears when hovering over overlay edges
- Dragging has no effect
- Overlays remain fixed at default 400px width

### Attempted Solutions

Multiple approaches were tried:

1. **CSS Isolation**: Created `OverlayContainer.razor.css` with scoped styles
   - **Issue**: Scoped CSS file not bundled into `TransparentAiAgentGui.styles.css`

2. **Global CSS**: Moved styles to `app.css` (lines 53-117)
   - **Result**: CSS loads correctly, but resize handle still non-functional
   - HTML structure is correct: `<div class="overlay-resize-handle"></div>` present in DOM
   - CSS cursor style defined but not applied

3. **JavaScript**: Refactored resize logic in `site.js`
   - Shared state pattern implemented
   - Event listeners properly registered
   - `overlayResize.init()` called from `OverlayContainer.OnAfterRenderAsync`

### Technical Details

**Relevant Files**:
- `TransparentAiAgentGui/wwwroot/app.css` (lines 90-105) - Resize handle CSS
- `TransparentAiAgentGui/wwwroot/js/site.js` (lines 8-78) - Resize JavaScript
- `TransparentAiAgentGui/Components/Layout/OverlayContainer.razor` - Overlay container component

**CSS Applied**:
```css
.overlay-resize-handle {
    position: absolute;
    left: 0;
    top: 0;
    bottom: 0;
    width: 8px;
    cursor: ew-resize;
    background: linear-gradient(to right, transparent, rgba(0, 0, 0, 0.05));
    transition: background 0.2s;
    z-index: 1;
}
```

**HTML Structure** (confirmed in DOM):
```html
<div class="overlay-panel overlay-panel-right visible" b-wckoz61m6o="">
    <div class="overlay-resize-handle" b-wckoz61m6o=""></div>
    <!-- overlay content -->
</div>
```

### Root Cause

Unknown. The CSS and JavaScript are correctly implemented, but the resize handle does not respond to user interaction. Possible causes:
- CSS specificity issues with Blazor's scoped attribute selectors
- Z-index stacking context issues
- JavaScript event listener timing (though `overlayResize.init()` is called)
- Blazor rehydration affecting dynamically added event listeners

### Workaround

None. Overlays remain at fixed 400px width (90vw on mobile).

### Related Code

- Commit: `4fb6283` - "Fix overlay resize CSS - move to global app.css"
- Commit: `1e387cf` - "Fix overlay resize - move CSS to OverlayContainer for proper scoping"
- Commit: `643a361` - "Fix overlay toggle - extract OverlayContainer as interactive component"

---

## Future Issues

Additional known issues will be documented here as they are discovered.

---

**See Also**:
- [UI Architecture](04-components/ui/architecture.md#known-limitations)
- [Troubleshooting Guide](05-guides/deployment/troubleshooting.md)
