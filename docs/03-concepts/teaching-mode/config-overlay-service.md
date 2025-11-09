# Config Overlay Service

**Status**: Design Phase
**Created**: 2025-11-09
**Target**: Phase 10b (Advanced Scenarios)

---

## Scope

**✅ This document covers**:
- High-level API design for Config Overlay Service
- Conceptual model and usage patterns
- Integration points

**❌ NOT in this document**:
- Detailed implementation specifics (TBD at implementation phase)
- Concrete data structures (will evolve during implementation)
- Error handling details (implementation-dependent)

---

## Purpose

The **Config Overlay Service** enables temporary, reversible overrides of system configuration. It's designed primarily for advanced teaching scenarios that need to manipulate the environment to create genuine teaching moments.

**Key Use Case:** Temporarily reduce message limits to demonstrate context truncation.

---

## Conceptual Model

### Stack-Based Overlays

Configuration works as a stack:

```
┌─────────────────────────┐
│  Overlay 2 (top)        │  ← Current scenario modifications
├─────────────────────────┤
│  Overlay 1              │  ← Previous overlay (if any)
├─────────────────────────┤
│  Base Config            │  ← Original appsettings.json values
└─────────────────────────┘

Effective Config = Base + Overlay 1 + Overlay 2
```

**Benefits:**
- Multiple overlays can be layered
- Removing an overlay restores previous state
- Automatic cleanup when scenario ends
- Thread-safe per-user configuration

---

## API Design (High-Level)

### Core Interface

```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// Service for managing temporary configuration overlays.
/// Primarily used by teaching scenarios to manipulate environment.
/// </summary>
public interface IConfigOverlayService
{
    /// <summary>
    /// Push a new configuration overlay onto the stack.
    /// Properties specified in the overlay will override current effective config.
    /// </summary>
    void PushOverlay(ConfigOverlay overlay);

    /// <summary>
    /// Remove the top overlay from the stack, restoring previous configuration.
    /// Safe to call even if no overlays exist (no-op).
    /// </summary>
    void PopOverlay();

    /// <summary>
    /// Remove all overlays, restoring base configuration.
    /// </summary>
    void ClearAllOverlays();

    /// <summary>
    /// Get the current effective configuration (base + all overlays).
    /// This is what the rest of the system should use.
    /// </summary>
    AgentConfiguration GetEffectiveConfig();

    /// <summary>
    /// Get the base configuration (no overlays applied).
    /// </summary>
    AgentConfiguration GetBaseConfig();

    /// <summary>
    /// Check if any overlays are currently active.
    /// </summary>
    bool HasActiveOverlays { get; }

    /// <summary>
    /// Raised when effective configuration changes due to overlay push/pop.
    /// </summary>
    event EventHandler<AgentConfiguration>? EffectiveConfigChanged;
}
```

---

### ConfigOverlay Class

```csharp
/// <summary>
/// Represents a temporary override of configuration properties.
/// Only specified properties are overridden; null values mean "no override".
/// </summary>
public class ConfigOverlay
{
    /// <summary>
    /// Override the maximum number of messages in conversation context.
    /// </summary>
    public int? MessageLimit { get; set; }

    /// <summary>
    /// Text to append to the system prompt temporarily.
    /// </summary>
    public string? SystemPromptAddition { get; set; }

    /// <summary>
    /// Temporarily disable user input (for auto-play scenarios).
    /// </summary>
    public bool? DisableUserInput { get; set; }

    /// <summary>
    /// Override the list of available tools (by name).
    /// Null = no override. Empty list = all tools disabled.
    /// </summary>
    public List<string>? ToolsAvailable { get; set; }

    /// <summary>
    /// Override maximum tokens per LLM request.
    /// </summary>
    public int? MaxTokens { get; set; }

    // Additional properties can be added as needed during implementation
    // Examples:
    // - public string? OverrideModelName { get; set; }
    // - public double? OverrideTemperature { get; set; }
    // - public bool? EnableDebugMode { get; set; }
}
```

**Design Note:** All properties are nullable. Only non-null properties override the underlying configuration.

---

## Usage Examples

### Example 1: Context Limits Scenario

```csharp
// Scenario starts - reduce message limit to demonstrate truncation
_configOverlayService.PushOverlay(new ConfigOverlay
{
    MessageLimit = 10,
    SystemPromptAddition = "Note: You are experiencing a teaching scenario about context windows.",
    DisableUserInput = true
});

// ... scenario runs with limited context ...
// Model genuinely experiences truncation

// Scenario ends - restore normal configuration
_configOverlayService.PopOverlay();
```

