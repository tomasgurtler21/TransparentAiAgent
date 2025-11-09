# Tool Execution Safety & Validation Plan

**Status**: Planning
**Priority**: CRITICAL
**Created**: 2025-11-09
**Issue**: Tools executing with missing/invalid arguments is a serious safety concern

---

## Problem Statement

### Critical Safety Issue Discovered

During debugging of UI control tools, we discovered that **tools were executing with empty arguments (`{}`) when argument parsing failed**, instead of failing with an error. This is **unacceptable** because:

1. **Silent failures** - JSON accumulation failed during streaming, but no error was raised
2. **Dangerous defaults** - System defaulted to `{}` instead of failing the tool call
3. **No validation** - Tools executed without checking required parameters against schema
4. **No feedback to LLM** - LLM never learned the tool call was malformed

### Real-World Impact

**What could go wrong**:
- A tool that requires specific parameters to avoid destructive actions executes with defaults
- File deletion tools execute without path parameters
- Configuration changes apply with partial/missing parameters
- UI state corruption from malformed control commands

### Root Cause

The immediate trigger was using wrong Anthropic SDK method name (`TryPickInputJsonDelta` instead of `TryPickInputJSON`), causing JSON accumulation to fail silently. However, the **real problem** is architectural:

1. **Streaming provider** (AnthropicProvider.cs, lines 286-293) defaults to `"{}"` when accumulation fails
2. **Tool executors** don't validate arguments against schema before execution
3. **No schema validation layer** exists in the tool execution pipeline
4. **Transparency gaps** - Raw LLM requests/responses not visible in Transparency Viewer for debugging

---

## Fix Plan Architecture

### Layer 1: Streaming Provider Safety (CRITICAL)

**File**: `TransparentAiAgentCore/Infrastructure/LLM/AnthropicProvider.cs`

**Current dangerous behavior** (lines 286-293):
```csharp
if (string.IsNullOrWhiteSpace(jsonString))
{
    _transparencyService.LogEvent(...Warning...);
    jsonString = "{}"; // DANGEROUS DEFAULT!
}
```

**Proposed fix options**:

**Option A - Don't return malformed tool calls**:
- Skip adding tool calls with empty JSON to the response
- LLM never sees them, treats as if tool wasn't called
- **Issue**: LLM might retry infinitely or get confused

**Option B - Mark as malformed, fail at execution**:
- Add tool calls with empty args to response
- Flag them as "malformed" with metadata
- Fail during execution with clear error
- **Issue**: Requires passing metadata through pipeline

**Option C - RECOMMENDED: Let validation fail**:
- Return tool calls with empty `{}`
- Validation layer (Level 2) catches missing required params
- Return proper error to LLM via tool result
- LLM can retry with correct parameters
- **Advantage**: Clean separation of concerns

**Implementation**:
```csharp
if (string.IsNullOrWhiteSpace(jsonString))
{
    // Log ERROR (not warning) - this is a critical failure
    _transparencyService.LogEvent(new Domain.Transparency.TransparencyEvent(
        Domain.Transparency.TransparencyEventType.Error,
        $"Tool call '{name}' JSON accumulation failed - arguments will be empty. " +
        $"Schema validation should catch this if parameters are required.",
        "Streaming Tool Call Build"));

    jsonString = "{}"; // Keep for now, but validation will fail if params required
}
```

### Layer 2: Schema Validation (HIGH PRIORITY)

**New component**: `TransparentAiAgentCore/Infrastructure/Tools/Validation/ToolSchemaValidator.cs`

**Responsibilities**:
1. Parse tool's JSON schema string
2. Extract `required` fields from schema
3. Validate arguments JSON against schema before execution
4. Return detailed error if validation fails

**API**:
```csharp
public class ToolSchemaValidator
{
    public ValidationResult ValidateArguments(ITool tool, string argumentsJson)
    {
        // Parse schema
        var schema = JsonDocument.Parse(tool.ParametersSchema);

        // Extract required fields
        var required = ExtractRequiredFields(schema);

        // Validate arguments
        var args = JsonDocument.Parse(argumentsJson);

        // Check required fields present
        foreach (var requiredField in required)
        {
            if (!HasField(args, requiredField))
            {
                return ValidationResult.Failure(
                    $"Missing required parameter '{requiredField}'");
            }
        }

        // Could add type validation, enum validation, etc.

        return ValidationResult.Success();
    }
}
```

