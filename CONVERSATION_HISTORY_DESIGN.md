# Conversation History Design Document

**Status**: Draft - Brainstorming Phase
**Created**: 2025-11-14
**Last Updated**: 2025-11-14

---

## ⚠️ IMPORTANT: Crash-Resistant Design Process

This document is designed to survive session crashes. All design discussions, questions, ideas, and decisions should be recorded here to maintain continuity across sessions.

**Working Process**:
1. All design ideas go into this document first
2. Commit and push changes frequently
3. Questions and clarifications are tracked in dedicated sections below
4. Implementation will be planned AFTER design is finalized

---

## 1. Overview

We want to add **conversation history** functionality to TransparentAiAgent, allowing users to:
- Save conversations for later reference
- Load previous conversations to continue discussions
- Switch between different conversations
- View a list of saved conversations

**Current State**:
- ConversationManager manages in-memory conversation (List<IMessage>)
- MessageSerializer already handles serialization/deserialization of IMessage instances
- Messages use MessageTypeDiscriminator for polymorphic serialization
- No persistence layer exists yet

**Goal**:
Design a conversation history system that integrates cleanly with existing architecture.

---

## 2. Design Considerations

### 2.1 Storage Format
- **Format**: JSON (leveraging existing MessageSerializer)
- **Location**: `./conversations/` (application directory) ✅ DECIDED
- **Structure**: Each conversation file contains:
  - Conversation metadata (ID, name, created date, last modified date)
  - Array of serialized IMessage objects
  - ConversationId for tracking
  - Configuration snapshot (model, endpoint, system message, context window size)

### 2.2 Conversation Naming
- **Initial Approach**: Use the start of the first user message as conversation name
- **Fallback**: "Conversation {timestamp}" if no user message exists
- **Future**: Allow user to rename conversations (later enhancement)

### 2.3 UI Integration
- **Location**: Dropdown in chat header ✅ DECIDED
- **Features**:
  - Dropdown with conversation list
  - "New Conversation" button
  - Load conversation action
  - Current conversation indicator
  - Auto-save after every message ✅ DECIDED

### 2.4 Architecture Integration

**Domain Layer**:
- Conversation entity (metadata + message collection + config snapshot)
- IConversationRepository interface

**Infrastructure Layer**:
- JsonConversationRepository (file-based storage)
- Uses existing MessageSerializer

**Application Layer**:
- ConversationHistoryManager service (separate from ConversationManager)
- Coordinates persistence operations
- ConversationManager remains focused on in-memory operations ✅ DECIDED

**UI Layer**:
- Conversation selector component in chat header
- Integration with existing chat UI

---

## 3. Key Questions & Clarifications

### Q1: Storage Location ✅ DECIDED
**Question**: Where should conversation files be stored?
- ~~Option A: `~/.transparentai/conversations/` (user home directory)~~
- **Option B: `./conversations/` (application directory)** ✅ CHOSEN
- ~~Option C: Configurable path in appsettings.json~~

**Decision**: Application directory (Option B)
**Rationale**: Simpler, keeps everything together with the app

### Q2: UI Placement ✅ DECIDED
**Question**: Where exactly should the conversation selector be placed?
- ~~Option A: Top bar next to other controls~~
- ~~Option B: Left sidebar panel~~
- **Option C: Dropdown in chat header** ✅ CHOSEN

**Decision**: Chat header dropdown (Option C)
**Rationale**: Most intuitive location, easy to access without cluttering UI

### Q3: Conversation Lifecycle ✅ DECIDED
**Question**: When should conversations be auto-saved?
- **Option A: After every message** ✅ CHOSEN
- ~~Option B: On explicit "Save" action only~~
- ~~Option C: Periodic auto-save (e.g., every N messages)~~
- ~~Option D: On conversation switch/close~~

**Decision**: Auto-save after every message (Option A)
**Rationale**: Maximum safety, no risk of losing work, transparent to user

### Q4: ConversationManager Integration ✅ DECIDED
**Question**: How should ConversationManager relate to conversations?
- **Option C: Separate concerns completely** ✅ CHOSEN
  - ConversationHistoryManager handles loading/saving
  - ConversationManager handles in-memory operations
  - Clear separation of concerns

**Decision**: Option C - Complete separation
**Rationale**: Clean architecture, ConversationManager stays focused on its core responsibility

