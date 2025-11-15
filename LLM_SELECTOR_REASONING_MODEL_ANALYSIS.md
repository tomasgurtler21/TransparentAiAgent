# LLM Selector Reasoning Model Analysis

**Created**: 2025-11-15
**Author**: Claude (Code Review)
**Status**: CRITICAL ISSUE IDENTIFIED
**Related**: LLM_SELECTOR_IMPLEMENTATION_PLAN.md, LLM_SELECTOR_DESIGN.md

---

## Executive Summary

🚨 **CRITICAL FINDING**: The new LLM Selector implementation has **lost support for reasoning model parameter restrictions** that were present in the old configuration system.

### Issues Identified

1. **Missing `IsReasoningModel` field** in new `ProviderConfig`
2. **Temperature and top_p NOT filtered** for reasoning models (even in old code!)
3. **Factory does not extract** `IsReasoningModel` from new config
4. **Breaking API calls** for OpenAI o1/o3 and similar models

---

## Background

### What Are Reasoning Models?

**Reasoning models** are a class of LLMs that use extended internal reasoning processes:

- **OpenAI**: o1, o1-mini, o3, o3-mini, o3-pro, o4-mini
- **Azure OpenAI**: GPT-5 series (gpt-5, gpt-5-mini, gpt-5-pro, gpt-5-nano), o1, o3, o4-mini
- **Anthropic**: Extended Thinking mode (different implementation, no restrictions)

### API Restrictions for Reasoning Models

According to OpenAI API documentation, reasoning models have **special requirements**:

1. ✅ **MUST use** `max_completion_tokens` instead of `max_tokens`
2. ❌ **MUST NOT send** `temperature` parameter (API will reject)
3. ❌ **MUST NOT send** `top_p` parameter (API will reject)
4. ⚠️ **Streaming support** may be limited

**Error if violated**: `400 Bad Request` with message about unsupported parameters

---

## Old Configuration System (Before LLM Selector)

### Configuration Structure

File: `TransparentAiAgentCore/Domain/Configuration/AzureOpenAIConfiguration.cs`

```csharp
public class AzureOpenAIConfiguration
{
    // ... other properties ...

    /// <summary>
    /// Indicates whether the model is a reasoning model (GPT-5 series, o1, o3, o4-mini, etc.).
    /// Reasoning models require max_completion_tokens instead of max_tokens.
    /// Set to true for: gpt-5, gpt-5-mini, gpt-5-pro, gpt-5-nano, o1, o1-mini, o3, o3-mini, o3-pro, o4-mini.
    /// Default is false (traditional models like GPT-4, GPT-4o).
    /// </summary>
    public bool IsReasoningModel { get; set; } = false;

    // ... validation ...
}
```

### How It Was Used

File: `TransparentAiAgentCore/Infrastructure/LLM/AzureOpenAIProvider.cs`

```csharp
public AzureOpenAIProvider(
    IAuthenticationProvider authProvider,
    string deploymentName,
    ITransparencyService transparencyService,
    AppConfiguration appConfig)
{
    // ...
    _isReasoningModel = appConfig.LLM.AzureOpenAI?.IsReasoningModel ?? false;  // Line 53
    // ...
}

public async Task<LLMResponse> SendRequestAsync(
    LLMRequest request,
    CancellationToken cancellationToken = default)
{
    // Line 156-160: Special handling for reasoning models
    if (_isReasoningModel)
    {
        return await SendRequestAsync_ReasoningModel(request, correlationId, requestStartTime, cancellationToken);
    }
    // ... standard path ...
}
```

### What It Did (Partially)

#### ✅ What Worked:
1. **Used `max_completion_tokens`** instead of `max_tokens` (Line 553 in `BuildRequestJsonForReasoningModel`)
2. **Skipped setting `MaxOutputTokenCount`** in SDK options (Line 473-476 in `BuildChatCompletionOptions`)

#### ❌ What Did NOT Work (BUG IN OLD CODE):
**Lines 549-550 in `BuildRequestJsonForReasoningModel`**:
```csharp
// Add parameters
requestJson["temperature"] = options.Temperature;  // 🚨 BUG: This is sent even for reasoning models!
requestJson["top_p"] = options.TopP;              // 🚨 BUG: This is sent even for reasoning models!

// CRITICAL: Use max_completion_tokens for reasoning models
requestJson["max_completion_tokens"] = maxTokens;
```

