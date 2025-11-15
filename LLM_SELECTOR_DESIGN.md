# LLM Selector Design Discussion

**Created**: 2025-11-15
**Status**: Design Phase - 90% Complete (1 question remaining)
**Related Components**: LLMProviderFactory, ILLMProvider, Configuration UI

---

## 📋 Executive Summary

**Goal**: Enable users to define multiple LLM providers in configuration and dynamically switch between them via UI dropdown without application restart.

**Key Design Decisions Made (9/10)**:
1. ✅ **Configuration Structure**: Named provider configs (e.g., "claude-fast", "azure-gpt4-eastus") instead of provider+model identifiers
2. ✅ **Parameters**: Per-provider with optional defaults (three-tier: DefaultParameters → provider base → overrides)
3. ✅ **Architecture**: Provider Manager Pattern (lazy-load + cache)
4. ✅ **Validation**: Config structure only (no connectivity checks to save tokens)
5. ✅ **UI Placement**: Horizontal layout with ConversationSelector (conversation left, provider right)
6. ✅ **Persistence**: Explicit ActiveProvider field (⚠️ **location TBD - see Q10 below**)
7. ✅ **Backward Compatibility**: Not required (breaking changes acceptable)
8. ✅ **Instance Lifecycle**: Lazy-load on first use, then cache
9. ✅ **Advanced Features**: No auto-fallback/routing (violates transparency principle)

**🔴 Remaining Question (Q10)**: Should ActiveProvider be stored in `appsettings.json` or separate `userpreferences.json`? See Decision 6 and Q10 sections.

**Implementation**: 5 phases planned, estimated 7-12 days total effort

---

## 🎯 Original Goal

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

### New Configuration Structure (Final)

**appsettings.json** (Provider definitions and static configuration):
```json
{
  "TransparentAiAgent": {
    "LLM": {
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
          // Inherits Temperature, TopP, MaxTokens from DefaultParameters
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
            "MaxTokens": 8192  // Override for extended thinking
            // Temperature and TopP inherited from DefaultParameters
          }
        },

        "azure-gpt4-eastus": {
          "Type": "AzureOpenAI",
          "DisplayName": "GPT-4 (Azure East US)",
          "Endpoint": "https://eastus.openai.azure.com/",
          "DeploymentName": "gpt-4",
          "ApiVersion": "2024-02-15-preview",
          "AuthenticationMode": "ApiKey",
          "ApiKey": "...",
          "IsReasoningModel": false
        },

        "azure-gpt4-westeu": {
          "Type": "AzureOpenAI",
          "DisplayName": "GPT-4 (Azure West EU)",
          "Endpoint": "https://westeu.openai.azure.com/",
          "DeploymentName": "gpt-4",  // Same deployment name, different region
          "ApiVersion": "2024-02-15-preview",
          "AuthenticationMode": "DefaultAzureCredential",
          "TenantId": "...",
          "IsReasoningModel": false,
          "Parameters": {
            "Temperature": 0.5,  // Different parameters for West EU region
            "MaxTokens": 2048
          }
        },

        "openai-gpt4o": {
          "Type": "OpenAI",
          "DisplayName": "GPT-4o (OpenAI)",
          "Model": "gpt-4o",
          "ApiKey": "sk-..."
        }
      }
    }
  }
}
```

**Note on ActiveProvider**: Location TBD by Q10 answer
- **Option A**: Add `"ActiveProvider": "claude-fast"` to `appsettings.json` above
- **Option B**: Create separate `userpreferences.json` with `{ "ActiveProvider": "claude-fast" }`

---

### Configuration Examples

**Example 1: Multiple Claude Models with Different Parameters**
```json
"Providers": {
  "claude-fast": {
    "Type": "Anthropic",
    "DisplayName": "Claude Haiku (Fast & Cheap)",
    "Model": "claude-haiku-4-5-20251001",
    "ApiKey": "sk-ant-...",
    "Parameters": {
      "Temperature": 0.3,
      "MaxTokens": 2048
    }
  },
  "claude-creative": {
    "Type": "Anthropic",
    "DisplayName": "Claude Sonnet (Creative Writing)",
    "Model": "claude-sonnet-4-5-20250929",
    "ApiKey": "sk-ant-...",
    "Parameters": {
      "Temperature": 1.0,
      "MaxTokens": 8192
    }
  },
  "claude-thinking": {
    "Type": "Anthropic",
    "DisplayName": "Claude Haiku (Deep Thinking)",
    "Model": "claude-haiku-4-5-20251001",
    "ApiKey": "sk-ant-...",
    "ExtendedThinking": {
      "Enabled": true,
      "BudgetTokens": 10000
    }
  }
}
```

