# Long-Term Memory Feature - Design Document

**Created**: 2025-11-16
**Status**: Design Phase
**Target Release**: First Release (v1.0)

---

## Executive Summary

The Long-Term Memory feature enables the agent to remember user preferences, context, and background across conversations. It uses simple markdown files (one per mode) that the agent can read and update through built-in tools, with explicit user control via a checkbox.

**Core Philosophy**: Simple, transparent, user-controlled memory that enhances continuity without compromising privacy.

---

## Design Goals

### Primary Goals
1. **Simplicity First**: Markdown files + two tools + one checkbox
2. **Privacy-Conscious**: Guardrails prevent storage of sensitive data
3. **Mode-Aware**: Separate memories for Normal and Teaching modes
4. **User Control**: Explicit opt-in via checkbox, clear visibility
5. **Transparent**: All memory operations visible in tool calls and transparency logs
6. **Local-First**: Files stored locally, no cloud dependencies

### Non-Goals (for v1.0)
- ❌ Multi-tenant/multi-user support
- ❌ Diff-based updates (full overwrite only)
- ❌ Memory search/query capabilities beyond full read
- ❌ Automatic memory extraction (LLM decides when to update)
- ❌ Memory versioning/history
- ❌ Encryption (files are plaintext markdown)

---

## High-Level Architecture

### Component Overview

```
┌───────────────────────────────────────────────────────┐
│           Long-Term Memory System                     │
│                                                        │
│  ┌────────────────────────────────────────────────┐  │
│  │ Built-In Memory Tools (2 tools)                │  │
│  │ - long_term_memory_read                        │  │
│  │ - long_term_memory_update                      │  │
│  └────────────────────────────────────────────────┘  │
│                            │                          │
│                            ▼                          │
│  ┌────────────────────────────────────────────────┐  │
│  │ LongTermMemoryService (scoped)                 │  │
│  │ - Reads/writes markdown files                  │  │
│  │ - Mode-aware file selection                    │  │
│  │ - Logs all operations to Transparency          │  │
│  └────────────────────────────────────────────────┘  │
│                            │                          │
│                            ▼                          │
│  ┌────────────────────────────────────────────────┐  │
│  │ Storage Layer (Infrastructure)                 │  │
│  │ - memory-normal.md                             │  │
│  │ - memory-teaching.md                           │  │
│  │ - Location: ./data/memory/                     │  │
│  └────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────┘
              │
              ▼ (integrates with existing tool system)
┌───────────────────────────────────────────────────────┐
│  Tool System (ToolSourceType.BuiltInLongTermMemory)  │
│  - ToolManager routes to LongTermMemoryToolExecutor  │
│  - BuiltInLongTermMemoryToolRegistry                 │
└───────────────────────────────────────────────────────┘
```

### Integration with Conversation Lifecycle

```
┌─────────────────────────────────────────────────────┐
│  User Action: Start new conversation               │
│  (checkbox "Use Long Term Memory" is checked)       │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│  ConversationUIService.StartNewConversation()       │
│  - Checks if memory is enabled                      │
│  - Calls LongTermMemoryService.LoadMemoryForMode()  │
│  - Injects memory as system message                 │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│  System Message Injected:                           │
│  "NEW CONVERSATION STARTED                          │
│                                                      │
│   Here is your long-term memory for this user:      │
│   [markdown content]                                │
│                                                      │
│   Use this to personalize responses..."             │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│  ... User has conversation ...                      │
│  Agent can call tools:                              │
│  - long_term_memory_read (re-check memory)          │
│  - long_term_memory_update (save new info)          │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│  User Action: End conversation                      │
│  (closes app, starts new conversation, clears chat) │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│  ConversationUIService.EndConversation()            │
│  - If memory enabled, sends prompt to LLM:          │
│    "Please review the conversation and update       │
│     long-term memory if you learned anything        │
│     important about the user..."                    │
│  - LLM calls long_term_memory_update if needed      │
│  - Waits for response (timeout: 30s)                │
└─────────────────────────────────────────────────────┘
```