**The old code had a bug**: It sent `temperature` and `top_p` to reasoning models, which would cause API errors if these were set!

---

## New Configuration System (LLM Selector)

### New Configuration Structure

File: `TransparentAiAgentCore/Domain/Configuration/ProviderConfig.cs`

```csharp
public class ProviderConfig
{
    public string Type { get; }                          // e.g., "Anthropic", "AzureOpenAI"
    public string DisplayName { get; }                   // UI name
    public Dictionary<string, object> Parameters { get; } // Provider-specific params
    public ProviderParameters? ParameterOverrides { get; } // Optional overrides
}
```

**Missing**: No `IsReasoningModel` field!

### How Factory Creates Providers

File: `TransparentAiAgentCore/Infrastructure/LLM/LLMProviderFactory.cs` (Lines 102-147)

```csharp
private ILLMProvider CreateAzureOpenAIProviderFromConfig(ProviderConfig config)
{
    // Extract required parameters
    var endpoint = GetRequiredStringParameter(config, "Endpoint", "AzureOpenAI");
    var deploymentName = GetRequiredStringParameter(config, "DeploymentName", "AzureOpenAI");
    var apiKey = GetRequiredStringParameter(config, "ApiKey", "AzureOpenAI");

    // ... extract optional parameters ...

    // 🚨 MISSING: Does NOT extract IsReasoningModel!

    // Create a temporary AppConfiguration with AzureOpenAI config
    var tempConfig = new AppConfiguration
    {
        LLM = new LLMConfiguration
        {
            Provider = "AzureOpenAI",
            AzureOpenAI = new AzureOpenAIConfiguration
            {
                Endpoint = endpoint,
                DeploymentName = deploymentName,
                ApiKey = apiKey,
                ApiVersion = apiVersion,
                AuthenticationMode = authenticationMode
                // 🚨 MISSING: IsReasoningModel is NOT set here!
                // Defaults to false, so reasoning models won't work properly
            }
        }
    };

    var tempAuthProvider = new ConfigurationAuthenticationProvider(tempConfig);

    return new AzureOpenAIProvider(tempAuthProvider, deploymentName, _transparencyService, tempConfig);
}
```

### Impact

**All reasoning models configured via LLM Selector will fail** because:
1. `IsReasoningModel` defaults to `false`
2. Provider will try to use `max_tokens` instead of `max_completion_tokens`
3. If Temperature/TopP are set in `DefaultParameters`, they will be sent (causing API error)

---

## Analysis of Other Providers

### OpenAI Provider

File: `TransparentAiAgentCore/Infrastructure/LLM/OpenAIProvider.cs`

```csharp
private ChatCompletionOptions BuildChatCompletionOptions(LLMRequest request)
{
    var options = new ChatCompletionOptions();

    // Lines 294-302: Always sets Temperature and TopP if present
    if (request.Temperature.HasValue)
    {
        options.Temperature = (float)request.Temperature.Value;  // 🚨 No reasoning model check!
    }

    if (request.TopP.HasValue)
    {
        options.TopP = (float)request.TopP.Value;  // 🚨 No reasoning model check!
    }

    options.MaxOutputTokenCount = request.MaxTokens;  // 🚨 No max_completion_tokens support!

    // ...
}
```

**Status**: ❌ No reasoning model support at all in OpenAI provider

### Anthropic Provider

File: `TransparentAiAgentCore/Infrastructure/LLM/AnthropicProvider.cs`

```csharp
// Lines 590-598: Always sets Temperature and TopP if present
if (request.Temperature.HasValue)
{
    messageParams.Temperature = request.Temperature.Value;
}

if (request.TopP.HasValue)
{
    messageParams.TopP = request.TopP.Value;
}
```

**Status**: ℹ️ Anthropic's Extended Thinking mode does NOT have the same restrictions as OpenAI reasoning models. Temperature and TopP can be used with Extended Thinking.

---

## Detailed Analysis

### Parameter Flow

