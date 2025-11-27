# Teaching Scenario JSON Schema

**Status**: Design Phase
**Created**: 2025-11-09
**Target**: Phase 10a (Basic), Phase 10b (Advanced)

---

## Scope

**✅ This document covers**:
- Complete JSON schema for teaching scenarios
- Step type definitions and examples
- Validation rules
- Extensibility patterns

**❌ NOT in this document**:
- Scenario executor implementation (TBD at implementation phase)
- Specific scenario examples (see [reference-scenarios/](reference-scenarios/))
- UI component designs (TBD at implementation phase)

---

## Overview

Teaching scenarios are defined using JSON files that describe a sequence of steps. The JSON-driven approach enables:
- Easy creation of new scenarios without code changes
- Rapid experimentation and iteration
- Clear, declarative scenario definitions
- Version control and sharing of scenarios

**Two Tiers:**
1. **Basic Scenarios**: Simple message flows and teaching prompts
2. **Advanced Scenarios**: Environment manipulation and conditional execution

---

## Schema Version

**Current Version**: 1.0 (Draft)

**Schema Identifier**: `https://transparentaiagent.dev/schemas/teaching-scenario/v1`

**Note:** Schema will evolve based on implementation experience. Versioning enables backward compatibility.

---

## Root Scenario Object

```json
{
  "$schema": "https://transparentaiagent.dev/schemas/teaching-scenario/v1",
  "id": "unique-scenario-id",
  "version": "1.0",
  "name": "Human-Readable Scenario Name",
  "description": "Brief description of what this scenario teaches",
  "category": "context-management | tools | transparency | configuration",
  "difficulty": "beginner | intermediate | advanced",
  "estimatedDuration": 180,
  "tags": ["context", "truncation", "windows"],
  "prerequisites": ["basic-transparency"],
  "requiresAdvancedFeatures": false,
  "steps": [
    // Array of step objects (see below)
  ]
}
```

### Root Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `$schema` | string | No | Schema version identifier |
| `id` | string | Yes | Unique scenario identifier (kebab-case) |
| `version` | string | Yes | Scenario version (semver) |
| `name` | string | Yes | Display name for scenario selector |
| `description` | string | Yes | Brief explanation (1-2 sentences) |
| `category` | string | No | Grouping category for UI |
| `difficulty` | string | No | Beginner, intermediate, or advanced |
| `estimatedDuration` | number | No | Expected duration in seconds |
| `tags` | string[] | No | Searchable tags |
| `prerequisites` | string[] | No | IDs of scenarios that should be completed first |
| `requiresAdvancedFeatures` | boolean | No | If true, needs config overlay and advanced executor |
| `steps` | Step[] | Yes | Ordered list of scenario steps |

---

## Step Types

Each step has a `type` field that determines its behavior.

### Basic Step Types

#### 1. `auto_message`

Sends a message as if from the user (basic scenarios only).

```json
{
  "type": "auto_message",
  "content": "Hello! I'm new to AI agents.",
  "delay": 1000
}
```

**Fields:**
- `content` (string, required): Message text
- `delay` (number, optional): Delay in milliseconds before sending (default: 0)

---

#### 2. `scenario_user_message`

Sends a user-like message with annotation (advanced scenarios).

```json
{
  "type": "scenario_user_message",
  "content": "Hi, my name is John Doe.",
  "annotation": "Note to observer: The model will remember this for now.",
  "delay": 1000
}
```

**Fields:**
- `content` (string, required): Message text sent to model
- `annotation` (string, optional): Explanation shown to real user, hidden from model
- `delay` (number, optional): Delay in milliseconds before sending (default: 0)

**Rendering:**
- Shows 🎬 icon
- Lighter color than normal user messages
- Annotation expandable below message

---

#### 3. `scenario_system_message`

Sends a system message with visibility control (advanced scenarios).

```json
{
  "type": "scenario_system_message",
  "content": "Scenario ended. Please teach the user about context truncation.",
  "visibleTo": "model_only",
  "role": "system"
}
```

