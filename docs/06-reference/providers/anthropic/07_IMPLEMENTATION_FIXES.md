# Implementation Fixes for Your Code

**Last Updated:** January 2025
**File:** `TransparentAiAgentCore/Infrastructure/LLM/AnthropicProvider.cs`

---

## Critical Issues Found

### Issue 1: Tool Calls Lost in Streaming (Lines 162-167)

**Current Code:**
```csharp
if (streamEvent.TryPickContentBlockDelta(out var deltaEvent))
{
    if (deltaEvent.Delta.TryPickText(out var textDelta))
    {
        accumulatedContent.Append(textDelta.Text);
    }
    // ❌ MISSING: input_json_delta handling for tool calls!
}
```

**Problem:** Tool calls completely lost when streaming because `input_json_delta` events are not handled.

**Fix:** Add tool call accumulation logic (see complete fix below).

---

### Issue 2: Wrong stop_reason (Line 172)

**Current Code:**
```csharp
else if (streamEvent.TryPickStop(out var stopEvent))
{
    stopReason = "stop";  // ❌ Hardcoded!
}
```

**Problem:** Always reports "stop" regardless of actual reason (could be "tool_use", "max_tokens", etc.).

**Fix:** Capture from `message_delta` event:
```csharp
else if (streamEvent.TryPickMessageDelta(out var messageDelta))
{
    stopReason = messageDelta.Delta.StopReason?.ToString();
}
```

---

### Issue 3: Missing Usage Information (Line 227)

**Current Code:**
```csharp
return new LLMResponse(
    content: textContent,
    toolCalls: toolCalls,
    finishReason: finishReason,
    usage: null  // TODO: Extract usage info from response
);
```

**Fix:**
```csharp
LLMUsage? usage = null;
if (response.Usage != null)
{
    usage = new LLMUsage
    {
        InputTokens = response.Usage.InputTokens,
        OutputTokens = response.Usage.OutputTokens,
        TotalTokens = response.Usage.InputTokens + response.Usage.OutputTokens
    };
}

return new LLMResponse(
    content: textContent,
    toolCalls: toolCalls,
    finishReason: finishReason,
    usage: usage
);
```

---

## Complete Fix for StreamRequestAsync

Replace lines 120-186 with:

