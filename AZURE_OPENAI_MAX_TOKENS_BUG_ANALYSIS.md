# Azure OpenAI max_tokens Bug Analysis

**Date**: 2025-11-17
**Status**: ✅ ROOT CAUSE IDENTIFIED - Configuration Issue, NOT Code Bug
**Error**: `HTTP 400 (invalid_request_error: unsupported_parameter) Parameter: max_tokens`

---

## 🎯 ACTUAL ROOT CAUSE

**The bug is in the configuration handling - missing validation!**

In the multi-provider config refactor, `IsReasoningModel` is an **optional** parameter. When it's missing from the config, `LLMProviderFactory` silently defaults it to `false` (line 130):

```csharp
// Extract IsReasoningModel parameter (critical for o1/o3/GPT-5 models)
var isReasoningModel = false;  // ❌ Silent default - no warning!
if (config.Parameters.TryGetValue("IsReasoningModel", out var reasoningObj))
{
    isReasoningModel = reasoningObj is bool boolValue ? boolValue :
                      bool.TryParse(reasoningObj?.ToString(), out var parsedValue) && parsedValue;
}
```

**Why it keeps happening**: When you configure an Azure OpenAI provider for reasoning models (o1, o3, GPT-5) and forget to add `IsReasoningModel: true`, there's:
- ❌ No validation error
- ❌ No warning message
- ❌ No log entry

The provider just silently uses the standard path which sends `max_tokens` → API rejects with 400 error.

**Example of broken config** (missing IsReasoningModel):
```json
"my-o1-preview": {
  "Type": "AzureOpenAI",
  "Parameters": {
    "Endpoint": "https://...",
    "DeploymentName": "o1-preview",
    "ApiKey": "...",
    // ❌ IsReasoningModel is missing - silently defaults to false!
  }
}
```

---

## 🔥 The Problem

When using Azure OpenAI provider with reasoning models (o1, o3, GPT-5 series), the following error occurs:

```
Error: HTTP 400 (invalid_request_error: unsupported_parameter) Parameter: max_tokens
Unsupported parameter: 'max_tokens' is not supported with this model.
Use 'max_completion_tokens' instead.
```

---

## 🔍 Root Cause Analysis

### The Core Issue

The Azure OpenAI SDK has a bug where it **always sends `max_tokens`** instead of `max_completion_tokens` for reasoning models. OpenAI's reasoning models (o1, o3, GPT-5 series) **reject** requests with `max_tokens` and require `max_completion_tokens` instead.

### What Was Already Fixed (Partially)

#### ✅ Non-Streaming Requests - WORKING
**File**: `AzureOpenAIProvider.cs:157-160`

```csharp
// For reasoning models, use protocol method to send max_completion_tokens
if (_isReasoningModel)
{
    return await SendRequestAsync_ReasoningModel(request, correlationId, requestStartTime, cancellationToken);
}
```

The `SendRequestAsync_ReasoningModel` method (lines 199-257):
- Builds raw JSON request with `max_completion_tokens` (line 557)
- Bypasses the SDK entirely by making direct HTTP POST to Azure OpenAI API
- **Works correctly** for non-streaming requests

#### ⚠️ Streaming Requests - POTENTIAL ISSUE
**File**: `AzureOpenAIProvider.cs:299-411` (`StreamRequestAsync` method)

**OBSERVATION**: This method does **NOT** have a separate code path for reasoning models like non-streaming does.

```csharp
public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
    LLMRequest request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // ...
    var messages = ConvertToAzureMessages(request.Messages);
    var options = BuildChatCompletionOptions(request);  // Uses MaxOutputTokenCount

    // Always uses SDK - no reasoning model check!
    streamingResponse = _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);
    // ...
}
```

**However**, `BuildChatCompletionOptions` (line 456) does check `_isReasoningModel`:
```csharp
// For reasoning models, we'll use protocol method with BinaryContent to avoid SDK bug
// So don't set MaxOutputTokenCount here for reasoning models
if (!_isReasoningModel)
{
    options.MaxOutputTokenCount = request.MaxTokens;
}
```

**So streaming might actually work IF**:
1. `IsReasoningModel: true` is set in config
2. `MaxOutputTokenCount` is not set for reasoning models
3. The SDK doesn't require the parameter

**Needs testing** to confirm if streaming works for reasoning models with proper config!

---

## 🎯 Why This Keeps Coming Back

The user mentioned: *"we fixed it like 3 times already. Why on earth is this back???"*

### The Real Reason:
**The code was fixed correctly, but the config keeps getting forgotten!**