```
User Configuration (appsettings.json)
    ↓
LLMConfiguration.Providers["provider-name"] (ProviderConfig)
    ↓
LLMProviderFactory.CreateProvider(configName, ProviderConfig)
    ↓
CreateAzureOpenAIProviderFromConfig / CreateOpenAIProviderFromConfig
    ↓
[🚨 IsReasoningModel NOT extracted from Parameters]
    ↓
Creates temp AzureOpenAIConfiguration (defaults IsReasoningModel = false)
    ↓
AzureOpenAIProvider constructor
    ↓
_isReasoningModel = appConfig.LLM.AzureOpenAI?.IsReasoningModel ?? false
    ↓
[🚨 Always false for LLM Selector providers]
    ↓
SendRequestAsync
    ↓
if (_isReasoningModel) ← Never true!
    ↓
Uses standard path (sends temperature, top_p, uses max_tokens)
    ↓
[🚨 API ERROR for o1/o3 models]
```

### Where Parameter Filtering Should Happen

**Current Flow**:
```
LLMRequest (has Temperature, TopP from DefaultParameters/ProviderOverrides)
    ↓
BuildChatCompletionOptions
    ↓
Sets options.Temperature, options.TopP (NO FILTERING)
    ↓
SDK sends to API
    ↓
[🚨 API rejects for reasoning models]
```

**Should Be**:
```
LLMRequest (has Temperature, TopP)
    ↓
BuildChatCompletionOptions
    ↓
if (isReasoningModel)
    → SKIP Temperature
    → SKIP TopP
    → Use max_completion_tokens
else
    → Set Temperature
    → Set TopP
    → Use max_tokens
```

---

## Evidence from Code

### 1. AzureOpenAIProvider Stores IsReasoningModel (Line 30)
```csharp
private readonly bool _isReasoningModel;
```

### 2. Constructor Reads It (Line 53)
```csharp
_isReasoningModel = appConfig.LLM.AzureOpenAI?.IsReasoningModel ?? false;
```

### 3. Special Path for Reasoning Models (Lines 156-160)
```csharp
if (_isReasoningModel)
{
    return await SendRequestAsync_ReasoningModel(request, correlationId, requestStartTime, cancellationToken);
}
```

### 4. Reasoning Model Request Builder Sends Temperature/TopP (Lines 549-550)
```csharp
requestJson["temperature"] = options.Temperature;  // 🚨 BUG!
requestJson["top_p"] = options.TopP;              // 🚨 BUG!
```

### 5. Factory Does NOT Extract IsReasoningModel (Lines 102-147)
```csharp
// Line 127-142: Creates temp config WITHOUT IsReasoningModel
var tempConfig = new AppConfiguration
{
    LLM = new LLMConfiguration
    {
        Provider = "AzureOpenAI",
        AzureOpenAI = new AzureOpenAIConfiguration
        {
            // ... extracted parameters ...
            // 🚨 IsReasoningModel NOT set (defaults to false)
        }
    }
};
```

---

## Proposed Fix

### Option 1: Add IsReasoningModel to ProviderConfig.Parameters

**Pros**:
- Minimal changes to domain model
- Backward compatible
- Uses existing Parameters dictionary

**Cons**:
- Not type-safe
- Requires documentation
- Easy to forget

**Implementation**:

#### 1. Update `LLMProviderFactory.CreateAzureOpenAIProviderFromConfig`:
```csharp
private ILLMProvider CreateAzureOpenAIProviderFromConfig(ProviderConfig config)
{
    // ... existing parameter extraction ...

    // Extract IsReasoningModel
    var isReasoningModel = false;
    if (config.Parameters.TryGetValue("IsReasoningModel", out var reasoningObj))
    {
        isReasoningModel = reasoningObj is bool b ? b :
                          bool.TryParse(reasoningObj?.ToString(), out var parsed) && parsed;
    }

    var tempConfig = new AppConfiguration
    {
        LLM = new LLMConfiguration
        {
            Provider = "AzureOpenAI",
            AzureOpenAI = new AzureOpenAIConfiguration
            {
                Endpoint = endpoint,
                DeploymentName = deploymentName,
                ApiKey = apiKey,
                ApiVersion = apiVersion,
                AuthenticationMode = authenticationMode,
                IsReasoningModel = isReasoningModel  // ✅ Set from config
            }
        }
    };

    // ...
}
```

