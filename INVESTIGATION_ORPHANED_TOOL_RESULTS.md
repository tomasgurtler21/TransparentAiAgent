# Investigation: Orphaned Tool Results Causing Anthropic API Errors

**Date:** 2025-11-10
**Issue ID:** 011CUzvrnZ88NguKgcFaE11H
**Branch:** `claude/investigate-issue-011CUzvrnZ88NguKgcFaE11H`

## Error Description

Users were experiencing intermittent `BadRequest` errors from the Anthropic API with the following message:

```
Error: Status Code: BadRequest
{
  "type":"error",
  "error":{
    "type":"invalid_request_error",
    "message":"messages.2.content.1: unexpected `tool_use_id` found in `tool_result` blocks: toolu_016sg9LZJaGt7Yrdn24KFvbB. Each `tool_result` block must have a corresponding `tool_use` block in the previous message."
  },
  "request_id":"req_011CUzvUw1kf7pe5a67rCDYK"
}
```

## Root Cause Analysis

### The Problem

The error occurred when the conversation contained `tool_result` messages without their corresponding `tool_use` messages. Analysis of the request JSON showed:

```
Message 1: User - "can you explain to me what tools are?"
Message 2: Assistant - [explanation about tools]
Message 3: User - "show me the panel"
Message 4: User - [tool_result with tool_use_id "toolu_016sg9LZJaGt7Yrdn24KFvbB"]
Message 5: User - "cool, how did you do it?"
```

**The assistant message with `tool_use` was missing between message 3 and 4.**

### Why This Happens

The application uses context window truncation to manage conversation length:

1. **Normal Flow:**
   - User sends message: "show me the panel"
   - Assistant responds with `tool_use` block (AssistantToolCallMessage)
   - Tool executes and returns `tool_result` (ToolResultMessage)
   - Both messages added to conversation

2. **Truncation Issue:**
   - ConversationManager truncates old messages when context window limit reached
   - Truncation uses a simple "oldest first" strategy
   - AssistantToolCallMessage can be truncated from context
   - ToolResultMessage remains in context (it's newer)
   - Request sent to Anthropic has orphaned `tool_result`

### Code Flow

1. **AgentOrchestrator.ExecuteToolCallsAsync()** (line 143-211)
   - Creates AssistantToolCallMessage with tool calls
   - Adds it to conversation (line 168)
   - Executes tools and creates ToolResultMessage for each
   - Adds tool results to conversation (line 194)

2. **ConversationManager.TruncateIfNeeded()** (line 149-193)
   - Removes oldest messages when context limit exceeded
   - No awareness of tool_use/tool_result relationships
   - Can break message pairs

3. **AnthropicProvider.ConvertToAnthropicMessages()** (line 513-626)
   - Converts domain messages to Anthropic API format
   - Previously had no validation for orphaned tool results
   - Would send invalid requests to API

## Solution

### Defensive Fix in AnthropicProvider

Added two-pass validation in `ConvertToAnthropicMessages()`:

**Pass 1: Collect Valid Tool Use IDs**
```csharp
var validToolUseIds = new HashSet<string>();
foreach (var msg in llmMessages)
{
    if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) &&
        msg.ToolCalls != null)
    {
        foreach (var toolCall in msg.ToolCalls)
        {
            validToolUseIds.Add(toolCall.Id);
        }
    }
}
```

**Pass 2: Filter Orphaned Tool Results**
```csharp
if (!validToolUseIds.Contains(toolMsg.ToolCallId))
{
    _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
        Domain.Transparency.TransparencyEventType.Error,
        $"Skipping orphaned tool_result with ID '{toolMsg.ToolCallId}' - " +
        $"corresponding tool_use not found in conversation context. " +
        $"This typically happens when context truncation removes the " +
        $"assistant message with tool_use but leaves the tool_result.",
        "Orphaned Tool Result Filtered"));
    i++;
    continue;
}
```

**Skip Empty Tool Result Messages**
```csharp
// Only create the user message if we have at least one valid tool result
if (toolResultBlocks.Count > 0)
{
    var messageParam = new MessageParam
    {
        Role = Role.User,
        Content = new Content(toolResultBlocks)
    };
    messages.Add(messageParam);
}
```

### Why This Approach

1. **Defensive:** Prevents API errors even if upstream truncation logic changes
2. **Transparent:** Logs when filtering occurs for debugging
3. **Minimal Impact:** Only affects messages being sent to API, not conversation history
4. **Safe:** Skipping orphaned results is better than crashing with API error

### Alternative Approaches Considered

1. **Smarter Truncation** - Make ConversationManager aware of tool pairs
   - More complex
   - Requires understanding of message relationships at truncation time
   - Still need defensive fix for safety

2. **Keep All Tool Messages** - Never truncate AssistantToolCallMessage or ToolResultMessage
   - Can exhaust context window with tool-heavy conversations
   - Doesn't solve fundamental problem

3. **Remove Tool Results on Truncation** - When AssistantToolCallMessage truncated, also remove its ToolResultMessages
   - Requires tracking relationships
   - More invasive change to truncation logic

## Impact

### Benefits
- Prevents `BadRequest` errors from Anthropic API
- Maintains conversation flow even with context limits
- Provides visibility into when/why filtering occurs
- No impact on normal operation (only affects edge case)

### Tradeoffs
- Tool results may be "lost" from conversation when their tool_use is truncated
- User won't see error, but also won't see tool result in some cases
- Logged to transparency service for debugging

## Testing

Manual testing with the error scenario showed:
- Orphaned tool_result messages are correctly filtered
- API no longer receives invalid requests
- Transparency log shows when filtering occurs
- Normal tool call flow unaffected

## Files Modified

- `TransparentAiAgentCore/Infrastructure/LLM/AnthropicProvider.cs`
  - Added two-pass validation in `ConvertToAnthropicMessages()`
  - Added orphaned tool result filtering
  - Added transparency logging for filtered results

## Recommendations

### Short Term
- Monitor transparency logs for frequency of orphaned tool results
- If frequent, consider implementing smarter truncation strategy

### Long Term
- Implement relationship-aware truncation in ConversationManager
- Consider tool_use/tool_result as atomic units that should truncate together
- Add integration tests for truncation edge cases
- Consider different truncation strategies per message type

## Related Code

- `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs:149-193` - Truncation logic
- `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs:143-211` - Tool call execution
- `TransparentAiAgentCore/Application/Pipeline/MessagePipeline.cs:22-27` - Message conversion
- `TransparentAiAgentCore/Domain/Models/ToolResultMessage.cs` - Tool result model
- `TransparentAiAgentCore/Domain/Models/AssistantToolCallMessage.cs` - Tool call model

## Commit

```
commit 23e20aa
Author: Claude
Date: 2025-11-10

Fix: Prevent orphaned tool_result messages causing Anthropic API errors
```
