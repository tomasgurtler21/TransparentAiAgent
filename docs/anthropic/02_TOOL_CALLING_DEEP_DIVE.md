# Tool Calling - Deep Dive

**Last Updated:** January 2025
**Models:** All Claude 4.x models support tool calling

---

## Table of Contents

1. [Overview](#overview)
2. [Tool Definition Format](#tool-definition-format)
3. [Tool Use Response](#tool-use-response)
4. [Tool Result Format](#tool-result-format)
5. [Multi-Turn Tool Calling Flow](#multi-turn-tool-calling-flow)
6. [Tool Choice Control](#tool-choice-control)
7. [Parallel Tool Use](#parallel-tool-use)
8. [Error Handling](#error-handling)
9. [Best Practices](#best-practices)
10. [Edge Cases & Gotchas](#edge-cases--gotchas)

---

## Overview

Claude can use tools (also known as function calling) to interact with external systems, APIs, and data sources. The tool calling flow follows this pattern:

1. **Define tools** in your request
2. **Claude requests tool use** when needed (stop_reason: "tool_use")
3. **Execute tools** on your end
4. **Return results** to Claude
5. **Claude provides final answer** using tool results

**Critical Difference from Other LLMs:**
- Tool calls are **embedded in content blocks**, not separate fields
- Tool results go in **user messages**, not separate tool messages
- System uses `stop_reason == "tool_use"` to signal tool calls needed

---

## Tool Definition Format

### Complete Tool Structure

```json
{
  "name": "get_weather",
  "description": "Get the current weather in a given location. Use this tool when the user asks about current weather conditions. Returns temperature in specified unit and general conditions (sunny, cloudy, rainy, etc.).",
  "input_schema": {
    "type": "object",
    "properties": {
      "location": {
        "type": "string",
        "description": "The city and state, e.g. San Francisco, CA"
      },
      "unit": {
        "type": "string",
        "enum": ["celsius", "fahrenheit"],
        "description": "Temperature unit preference"
      }
    },
    "required": ["location"]
  }
}
```

### Field Requirements

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `name` | string | **Yes** | Regex: `^[a-zA-Z0-9_-]{1,64}$` | Tool identifier (alphanumeric, underscore, hyphen only, max 64 chars) |
| `description` | string | **Yes** | 3-4 sentences recommended | Detailed explanation of what tool does, when to use it, and what it returns |
| `input_schema` | object | **Yes** | JSON Schema | Parameter definitions |

### Input Schema Details

#### Supported JSON Schema Features

```json
{
  "type": "object",
  "properties": {
    "parameter_name": {
      "type": "string",        // or "number", "integer", "boolean", "array", "object"
      "description": "What this parameter means",
      "enum": ["option1", "option2"],  // Optional: restrict to specific values
      "default": "value"      // Optional: default value
    },
    "nested_object": {
      "type": "object",
      "properties": {
        "nested_param": {"type": "string"}
      }
    },
    "array_param": {
      "type": "array",
      "items": {"type": "string"}
    }
  },
  "required": ["parameter_name"]  // Array of required parameter names
}
```

**Supported Types:**
- `string`, `number`, `integer`, `boolean`
- `array` (with items schema)
- `object` (with properties)
- `enum` for value restrictions
- `description` for parameter documentation
- `required` array for mandatory parameters

#### UNSUPPORTED Features (Will Cause Errors)

- `oneOf` at top level
- `allOf` at top level
- `anyOf` at top level

**Error Message:** "Invalid tool input schema"

**Workaround:** Use simpler schema structures or handle variations in tool description.

**Example - Wrong:**
```json
{
  "input_schema": {
    "oneOf": [
      {"type": "object", "properties": {"option1": {...}}},
      {"type": "object", "properties": {"option2": {...}}}
    ]
  }
}
```

**Example - Right:**
```json
{
  "input_schema": {
    "type": "object",
    "properties": {
      "mode": {
        "type": "string",
        "enum": ["option1", "option2"],
        "description": "Which mode to use"
      },
      "option1_params": {
        "type": "object",
        "description": "Parameters for option1 (used when mode=option1)"
      },
      "option2_params": {
        "type": "object",
        "description": "Parameters for option2 (used when mode=option2)"
      }
    },
    "required": ["mode"]
  }
}
```

---

## Tool Use Response

### When Claude Wants to Use a Tool

When Claude decides to call a tool, the response will contain:
- `stop_reason: "tool_use"`
- Content blocks including `tool_use` type

### Response Structure

```json
{
  "id": "msg_01234",
  "type": "message",
  "role": "assistant",
  "content": [
    {
      "type": "text",
      "text": "I'll check the weather for you."
    },
    {
      "type": "tool_use",
      "id": "toolu_01A09q90qw90lq917835lq9",
      "name": "get_weather",
      "input": {
        "location": "San Francisco, CA",
        "unit": "fahrenheit"
      }
    }
  ],
  "stop_reason": "tool_use",
  "model": "claude-sonnet-4-5-20250929",
  "usage": {...}
}
```

### Tool Use Block Fields

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | Always `"tool_use"` |
| `id` | string | Unique identifier (format: `toolu_*`) - use this to match results |
| `name` | string | Tool name from your definition |
| `input` | object | **Always a parsed JSON object**, never a string |

**CRITICAL:**
- `stop_reason == "tool_use"` means conversation is NOT complete
- You MUST execute the tool(s) and continue conversation
- Each `tool_use` block needs a corresponding `tool_result` response

---

## Tool Result Format

### Success Result

After executing a tool, send the result back in a **user message**:

```json
{
  "role": "user",
  "content": [
    {
      "type": "tool_result",
      "tool_use_id": "toolu_01A09q90qw90lq917835lq9",
      "content": "72 degrees Fahrenheit and sunny"
    }
  ]
}
```

### Error Result

If tool execution fails:

```json
{
  "role": "user",
  "content": [
    {
      "type": "tool_result",
      "tool_use_id": "toolu_01A09q90qw90lq917835lq9",
      "content": "Error: Weather API returned 503 Service Unavailable. Please try again in a moment.",
      "is_error": true
    }
  ]
}
```

### Tool Result Block Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | Yes | Always `"tool_result"` |
| `tool_use_id` | string | Yes | Must match `id` from `tool_use` block |
| `content` | string | Yes | Result data (can be JSON string) |
| `is_error` | boolean | No | True if execution failed (default: false) |

### Multiple Tool Results

When Claude calls multiple tools, return ALL results in a single user message:

```json
{
  "role": "user",
  "content": [
    {
      "type": "tool_result",
      "tool_use_id": "toolu_01",
      "content": "Weather: 72F, sunny"
    },
    {
      "type": "tool_result",
      "tool_use_id": "toolu_02",
      "content": "Stock price: $150.25"
    }
  ]
}
```

### CRITICAL Requirements

1. **All results in ONE message:** Cannot split tool results across multiple messages
2. **Tool results FIRST:** Any additional text content must come AFTER tool_result blocks
3. **Exact ID match:** `tool_use_id` must correspond to `id` from tool_use block
4. **One result per call:** Each tool_use requires exactly one tool_result

**Correct Order:**
```json
{
  "role": "user",
  "content": [
    {"type": "tool_result", "tool_use_id": "toolu_01", "content": "..."},
    {"type": "tool_result", "tool_use_id": "toolu_02", "content": "..."},
    {"type": "text", "text": "Additional context"}  // Text AFTER tool results
  ]
}
```

**WRONG - Will fail:**
```json
{
  "role": "user",
  "content": [
    {"type": "text", "text": "Here are the results"},  // Text BEFORE results - ERROR!
    {"type": "tool_result", "tool_use_id": "toolu_01", "content": "..."}
  ]
}
```

---

## Multi-Turn Tool Calling Flow

### Complete Example

**Turn 1 - User Request:**
```json
{
  "role": "user",
  "content": "What's the weather in San Francisco and the stock price of AAPL?"
}
```

**Turn 2 - Claude Requests Tools:**
```json
{
  "role": "assistant",
  "content": [
    {
      "type": "text",
      "text": "I'll check both the weather and the stock price for you."
    },
    {
      "type": "tool_use",
      "id": "toolu_01weather",
      "name": "get_weather",
      "input": {"location": "San Francisco, CA"}
    },
    {
      "type": "tool_use",
      "id": "toolu_02stock",
      "name": "get_stock_price",
      "input": {"symbol": "AAPL"}
    }
  ],
  "stop_reason": "tool_use"
}
```

**Turn 3 - You Return Tool Results:**
```json
{
  "role": "user",
  "content": [
    {
      "type": "tool_result",
      "tool_use_id": "toolu_01weather",
      "content": "72 degrees Fahrenheit, sunny"
    },
    {
      "type": "tool_result",
      "tool_use_id": "toolu_02stock",
      "content": "$150.25"
    }
  ]
}
```

**Turn 4 - Claude Final Response:**
```json
{
  "role": "assistant",
  "content": [
    {
      "type": "text",
      "text": "The weather in San Francisco is 72°F and sunny. Apple stock (AAPL) is currently trading at $150.25."
    }
  ],
  "stop_reason": "end_turn"
}
```

### Handling in Your Code

```csharp
public async Task<string> HandleConversationWithTools(string userMessage)
{
    var messages = new List<MessageParam>
    {
        new() { Role = Role.User, Content = new Content(userMessage) }
    };

    while (true)
    {
        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-sonnet-4-5-20250929",
            Messages = messages,
            MaxTokens = 4096,
            Tools = tools
        });

        // Add assistant response to history
        messages.Add(new MessageParam
        {
            Role = Role.Assistant,
            Content = SerializeContent(response.Content) // Keep exact content
        });

        // Check stop reason
        if (response.StopReason == "tool_use")
        {
            // Extract tool calls
            var toolCalls = response.Content
                .Where(c => c.TryPickToolUse(out _))
                .Select(c => { c.TryPickToolUse(out var tool); return tool; })
                .ToList();

            // Execute tools
            var results = new List<ContentBlock>();
            foreach (var toolCall in toolCalls)
            {
                var result = await ExecuteTool(toolCall.Name, toolCall.Input);
                results.Add(new ToolResultBlock
                {
                    ToolUseId = toolCall.ID,
                    Content = result
                });
            }

            // Add tool results to conversation
            messages.Add(new MessageParam
            {
                Role = Role.User,
                Content = new Content(results)
            });

            // Continue loop - send back to Claude
            continue;
        }
        else
        {
            // Normal completion - extract final text
            var finalText = string.Empty;
            foreach (var block in response.Content)
            {
                if (block.TryPickText(out var text))
                    finalText += text.Text;
            }
            return finalText;
        }
    }
}
```

---

## Tool Choice Control

### auto (Default)

Claude decides whether to use tools:

```json
{
  "tool_choice": {"type": "auto"}
}
```

**Behavior:** Claude will call tools when appropriate, or respond normally if tools aren't needed.

### any (Force Tool Use)

Claude MUST use at least one tool:

```json
{
  "tool_choice": {"type": "any"}
}
```

**Use Cases:**
- Forcing structured output via a tool
- Ensuring tool validation logic runs
- Testing tool definitions

**Warning:** Will error if no tools are provided.

### tool (Force Specific Tool)

Force usage of a specific tool:

```json
{
  "tool_choice": {
    "type": "tool",
    "name": "get_weather"
  }
}
```

**Use Cases:**
- You know exactly which tool should be called
- Implementing a forced tool pipeline
- Testing specific tool behavior

### none (Disable Tools)

Prevent tool use for this turn:

```json
{
  "tool_choice": {"type": "none"}
}
```

**Use Cases:**
- Getting text-only response even with tools defined
- Final response after tool execution

---

## Parallel Tool Use

By default, Claude can invoke **multiple tools simultaneously** in one response:

```json
{
  "content": [
    {"type": "tool_use", "id": "toolu_01", "name": "get_weather", "input": {...}},
    {"type": "tool_use", "id": "toolu_02", "name": "get_stock_price", "input": {...}},
    {"type": "tool_use", "id": "toolu_03", "name": "get_news", "input": {...}}
  ],
  "stop_reason": "tool_use"
}
```

**Benefits:**
- Faster execution (parallel tool calls)
- More efficient conversations
- Reduced total turns

### Disabling Parallel Tool Use

To force sequential tool calls:

```json
{
  "disable_parallel_tool_use": true
}
```

**Use Cases:**
- Tools have dependencies on each other
- Resource constraints
- Debugging tool behavior

---

## Error Handling

### Invalid Parameters from Claude

Claude occasionally provides invalid parameters despite the schema. When this happens:

**Claude's Behavior:**
- Automatically retries 2-3 times
- Eventually apologizes if unable to succeed

**Your Response:**
```json
{
  "type": "tool_result",
  "tool_use_id": "toolu_01",
  "content": "Error: 'location' parameter must be in format 'City, State'. Received: 'SF'. Please use full city and state names.",
  "is_error": true
}
```

**Best Practices:**
- Provide clear, specific error messages
- Explain what was wrong and what format is expected
- Claude will use your error message to correct its approach

### Tool Execution Failures

Always return detailed error information:

```json
{
  "type": "tool_result",
  "tool_use_id": "toolu_01",
  "content": "HTTP 503: Weather service temporarily unavailable. The service is experiencing high load. Please try again in 30-60 seconds.",
  "is_error": true
}
```

**What to Include:**
- Error type/code
- Root cause
- Suggested remediation
- Whether user should retry

### Network Timeouts

```json
{
  "type": "tool_result",
  "tool_use_id": "toolu_01",
  "content": "Timeout: Weather API did not respond within 10 seconds. This may indicate network issues or API downtime.",
  "is_error": true
}
```

---

## Best Practices

### 1. Tool Description Quality

**Good Description (Recommended):**
```
Get the current weather conditions for a given location. Use this tool when
the user asks about weather, temperature, or atmospheric conditions. This tool
returns the current temperature, weather conditions (sunny, cloudy, rainy),
humidity percentage, and wind speed. The tool requires a location in "City, State"
format for US locations or "City, Country" for international locations.
```

**Why it's good:**
- Explains WHAT (get weather conditions)
- Explains WHEN (user asks about weather)
- Explains HOW (returns temperature, conditions, etc.)
- Specifies format requirements

**Poor Description (Avoid):**
```
Gets weather
```

**Why it's poor:**
- Too brief
- No context for when to use
- No details on behavior

**Minimum Recommendation:**
- 3-4 sentences
- Cover: purpose, usage triggers, return format
- Be specific about requirements

### 2. Parameter Descriptions

**Good:**
```json
{
  "location": {
    "type": "string",
    "description": "The city and state in 'City, State' format for US locations (e.g., 'San Francisco, CA') or 'City, Country' format for international locations (e.g., 'London, UK')"
  }
}
```

**Poor:**
```json
{
  "location": {
    "type": "string",
    "description": "Location"
  }
}
```

### 3. Required vs Optional Parameters

Be explicit about what's required:

```json
{
  "properties": {
    "location": {
      "type": "string",
      "description": "Required: City and state"
    },
    "unit": {
      "type": "string",
      "enum": ["celsius", "fahrenheit"],
      "description": "Optional: Temperature unit (defaults to fahrenheit)"
    }
  },
  "required": ["location"]
}
```

### 4. Handling Ambiguity

If your tool can't handle ambiguous input, say so:

```json
{
  "name": "get_weather",
  "description": "Get weather for a location. IMPORTANT: This tool cannot disambiguate between multiple cities with the same name. If the user's query is ambiguous (e.g., 'Portland' could be Oregon or Maine), ask for clarification before calling this tool."
}
```

### 5. Tool Naming

**Good Names:**
- `get_weather`
- `search_documents`
- `calculate_mortgage`
- `send_email`

**Poor Names:**
- `tool1`
- `helper`
- `process`

**Rules:**
- Use snake_case
- Start with verb
- Be specific and descriptive

---

## Edge Cases & Gotchas

### Case 1: No Suitable Tool

If user requests something but no tool matches:
- Claude responds normally without tools
- No error occurs

**Example:**
- User: "What's the weather?"
- Tools: `[get_stock_price, send_email]`
- Result: Claude responds "I don't have access to weather information"

### Case 2: Tool Required But None Suitable

Force tool use with `tool_choice: {"type": "any"}`:
- Claude will select the best match
- May result in unexpected tool calls

**Use with caution.**

### Case 3: Extended Thinking with Tools

When extended thinking is enabled:
- `tool_use` blocks must be preceded by `thinking` blocks
- Must include complete unmodified thinking in conversation history
- See extended thinking documentation

### Case 4: Streaming with Tools

Tool calls appear in streaming mode as:
- `content_block_start` with type `tool_use`
- `content_block_delta` with `input_json_delta` type
- JSON is streamed as partial strings

**See:** [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md)

### Case 5: Missing Tool Results

If you don't provide tool results after `stop_reason: "tool_use"`:
- Conversation cannot continue
- Model expects tool results
- Will cause confusion or errors

**Always check stop_reason and handle tool_use appropriately.**

### Case 6: Tool Result Order Matters

**CRITICAL:** Tool results must come BEFORE any text content in the same message.

**This works:**
```json
{
  "content": [
    {"type": "tool_result", ...},
    {"type": "text", "text": "Additional info"}
  ]
}
```

**This FAILS:**
```json
{
  "content": [
    {"type": "text", "text": "Here are results"},
    {"type": "tool_result", ...}  // ERROR: tool_result after text
  ]
}
```

---

## C# Code Examples

### Defining Tools

```csharp
var weatherTool = new Tool
{
    Name = "get_weather",
    Description = "Get the current weather in a given location. Use when user asks about weather.",
    InputSchema = new InputSchema
    {
        Type = JsonSerializer.Deserialize<JsonElement>("\"object\""),
        Properties1 = JsonSerializer.Deserialize<JsonElement>(@"
        {
            ""location"": {
                ""type"": ""string"",
                ""description"": ""City and state, e.g. San Francisco, CA""
            },
            ""unit"": {
                ""type"": ""string"",
                ""enum"": [""celsius"", ""fahrenheit""]
            }
        }"),
        Required = new List<string> { "location" }
    }
};

var tools = new List<ToolUnion> { new ToolUnion(weatherTool) };
```

### Detecting Tool Calls

```csharp
var response = await client.Messages.Create(parameters);

if (response.StopReason == "tool_use")
{
    // Extract tool calls
    foreach (var contentBlock in response.Content)
    {
        if (contentBlock.TryPickToolUse(out var toolUse))
        {
            Console.WriteLine($"Tool: {toolUse.Name}");
            Console.WriteLine($"ID: {toolUse.ID}");
            Console.WriteLine($"Input: {JsonSerializer.Serialize(toolUse.Input)}");
        }
    }
}
```

### Returning Tool Results

```csharp
var toolResults = new List<ContentBlock>();

foreach (var toolCall in extractedToolCalls)
{
    var result = await ExecuteTool(toolCall.Name, toolCall.Input);

    toolResults.Add(new ContentBlock
    {
        Type = "tool_result",
        ToolUseId = toolCall.ID,
        Content = result,
        IsError = false
    });
}

// Send results back
var resultMessage = new MessageParam
{
    Role = Role.User,
    Content = new Content(toolResults)
};
```

---

## Testing Tools

### Test Checklist

- [ ] Tool called when expected
- [ ] Tool NOT called when inappropriate
- [ ] Parameters match schema
- [ ] Required parameters always present
- [ ] Tool results properly formatted
- [ ] Error handling works
- [ ] Parallel tool calls work (if enabled)
- [ ] Multi-turn conversations flow correctly
- [ ] stop_reason properly detected
- [ ] Tool result order correct

### Common Test Scenarios

1. **Happy Path:** User query → Tool call → Result → Final answer
2. **Invalid Parameters:** Claude provides wrong format → Error → Retry
3. **Tool Failure:** Execution error → Error result → Claude handles gracefully
4. **Multiple Tools:** Parallel execution → All results → Synthesized answer
5. **No Tool Needed:** User query that doesn't need tools → Direct answer

---

## Additional Resources

- [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md) - API parameters
- [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md) - Streaming with tools
- [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md) - Troubleshooting tools
- [07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md) - Your specific implementation

---

**Next:** [Streaming Implementation Guide →](03_STREAMING_IMPLEMENTATION_GUIDE.md)
