# LLM Selector Design Discussion

**Created**: 2025-11-15
**Status**: Initial Design Phase
**Related Components**: LLMProviderFactory, ILLMProvider, Configuration UI

---

## 🎯 Goal

Allow users to define multiple LLM providers in `appsettings.json` and select between them via a dropdown in the UI without requiring application restart.

---

## 📋 Current State Analysis

### Provider Implementation
- **Providers Implemented**: 3 providers (Anthropic, AzureOpenAI, OpenAI)
- **Location**: `TransparentAiAgentCore/Infrastructure/LLM/`
- **Abstraction**: `ILLMProvider` interface
- **Factory**: `LLMProviderFactory` with string-based provider creation

### Current Configuration Structure
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "Anthropic",  // Single active provider
      "Temperature": 0.7,
      "TopP": 1.0,
      "MaxTokens": 4096,

      // All provider configs present (only active one used)
      "Anthropic": {
        "ApiKey": "...",
        "Model": "claude-haiku-4-5-20251001",
        "ExtendedThinking": { ... }
      },
      "AzureOpenAI": {
        "Endpoint": "...",
        "DeploymentName": "gpt-4",
        "ApiKey": "...",
        ...
      },
      "OpenAI": {
        "ApiKey": "...",
        "Model": "gpt-4o"
      }
    }
  }
}
```

### Current Limitations
1. ❌ Only one provider active at a time
2. ❌ Provider selection at startup only (requires app restart)
3. ❌ String-based provider matching (case-insensitive, but fragile)
4. ❌ No UI for provider selection (Configuration page exists but doesn't show dropdown)
5. ❌ Shared parameters (Temperature, TopP, MaxTokens) apply to all providers

---

## 💡 User's Proposed Idea

### Core Concept
- Define multiple providers in `appsettings.json` simultaneously (may already be possible ✅)
- Add UI dropdown selector to pick active provider
- First provider in config = default option
- Combine provider name + model name = unique identifier

### My Initial Review

✅ **What's Good:**
- Aligns with existing architecture (all provider configs already coexist)
- Factory pattern supports this naturally
- Provider + Model combination is already how system works internally

⚠️ **Potential Issues Identified:**
1. **Unique Identifier Assumption**: Provider + Model may NOT always be unique
2. **Shared Parameters Problem**: Current design has shared Temperature/TopP/MaxTokens
3. **Dynamic Switching Complexity**: Singleton provider registration in DI
4. **Configuration Validation**: Currently only validates active provider's config
5. **Authentication State**: Provider clients hold authentication state

---

## 🔍 Detailed Analysis & Design Problems

### Problem 1: Unique Identifier Concerns

**User's Assumption**: "Provider + Model = Unique Identifier"

**Reality Check**:
- ✅ Works for Anthropic + OpenAI (different model naming)
- ❌ **Fails for Azure OpenAI**: Multiple Azure deployments can have same deployment name
  - Example:
    - Azure East US: `gpt-4` deployment
    - Azure West EU: `gpt-4` deployment
    - Both would have identifier: `AzureOpenAI:gpt-4`

**Proposal**: Use **Provider Configuration Name** as unique identifier instead
```json
{
  "LLM": {
    "Providers": {
      "claude-fast": {
        "Type": "Anthropic",
        "Model": "claude-haiku-4-5-20251001",
        "ApiKey": "...",
        "DisplayName": "Claude Haiku (Fast)",
        ...
      },
      "claude-powerful": {
        "Type": "Anthropic",
        "Model": "claude-opus-4-5",
        "ApiKey": "...",
        "DisplayName": "Claude Opus (Powerful)",
        ...
      },
      "azure-gpt4-eastus": {
        "Type": "AzureOpenAI",
        "DeploymentName": "gpt-4",
        "Endpoint": "https://eastus.openai.azure.com/",
        "DisplayName": "GPT-4 (East US)",
        ...
      },
      "azure-gpt4-westeu": {
        "Type": "AzureOpenAI",
        "DeploymentName": "gpt-4",
        "Endpoint": "https://westeu.openai.azure.com/",
        "DisplayName": "GPT-4 (West EU)",
        ...
      }
    },
    "ActiveProvider": "claude-fast",  // Default/current selection
    "SharedParameters": {
      "Temperature": 0.7,
      "TopP": 1.0,
      "MaxTokens": 4096
    }
  }
}
```

### Problem 2: Shared Parameters vs Per-Provider Parameters

**Current State**: Temperature, TopP, MaxTokens are shared across all providers

**Design Questions**:
1. Should parameters be shared across all providers?
2. Or should each provider config have its own parameters?
3. Should we support both (per-provider overrides shared defaults)?

**Proposal**: **Three-Tier Parameter System**
```json
{
  "LLM": {
    "DefaultParameters": {
      "Temperature": 0.7,
      "TopP": 1.0,
      "MaxTokens": 4096
    },
    "Providers": {
      "claude-fast": {
        "Type": "Anthropic",
        "Model": "claude-haiku-4-5-20251001",
        // Inherits from DefaultParameters (no override)
      },
      "claude-creative": {
        "Type": "Anthropic",
        "Model": "claude-opus-4-5",
        "Parameters": {
          "Temperature": 1.0,  // Override for creative tasks
          "MaxTokens": 8192
          // TopP inherits from default
        }
      }
    }
  }
}
```

### Problem 3: Dynamic Provider Switching

**Current Architecture**: Provider registered as singleton in DI container at startup

```csharp
builder.Services.AddSingleton<ILLMProvider>(sp => {
    var factory = sp.GetRequiredService<LLMProviderFactory>();
    return factory.CreateProvider();  // Called once at startup
});
```

**Challenge**: Cannot swap singleton after registration

**Design Options**:

**Option A: Scoped Provider (Simple but requires refactoring)**
```csharp
builder.Services.AddScoped<ILLMProvider>(sp => {
    var factory = sp.GetRequiredService<LLMProviderFactory>();
    var activeProvider = sp.GetRequiredService<IActiveProviderService>();
    return factory.CreateProvider(activeProvider.GetActiveProvider());
});
```
- ✅ Simple to implement
- ❌ All consumers must be scoped (ConversationManager, AgentOrchestrator)
- ❌ Potential lifecycle issues

**Option B: Provider Manager Pattern (Recommended)**
```csharp
public interface ILLMProviderManager
{
    ILLMProvider GetActiveProvider();
    void SetActiveProvider(string providerConfigName);
    IReadOnlyList<ProviderInfo> GetAvailableProviders();
}

