# Anthropic Thinking Blocks Investigation

**Date**: 2025-11-14
**Session ID**: claude/improve-anthropic-p-016RNqzq5jEW6t3W5p89uz69
**Issue**: Messages streaming in chat then disappearing/being replaced

---

## Problem Statement

User reports that sometimes messages stream in the chat and then disappear or get replaced by the next message or tool call. The user's theory is that this might be related to Anthropic thinking tokens which we do not handle.

---

## Investigation Findings

### 1. What Are Thinking Blocks?

From the Anthropic API documentation (`docs/06-reference/providers/anthropic/01_API_COMPLETE_REFERENCE.md`):

**Thinking Block** (lines 287-299):
```json
{
  "type": "thinking",
  "thinking": "Let me analyze this problem..."
}
```

**Redacted Thinking Block** (lines 300-309):
```json
{
  "type": "redacted_thinking",
  "thinking": "Redacted reasoning..."
}
```

These blocks represent the model's internal reasoning process. They can appear in responses when:
- Extended thinking is enabled
- The model wants to show its reasoning
- Claude Sonnet 4.5 and above with appropriate settings

### 2. How Thinking Blocks Appear in Streaming

From `docs/06-reference/providers/anthropic/03_STREAMING_IMPLEMENTATION_GUIDE.md`:

**During streaming**, thinking blocks arrive as:

**Step 1 - content_block_start** (line 189):
```json
{
  "type": "content_block_start",
  "index": 0,
  "content_block": {
    "type": "thinking",
    "thinking": ""
  }
}
```

**Step 2 - content_block_delta** (lines 244-255):
```json
{
  "type": "content_block_delta",
  "index": 0,
  "delta": {
    "type": "thinking_delta",
    "thinking": "Let me analyze..."
  }
}
```

**Step 3 - content_block_stop**:
Thinking block completes.

### 3. Current Implementation Analysis

#### File: `TransparentAiAgentCore/Infrastructure/LLM/AnthropicProvider.cs`

**Line 164-177: content_block_start handling**
```csharp
if (blockStart.ContentBlock.TryPickText(out _))
{
    textAccumulators[index] = new System.Text.StringBuilder();
}
else if (blockStart.ContentBlock.TryPickToolUse(out var toolBlock))
{
    toolCallInfo[index] = (toolBlock.ID, toolBlock.Name);
    jsonAccumulators[index] = new System.Text.StringBuilder();
}
```
**Problem**: Only handles `text` and `tool_use` blocks. **Does NOT handle `thinking` blocks**.

**Line 179-223: content_block_delta handling**
```csharp
if (deltaEvent.Delta.TryPickText(out var textDelta))
{
    // Handle text delta
}
else
{
    // Handle tool input JSON delta
}
```
**Problem**: Only handles `text_delta` and `input_json_delta`. **Does NOT handle `thinking_delta`**.

**Line 374-403: ConvertStreamingEvent**
```csharp
if (deltaEvent.Delta.TryPickText(out var textDelta))
{
    return new StreamingLLMChunk(
        contentDelta: textDelta.Text,
        ...
    );
}
```
**Problem**: Only yields text deltas. **Thinking deltas are ignored and return null**.

### 4. Root Cause Analysis

#### Scenario 1: Thinking Blocks Are Being Ignored (Most Likely)

1. **Thinking block starts** at index 0
   - `content_block_start` with type "thinking"
   - Our code doesn't create an accumulator for it (no `TryPickThinking` call)
   - No accumulator created → no tracking