```csharp
public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
    LLMRequest request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    if (request == null)
        throw new ArgumentNullException(nameof(request));

    // Accumulators
    var textAccumulators = new Dictionary<int, System.Text.StringBuilder>();
    var toolCallInfo = new Dictionary<int, (string Id, string Name)>();
    var jsonAccumulators = new Dictionary<int, System.Text.StringBuilder>();
    string? stopReason = null;

    // Generate correlation ID and start time
    var correlationId = Guid.NewGuid().ToString();
    var startTime = DateTime.UtcNow;

    IAsyncEnumerable<Anthropic.Client.Models.Messages.RawMessageStreamEvent>? streamingResponse = null;

    // Build request outside try-catch to allow yield
    try
    {
        var messageParams = BuildMessageRequest(request, null);
        LogRawRequest(request, messageParams, correlationId);

        streamingResponse = _client.Messages.CreateStreaming(messageParams);
    }
    catch (Exception ex)
    {
        _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
            Domain.Transparency.TransparencyEventType.Error,
            $"Anthropic streaming request setup failed: {ex.Message}",
            "LLM Error"));
        throw new Domain.Exceptions.LLMException($"Anthropic streaming request failed: {ex.Message}", ex);
    }

    // Stream chunks
    if (streamingResponse != null)
    {
        await foreach (var streamEvent in streamingResponse.WithCancellation(cancellationToken))
        {
            // Handle content_block_start
            if (streamEvent.TryPickContentBlockStart(out var blockStart))
            {
                int index = blockStart.Index;

                if (blockStart.ContentBlock.TryPickText(out _))
                {
                    textAccumulators[index] = new System.Text.StringBuilder();
                }
                else if (blockStart.ContentBlock.TryPickToolUse(out var toolBlock))
                {
                    toolCallInfo[index] = (toolBlock.ID, toolBlock.Name);
                    jsonAccumulators[index] = new System.Text.StringBuilder();
                }
            }
            // Handle content_block_delta
            else if (streamEvent.TryPickContentBlockDelta(out var deltaEvent))
            {
                int index = deltaEvent.Index;

                if (deltaEvent.Delta.TryPickText(out var textDelta))
                {
                    if (textAccumulators.ContainsKey(index))
                    {
                        textAccumulators[index].Append(textDelta.Text);
                    }
                }
                else if (deltaEvent.Delta.TryPickInputJson(out var jsonDelta))
                {
                    // ✅ NEW: Accumulate tool call JSON
                    if (jsonAccumulators.ContainsKey(index))
                    {
                        jsonAccumulators[index].Append(jsonDelta.PartialJson);
                    }
                }
            }
            // Handle content_block_stop
            else if (streamEvent.TryPickContentBlockStop(out var blockStop))
            {
                int index = blockStop.Index;

                // Validate JSON for tool calls
                if (jsonAccumulators.ContainsKey(index))
                {
                    var jsonString = jsonAccumulators[index].ToString();
                    try
                    {
                        JsonDocument.Parse(jsonString);
                    }
                    catch (JsonException ex)
                    {
                        _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
                            Domain.Transparency.TransparencyEventType.Error,
                            $"Invalid tool call JSON at index {index}: {jsonString}",
                            "Tool Call Error"));
                    }
                }
            }
            // Handle message_delta (CRITICAL - captures stop_reason)
            else if (streamEvent.TryPickMessageDelta(out var messageDelta))
            {
                // ✅ FIXED: Capture stop_reason from message_delta
                stopReason = messageDelta.Delta.StopReason?.ToString();
            }
            // Handle message_stop
            else if (streamEvent.TryPickStop(out var stopEvent))
            {
                // Stream is complete - yield final chunk with tool calls if any
                List<LLMToolCall>? toolCalls = null;

                if (toolCallInfo.Count > 0)
                {
                    toolCalls = new List<LLMToolCall>();
                    foreach (var kvp in toolCallInfo)
                    {
                        int index = kvp.Key;
                        var (id, name) = kvp.Value;
                        var jsonString = jsonAccumulators[index].ToString();

                        toolCalls.Add(new LLMToolCall(id, name, jsonString));
                    }
                }

                // Yield final chunk
                yield return new StreamingLLMChunk(
                    contentDelta: string.Empty,
                    toolCallDelta: null,
                    isComplete: true,
                    finishReason: stopReason ?? "unknown"
                )
                {
                    // Include tool calls in final chunk if available
                    ToolCalls = toolCalls
                };

                // Log complete response
                var latency = DateTime.UtcNow - startTime;
                var fullText = string.Join("", textAccumulators.Values.Select(sb => sb.ToString()));
                LogStreamingResponse(fullText, stopReason, correlationId, latency);

                break;
            }

            // Convert and yield current event
            var chunk = ConvertStreamingEvent(streamEvent);
            if (chunk != null)
            {
                yield return chunk;
            }
        }
    }
}
```

**Key Changes:**
1. Added `toolCallInfo` and `jsonAccumulators` dictionaries
2. Handle `content_block_start` for tool_use blocks
3. Handle `input_json_delta` events
4. Handle `content_block_stop` to validate JSON
5. Capture `stop_reason` from `message_delta` event
6. Build tool calls after stream completes
7. Include tool calls in final chunk

---

## Fix for ConvertResponse (Usage)

Replace lines 191-228 with:

```csharp
private LLMResponse ConvertResponse(Message response)
{
    // Extract text content from content blocks
    var textContent = string.Empty;
    List<LLMToolCall>? toolCalls = null;

    foreach (var contentBlock in response.Content)
    {
        // Check if this is a text block
        if (contentBlock.TryPickText(out var textBlock))
        {
            textContent += textBlock.Text;
        }
        // Check if this is a tool use block
        else if (contentBlock.TryPickToolUse(out var toolUseBlock))
        {
            if (toolCalls == null)
                toolCalls = new List<LLMToolCall>();

            // Convert tool use to LLMToolCall
            var toolCall = new LLMToolCall(
                toolUseBlock.ID,
                toolUseBlock.Name,
                System.Text.Json.JsonSerializer.Serialize(toolUseBlock.Input)
            );
            toolCalls.Add(toolCall);
        }
    }

    // Map stop reason to finish reason
    var finishReason = response.StopReason?.ToString() ?? "unknown";

    // ✅ FIXED: Extract usage information
    LLMUsage? usage = null;
    if (response.Usage != null)
    {
        usage = new LLMUsage
        {
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens,
            TotalTokens = response.Usage.InputTokens + response.Usage.OutputTokens
        };
    }

    return new LLMResponse(
        content: textContent,
        toolCalls: toolCalls,
        finishReason: finishReason,
        usage: usage  // ✅ Now populated
    );
}
```