// Usage
public class AgentOrchestrator
{
    private readonly ILLMProviderManager _providerManager;

    public async Task RunAsync()
    {
        var provider = _providerManager.GetActiveProvider();
        var response = await provider.GenerateResponseAsync(...);
    }
}
```
- ✅ Clear separation of concerns
- ✅ Easy to test
- ✅ Provider caching/pooling possible
- ✅ Thread-safe provider switching
- ❌ More code to write

**Option C: Factory Pattern Evolution (Minimal Changes)**
```csharp
// Keep factory but make it stateful
public class LLMProviderFactory
{
    private string _activeProviderName;
    private readonly Dictionary<string, ILLMProvider> _providerCache;

    public ILLMProvider GetActiveProvider() { ... }
    public void SetActiveProvider(string name) { ... }
}

// Consumers inject factory instead of ILLMProvider
public class AgentOrchestrator
{
    private readonly LLMProviderFactory _factory;

    public async Task RunAsync()
    {
        var provider = _factory.GetActiveProvider();
        ...
    }
}
```
- ✅ Minimal refactoring
- ⚠️ Breaks abstraction (consumers know about factory)
- ⚠️ Factory becomes service, not just factory

### Problem 4: Configuration Validation

**Current**: Only active provider's configuration is validated

**With Multi-Provider**: Need to validate all configured providers or only active one?

**Design Questions**:
1. Validate all providers at startup? (Slow, but catches errors early)
2. Validate only when switching? (Fast startup, but errors discovered late)
3. Validate configuration structure vs actual API connectivity?

**Proposal**: **Two-Phase Validation**
- **Startup**: Validate configuration structure for ALL providers (fast)
- **On Switch**: Validate API connectivity for selected provider (lazy)

```csharp
public class ProviderConfigValidator
{
    // Fast - checks required fields, format, etc.
    public ValidationResult ValidateConfiguration(ProviderConfig config);