---

## Detailed Component Design

### 1. Storage Layer

**File Structure**:
```
./data/memory/
├── memory-normal.md      # Normal mode memory
└── memory-teaching.md    # Teaching mode memory
```

**File Format** (Markdown):
```markdown
# Long-Term Memory

**Last Updated**: 2025-11-16 14:30 UTC
**Mode**: Normal

---

## User Profile

- **Name**: Tomas
- **Role**: Software Engineer
- **Experience Level**: Senior
- **Tech Background**: C#, .NET, Blazor, AI/LLM integration

---

## Preferences

- Prefers concise explanations with code examples
- Interested in clean architecture patterns
- Values transparency and testability
- Working on TransparentAiAgent project

---

## Context

- Currently implementing Phase 10 features
- Prefers TDD approach (MSTest)
- Uses design → plan → implement workflow
- Environment prone to crashes, prefers file-based communication

---

## Topics of Interest

- LLM integration patterns
- Blazor Server architecture
- Tool-calling systems (MCP protocol)
- Teaching mode design

---

## Notes

- Remember to commit and push before communicating with user
- Avoid verbose explanations unless requested
```

**Guardrails** (enforced via tool description and validation):
- ✅ First name only (no full names)
- ✅ Professional role and tech background
- ✅ Preferences and working style
- ✅ Project context
- ❌ NO sensitive personal information (address, phone, email, etc.)
- ❌ NO credentials or API keys
- ❌ NO private/confidential project details
- ❌ NO health or financial information

**File Size Limits**:
- Maximum: 10,000 characters (~2,500 tokens)
- Enforced by tool validation
- If exceeded, LLM must summarize/condense

### 2. Domain Layer

**Interface**: `ILongTermMemoryService`
```csharp
namespace TransparentAiAgentCore.Domain.Memory;

/// <summary>
/// Service for managing long-term memory storage.
/// Provides mode-aware memory persistence using markdown files.
/// </summary>
public interface ILongTermMemoryService
{
    /// <summary>
    /// Reads the memory file for the specified mode.
    /// Returns empty string if file doesn't exist.
    /// </summary>
    Task<string> ReadMemoryAsync(AppMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the memory file for the specified mode.
    /// Creates file if it doesn't exist.
    /// </summary>
    Task<MemoryUpdateResult> UpdateMemoryAsync(
        AppMode mode,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if memory exists for the specified mode.
    /// </summary>
    Task<bool> HasMemoryAsync(AppMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last update timestamp for the specified mode.
    /// Returns null if file doesn't exist.
    /// </summary>
    Task<DateTime?> GetLastUpdateTimeAsync(AppMode mode, CancellationToken cancellationToken = default);
}

public record MemoryUpdateResult(
    bool Success,
    string? Error = null,
    int CharacterCount = 0,
    DateTime UpdatedAt = default);
```

**Configuration**:
```csharp
namespace TransparentAiAgentCore.Domain.Memory;

public class LongTermMemoryConfiguration
{
    /// <summary>
    /// Enable/disable long-term memory feature
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Directory path for memory files (relative to app root)
    /// </summary>
    public string StorageDirectory { get; set; } = "data/memory";

    /// <summary>
    /// Maximum memory file size in characters
    /// </summary>
    public int MaxCharacters { get; set; } = 10_000;

    /// <summary>
    /// Whether to auto-load memory at conversation start
    /// </summary>
    public bool AutoLoadOnStart { get; set; } = true;

    /// <summary>
    /// Whether to prompt for memory update on conversation end
    /// </summary>
    public bool PromptUpdateOnEnd { get; set; } = true;

    /// <summary>
    /// Timeout for memory update prompt (seconds)
    /// </summary>
    public int UpdatePromptTimeoutSeconds { get; set; } = 30;
}
```

### 3. Built-In Tools

