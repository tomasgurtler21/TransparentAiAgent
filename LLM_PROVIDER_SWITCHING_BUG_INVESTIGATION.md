# LLM Provider Switching Bug Investigation

**Date**: 2025-11-16
**Issue**: LLM provider selection feature not working - all requests go to first provider regardless of user selection
**Status**: Root cause identified, fix implemented

---

## Executive Summary

The LLM provider switching feature is not working because `ILLMProvider` is registered as a **Singleton** that resolves the active provider **once at startup** and never updates when the user changes the selection in the UI.

**Root Cause**: Dependency Injection lifetime mismatch
**Impact**: Provider switching appears to work in UI but has no effect on actual LLM requests
**Fix**: Implemented proxy pattern to delegate to current active provider on each request

---

## Investigation Details

### 1. Initial Symptoms

User reported:
- ✅ Configuration loading works (3 providers configured: 2x Anthropic, 1x OpenAI)
- ✅ No errors in console
- ✅ LLM selector UI shows all 3 entries
- ❌ Despite selection, all requests go to first provider (visible in Transparency viewer)

### 2. Architecture Review

The LLM provider selection system consists of:

```
[UI] ProviderSelector
  ↓
[Service] ProviderStateService
  ↓
[Manager] LLMProviderManager (manages multiple providers)
  ↓
[Factory] LLMProviderFactory (creates provider instances)
  ↓
[Provider] ILLMProvider (actual LLM implementation)
```

### 3. Code Flow Analysis

#### Startup (Program.cs:267-271)
```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var manager = sp.GetRequiredService<ILLMProviderManager>();
    return manager.GetActiveProvider();  // ⚠️ Called ONCE at startup
});
```

**Problem**:
- `ILLMProvider` is registered as **Singleton**
- The factory lambda executes **once** when DI first resolves `ILLMProvider`
- It calls `manager.GetActiveProvider()` which returns the **first** provider
- This provider instance is **cached forever** by the DI container

#### User Changes Provider (UI → Manager)
```csharp
// ProviderStateService.cs:43-49
public async Task ChangeProviderAsync(string configName)
{
    await _providerManager.SetActiveProviderAsync(configName);
    OnProviderChanged();  // UI updates ✅
}

// LLMProviderManager.cs:52-66
public Task SetActiveProviderAsync(string configName)
{
    // ... validation ...
    lock (_lock)
    {
        _activeProviderName = configName;  // Updates internal state ✅
        _configuration.ActiveProvider = configName;  // Updates config ✅
    }
    return Task.CompletedTask;
}
```

**Result**: Manager's internal state updates, but the **Singleton ILLMProvider** still references the old provider.

#### AgentOrchestrator Uses Provider
```csharp
// AgentOrchestrator.cs:22, 34, 102
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ILLMProvider _llmProvider;  // Injected once at construction

    public AgentOrchestrator(ILLMProvider llmProvider, ...)
    {
        _llmProvider = llmProvider;  // ⚠️ First provider, never changes
    }

    private async Task<IMessage> ProcessWithToolLoopAsync(...)
    {
        var llmResponse = await _llmProvider.SendRequestAsync(...);  // ⚠️ Always uses first provider
    }
}
```

**Problem**: `AgentOrchestrator` is **Scoped**, but it injects the **Singleton** `ILLMProvider` which was resolved once and never changes.

### 4. Execution Timeline

```
[Startup]
1. DI resolves ILLMProvider singleton
2. Calls manager.GetActiveProvider() → Returns Provider #1 (claude-fast)
3. Provider #1 cached by DI container forever

[User Switches to Provider #2]
4. UI calls ProviderStateService.ChangeProviderAsync("azure-gpt4")
5. Calls manager.SetActiveProviderAsync("azure-gpt4")
6. Manager updates _activeProviderName = "azure-gpt4" ✅
7. Manager updates _configuration.ActiveProvider = "azure-gpt4" ✅
8. UI updates to show "azure-gpt4" as active ✅

[User Sends Message]
9. New AgentOrchestrator scope created
10. DI injects ILLMProvider → Gets cached Provider #1 ❌
11. AgentOrchestrator calls _llmProvider.SendRequestAsync()
12. Request goes to Provider #1 (claude-fast) ❌
13. Transparency viewer shows Provider #1 ❌
```

---

## Root Cause

**The Bug**: Singleton lifetime + early resolution = stale provider reference