    // Slow - makes test API call
    public async Task<ValidationResult> ValidateConnectivityAsync(ILLMProvider provider);
}
```

### Problem 5: UI Design & User Experience

**Dropdown Options - What to Display?**
- Provider Type only? ("Anthropic", "AzureOpenAI")
- Model name only? ("claude-haiku-4-5", "gpt-4")
- Both? ("Anthropic - Claude Haiku")
- Custom display name? ("Fast Claude", "Powerful GPT-4")

**Proposal**: Use **Custom Display Names** with provider info
```
Dropdown shows:
┌─────────────────────────────────────┐
│ Fast Claude (Anthropic Haiku)      ▼│
├─────────────────────────────────────┤
│ ✓ Fast Claude (Anthropic Haiku)    │  <- Currently active
│   Powerful Claude (Anthropic Opus)  │
│   GPT-4 East US (Azure OpenAI)      │
│   GPT-4o (OpenAI)                   │
└─────────────────────────────────────┘
```

**Where to Place Dropdown?**
- Option 1: Header/Navbar (always visible)
- Option 2: Settings/Configuration page
- Option 3: Both (navbar shows current, settings allows change)

**State Persistence**:
- Save selection to user preferences? (per-user)
- Save to appsettings.json? (application-wide)
- Session-only? (lost on refresh)

### Problem 6: Backward Compatibility

**Current Configuration Must Still Work**:
```json
{
  "LLM": {
    "Provider": "Anthropic",  // Old format
    "Anthropic": { ... }
  }
}
```

**Migration Path**:
- Detect old vs new configuration format
- Auto-migrate old format to new format on first run?
- Support both formats simultaneously?

**Proposal**: **Hybrid Support**
```csharp
public class LLMConfiguration
{
    // Old format (deprecated but supported)
    public string? Provider { get; set; }

    // New format
    public Dictionary<string, ProviderConfig>? Providers { get; set; }
    public string? ActiveProvider { get; set; }

    // Migration logic
    public Dictionary<string, ProviderConfig> GetEffectiveProviders()
    {
        if (Providers != null && Providers.Any())
            return Providers;  // New format

        // Fallback to old format
        return MigrateOldFormat();
    }
}
```

---

## 🏗️ Proposed Architecture

### New Configuration Structure (Detailed)

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "ActiveProvider": "claude-fast",

      "DefaultParameters": {
        "Temperature": 0.7,
        "TopP": 1.0,
        "MaxTokens": 4096
      },

      "Providers": {
        "claude-fast": {
          "Type": "Anthropic",
          "DisplayName": "Claude Haiku (Fast)",
          "Model": "claude-haiku-4-5-20251001",
          "ApiKey": "sk-ant-...",
          "ExtendedThinking": {
            "Enabled": false
          }
        },

        "claude-thinking": {
          "Type": "Anthropic",
          "DisplayName": "Claude Haiku (Extended Thinking)",
          "Model": "claude-haiku-4-5-20251001",
          "ApiKey": "sk-ant-...",
          "ExtendedThinking": {
            "Enabled": true,
            "BudgetTokens": 10000
          },
          "Parameters": {
            "MaxTokens": 8192  // Override for thinking mode
          }
        },

        "azure-gpt4": {
          "Type": "AzureOpenAI",
          "DisplayName": "GPT-4 (Azure East US)",
          "Endpoint": "https://eastus.openai.azure.com/",
          "DeploymentName": "gpt-4",
          "ApiVersion": "2024-02-15-preview",
          "AuthenticationMode": "ApiKey",
          "ApiKey": "..."
        },

        "openai-gpt4o": {
          "Type": "OpenAI",
          "DisplayName": "GPT-4o (OpenAI)",
          "Model": "gpt-4o",
          "ApiKey": "sk-..."
        }
      },

      // Backward compatibility (deprecated)
      "Provider": null,
      "Anthropic": null,
      "AzureOpenAI": null,
      "OpenAI": null
    }
  }
}
```

