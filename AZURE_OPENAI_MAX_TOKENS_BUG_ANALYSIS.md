# Azure OpenAI max_tokens Bug Analysis

**Date**: 2025-11-17
**Status**: CRITICAL BUG IDENTIFIED - Streaming path broken for reasoning models
**Error**: `HTTP 400 (invalid_request_error: unsupported_parameter) Parameter: max_tokens`

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

#### ❌ Streaming Requests - BROKEN
**File**: `AzureOpenAIProvider.cs:299-411` (`StreamRequestAsync` method)

**THE BUG**: This method does **NOT** have a separate code path for reasoning models!

```csharp
public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
    LLMRequest request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // ...
    var messages = ConvertToAzureMessages(request.Messages);
    var options = BuildChatCompletionOptions(request);  // ⚠️ Uses MaxOutputTokenCount

    // ⚠️ ALWAYS uses SDK - no reasoning model check!
    streamingResponse = _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);
    // ...
}
```

**What happens**:
1. `BuildChatCompletionOptions` sets `options.MaxOutputTokenCount = request.MaxTokens` (line 479)
2. The Azure OpenAI SDK **internally converts** `MaxOutputTokenCount` to `max_tokens` parameter
3. Azure OpenAI API **rejects** the request with 400 error for reasoning models

---

## 🎯 Why This Keeps Coming Back

The user mentioned: *"we fixed it like 3 times already. Why on earth is this back???"*

### History of Fixes:
1. **Fix #1**: Added `_isReasoningModel` flag and special handling for non-streaming requests
2. **Fix #2**: (Unknown - user mentioned multiple fixes)
3. **Fix #3**: (Unknown - user mentioned multiple fixes)

### Why It Keeps Breaking:
**Streaming was never fixed!** The previous fixes only addressed **non-streaming** requests. Every time the user switches from non-streaming to streaming mode, the bug resurfaces.

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

## ✅ Recommended Solution

Implement **Option 1** (streaming for reasoning models) to provide complete functionality:

1. Create `StreamRequestAsync_ReasoningModel` method
2. Use `HttpClient` with SSE streaming
3. Parse chunks manually and convert to `StreamingLLMChunk`
4. Update `StreamRequestAsync` to route reasoning models to new method

This ensures both streaming and non-streaming work correctly for all model types.

---

## 🚨 Priority

**CRITICAL** - This blocks all streaming usage of reasoning models on Azure OpenAI.

---

**Next Steps**: Implement fix and add tests for streaming with reasoning models.