**Integration point**: Call from `ToolManager.ExecuteToolAsync()` before routing to executor

### Layer 3: Tool Executor Enhancement (HIGH PRIORITY)

**Files**:
- `TransparentAiAgentCore/Application/Tools/ToolManager.cs`
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutor.cs`

**Current flow**:
```
ToolManager.ExecuteToolAsync()
  → Parse JSON arguments
  → Route to executor
  → Executor.ExecuteAsync()
  → Return result
```

**Proposed flow**:
```
ToolManager.ExecuteToolAsync()
  → Parse JSON arguments
  → Validate against schema (NEW)
     ↓ (if validation fails)
     Return ToolExecutionResult.Failure with error details
  → Route to executor
  → Executor.ExecuteAsync()
  → Return result
```

**Error message format**:
```json
{
  "success": false,
  "error": "Schema validation failed: Missing required parameter 'show_tool_calls'",
  "validationErrors": [
    {
      "field": "show_tool_calls",
      "error": "required field missing"
    }
  ]
}
```

### Layer 4: LLM Error Communication (MEDIUM PRIORITY)

**Ensure errors propagate to LLM**:

1. **Tool result message** should contain full error details
2. **LLM sees**: "Tool execution failed: Missing required parameter 'show_tool_calls'"
3. **LLM can retry** with corrected parameters
4. **Conversation continues** without breaking

**Current**: Tool results already flow back to LLM via `ToolExecutionResult`
**Enhancement**: Ensure error messages are clear and actionable

### Layer 5: Transparency & Debugging (HIGH PRIORITY)

**Problem**: Cannot see raw LLM requests/responses in Transparency Viewer due to streaming

**Current transparency gaps**:
1. Streaming chunks logged individually (too noisy)
2. No final accumulated request/response visible
3. Hard to debug "what did we send to LLM?" and "what did LLM actually return?"

**Proposed solution**:

**Add final accumulated logging**:
```csharp
// In AnthropicProvider.SendMessageStreamingAsync()
// At message_stop event (after all chunks accumulated):

_transparencyService.LogEvent(new TransparencyEvent(
    TransparencyEventType.RawLLMRequest,
    JsonSerializer.Serialize(messageParams, indentedOptions),
    "Complete LLM Request"));

_transparencyService.LogEvent(new TransparencyEvent(
    TransparencyEventType.RawLLMResponse,
    JsonSerializer.Serialize(new {
        content = fullText,
        toolCalls = toolCalls,
        stopReason = stopReason,
        usage = usage
    }, indentedOptions),
    "Complete LLM Response"));
