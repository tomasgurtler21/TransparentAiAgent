# LLM Provider Manager

**Last Updated**: 2025-11-16
**Status**: ✅ Implemented
**Layer**: Domain + Infrastructure
**Implementation**: Phase 8 (Extended as part of LLM Selector feature)

---

## 📋 Scope

### ✅ What's in this document
- ILLMProviderManager interface and implementation
- Provider lifecycle (lazy-loading and caching)
- Provider switching mechanism
- ProviderInfo model for UI integration

### ❌ What's NOT in this document
- Individual provider implementations → See [anthropic-provider.md](anthropic-provider.md) and [azure-openai-provider.md](azure-openai-provider.md)
- Provider configuration → See [LLM Provider Selector Guide](../../05-guides/deployment/llm-provider-selector.md)
- Provider abstraction → See [provider-abstraction.md](provider-abstraction.md)

---

## Overview

The **LLM Provider Manager** is the central service for managing multiple LLM provider instances, enabling dynamic provider switching without application restart. It implements lazy-loading and caching to optimize resource usage while providing a clean abstraction for consumers.

### Key Features

- ✅ **Lazy Loading**: Providers created only when first used
- ✅ **Caching**: Provider instances reused across requests
- ✅ **Dynamic Switching**: Change active provider at runtime
- ✅ **Thread-Safe**: Safe concurrent access to provider instances
- ✅ **Provider Discovery**: List all available providers with metadata

---

## Architecture

### Provider Manager Pattern

```
┌─────────────────────────────────────┐
│   Application Layer                 │
│ (AgentOrchestrator, etc.)           │
└──────────────┬──────────────────────┘
               │ GetActiveProvider()
               ▼
      ┌────────────────────┐
      │ ILLMProviderManager│ (Domain Interface)
      │                    │
      │ - GetActiveProvider()
      │ - SetActiveProviderAsync()
      │ - GetAvailableProviders()
      │ - GetCurrentProviderInfo()
      └────────────────────┘
               ▲
               │ Implements
               │
      ┌────────▼────────────┐
      │ LLMProviderManager  │ (Infrastructure)
      │                     │
      │ - Configuration     │
      │ - Provider Cache    │
      │ - Factory           │
      └─────────────────────┘
               │
               │ Lazy-loads via
               ▼
      ┌────────────────────┐
      │ LLMProviderFactory │
      │                    │
      │ CreateProvider()   │
      └────────────────────┘
               │
               ├──→ AnthropicProvider
               ├──→ AzureOpenAIProvider
               └──→ OpenAIProvider
```

### Component Flow

```
1. Application Request:
   GetActiveProvider()
        ↓
2. Manager Checks Cache:
   Provider cached? → Return cached instance
        ↓ (if not cached)
3. Lazy Load:
   Factory.CreateProvider(config)
        ↓
4. Cache Instance:
   Store in dictionary
        ↓
5. Return Provider:
   ILLMProvider instance
```

---

## Interface

### ILLMProviderManager

**Location**: `TransparentAiAgentCore/Domain/LLM/ILLMProviderManager.cs`

```csharp
public interface ILLMProviderManager
{
    /// <summary>
    /// Gets the currently active LLM provider instance.
    /// Provider is lazy-loaded on first access and cached for reuse.
    /// </summary>
    /// <returns>Active provider instance</returns>
    /// <exception cref="InvalidOperationException">No active provider configured</exception>
    ILLMProvider GetActiveProvider();

    /// <summary>
    /// Changes the active provider and persists selection to configuration.
    /// </summary>
    /// <param name="configName">Provider configuration name</param>
    /// <exception cref="ArgumentException">Provider not found in configuration</exception>
    Task SetActiveProviderAsync(string configName);

    /// <summary>
    /// Gets information about all available providers.
    /// </summary>
    /// <returns>List of provider metadata</returns>
    IReadOnlyList<ProviderInfo> GetAvailableProviders();

    /// <summary>
    /// Gets information about the currently active provider.
    /// </summary>
    /// <returns>Active provider metadata</returns>
    ProviderInfo GetCurrentProviderInfo();
}
```

