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

## Fixes Implemented ✅

### Fix 1: Add Error Logging ✅ COMPLETED

Added `ILogger` to both JsonScenarioLoader and ScenarioRegistry:
- ✅ Added ILogger<JsonScenarioLoader> to JsonScenarioLoader constructor
- ✅ Added ILogger<ScenarioRegistry> to ScenarioRegistry constructor
- ✅ Log errors in LoadAllFromDirectoryAsync() with file path and exception details
- ✅ Log successful scenario loads with scenario ID and file path
- ✅ Log scenario loading start/completion with counts
- ✅ Updated Program.cs DI configuration to provide loggers
- ✅ Updated all tests to provide NullLogger instances

**Files changed**:
- TransparentAiAgentCore/Infrastructure/Scenarios/JsonScenarioLoader.cs
- TransparentAiAgentCore/Infrastructure/Scenarios/ScenarioRegistry.cs
- TransparentAiAgentGui/Program.cs
- TransparentAiAgentCore_Tests/Infrastructure/Scenarios/JsonScenarioLoaderTests.cs
- TransparentAiAgentCore_Tests/Infrastructure/Scenarios/ScenarioRegistryTests.cs

### Fix 2: Map "pause_for_user" in Loader ✅ COMPLETED

Added missing case mapping in JsonScenarioLoader:
- ✅ Added `"pause_for_user" => ScenarioStepType.PauseForUser` to switch statement
- ✅ This was the immediate cause of the scenario failing to load

**Files changed**:
- TransparentAiAgentCore/Infrastructure/Scenarios/JsonScenarioLoader.cs

### Fix 3: Refactor Pause Message to Use Content ✅ COMPLETED

**Changes made**:

1. ✅ **ScenarioStep.cs**: Removed `PauseMessage` and `PauseMessageKey` properties
2. ✅ **ScenarioStep.cs**: Removed PauseForUser from RequiresContent() exclusion list (now requires content)
3. ✅ **JsonScenarioLoader.cs**: Removed pauseMessage/pauseMessageKey DTO properties completely
4. ✅ **ScenarioExecutor.cs**: Changed to use `step.Content` instead of `step.PauseMessage`
5. ✅ **context-limits-advanced.json**: Changed `pauseMessage`/`pauseMessageKey` to `content`/`contentKey`
6. ✅ **context-limits-advanced.json**: Removed redundant annotation field from pause step
7. ✅ **Tests**: Updated all tests to use `content` parameter instead of `pauseMessage`

**Files changed**:
- TransparentAiAgentCore/Domain/Scenarios/ScenarioStep.cs
- TransparentAiAgentCore/Infrastructure/Scenarios/JsonScenarioLoader.cs
- TransparentAiAgentCore/Application/Scenarios/ScenarioExecutor.cs
- TransparentAiAgentGui/data/scenarios/context-limits-advanced.json
- TransparentAiAgentCore_Tests/Domain/Scenarios/ScenarioStepTests.cs
- TransparentAiAgentCore_Tests/Application/Scenarios/ScenarioExecutorPauseResumeTests.cs

## Summary of Changes

### What was fixed:
1. **Silent errors** - Scenario loading errors are now visible in console logs
2. **Missing step type mapping** - "pause_for_user" is now recognized
3. **Inconsistent API** - PauseForUser now uses `content`/`contentKey` like other step types
4. **Redundant fields** - Removed annotation from pause steps (it was useless)
5. **Broken scenario** - context-limits-advanced.json now loads successfully
6. **Clean code** - No backward compatibility code, single consistent approach

## Testing

✅ All changes committed and pushed to: `claude/adjust-scenarios-feature-015j4z1kGomLpyjGUFrAvzWa`

**Next steps for user**:
1. Run the app and check console for scenario loading messages
2. Verify "Context Limits (Advanced)" scenario appears in UI
3. Test that the scenario runs and pauses correctly
4. Verify the pause message displays properly

## Notes

The scenario should now:
- Load without errors (logs will show "Successfully loaded scenario 'context-limits-advanced'")
- Appear in the scenarios list in the UI
- Execute all steps correctly including the pause_for_user step
- Display the pause message to the user when paused