**Tool 1: `long_term_memory_read`**
```csharp
public class LongTermMemoryReadTool : ITool
{
    public string Name => "long_term_memory_read";

    public string Description =>
        "Read the full content of your long-term memory for the current mode (Normal or Teaching). " +
        "Use this to refresh your memory about the user's preferences, background, and context. " +
        "Memory is stored in markdown format and includes user profile, preferences, and notes.";

    public string ParametersSchema => """
    {
      "type": "object",
      "properties": {},
      "required": []
    }
    """;

    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;
}
```

**Tool 2: `long_term_memory_update`**
```csharp
public class LongTermMemoryUpdateTool : ITool
{
    public string Name => "long_term_memory_update";

    public string Description =>
        "Update your long-term memory with new information about the user. " +
        "This OVERWRITES the entire memory file, so include all information you want to keep. " +
        "\n\n" +
        "IMPORTANT GUARDRAILS:\n" +
        "- Store ONLY: first name, professional role, tech background, preferences, project context\n" +
        "- NEVER store: full names, email, phone, address, credentials, API keys, sensitive data\n" +
        "- Keep it concise (max 10,000 characters)\n" +
        "- Use markdown format with clear sections\n" +
        "- Update the 'Last Updated' timestamp\n" +
        "\n" +
        "Think carefully: Is this information helpful for future conversations? Is it appropriate to store?";

    public string ParametersSchema => """
    {
      "type": "object",
      "properties": {
        "content": {
          "type": "string",
          "description": "The complete markdown content to store in memory (max 10,000 characters)"
        },
        "reason": {
          "type": "string",
          "description": "Brief explanation of why you're updating memory (for transparency logs)"
        }
      },
      "required": ["content", "reason"]
    }
    """;

    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;
}
```

**Tool Executor**: `LongTermMemoryToolExecutor`
- Implements `IToolExecutor`
- Routes to `ILongTermMemoryService`
- Validates content length (max 10,000 chars)
- Logs all operations to `TransparencyService`
- Execution time: <50ms (local file I/O)

**Tool Registry**: `BuiltInLongTermMemoryToolRegistry`
- Implements `IToolRegistry`
- Returns both tools in `GetAllTools()`
- Follows same pattern as `BuiltInKnowledgeToolRegistry`

### 4. Application Layer Integration

**ConversationUIService Changes**:

```csharp
// New dependency
private readonly ILongTermMemoryService _memoryService;
private readonly LongTermMemoryConfiguration _memoryConfig;

// New property
public bool IsMemoryEnabled { get; private set; } = false;

// New method
public async Task SetMemoryEnabledAsync(bool enabled)
{
    IsMemoryEnabled = enabled;

    if (enabled && _memoryConfig.AutoLoadOnStart)
    {
        await LoadMemoryIntoConversation();
    }

    // Persist setting to user preferences (future: browser localStorage)
}

// Auto-load memory on conversation start
private async Task LoadMemoryIntoConversation()
{
    if (!IsMemoryEnabled) return;

    var currentMode = _appModeService.CurrentMode;
    var memoryContent = await _memoryService.ReadMemoryAsync(currentMode);

    if (string.IsNullOrWhiteSpace(memoryContent))
    {
        // No memory yet, skip
        return;
    }

    var systemMessage = BuildMemoryIntroductionMessage(memoryContent);
    await _orchestrator.ProcessSystemMessageAsync(systemMessage);
}

// Auto-prompt for memory update on conversation end
public async Task EndConversationAsync()
{
    if (!IsMemoryEnabled || !_memoryConfig.PromptUpdateOnEnd)
    {
        return;
    }

    var prompt =
        "CONVERSATION ENDING: Please review our conversation. " +
        "If you learned anything important about the user (preferences, background, context), " +
        "update long-term memory using the long_term_memory_update tool. " +
        "If nothing significant changed, no action needed.";

    // Send prompt and wait for response (with timeout)
    using var cts = new CancellationTokenSource(
        TimeSpan.FromSeconds(_memoryConfig.UpdatePromptTimeoutSeconds));

    try
    {
        await _orchestrator.ProcessSystemMessageAsync(
            new SystemInstructionMessage(prompt),
            cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Timeout - that's fine, memory update is best-effort
        _logger.LogWarning("Memory update prompt timed out");
    }
}
```