```csharp
// PROBLEMATIC CODE (Program.cs:267-271)
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var manager = sp.GetRequiredService<ILLMProviderManager>();
    return manager.GetActiveProvider();  // ⚠️ Executes ONCE, not on every injection
});
```

**Why This Doesn't Work**:
1. `AddSingleton` with factory lambda means the factory runs **once** and caches the result
2. `manager.GetActiveProvider()` returns the provider that was active **at startup**
3. When user changes provider, the manager's internal state updates, but the **cached singleton** never changes
4. AgentOrchestrator always gets the same stale provider instance

---

## Solution Options

### Option A: Change to Scoped ❌
```csharp
builder.Services.AddScoped<ILLMProvider>(sp =>
{
    var manager = sp.GetRequiredService<ILLMProviderManager>();
    return manager.GetActiveProvider();
});
```

**Pros**: Simple change
**Cons**:
- Creates new provider instance per scope (wasteful, providers should be cached)
- Doesn't solve the problem - scoped instance still resolved once per request

### Option B: Proxy Pattern ✅ (RECOMMENDED)
```csharp
// Create proxy that delegates to current active provider on EVERY call
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var manager = sp.GetRequiredService<ILLMProviderManager>();
    return new DelegatingLLMProvider(manager);
});

// DelegatingLLMProvider.cs
public class DelegatingLLMProvider : ILLMProvider
{
    private readonly ILLMProviderManager _manager;

    public DelegatingLLMProvider(ILLMProviderManager manager)
    {
        _manager = manager;
    }

    public Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken)
    {
        var activeProvider = _manager.GetActiveProvider();  // ✅ Gets CURRENT active provider
        return activeProvider.SendRequestAsync(request, cancellationToken);
    }

    // ... delegate all other methods similarly
}
```

**Pros**:
- ✅ Backward compatible - no changes to consumers
- ✅ Provider switching works correctly
- ✅ Providers still cached by manager (efficient)
- ✅ Clean separation of concerns

**Cons**:
- Requires new proxy class
- Slight indirection overhead (negligible)

### Option C: Refactor Consumers ❌
```csharp
// Change AgentOrchestrator to inject ILLMProviderManager
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ILLMProviderManager _providerManager;

    public AgentOrchestrator(ILLMProviderManager providerManager, ...)
    {
        _providerManager = providerManager;
    }

    private async Task<IMessage> ProcessWithToolLoopAsync(...)
    {
        var provider = _providerManager.GetActiveProvider();  // ✅ Gets current provider
        var llmResponse = await provider.SendRequestAsync(...);
    }
}
```

**Pros**:
- Most direct solution
- No proxy needed

**Cons**:
- ❌ Requires changing all consumers (AgentOrchestrator, tests, etc.)
- ❌ This was explicitly deferred in Step 8 of implementation plan
- ❌ More invasive change

---

## Recommended Fix: Option B (Proxy Pattern)

Implemented `DelegatingLLMProvider` class that:
1. Wraps `ILLMProviderManager`
2. Delegates all `ILLMProvider` method calls to `manager.GetActiveProvider()`
3. Ensures **current** active provider is used on **every** request

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/LLM/DelegatingLLMProvider.cs`

```csharp
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Proxy that delegates all ILLMProvider calls to the currently active provider.
/// Ensures provider switching works correctly with singleton DI registration.
/// </summary>
public class DelegatingLLMProvider : ILLMProvider
{
    private readonly ILLMProviderManager _manager;

