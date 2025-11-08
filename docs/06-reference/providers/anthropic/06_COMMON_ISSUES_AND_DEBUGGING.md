# Common Issues and Debugging

**Last Updated:** January 2025

---

## Tool Calling Issues

### Issue: Claude Adds Unwanted Parameters

**Symptom:** Claude persistently adds parameters not requested (e.g., adding "2025" to searches)

**Solution:** Optimize tool description to be explicit about what NOT to do:
```json
{
  "name": "search",
  "description": "Search for information. Do NOT add year constraints unless explicitly requested by the user. The search is already date-aware and will return recent results automatically."
}
```

**Lesson:** Tool descriptions guide behavior—be explicit about restrictions.

### Issue: Invalid Parameters Despite Schema

**Symptom:** Claude provides parameters that don't match schema

**Behavior:** Model automatically retries 2-3 times, then apologizes

**Solution:** Provide clear error feedback:
```json
{
  "type": "tool_result",
  "tool_use_id": "toolu_01",
  "content": "Error: 'date' parameter must be in YYYY-MM-DD format. Received: '2025-1-5'. Please use leading zeros.",
  "is_error": true
}
```

### Issue: Tool Not Selected When Expected

**Possible Causes:**
1. Tool description not clear enough
2. Tool name doesn't match semantics
3. Model thinks it can answer without tool

**Solutions:**
- Make description longer (3-4 sentences minimum)
- Use `tool_choice: {"type": "any"}` to force tool use
- Add examples in description of when to use

### Issue: JSON Schema Validation Errors

**Symptom:** API returns 400: "Unsupported JSON Schema keyword"

**Cause:** Using `oneOf`, `allOf`, or `anyOf` at top level

**Solution:** Simplify schema (see [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md))

---

## Streaming Issues

### Issue: Stream Prints Nothing

**Symptom:** Stream completes but no output

**Debugging Steps:**
1. Log event types:
```csharp
await foreach (var evt in stream)
{
    Console.WriteLine($"Event: {evt.GetType().Name}");
}
```

2. Ensure handling `content_block_delta`:
```csharp
if (evt.TryPickContentBlockDelta(out var delta))
{
    if (delta.Delta.TryPickText(out var text))
    {
        Console.Write(text.Text);  // ← Must have this!
    }
}
```

### Issue: Tool Calls Lost in Streaming

**Symptom:** Text appears but tool calls missing

**Cause:** Only handling `text_delta`, not `input_json_delta`

**Solution:** Add JSON delta handling:
```csharp
else if (delta.Delta.TryPickInputJson(out var jsonDelta))
{
    AccumulateToolJson(delta.Index, jsonDelta.PartialJson);
}
```

### Issue: Delays Between Streaming Events

**Symptom:** Long pauses between `content_block_delta` events

**Cause:** Current models emit one complete key-value pair at a time for tool inputs

**Explanation:** This is **expected behavior**, not a bug. Model is working between emissions.

**Workaround:** Use fine-grained tool streaming beta to reduce delays.

### Issue: JSON Parsing Fails After Accumulation

**Symptom:** Accumulated JSON string is invalid

**Causes:**
1. Mixing JSON from different content blocks
2. Not tracking indices properly
3. Including non-JSON content

**Solution:** Use per-index accumulators:
```csharp
Dictionary<int, StringBuilder> jsonAccumulators = new();

// On content_block_start (tool_use):
if (blockStart.ContentBlock.TryPickToolUse(out var tool))
{
    jsonAccumulators[blockStart.Index] = new StringBuilder();
}

// On content_block_delta (input_json):
if (delta.Delta.TryPickInputJson(out var json))
{
    jsonAccumulators[delta.Index].Append(json.PartialJson);
}

// On content_block_stop:
var jsonString = jsonAccumulators[stopIndex].ToString();
JsonDocument.Parse(jsonString); // Validate
```

---

## Error Messages

### "invalid_request_error: Invalid tool input schema"

