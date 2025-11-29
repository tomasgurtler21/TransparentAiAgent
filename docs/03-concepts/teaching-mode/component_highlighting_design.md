# UI Component Highlighting Architecture Plan

## Executive Summary

This plan establishes a **registry-based component highlighting system** for teaching mode that supports:
- **Multiple simultaneous highlights** (e.g., highlight context indicators AND a specific message)
- **Instance-level granularity** (highlight message #5 specifically, not all messages)
- **Unified styling** (consistent glow/pulse animation across all components)
- **Click-to-dismiss** (users can click highlighted elements to remove highlight)
- **Future extensibility** (add new highlightable components without breaking changes)

**Key Decision:** Use registry-based architecture over extending UIState properties. This enables dynamic component registration and instance-level highlighting (critical for "highlight message #123" use case).

---

## Architecture Overview

### Registry-Based Highlighting System

```
┌─────────────────────── DOMAIN LAYER ─────────────────────────┐
│                                                                │
│  UIState (record) - EXTENDED                                  │
│  + HighlightState HighlightState { get; init; }               │
│                                                                │
│  HighlightState (record) - NEW                                │
│  + IReadOnlySet<string> ActiveHighlights                      │
│  + WithHighlight(id), WithoutHighlight(id), ClearAll()        │
│  + IsHighlighted(id) → bool                                   │
│                                                                │
│  IUIControlService - EXTENDED                                 │
│  + SetHighlight(componentId, enabled)                         │
│  + SetHighlights(componentIds[], enabled)                     │
│  + ClearAllHighlights()                                       │
│  + DismissHighlight(componentId) ← user click                 │
└────────────────────────────────────────────────────────────────┘

┌─────────────────── INFRASTRUCTURE LAYER ─────────────────────┐
│                                                                │
│  IComponentRegistry (Singleton) - NEW                         │
│  + Register(id, description, category)                        │
│  + Unregister(id)                                             │
│  + GetAll() → ComponentRegistration[]                         │
│                                                                │
│  UI Control Tools (3 NEW tools)                               │
│  + ui_highlight_component(component_ids[], enabled)           │
│  + ui_list_highlightable_components(category?)                │
│  + ui_clear_all_highlights()                                  │
└────────────────────────────────────────────────────────────────┘

┌─────────────────── PRESENTATION LAYER ───────────────────────┐
│                                                                │
│  HighlightableComponentBase (abstract) - NEW                  │
│  - Auto-subscribes to UIStateChanged                          │
│  - Auto-registers/unregisters with registry                   │
│  - Handles click-to-dismiss via JS Interop                    │
│  - Provides GetHighlightClass() helper                        │
│                                                                │
│  highlight.js - NEW                                           │
│  + attachClickListener(element, dotNetRef, componentId)       │
│  + removeClickListener(element)                               │
└────────────────────────────────────────────────────────────────┘
```

### Why Registry-Based?

1. **Instance-Level Highlighting**: Can highlight `chat.message-123` vs `chat.message-456` (user requirement)
2. **No UIState Bloat**: Don't need `Message1State`, `Message2State`, etc.
3. **Dynamic Components**: Messages added/removed at runtime
4. **Infinite Extensibility**: Add new component types without core changes
5. **Type-Safe IDs**: Use constants/enums for well-known IDs, strings for dynamic IDs

---

## Core Components

### 1. HighlightState (Domain Model)

**File:** `TransparentAiAgentCore/Domain/UIControl/HighlightState.cs` (NEW)

```csharp
public record HighlightState
{
    public IReadOnlySet<string> ActiveHighlights { get; init; } = new HashSet<string>();
    public DateTime? LastDismissalTime { get; init; }

    public HighlightState WithHighlight(string componentId)
    {
        var newSet = new HashSet<string>(ActiveHighlights) { componentId };
        return this with { ActiveHighlights = newSet };
    }

    public HighlightState WithoutHighlight(string componentId)
    {
        var newSet = new HashSet<string>(ActiveHighlights);
        newSet.Remove(componentId);
        return this with { ActiveHighlights = newSet };
    }

    public HighlightState WithHighlights(IEnumerable<string> ids, bool enabled)
    {
        var newSet = new HashSet<string>(ActiveHighlights);
        foreach (var id in ids)
        {
            if (enabled) newSet.Add(id);
            else newSet.Remove(id);
        }
        return this with { ActiveHighlights = newSet };
    }

    public HighlightState ClearAll() =>
        this with { ActiveHighlights = new HashSet<string>() };

    public bool IsHighlighted(string componentId) =>
        ActiveHighlights.Contains(componentId);
}
```

**Integrate into UIState:**

```csharp
// In UIState.cs - ADD this property
public HighlightState HighlightState { get; init; } = new();
```

### 2. Component Registry

**File:** `TransparentAiAgentCore/Infrastructure/ComponentRegistry/IComponentRegistry.cs` (NEW)

```csharp
public interface IComponentRegistry
{
    void Register(string componentId, string description, string category);
    void Unregister(string componentId);
    IEnumerable<ComponentRegistration> GetAll();
    IEnumerable<ComponentRegistration> GetByCategory(string category);
    bool IsRegistered(string componentId);
}

public record ComponentRegistration
{
    public required string ComponentId { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
}
```

**Implementation:** `ComponentRegistry.cs` (NEW)

```csharp
public class ComponentRegistry : IComponentRegistry
{
    private readonly ConcurrentDictionary<string, ComponentRegistration> _components = new();
    private readonly ILogger<ComponentRegistry> _logger;

    // Thread-safe registration
    public void Register(string componentId, string description, string category)
    {
        var registration = new ComponentRegistration
        {
            ComponentId = componentId,
            Description = description,
            Category = category
        };

        if (_components.TryAdd(componentId, registration))
        {
            _logger.LogDebug("Registered: {ComponentId} ({Category})",
                componentId, category);
        }
    }

    public void Unregister(string componentId) =>
        _components.TryRemove(componentId, out _);

    public IEnumerable<ComponentRegistration> GetAll() => _components.Values;

    public IEnumerable<ComponentRegistration> GetByCategory(string category) =>
        _components.Values.Where(c =>
            c.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    public bool IsRegistered(string componentId) =>
        _components.ContainsKey(componentId);
}
```

**Registration:** Singleton service

```csharp
// In Program.cs
builder.Services.AddSingleton<IComponentRegistry, ComponentRegistry>();
```

### 3. UIControlService Extensions

**File:** `TransparentAiAgentGui/Services/UIControlService.cs` (MODIFY)

Add these 4 methods:

```csharp
public Result<UIState> SetHighlight(string componentId, bool enabled)
{
    try
    {
        lock (_stateLock)
        {
            // Validate component is registered
            if (!_componentRegistry.IsRegistered(componentId))
            {
                _logger.LogWarning("Unregistered component: {ComponentId}", componentId);
                return Result<UIState>.Fail($"Component '{componentId}' not registered");
            }

            // Update highlight state
            var newHighlightState = enabled
                ? _currentState.HighlightState.WithHighlight(componentId)
                : _currentState.HighlightState.WithoutHighlight(componentId);

            _currentState = _currentState with { HighlightState = newHighlightState };

            LogUIControlEvent("ComponentHighlightChanged", new
            {
                ComponentId = componentId,
                Enabled = enabled,
                Source = "Agent"
            });

            UIStateChanged?.Invoke(this, _currentState);
            return Result<UIState>.Ok(_currentState);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to set highlight");
        return Result<UIState>.Fail($"Failed: {ex.Message}");
    }
}

public Result<UIState> SetHighlights(IEnumerable<string> componentIds, bool enabled)
{
    try
    {
        lock (_stateLock)
        {
            var validIds = componentIds.Where(_componentRegistry.IsRegistered).ToList();

            if (!validIds.Any())
            {
                return Result<UIState>.Fail("No valid component IDs provided");
            }

            var newHighlightState = _currentState.HighlightState.WithHighlights(validIds, enabled);
            _currentState = _currentState with { HighlightState = newHighlightState };

            LogUIControlEvent("MultipleHighlightsChanged", new
            {
                ComponentIds = validIds,
                Enabled = enabled,
                Count = validIds.Count,
                Source = "Agent"
            });

            UIStateChanged?.Invoke(this, _currentState);
            return Result<UIState>.Ok(_currentState);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to set highlights");
        return Result<UIState>.Fail($"Failed: {ex.Message}");
    }
}

public Result<UIState> ClearAllHighlights()
{
    try
    {
        lock (_stateLock)
        {
            var clearedCount = _currentState.HighlightState.ActiveHighlights.Count;
            var newHighlightState = _currentState.HighlightState.ClearAll();
            _currentState = _currentState with { HighlightState = newHighlightState };

            LogUIControlEvent("AllHighlightsCleared", new
            {
                ClearedCount = clearedCount,
                Source = "Agent"
            });

            UIStateChanged?.Invoke(this, _currentState);
            return Result<UIState>.Ok(_currentState);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to clear highlights");
        return Result<UIState>.Fail($"Failed: {ex.Message}");
    }
}

public Result<UIState> DismissHighlight(string componentId)
{
    try
    {
        lock (_stateLock)
        {
            var newHighlightState = _currentState.HighlightState
                .WithoutHighlight(componentId)
                with { LastDismissalTime = DateTime.UtcNow };

            _currentState = _currentState with { HighlightState = newHighlightState };

            LogUIControlEvent("HighlightDismissed", new
            {
                ComponentId = componentId,
                Source = "User" // KEY: User-initiated action
            });

            UIStateChanged?.Invoke(this, _currentState);
            return Result<UIState>.Ok(_currentState);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to dismiss highlight");
        return Result<UIState>.Fail($"Failed: {ex.Message}");
    }
}
```

**Add dependency injection:**

```csharp
private readonly IComponentRegistry _componentRegistry;

public UIControlService(
    ITransparencyService transparencyService,
    ILogger<UIControlService> logger,
    IComponentRegistry componentRegistry) // ADD
{
    _transparencyService = transparencyService;
    _logger = logger;
    _componentRegistry = componentRegistry; // ADD
    _currentState = UIState.DefaultNormalMode();
}
```

### 4. Base Component Class

**File:** `TransparentAiAgentGui/Components/Shared/HighlightableComponentBase.cs` (NEW)

This eliminates 90% of boilerplate for making components highlightable.

```csharp
public abstract class HighlightableComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected IUIControlService UIControlService { get; set; } = null!;
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IComponentRegistry ComponentRegistry { get; set; } = null!;

    [Parameter, EditorRequired] public string ComponentId { get; set; } = "";
    [Parameter] public string ComponentDescription { get; set; } = "";
    [Parameter] public string ComponentCategory { get; set; } = "general";

    protected ElementReference HighlightElement { get; set; }
    protected bool IsHighlighted { get; private set; }

    private UIState _uiState = UIState.DefaultNormalMode();
    private DotNetObjectReference<HighlightableComponentBase>? _dotNetRef;
    private bool _clickListenerAttached = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UIControlService.UIStateChanged += OnUIStateChanged;
        _uiState = UIControlService.GetCurrentState();
        IsHighlighted = _uiState.HighlightState.IsHighlighted(ComponentId);

        if (!string.IsNullOrWhiteSpace(ComponentId))
        {
            ComponentRegistry.Register(ComponentId, ComponentDescription, ComponentCategory);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsHighlighted && !_clickListenerAttached)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("highlightComponent.attachClickListener",
                HighlightElement, _dotNetRef, ComponentId);
            _clickListenerAttached = true;
        }
        else if (!IsHighlighted && _clickListenerAttached)
        {
            await JS.InvokeVoidAsync("highlightComponent.removeClickListener",
                HighlightElement);
            _clickListenerAttached = false;
        }
    }

    private void OnUIStateChanged(object? sender, UIState newState)
    {
        InvokeAsync(() =>
        {
            _uiState = newState;
            bool wasHighlighted = IsHighlighted;
            IsHighlighted = _uiState.HighlightState.IsHighlighted(ComponentId);

            if (wasHighlighted != IsHighlighted)
                StateHasChanged();
        });
    }

    [JSInvokable]
    public async Task OnComponentClicked()
    {
        UIControlService.DismissHighlight(ComponentId);
        await OnHighlightDismissedAsync();
    }

    protected virtual Task OnHighlightDismissedAsync() => Task.CompletedTask;

    protected string GetHighlightClass() => IsHighlighted ? "highlighted" : "";

    public async ValueTask DisposeAsync()
    {
        UIControlService.UIStateChanged -= OnUIStateChanged;
        ComponentRegistry.Unregister(ComponentId);

        if (_clickListenerAttached)
        {
            try {
                await JS.InvokeVoidAsync("highlightComponent.removeClickListener", HighlightElement);
            }
            catch { /* Suppress JS exceptions during disposal */ }
        }

        _dotNetRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### 5. JavaScript Interop

**File:** `TransparentAiAgentGui/wwwroot/js/highlight.js` (NEW)

```javascript
window.highlightComponent = {
    activeListeners: new Map(),

    attachClickListener: function(element, dotNetRef, componentId) {
        if (!element) return;
        this.removeClickListener(element);

        const handler = async (e) => {
            e.stopPropagation();
            await dotNetRef.invokeMethodAsync('OnComponentClicked');
        };

        this.activeListeners.set(element, handler);
        element.addEventListener('click', handler, { once: true });
    },

    removeClickListener: function(element) {
        if (!element) return;
        const handler = this.activeListeners.get(element);
        if (handler) {
            element.removeEventListener('click', handler);
            this.activeListeners.delete(element);
        }
    }
};
```

**Reference in `_Host.cshtml`:**

```html
<script src="~/js/highlight.js"></script>
```

### 6. Global CSS Styling

**File:** `TransparentAiAgentGui/wwwroot/css/app.css` (ADD)

```css
/* ============================================
   Component Highlighting System
   ============================================ */

.highlighted {
    position: relative;
    background-color: var(--bg-elevated);
    border: 2px solid var(--accent-orange);
    border-radius: 0.375rem;
    padding: 4px 8px;
    box-shadow: 0 0 12px rgba(255, 169, 64, 0.6);
    animation: highlight-pulse 1.5s ease-in-out infinite;
    cursor: pointer;
    transition: all var(--transition-fast);
}

.highlighted:hover {
    box-shadow: 0 0 16px rgba(255, 169, 64, 0.8);
}

@keyframes highlight-pulse {
    0%, 100% { box-shadow: 0 0 12px rgba(255, 169, 64, 0.6); }
    50% { box-shadow: 0 0 20px rgba(255, 169, 64, 0.9); }
}

.highlighted::after {
    content: '👆 Click to dismiss';
    position: absolute;
    top: -28px;
    right: 0;
    background-color: var(--accent-orange);
    color: var(--bg-primary);
    padding: 4px 8px;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    font-weight: 600;
    white-space: nowrap;
    opacity: 0;
    transition: opacity 0.3s ease;
    pointer-events: none;
    z-index: 1000;
}

.highlighted:hover::after {
    opacity: 1;
}
```

### 7. New UI Control Tools

**File:** `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistry.cs` (MODIFY)

Add 3 new tools to `RegisterAllTools()`:

```csharp
private void RegisterAllTools()
{
    // Existing tools
    _tools.Add(CreateChatFilterTool());
    _tools.Add(CreateFilterVisibilityTool());
    _tools.Add(CreateGetStateTool());
    _tools.Add(CreateTransparencyViewerTool());
    _tools.Add(CreateToolsPanelTool());
    _tools.Add(CreateContextIndicatorsTool());
    _tools.Add(CreateConfigurationTool());
    _tools.Add(CreateScenarioSelectorTool());

    // NEW highlighting tools
    _tools.Add(CreateHighlightComponentTool());
    _tools.Add(CreateListComponentsTool());
    _tools.Add(CreateClearHighlightsTool());
}

private Tool CreateHighlightComponentTool() => new()
{
    Name = "ui_highlight_component",
    Description = "Highlight one or more UI components to draw user attention. Highlights persist until cleared or user clicks the element. Use this to teach about specific UI features.",
    InputSchema = new ToolInputSchema
    {
        Type = "object",
        Properties = new Dictionary<string, object>
        {
            ["component_ids"] = new
            {
                type = "array",
                items = new { type = "string" },
                description = "Component IDs to highlight (e.g., ['chat.context-indicators', 'chat.message-123']). Call ui_list_highlightable_components to discover available components."
            },
            ["enabled"] = new
            {
                type = "boolean",
                description = "True to add highlight, false to remove",
                @default = true
            }
        },
        Required = new List<string> { "component_ids" }
    },
    SourceType = ToolSourceType.BuiltInUIControl
};

private Tool CreateListComponentsTool() => new()
{
    Name = "ui_list_highlightable_components",
    Description = "List all currently registered highlightable components. Use this before highlighting to discover what components are available.",
    InputSchema = new ToolInputSchema
    {
        Type = "object",
        Properties = new Dictionary<string, object>
        {
            ["category"] = new
            {
                type = "string",
                description = "Optional category filter (e.g., 'chat', 'tools', 'config')"
            }
        }
    },
    SourceType = ToolSourceType.BuiltInUIControl
};

private Tool CreateClearHighlightsTool() => new()
{
    Name = "ui_clear_all_highlights",
    Description = "Clear all active highlights. Useful when transitioning between teaching topics.",
    InputSchema = new ToolInputSchema
    {
        Type = "object",
        Properties = new Dictionary<string, object>()
    },
    SourceType = ToolSourceType.BuiltInUIControl
};
```

**File:** `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutor.cs` (MODIFY)

Add handlers for the new tools:

```csharp
public Task<ToolResult> ExecuteToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken)
{
    try
    {
        var result = toolName switch
        {
            "ui_control_chat_filter" => ExecuteChatFilterTool(arguments),
            "ui_control_filter_visibility" => ExecuteFilterVisibilityTool(arguments),
            "ui_get_state" => ExecuteGetStateTool(arguments),
            "ui_control_transparency_viewer" => ExecuteTransparencyViewerTool(arguments),
            "ui_control_tools_panel" => ExecuteToolsPanelTool(arguments),
            "ui_control_context_indicators" => ExecuteContextIndicatorsTool(arguments),
            "ui_control_configuration" => ExecuteConfigurationTool(arguments),
            "ui_control_scenario_selector" => ExecuteScenarioSelectorTool(arguments),

            // NEW handlers
            "ui_highlight_component" => ExecuteHighlightComponentTool(arguments),
            "ui_list_highlightable_components" => ExecuteListComponentsTool(arguments),
            "ui_clear_all_highlights" => ExecuteClearHighlightsTool(arguments),

            _ => throw new ArgumentException($"Unknown UI control tool: {toolName}")
        };

        return Task.FromResult(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to execute UI control tool: {ToolName}", toolName);
        return Task.FromResult(ToolResult.Failure($"Tool execution failed: {ex.Message}"));
    }
}

private ToolResult ExecuteHighlightComponentTool(JsonElement arguments)
{
    var componentIds = GetStringArray(arguments, "component_ids");
    var enabled = GetOptionalBool(arguments, "enabled") ?? true;

    if (!componentIds.Any())
    {
        return ToolResult.Failure("No component IDs provided");
    }

    var result = _uiControlService.SetHighlights(componentIds, enabled);

    return result.Success
        ? ToolResult.Success($"Highlight {(enabled ? "added to" : "removed from")} {componentIds.Count()} component(s)",
            SerializeHighlightState(result.Value!.HighlightState))
        : ToolResult.Failure(result.ErrorMessage!);
}

private ToolResult ExecuteListComponentsTool(JsonElement arguments)
{
    var category = GetOptionalString(arguments, "category");

    var components = string.IsNullOrWhiteSpace(category)
        ? _componentRegistry.GetAll()
        : _componentRegistry.GetByCategory(category);

    var groupedByCategory = components
        .GroupBy(c => c.Category)
        .ToDictionary(
            g => g.Key,
            g => g.Select(c => new { c.ComponentId, c.Description }).ToList()
        );

    var jsonDoc = JsonSerializer.SerializeToDocument(groupedByCategory, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    return ToolResult.Success($"Found {components.Count()} highlightable component(s)", jsonDoc);
}

private ToolResult ExecuteClearHighlightsTool(JsonElement arguments)
{
    var result = _uiControlService.ClearAllHighlights();

    return result.Success
        ? ToolResult.Success("All highlights cleared")
        : ToolResult.Failure(result.ErrorMessage!);
}

// Helper methods
private IEnumerable<string> GetStringArray(JsonElement args, string propertyName)
{
    if (args.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
    {
        return prop.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s));
    }
    return Enumerable.Empty<string>();
}

private JsonDocument SerializeHighlightState(HighlightState state)
{
    return JsonSerializer.SerializeToDocument(new
    {
        activeHighlights = state.ActiveHighlights,
        count = state.ActiveHighlights.Count
    }, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}
```

**Add dependencies:**

```csharp
private readonly IComponentRegistry _componentRegistry;

public UIControlToolExecutor(
    IUIControlService uiControlService,
    ILogger<UIControlToolExecutor> logger,
    IComponentRegistry componentRegistry) // ADD
{
    _uiControlService = uiControlService;
    _logger = logger;
    _componentRegistry = componentRegistry; // ADD
}
```

---

## Migration Strategy

### Phase 1: Foundation Setup (No Breaking Changes)

1. **Create HighlightState.cs** - new immutable record
2. **Extend UIState** - add `HighlightState` property with default value
3. **Create IComponentRegistry** and implementation
4. **Register ComponentRegistry** as singleton in DI
5. **Unit tests** for HighlightState immutability

**Outcome:** New infrastructure exists but nothing uses it yet. Existing code unaffected.

### Phase 2: Service Layer Extensions

6. **Extend IUIControlService** - add 4 new method signatures
7. **Implement methods in UIControlService** - inject IComponentRegistry
8. **Create HighlightableComponentBase** - reusable base class
9. **Create highlight.js** - JavaScript interop for click handling
10. **Add global CSS** - `.highlighted` class with animations
11. **Unit tests** for service methods and state management

**Outcome:** Full highlighting infrastructure ready. Still no UI impact.

### Phase 3: Tool Integration

12. **Add 3 new tools** to BuiltInUIControlToolRegistry
13. **Extend UIControlToolExecutor** - add tool handlers, inject registry
14. **Integration tests** - test tool execution end-to-end

**Outcome:** Agent can call highlighting tools. Registry is queryable.

### Phase 4: Migrate Existing Highlight (Context Indicators)

15. **Refactor MessageDisplay.razor** to use new system
    - Keep old code path temporarily (feature flag)
    - New path: inherit from HighlightableComponentBase OR use new registry approach
    - Test both paths work identically
16. **Verify backward compatibility** - existing scenarios still work
17. **Remove old code path** once confident

**Migration Example:**

**Option A: Minimal Change (Keep Component Structure)**
```razor
@* MessageDisplay.razor - BEFORE *@
<span class="message-context-status @(_uiState.ContextIndicators.Highlighted ? "highlighted" : "")">
    @Message.ContextStatusIcon
</span>

@* MessageDisplay.razor - AFTER (minimal change) *@
<span @ref="_highlightElement"
      class="message-context-status @GetContextIndicatorHighlightClass()">
    @Message.ContextStatusIcon
</span>

@code {
    private ElementReference _highlightElement;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Register this specific instance
        ComponentRegistry.Register(
            "chat.context-indicators",
            "Message context status indicators (✅/⚠️)",
            "chat");
    }

    private string GetContextIndicatorHighlightClass()
    {
        // Check BOTH old and new systems during migration
        bool oldHighlight = _uiState.ContextIndicators.Highlighted;
        bool newHighlight = _uiState.HighlightState.IsHighlighted("chat.context-indicators");

        return (oldHighlight || newHighlight) ? "highlighted" : "";
    }
}
```

**Option B: Full Base Class (Cleaner Long-Term)**

Create wrapper component `ContextIndicatorsDisplay.razor`:

```razor
@inherits HighlightableComponentBase

<span @ref="HighlightElement" class="message-context-status @GetHighlightClass()">
    @ContextStatusIcon
</span>

@code {
    [Parameter, EditorRequired]
    public string ContextStatusIcon { get; set; } = "";

    protected override void OnInitialized()
    {
        ComponentId = "chat.context-indicators";
        ComponentDescription = "Message context status indicators (✅/⚠️)";
        ComponentCategory = "chat";

        base.OnInitialized(); // Handles registration + subscription
    }
}
```

**Recommendation:** Start with Option A (minimal), migrate to Option B once proven.

### Phase 5: Documentation & Expansion

18. **Update teaching mode docs** - document new architecture
19. **Create developer guide** - "How to make components highlightable"
20. **Example implementations** - 2-3 more components as proof
21. **Update scenario examples** - demonstrate new highlighting capabilities

---

## Component ID Naming Convention

To support future instance-level highlighting (e.g., specific messages), establish consistent ID pattern:

### Pattern: `{category}.{component-type}[.{instance-id}]`

**Examples:**

```typescript
// Type-level (all instances)
"chat.context-indicators"      // All context indicators
"chat.filter-panel"            // The filter controls panel
"tools.panel"                  // Tools overview panel
"config.llm-parameters"        // LLM config section

// Instance-level (specific instance)
"chat.message-{messageId}"     // Specific message by ID
"chat.message-123"             // Message with ID 123
"tools.tool-read_file"         // Specific tool card

// Nested components
"chat.message-123.thinking"    // Thinking block within message 123
"chat.message-123.tool-call"   // Tool call within message 123
```

### Component Categories

- `chat` - Chat/messaging UI
- `tools` - Tools panel and tool cards
- `config` - Configuration page sections
- `transparency` - Transparency viewer
- `filter` - Filter controls
- `scenario` - Scenario UI elements

---

## Future Instance-Level Highlighting (Messages)

When ready to highlight specific messages, the pattern is:

**1. Assign IDs to messages:**

```csharp
// In UIMessage model
public string MessageId { get; init; } = Guid.NewGuid().ToString();
```

**2. Register each message component:**

```razor
@* MessageDisplay.razor *@
@code {
    protected override void OnInitialized()
    {
        ComponentRegistry.Register(
            $"chat.message-{Message.MessageId}",
            $"Message: {Message.Content.Substring(0, Math.Min(50, Message.Content.Length))}...",
            "chat");
    }
}
```

**3. Agent highlights specific message:**

```json
{
  "tool": "ui_highlight_component",
  "arguments": {
    "component_ids": ["chat.message-abc123"]
  }
}
```

**4. Click to dismiss works automatically** (base class handles it)

---

## Testing Strategy

### Unit Tests

**HighlightStateTests.cs**
- Immutability (WithHighlight returns new instance)
- Set operations (add, remove, clear)
- IsHighlighted query

**ComponentRegistryTests.cs**
- Thread-safe registration
- Unregistration
- Query by category
- Duplicate IDs

**UIControlServiceTests.cs**
- SetHighlight validates against registry
- SetHighlights batch operation
- ClearAllHighlights
- DismissHighlight (user vs agent source)
- Event firing

### Integration Tests

**HighlightingIntegrationTests.cs**
- Tool call → service → event → UI update flow
- Multiple simultaneous highlights
- Click-to-dismiss via JS interop
- Registry discovery via list tool

### Component Tests (bUnit)

**HighlightableComponentTests.cs**
- Base class auto-registration
- CSS class toggling
- Event subscription/disposal
- JS interop invocation

---

## Critical Files to Create/Modify

### NEW Files (11)

1. `TransparentAiAgentCore/Domain/UIControl/HighlightState.cs`
2. `TransparentAiAgentCore/Infrastructure/ComponentRegistry/IComponentRegistry.cs`
3. `TransparentAiAgentCore/Infrastructure/ComponentRegistry/ComponentRegistry.cs`
4. `TransparentAiAgentGui/Components/Shared/HighlightableComponentBase.cs`
5. `TransparentAiAgentGui/wwwroot/js/highlight.js`
6. `TransparentAiAgentCore_Tests/Domain/UIControl/HighlightStateTests.cs`
7. `TransparentAiAgentCore_Tests/Infrastructure/ComponentRegistry/ComponentRegistryTests.cs`
8. `TransparentAiAgentCore_Tests/Services/UIControlServiceHighlightTests.cs`
9. `TransparentAiAgentCore_Tests/Infrastructure/Tools/HighlightToolTests.cs`
10. `TransparentAiAgentGui_Tests/Components/HighlightableComponentTests.cs` (if using bUnit)
11. `docs/03-concepts/teaching-mode/component-highlighting.md` (architecture documentation)

### MODIFY Files (7)

1. `TransparentAiAgentCore/Domain/UIControl/UIState.cs`
   - Add `HighlightState` property

2. `TransparentAiAgentCore/Domain/UIControl/IUIControlService.cs`
   - Add 4 new method signatures

3. `TransparentAiAgentGui/Services/UIControlService.cs`
   - Inject IComponentRegistry
   - Implement 4 new methods

4. `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistry.cs`
   - Add 3 new tool definitions

5. `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutor.cs`
   - Inject IComponentRegistry
   - Add 3 tool handlers

6. `TransparentAiAgentGui/Program.cs`
   - Register IComponentRegistry as singleton

7. `TransparentAiAgentGui/wwwroot/css/app.css`
   - Add `.highlighted` CSS rules

8. `TransparentAiAgentGui/Pages/_Host.cshtml` (or equivalent layout)
   - Reference `highlight.js`

### REFACTOR (Optional - Phase 4)

9. `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor`
   - Migrate context indicators to new system

---

## Success Criteria

### Functional Requirements
✅ Components can register with unique IDs
✅ Multiple components can be highlighted simultaneously
✅ Highlights persist until explicit removal or user click
✅ Agent can query available components via tool
✅ Agent can highlight/un-highlight via tool
✅ User can dismiss highlights by clicking
✅ Unified visual style (glow/pulse animation)
✅ Teaching mode only (respects AppMode)

### Non-Functional Requirements
✅ Zero breaking changes to existing code
✅ Thread-safe component registration
✅ Proper event subscription cleanup (no leaks)
✅ Clear transparency logging (agent vs user actions)
✅ Extensible for future components (no core changes needed)

### Testing Requirements
✅ Unit test coverage >80% for new code
✅ Integration tests for tool execution
✅ Manual testing of click-to-dismiss
✅ Verification that existing context indicators still work

---

## Documentation Plan

### Update Existing Docs

**File:** `docs/03-concepts/teaching-mode/architecture.md`
- Add section on component highlighting system
- Document registry pattern
- Tool reference

### Create New Docs

**File:** `docs/03-concepts/teaching-mode/component-highlighting.md`
- Complete architecture specification
- Developer guide: "How to make components highlightable"
- ID naming conventions
- Example implementations

**File:** `docs/05-guides/development/adding-highlightable-components.md`
- Step-by-step tutorial
- Code examples for both base class and manual approaches
- Testing checklist

**File:** `docs/04-components/ui/highlightable-component-base.md`
- API reference for base class
- Lifecycle explanation
- Customization options

---

## Open Questions & Future Considerations

### Message Highlighting Timeline
- **Q:** When to implement per-message highlighting?
- **A:** After base system proven stable (likely Phase 6+). UIMessage model needs stable IDs first.

### Performance with Many Components
- **Q:** What if 100+ messages register simultaneously?
- **A:** ConcurrentDictionary handles this. If bottleneck, add batched registration API.

### Highlight Animations Variety
- **Q:** Different animations for different component types?
- **A:** Not in v1. Global `.highlighted` class keeps it simple. Can add `.highlighted-{type}` variants later.

### Teaching Mode Auto-Clear
- **Q:** Auto-clear highlights when switching to Normal mode?
- **A:** Yes, add to `SwitchMode()` in UIControlService.

### Nested Component Highlighting
- **Q:** Highlight parent highlights all children?
- **A:** Not in v1. Agent must specify each component ID explicitly. Future: add `include_children` parameter.

---

## Implementation Checklist (TDD Sessions)

### 🔴 RED → 🟢 GREEN → 🔵 REFACTOR

Each session follows complete TDD cycle. Sessions are independent and can be done separately.

---

## Session 1: HighlightState Domain Model ✅ COMPLETED

**Goal:** Domain model for tracking active highlights (immutable record)

**TDD Cycle:**
1. 🔴 RED: Write tests for HighlightState behavior
2. 🟢 GREEN: Implement HighlightState to pass tests
3. 🔵 REFACTOR: Clean up implementation

**What to Test:**
- ✅ `WithHighlight` creates new instance with added ID (immutability)
- ✅ `WithoutHighlight` creates new instance with removed ID
- ✅ `WithHighlights` batch operation (add/remove multiple)
- ✅ `ClearAll` returns empty state
- ✅ `IsHighlighted` query correctness

**Files:**
- `TransparentAiAgentCore_Tests/Domain/UIControl/HighlightStateTests.cs` (RED first)
- `TransparentAiAgentCore/Domain/UIControl/HighlightState.cs` (GREEN second)
- `TransparentAiAgentCore/Domain/UIControl/UIState.cs` (add property)

**Status:** ✅ Implementation done, tests pending

---

## Session 2: Component Registry Infrastructure

**Goal:** Thread-safe registry for component registration/lookup

**TDD Cycle:**
1. 🔴 RED: Write tests for ComponentRegistry
2. 🟢 GREEN: Implement ComponentRegistry
3. 🔵 REFACTOR: Optimize thread safety

**What to Test:**
- ✅ `Register` adds component to registry
- ✅ `Unregister` removes component
- ✅ `IsRegistered` returns correct bool
- ✅ `GetAll` returns all registrations
- ✅ `GetByCategory` filters correctly (case-insensitive)
- ✅ Duplicate IDs handled gracefully (TryAdd semantics)
- ✅ Thread safety (concurrent registration from 10+ threads)

**Files:**
- `TransparentAiAgentCore_Tests/Infrastructure/ComponentRegistry/ComponentRegistryTests.cs` (RED)
- `TransparentAiAgentCore/Infrastructure/ComponentRegistry/ComponentRegistry.cs` (GREEN)
- `TransparentAiAgentGui/Program.cs` (register as singleton)

**Dependencies:** Session 1 complete

---

## Session 3: UIControlService Highlight Methods

**Goal:** Extend UIControlService with highlight control methods

**TDD Cycle:**
1. 🔴 RED: Write tests for 4 new methods
2. 🟢 GREEN: Implement methods in UIControlService
3. 🔵 REFACTOR: Extract common validation logic

**What to Test:**
- ✅ `SetHighlight` validates component is registered (fails if not)
- ✅ `SetHighlight` updates state and fires UIStateChanged event
- ✅ `SetHighlights` batch operation validates all IDs
- ✅ `SetHighlights` ignores invalid IDs, processes valid ones
- ✅ `ClearAllHighlights` clears all and fires event
- ✅ `DismissHighlight` logs "User" as source (vs "Agent")
- ✅ All methods are thread-safe (lock usage)
- ✅ Transparency events logged for all operations

**Files:**
- `TransparentAiAgentCore_Tests/Services/UIControlServiceHighlightTests.cs` (RED)
- `TransparentAiAgentCore/Domain/UIControl/IUIControlService.cs` (add method signatures)
- `TransparentAiAgentGui/Services/UIControlService.cs` (GREEN - inject registry, implement)

**Dependencies:** Session 2 complete (needs ComponentRegistry)

---

## Session 4: UI Control Tools (Agent Interface)

**Goal:** Agent tools for controlling highlights

**TDD Cycle:**
1. 🔴 RED: Write tests for tool execution
2. 🟢 GREEN: Implement tool handlers
3. 🔵 REFACTOR: Extract JSON parsing helpers

**What to Test:**
- ✅ `ui_highlight_component` parses component_ids array correctly
- ✅ `ui_highlight_component` calls SetHighlights with correct params
- ✅ `ui_highlight_component` returns success with highlight state
- ✅ `ui_highlight_component` returns failure if no valid IDs
- ✅ `ui_list_highlightable_components` queries registry
- ✅ `ui_list_highlightable_components` groups by category
- ✅ `ui_list_highlightable_components` filters by category (optional param)
- ✅ `ui_clear_all_highlights` clears and returns success

**Files:**
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/HighlightToolTests.cs` (RED)
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistry.cs` (add tools)
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutor.cs` (GREEN - handlers)

**Dependencies:** Session 3 complete (needs UIControlService methods)

---

## Session 5: HighlightableComponentBase (UI Infrastructure)

**Goal:** Reusable base class for highlightable components

**TDD Cycle:**
1. 🔴 RED: Component tests (bUnit or manual test component)
2. 🟢 GREEN: Implement base class
3. 🔵 REFACTOR: Simplify lifecycle management

**What to Test:**
- ✅ `OnInitialized` registers component with registry
- ✅ `OnInitialized` subscribes to UIStateChanged event
- ✅ `OnUIStateChanged` updates IsHighlighted property
- ✅ `OnUIStateChanged` calls StateHasChanged when highlight changes
- ✅ `GetHighlightClass` returns "highlighted" when true, "" when false
- ✅ `Dispose` unregisters component
- ✅ `Dispose` unsubscribes from event (no memory leak)

**Files:**
- `TransparentAiAgentGui_Tests/Components/HighlightableComponentBaseTests.cs` (RED - optional, can use integration test)
- `TransparentAiAgentGui/Components/Shared/HighlightableComponentBase.cs` (GREEN)
- `TransparentAiAgentGui/wwwroot/js/highlight.js` (JS interop)
- `TransparentAiAgentGui/wwwroot/css/app.css` (add .highlighted CSS)

**Dependencies:** Session 3 complete (needs UIControlService)

**Note:** JS interop for click-to-dismiss is integration-tested in Session 6

---

## Session 6: Click-to-Dismiss Integration

**Goal:** JavaScript interop for user-dismissing highlights

**TDD Cycle:**
1. 🔴 RED: Manual integration test (highlight → click → verify dismissed)
2. 🟢 GREEN: Implement JS interop and C# callback
3. 🔵 REFACTOR: Clean up event listener management

**What to Test (Manual/Integration):**
- ✅ Click on highlighted element calls DismissHighlight
- ✅ DismissHighlight logs "User" source to transparency
- ✅ Highlight removed after click
- ✅ Event listener cleaned up after click ({ once: true })
- ✅ Multiple highlights work independently

**Files:**
- `TransparentAiAgentGui/Components/Shared/HighlightableComponentBase.cs` (complete OnAfterRenderAsync logic)
- `TransparentAiAgentGui/wwwroot/js/highlight.js` (complete implementation)
- `TransparentAiAgentGui/Pages/_Host.cshtml` (reference script)

**Dependencies:** Session 5 complete

**Testing:** Manual browser test (automated bUnit test optional)

---

## Session 7: Migrate Context Indicators

**Goal:** Migrate existing MessageDisplay context indicators to new system

**TDD Cycle:**
1. 🔴 RED: Verify existing scenario fails with new system
2. 🟢 GREEN: Update MessageDisplay to support both old + new
3. 🔵 REFACTOR: Remove old path once verified

**What to Test:**
- ✅ Existing `why-tool-visibility-matters.json` scenario still works
- ✅ Old system (ContextIndicators.Highlighted) still works
- ✅ New system (HighlightState.IsHighlighted) works
- ✅ Dual system check (old OR new) during migration
- ✅ Remove old system after 1-2 sessions of stability

**Files:**
- `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor` (MODIFY)
- Run existing scenarios as regression tests

**Dependencies:** Session 6 complete

**Migration Strategy:** Keep both systems working in parallel temporarily

---

## Session 8: Documentation & Examples

**Goal:** Document the architecture for future developers

**No TDD** (documentation task)

**Create:**
- `docs/03-concepts/teaching-mode/component-highlighting.md` (architecture spec)
- `docs/05-guides/development/adding-highlightable-components.md` (how-to guide)
- Update `docs/03-concepts/teaching-mode/architecture.md` (add section)
- Create example scenario using new highlighting

**Dependencies:** Session 7 complete

---

## Session Dependencies Graph

```
Session 1 (HighlightState) ✅ DONE
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

## TDD Discipline Reminders

### Before Each Session

1. **READ** the Lean TDD principles (`/.claude/skills/tdd/SKILL.md`)
2. **PLAN** what behavior to test (validation, transformations, business rules)
3. **SKIP** trivial tests (property getters, framework features)

### During RED Phase

1. **WRITE TEST FIRST** - before any implementation
2. **ADD MINIMAL STUBS** to make test compile (empty methods, NotImplementedException)
3. **RUN TEST** - must execute and FAIL with proper assertion error
4. **INVESTIGATE** failure message - verify it's the RIGHT failure
5. **STOP** if test passes unexpectedly - understand WHY

### During GREEN Phase

1. **WRITE MINIMUM CODE** to pass test
2. **NO EXTRA FEATURES** - only what test requires
3. **RUN TEST** - must pass
4. **INVESTIGATE** if fails - fix the bug

### During REFACTOR Phase

1. **IMPROVE CODE QUALITY** while keeping tests green
2. **RUN TESTS** after each refactoring
3. **DON'T ADD FEATURES** - only improve existing code

### Test Naming

Pattern: `{MethodOrScenario}_{StateUnderTest}_{ExpectedBehavior}`

Examples:
- `WithHighlight_ValidId_ReturnsNewInstance`
- `Register_SameIdTwice_IgnoresDuplicate`
- `SetHighlight_UnregisteredComponent_ReturnsFailure`

### UI Infrastructure (Week 2)
- [ ] Create HighlightableComponentBase abstract class
- [ ] Create highlight.js with click listener management
- [ ] Add global .highlighted CSS with animations
- [ ] Reference highlight.js in _Host.cshtml
- [ ] Component tests for base class (if using bUnit)

### Tool Integration (Week 2)
- [ ] Add ui_highlight_component tool definition
- [ ] Add ui_list_highlightable_components tool definition
- [ ] Add ui_clear_all_highlights tool definition
- [ ] Implement ExecuteHighlightComponentTool handler
- [ ] Implement ExecuteListComponentsTool handler
- [ ] Implement ExecuteClearHighlightsTool handler
- [ ] Integration tests for tool execution

### Migration & Testing (Week 3)
- [ ] Migrate MessageDisplay context indicators to new system
- [ ] Verify existing scenarios still work
- [ ] Manual test: click-to-dismiss functionality
- [ ] Manual test: multiple simultaneous highlights
- [ ] Performance test: 50+ components registered
- [ ] Update existing teaching mode documentation
- [ ] Create new developer guide documentation

### Future Expansion (Week 4+)
- [ ] Add 2-3 more highlightable components (filter panel, tools panel)
- [ ] Create example scenario using new highlights
- [ ] Implement per-message highlighting (when UIMessage has IDs)
- [ ] Add highlight animation variants (if needed)

---

## Summary

This plan establishes a **flexible, registry-based highlighting architecture** that:

1. **Supports the user's requirement** for instance-level highlighting (specific messages)
2. **Zero breaking changes** - new infrastructure coexists with existing code
3. **Minimal boilerplate** - base class eliminates 90% of repetitive code
4. **Future-proof** - extensible without core modifications
5. **Production-ready** - thread-safe, tested, documented

The registry pattern was chosen over extending UIState because it enables dynamic component registration and instance-specific highlighting (`chat.message-123` vs `chat.message-456`), which is critical for the user's vision of highlighting individual messages in chat history.

**Next Steps:** Review plan, clarify any questions, then proceed with Phase 1 implementation.