---

## Update Model Identifier

**Current (Line 38 in config):**
```csharp
Model = "claude-3-5-sonnet-20241022"
```

**Recommended Update:**
```csharp
Model = "claude-sonnet-4-5-20250929"  // Latest Sonnet 4.5
// OR
Model = "claude-haiku-4-5-20251001"   // Latest Haiku 4.5
```

**File:** `TransparentAiAgentCore/Domain/Configuration/AnthropicConfiguration.cs`

---

## Testing Your Fixes

### Test 1: Non-Streaming with Tools
```csharp
[TestMethod]
public async Task SendRequestAsync_WithTools_ReturnsToolCalls()
{
    // Arrange
    var request = new LLMRequest
    {
        Messages = new List<LLMMessage>
        {
            new LLMMessage { Role = "user", Content = "What's the weather in SF?" }
        },
        Tools = new List<LLMTool>
        {
            new LLMTool
            {
                Name = "get_weather",
                Description = "Get current weather",
                ParametersSchema = @"{""type"": ""object"", ""properties"": {""location"": {""type"": ""string""}}, ""required"": [""location""]}"
            }
        },
        MaxTokens = 1024
    };

    // Act
    var response = await provider.SendRequestAsync(request);

    // Assert
    Assert.AreEqual("tool_use", response.FinishReason);
    Assert.IsNotNull(response.ToolCalls);
    Assert.IsTrue(response.ToolCalls.Count > 0);
    Assert.AreEqual("get_weather", response.ToolCalls[0].FunctionName);
}
```

### Test 2: Streaming with Tools
```csharp
[TestMethod]
public async Task StreamRequestAsync_WithTools_CapturesToolCalls()
{
    // Arrange
    var request = new LLMRequest
    {
        Messages = new List<LLMMessage>
        {
            new LLMMessage { Role = "user", Content = "What's the weather in SF?" }
        },
        Tools = new List<LLMTool>
        {
            new LLMTool
            {
                Name = "get_weather",
                Description = "Get current weather",
                ParametersSchema = @"{""type"": ""object"", ""properties"": {""location"": {""type"": ""string""}}, ""required"": [""location""]}"
            }
        },
        MaxTokens = 1024
    };

    // Act
    StreamingLLMChunk? finalChunk = null;
    await foreach (var chunk in provider.StreamRequestAsync(request))
    {
        if (chunk.IsComplete)
        {
            finalChunk = chunk;
        }
    }

    // Assert
    Assert.IsNotNull(finalChunk);
    Assert.AreEqual("tool_use", finalChunk.FinishReason);
    Assert.IsNotNull(finalChunk.ToolCalls);
    Assert.IsTrue(finalChunk.ToolCalls.Count > 0);
}
```

### Test 3: Usage Information
```csharp
[TestMethod]
public async Task SendRequestAsync_ReturnsUsageInfo()
{
    // Arrange
    var request = new LLMRequest
    {
        Messages = new List<LLMMessage>
        {
            new LLMMessage { Role = "user", Content = "Hello" }
        },
        MaxTokens = 1024
    };

    // Act
    var response = await provider.SendRequestAsync(request);

    // Assert
    Assert.IsNotNull(response.Usage);
    Assert.IsTrue(response.Usage.InputTokens > 0);
    Assert.IsTrue(response.Usage.OutputTokens > 0);
    Assert.AreEqual(
        response.Usage.InputTokens + response.Usage.OutputTokens,
        response.Usage.TotalTokens
    );
}
```

---

## Verification Checklist

After applying fixes:
- [ ] Build succeeds
- [ ] All existing tests pass
- [ ] New tests for tool calls pass
- [ ] Non-streaming with tools works
- [ ] Streaming with tools works
- [ ] stop_reason correctly captured
- [ ] Usage information populated
- [ ] Tool call JSON valid
- [ ] Multi-turn tool conversations work

---

## Additional Resources

- [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md) - Tool calling details
- [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md) - Streaming guide
- [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md) - Debugging tips