#### 2. Update `LLMProviderFactory.CreateOpenAIProviderFromConfig`:
```csharp
private ILLMProvider CreateOpenAIProviderFromConfig(ProviderConfig config)
{
    // ... existing parameter extraction ...

    // Extract IsReasoningModel
    var isReasoningModel = false;
    if (config.Parameters.TryGetValue("IsReasoningModel", out var reasoningObj))
    {
        isReasoningModel = reasoningObj is bool b ? b :
                          bool.TryParse(reasoningObj?.ToString(), out var parsed) && parsed;
    }

    // Create OpenAIConfiguration that supports IsReasoningModel
    // (requires adding IsReasoningModel to OpenAIConfiguration first)

    var tempConfig = new AppConfiguration
    {
        LLM = new LLMConfiguration
        {
            Provider = "OpenAI",
            OpenAI = new OpenAIConfiguration
            {
                ApiKey = apiKey,
                Model = model,
                IsReasoningModel = isReasoningModel  // ✅ New field
            }
        }
    };

    // ...
}
```

#### 3. Add `IsReasoningModel` to `OpenAIConfiguration`:
```csharp
public class OpenAIConfiguration
{
    // ... existing properties ...

    /// <summary>
    /// Indicates whether the model is a reasoning model (o1, o3, o4-mini, etc.).
    /// Reasoning models require max_completion_tokens instead of max_tokens
    /// and do not support temperature/top_p parameters.
    /// Set to true for: o1, o1-mini, o3, o3-mini, o3-pro, o4-mini.
    /// Default is false (traditional models like GPT-4, GPT-4o).
    /// </summary>
    public bool IsReasoningModel { get; set; } = false;

    // ...
}
```

#### 4. Update `OpenAIProvider` to support reasoning models:
```csharp
public class OpenAIProvider : ILLMProvider
{
    private readonly ChatClient _chatClient;
    private readonly ITransparencyService _transparencyService;
    private readonly bool _isReasoningModel;  // ✅ Add field

    public OpenAIProvider(
        IAuthenticationProvider authProvider,
        string model,
        ITransparencyService transparencyService,
        AppConfiguration appConfig)
    {
        // ... existing code ...
        _isReasoningModel = appConfig.LLM.OpenAI?.IsReasoningModel ?? false;  // ✅ Read from config
        // ...
    }

    private ChatCompletionOptions BuildChatCompletionOptions(LLMRequest request)
    {
        var options = new ChatCompletionOptions();

        // ✅ Only set Temperature and TopP if NOT a reasoning model
        if (!_isReasoningModel)
        {
            if (request.Temperature.HasValue)
            {
                options.Temperature = (float)request.Temperature.Value;
            }

            if (request.TopP.HasValue)
            {
                options.TopP = (float)request.TopP.Value;
            }
        }

        // ✅ For reasoning models, would need max_completion_tokens support
        // (may require direct HTTP call like AzureOpenAIProvider does)
        options.MaxOutputTokenCount = request.MaxTokens;

        // ...
    }
}
```

#### 5. **CRITICAL**: Fix `AzureOpenAIProvider.BuildRequestJsonForReasoningModel`:
```csharp
private JsonObject BuildRequestJsonForReasoningModel(List<ChatMessage> messages, ChatCompletionOptions options, int maxTokens)
{
    var requestJson = new JsonObject();

    // Add messages
    var messagesArray = new JsonArray();
    // ... message conversion ...
    requestJson["messages"] = messagesArray;

    // ✅ FIX: Do NOT send temperature and top_p for reasoning models
    // Remove lines 549-550:
    // requestJson["temperature"] = options.Temperature;  // ❌ REMOVE
    // requestJson["top_p"] = options.TopP;              // ❌ REMOVE

    // CRITICAL: Use max_completion_tokens for reasoning models
    requestJson["max_completion_tokens"] = maxTokens;

    // Add tools if present
    // ...

    return requestJson;
}
```

#### 6. Update documentation and examples:

**File**: `docs/05-guides/deployment/llm-provider-selector.md`

Add section on reasoning models:

```markdown
### Configuring Reasoning Models

Reasoning models (o1, o3, o4-mini for OpenAI, GPT-5 series for Azure) require special configuration:

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "ActiveProvider": "openai-o1",
      "DefaultParameters": {
        "MaxTokens": 4096
        // ⚠️ Do NOT set Temperature or TopP for reasoning models
      },
      "Providers": {
        "openai-o1": {
          "Type": "OpenAI",
          "DisplayName": "OpenAI o1-preview",
          "Model": "o1-preview",
          "ApiKey": "YOUR_API_KEY",
          "IsReasoningModel": true  // ✅ Required for reasoning models
        },
        "azure-o3": {
          "Type": "AzureOpenAI",
          "DisplayName": "Azure o3-mini",
          "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
          "DeploymentName": "o3-mini",
          "ApiKey": "YOUR_API_KEY",
          "ApiVersion": "2024-02-15-preview",
          "IsReasoningModel": true  // ✅ Required for reasoning models
        }
      }
    }
  }
}
```