### 5. UI Layer

**Home.razor Changes**:

Add checkbox to main chat page (above or below message input):

```razor
<div class="memory-controls">
    <label>
        <input type="checkbox"
               @bind="IsMemoryEnabled"
               @bind:after="OnMemoryToggled" />
        <span>Use long-term memory</span>
        <span class="info-icon" title="Allow agent to remember your preferences and context across conversations">ℹ️</span>
    </label>

    @if (IsMemoryEnabled && HasMemory)
    {
        <button class="btn-link" @onclick="ViewMemory">View Memory</button>
        <button class="btn-link" @onclick="ClearMemory">Clear Memory</button>
    }
</div>

@code {
    private bool IsMemoryEnabled { get; set; } = false;
    private bool HasMemory { get; set; } = false;

    private async Task OnMemoryToggled()
    {
        await ConversationUIService.SetMemoryEnabledAsync(IsMemoryEnabled);

        if (IsMemoryEnabled)
        {
            // Check if memory exists
            HasMemory = await MemoryService.HasMemoryAsync(AppModeService.CurrentMode);
        }
    }

    private async Task ViewMemory()
    {
        // Show modal dialog with memory content (read-only markdown viewer)
        var content = await MemoryService.ReadMemoryAsync(AppModeService.CurrentMode);
        await ModalService.ShowMemoryViewerAsync(content);
    }

    private async Task ClearMemory()
    {
        if (await ConfirmService.ConfirmAsync("Clear long-term memory?"))
        {
            await MemoryService.UpdateMemoryAsync(AppModeService.CurrentMode, string.Empty);
            HasMemory = false;
        }
    }
}
```

**Styling**:
- Checkbox styled consistently with existing UI
- Info icon provides tooltip explaining the feature
- View/Clear buttons only visible when memory exists
- Modal dialog for viewing memory (read-only, markdown rendered)

---

## Data Flow Examples

### Example 1: First Conversation with Memory Enabled

```
1. User checks "Use long-term memory" checkbox
   → IsMemoryEnabled = true
   → LoadMemoryIntoConversation() called
   → memory-normal.md doesn't exist yet → skip

2. User: "Hi, I'm Tomas. I'm working on a Blazor project."
   → Agent responds normally

3. User closes app
   → EndConversationAsync() called
   → System prompt: "Review conversation and update memory if appropriate"
   → LLM decides to update memory
   → Calls long_term_memory_update with:
      {
        "content": "# Long-Term Memory\n**Last Updated**: 2025-11-16...\n## User Profile\n- Name: Tomas\n...",
        "reason": "Initial user introduction - stored name and project context"
      }
   → LongTermMemoryToolExecutor validates and saves to memory-normal.md

4. Next day: User starts new conversation (memory still enabled)
   → LoadMemoryIntoConversation() called
   → Reads memory-normal.md
   → Injects system message: "Here is your long-term memory for this user..."
   → Agent now knows user is Tomas working on Blazor project
```

### Example 2: Agent Proactively Updates Memory Mid-Conversation

```
1. User: "I prefer TDD approach, always write tests first"

2. Agent (thinking): This is an important preference, should remember it
   → Calls long_term_memory_read to check current memory
   → Calls long_term_memory_update to add preference
   → Responds: "Got it, I'll remember you prefer TDD. I've updated my memory."

3. All tool calls visible in transparency logs
```

### Example 3: Mode Switch Isolation

```
1. User in Normal mode with memory enabled
   → Memory reads/writes from memory-normal.md

2. User switches to Teaching mode
   → AppModeService.SwitchModeAsync(AppMode.Teaching)
   → LoadMemoryIntoConversation() called
   → Reads memory-teaching.md (separate file!)
   → Teaching mode memory is independent from Normal mode

3. Updates in Teaching mode don't affect Normal mode memory
```