### Example 2: Tool Failure Scenario

```csharp
// Disable a specific tool to demonstrate error handling
_configOverlayService.PushOverlay(new ConfigOverlay
{
    ToolsAvailable = new List<string> { "ui_control_chat_filter", "ui_get_state" }
    // All other tools temporarily unavailable
});

// ... user tries to use unavailable tool, model handles gracefully ...

_configOverlayService.PopOverlay();
```

### Example 3: Layered Overlays

```csharp
// Scenario 1: Reduce message limit
_configOverlayService.PushOverlay(new ConfigOverlay
{
    MessageLimit = 10
});

// Nested scenario: Also add debug mode
_configOverlayService.PushOverlay(new ConfigOverlay
{
    EnableDebugMode = true
});

// Effective config: MessageLimit=10, EnableDebugMode=true

// Pop inner overlay
_configOverlayService.PopOverlay();

// Effective config: MessageLimit=10, EnableDebugMode=false (restored)

// Pop outer overlay
_configOverlayService.PopOverlay();

// Effective config: Fully restored to base
```

---

## Integration Points

### ConversationManager

**Before:**
```csharp
public class ConversationManager
{
    private readonly AgentConfiguration _config;

    public async Task SendMessageAsync(string message)
    {
        // Truncate messages based on _config.MessageLimit
        var messagesToSend = TruncateMessages(_messages, _config.MessageLimit);
        // ...
    }
}
```

**After:**
```csharp
public class ConversationManager
{
    private readonly IConfigOverlayService _configOverlayService;

    public async Task SendMessageAsync(string message)
    {
        // Get EFFECTIVE config (includes overlays)
        var effectiveConfig = _configOverlayService.GetEffectiveConfig();
        var messagesToSend = TruncateMessages(_messages, effectiveConfig.MessageLimit);
        // ...
    }
}
```

### AgentOrchestrator

Subscribe to config changes to update behavior dynamically:

```csharp
public class AgentOrchestrator
{
    protected override void OnInitialized()
    {
        _configOverlayService.EffectiveConfigChanged += OnConfigChanged;
    }

    private void OnConfigChanged(object? sender, AgentConfiguration newConfig)
    {
        // React to config changes (e.g., update tool availability)
        UpdateAvailableTools(newConfig.ToolsAvailable);
    }
}
```

### ScenarioExecutor

Uses overlay service to apply and restore environment changes:

```csharp
public class ScenarioExecutor
{
    public async Task ExecuteStepAsync(ScenarioStep step)
    {
        if (step.Type == "apply_config_overlay")
        {
            var overlay = ParseOverlay(step.Overlay);
            _configOverlayService.PushOverlay(overlay);
        }
        else if (step.Type == "restore_config_overlay")
        {
            _configOverlayService.PopOverlay();
        }
    }

    public async Task CleanupAsync()
    {
        // Always cleanup overlays when scenario ends (even if failed)
        _configOverlayService.ClearAllOverlays();
    }
}
```

---

## Service Registration

```csharp
// In Program.cs or DI setup

// Registered as SCOPED service (per-user/connection)
// Each user gets independent overlay stack
builder.Services.AddScoped<IConfigOverlayService, ConfigOverlayService>();
```

**Why Scoped?**
- Each SignalR connection (user session) has independent overlay state
- No cross-user interference
- Automatic cleanup when connection closes

---

## Design Considerations

### Why Stack-Based?

**Pros:**
- ✅ Supports nested scenarios
- ✅ Automatic restoration on pop
- ✅ Clear ownership (last pushed, first popped)
- ✅ Simple mental model

**Cons:**
- ❌ Can't remove arbitrary overlay from middle of stack (only top)
- ❌ Requires discipline to push/pop in correct order

**Mitigations:**
- Scenarios should always pop their own overlays before ending
- Automatic cleanup on scenario failure prevents leaks

### Why Nullable Properties?

**Pros:**
- ✅ Only specified properties are overridden
- ✅ Clear intent: null = "don't override", value = "override"
- ✅ Easy to merge overlays (skip nulls)

**Cons:**
- ❌ Can't explicitly set a property to "default" (only to null = no override)

**Alternative Considered:**
- Dictionary-based overlays (`Dictionary<string, object>`)
- Rejected: Less type-safe, harder to work with

### Thread Safety

**Requirements:**
- Service is scoped per SignalR connection
- Within a connection, only one thread accesses service at a time (SignalR guarantees)
- No need for locking within scoped instance