```

**New transparency event types** (already added):
- ✅ `RawLLMRequest` - Complete request after streaming starts
- ✅ `RawLLMResponse` - Complete accumulated response
- ✅ `ToolArgumentValidationFailed` (TODO) - When schema validation fails
- ✅ `ToolStreamingDataCorrupted` (TODO) - When JSON accumulation fails

**Benefits**:
- See exactly what was sent to LLM (including tool schemas)
- See exactly what LLM returned (including tool call arguments)
- Debug accumulation issues (compare streaming chunks vs final result)
- Verify tool schemas are correct

---

## Implementation Phases

### Phase 1: Immediate Safety (CRITICAL - Do First)

**Goal**: Prevent silent failures, add basic validation

**Tasks**:
1. ✅ Fix Anthropic SDK method name (`TryPickInputJSON`)
2. Change empty JSON default from Warning to Error level
3. Add schema validation to ToolManager before execution
4. Create ToolSchemaValidator class
5. Add `ToolArgumentValidationFailed` transparency event type
6. Test with UI control tools (require parameters, verify failure)

**Acceptance criteria**:
- Tools with required parameters FAIL if arguments are empty/missing
- Error is visible in Transparency Viewer
- LLM receives error in tool result
- No tools execute with invalid arguments

**Estimated effort**: 4-6 hours

### Phase 2: Enhanced Transparency (HIGH PRIORITY)

**Goal**: Make streaming requests/responses visible for debugging

**Tasks**:
1. Add final accumulated request logging to AnthropicProvider
2. Add final accumulated response logging to AnthropicProvider
3. Add `ToolStreamingDataCorrupted` transparency event type
4. Log when JSON accumulation fails during streaming
5. Make these events prominent in Transparency Viewer UI
6. Test with various tool call scenarios

**Acceptance criteria**:
- Can see complete request sent to LLM in Transparency Viewer
- Can see complete response from LLM in Transparency Viewer
- Can see tool call arguments as LLM provided them
- Can debug schema/accumulation issues

**Estimated effort**: 3-4 hours

### Phase 3: Robust Validation (MEDIUM PRIORITY)

**Goal**: Comprehensive schema validation beyond required fields

**Tasks**:
1. Enhance ToolSchemaValidator with type checking
2. Add enum validation
3. Add format validation (e.g., regex patterns)
4. Add range validation (min/max)
5. Add custom validation rules if needed
6. Write comprehensive tests

**Acceptance criteria**:
- Validates required fields ✓
- Validates field types (string, boolean, number, array)
- Validates enum values
- Clear error messages for each validation failure

**Estimated effort**: 6-8 hours

### Phase 4: Testing & Documentation (MEDIUM PRIORITY)

**Goal**: Ensure robustness and maintainability

**Tasks**:
1. Unit tests for ToolSchemaValidator
2. Integration tests for tool execution pipeline
3. Test edge cases (malformed JSON, missing schema, etc.)
4. Update tool system documentation
5. Add troubleshooting guide for tool failures
6. Document transparency events in viewer guide

**Acceptance criteria**:
- 90%+ code coverage for validation layer
- All edge cases tested
- Documentation updated
- Team can debug tool issues using Transparency Viewer

**Estimated effort**: 4-6 hours

---

## Success Criteria

### Before (Current State)
- ❌ Tools execute with empty arguments when accumulation fails
- ❌ No validation against schema
- ❌ Silent failures with only warnings
- ❌ Cannot see raw LLM requests/responses
- ❌ Hard to debug tool argument issues

### After (Target State)
- ✅ Tools FAIL if required parameters missing
- ✅ Schema validation before execution
- ✅ Clear errors logged to Transparency Viewer
- ✅ Raw LLM requests/responses visible
- ✅ Easy to debug tool issues
- ✅ LLM receives actionable error messages
- ✅ Safe tool execution guaranteed

---

## Risks & Mitigations

### Risk 1: Breaking existing tool calls
**Mitigation**: Phase rollout, test with all existing tools first

### Risk 2: Performance impact from validation
**Mitigation**: Validation is fast (JSON parsing), happens once per tool call

### Risk 3: Schema parsing complexity
**Mitigation**: Start simple (required fields only), iterate

### Risk 4: Transparency log volume
**Mitigation**: Only log complete request/response (not chunks), filter by event type in UI

---

## Open Questions

1. **Should we retry failed tool calls automatically?**
   - Probably no - let LLM decide to retry
   - Log the failure, return error, let LLM handle it

2. **How to handle tools with all-optional parameters?**
   - Empty `{}` is valid if no required fields
   - Validation passes
   - Tool decides if defaults are acceptable

3. **Should validation be configurable per tool?**
   - Some tools might want strict validation
   - Others might want permissive (extra fields ok)
   - Start strict, add config if needed

4. **How to show validation errors in UI?**
   - Transparency Viewer shows event
   - Tool result in chat shows error
   - Maybe add validation status to tool cards?

---

## Related Issues

- Original issue: UI tools receiving empty arguments
- Root cause: Anthropic SDK method name wrong (`TryPickInputJsonDelta` vs `TryPickInputJSON`)
- Deeper issue: No safety net when streaming fails
- Architecture gap: No schema validation in tool pipeline
- Transparency gap: Cannot see raw LLM requests/responses

---

## Next Steps

1. **Immediate**: Implement Phase 1 (safety fixes)
2. **Short term**: Implement Phase 2 (transparency)
3. **Medium term**: Implement Phase 3 (robust validation)
4. **Long term**: Implement Phase 4 (testing & docs)

---

**Document Status**: Initial draft
**Last Updated**: 2025-11-09
**Owner**: Architecture/Core Team