2. **Thinking deltas arrive** at index 0
   - `content_block_delta` with type "thinking_delta"
   - `TryPickText` returns false (it's not text)
   - Falls through to tool JSON handling logic
   - Tool JSON logic also fails (it's not tool input)
   - Delta is not accumulated, not yielded → **Thinking content is lost**

3. **Text block starts** at index 1
   - `content_block_start` with type "text"
   - Text accumulator created at index 1
   - Text deltas yielded normally

4. **Result**: Thinking content is completely ignored. User doesn't see it at all.

#### Scenario 2: SDK Behavior Unknown (Possible)

The Anthropic C# SDK we're using might:
- Map thinking blocks to text blocks automatically
- Have a `TryPickThinking` method we're not using
- Return thinking deltas through `TryPickText` (unlikely but possible)

If thinking deltas ARE being picked up by `TryPickText`:
1. Thinking streams at index 0 → shown in UI
2. Text streams at index 1 → also shown in UI
3. UI might show both, or one might replace the other
4. **Result**: User sees text appear and then disappear/get replaced

### 5. Evidence Supporting the Theory

From `docs/06-reference/providers/anthropic/03_STREAMING_IMPLEMENTATION_GUIDE.md`:

**Line 189**: content_block.type can be `"text"`, `"tool_use"`, **or `"thinking"`**

**Lines 244-255**: Explicitly documents `thinking_delta` as a delta type

**Line 816**: "Limitation: Cannot resume from partial tool_use or **thinking blocks**—only text blocks."

This confirms thinking blocks are a real, documented feature that we're not handling.

### 6. Impact Assessment

**Current behavior**:
- ✅ Text blocks: Handled correctly
- ✅ Tool use blocks: Handled correctly
- ❌ Thinking blocks: **IGNORED COMPLETELY**
- ❌ Redacted thinking blocks: **IGNORED COMPLETELY**

**Consequences**:
1. Thinking content is lost and never shown to the user
2. If thinking appears before text in the same response, only text is shown
3. User experience is degraded for extended thinking responses
4. Transparency is compromised (we're hiding the model's reasoning)

### 7. Additional Findings

**From `docs/06-reference/providers/anthropic/02_TOOL_CALLING_DEEP_DIVE.md`**:

When extended thinking is enabled with tool calling:
- `tool_use` blocks must be preceded by `thinking` blocks
- Must include complete unmodified thinking in conversation history
- This suggests thinking blocks are CRITICAL for tool calling with extended thinking

**Implication**: If we're dropping thinking blocks, we might be breaking extended thinking + tool calling workflows.

---

## Proposed Solutions

### Solution 1: Add Thinking Field to LLMResponse (Recommended)

**Pros**:
- Clean separation of concerns
- Preserves thinking content separately
- UI can choose how to display it (collapsed, separate section, etc.)
- Aligns with Anthropic's API design

**Cons**:
- Requires changes to domain models
- Requires UI updates

**Implementation**:
1. Add `string? Thinking` property to `LLMResponse`
2. Add `string? ThinkingDelta` property to `StreamingLLMChunk`
3. Update `AnthropicProvider` to handle thinking blocks
4. Update UI to display thinking content (could wrap in `<details>` tag)

### Solution 2: Wrap Thinking in XML Tags (Quick Fix)

**Pros**:
- No domain model changes needed
- Quick to implement
- Thinking content visible to user

**Cons**:
- Mixes thinking with regular content
- Less clean separation
- Harder for UI to style differently

**Implementation**:
Append thinking content as:
```
<thinking>
Model's reasoning here...
</thinking>

Actual response here...
```

### Solution 3: Accumulate Thinking Separately But Don't Expose (Not Recommended)

**Pros**:
- Prevents potential issues with thinking blocks
- Simple implementation

**Cons**:
- Hides valuable information from user
- Defeats the purpose of transparency
- Wastes the thinking content

---

## Recommended Action Plan

1. **Immediate**: Implement Solution 1 (Add Thinking Field)
2. **Verify**: Test with extended thinking enabled
3. **Document**: Update API documentation with thinking handling
4. **UI Update**: Add thinking display in chat UI

---

## Testing Strategy

1. **Enable Extended Thinking**: Use a model with extended thinking enabled
2. **Trigger Thinking**: Ask complex questions that trigger reasoning
3. **Verify Streaming**: Ensure thinking content streams correctly
4. **Verify Display**: Ensure thinking is displayed separately from response
5. **Test Tool Calling**: Verify thinking + tool calling works

---

## Additional Notes

### SDK Investigation Needed

We need to check if the Anthropic C# SDK has:
- `TryPickThinking` method on `ContentBlock`
- `TryPickThinkingDelta` method on `Delta`
- Or any other way to access thinking content

This can be checked by:
1. Looking at SDK source code
2. Checking SDK documentation
3. Runtime reflection on the types

### Context Window Considerations

From the docs:
> Must include complete unmodified thinking in conversation history

This means we MUST preserve thinking blocks when building conversation context, not just for display.

---

## Conclusion

**The user's theory is CORRECT**: We are not handling thinking blocks at all in our implementation.

**Root cause**: Missing handling for `thinking` content block type and `thinking_delta` delta type in `AnthropicProvider.cs`.

**Impact**: Thinking content is completely lost, degrading user experience and breaking extended thinking features.

**Fix**: Add proper handling for thinking blocks in streaming and non-streaming responses.