**If Singleton Later:**
- Would need thread-safe stack per user (ConcurrentDictionary<UserId, Stack<ConfigOverlay>>)
- More complex, deferred unless needed

---

## Open Questions (To Resolve During Implementation)

### Overlay Merging

**Question:** How to merge overlays when multiple are stacked?

**Option 1: Override (Last Wins)**
```
Base: MessageLimit=50
Overlay1: MessageLimit=10
Overlay2: MessageLimit=5
Effective: MessageLimit=5 (Overlay2 wins)
```

**Option 2: Additive (for some properties)**
```
Base: SystemPrompt="You are an AI."
Overlay1: SystemPromptAddition="Note: Teaching mode."
Overlay2: SystemPromptAddition="Scenario: Context limits."
Effective: SystemPrompt="You are an AI. Note: Teaching mode. Scenario: Context limits."
```

**Recommendation:** Hybrid approach
- Most properties: Override (last wins)
- SystemPromptAddition: Additive (concatenate)
- ToolsAvailable: Intersection (most restrictive wins)

### Cleanup Guarantees

**Question:** What if scenario crashes before popping overlay?

**Solution:**
- ScenarioExecutor has try-finally block
- Always call ClearAllOverlays() in finally
- Or: Overlay has TTL, auto-expires after N seconds

### Persistence

**Question:** Should overlays persist across page refreshes?

**Recommendation:** No
- Overlays are ephemeral, scenario-specific
- Page refresh ends scenario
- Simplifies implementation

---

## Implementation Notes

**Phase 10b Implementation Checklist:**

1. ✅ Define IConfigOverlayService interface
2. ✅ Implement ConfigOverlayService with overlay stack
3. ✅ Define ConfigOverlay class with common properties
4. ✅ Register service as Scoped in DI
5. ✅ Update ConversationManager to use GetEffectiveConfig()
6. ✅ Update AgentOrchestrator to respect overlays
7. ✅ Integrate with ScenarioExecutor (apply/restore steps)
8. ✅ Add automatic cleanup in ScenarioExecutor
9. ✅ Add EffectiveConfigChanged event
10. ✅ Test layered overlays
11. ✅ Test cleanup on scenario failure
12. ✅ Document any new overlay properties added

**Estimated Effort:** 2-3 days (part of Phase 10b)

---

## Testing Strategy

### Unit Tests

**ConfigOverlayServiceTests:**
- Test push/pop operations
- Test GetEffectiveConfig() merges overlays correctly
- Test ClearAllOverlays()
- Test EffectiveConfigChanged event fires
- Test overlays with null properties (no override)

**Example:**
```csharp
[TestMethod]
public void PushOverlay_OverridesMessageLimit()
{
    var service = new ConfigOverlayService(baseConfig);

    service.PushOverlay(new ConfigOverlay { MessageLimit = 10 });

    var effectiveConfig = service.GetEffectiveConfig();
    Assert.AreEqual(10, effectiveConfig.MessageLimit);
}

[TestMethod]
public void PopOverlay_RestoresPreviousConfig()
{
    var service = new ConfigOverlayService(baseConfig);

    service.PushOverlay(new ConfigOverlay { MessageLimit = 10 });
    service.PopOverlay();

    var effectiveConfig = service.GetEffectiveConfig();
    Assert.AreEqual(baseConfig.MessageLimit, effectiveConfig.MessageLimit);
}
```

### Integration Tests

**Scenario + Config Overlay:**
- Start scenario with overlay
- Verify ConversationManager uses overridden limit
- Verify messages actually truncate at new limit
- Restore overlay
- Verify original limit restored

---

## Future Enhancements

**Possible additions (beyond Phase 10b):**

1. **Named Overlays**: Assign IDs to overlays for easier management
2. **Partial Pop**: Remove specific overlay by ID, not just top
3. **Overlay Templates**: Pre-defined overlays for common scenarios
4. **Validation**: Validate overlay values (e.g., MessageLimit > 0)
5. **Audit Trail**: Log all overlay operations to TransparencyService
6. **UI Indicator**: Show user when overlays are active (e.g., banner)

---

## Related Documentation

- [future-enhancements.md](future-enhancements.md) - Advanced scenarios concept
- [scenario-schema.md](scenario-schema.md) - Scenario step types including apply_config_overlay
- [reference-scenarios/context-limits-advanced.md](reference-scenarios/context-limits-advanced.md) - Example usage

---

**Document Version:** 1.0
**Last Updated:** 2025-11-09
**Status:** Design Draft (Simple/High-Level)
**Note:** Detailed implementation will be refined during Phase 10b based on actual requirements and constraints discovered.