### ProviderInfo

**Location**: `TransparentAiAgentCore/Domain/Configuration/ProviderInfo.cs`

```csharp
public class ProviderInfo
{
    /// <summary>Configuration name (e.g., "claude-fast")</summary>
    public string ConfigName { get; }

    /// <summary>Display name shown in UI (e.g., "Claude Haiku (Fast)")</summary>
    public string DisplayName { get; }

    /// <summary>Provider type (e.g., "Anthropic", "AzureOpenAI")</summary>
    public string ProviderType { get; }

    /// <summary>Model/deployment name</summary>
    public string ModelName { get; }

    /// <summary>Whether this is the currently active provider</summary>
    public bool IsActive { get; }
}
```

---

## Implementation

### LLMProviderManager

**Location**: `TransparentAiAgentCore/Infrastructure/LLM/LLMProviderManager.cs`

#### Key Components

```csharp
public class LLMProviderManager : ILLMProviderManager, IDisposable
{
    private readonly LLMConfiguration _configuration;
    private readonly ILLMProviderFactory _factory;
    private readonly Dictionary<string, ILLMProvider> _providerCache;
    private readonly object _lock;
    private string _activeProviderName;
}
```

#### Lazy Loading

Providers are created on-demand when first accessed:

```csharp
public ILLMProvider GetActiveProvider()
{
    lock (_lock)
    {
        // Check cache first
        if (!_providerCache.TryGetValue(_activeProviderName, out var provider))
        {
            // Lazy load: Create provider only when needed
            var config = _configuration.Providers[_activeProviderName];
            provider = _factory.CreateProvider(_activeProviderName, config);
            _providerCache[_activeProviderName] = provider;
        }
        return provider;
    }
}
```