### Component Changes

#### 1. Configuration Domain Models
**New**: `ProviderConfig.cs`
```csharp
public class ProviderConfig
{
    public string Type { get; set; }  // "Anthropic", "AzureOpenAI", "OpenAI"
    public string DisplayName { get; set; }
    public Dictionary<string, object> Parameters { get; set; }  // Provider-specific
    public ParameterOverrides? ParameterOverrides { get; set; }  // Optional overrides
}

public class ParameterOverrides
{
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxTokens { get; set; }
}
```

#### 2. Provider Manager Service
**New**: `ILLMProviderManager.cs`
```csharp
public interface ILLMProviderManager
{
    ILLMProvider GetActiveProvider();
    Task SetActiveProviderAsync(string configName);
    IReadOnlyList<ProviderInfo> GetAvailableProviders();
    ProviderInfo GetCurrentProviderInfo();
}

public class ProviderInfo
{
    public string ConfigName { get; set; }
    public string DisplayName { get; set; }
    public string ProviderType { get; set; }
    public string ModelName { get; set; }
    public bool IsActive { get; set; }
}
```

#### 3. Factory Evolution
**Modified**: `LLMProviderFactory.cs`
```csharp
public class LLMProviderFactory
{
    // New: Create from ProviderConfig instead of string
    public ILLMProvider CreateProvider(string configName, ProviderConfig config)
    {
        return config.Type.ToLowerInvariant() switch
        {
            "anthropic" => CreateAnthropicProvider(config),
            "azureopenai" => CreateAzureOpenAIProvider(config),
            "openai" => CreateOpenAIProvider(config),
            _ => throw new ConfigurationException($"Unknown provider type: {config.Type}")
        };
    }

    private ILLMProvider CreateAnthropicProvider(ProviderConfig config)
    {
        // Extract Anthropic-specific parameters from config.Parameters
        var model = config.Parameters["Model"] as string;
        var apiKey = config.Parameters["ApiKey"] as string;
        // ... construct provider
    }
}
```

#### 4. UI Components

**New**: `ProviderSelector.razor`
```razor
<div class="provider-selector">
    <label>LLM Provider:</label>
    <select @bind="SelectedProvider" @bind:after="OnProviderChanged">
        @foreach (var provider in AvailableProviders)
        {
            <option value="@provider.ConfigName">
                @provider.DisplayName (@provider.ProviderType - @provider.ModelName)
            </option>
        }
    </select>
</div>

@code {
    private string SelectedProvider { get; set; }
    private List<ProviderInfo> AvailableProviders { get; set; }

    private async Task OnProviderChanged()
    {
        await ProviderManager.SetActiveProviderAsync(SelectedProvider);
        // Update UI, show notification, etc.
    }
}
```

---

## ❓ Open Questions for User

### Q1: Configuration Structure
**Should we support the new nested "Providers" structure as proposed, or modify the existing structure?**

Current proposal requires migration:
```json
// OLD
"Provider": "Anthropic"
"Anthropic": { ... }

// NEW
"Providers": {
  "claude-fast": { "Type": "Anthropic", ... }
}
```

Alternative: Keep flat structure but add aliases?

---

### Q2: Parameter Scope
**Should Temperature/TopP/MaxTokens be:**
- A) Shared across all providers (current behavior)
- B) Per-provider configuration
- C) Shared defaults with per-provider overrides (my recommendation)