**Important Notes for Reasoning Models**:

1. ✅ **MUST set** `IsReasoningModel: true` in provider Parameters
2. ❌ **DO NOT set** `Temperature` in DefaultParameters or ParameterOverrides
3. ❌ **DO NOT set** `TopP` in DefaultParameters or ParameterOverrides
4. ✅ Use `MaxTokens` normally (converted to `max_completion_tokens` internally)
5. ⚠️ Streaming may be limited or unavailable

**Supported Reasoning Models**:
- **OpenAI**: o1, o1-mini, o3, o3-mini, o3-pro, o4-mini
- **Azure OpenAI**: gpt-5, gpt-5-mini, gpt-5-pro, gpt-5-nano, o1, o1-mini, o3, o3-mini, o3-pro, o4-mini

**Not Reasoning Models** (can use Temperature/TopP):
- GPT-4, GPT-4o, GPT-4-turbo, GPT-3.5-turbo
- Claude (all models, including Extended Thinking mode)
```

### Option 2: Add Strong Typing to ProviderConfig

**Pros**:
- Type-safe
- Compile-time checking
- Self-documenting

**Cons**:
- Larger refactoring
- Changes domain model
- More complex

**Implementation**: (Not recommended at this stage, Option 1 is simpler)

---

## Configuration Examples

### Example 1: Azure OpenAI with o3-mini (Reasoning Model)

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "ActiveProvider": "azure-o3-mini",
      "DefaultParameters": {
        "MaxTokens": 4096
        // ⚠️ NO Temperature or TopP for reasoning models
      },
      "Providers": {
        "azure-o3-mini": {
          "Type": "AzureOpenAI",
          "DisplayName": "Azure o3-mini (Reasoning)",
          "Endpoint": "https://your-resource.openai.azure.com/",
          "DeploymentName": "o3-mini",
          "ApiKey": "your-api-key",
          "ApiVersion": "2024-02-15-preview",
          "AuthenticationMode": "ApiKey",
          "IsReasoningModel": true  // ✅ Critical!
        }
      }
    }
  }
}
```

### Example 2: OpenAI with o1-preview and GPT-4o (Mixed)

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "ActiveProvider": "gpt4o",
      "DefaultParameters": {
        "Temperature": 0.7,
        "TopP": 1.0,
        "MaxTokens": 4096
      },
      "Providers": {
        "gpt4o": {
          "Type": "OpenAI",
          "DisplayName": "GPT-4o",
          "Model": "gpt-4o",
          "ApiKey": "your-api-key"
          // IsReasoningModel defaults to false, Temperature/TopP will be used
        },
        "o1-reasoning": {
          "Type": "OpenAI",
          "DisplayName": "o1-preview (Reasoning)",
          "Model": "o1-preview",
          "ApiKey": "your-api-key",
          "IsReasoningModel": true,  // ✅ Critical!
          "Parameters": {
            "Temperature": null,  // ✅ Override default to null
            "TopP": null          // ✅ Override default to null
          }
        }
      }
    }
  }
}
```

### Example 3: Anthropic (No Restrictions)

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "ActiveProvider": "claude-thinking",
      "DefaultParameters": {
        "Temperature": 0.7,
        "TopP": 1.0,
        "MaxTokens": 4096
      },
      "Providers": {
        "claude-thinking": {
          "Type": "Anthropic",
          "DisplayName": "Claude with Extended Thinking",
          "Model": "claude-sonnet-4-5-20250929",
          "ApiKey": "your-api-key",
          "ExtendedThinking": {
            "Enabled": true,
            "BudgetTokens": 10000
          }
          // ℹ️ Anthropic Extended Thinking CAN use Temperature/TopP
        }
      }
    }
  }
}
```

---

## Testing Recommendations

### Unit Tests