Every time this error appears, it's because:
1. User switches to a reasoning model on their other PC
2. Forgets to set `IsReasoningModel: true` in the config
3. Gets the `max_tokens` error
4. Thinks the code is broken (but it's just misconfigured)

### The Fix Was Already Implemented:
- ✅ Non-streaming uses `SendRequestAsync_ReasoningModel` with `max_completion_tokens` (line 159)
- ✅ Bypasses SDK entirely with direct HTTP POST (lines 199-257)
- ✅ Correctly handles reasoning models when `_isReasoningModel = true`

**The issue is that `_isReasoningModel` is set from config**, and if config says `false`, the special handling never runs!

---

## 📊 Code Paths Comparison

### Non-Streaming (✅ Fixed)
```
Request → SendRequestAsync()
       → if(_isReasoningModel) → SendRequestAsync_ReasoningModel()
       → Build JSON with max_completion_tokens
       → Direct HTTP POST
       → ✅ Works!
```

### Streaming (❌ Broken)
```
Request → StreamRequestAsync()
       → BuildChatCompletionOptions() sets MaxOutputTokenCount
       → _chatClient.CompleteChatStreamingAsync()
       → SDK converts to max_tokens
       → ❌ API rejects with 400 error!
```

---

## 🔧 Required Fix

### Option 1: Implement Streaming for Reasoning Models (Recommended)
Add similar HTTP-based streaming for reasoning models:

1. Check `_isReasoningModel` at start of `StreamRequestAsync`
2. Route to new `StreamRequestAsync_ReasoningModel` method
3. Build raw JSON with `max_completion_tokens`
4. Use `HttpClient` with streaming response
5. Parse SSE (Server-Sent Events) chunks manually

**Pros**:
- Complete fix for all scenarios
- Consistent with non-streaming approach

**Cons**:
- More complex implementation
- Need to parse SSE format manually

### Option 2: Disable Streaming for Reasoning Models
Quick workaround - throw exception if streaming requested for reasoning model:

```csharp
public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(...)
{
    if (_isReasoningModel)
    {
        throw new LLMException(
            "Streaming is not supported for reasoning models (o1, o3, GPT-5 series). " +
            "Please disable streaming in your request.");
    }
    // ... existing code
}
```

**Pros**:
- Quick fix
- Prevents error with clear message

**Cons**:
- Loses streaming functionality for reasoning models

---

## 🧪 How to Reproduce

### Prerequisites:
1. Configure Azure OpenAI with reasoning model (e.g., o1-preview)
2. Set `IsReasoningModel: true` in `appsettings.json`:
   ```json
   "AzureOpenAI": {
     "IsReasoningModel": true,
     ...
   }
   ```

### Steps:
1. Send a request with `Stream: true`
2. Observe 400 error: "Unsupported parameter: 'max_tokens'"

### Expected:
- Request succeeds with `max_completion_tokens` parameter

### Actual:
- Request fails with 400 error because SDK sends `max_tokens`

---

## 📝 Related Files

- **Main Provider**: `TransparentAiAgentCore/Infrastructure/LLM/AzureOpenAIProvider.cs`
- **Configuration**: `TransparentAiAgentCore/Domain/Configuration/AzureOpenAIConfiguration.cs`
- **Previous Analysis**: `LLM_SELECTOR_REASONING_MODEL_ANALYSIS.md`

---

## ✅ Solution

### Immediate Fix (User Side):
Add `IsReasoningModel: true` to your Azure OpenAI provider config:

```json
"my-o1-preview": {
  "Type": "AzureOpenAI",
  "DisplayName": "Azure o1-preview",
  "Parameters": {
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "DeploymentName": "o1-preview",
    "ApiKey": "...",
    "AuthenticationMode": "ApiKey",
    "ApiVersion": "2024-02-15-preview",
    "IsReasoningModel": true  // ✅ ADD THIS!
  }
}
```

**Models that need this**:
- o1, o1-mini, o1-preview
- o3, o3-mini, o3-pro
- o4-mini
- gpt-5, gpt-5-mini, gpt-5-pro, gpt-5-nano

### Real Fix (Code Side):
Add **validation with helpful error message** in `LLMProviderFactory.CreateAzureOpenAIProviderFromConfig()`:

**Location**: `TransparentAiAgentCore/Infrastructure/LLM/LLMProviderFactory.cs:129-137`

```csharp
// Extract IsReasoningModel parameter (critical for o1/o3/GPT-5 models)
var isReasoningModel = false;
if (config.Parameters.TryGetValue("IsReasoningModel", out var reasoningObj))
{
    isReasoningModel = reasoningObj is bool boolValue ? boolValue :
                      bool.TryParse(reasoningObj?.ToString(), out var parsedValue) && parsedValue;
}

// ✅ ADD VALIDATION: Warn if deployment name suggests reasoning model but IsReasoningModel not set
if (!isReasoningModel)
{
    var nameIndicatesReasoning =
        deploymentName.Contains("o1", StringComparison.OrdinalIgnoreCase) ||
        deploymentName.Contains("o3", StringComparison.OrdinalIgnoreCase) ||
        deploymentName.Contains("o4-mini", StringComparison.OrdinalIgnoreCase) ||
        deploymentName.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase);

    if (nameIndicatesReasoning)
    {
        throw new ConfigurationException(
            $"Deployment '{deploymentName}' appears to be a reasoning model (o1/o3/o4-mini/gpt-5), " +
            $"but 'IsReasoningModel' is not set to true in configuration. " +
            $"Reasoning models require 'max_completion_tokens' instead of 'max_tokens'. " +
            $"Add \"IsReasoningModel\": true to your provider parameters.");
    }
}
```

**Why throw exception instead of warning?**
- User will see the error immediately when starting the app
- Forces correct configuration before deployment
- Prevents the cryptic "unsupported_parameter: max_tokens" error from Azure API
- Same validation should be added to OpenAIProvider too (line 172-178)

---

## 🚨 Priority

**CRITICAL** - This blocks all streaming usage of reasoning models on Azure OpenAI.

---

**Next Steps**: Implement fix and add tests for streaming with reasoning models.