---

### Q3: Provider Switching Scope
**When user switches provider, should it:**
- A) Apply to entire application (all users see new provider)
- B) Per-user preference (each user can pick their provider)
- C) Per-conversation (different conversations can use different providers)

This affects state management significantly!

---

### Q4: UI Placement
**Where should the provider selector dropdown be located?**
- A) Top navbar/header (always visible)
- B) Configuration page only
- C) Both places (navbar shows current, config page edits)

---

### Q5: Default Provider Selection
**How should default provider be determined?**
- A) First in configuration dictionary (you suggested this)
- B) Explicit "ActiveProvider" field in config (my proposal)
- C) Last used provider (stored in preferences)

Note: JSON dictionaries don't guarantee order, so option A might be fragile.

---

### Q6: Validation Strategy
**Should we validate all provider configurations at startup?**
- A) Validate all (slow startup, catch errors early)
- B) Validate only active provider (fast startup, errors discovered late)
- C) Validate config structure for all, connectivity only for active (my recommendation)

---

### Q7: Backward Compatibility
**How important is supporting the old configuration format?**
- A) Critical - must support both indefinitely
- B) Important - support for 1-2 versions then deprecate
- C) Not important - breaking change is acceptable

---

### Q8: Provider Instance Lifecycle
**Should we create provider instances:**
- A) Once at startup and cache (current behavior)
- B) On-demand when switching, then cache
- C) Fresh instance every time (no caching)

Caching is faster but holds resources (HTTP clients, auth tokens).

---

### Q9: Multi-Provider Features
**Future consideration: Should we support:**
- Provider fallback (if primary fails, try secondary)?
- Provider load balancing?
- Provider routing rules (use Claude for analysis, GPT-4 for code)?

Not needed now, but affects architecture decisions.

---

## 🎯 Recommended Implementation Phases

### Phase 1: Foundation (No UI Changes)
1. Create new configuration structure (support both old and new)
2. Implement `ILLMProviderManager` service
3. Update `LLMProviderFactory` to work with new structure
4. Add migration logic for old config format
5. Write tests for configuration loading and provider switching

**Deliverable**: Backend supports multiple providers, but still uses single provider from config

### Phase 2: Dynamic Switching
1. Refactor DI registration to use provider manager
2. Implement active provider state management
3. Add configuration validation for all providers
4. Update existing consumers (ConversationManager, AgentOrchestrator)

**Deliverable**: Can switch providers programmatically (no UI yet)

### Phase 3: UI Integration
1. Create `ProviderSelector.razor` component
2. Add to navbar or configuration page (per Q4 answer)
3. Implement provider switching UI flow
4. Add visual feedback (loading, success, error states)

**Deliverable**: Full end-to-end provider selection in UI

### Phase 4: Polish & Enhancement
1. Add provider validation indicators in UI
2. Implement configuration editor for adding/removing providers
3. Add provider health monitoring
4. Documentation and examples

---

## 📝 Next Steps

**Immediate:**
1. **User to answer open questions above** (Q1-Q9)
2. Review and approve/modify proposed architecture
3. Decide on implementation scope (all phases or incremental)

**Then:**
1. Create design decision document (DD-XXX)
2. Update component documentation
3. Begin Phase 1 implementation

---

## 🔖 References

**Related Files:**
- `TransparentAiAgentCore/Infrastructure/LLM/LLMProviderFactory.cs`
- `TransparentAiAgentCore/Domain/LLM/ILLMProvider.cs`
- `TransparentAiAgentCore/Domain/Configuration/LLMConfiguration.cs`
- `TransparentAiAgentBlazor/Program.cs:230-235` (DI registration)

**Related Documentation:**
- `docs/04-components/llm/` - LLM component docs
- `docs/02-architecture/design-decisions.md` - Design decisions

---

**Last Updated**: 2025-11-15
**Next Review**: After user answers open questions