### Q5: Multiple Conversations in Memory ✅ DECIDED
**Question**: Can multiple conversations exist in memory simultaneously?
- **Option A: Only one active conversation at a time (simpler)** ✅ CHOSEN
- ~~Option B: Multiple conversations can be loaded (more complex, future tabs?)~~

**Decision**: Single active conversation (Option A)
**Rationale**: Simpler implementation, matches current UI model, sufficient for MVP

### Q6: System Messages & Context Window ✅ DECIDED
**Question**: How do we handle system prompts and context window settings per conversation?
- **Decision**: Store full lightweight configuration snapshot with each conversation:
  - Model name
  - Endpoint URL
  - System message/prompt
  - Context window size settings
  - Any other relevant LLM configuration

**Rationale**: More complex but provides complete conversation context restoration. Essential for reproducing exact conversation conditions.

---

## 4. Ideas & Design Notes

### 4.1 Conversation Metadata Structure (FINALIZED)
```json
{
  "conversationId": "guid",
  "name": "What is clean architecture?",
  "createdAt": "2025-11-14T10:30:00Z",
  "lastModifiedAt": "2025-11-14T11:45:00Z",
  "configuration": {
    // Agent Configuration
    "systemPrompt": "You are a helpful assistant...",
    "contextWindowSize": 50,
    "enableTools": true,
    "toolExecutionMode": "Sequential",

    // LLM Configuration
    "provider": "Anthropic",
    "temperature": 1.0,
    "topP": null,
    "maxTokens": 4096,

    // Provider-specific (Anthropic example)
    "anthropic": {
      "model": "claude-sonnet-4-5-20250929",
      "extendedThinking": {
        "enabled": false,
        "budgetTokens": 5000
      }
    },

    // Provider-specific (Azure OpenAI example - null if not using)
    "azureOpenAI": null
    // When used, would contain: endpoint, deploymentName, apiVersion, isReasoningModel, authenticationMode
  },
  "messages": [
    { /* serialized IMessage using MessageSerializer */ },
    { /* serialized IMessage using MessageSerializer */ }
  ]
}
```

**Security Note**: API keys and TenantId are deliberately excluded from snapshot. Users will need valid credentials in their current configuration when loading old conversations.

### 4.2 Potential Issues to Consider
- **Concurrent Access**: What if multiple instances try to modify same conversation file?
  - Current decision: Single active conversation, so less of a concern
  - Future: Consider file locking if needed
- **File Corruption**: How to handle corrupted JSON files?
  - Implement try-catch with user notification
  - Consider backup/recovery mechanism
- **Migration**: If message schema changes, how to migrate old conversations?
  - Plan for versioning in metadata
  - Migration utility for future schema changes
- **Performance**: Loading large conversations with 1000s of messages?
  - Start simple, optimize if needed
  - Consider pagination/lazy loading in future
- **Search**: Future feature - search across conversations?
  - Out of scope for MVP

### 4.3 Clean Architecture Alignment
- Domain defines Conversation entity and IConversationRepository interface
- Infrastructure implements JsonConversationRepository
- Application layer has ConversationHistoryManager orchestrating operations
- ConversationManager remains unchanged, focused on in-memory operations
- UI layer only calls ConversationHistoryManager, never repository directly

### 4.4 Auto-Save Implementation Strategy
With auto-save after every message:
- Hook into message pipeline after message is added
- Async save to avoid blocking UI
- Error handling for save failures (log, notify user)
- Consider debouncing if performance issues arise

### 4.5 Future Enhancements (Out of Scope for Initial Design)
- Conversation search/filter functionality
- Export conversation to markdown/PDF
- Conversation tags/categories
- Conversation sharing/import
- Cloud sync
- Conversation branching (save multiple variations)
- Conversation templates

---

## 5. Technical Investigation Results ✅

### 5.1 UI Structure Investigation - COMPLETED

**Chat Header Location** (`Home.razor:14-22`):
```razor
<div class="chat-header">
    <h1>Transparent AI Agent</h1>
    @if (AppModeService.CurrentMode == AppMode.Teaching)
    {
        <ScenarioIndicator />
    }
    <button @onclick="HandleClearClick" class="clear-button">Clear Conversation</button>
</div>
```

**Key Findings**:
- ✅ Perfect location identified for conversation dropdown
- ✅ Can add dropdown component between title and clear button
- ✅ Already has conditional rendering pattern (ScenarioIndicator)
- ✅ Clear button will become "New Conversation" button
- Component hierarchy: `Home.razor` → `MessageList.razor` → `ChatInput.razor`