---

## System Prompts

### Memory Introduction Prompt (Auto-Injected)

```
NEW CONVERSATION STARTED

Here is your long-term memory for this user in {mode} mode:

---
{memory_content}
---

Use this information to:
- Personalize your responses based on their preferences and background
- Avoid asking questions you already know the answer to
- Maintain context from previous conversations
- Tailor explanations to their experience level

You can update this memory anytime using the `long_term_memory_update` tool if you learn new important information.

Remember: ONLY store appropriate information (first name, role, preferences, tech background). NEVER store sensitive data.
```

### Memory Update Prompt (Conversation End)

```
CONVERSATION ENDING

Please review our conversation and consider if you learned anything important about the user that would be helpful to remember for future conversations.

If yes, update long-term memory using the `long_term_memory_update` tool with the new information.

Store ONLY:
✅ First name, professional role, tech background
✅ Preferences, working style, communication style
✅ Project context, topics of interest
✅ Important notes for future interactions

NEVER store:
❌ Sensitive personal information
❌ Credentials, API keys, secrets
❌ Private/confidential details
❌ Health, financial, or location data

If nothing significant changed, no action is needed.
```

---

## Privacy & Security Considerations

### Guardrails

1. **Tool Description Guardrails**:
   - Explicit warnings in `long_term_memory_update` description
   - Lists what should/shouldn't be stored
   - Reminds LLM to think before storing

