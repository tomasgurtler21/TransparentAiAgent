# Streaming Implementation Guide

**Last Updated:** January 2025
**Critical Document:** This covers the "insanely tough to debug" streaming implementation

---

## Table of Contents

1. [Overview](#overview)
2. [Enabling Streaming](#enabling-streaming)
3. [Complete Event Sequence](#complete-event-sequence)
4. [All Event Types - Detailed](#all-event-types---detailed)
5. [Streaming with Tool Calls](#streaming-with-tool-calls)
6. [Fine-Grained Tool Streaming (Beta)](#fine-grained-tool-streaming-beta)
7. [Accumulation Strategies](#accumulation-strategies)
8. [Error Handling](#error-handling)
9. [Common Bugs and Solutions](#common-bugs-and-solutions)
10. [Complete C# Implementation](#complete-c-implementation)

---

## Overview

Streaming allows real-time response delivery as Claude generates content. This is essential for:
- Real-time UI updates
- Long-form content generation
- Better user experience
- Lower perceived latency

**Key Challenges:**
- Multiple event types to handle
- Tool calls require JSON accumulation across chunks
- stop_reason comes in separate event
- Events can arrive with delays
- Proper cleanup and error handling

---

## Enabling Streaming

### Request Parameter

```json
{
  "model": "claude-sonnet-4-5-20250929",
  "messages": [...],
  "max_tokens": 1024,
  "stream": true  // ← Enable streaming
}
```

### Response Format

Instead of single JSON response, you receive Server-Sent Events (SSE):

```
event: message_start
data: {"type": "message_start", "message": {...}}

event: content_block_delta
data: {"type": "content_block_delta", "index": 0, "delta": {...}}

event: message_stop
data: {"type": "message_stop"}
```

---

## Complete Event Sequence

### Standard Flow (Text Only)

```
1. message_start        → Initial metadata
2. content_block_start  → Text block begins
3. content_block_delta  → Text chunk 1
4. content_block_delta  → Text chunk 2
5. content_block_delta  → Text chunk 3
6. content_block_stop   → Text block complete
7. message_delta        → Usage update, stop_reason
8. message_stop         → Stream complete
```

### Flow with Tool Calls

```
1. message_start
2. content_block_start  → Text block (index 0)
3. content_block_delta  → Text chunks
4. content_block_stop   → Text complete
5. content_block_start  → Tool use block (index 1)
6. content_block_delta  → JSON partial 1
7. content_block_delta  → JSON partial 2
8. content_block_stop   → Tool call complete
9. message_delta        → stop_reason: "tool_use"
10. message_stop         → Stream complete
```

### With Ping Events

```
message_start
→ ping (keepalive - can appear anytime)
→ content_block_start
→ content_block_delta
→ ping (another keepalive)
→ content_block_delta
→ content_block_stop
→ message_delta
→ message_stop
```

**Note:** `ping` events can appear at any time. They should be ignored.

---

## All Event Types - Detailed

### 1. message_start

**When:** First event in stream

```
event: message_start
data: {
  "type": "message_start",
  "message": {
    "id": "msg_1nZdL29xx5MUA1yADyHTEsnR8uuvGzszyY",
    "type": "message",
    "role": "assistant",
    "content": [],
    "model": "claude-sonnet-4-5-20250929",
    "stop_reason": null,
    "stop_sequence": null,
    "usage": {
      "input_tokens": 25,
      "output_tokens": 1
    }
  }
}
```

**Contains:**
- Message ID
- Empty content array (will be populated by subsequent events)
- Initial usage statistics
- Metadata

**What to Do:**
- Store message ID if needed
- Initialize accumulators
- Log start of stream

### 2. content_block_start

**When:** New content block begins

**Text Block:**
```
event: content_block_start
data: {
  "type": "content_block_start",
  "index": 0,
  "content_block": {
    "type": "text",
    "text": ""
  }
}
```

**Tool Use Block:**
```
event: content_block_start
data: {
  "type": "content_block_start",
  "index": 1,
  "content_block": {
    "type": "tool_use",
    "id": "toolu_01A09q90qw90lq917835lq9",
    "name": "get_weather",
    "input": {}
  }
}
```

**Important Fields:**
- `index`: Position in final content array (0-indexed)
- `content_block.type`: `"text"`, `"tool_use"`, or `"thinking"`
- Initial state is empty (text: "", input: {})

**What to Do:**
- Create accumulator for this index
- Store tool ID and name (for tool_use blocks)
- Prepare for deltas

### 3. content_block_delta

**Type A - Text Delta:**
```
event: content_block_delta
data: {
  "type": "content_block_delta",
  "index": 0,
  "delta": {
    "type": "text_delta",
    "text": "Hello"
  }
}
```

**Type B - Tool Input JSON Delta:**
```
event: content_block_delta
data: {
  "type": "content_block_delta",
  "index": 1,
  "delta": {
    "type": "input_json_delta",
    "partial_json": "{\"location\": \"San Fra"
  }
}
```

Next delta:
```
event: content_block_delta
data: {
  "type": "content_block_delta",
  "index": 1,
  "delta": {
    "type": "input_json_delta",
    "partial_json": "ncisco, CA\"}"
  }
}
```

**CRITICAL for Tool Calls:**
- `partial_json` contains **string fragments**, not parsed JSON
- Must accumulate strings then parse at end
- Current models emit one key-value pair at a time
- May have delays between events

**Type C - Thinking Delta:**
```
event: content_block_delta
data: {
  "type": "content_block_delta",
  "index": 0,
  "delta": {
    "type": "thinking_delta",
    "thinking": "Let me analyze..."
  }
}
```

**What to Do:**
- Branch on `delta.type`
- Append to appropriate accumulator for this `index`
- Yield text chunks immediately (for UI updates)
- Store JSON fragments (parse later)

### 4. content_block_stop

**When:** Content block at given index is complete

```
event: content_block_stop
data: {
  "type": "content_block_stop",
  "index": 0
}
```

**What to Do:**
- Content block at this `index` is finalized
- For tool_use blocks: Parse accumulated JSON now
- Validate JSON is complete
- Mark this index as done

### 5. message_delta

**When:** Top-level message changes

```
event: message_delta
data: {
  "type": "message_delta",
  "delta": {
    "stop_reason": "end_turn",
    "stop_sequence": null
  },
  "usage": {
    "output_tokens": 15
  }
}
```

**Critical Fields:**
- `delta.stop_reason`: **THIS IS WHERE stop_reason COMES FROM**
- `usage.output_tokens`: Final token count

**What to Do:**
- **ALWAYS capture stop_reason from this event**
- Store final usage statistics
- Prepare for stream completion

### 6. message_stop

**When:** Stream is complete

```
event: message_stop
data: {
  "type": "message_stop"
}
```

**What to Do:**
- Finalize all accumulators
- Build final response
- Clean up resources
- Return/yield final chunk with `isComplete: true`

### 7. ping

**When:** Any time (keepalive)

```
event: ping
data: {"type": "ping"}
```

**What to Do:**
- Ignore completely
- Or log for monitoring

### 8. error

**When:** Something went wrong

```
event: error
data: {
  "type": "error",
  "error": {
    "type": "overloaded_error",
    "message": "Overloaded"
  }
}
```

**What to Do:**
- Stop processing
- Throw exception
- Clean up resources
- Consider retry logic

---

## Streaming with Tool Calls

### Complete Event Flow Example

**Request:** "What's the weather in SF?"

**Event 1 - message_start:**
```json
{
  "type": "message_start",
  "message": {
    "id": "msg_abc123",
    "content": [],
    "stop_reason": null
  }
}
```

**Event 2 - content_block_start (text):**
```json
{
  "type": "content_block_start",
  "index": 0,
  "content_block": {"type": "text", "text": ""}
}
```

**Event 3-5 - content_block_delta (text):**
```json
{"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": "Let"}}
{"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": " me check"}}
{"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": " the weather."}}
```

**Event 6 - content_block_stop (text):**
```json
{"type": "content_block_stop", "index": 0}
```

**Event 7 - content_block_start (tool_use):**
```json
{
  "type": "content_block_start",
  "index": 1,
  "content_block": {
    "type": "tool_use",
    "id": "toolu_01xyz",
    "name": "get_weather",
    "input": {}
  }
}
```

**Event 8-9 - content_block_delta (JSON):**
```json
{"type": "content_block_delta", "index": 1, "delta": {"type": "input_json_delta", "partial_json": "{\"location\": \"San"}}
{"type": "content_block_delta", "index": 1, "delta": {"type": "input_json_delta", "partial_json": " Francisco, CA\"}"}}
```

**Event 10 - content_block_stop (tool_use):**
```json
{"type": "content_block_stop", "index": 1}
```

**Event 11 - message_delta (CRITICAL):**
```json
{
  "type": "message_delta",
  "delta": {"stop_reason": "tool_use"},
  "usage": {"output_tokens": 45}
}
```

**Event 12 - message_stop:**
```json
{"type": "message_stop"}
```

### Key Insights

1. **Text comes first**, then tool calls
2. **Each content block has unique index**
3. **JSON arrives as string fragments** - concatenate before parsing
4. **stop_reason comes in message_delta**, not message_stop
5. **Delays between JSON deltas are normal** (model is working)

---

## Fine-Grained Tool Streaming (Beta)

### Enabling

Add beta header:
```
anthropic-beta: fine-grained-tool-streaming-2025-05-14
```

AND set `stream: true`.

### Differences from Regular Streaming

**Without Fine-Grained (Regular):**
- Chunks fragmented over ~15 seconds
- Example: `'query": "Ty'`, `'peScri'`, `'pt 5.0 5.1 '`
- One key-value pair at a time

**With Fine-Grained:**
- Substantially larger chunks
- Arrive within ~3 seconds
- Example: `'{"query": "TypeScript 5.0 5.1 5.2 5.3'`, `' new features comparison'`
- Faster, but potentially incomplete JSON

### Edge Cases

**1. Potentially Invalid JSON**

You may receive incomplete or malformed JSON:
```json
{
  "partial_json": "{\"location\": \"San Fra" // Incomplete!
}
```

**Solution:** Always parse in try-catch:
```csharp
try
{
    var input = JsonSerializer.Deserialize<Dictionary<string, object>>(
        accumulatedJson
    );
}
catch (JsonException ex)
{
    _logger.LogWarning("Invalid tool input JSON: {Json}", accumulatedJson);

    // Return error to Claude
    return new ToolResult
    {
        ToolUseId = toolId,
        Content = JsonSerializer.Serialize(new
        {
            INVALID_JSON = accumulatedJson
        }),
        IsError = true
    };
}
```

**2. Max Tokens Hit Mid-Parameter**

Stream terminates before completing JSON:
```json
{
  "partial_json": "{\"query\": \"This is a very long que..."
}
// Stream ends - no closing brace!
```

**Solution:** Detect and handle:
```csharp
if (stopReason == "max_tokens" && !IsValidJson(accumulatedJson))
{
    _logger.LogWarning("Tool call truncated due to max_tokens");
    // Handle gracefully - maybe extend max_tokens and retry
}
```

### Beta Status

- Currently beta feature
- Evaluate response quality before production
- Provide feedback to Anthropic

---

## Accumulation Strategies

### Strategy 1: Per-Index Dictionaries (Recommended)

```csharp
public class StreamAccumulator
{
    // Text content per index
    private Dictionary<int, StringBuilder> _textAccumulators = new();

    // Tool call info per index
    private Dictionary<int, ToolCallInfo> _toolCallInfo = new();

    // JSON accumulation per index
    private Dictionary<int, StringBuilder> _jsonAccumulators = new();

    // Stop reason from message_delta
    private string? _stopReason;

    public class ToolCallInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public void HandleContentBlockStart(ContentBlockStart evt)
    {
        int index = evt.Index;

        if (evt.ContentBlock.TryPickText(out _))
        {
            _textAccumulators[index] = new StringBuilder();
        }
        else if (evt.ContentBlock.TryPickToolUse(out var toolBlock))
        {
            _toolCallInfo[index] = new ToolCallInfo
            {
                Id = toolBlock.ID,
                Name = toolBlock.Name
            };
            _jsonAccumulators[index] = new StringBuilder();
        }
    }

    public void HandleContentBlockDelta(ContentBlockDelta evt)
    {
        int index = evt.Index;

        if (evt.Delta.TryPickText(out var textDelta))
        {
            if (_textAccumulators.ContainsKey(index))
            {
                _textAccumulators[index].Append(textDelta.Text);
            }
        }
        else if (evt.Delta.TryPickInputJson(out var jsonDelta))
        {
            if (_jsonAccumulators.ContainsKey(index))
            {
                _jsonAccumulators[index].Append(jsonDelta.PartialJson);
            }
        }
    }

    public void HandleContentBlockStop(int index)
    {
        // Validate and parse JSON for tool calls
        if (_jsonAccumulators.ContainsKey(index))
        {
            var jsonString = _jsonAccumulators[index].ToString();

            try
            {
                // Validate JSON
                JsonDocument.Parse(jsonString);
                // JSON is valid, ready to use
            }
            catch (JsonException ex)
            {
                _logger.LogError("Invalid JSON at index {Index}: {Json}", index, jsonString);
            }
        }
    }

    public void HandleMessageDelta(MessageDelta evt)
    {
        _stopReason = evt.Delta.StopReason?.ToString();
    }

    public LLMResponse BuildFinalResponse()
    {
        var textContent = string.Empty;
        List<LLMToolCall>? toolCalls = null;

        // Combine all text blocks
        foreach (var kvp in _textAccumulators.OrderBy(x => x.Key))
        {
            textContent += kvp.Value.ToString();
        }

        // Build tool calls
        if (_toolCallInfo.Count > 0)
        {
            toolCalls = new List<LLMToolCall>();
            foreach (var kvp in _toolCallInfo)
            {
                int index = kvp.Key;
                var info = kvp.Value;
                var jsonString = _jsonAccumulators[index].ToString();

                toolCalls.Add(new LLMToolCall(
                    info.Id,
                    info.Name,
                    jsonString
                ));
            }
        }

        return new LLMResponse(
            content: textContent,
            toolCalls: toolCalls,
            finishReason: _stopReason ?? "unknown",
            usage: null
        );
    }
}
```

### Strategy 2: StreamingResponseAccumulator (Existing Class)

If your project already has `StreamingResponseAccumulator`:

```csharp
var accumulator = new StreamingResponseAccumulator();

await foreach (var evt in stream)
{
    if (evt.TryPickContentBlockDelta(out var delta))
    {
        if (delta.Delta.TryPickText(out var text))
        {
            accumulator.AccumulateChunk(new StreamingLLMChunk(
                contentDelta: text.Text,
                toolCallDelta: null,
                isComplete: false,
                finishReason: null
            ));
        }
        else if (delta.Delta.TryPickInputJson(out var json))
        {
            accumulator.AccumulateToolInput(delta.Index, json.PartialJson);
        }
    }
    // ... handle other events
}
```

---

## Error Handling

### Strategy 1: Graceful Degradation

```csharp
StringBuilder textAccumulator = new();
bool hasError = false;
string? errorMessage = null;

await foreach (var evt in stream)
{
    try
    {
        if (evt.TryPickError(out var error))
        {
            hasError = true;
            errorMessage = error.Error.Message;
            break;
        }

        ProcessEvent(evt);
    }
    catch (Exception ex)
    {
        hasError = true;
        errorMessage = ex.Message;
        _logger.LogError(ex, "Error processing stream event");
        break;
    }
}

if (hasError)
{
    // Attempt to construct partial response
    var partialResponse = new LLMResponse(
        content: textAccumulator.ToString(),
        toolCalls: null,
        finishReason: "error",
        usage: null
    );

    throw new LLMException($"Stream error: {errorMessage}", partialResponse);
}
```

### Strategy 2: Resumption from Partial

If interruption occurs mid-stream:

```csharp
public async Task<LLMResponse> SendWithResumption(LLMRequest request)
{
    string? partialContent = null;
    int attempts = 0;
    const int maxAttempts = 3;

    while (attempts < maxAttempts)
    {
        try
        {
            var messages = BuildMessages(request, partialContent);

            await foreach (var evt in StreamRequest(messages))
            {
                if (evt.TryPickContentBlockDelta(out var delta))
                {
                    if (delta.Delta.TryPickText(out var text))
                    {
                        partialContent += text.Text;
                    }
                }
                // Process other events...
            }

            // Success - return response
            return BuildResponse(partialContent);
        }
        catch (Exception ex)
        {
            attempts++;
            _logger.LogWarning("Stream attempt {Attempt} failed: {Message}",
                attempts, ex.Message);

            if (attempts >= maxAttempts)
            {
                throw;
            }

            // Resume with partial content
            await Task.Delay(1000 * attempts); // Backoff
        }
    }

    throw new LLMException("Failed after max attempts");
}

private List<MessageParam> BuildMessages(LLMRequest request, string? partialContent)
{
    var messages = request.Messages.ToList();

    if (partialContent != null)
    {
        // Add partial assistant response
        messages.Add(new MessageParam
        {
            Role = Role.Assistant,
            Content = new Content(partialContent)
        });

        // Ask to continue
        messages.Add(new MessageParam
        {
            Role = Role.User,
            Content = new Content("Please continue")
        });
    }

    return messages;
}
```

**Limitation:** Cannot resume from partial tool_use or thinking blocks—only text blocks.

### Strategy 3: Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    await foreach (var evt in stream.WithCancellation(cts.Token))
    {
        if (shouldCancel)
        {
            cts.Cancel();
            break;
        }

        ProcessEvent(evt);
    }
}
catch (OperationCanceledException)
{
    _logger.LogInformation("Stream cancelled by user");
    // Clean up resources
}
finally
{
    cts.Dispose();
}
```

---

## Common Bugs and Solutions

### Bug 1: Stream Prints Nothing

**Symptom:** Stream completes but no output.

**Cause:** Not handling `content_block_delta` events.

**Solution:**
```csharp
await foreach (var evt in stream)
{
    if (evt.TryPickContentBlockDelta(out var delta))
    {
        if (delta.Delta.TryPickText(out var textDelta))
        {
            Console.Write(textDelta.Text); // ← Add this!
        }
    }
}
```

### Bug 2: Tool Calls Lost

**Symptom:** Text appears but tool calls missing.

**Cause:** Only handling `text_delta`, not `input_json_delta`.

**Solution:**
```csharp
if (evt.TryPickContentBlockDelta(out var delta))
{
    if (delta.Delta.TryPickText(out var textDelta))
    {
        AccumulateText(delta.Index, textDelta.Text);
    }
    else if (delta.Delta.TryPickInputJson(out var jsonDelta))  // ← Add this!
    {
        AccumulateToolJson(delta.Index, jsonDelta.PartialJson);
    }
}
```

### Bug 3: Malformed Tool JSON

**Symptom:** JSON parsing fails after accumulation.

**Cause:** Mixing JSON from different indices.

**Solution:** Use per-index accumulators:
```csharp
Dictionary<int, StringBuilder> jsonAccumulators = new();

// On input_json_delta:
if (!jsonAccumulators.ContainsKey(index))
    jsonAccumulators[index] = new StringBuilder();

jsonAccumulators[index].Append(partialJson);
```

### Bug 4: Wrong stop_reason

**Symptom:** Always reports "stop" regardless of actual reason.

**Cause:** Hardcoding stop_reason instead of capturing from `message_delta`.

**Solution:**
```csharp
string? stopReason = null;

if (evt.TryPickMessageDelta(out var msgDelta))
{
    stopReason = msgDelta.Delta.StopReason?.ToString();  // ← Capture here!
}

if (evt.TryPickStop(out _))
{
    // Use captured stopReason
    return BuildResponse(stopReason ?? "unknown");
}
```

### Bug 5: Premature Finalization

**Symptom:** Response processed before all content received.

**Cause:** Not waiting for `message_stop` event.

**Solution:**
```csharp
bool isComplete = false;

await foreach (var evt in stream)
{
    // ... process events ...

    if (evt.TryPickStop(out _))
    {
        isComplete = true;
        break;
    }
}

if (isComplete)
{
    return BuildFinalResponse();
}
else
{
    throw new Exception("Stream ended without message_stop");
}
```

---

## Complete C# Implementation

### Full Working Example

```csharp
public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
    LLMRequest request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    if (request == null)
        throw new ArgumentNullException(nameof(request));

    // Accumulators
    var textAccumulators = new Dictionary<int, StringBuilder>();
    var toolCallInfo = new Dictionary<int, (string Id, string Name)>();
    var jsonAccumulators = new Dictionary<int, StringBuilder>();
    string? stopReason = null;

    // Build request
    var messageParams = BuildMessageRequest(request, null);

    // Log request
    var correlationId = Guid.NewGuid().ToString();
    LogRawRequest(request, messageParams, correlationId);

    // Get stream
    IAsyncEnumerable<RawMessageStreamEvent> stream;
    try
    {
        stream = _client.Messages.CreateStreaming(messageParams);
    }
    catch (Exception ex)
    {
        _transparencyService.LogEvent(new TransparencyEvent(
            TransparencyEventType.Error,
            $"Streaming request failed: {ex.Message}",
            "LLM Error"));
        throw new LLMException($"Anthropic streaming failed: {ex.Message}", ex);
    }

    // Process stream
    await foreach (var evt in stream.WithCancellation(cancellationToken))
    {
        // Handle content_block_start
        if (evt.TryPickContentBlockStart(out var blockStart))
        {
            int index = blockStart.Index;

            if (blockStart.ContentBlock.TryPickText(out _))
            {
                textAccumulators[index] = new StringBuilder();
            }
            else if (blockStart.ContentBlock.TryPickToolUse(out var toolBlock))
            {
                toolCallInfo[index] = (toolBlock.ID, toolBlock.Name);
                jsonAccumulators[index] = new StringBuilder();
            }
        }
        // Handle content_block_delta
        else if (evt.TryPickContentBlockDelta(out var delta))
        {
            int index = delta.Index;

            if (delta.Delta.TryPickText(out var textDelta))
            {
                if (textAccumulators.ContainsKey(index))
                {
                    textAccumulators[index].Append(textDelta.Text);
                }

                // Yield text chunk immediately
                yield return new StreamingLLMChunk(
                    contentDelta: textDelta.Text,
                    toolCallDelta: null,
                    isComplete: false,
                    finishReason: null
                );
            }
            else if (delta.Delta.TryPickInputJson(out var jsonDelta))
            {
                if (jsonAccumulators.ContainsKey(index))
                {
                    jsonAccumulators[index].Append(jsonDelta.PartialJson);
                }
            }
        }
        // Handle content_block_stop
        else if (evt.TryPickContentBlockStop(out var blockStop))
        {
            int index = blockStop.Index;

            // Validate JSON for tool calls
            if (jsonAccumulators.ContainsKey(index))
            {
                var jsonString = jsonAccumulators[index].ToString();
                try
                {
                    JsonDocument.Parse(jsonString);
                    _logger.LogDebug("Tool call JSON valid at index {Index}", index);
                }
                catch (JsonException ex)
                {
                    _logger.LogError("Invalid tool call JSON at index {Index}: {Json}",
                        index, jsonString);
                }
            }
        }
        // Handle message_delta (CRITICAL - captures stop_reason)
        else if (evt.TryPickMessageDelta(out var msgDelta))
        {
            stopReason = msgDelta.Delta.StopReason?.ToString();
        }
        // Handle message_stop
        else if (evt.TryPickStop(out _))
        {
            // Build final chunk with tool calls if any
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
                ToolCalls = toolCalls
            };

            // Log completion
            var latency = DateTime.UtcNow - startTime;
            var fullText = string.Join("", textAccumulators.Values.Select(sb => sb.ToString()));
            LogStreamingResponse(fullText, stopReason, correlationId, latency);

            break;
        }
        // Handle ping (ignore)
        else if (evt.TryPickPing(out _))
        {
            // Ignore keepalive pings
        }
    }
}
```

---

## Logging Best Practices

### Environment Variable

```bash
export ANTHROPIC_LOG=debug  # or info
```

### What to Log

**Essential:**
```csharp
_logger.LogInformation("Stream started: {CorrelationId}", correlationId);
_logger.LogInformation("stop_reason: {StopReason}", stopReason);
_logger.LogInformation("Tool calls: {Count}", toolCalls?.Count ?? 0);
```

**Debug:**
```csharp
_logger.LogDebug("Event: {Type}", evt.GetType().Name);
_logger.LogDebug("Text delta: {Length} chars", textDelta.Text.Length);
_logger.LogDebug("JSON delta: {Fragment}", jsonDelta.PartialJson);
_logger.LogDebug("Accumulated JSON at index {Index}: {Json}", index, jsonString);
```

### Event Sequence Tracking

```csharp
List<string> eventLog = new();

await foreach (var evt in stream)
{
    string eventType = evt.GetType().Name;
    eventLog.Add(eventType);

    // Process event...
}

_logger.LogInformation("Event sequence: {Sequence}",
    string.Join(" → ", eventLog));
```

---

## Testing Streaming

### Test Checklist

- [ ] Text-only streaming works
- [ ] Tool calls captured in streaming
- [ ] Multiple tool calls in one stream
- [ ] JSON accumulation correct
- [ ] stop_reason properly captured
- [ ] Error handling works
- [ ] Cancellation works
- [ ] Partial content resumption works
- [ ] No memory leaks
- [ ] Proper cleanup on error

### Test Scenarios

**1. Simple Text Stream:**
```csharp
var chunks = new List<string>();
await foreach (var chunk in StreamRequest(simpleTextRequest))
{
    if (!string.IsNullOrEmpty(chunk.ContentDelta))
    {
        chunks.Add(chunk.ContentDelta);
    }
}
Assert.IsTrue(chunks.Count > 0);
```

**2. Stream with Tool Calls:**
```csharp
StreamingLLMChunk? finalChunk = null;
await foreach (var chunk in StreamRequest(toolCallRequest))
{
    if (chunk.IsComplete)
    {
        finalChunk = chunk;
    }
}
Assert.AreEqual("tool_use", finalChunk?.FinishReason);
Assert.IsNotNull(finalChunk?.ToolCalls);
Assert.IsTrue(finalChunk.ToolCalls.Count > 0);
```

---

## Additional Resources

- [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md) - API basics
- [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md) - Tool calling details
- [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md) - More debugging tips
- [07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md) - Fixes for your code

---

**Next:** [C# SDK Reference →](04_CSHARP_SDK_REFERENCE.md)