**Meaning:** JSON Schema validation failed

**Common Causes:**
- Using `oneOf`, `allOf`, `anyOf` at top level
- Invalid JSON Schema syntax
- Missing required fields

**Solution:** Simplify schema, validate JSON structure

### "rate_limit_error" (429)

**Meaning:** Exceeded RPM, ITPM, or OTPM limits

**Solution:**
- Implement exponential backoff
- Use prompt caching to reduce ITPM
- Upgrade to higher tier

**Retry Pattern:**
```csharp
int retries = 0;
while (retries < 3)
{
    try
    {
        return await client.Messages.Create(params);
    }
    catch (AnthropicRateLimitException)
    {
        retries++;
        await Task.Delay(1000 * (int)Math.Pow(2, retries));
    }
}
```

### "overloaded_error" (529)

**Meaning:** API temporarily overloaded

**Solution:** Retry with exponential backoff (usually resolves quickly)

### "model_context_window_exceeded"

**Meaning:** Total tokens exceed model's context window

**Solutions:**
- Reduce conversation history
- Summarize older messages
- Use shorter prompts
- For Sonnet 4.5: Use 1M context beta

---

## Debugging Techniques

### Technique 1: Enable Verbose Logging

**Environment Variable:**
```bash
export ANTHROPIC_LOG=debug
```

**In Code:**
```csharp
await foreach (var evt in stream)
{
    _logger.LogDebug("Event: {Event}",
        JsonSerializer.Serialize(evt));

    // Process...
}
```

### Technique 2: Track Event Sequence

```csharp
List<string> eventLog = new();

await foreach (var evt in stream)
{
    string type = "unknown";

    if (evt.TryPickMessageStart(out _)) type = "message_start";
    else if (evt.TryPickContentBlockStart(out _)) type = "content_block_start";
    else if (evt.TryPickContentBlockDelta(out _)) type = "content_block_delta";
    // ... etc

    eventLog.Add(type);
}

_logger.LogInformation("Sequence: {Seq}",
    string.Join(" → ", eventLog));
```

### Technique 3: Accumulation Verification

```csharp
private class DebugState
{
    public StringBuilder Text { get; } = new();
    public Dictionary<int, StringBuilder> Jsons { get; } = new();
    public List<string> Log { get; } = new();
}

var state = new DebugState();

await foreach (var evt in stream)
{
    if (evt.TryPickContentBlockDelta(out var delta))
    {
        if (delta.Delta.TryPickText(out var text))
        {
            state.Text.Append(text.Text);
            state.Log.Add($"text[{delta.Index}]: {text.Text.Length} chars");
        }
        else if (delta.Delta.TryPickInputJson(out var json))
        {
            if (!state.Jsons.ContainsKey(delta.Index))
                state.Jsons[delta.Index] = new StringBuilder();

            state.Jsons[delta.Index].Append(json.PartialJson);
            state.Log.Add($"json[{delta.Index}]: {json.PartialJson.Length} chars");
        }
    }
    else if (evt.TryPickContentBlockStop(out var stop))
    {
        if (state.Jsons.ContainsKey(stop.Index))
        {
            var json = state.Jsons[stop.Index].ToString();
            try
            {
                JsonDocument.Parse(json);
                state.Log.Add($"  ✅ Valid JSON ({json.Length} chars)");
            }
            catch (JsonException ex)
            {
                state.Log.Add($"  ❌ Invalid: {ex.Message}");
                _logger.LogError("JSON: {Json}", json);
            }
        }
    }
}

_logger.LogInformation("Debug log:\n{Log}",
    string.Join("\n", state.Log));
```

---

## Common Mistakes

### Mistake 1: Not Handling All Event Types

**Problem:**
```csharp
await foreach (var evt in stream)
{
    if (evt.TryPickContentBlockDelta(out var delta))
    {
        // Only handling deltas!
    }
}
```