**File**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/LLMProviderFactoryTests.cs`

Add tests:

```csharp
[TestMethod]
public void CreateAzureOpenAIProvider_WithIsReasoningModel_SetsFieldCorrectly()
{
    var config = new ProviderConfig(
        type: "AzureOpenAI",
        displayName: "o3-mini",
        parameters: new Dictionary<string, object>
        {
            ["Endpoint"] = "https://test.openai.azure.com/",
            ["DeploymentName"] = "o3-mini",
            ["ApiKey"] = "test-key",
            ["IsReasoningModel"] = true  // ✅ Test this!
        });

    var provider = _factory.CreateProvider("test", config);

    // Assert provider was created with IsReasoningModel = true
    // (requires exposing field or testing behavior)
}

[TestMethod]
public void CreateOpenAIProvider_WithIsReasoningModel_SetsFieldCorrectly()
{
    var config = new ProviderConfig(
        type: "OpenAI",
        displayName: "o1-preview",
        parameters: new Dictionary<string, object>
        {
            ["Model"] = "o1-preview",
            ["ApiKey"] = "test-key",
            ["IsReasoningModel"] = true
        });

    var provider = _factory.CreateProvider("test", config);

    // Assert provider was created with IsReasoningModel = true
}
```

**File**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/AzureOpenAIProviderTests.cs`

Add test:

```csharp
[TestMethod]
public async Task SendRequestAsync_ReasoningModel_DoesNotSendTemperatureOrTopP()
{
    // Create provider with IsReasoningModel = true
    // Mock HTTP request
    // Verify request JSON does NOT contain "temperature" or "top_p" fields
}
```

### Integration Tests

Test with actual API (if possible):

1. Configure o1-preview with `IsReasoningModel: true`
2. Send request with DefaultParameters containing Temperature
3. Verify API accepts request (parameters filtered)
4. Configure o1-preview with `IsReasoningModel: false` (wrong)
5. Send request with Temperature
6. Verify API rejects request with 400

---

## Summary of Findings

### Critical Issues

1. ❌ **`IsReasoningModel` missing from new ProviderConfig**
   Impact: All reasoning models will fail with API errors

2. ❌ **Factory does not extract `IsReasoningModel` from Parameters**
   Impact: Field always defaults to `false`, reasoning model path never taken

3. ❌ **OpenAI provider has no reasoning model support**
   Impact: o1/o3 models via OpenAI provider will fail

4. ❌ **BUG: Temperature/TopP sent even for reasoning models in old code**
   Impact: Even old config could fail if Temperature/TopP are set

### Recommendations

**Priority 1 (Critical)**:
- Add `IsReasoningModel` parameter extraction in `LLMProviderFactory`
- Fix `AzureOpenAIProvider.BuildRequestJsonForReasoningModel` to NOT send temperature/top_p
- Add `IsReasoningModel` support to `OpenAIProvider`

**Priority 2 (High)**:
- Add unit tests for reasoning model configuration
- Update documentation with reasoning model examples
- Add configuration validation warnings

**Priority 3 (Medium)**:
- Consider automatic detection of reasoning models from model name
- Add UI indicators for reasoning model restrictions
- Improve error messages for reasoning model misconfigurations

---

## References

### OpenAI Documentation
- **Reasoning Models API**: https://platform.openai.com/docs/guides/reasoning
- **o1 series**: Requires `max_completion_tokens`, no temperature/top_p
- **o3 series**: Same restrictions as o1

### Anthropic Documentation
- **Extended Thinking**: https://docs.anthropic.com/en/docs/build-with-claude/extended-thinking
- **No parameter restrictions**: Temperature and top_p CAN be used with Extended Thinking

### Code References
- `AzureOpenAIConfiguration.cs:31-36` - IsReasoningModel definition
- `AzureOpenAIProvider.cs:30,53,156-160,549-550` - Reasoning model handling
- `LLMProviderFactory.cs:102-147` - Missing IsReasoningModel extraction
- `ProviderConfig.cs` - New config structure (missing field)

---

## Conclusion

The user's memory was **100% correct**. The `IsReasoningModel` field from the old configuration was critical for proper reasoning model support, and it was **completely lost** during the LLM Selector implementation.

Additionally, a **critical bug** was discovered: Even the old code sent `temperature` and `top_p` to reasoning models, which would cause API errors.

**Immediate action required** to restore reasoning model support in the new multi-provider system.