2. **Validation Guardrails** (Future Enhancement):
   - Regex patterns to detect emails, phone numbers, URLs with auth tokens
   - Warning if detected (don't block, but log warning)
   - Size limit enforcement (10,000 chars)

3. **User Control Guardrails**:
   - Memory is opt-in (disabled by default)
   - User can view memory anytime
   - User can clear memory anytime
   - All memory operations logged to transparency system

### Transparency

- Every memory read/update is a tool call → visible in UI
- Transparency logs show:
  - When memory was read
  - When memory was updated
  - Reason for update (from tool arguments)
  - Character count
- User always knows what's being stored

### Data Storage

- Files stored locally in `./data/memory/`
- Plaintext markdown (no encryption in v1.0)
- No cloud sync (local-first design)
- Files committed to git? → User's choice (gitignore recommended)

**Recommended `.gitignore` entry**:
```
# Long-term memory (user-specific, don't commit)
data/memory/*.md
```

---

## Error Handling

### Scenarios

1. **File doesn't exist**:
   - `ReadMemoryAsync()` → returns empty string
   - Not an error, just means no memory yet

2. **File is corrupted**:
   - Read returns whatever content exists
   - LLM deals with malformed markdown gracefully

3. **Disk full / Permission denied**:
   - `UpdateMemoryAsync()` → returns `MemoryUpdateResult.Failure`
   - Error logged to transparency system
   - User sees error message

4. **Content exceeds max size**:
   - `UpdateMemoryAsync()` → returns `MemoryUpdateResult.Failure`
   - Error: "Memory content exceeds maximum size of 10,000 characters"
   - LLM must condense/summarize and retry

5. **Timeout on update prompt**:
   - Conversation end prompt times out after 30s
   - Logged as warning, not an error
   - Memory not updated (user can manually prompt later)

### Logging

All operations logged to `ILogger` and `TransparencyService`:
- `LogInformation`: Successful read/update
- `LogWarning`: Size limit exceeded, timeout
- `LogError`: File I/O errors, unexpected exceptions

---

## Testing Strategy

### Unit Tests

1. **LongTermMemoryService**:
   - Read non-existent file → empty string
   - Write and read back → content matches
   - Overwrite existing file → old content replaced
   - Size limit enforcement → error when exceeded
   - Mode-aware file selection → correct files used

2. **LongTermMemoryToolExecutor**:
   - Execute read tool → calls service correctly
   - Execute update tool → validates arguments, calls service
   - Invalid arguments → returns failure result
   - Size validation → rejects oversized content

3. **BuiltInLongTermMemoryToolRegistry**:
   - GetAllTools() → returns 2 tools
   - GetTool("long_term_memory_read") → returns correct tool
   - HasTool() → returns true for valid tools

### Integration Tests

1. **End-to-End Memory Flow**:
   - Enable memory → start conversation → check memory loaded
   - Update memory via tool → verify file updated
   - Switch modes → verify separate files used
   - End conversation → verify update prompt sent

2. **UI Integration**:
   - Toggle checkbox → memory enabled/disabled
   - View memory → displays current content
   - Clear memory → file content cleared

### Manual Testing Scenarios

1. **First-time user**:
   - Enable memory, have conversation, verify memory created
   - Start new conversation, verify memory loaded

2. **Privacy validation**:
   - Try to store email → verify LLM self-censors (via guardrails)
   - View memory → verify only appropriate info stored

3. **Mode isolation**:
   - Store different info in Normal vs Teaching mode
   - Switch modes, verify memories are separate

---

## Configuration

### appsettings.json

```json
{
  "LongTermMemory": {
    "Enabled": false,
    "StorageDirectory": "data/memory",
    "MaxCharacters": 10000,
    "AutoLoadOnStart": true,
    "PromptUpdateOnEnd": true,
    "UpdatePromptTimeoutSeconds": 30
  }
}
```

### User Preferences (Future)

Store per-user memory preference in browser localStorage:
```json
{
  "memoryEnabled": true
}
```

This persists across browser sessions.

---

## Future Enhancements (Out of Scope for v1.0)

### Phase 2 Enhancements

1. **Semantic Memory Search**:
   - Vector embeddings of memory content
   - Query memory by semantic similarity
   - "What do you remember about my preferences for testing?"

2. **Structured Memory Sections**:
   - Dedicated sections: Profile, Preferences, Context, Notes
   - Tools to update specific sections without full overwrite
   - JSON schema validation

3. **Memory Versioning**:
   - Keep history of memory changes
   - Rollback to previous version
   - Diff view showing what changed

4. **Intelligent Memory Pruning**:
   - Auto-summarize when approaching size limit
   - Mark sections as "ephemeral" vs "permanent"
   - LLM decides what to keep vs discard

5. **Multi-User Support**:
   - User authentication
   - Per-user memory files
   - Shared vs personal memory spaces

6. **Privacy Enhancements**:
   - Content validation (detect PII automatically)
   - Encryption at rest
   - Compliance with GDPR/privacy regulations

### Phase 3 Enhancements

1. **Cross-Mode Memory**:
   - Shared memory accessible from both modes
   - Mode-specific overlays on top of shared base

2. **Memory Export/Import**:
   - Export memory as JSON/markdown
   - Import from previous sessions or other tools

3. **Memory Analytics**:
   - Visualize memory growth over time
   - Most frequently referenced topics
   - Memory health score

---

## Open Questions for Implementation Plan

These will be resolved in the implementation plan:

1. **UI Placement**: Where exactly should the checkbox go? Above input? In header? Settings page?

2. **Modal Design**: How should the "View Memory" modal look? Markdown renderer? Raw text? Editable?

3. **Startup Behavior**: Should memory load on app startup or only when user sends first message?

4. **Error Messaging**: What should user see if memory update fails? Toast? Alert? Inline message?

5. **File Creation**: Should empty template be created on first enable? Or wait for first update?

6. **Conversation End Detection**: What triggers "end conversation"?
   - User clicks "New Conversation" button?
   - User closes browser tab?
   - User switches modes?
   - All of the above?

7. **Testing Data**: Should we include sample memory files in the repo for testing?

---

## Success Criteria

The feature is successful if:

1. ✅ User can enable/disable memory via checkbox
2. ✅ Memory auto-loads at conversation start
3. ✅ LLM can read memory via tool
4. ✅ LLM can update memory via tool
5. ✅ Updates respect size limits
6. ✅ Separate memories for Normal/Teaching modes
7. ✅ All operations logged transparently
8. ✅ User can view and clear memory
9. ✅ Memory persists across app restarts
10. ✅ Privacy guardrails prevent sensitive data storage

---

## Next Steps

1. **Review Design**: Discuss with user, gather feedback
2. **Resolve Open Questions**: Decide on UI placement, error handling, etc.
3. **Create Implementation Plan**: Break down into tasks, TDD approach
4. **Implement Core Components**: Service → Tools → Executor → Registry
5. **UI Integration**: Checkbox, modal, event handling
6. **Testing**: Unit → Integration → Manual
7. **Documentation**: Update component docs, guides

---

## Design Decisions (Resolved)

### 1. UI Placement
**Decision**: Checkbox goes below the conversation selector
- Consistent location with related conversation controls
- Always visible and accessible
- Grouped with conversation management UI

### 2. Conversation End Trigger
**Decision**: Trigger memory update prompt on:
- ✅ User clicks "End Conversation" button (renamed from "Clear Conversation")
- ✅ User switches modes
- ❌ NOT on browser close (treat as hard termination)

**Rationale**:
- Rename "Clear Conversation" to "End Conversation" for clearer UX
- Browser close would require blocking tab close (poor UX)
- User closing browser = hard termination, no memory update needed

### 3. View Memory UI
**Decision**: Create new overlay/modal for memory viewer
- Markdown rendered display
- Editable (allow manual editing)
- Separate from config overlay (config is already crowded)

**Note**: User can always edit markdown files directly in `./data/memory/` folder

### 4. Integration Scope
**Decision**: Full implementation for v1.0
- Core tools + service ✅
- UI checkbox + auto-load/save ✅
- View/edit/clear features ✅
- Everything in design except "Future Enhancements" section

### 5. Privacy Validation
**Decision**: Rely on LLM guardrails for v1.0
- No automatic content scanning initially
- Tool description provides clear guardrails
- Size limit enforcement (10K chars)
- Can add validation in future if needed

### 6. Memory Read Tool - Design Discussion

**Question Raised**: Do we need `long_term_memory_read` if memory is auto-injected as system message?

**Analysis**:
- Memory is injected at conversation start (in system message)
- When LLM updates memory, it gets the new content back in tool result
- For Anthropic: system messages re-sent each turn, so updated memory available
- For OpenAI: we can append updated memory as system message after tool result
- **Conclusion**: LLM already has memory in context, read tool might be redundant

**Proposed Decision**: Keep the read tool for v1.0, but mark as optional/edge-case
- **Use cases**:
  - Very long conversations where system message is far back in context window
  - Explicit "refresh" if LLM wants to double-check stored memory
  - Debugging/transparency (visible when LLM checks memory)
- **Benefits**:
  - Explicit control for LLM
  - Doesn't hurt to have it
  - Can remove in future if proves unnecessary
- **Alternative**: Could remove entirely and rely only on system message injection

**Status**: Awaiting final decision from user

### 7. Initial Memory Template
**Decision**: TBD in implementation plan
- Defer to implementation phase
- Likely: empty file until first update (simpler)

---

## Critical Design Review

**⚠️ IMPORTANT**: A comprehensive critical review has identified several issues that must be resolved before implementation.

**See**: [LONG_TERM_MEMORY_DESIGN_REVIEW.md](./LONG_TERM_MEMORY_DESIGN_REVIEW.md)

### Critical Issues Summary

1. **🚨 CRITICAL**: AppModeService layering violation - interface must move to Domain layer
2. **🚨 CRITICAL**: System message injection point unclear - need explicit specification
3. **🚨 CRITICAL**: Mode switch event handling not specified

### High Priority Issues

4. Checkbox state persistence missing (localStorage)
5. Error feedback UX not specified
6. Memory update prompt behavior underspecified

### Action Required

Review the detailed analysis in `LONG_TERM_MEMORY_DESIGN_REVIEW.md` and make decisions on critical issues before proceeding with implementation.