**Example 2: Multi-Region Azure OpenAI Setup**
```json
"Providers": {
  "azure-eastus": {
    "Type": "AzureOpenAI",
    "DisplayName": "GPT-4 East US (Primary)",
    "Endpoint": "https://my-eastus-instance.openai.azure.com/",
    "DeploymentName": "gpt-4",
    "ApiKey": "...",
    "ApiVersion": "2024-02-15-preview"
  },
  "azure-westeu": {
    "Type": "AzureOpenAI",
    "DisplayName": "GPT-4 West EU (Backup)",
    "Endpoint": "https://my-westeu-instance.openai.azure.com/",
    "DeploymentName": "gpt-4",
    "ApiKey": "...",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

**Example 3: Mixed Providers for Different Use Cases**
```json
"Providers": {
  "claude-code": {
    "Type": "Anthropic",
    "DisplayName": "Claude for Coding",
    "Model": "claude-sonnet-4-5-20250929",
    "ApiKey": "sk-ant-..."
  },
  "gpt4-analysis": {
    "Type": "OpenAI",
    "DisplayName": "GPT-4o for Analysis",
    "Model": "gpt-4o",
    "ApiKey": "sk-..."
  },
  "azure-enterprise": {
    "Type": "AzureOpenAI",
    "DisplayName": "Azure GPT-4 (Enterprise)",
    "Endpoint": "https://corporate.openai.azure.com/",
    "DeploymentName": "gpt-4-enterprise",
    "AuthenticationMode": "DefaultAzureCredential"
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

## ✅ Design Decisions Made

### Decision 1: Configuration Structure ✅
**Approved**: New nested structure with config-name-based identifiers

```json
"LLM": {
  "Providers": {
    "claude-fast": { "Type": "Anthropic", ... },
    "azure-gpt4-eastus": { "Type": "AzureOpenAI", ... }
  }
}
```

**Rationale**:
- Each config has unique name (e.g., "claude-fast", "azure-gpt4-eastus")
- "Type" field specifies provider type ("Anthropic", "AzureOpenAI", "OpenAI")
- Solves the Azure multi-deployment issue elegantly
- User approved this approach

**Naming Note**: User suggested potentially renaming "Providers" to "LLM" or "LLMConfig", but since we're already under "LLM" section, keeping "Providers" is clearest. Alternative names considered:
- "Configurations" - too generic
- "ConfiguredProviders" - too verbose
- "Providers" - ✅ **Recommended** (clear, concise, accurate)

---

### Decision 2: Parameter Scope ✅
**Approved**: Per-provider parameters with optional defaults

**User's Requirement**: "Definitely per-provider, gives user option to experiment easily with same model with different parameters."

**Implementation**: Three-tier system
1. **DefaultParameters** (optional, applies to all)
2. **Per-provider base config** (inherits defaults)
3. **Per-provider overrides** (optional, overrides defaults)

```json
"DefaultParameters": {
  "Temperature": 0.7,
  "TopP": 1.0,
  "MaxTokens": 4096
},
"Providers": {
  "claude-fast": {
    "Type": "Anthropic",
    "Model": "claude-haiku-4-5-20251001"
    // Inherits all from DefaultParameters
  },
  "claude-creative": {
    "Type": "Anthropic",
    "Model": "claude-opus-4-5",
    "Parameters": {
      "Temperature": 1.0,  // Override for creativity
      "MaxTokens": 8192    // Override for longer responses
      // TopP inherits from DefaultParameters
    }
  }
}
```

**Benefits**:
- DRY principle (don't repeat common values)
- Easy experimentation with different parameters for same model
- Clear override semantics

---

### Decision 3: Provider Switching Architecture ✅
**Approved**: Option B - Provider Manager Pattern

**User's Choice**: "I like option B. Easily testable is nice thing to have, also looks like most flexible for the future."

**Scope Clarification**: User clarified this is a **single-user, single-conversation application**. Provider selection is **global/application-wide**. No need for per-user or per-conversation provider selection.

**Implementation**:
```csharp
public interface ILLMProviderManager
{
    ILLMProvider GetActiveProvider();
    Task SetActiveProviderAsync(string configName);
    IReadOnlyList<ProviderInfo> GetAvailableProviders();
    ProviderInfo GetCurrentProviderInfo();
}
```

**Benefits**:
- ✅ Clean separation of concerns
- ✅ Easy to test (mockable interface)
- ✅ Flexible for future enhancements
- ✅ Thread-safe provider switching
- ✅ Supports provider caching/pooling

---

### Decision 4: Validation Strategy ✅
**Approved**: Validate config structure only, NOT connectivity

**User's Requirement**: "Validate all, but only config, not connectivity, that wastes tokens"

**Implementation**:
- **Startup**: Validate configuration structure for ALL providers (fast, catches config errors)
  - Check required fields present (ApiKey, Model, Endpoint, etc.)
  - Validate field formats (URLs, enum values)
  - Type consistency checks
- **Runtime**: NO automatic connectivity validation
- **On First Use**: If provider fails, show user-friendly error with validation suggestions

**Rationale**:
- Connectivity checks waste API tokens/calls
- Config validation is fast and catches 90% of errors
- Real errors surface naturally when user tries to use provider
- User can manually test provider after configuration

---

### Decision 5: UI Placement & Display ✅
**Approved**: Custom config names in dropdown, placed horizontally with ConversationSelector

**Dropdown Display**: Use custom config names (DisplayName field)
- Shows meaningful names like "Claude Haiku (Fast)" instead of technical IDs
- User confirmed: "Custom provider name is best and sufficient"

**UI Layout**:
```
+--------------------------------------------------+
| Transparent AI Agent                    [Clear] |
+--------------------------------------------------+
| [Conversation Selector ▼]  [Provider Selector ▼]|
|  (left side)                (right side)        |
+--------------------------------------------------+
| Chat messages...                                 |
+--------------------------------------------------+
```

**User's Specification**: "Selector should be placed at level where ConversationSelector is, maybe its possible to place it next to it? Conversation on the left, selector on the right"

**Current Layout** (from Home.razor:25):
- ConversationSelector is between chat-header and MessageList
- Need to create horizontal layout with two selectors side-by-side

---

### Decision 6: Default Provider & Persistence ✅
**Approved**: Explicit "ActiveProvider" field, persisted to config on change

**User's Requirement**: "Go for your suggestion - store it in the config, most likely we will store last used model there on app closeup"

**Implementation Approach**:
```json
"LLM": {
  "ActiveProvider": "claude-fast",  // Persisted, updated when user changes
  "Providers": { ... }
}
```

**⚠️ Critical Design Question**: Should we write to `appsettings.json` at runtime?

**Standard Practice**: `appsettings.json` is typically for **deployment configuration**, not runtime state.

**Recommendation**: Use separate **user preferences file**
```
appsettings.json           → Provider definitions (static, deployment config)
userpreferences.json       → ActiveProvider, UI state (dynamic, runtime state)
```

**Pros of separate file**:
- ✅ Follows .NET conventions
- ✅ appsettings.json can be read-only in production
- ✅ Easier to reset to defaults (delete userpreferences.json)
- ✅ Can .gitignore user preferences

**Cons**:
- ❌ Two files to manage
- ❌ Slightly more complex

**Alternative**: Write to `appsettings.json` directly
- ✅ Single file, simpler
- ❌ Violates conventions
- ❌ Harder to separate deployment config from user state

**User to decide**: Single file (appsettings.json) or dual file (appsettings.json + userpreferences.json)?

---

### Decision 7: Backward Compatibility ✅
**Approved**: NOT REQUIRED - breaking changes acceptable

**User's Statement**: "Backwards compatibility is not concern at all, there was no release of this app yet."

**Impact**:
- ✅ No migration code needed
- ✅ Simpler implementation
- ✅ Can remove old configuration structure entirely
- ✅ Cleaner codebase

**Action**: Remove all backward compatibility code from proposal

---

### Decision 8: Provider Instance Lifecycle ✅
**Approved**: Lazy-load on first use, then cache

**User's Response**: "Tbh I do not really know, I suppose both options have negligible performance impact. This is classic more memory used vs slower performance. I suppose there is no difference in security, does not matter if we cache all keys at once, instead having only single one cached?"

**My Recommendation**: **Lazy-load + Cache (Option B)**

**Rationale**:
1. **Performance**: Creating providers is relatively expensive
   - HTTP client initialization
   - Potential authentication setup (especially Azure AD)
   - Better to do once and reuse

2. **Memory**: Negligible impact
   - 3-5 provider instances max
   - HTTP clients are reusable (SocketsHttpHandler)
   - Modern systems can easily handle this

3. **Security**: No difference
   - User is correct - all API keys are in memory regardless
   - Caching all vs one-at-a-time has same security profile
   - Config is already loaded into memory

4. **Lazy-load benefit**: Only create providers user actually uses
   - If user has 5 providers configured but only uses 2, we only create 2
   - Saves initial startup time

**Implementation**:
```csharp
public class LLMProviderManager
{
    private readonly Dictionary<string, ILLMProvider> _providerCache = new();

    public ILLMProvider GetActiveProvider()
    {
        if (!_providerCache.TryGetValue(_activeProviderName, out var provider))
        {
            provider = _factory.CreateProvider(_activeProviderName, config);
            _providerCache[_activeProviderName] = provider;
        }
        return provider;
    }
}
```

**Disposal**: Implement `IDisposable` to properly clean up HTTP clients on app shutdown

---

### Decision 9: Advanced Multi-Provider Features ✅
**Approved**: NO automatic fallback, routing, or load balancing

**User's Requirement**: "I see no reason for this. If provider fails, user have to select different one manually. Used provider is up to him always, no auto routing should be done - it goes against main principle of app, transparency."

**Rationale**:
- ✅ Aligns with core transparency principle
- ✅ User always knows exactly which provider is being used
- ✅ No hidden automatic decisions
- ✅ Simpler implementation

**Future Consideration**: If these features are needed later, architecture should not prevent them, but they won't be implemented now.

---

## ❓ Remaining Open Questions

### Q10: Configuration Persistence Location 🔴 **NEEDS ANSWER**
**Should ActiveProvider be stored in:**
- **Option A**: `appsettings.json` (single file, simpler, non-standard)
- **Option B**: Separate `userpreferences.json` (dual file, follows .NET conventions)

See Decision 6 for full analysis.

This is the only remaining design question before implementation can begin.

---

## 🎯 Implementation Phases (Based on Decisions)

### Phase 1: Configuration & Domain Models
**Goal**: Establish new configuration structure and domain models

**Tasks**:
1. Create new configuration classes:
   - `ProviderConfig` - Base class for all provider configs
   - `LLMConfiguration` - Updated to support Providers dictionary
   - `ProviderParameters` - Temperature, TopP, MaxTokens
   - Update existing `AnthropicConfiguration`, `AzureOpenAIConfiguration`, `OpenAIConfiguration`

2. Implement configuration loading:
   - Load Providers dictionary from appsettings.json
   - Load DefaultParameters (optional)
   - Implement parameter inheritance/override logic
   - **Decision Point**: Load ActiveProvider from appsettings.json or userpreferences.json (Q10)

3. Configuration validation:
   - `ProviderConfigValidator` - Validates structure of all provider configs
   - Check required fields for each provider type
   - Validate formats (URLs, enum values, etc.)
   - NO connectivity checks

4. Write comprehensive tests:
   - Configuration loading tests
   - Parameter inheritance tests
   - Validation tests for each provider type
   - Edge cases (missing fields, invalid formats)

**Deliverable**: Configuration system ready, can load multiple provider configs
**Estimated Effort**: 1-2 days

---

### Phase 2: Provider Manager Service
**Goal**: Implement dynamic provider switching with lazy-loading and caching

**Tasks**:
1. Create `ILLMProviderManager` interface and implementation:
   ```csharp
   - GetActiveProvider() → ILLMProvider
   - SetActiveProviderAsync(configName) → Task
   - GetAvailableProviders() → List<ProviderInfo>
   - GetCurrentProviderInfo() → ProviderInfo
   ```

2. Implement provider lifecycle:
   - Lazy-load: Create provider on first use
   - Cache: Store in dictionary for reuse
   - Thread-safety: Lock during provider creation/switching
   - Disposal: Implement IDisposable for cleanup

3. Update `LLMProviderFactory`:
   - Modify to accept `ProviderConfig` instead of string
   - Extract provider-specific parameters from config
   - Apply parameter inheritance (defaults + overrides)

4. Implement persistence:
   - Save ActiveProvider when changed
   - **Decision Point**: Write to appsettings.json or userpreferences.json (Q10)

5. Write tests:
   - Provider switching tests
   - Lazy-load verification
   - Caching behavior tests
   - Thread-safety tests
   - Parameter override tests

**Deliverable**: Can switch providers programmatically with full lifecycle management
**Estimated Effort**: 2-3 days

---

### Phase 3: DI & Consumer Refactoring
**Goal**: Update dependency injection and existing consumers to use ProviderManager

**Tasks**:
1. Update DI registration in `Program.cs`:
   ```csharp
   // Remove old singleton ILLMProvider registration
   // Add new registrations
   builder.Services.AddSingleton<LLMProviderFactory>();
   builder.Services.AddSingleton<ILLMProviderManager, LLMProviderManager>();
   ```

2. Refactor consumers to use `ILLMProviderManager`:
   - `AgentOrchestrator`: Inject `ILLMProviderManager`, call `GetActiveProvider()`
   - `ConversationManager`: Same as above
   - Any other components using `ILLMProvider` directly

3. Update configuration UI (if exists):
   - Remove old "Provider" dropdown/selector
   - Ensure new provider configs can be viewed

4. Test end-to-end:
   - Start app with multiple providers configured
   - Verify correct provider is loaded
   - Test provider switching programmatically
   - Verify caching works correctly

**Deliverable**: Full backend integration, can switch providers programmatically
**Estimated Effort**: 1-2 days

---

### Phase 4: UI Components
**Goal**: Add provider selector UI component

**Tasks**:
1. Create `ProviderSelector.razor` component:
   - Dropdown showing all available providers (from `GetAvailableProviders()`)
   - Display `DisplayName` from config
   - Show currently active provider (checkmark or highlight)
   - Handle selection change → call `SetActiveProviderAsync()`

2. Integrate into Home.razor:
   - Place horizontally next to `ConversationSelector`
   - Layout: Conversation selector (left), Provider selector (right)
   - Update CSS for side-by-side layout

3. Add visual feedback:
   - Loading spinner during provider switch
   - Success notification when switch completes
   - Error handling if provider switch fails (e.g., invalid config)

4. Handle edge cases:
   - What if no providers configured?
   - What if ActiveProvider doesn't exist?
   - What if provider switch fails mid-conversation?

5. Test UX flow:
   - User selects different provider
   - Sends message, verify correct provider is used
   - Switch during conversation (should work seamlessly)
   - Refresh page, verify selection persists

**Deliverable**: Full UI for provider selection, end-to-end working
**Estimated Effort**: 2-3 days

---

### Phase 5: Polish & Documentation
**Goal**: Refinement, error handling, and documentation

**Tasks**:
1. Enhanced error handling:
   - User-friendly error messages if provider fails
   - Suggestion to check configuration if validation fails
   - Graceful degradation if provider temporarily unavailable

2. Configuration guide:
   - Document new configuration structure
   - Provide examples for each provider type
   - Show parameter inheritance examples
   - Migration guide from old to new format

3. UI polish:
   - Tooltips showing provider details (model, parameters)
   - Visual indicator of which provider is active (beyond just dropdown)
   - Keyboard shortcuts? (optional)

4. Performance optimization:
   - Ensure provider switching is fast (<100ms)
   - Verify no memory leaks from cached providers
   - Profile HTTP client usage

5. Create sample configurations:
   - Example: Multiple Claude models with different parameters
   - Example: Azure OpenAI multi-region setup
   - Example: Mixed providers (Claude + GPT-4)

**Deliverable**: Production-ready feature with documentation
**Estimated Effort**: 1-2 days

---

### Summary
**Total Estimated Effort**: 7-12 days (depends on scope of each phase)

**Dependencies**:
- Phase 2 depends on Phase 1 (configuration models)
- Phase 3 depends on Phase 2 (provider manager)
- Phase 4 depends on Phase 3 (DI integration)
- Phase 5 depends on Phase 4 (UI working)

**Can be parallelized**:
- Tests can be written alongside implementation in each phase
- Documentation can be drafted early and refined later

---

## 📝 Next Steps

### Immediate Action Required
1. **🔴 Answer Q10**: Configuration Persistence Location
   - Single file (appsettings.json) or dual file (appsettings.json + userpreferences.json)?
   - See Decision 6 for detailed analysis

### Once Q10 is Answered
1. **Final review**: Review updated design decisions and implementation phases
2. **Start implementation**: Begin Phase 1 (Configuration & Domain Models)
3. **Create DD-XXX**: Formalize design decision document for docs/02-architecture/design-decisions.md
4. **Update docs**: Update component documentation to reflect new architecture

### Ready to Begin
- ✅ 9 of 10 design decisions made
- ✅ Implementation phases planned
- ✅ Architecture designed
- 🔴 1 remaining question (Q10)

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