    public DelegatingLLMProvider(ILLMProviderManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var activeProvider = _manager.GetActiveProvider();
        return activeProvider.SendRequestAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<StreamingChunk> StreamRequestAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var activeProvider = _manager.GetActiveProvider();
        return activeProvider.StreamRequestAsync(request, cancellationToken);
    }
}
```

**File**: `TransparentAiAgentGui/Program.cs` (lines 267-271)

**BEFORE**:
```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var manager = sp.GetRequiredService<ILLMProviderManager>();
    return manager.GetActiveProvider();  // ❌ Called once
});
```

**AFTER**:
```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var manager = sp.GetRequiredService<ILLMProviderManager>();
    return new DelegatingLLMProvider(manager);  // ✅ Delegates to current provider
});
```

---

## Testing Plan

### Manual Testing

1. **Startup Check**
   - ✅ Verify app starts without errors
   - ✅ Verify first provider is active
   - ✅ Send test message → Verify it uses first provider (check Transparency viewer)

2. **Switch to Second Provider**
   - ✅ Select second provider in UI dropdown
   - ✅ Verify UI updates
   - ✅ Send test message → Verify it uses **second** provider (check Transparency viewer)

3. **Switch to Third Provider**
   - ✅ Select third provider in UI dropdown
   - ✅ Verify UI updates
   - ✅ Send test message → Verify it uses **third** provider (check Transparency viewer)

4. **Switch Back to First Provider**
   - ✅ Select first provider again
   - ✅ Send test message → Verify it uses **first** provider

### Automated Testing

Create test: `DelegatingLLMProviderTests.cs`

```csharp
[TestClass]
public class DelegatingLLMProviderTests
{
    [TestMethod]
    public async Task SendRequestAsync_UsesCurrentActiveProvider()
    {
        // Arrange
        var mockManager = new Mock<ILLMProviderManager>();
        var mockProvider1 = new Mock<ILLMProvider>();
        var mockProvider2 = new Mock<ILLMProvider>();

        var response1 = new LLMResponse { Content = "Response from Provider 1" };
        var response2 = new LLMResponse { Content = "Response from Provider 2" };

        mockProvider1.Setup(p => p.SendRequestAsync(It.IsAny<LLMRequest>(), default))
            .ReturnsAsync(response1);
        mockProvider2.Setup(p => p.SendRequestAsync(It.IsAny<LLMRequest>(), default))
            .ReturnsAsync(response2);

        // Initially return provider 1
        mockManager.Setup(m => m.GetActiveProvider()).Returns(mockProvider1.Object);

        var delegatingProvider = new DelegatingLLMProvider(mockManager.Object);
        var request = new LLMRequest(new List<LLMMessage>());

        // Act - First call
        var result1 = await delegatingProvider.SendRequestAsync(request);

        // Assert - Should use provider 1
        Assert.AreEqual("Response from Provider 1", result1.Content);

        // Switch to provider 2
        mockManager.Setup(m => m.GetActiveProvider()).Returns(mockProvider2.Object);

        // Act - Second call
        var result2 = await delegatingProvider.SendRequestAsync(request);

        // Assert - Should use provider 2 (NOT cached provider 1)
        Assert.AreEqual("Response from Provider 2", result2.Content);
    }
}
```

---

## Files Modified

1. **NEW**: `TransparentAiAgentCore/Infrastructure/LLM/DelegatingLLMProvider.cs`
   - Proxy implementation

2. **MODIFIED**: `TransparentAiAgentGui/Program.cs` (line ~270)
   - Changed `ILLMProvider` registration to use `DelegatingLLMProvider`

3. **NEW**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/DelegatingLLMProviderTests.cs`
   - Unit tests for proxy

4. **NEW**: `LLM_PROVIDER_SWITCHING_BUG_INVESTIGATION.md` (this file)
   - Investigation documentation

---

## Related Documentation

- **Implementation Plan**: `LLM_SELECTOR_IMPLEMENTATION_PLAN.md`
  - Step 8 notes backward compatibility approach
  - This bug is a consequence of that decision

- **Design Document**: `LLM_SELECTOR_DESIGN.md`
  - Original design for provider switching

- **User Guide**: `docs/05-guides/deployment/llm-provider-selector.md`
  - User-facing documentation

---

## Lessons Learned

1. **DI Lifetime Matters**: Singleton + factory lambda = one-time execution
2. **Backward Compatibility Trade-offs**: Attempting to maintain `ILLMProvider` registration for backward compatibility introduced subtle bug
3. **Proxy Pattern**: Effective solution for runtime behavior changes with singleton services
4. **Testing**: Need integration tests that verify provider switching end-to-end

---

## Verification Steps

After fix is deployed:

```bash
# 1. Start application
dotnet run --project TransparentAiAgentGui

# 2. Open browser to http://localhost:5XXX

# 3. Open Transparency Viewer (should show provider events)

# 4. Send message "Hello" → Check which provider handled it

# 5. Switch provider in dropdown

# 6. Send message "Hello again" → Verify DIFFERENT provider handled it

# 7. Check Transparency Viewer events to confirm provider switching
```

---

**Status**: Fix implemented and ready for testing
**Next Steps**: Manual testing, then automated test creation, then commit & push