**Solution:** Handle or explicitly ignore all types:
```csharp
if (evt.TryPickMessageStart(out _)) { /* ... */ }
else if (evt.TryPickContentBlockStart(out _)) { /* ... */ }
else if (evt.TryPickContentBlockDelta(out _)) { /* ... */ }
else if (evt.TryPickContentBlockStop(out _)) { /* ... */ }
else if (evt.TryPickMessageDelta(out _)) { /* ... */ }
else if (evt.TryPickStop(out _)) { /* ... */ }
else if (evt.TryPickPing(out _)) { /* ignore */ }
```

### Mistake 2: Finalizing Response Too Early

**Problem:**
```csharp
await foreach (var evt in stream)
{
    if (evt.TryPickContentBlockDelta(out var delta))
    {
        accumulator.Append(delta.Text);
    }
}
return accumulator.ToString(); // Missing tool calls!
```

**Solution:** Only finalize on `message_stop`:
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

if (isComplete) return BuildFinalResponse();
```

### Mistake 3: Ignoring stop_reason

**Problem:**
```csharp
if (evt.TryPickStop(out _))
{
    return BuildResponse(); // Doesn't check stop_reason!
}
```

**Solution:** Check and act on stop_reason:
```csharp
string? stopReason = null;

if (evt.TryPickMessageDelta(out var msgDelta))
{
    stopReason = msgDelta.Delta.StopReason?.ToString();
}

if (evt.TryPickStop(out _))
{
    if (stopReason == "tool_use")
    {
        // Execute tools and continue conversation
        return await HandleToolCalls();
    }
    else
    {
        return BuildFinalResponse();
    }
}
```

---

## Known Infrastructure Issues (2025)

### August-September 2025 Incidents

Anthropic experienced three infrastructure bugs:

1. **Context Window Routing Error** (Aug 31)
   - Affected Sonnet 4
   - Peak: 16% of requests
   - Symptom: Degraded output quality

2. **Output Corruption on TPU** (Aug 25 - Sep 2)
   - Affected Opus 4.1, Opus 4, Sonnet 4
   - Misconfiguration triggered token generation errors

3. **XLA:TPU Compiler Bug** (Two Weeks)
   - Affected Haiku 3.5
   - Latent bug in top-k compilation
   - Symptom: Inconsistent quality

**Key Takeaway:** These were infrastructure bugs, not intentional throttling. Anthropic never reduces model quality due to demand.

---

## Testing & Validation

### Test Checklist

**Non-Streaming:**
- [ ] Simple chat works
- [ ] System prompts work
- [ ] Tool calls detected
- [ ] Tool results processed
- [ ] Multi-turn conversations
- [ ] Error handling
- [ ] Usage tracking

**Streaming:**
- [ ] Text-only streaming works
- [ ] Tool calls captured
- [ ] Multiple tool calls work
- [ ] JSON accumulation correct
- [ ] stop_reason captured
- [ ] Cancellation works
- [ ] Error handling works

### Integration Testing

**Scenario 1: Simple Chat**
```csharp
var response = await SendRequest("What is 2+2?");
Assert.AreEqual("end_turn", response.FinishReason);
Assert.IsTrue(response.Content.Contains("4"));
```

**Scenario 2: Tool Calling**
```csharp
var response = await SendRequest("What's the weather in SF?");
Assert.AreEqual("tool_use", response.FinishReason);
Assert.IsNotNull(response.ToolCalls);
Assert.IsTrue(response.ToolCalls.Any(t => t.FunctionName == "get_weather"));
```

**Scenario 3: Streaming with Tools**
```csharp
StreamingLLMChunk? finalChunk = null;
await foreach (var chunk in StreamRequest("What's the weather?"))
{
    if (chunk.IsComplete) finalChunk = chunk;
}
Assert.IsNotNull(finalChunk);
Assert.AreEqual("tool_use", finalChunk.FinishReason);
```

---

## Additional Resources

- [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md)
- [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md)
- [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md)
- [07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md) - Fixes for your code
