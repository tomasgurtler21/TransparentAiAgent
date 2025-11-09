# Tool Schema Validator

**Last Updated**: 2025-11-09
**Status**: Active
**Phase**: Phase 9b (Tool Execution Safety)
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- ToolSchemaValidator implementation
- Schema validation logic (required fields, types, enums)
- ValidationResult model
- Integration with ToolManager

**What does NOT belong here**:
- ❌ Tool execution → See [tool-manager.md](tool-manager.md)
- ❌ Tool registries → See [mcp/](mcp/), [builtin/](builtin/)
- ❌ Schema definitions → Defined by tools themselves

---

## Overview

ToolSchemaValidator validates tool arguments against JSON Schema before execution. It ensures required parameters are present, types are correct, and enum values are valid, preventing unsafe tool execution with malformed arguments.

## Purpose

- **Validate** tool arguments against JSON Schema before execution
- **Prevent** tool execution with missing/invalid arguments
- **Provide** clear error messages for LLM to retry with correct parameters
- **Guarantee** safe tool execution

## Responsibilities

- Parse JSON Schema from tool definitions
- Extract required fields from schema
- Validate required fields are present in arguments
- Validate field types match schema expectations
- Validate enum values are in allowed list
- Return detailed validation errors or success

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Tools/Validation/ToolSchemaValidator.cs`

```csharp
public class ToolSchemaValidator
{
    public ValidationResult ValidateArguments(string schema, string? argumentsJson)
    {
        // 1. Validate arguments is not null
        // 2. Parse arguments JSON
        // 3. Parse schema JSON
        // 4. Extract required fields from schema
        // 5. Check required fields present in arguments
        // 6. Validate types and enums for all properties
        // 7. Return ValidationResult (success or failure)
    }
}
```

**File**: `TransparentAiAgentCore/Infrastructure/Tools/Validation/ValidationResult.cs`

```csharp
public class ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static ValidationResult Success();
    public static ValidationResult Failure(string errorMessage);
}
```

### Dependencies

**Depends on**:
- `System.Text.Json` - JSON parsing and manipulation

**Used by**:
- `Application/Tools/ToolManager` - Validates before tool execution

## Validation Logic

### Phase 1: Required Field Validation

**Purpose**: Ensure all required parameters are present

**Logic**:
1. Extract `required` array from schema root
2. For each required field:
   - Check if field exists in arguments JSON
   - If missing → Return failure with field name

**Error Message**: `"Missing required parameter '{fieldName}'"`

### Phase 3: Type Validation

**Purpose**: Ensure field values match expected types

**Supported Types**:
- `string` → JsonValueKind.String
- `number` / `integer` → JsonValueKind.Number
- `boolean` → JsonValueKind.True or JsonValueKind.False
- `object` → JsonValueKind.Object
- `array` → JsonValueKind.Array
- `null` → JsonValueKind.Null

**Logic**:
1. For each property in schema with a `type` field:
   - Get actual value kind from arguments
   - Compare with expected type
   - If mismatch → Return failure

**Error Message**: `"Parameter '{fieldName}' has invalid type. Expected '{expectedType}' but got '{actualKind}'"`

### Phase 3: Enum Validation

**Purpose**: Ensure field values are in allowed enum list

**Logic**:
1. For each property in schema with an `enum` field:
   - Extract allowed values from enum array
   - Check if actual value matches any allowed value
   - If no match → Return failure with allowed values list

**Error Message**: `"Parameter '{fieldName}' has invalid value '{actualValue}'. Allowed values: [value1, value2, ...]"`

## Integration with ToolManager

### Validation Flow

**File**: `TransparentAiAgentCore/Application/Tools/ToolManager.cs:89-114`

```csharp
// 3a. Validate arguments against schema BEFORE execution
var validationResult = _validator.ValidateArguments(tool.ParametersSchema, toolCall.Arguments);
if (!validationResult.IsValid)
{
    stopwatch.Stop();

    // Log validation failure to Transparency System
    _transparencyService.LogEvent(new TransparencyEvent(
        TransparencyEventType.ToolArgumentValidationFailed,
        /* ... validation details ... */));

    // Record statistics for failed validation
    _statistics.RecordToolCall(tool.Name, false, stopwatch.Elapsed);

    return ToolExecutionResult.Failure(
        $"Schema validation failed: {validationResult.ErrorMessage}",
        stopwatch.Elapsed);
}