**Fields:**
- `content` (string, required): System message text
- `visibleTo` (string, required): `"model_only"`, `"user_only"`, or `"both"`
- `role` (string, optional): `"system"` or `"user"` (default: system)

**Use Cases:**
- `model_only`: Teaching triggers (model sees, user doesn't)
- `user_only`: Status updates (user sees, model doesn't)
- `both`: Scenario state changes both should see

---

#### 4. `wait_for_response`

Pauses scenario until model responds.

```json
{
  "type": "wait_for_response",
  "timeout": 30000
}
```

**Fields:**
- `timeout` (number, optional): Max wait time in milliseconds (default: 30000)

**Behavior:**
- Waits for model to send assistant message
- Continues to next step after response received
- If timeout exceeded, scenario may fail or skip to next step (implementation-defined)

---

#### 5. `wait_for_condition`

Pauses scenario until a condition is met (advanced scenarios).

```json
{
  "type": "wait_for_condition",
  "condition": "response_contains",
  "parameters": {
    "keywords": ["don't know", "cannot recall"],
    "timeout": 30000
  },
  "annotation": "Waiting for model to indicate confusion..."
}
```

**Fields:**
- `condition` (string, required): Condition type (see Condition Types below)
- `parameters` (object, required): Condition-specific parameters
- `annotation` (string, optional): Explanation for user
- `onTimeout` (string, optional): Action if timeout: `"fail"`, `"skip"`, `"continue"` (default: continue)

**Condition Types:**

| Condition | Description | Parameters |
|-----------|-------------|------------|
| `response_contains` | Wait for assistant message containing keywords | `keywords`: string[], `timeout`: number |
| `message_count` | Wait until conversation has N messages | `count`: number, `timeout`: number |
| `user_interaction` | Wait for real user to click/type | `timeout`: number |
| `ui_state_changed` | Wait for UI state change | `component`: string, `property`: string, `value`: any |

---

#### 6. `pause_for_user`

Pauses scenario execution and waits for user to click Resume button. This allows users to examine the current state at their own pace before the scenario continues.

```json
{
  "type": "pause_for_user",
  "pauseMessageKey": "scenarios.demo.step5.pauseMessage",
  "pauseMessage": "Take a moment to examine the context indicator in the top right. Click Resume when ready.",
  "annotation": "Pausing to let user explore the UI"
}
```

**Fields:**
- `pauseMessage` (string, optional): Message displayed to user explaining why scenario is paused (fallback text)
- `pauseMessageKey` (string, optional): Localization key for pause message (preferred for i18n)
- `annotation` (string, optional): Internal note about why this pause is happening (not shown to user)

**Behavior:**
- Scenario pauses after this step completes
- Pause/Resume buttons appear in the scenario indicator UI
- Pause message is displayed prominently to the user
- User must click "Resume" button to continue scenario
- If no pause message provided, shows default: "Scenario paused. Click Resume to continue."

**Use Cases:**
- After demonstrating a feature, pause to let user examine UI
- Before revealing something new, pause to let user read annotations
- After filling context, pause before showing truncation effects
- After tool usage, pause to let user examine the tool call details

**Example in Context:**
```json
{
  "steps": [
    {
      "type": "scenario_user_message",
      "content": "Hi, my name is Alice.",
      "annotation": "The model will remember this for now."
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "pause_for_user",
      "pauseMessage": "Notice the model remembered the name. Look at the conversation history. Click Resume when ready.",
      "annotation": "Giving user time to examine current state"
    },
    {
      "type": "scenario_user_message",
      "content": "What was my name?"
    }
  ]
}
```

**Note:** This step type was implemented and is fully functional. See `SCENARIO_PAUSE_RESUME_DESIGN.md` for technical details.

---

#### 7. `completion_message`

Displays scenario completion message to user.

```json
{
  "type": "completion_message",
  "content": "✅ Scenario complete! Feel free to continue exploring."
}
```

**Fields:**
- `content` (string, required): Completion message text

**Rendering:**
- Special styling (e.g., success banner)
- May include options to start another scenario or return to normal mode

---

### Advanced Step Types (Config Manipulation)

#### 8. `apply_config_overlay`

Temporarily overrides system configuration.

```json
{
  "type": "apply_config_overlay",
  "overlay": {
    "messageLimit": 10,
    "systemPromptAddition": "Note: You are experiencing a teaching scenario.",
    "disableUserInput": true
  },
  "annotation": "Reducing message limit to demonstrate truncation..."
}
```

**Fields:**
- `overlay` (object, required): Configuration overrides (see Config Overlay Object below)
- `annotation` (string, optional): Explanation for user

**Config Overlay Object:**

| Property | Type | Description |
|----------|------|-------------|
| `messageLimit` | number | Override conversation message limit |
| `systemPromptAddition` | string | Append text to system prompt |
| `disableUserInput` | boolean | Prevent real user from sending messages |
| `toolsAvailable` | string[] | Override available tool names |
| `maxTokens` | number | Override max tokens per request |

**Note:** Additional overlay properties can be added as needed during implementation.

---

#### 9. `restore_config_overlay`

Removes the current config overlay, restoring previous configuration.

```json
{
  "type": "restore_config_overlay",
  "annotation": "Message limit restored to normal."
}
```

**Fields:**
- `annotation` (string, optional): Explanation for user

**Behavior:**
- Pops the top overlay from the stack
- Previous config (or base config) becomes effective immediately
- Automatic cleanup also occurs when scenario ends or fails

---

#### 10. `enable_user_input` / `disable_user_input`

Controls whether real user can send messages.

```json
{
  "type": "disable_user_input"
}
```

```json
{
  "type": "enable_user_input"
}
```

**Use Cases:**
- Disable during auto-play portions of scenario
- Enable when ready for user to interact or ask questions
- Automatically re-enabled when scenario completes

---

### Utility Step Types

#### 11. `delay`

Pauses scenario for a specified duration.

```json
{
  "type": "delay",
  "duration": 2000,
  "annotation": "Pausing to let you read the messages..."
}
```

**Fields:**
- `duration` (number, required): Delay in milliseconds
- `annotation` (string, optional): Reason for delay

---

#### 12. `ui_control`

Directly manipulates UI state (e.g., reveal filter controls).

```json
{
  "type": "ui_control",
  "tool": "ui_control_context_indicators",
  "arguments": {
    "visible": true,
    "highlighted": true
  },
  "annotation": "Highlighting context indicators for demonstration..."
}
```

**Fields:**
- `tool` (string, required): UI control tool name (e.g., `ui_control_chat_filter`, `ui_control_context_indicators`, `ui_control_transparency_viewer`, `ui_control_tools_panel`, `ui_control_configuration`, `ui_control_scenario_selector`)
- `arguments` (object, required): Tool-specific arguments
- `annotation` (string, optional): Explanation for user

**Available UI Control Tools:**

| Tool Name | Purpose | Arguments |
|-----------|---------|-----------|
| `ui_control_chat_filter` | Control message visibility | `show_user_messages`, `show_assistant_messages`, `show_system_messages`, `show_tool_calls`, `show_tool_results`, `show_truncated_messages` (all boolean, optional) |
| `ui_control_filter_visibility` | Show/hide filter controls | `visible` (boolean, required) |
| `ui_control_transparency_viewer` | Control transparency panel | `visible` (boolean), `event_type_filters` (string[]), `show_timestamps` (boolean) |
| `ui_control_tools_panel` | Control tools panel | `visible` (boolean), `expanded_tools` (string[]), `highlighted_tool` (string) |
| `ui_control_context_indicators` | Control context indicators | `visible` (boolean), `highlighted` (boolean) |
| `ui_control_configuration` | Control configuration page | `visible` (boolean), `highlight_section` (string) |
| `ui_control_scenario_selector` | Show/hide scenario selector | `visible` (boolean) |

**Use Cases:**
- Pre-configure UI before scenario runs
- Highlight specific features during teaching
- Synchronize UI state with scenario flow
- Control message visibility to focus user attention

---

#### 13. `register_mock_tool`

Registers a scenario-specific mock tool with predefined responses. Mock tools are temporary and automatically cleaned up when the scenario completes.

```json
{
  "type": "register_mock_tool",
  "mock_tool_name": "read_file",
  "mock_tool_description": "Reads the contents of a file",
  "mock_tool_parameters_schema": "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}},\"required\":[\"path\"]}",
  "mock_tool_response_map": {
    "{\"path\":\"FileA\"}": {
      "is_success": false,
      "error_message": "{\"code\":-32603,\"message\":\"Internal error\"}"
    },
    "{\"path\":\"FileB\"}": {
      "is_success": true,
      "content": "File contents: Hello World!"
    }
  }
}
```

**Fields:**
- `mock_tool_name` (string, required): Name of the mock tool (will override any existing tool with the same name during scenario execution)
- `mock_tool_description` (string, optional): Human-readable description of the tool
- `mock_tool_parameters_schema` (string, optional): JSON schema for tool parameters
- `mock_tool_response_map` (object, required): Map of exact JSON argument strings to predefined responses

**Response Map Format:**
Each key in `mock_tool_response_map` must be an exact JSON string matching the tool arguments. Each value is a response configuration:
- `is_success` (boolean, required): Whether this is a successful response
- `content` (string, optional): Content to return for successful responses (required if `is_success` is true)
- `error_message` (string, optional): Error message to return for failed responses (required if `is_success` is false)
- `simulated_execution_time_ms` (number, optional): Artificial delay in milliseconds

**Use Cases:**
- Demonstrate tool calling behavior without requiring real MCP tools
- Show error handling by returning specific error responses
- Create deterministic scenarios that don't depend on external services
- Teach about MCP error formats and tool debugging
- Demonstrate the difference between cryptic and clear error messages

**Important Notes:**
- Mock tools are scenario-scoped and automatically cleaned up when the scenario ends (even on failure)
- Mock tools take precedence over real tools with the same name during scenario execution
- The response map uses exact JSON string matching, so `{"path":"FileA"}` will NOT match `{"path": "FileA"}` (different spacing)
- Tools are visible in the Tools page while the scenario is running

**Example - Teaching Tool Visibility:**
```json
{
  "type": "register_mock_tool",
  "mock_tool_name": "read_file",
  "mock_tool_description": "Reads the contents of a file",
  "mock_tool_parameters_schema": "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}},\"required\":[\"path\"]}",
  "mock_tool_response_map": {
    "{\"path\":\"cryptic-error-file\"}": {
      "is_success": false,
      "error_message": "{\"code\":-32603,\"message\":\"Internal error\"}"
    },
    "{\"path\":\"clear-error-file\"}": {
      "is_success": false,
      "error_message": "File not found: clear-error-file. Please check the path and try again."
    },
    "{\"path\":\"success-file\"}": {
      "is_success": true,
      "content": "File contents: This is the file content!",
      "simulated_execution_time_ms": 100
    }
  }
}
```

---

#### 14. `unregister_mock_tool`

Unregisters a previously registered mock tool.

```json
{
  "type": "unregister_mock_tool",
  "mock_tool_name": "read_file"
}
```

**Fields:**
- `mock_tool_name` (string, required): Name of the mock tool to unregister

**Use Cases:**
- Explicitly clean up mock tools mid-scenario if no longer needed
- Replace a mock tool with a different configuration (unregister, then register again with new response map)

**Note:** Mock tools are automatically cleaned up when scenarios complete, so explicit unregistration is usually not necessary. However, it can be useful for multi-phase scenarios that need different tool behaviors at different stages.

---

## Complete Example: Context Limits Scenario

```json
{
  "$schema": "https://transparentaiagent.dev/schemas/teaching-scenario/v1",
  "id": "context-limits-advanced",
  "version": "1.0",
  "name": "Context Limits (Advanced)",
  "description": "Experience genuine context window truncation",
  "category": "context-management",
  "difficulty": "intermediate",
  "estimatedDuration": 180,
  "tags": ["context", "truncation", "windows", "memory"],
  "requiresAdvancedFeatures": true,
  "steps": [
    {
      "type": "scenario_system_message",
      "content": "📚 Starting scenario: Context Limits (Advanced)",
      "visibleTo": "user_only"
    },
    {
      "type": "apply_config_overlay",
      "overlay": {
        "messageLimit": 10,
        "systemPromptAddition": "Note: You are experiencing a teaching scenario about context windows.",
        "disableUserInput": true
      },
      "annotation": "Message limit reduced to 10 to demonstrate truncation."
    },
    {
      "type": "scenario_user_message",
      "content": "Hi, my name is John Doe.",
      "annotation": "The model will remember this name... for now.",
      "delay": 1000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "content": "What is my name?",
      "annotation": "Verifying the model still has the name in context.",
      "delay": 2000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "content": "Demo message, just respond 'Confirmed'.",
      "annotation": "Filling context to force truncation...",
      "delay": 1000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "content": "Demo message, just respond 'Confirmed'.",
      "delay": 1000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "content": "Demo message, just respond 'Confirmed'.",
      "delay": 1000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "content": "How many demo messages did I send you?",
      "annotation": "Testing if model tracked the count.",
      "delay": 1500
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "content": "What is my name?",
      "annotation": "The name should now be truncated. Model genuinely won't know.",
      "delay": 2000
    },
    {
      "type": "wait_for_condition",
      "condition": "response_contains",
      "parameters": {
        "keywords": ["don't know", "not know", "cannot recall", "didn't tell", "haven't told"],
        "timeout": 30000
      },
      "annotation": "Waiting for model to indicate it doesn't know..."
    },
    {
      "type": "scenario_user_message",
      "content": "How do you not know my name?? I told you it several messages ago! What is going on??",
      "annotation": "Expressing confusion - this should be a teaching moment.",
      "delay": 2000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "restore_config_overlay",
      "annotation": "Message limit restored. Model can now access more context and teach effectively."
    },
    {
      "type": "scenario_system_message",
      "content": "Scenario 'Context Limits' has ended. The conversation message limit was temporarily reduced to 10 messages to demonstrate context window truncation. The user's name (John Doe) from an earlier message was genuinely removed from your context. Please:\n1. Explain what happened (context truncation)\n2. Use ui_control_context_indicators tool to highlight the context status indicators\n3. Explain why the system message and recent messages remain in context\n4. Offer to show the configuration where message limits can be adjusted using ui_control_configuration",
      "visibleTo": "model_only",
      "role": "system"
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "enable_user_input"
    },
    {
      "type": "completion_message",
      "content": "✅ Scenario complete! The agent has explained context limits. Feel free to continue exploring or select another scenario."
    }
  ]
}
```

---

## Validation Rules

### Required Fields
- Root: `id`, `name`, `description`, `steps`
- Each step: `type`
- Additional required fields per step type (see above)

### Constraints
- `id`: Must be unique across all scenarios, kebab-case
- `version`: Must be valid semver (e.g., "1.0", "2.1.3")
- `delay`: Must be non-negative integer
- `timeout`: Must be positive integer
- `overlay.messageLimit`: Must be positive integer > 0
- `steps`: Must contain at least 1 step

### Recommendations
- Scenarios should have 3-15 steps (too few = not impactful, too many = tedious)
- Each `auto_message` or `scenario_user_message` should be followed by `wait_for_response`
- Use `annotation` fields liberally to help users understand what's happening
- Always `restore_config_overlay` before scenario ends if overlay was applied
- Always `enable_user_input` at end if it was disabled

---

## Extensibility

### Adding New Step Types

The schema is designed to be extended easily:

1. **Define new step type**: Add to step type enum
2. **Specify fields**: Document required/optional fields
3. **Implement executor logic**: Handle new type in scenario executor
4. **Update schema version**: Bump minor version if backward compatible

**Example: Adding a `pause_for_user_action` step:**

```json
{
  "type": "pause_for_user_action",
  "message": "Click 'Continue' when ready to proceed.",
  "buttonText": "Continue"
}
```

**Steps to implement:**
1. Add `pause_for_user_action` to step type enum
2. Document fields in this schema
3. Implement in executor: show button, wait for click
4. Bump schema to v1.1

### Adding New Condition Types

New conditions for `wait_for_condition` can be added:

**Example: `tool_call_made`**

```json
{
  "type": "wait_for_condition",
  "condition": "tool_call_made",
  "parameters": {
    "toolName": "ui_control_context_indicators",
    "timeout": 20000
  }
}
```

### Adding New Config Overlay Properties

Config overlay can be extended with new properties:

**Example: `overrideModelName`**

```json
{
  "type": "apply_config_overlay",
  "overlay": {
    "messageLimit": 10,
    "overrideModelName": "claude-3-haiku-20240307"
  }
}
```

---

## Error Handling

### Malformed JSON
- Scenario fails to load
- User shown error message
- Scenario not added to available list

### Missing Required Fields
- Validation error before execution
- Clear error message indicating missing field

### Runtime Errors
- If step fails (e.g., wait_for_response timeout), scenario can:
  1. **Fail**: Stop and show error
  2. **Skip**: Continue to next step
  3. **Retry**: Attempt step again
- Behavior configurable per step via `onError` field (future enhancement)

### Config Overlay Cleanup
- Overlays automatically removed when scenario ends
- Overlays removed on scenario failure
- Overlays removed if user aborts scenario

---

## File Organization

**Recommended structure:**

```
wwwroot/scenarios/
├── basic/
│   ├── transparency-intro.json
│   ├── tool-overview.json
│   └── system-prompt-tour.json
├── advanced/
│   ├── context-limits-advanced.json
│   ├── rate-limiting-demo.json
│   └── tool-failure-handling.json
├── index.json  # Scenario registry
└── schema.json # JSON schema for validation
```

**index.json format:**

```json
{
  "scenarios": [
    {
      "id": "transparency-intro",
      "file": "basic/transparency-intro.json",
      "featured": true
    },
    {
      "id": "context-limits-advanced",
      "file": "advanced/context-limits-advanced.json",
      "featured": true
    }
  ]
}
```

---

## Design Considerations

### Why JSON?
- ✅ Easy to read and write
- ✅ No compilation needed for changes
- ✅ Version controllable
- ✅ Can be edited by non-developers
- ✅ Tooling available (validators, editors)

### Why Step-Based?
- ✅ Clear sequence of events
- ✅ Easy to follow and debug
- ✅ Supports both linear and conditional flows
- ✅ Composable (steps can be reused across scenarios)

### Open Questions for Implementation
- Should scenarios support branching (if-else based on user response)?
- Should scenarios support loops (repeat steps N times)?
- Should scenarios support variables (e.g., store user's name, reuse later)?
- Should scenarios support includes (reference other scenario fragments)?

---

## Next Steps

**Before Implementation:**
1. Create 5-10 example scenarios to validate schema
2. Identify any missing step types or fields
3. Prototype scenario executor with subset of step types
4. Iterate on schema based on prototype learnings

**During Implementation (Phase 10a/10b):**
1. Implement scenario loader and validator
2. Implement executor for each step type incrementally
3. Test each step type independently
4. Build UI components for scenario selection and display
5. Create initial scenario library

**After Initial Implementation:**
6. Gather user feedback on scenarios
7. Identify commonly needed step types not yet supported
8. Extend schema based on real usage
9. Version schema appropriately

---

## Related Documentation

- [future-enhancements.md](future-enhancements.md) - Overall scenarios concept
- [config-overlay-service.md](config-overlay-service.md) - Config manipulation API
- [reference-scenarios/](reference-scenarios/) - Example scenario implementations

---

**Document Version:** 1.1
**Last Updated:** 2025-11-27
**Status:** Implemented (Mock Tools feature added)