**UI Service Flow** (`ConversationUIService.cs`):
- Line 90-238: `SendMessageStreamingAsync()` - main message flow
- Line 218: `RefreshMessages()` - syncs UI with ConversationManager after streaming
- Line 240-252: `ClearConversationAsync()` - clears conversation
- Events: `MessagesChanged`, `ProcessingStateChanged`, `StreamingMessageUpdated`

### 5.2 Configuration System Investigation - COMPLETED

**Configuration Hierarchy**:
```
AppConfiguration (root)
├── AgentConfiguration
│   ├── SystemPrompt: string
│   ├── ContextWindowSize: int
│   ├── EnableTools: bool
│   └── ToolExecutionMode: enum (Sequential/Parallel)
├── LLMConfiguration
│   ├── Provider: string ("Anthropic" or "AzureOpenAI")
│   ├── Temperature: double?
│   ├── TopP: double?
│   ├── MaxTokens: int
│   ├── AnthropicConfiguration?
│   │   ├── ApiKey: string (exclude from snapshot!)
│   │   ├── Model: string
│   │   └── ExtendedThinking?
│   │       ├── Enabled: bool
│   │       └── BudgetTokens: int
│   └── AzureOpenAIConfiguration?
│       ├── Endpoint: string
│       ├── AuthenticationMode: enum
│       ├── ApiKey: string? (exclude from snapshot!)
│       ├── DeploymentName: string
│       ├── ApiVersion: string
│       ├── TenantId: string?
│       └── IsReasoningModel: bool
└── MCPConfiguration (not relevant for conversation snapshot)
```

**Access Pattern**:
- Interface: `IConfigurationService.GetConfiguration()` returns `AppConfiguration`
- Location: Injected as dependency, available throughout application
- Thread-safe: Yes, service manages configuration state

**Configuration Snapshot Strategy**:
```csharp
// Capture these fields for conversation snapshot:
- Agent.SystemPrompt
- Agent.ContextWindowSize
- Agent.EnableTools
- Agent.ToolExecutionMode
- LLM.Provider
- LLM.Temperature
- LLM.TopP
- LLM.MaxTokens
- If Provider == "Anthropic":
  - Anthropic.Model
  - Anthropic.ExtendedThinking.Enabled
  - Anthropic.ExtendedThinking.BudgetTokens
- If Provider == "AzureOpenAI":
  - AzureOpenAI.Endpoint
  - AzureOpenAI.DeploymentName
  - AzureOpenAI.ApiVersion
  - AzureOpenAI.IsReasoningModel
  - AzureOpenAI.AuthenticationMode (enum value only)

// EXCLUDE from snapshot (security):
- AnthropicConfiguration.ApiKey
- AzureOpenAIConfiguration.ApiKey
- AzureOpenAIConfiguration.TenantId (potentially sensitive)
```

### 5.3 Auto-Save Hook Points Investigation - COMPLETED

**Identified Hook Points**:

1. **Primary Hook**: `ConversationUIService.SendMessageStreamingAsync()`
   - Line 218: After `RefreshMessages()` completes
   - Ensures UI and ConversationManager are synced
   - Perfect timing: after assistant response is complete

2. **Secondary Hook**: `ConversationUIService.SendMessageAsync()` (non-streaming)
   - Line 77: After `RefreshMessages()` completes
   - Handles non-streaming message flow

3. **Clear Hook**: `ConversationUIService.ClearConversationAsync()`
   - Line 240-252: When user clears conversation
   - Should trigger "New Conversation" creation

**Recommended Auto-Save Strategy**:
```csharp
// Add to ConversationUIService after RefreshMessages():
private async Task AutoSaveConversationAsync()
{
    try
    {
        var currentConfig = _configurationService.GetConfiguration();
        var messages = _conversationManager.GetAllMessages();
        var conversationId = _conversationManager.ConversationId;

        await _conversationHistoryManager.SaveCurrentConversationAsync(
            conversationId,
            messages,
            currentConfig
        );
    }
    catch (Exception ex)
    {
        // Log error, notify user
        _logger.LogError(ex, "Failed to auto-save conversation");
        // Consider retry mechanism or user notification
    }
}
```

