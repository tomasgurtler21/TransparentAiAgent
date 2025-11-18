# Scenario Loading Issues - Analysis and Fixes

## Issues Found

### 1. Silent Scenario Loading Errors ❌

**Problem**: Scenario loading failures are completely silent - no console output, no logs, no indication that anything went wrong.

**Locations**:
- `JsonScenarioLoader.cs:88-93` - Swallows all exceptions in `LoadAllFromDirectoryAsync()`
- `ScenarioRegistry.cs:64-68` - Swallows all exceptions in `LoadScenariosAsync()`

**Impact**: User has no idea why scenarios aren't showing up in the app.

### 2. Missing "pause_for_user" Step Type Mapping ❌

**Problem**: The JSON scenario uses `"type": "pause_for_user"` but the loader doesn't recognize it.

**Location**: `JsonScenarioLoader.cs:233-247` - Missing case for "pause_for_user"

**Current enum**: `ScenarioStepType.PauseForUser` exists (ScenarioStepType.cs:73)

**Fix needed**: Add case mapping `"pause_for_user"` → `ScenarioStepType.PauseForUser`

### 3. Missing Pause Message DTO Properties ❌

**Problem**: The JSON has `pauseMessage` and `pauseMessageKey` fields, but the DTO doesn't deserialize them.

**Location**: `JsonScenarioLoader.cs` - `ScenarioStepDto` class

**Domain model has**:
- `ScenarioStep.PauseMessage` (line 63)
- `ScenarioStep.PauseMessageKey` (line 69)

**DTO missing**: These properties aren't in `ScenarioStepDto`

### 4. Pause Message Design Issue ⚠️

**User's concern**: Why does `pause_for_user` have special fields `pauseMessage`/`pauseMessageKey` instead of using `content`/`contentKey` like other step types?

**Current design**:
```json
{
  "type": "pause_for_user",
  "pauseMessageKey": "...",
  "pauseMessage": "...",
  "annotationKey": "...",
  "annotation": ""
}
```

**Suggested design**:
```json
{
  "type": "pause_for_user",
  "contentKey": "...",
  "content": "..."
}
```

**User's note**: Annotation is completely useless for pause messages - the whole message is displayed to the user only, so annotation is redundant.

## Fixes to Implement

### Fix 1: Add Error Logging ✅

Add `ILogger` to both classes and log errors with details:
- File path that failed
- Exception type and message
- Stack trace for debugging

### Fix 2: Map "pause_for_user" in Loader ✅

Add case in `ToScenarioStep()`:
```csharp
"pause_for_user" => ScenarioStepType.PauseForUser,
```

### Fix 3: Add Pause Message DTO Properties ✅

Add to `ScenarioStepDto`:
```csharp
[JsonPropertyName("pauseMessage")]
public string? PauseMessage { get; set; }

[JsonPropertyName("pauseMessageKey")]
public string? PauseMessageKey { get; set; }
```

Pass to `ScenarioStep` constructor in `ToScenarioStep()`.

### Fix 4: Refactor Pause Message to Use Content ✅

**Changes needed**:

1. **ScenarioStep.cs**: Remove `PauseMessage` and `PauseMessageKey` properties
2. **JsonScenarioLoader.cs**: Remove pause message DTO properties
3. **ScenarioExecutor.cs**: Use `step.Content` instead of `step.PauseMessage` for pause steps
4. **Update JSON**: Change `pauseMessage`/`pauseMessageKey` to `content`/`contentKey`
5. **Remove annotation**: It's redundant for pause messages

## Testing

After fixes:
1. Run app and check console for scenario loading messages
2. Verify scenario appears in UI
3. Test pause functionality works correctly
4. Verify pause message displays the content properly

## Questions for User

If any design decisions need clarification, they will be documented here.