**Benefits**:
- Faster application startup (don't create all providers)
- Lower memory usage (only active providers loaded)
- Network connections established only when needed

#### Provider Switching

```csharp
public async Task SetActiveProviderAsync(string configName)
{
    // Validate provider exists
    if (!_configuration.Providers.ContainsKey(configName))
        throw new ArgumentException($"Provider '{configName}' not found");

    lock (_lock)
    {
        _activeProviderName = configName;
        _configuration.ActiveProvider = configName;
    }

    // Persist to configuration file
    await _configurationService.UpdateActiveProviderAsync(configName);
}
```

**Persistence**: Active provider selection saved to `appsettings.json` for persistence across application restarts.

#### Thread Safety

All provider cache operations protected by lock:
- Provider creation (lazy-load)
- Provider retrieval
- Active provider changes

#### Resource Management

Implements `IDisposable` for proper cleanup:

```csharp
public void Dispose()
{
    lock (_lock)
    {
        foreach (var provider in _providerCache.Values.OfType<IDisposable>())
        {
            provider.Dispose();
        }
        _providerCache.Clear();
    }
}
```

---

## Usage Examples

### Basic Usage (Application Layer)

```csharp
public class AgentOrchestrator
{
    private readonly ILLMProviderManager _providerManager;

    public async Task<string> ProcessRequestAsync(string userMessage)
    {
        // Get active provider (lazy-loaded and cached)
        var provider = _providerManager.GetActiveProvider();

        // Use provider
        var response = await provider.SendRequestAsync(request);
        return response.Content;
    }
}
```

### Switching Providers

```csharp
public class ProviderStateService
{
    private readonly ILLMProviderManager _providerManager;

    public async Task ChangeProviderAsync(string configName)
    {
        // Switch to different provider
        await _providerManager.SetActiveProviderAsync(configName);

        // Notify UI
        OnProviderChanged?.Invoke();
    }
}
```

### Listing Available Providers (for UI)

```csharp
public class ProviderSelectorComponent
{
    private readonly ILLMProviderManager _providerManager;
    private List<ProviderInfo> _providers;

    protected override void OnInitialized()
    {
        // Get all configured providers
        _providers = _providerManager.GetAvailableProviders().ToList();

        // Find active provider
        var activeProvider = _providers.FirstOrDefault(p => p.IsActive);
    }
}
```

---

## Configuration

Provider Manager is registered in dependency injection during application startup:

**File**: `TransparentAiAgentBlazor/Program.cs`

```csharp
// Load LLM configuration
var llmConfig = builder.Configuration
    .GetSection("TransparentAiAgent:LLM")
    .Get<LLMConfiguration>();

// Validate all provider configurations
var validator = new ProviderConfigValidator();
foreach (var (name, providerConfig) in llmConfig.Providers)
{
    var validationResult = validator.Validate(providerConfig);
    if (!validationResult.IsValid)
        throw new ConfigurationException($"Invalid config for '{name}': {string.Join(", ", validationResult.Errors)}");
}

// Register Provider Manager
builder.Services.AddSingleton(llmConfig);
builder.Services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddSingleton<ILLMProviderManager, LLMProviderManager>();

// Backward compatibility: Provide ILLMProvider for existing consumers
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var manager = sp.GetRequiredService<ILLMProviderManager>();
    return manager.GetActiveProvider();
});
```

---

## Provider Lifecycle

### State Diagram

```
Provider States:
┌──────────────┐
│  Configured  │ (in appsettings.json)
└──────┬───────┘
       │
       │ First GetActiveProvider() call
       ▼
┌──────────────┐
│   Creating   │ (Factory.CreateProvider)
└──────┬───────┘
       │
       │ Creation complete
       ▼
┌──────────────┐
│    Cached    │ (Stored in dictionary)
└──────┬───────┘
       │
       │ Application shutdown
       ▼
┌──────────────┐
│   Disposed   │ (Cleanup resources)
└──────────────┘
```

### Lifecycle Events

| Event | Action | Performance Impact |
|-------|--------|-------------------|
| **Application Startup** | Load configuration, validate structure | ~50ms |
| **First GetActiveProvider()** | Create provider instance, establish connection | ~200-500ms |
| **Subsequent GetActiveProvider()** | Return cached instance | <1ms |
| **SetActiveProviderAsync()** | Update in-memory state, persist to file | ~10-50ms |
| **Switch to Cached Provider** | Return existing instance | <1ms |
| **Switch to New Provider** | Lazy-load on next GetActiveProvider() | ~200-500ms |
| **Application Shutdown** | Dispose all cached providers | ~50-100ms |

---

## Error Handling

### Common Errors

#### No Active Provider Configured

```csharp
// Thrown when: ActiveProvider is null or empty
throw new InvalidOperationException("No active provider configured");

// Solution: Set ActiveProvider in appsettings.json
```

#### Provider Not Found

```csharp
// Thrown when: SetActiveProviderAsync() called with invalid name
throw new ArgumentException($"Provider '{configName}' not found in configuration");

// Solution: Use valid provider name from GetAvailableProviders()
```

#### Provider Creation Failed

```csharp
// Thrown when: Factory.CreateProvider() fails
throw new ConfigurationException($"Failed to create provider '{configName}': {ex.Message}");

// Common causes: Invalid API key, network issues, invalid endpoint
```

### Error Recovery

Provider Manager handles errors gracefully:
- **Creation failures**: Logged, exception propagated to caller
- **Cache corruption**: Lock ensures consistency
- **Concurrent access**: Thread-safe via locking

---

## Testing

### Unit Tests

**Location**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/LLMProviderManagerTests.cs`

**Coverage**:
- ✅ Lazy loading behavior
- ✅ Provider caching
- ✅ Provider switching
- ✅ Thread safety (concurrent GetActiveProvider calls)
- ✅ Error cases (invalid provider, missing config)
- ✅ Resource disposal

**Example Test**:

```csharp
[TestMethod]
public void GetActiveProvider_FirstCall_CreatesAndCachesProvider()
{
    // Arrange
    var mockFactory = new Mock<ILLMProviderFactory>();
    var mockProvider = new Mock<ILLMProvider>();
    mockFactory.Setup(f => f.CreateProvider(It.IsAny<string>(), It.IsAny<ProviderConfig>()))
        .Returns(mockProvider.Object);

    var manager = new LLMProviderManager(config, mockFactory.Object);

    // Act
    var provider1 = manager.GetActiveProvider();
    var provider2 = manager.GetActiveProvider();

    // Assert
    Assert.AreSame(provider1, provider2); // Same cached instance
    mockFactory.Verify(f => f.CreateProvider(It.IsAny<string>(), It.IsAny<ProviderConfig>()), Times.Once);
}
```

### Integration Tests

Tests with real provider configurations:
- Provider switching with real API calls
- Persistence to appsettings.json
- UI integration with ProviderSelector component

---

## Performance Characteristics

| Operation | Time Complexity | Notes |
|-----------|----------------|-------|
| GetActiveProvider() (cached) | O(1) | Dictionary lookup |
| GetActiveProvider() (not cached) | O(1) + provider creation | ~200-500ms for network |
| SetActiveProviderAsync() | O(1) + file I/O | ~10-50ms for persistence |
| GetAvailableProviders() | O(n) | n = number of providers |

**Memory Usage**:
- Per provider instance: ~1-5 MB (HTTP clients, buffers)
- Typical deployment (3-5 providers): ~5-25 MB total
- Negligible for modern systems

---

## Related Documentation

- **User Guide**: [LLM Provider Selector Guide](../../05-guides/deployment/llm-provider-selector.md)
- **Provider Abstraction**: [provider-abstraction.md](provider-abstraction.md)
- **Provider Factory**: [LLM Components README](README.md#provider-selection)
- **Configuration Service**: [configuration-service.md](../infrastructure/configuration-service.md)
- **Design Decision**: [DD-026](../../02-architecture/design-decisions.md#dd-026-llm-provider-selector-architecture)

---

## Design Rationale

**Why Provider Manager Pattern?**
- ✅ Clean separation: routing (manager) vs creation (factory) vs execution (providers)
- ✅ Testable: Easy to mock ILLMProviderManager in tests
- ✅ Flexible: Can add caching, pooling, health checks without affecting consumers
- ✅ Thread-safe: Centralized locking strategy

**Why Lazy Loading?**
- ✅ Faster startup: Don't create unused providers
- ✅ Lower resource usage: Only active providers consume memory/connections
- ✅ On-demand initialization: Authentication and network only when needed

**Why Caching?**
- ✅ Performance: Avoid recreating HTTP clients and re-authenticating
- ✅ Resource efficiency: Reuse existing connections
- ✅ Consistent state: Same provider instance across requests

**Alternatives Considered**:
- ❌ Scoped providers: Lifecycle issues, more complex
- ❌ Singleton per provider: Less flexible, harder to manage lifecycle
- ❌ Factory only: No caching, no centralized management

---

## Future Enhancements

Potential features (not currently implemented):
- [ ] Provider health checks
- [ ] Automatic failover to backup provider
- [ ] Provider usage statistics
- [ ] Concurrent provider execution (load balancing)
- [ ] Provider-specific middleware/interceptors

**Note**: These features were explicitly decided against in initial implementation to maintain transparency principle (user always knows which provider is used). See [LLM_SELECTOR_DESIGN.md](../../../LLM_SELECTOR_DESIGN.md#decision-9-advanced-multi-provider-features-) for rationale.

---

**Last Updated**: 2025-11-16
**Implementation Status**: ✅ Complete (872+ tests passing)