// 4. Execute tool (only if validation passed)
var result = await executor.ExecuteAsync(tool, toolCall.Arguments, cancellationToken);
```

**Key Points**:
- Validation happens BEFORE tool execution
- Validation failures are logged to Transparency Viewer
- LLM receives clear error message via ToolExecutionResult
- No tool executes with invalid arguments

## Dependency Injection

**File**: `TransparentAiAgentGui/Program.cs:44`

```csharp
// Register Core Infrastructure services
builder.Services.AddSingleton<ToolSchemaValidator>();
```

**File**: `TransparentAiAgentGui/Program.cs:147`

```csharp
// Get tool schema validator service (Phase 9b - Tool Execution Safety)
var validator = sp.GetRequiredService<ToolSchemaValidator>();

// Create Tool Manager with all available executors
var toolManager = new ToolManager(
    toolRegistry,
    executors,
    transparencyService,
    statistics,
    validator);
```

## Error Scenarios

### 1. Null Arguments

**Input**: `argumentsJson = null`

**Result**: `ValidationResult.Failure("Arguments cannot be null")`

### 2. Invalid JSON

**Input**: `argumentsJson = "{invalid json}"`

**Result**: `ValidationResult.Failure("Invalid JSON in arguments: {ex.Message}")`

### 3. Missing Required Field

**Schema**: `{ "required": ["name"] }`

**Arguments**: `{}`

**Result**: `ValidationResult.Failure("Missing required parameter 'name'")`

### 4. Wrong Type

**Schema**: `{ "properties": { "age": { "type": "number" } } }`

**Arguments**: `{ "age": "not a number" }`

**Result**: `ValidationResult.Failure("Parameter 'age' has invalid type. Expected 'number' but got 'String'")`

### 5. Invalid Enum Value

**Schema**: `{ "properties": { "status": { "enum": ["active", "inactive"] } } }`

**Arguments**: `{ "status": "deleted" }`

**Result**: `ValidationResult.Failure("Parameter 'status' has invalid value 'deleted'. Allowed values: [active, inactive]")`

## Testing

**File**: `TransparentAiAgentCore_Tests/Infrastructure/Tools/ToolSchemaValidatorTests.cs`

**Test Coverage** (13 tests):
- ✅ Required field validation (3 tests)
- ✅ Null and invalid input handling (2 tests)
- ✅ Type validation (3 tests)
- ✅ Enum validation (2 tests)
- ✅ Edge cases (3 tests)

**TDD Approach**: All tests follow Lean TDD principles:
1. Write test FIRST (RED phase)
2. Implement minimum code to pass (GREEN phase)
3. Tests validate meaningful behavior, not compiler features

## Transparency Events

### ToolArgumentValidationFailed

**Event Type**: `TransparencyEventType.ToolArgumentValidationFailed`

**Logged When**: Tool arguments fail schema validation

**Event Data**:
```json
{
  "ToolName": "example_tool",
  "Arguments": "{}",
  "Schema": "{ ... }",
  "ValidationError": "Missing required parameter 'name'",
  "CallId": "abc123"
}
```

**Purpose**: Allows debugging validation failures in Transparency Viewer

## Future Enhancements (Optional)

### Advanced Validation (Deferred)

**Not critical for basic safety, can be added if needed**:

1. **Format Validation**:
   - Regex patterns (e.g., email, URL formats)
   - Schema: `{ "format": "email" }`

2. **Range Validation**:
   - Min/max for numbers
   - Schema: `{ "minimum": 0, "maximum": 100 }`

3. **Custom Validation Rules**:
   - Complex business logic validation
   - Cross-field validation

**Note**: Current implementation meets all critical safety requirements.

## References

- **Implementation Plan**: `docs/TOOL_EXECUTION_SAFETY_PLAN.md`
- **Tool Manager**: `docs/04-components/tools/tool-manager.md`
- **Transparency Events**: `docs/04-components/infrastructure/transparency-service.md`

---

**This component is part of Phase 9b: Tool Execution Safety**