**Integration Points**:
- Inject `IConversationHistoryManager` into `ConversationUIService`
- Inject `IConfigurationService` into `ConversationUIService` (for config snapshot)
- Call `AutoSaveConversationAsync()` after line 218 and line 77
- Make it fire-and-forget (don't block UI), but log failures

### 5.4 Existing Components Reviewed
- ✅ ConversationManager - Manages List<IMessage>, has context window logic
- ✅ MessageSerializer - Handles IMessage serialization/deserialization
- ✅ ConversationUIService - Main UI service, perfect location for auto-save hook
- ✅ IConfigurationService - Provides access to current AppConfiguration
- ✅ Home.razor - Chat UI with header, perfect location for dropdown
- ⬜ ISerializationService - May not need this, MessageSerializer is sufficient

### 5.5 Dependencies
- Existing: System.Text.Json (already used in MessageSerializer)
- Existing: File I/O (standard .NET)
- Existing: IConfigurationService (for config snapshot)
- New: None identified

---

## 6. Open Design Topics

### 6.1 Conversation Lifecycle State Machine
Need to define states:
- New (unsaved, no messages yet)
- Active (current conversation being used)
- Saved (persisted to disk, auto-saved after each message)
- Loaded (restored from disk)
- Modified (loaded conversation with new messages - auto-saved)

With auto-save, the Modified state essentially triggers immediate transition back to Saved.

### 6.2 Error Handling Strategy
- **Missing file**: Show error, offer to create new conversation
- **Corrupted file**: Show error, log details, offer to create new conversation
- **Save failure**: Log error, notify user, retry mechanism?
- **Load while unsaved changes**: Not applicable with auto-save

### 6.3 Testing Strategy
- Unit tests for ConversationRepository (save/load/list operations)
- Unit tests for ConversationHistoryManager
- Integration tests for complete save/load cycle with real files
- UI tests for conversation switching
- Test auto-save mechanism
- Test configuration snapshot capture and restoration

---

## 7. Next Steps

1. ✅ **Answer Questions #1-6** - COMPLETED
2. ✅ **Review existing UI structure** - COMPLETED (see Section 5.1)
3. ✅ **Finalize conversation metadata structure** - COMPLETED (see Section 4.1)
4. ✅ **Design configuration snapshot mechanism** - COMPLETED (see Section 5.2)
5. ✅ **Design auto-save hook** - COMPLETED (see Section 5.3)
6. ⬜ **Create detailed component design** - classes, interfaces, methods with full signatures
7. ⬜ **Design UI component** - ConversationSelector.razor with dropdown behavior
8. ⬜ **Design file naming strategy** - how to name conversation files on disk
9. ⬜ **Move to implementation planning** (separate document/phase)

---

## 8. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-11-14 | Use JSON for storage | Leverages existing MessageSerializer, human-readable, easy to debug |
| 2025-11-14 | Conversation name from first message | Simple, intuitive, good enough for MVP |
| 2025-11-14 | Store in `./conversations/` directory | Keeps everything with app, simpler than user home |
| 2025-11-14 | Chat header dropdown for UI | Most intuitive, easy access, clean UI |
| 2025-11-14 | Auto-save after every message | Maximum safety, no lost work, transparent |
| 2025-11-14 | Single active conversation | Simpler, matches current UI, sufficient for MVP |
| 2025-11-14 | Separate ConversationHistoryManager | Clean separation of concerns, ConversationManager stays focused |
| 2025-11-14 | Store full config snapshot | Complete context restoration, essential for reproducibility |
| 2025-11-14 | Terminology: "Conversation" over "Session" | More intuitive, better describes the feature |
| 2025-11-14 | UI placement: Chat header between title and clear button | Natural location, follows existing pattern (ScenarioIndicator) |
| 2025-11-14 | Auto-save hooks: After RefreshMessages() in ConversationUIService | Ensures UI and ConversationManager are synced before save |
| 2025-11-14 | Security: Exclude API keys and TenantId from snapshots | Prevents credential leakage, users must have valid creds when loading |
| 2025-11-14 | No new dependencies required | Can reuse MessageSerializer and existing infrastructure |
| 2025-11-14 | File naming: {conversationId}_{sanitized_name}.json | Unique + human-readable, best of both worlds |

---

## 9. Discussion Notes

_Add timestamped discussion points here as we iterate_

### 2025-11-14 - Initial Draft
- Created initial design document
- Identified key architectural layers needed
- Listed open questions for clarification
- Need user feedback on storage location, UI placement, and conversation lifecycle

### 2025-11-14 - Decisions Made (Post-Crash Recovery)
User provided answers to all key questions:
- **Q1**: Option B - Application directory (`./conversations/`)
- **Q2**: Chat header dropdown for UI placement
- **Q3**: Auto-save after every message
- **Q5**: Single active conversation at a time
- **Q6**: Store full lightweight config (model, endpoint, system message, window size)
- **Q4**: Clarified by other answers - separate ConversationHistoryManager from ConversationManager

**Terminology Change**: Agreed to use "conversation" instead of "session" throughout.
- More intuitive and descriptive
- Better matches user mental model
- Renamed document to CONVERSATION_HISTORY_DESIGN.md

**Next Focus**:
- Review UI structure to understand chat header implementation
- Understand how to capture current LLM configuration
- Design the auto-save hook integration point
- Detail out the component interfaces and classes

### 2025-11-14 - Technical Investigation Completed

**Investigation Results**:
- ✅ Identified exact UI placement: `Home.razor:14-22` chat header
- ✅ Mapped complete configuration hierarchy from AppConfiguration
- ✅ Identified auto-save hook points in ConversationUIService
- ✅ Finalized configuration snapshot strategy (with security exclusions)
- ✅ Updated conversation metadata structure to match actual config

**Key Findings**:
1. **UI Integration**: Chat header already exists with perfect structure for dropdown
2. **Configuration Access**: IConfigurationService provides thread-safe access to AppConfiguration
3. **Auto-Save Hooks**: Two primary hooks identified (streaming and non-streaming) at RefreshMessages()
4. **Security**: API keys and TenantId must be excluded from conversation snapshots
5. **Dependencies**: No new dependencies needed, can reuse existing MessageSerializer

**Remaining Design Tasks**:
1. Design detailed component interfaces and class signatures
2. Design ConversationSelector.razor UI component
3. Design file naming strategy for conversation files
4. Plan implementation phases

**Status**: Design is ~80% complete. Ready to move to detailed component design phase.

---

## 10. References

**Core Components**:
- `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs` - Current conversation management
- `TransparentAiAgentCore/Infrastructure/Serialization/MessageSerializer.cs` - Message serialization
- `TransparentAiAgentCore/Domain/Models/IMessage.cs` - Message interface

**UI Components**:
- `TransparentAiAgentGui/Components/Pages/Home.razor` - Main chat page with header (lines 14-22)
- `TransparentAiAgentGui/Services/ConversationUIService.cs` - UI service with auto-save hooks (lines 218, 77)

**Configuration System**:
- `TransparentAiAgentCore/Domain/Configuration/AppConfiguration.cs` - Root config
- `TransparentAiAgentCore/Domain/Configuration/AgentConfiguration.cs` - Agent settings
- `TransparentAiAgentCore/Domain/Configuration/LLMConfiguration.cs` - LLM settings
- `TransparentAiAgentCore/Domain/Configuration/AnthropicConfiguration.cs` - Anthropic provider config
- `TransparentAiAgentCore/Domain/Configuration/AzureOpenAIConfiguration.cs` - Azure OpenAI provider config
- `TransparentAiAgentCore/Infrastructure/Configuration/IConfigurationService.cs` - Config access interface

**Architecture**:
- `docs/02-architecture/overview.md` - Clean Architecture guidelines

---

## 11. Implementation Considerations (To Be Detailed Later)

### 11.1 Component Skeleton (Draft)

**Domain Layer**:
```csharp
// Conversation.cs - Entity
public class Conversation
{
    public Guid ConversationId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public ConversationConfiguration Configuration { get; set; }
    public List<IMessage> Messages { get; set; }
}

// ConversationConfiguration.cs - Value Object
public class ConversationConfiguration
{
    public string ModelName { get; set; }
    public string Endpoint { get; set; }
    public string SystemPrompt { get; set; }
    public int ContextWindowSize { get; set; }
    // ... other config properties
}

// IConversationRepository.cs - Repository Interface
public interface IConversationRepository
{
    Task<Conversation> LoadAsync(Guid conversationId);
    Task SaveAsync(Conversation conversation);
    Task<List<ConversationMetadata>> ListAllAsync();
    Task DeleteAsync(Guid conversationId);
}
```

**Infrastructure Layer**:
```csharp
// JsonConversationRepository.cs
public class JsonConversationRepository : IConversationRepository
{
    // Implements file-based storage in ./conversations/
    // Uses MessageSerializer for message serialization
}
```

**Application Layer**:
```csharp
// ConversationHistoryManager.cs
public class ConversationHistoryManager
{
    private readonly IConversationRepository _repository;

    public Task<Conversation> LoadConversationAsync(Guid id);
    public Task SaveConversationAsync(Conversation conversation);
    public Task<List<ConversationMetadata>> GetConversationListAsync();
    public Task CreateNewConversationAsync();
    // Auto-save hook integration
}
```

### 11.2 Auto-Save Integration Points (DETAILED)
**Implementation in ConversationUIService.cs**:
```csharp
// Add dependencies to constructor:
private readonly IConversationHistoryManager _conversationHistoryManager;
private readonly IConfigurationService _configurationService;

// Call after line 218 (streaming):
RefreshMessages();
_ = AutoSaveConversationAsync(); // Fire and forget

// Call after line 77 (non-streaming):
RefreshMessages();
_ = AutoSaveConversationAsync(); // Fire and forget

private async Task AutoSaveConversationAsync()
{
    try
    {
        var currentConfig = _configurationService.GetConfiguration();
        var messages = _conversationManager.GetAllMessages();
        var conversationId = _conversationManager.ConversationId;

        await _conversationHistoryManager.SaveCurrentConversationAsync(
            conversationId,
            messages,
            currentConfig
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to auto-save conversation {ConversationId}",
            _conversationManager.ConversationId);
        // Don't throw - auto-save failure shouldn't crash the app
        // Consider adding user notification in future
    }
}
```

### 11.3 Configuration Snapshot Strategy (IMPLEMENTED)
**Source**: `IConfigurationService.GetConfiguration()` returns `AppConfiguration`

**Snapshot Process**:
```csharp
public static ConversationConfiguration CreateSnapshot(AppConfiguration appConfig)
{
    var snapshot = new ConversationConfiguration
    {
        // Agent
        SystemPrompt = appConfig.Agent.SystemPrompt,
        ContextWindowSize = appConfig.Agent.ContextWindowSize,
        EnableTools = appConfig.Agent.EnableTools,
        ToolExecutionMode = appConfig.Agent.ToolExecutionMode.ToString(),

        // LLM
        Provider = appConfig.LLM.Provider,
        Temperature = appConfig.LLM.Temperature,
        TopP = appConfig.LLM.TopP,
        MaxTokens = appConfig.LLM.MaxTokens
    };

    // Provider-specific config (excluding API keys!)
    if (appConfig.LLM.Provider == "Anthropic" && appConfig.LLM.Anthropic != null)
    {
        snapshot.Anthropic = new AnthropicSnapshot
        {
            Model = appConfig.LLM.Anthropic.Model,
            ExtendedThinking = appConfig.LLM.Anthropic.ExtendedThinking != null
                ? new ExtendedThinkingSnapshot
                {
                    Enabled = appConfig.LLM.Anthropic.ExtendedThinking.Enabled,
                    BudgetTokens = appConfig.LLM.Anthropic.ExtendedThinking.BudgetTokens
                }
                : null
        };
    }
    else if (appConfig.LLM.Provider == "AzureOpenAI" && appConfig.LLM.AzureOpenAI != null)
    {
        snapshot.AzureOpenAI = new AzureOpenAISnapshot
        {
            Endpoint = appConfig.LLM.AzureOpenAI.Endpoint,
            DeploymentName = appConfig.LLM.AzureOpenAI.DeploymentName,
            ApiVersion = appConfig.LLM.AzureOpenAI.ApiVersion,
            IsReasoningModel = appConfig.LLM.AzureOpenAI.IsReasoningModel,
            AuthenticationMode = appConfig.LLM.AzureOpenAI.AuthenticationMode.ToString()
            // Deliberately exclude: ApiKey, TenantId
        };
    }

    return snapshot;
}
```

### 11.4 File Naming Strategy (TO BE DESIGNED)
Options to consider:
- Option A: `{conversationId}.json` - Simple, guaranteed unique
- Option B: `{timestamp}_{sanitized_name}.json` - Human-readable, risk of collisions
- Option C: `{conversationId}_{sanitized_name}.json` - Best of both worlds

**Recommendation**: Option C
- Allows easy identification in file browser
- Guaranteed unique via GUID
- Easy to implement

---

**End of Design Document**
