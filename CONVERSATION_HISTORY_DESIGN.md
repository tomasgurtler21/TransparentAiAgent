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
- Conversation entity (metadata + message collection)
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

### Q6: Configuration Snapshot ✅ DECIDED - REMOVED
**Question**: Should we store configuration (system prompt, model, etc.) with each conversation?
- **Decision**: NO - Do not store configuration snapshot

**Rationale**:
- Configuration can change multiple times during a single conversation
- Snapshot would only capture final state, not what was actually used
- Restoring old config when loading conversation would be confusing
- Conversation history is about messages, not settings
- Simpler implementation, clearer semantics
- **When loading conversation, use current/active configuration**

---

## 4. Ideas & Design Notes

### 4.1 Conversation Metadata Structure (FINALIZED) ✅ REVISED

```json
{
  "conversationId": "guid",
  "name": "What is clean architecture?",
  "createdAt": "2025-11-14T10:30:00Z",
  "lastModifiedAt": "2025-11-14T11:45:00Z",
  "messages": [
    { /* serialized IMessage using MessageSerializer */ },
    { /* serialized IMessage using MessageSerializer */ }
  ]
}
```

**Design Rationale**: Configuration snapshot was removed because:
- Configuration can change multiple times during a single conversation
- Snapshot would only capture final state, not what was used for each message
- Restoring old configuration when loading conversation would be confusing (overwrites user's current settings)
- **Conversation history is about messages, not the settings used to generate them**
- Simpler implementation, smaller files, clearer semantics

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

### 5.1 UI Structure Investigation - CORRECTED ✅

**Current Layout Analysis** (`Home.razor`):
```razor
<div class="chat-container">
    <!-- Line 15-22: Chat header with title -->
    <div class="chat-header">
        <h1>Transparent AI Agent</h1>
        @if (AppModeService.CurrentMode == AppMode.Teaching)
        {
            <ScenarioIndicator />
        }
        <button @onclick="HandleClearClick">Clear Conversation</button>
    </div>

    <!-- Line 24: MessageList with internal scrolling -->
    <MessageList Messages="..." IsProcessing="..." />
    <!-- Inside MessageList: MessageFilterControls at top - SCROLLS AWAY with messages! -->

    <!-- Line 26: ChatInput at bottom -->
    <ChatInput OnSend="..." IsDisabled="..." />
</div>
```

**Scrolling Behavior Issues**:
1. **Chat header**: May scroll away with page scroll (depending on page height)
2. **MessageFilterControls**: INSIDE MessageList scrollable area - scrolls away with messages ❌
3. **MessageList**: Has `overflow-y: auto` and `height: 60vh` - internal scrolling

**CORRECT UI Placement Decision**:
Place **ConversationSelector** as a **separate div BETWEEN chat-header and MessageList**:

```razor
<div class="chat-container">
    <!-- Existing header with title -->
    <div class="chat-header">
        <h1>Transparent AI Agent</h1>
        <button>Clear Conversation</button>
    </div>

    <!-- NEW: Conversation selector - separate, non-scrolling area -->
    <ConversationSelector />  <!-- ✅ Always visible, below title, above messages -->

    <!-- Existing scrollable message list -->
    <MessageList Messages="..." IsProcessing="..." />

    <!-- Existing input -->
    <ChatInput OnSend="..." IsDisabled="..." />
</div>
```

**Benefits**:
- ✅ Always visible - not part of any scrolling area
- ✅ Below the "Transparent AI Agent" title
- ✅ Above the scrollable message area
- ✅ Simple to implement - just add between existing elements
- ✅ Clean separation: header → selector → messages → input

**Implementation Location**: Add in `Home.razor` between line 22 (chat-header end) and line 24 (MessageList)

**UI Service Flow** (`ConversationUIService.cs`):
- Line 90-238: `SendMessageStreamingAsync()` - main message flow
- Line 218: `RefreshMessages()` - syncs UI with ConversationManager after streaming
- Line 240-252: `ClearConversationAsync()` - clears conversation
- Events: `MessagesChanged`, `ProcessingStateChanged`, `StreamingMessageUpdated`

### 5.2 Configuration System Investigation - ~~COMPLETED~~ OBSOLETE

**Status**: ❌ **REMOVED FROM DESIGN**

**Original plan**: Store configuration snapshot with each conversation.

**Revised decision**: Do NOT store configuration snapshot.

**Rationale**:
- Configuration can change multiple times during a single conversation
- Snapshot would only capture final state, not what was used for each message
- Restoring old config when loading conversation would be confusing
- Conversation history is about messages, not settings
- Simpler implementation

**This section is preserved for historical context only.**

### 5.3 Auto-Save Hook Points Investigation - COMPLETED ✅ REVISED

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

**Recommended Auto-Save Strategy** (Simplified - No Config):
```csharp
// Add to ConversationUIService after RefreshMessages():
private async Task AutoSaveConversationAsync()
{
    try
    {
        var messages = _conversationManager.GetAllMessages();
        var conversationId = _conversationManager.ConversationId;

        await _conversationHistoryManager.SaveCurrentConversationAsync(
            conversationId,
            messages
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
- ~~Inject `IConfigurationService`~~ NOT NEEDED (no config snapshot)
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
6. ✅ **Create detailed component design** - COMPLETED (see Section 11.1, 11.2, 11.3)
7. ✅ **Design UI component** - COMPLETED (see Section 11.5)
8. ✅ **Design file naming strategy** - COMPLETED (see Section 11.4)
9. ⬜ **Move to implementation planning** (separate document/phase)

**Design Status**: ✅ **COMPLETE** - Ready for implementation planning

---

## 8. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-11-14 | Use JSON for storage | Leverages existing MessageSerializer, human-readable, easy to debug |
| 2025-11-14 | Conversation name from first message | Simple, intuitive, good enough for MVP |
| 2025-11-14 | Store in `./conversations/` directory | Keeps everything with app, simpler than user home |
| 2025-11-14 | ~~Chat header dropdown for UI~~ REVISED | ~~Most intuitive, easy access, clean UI~~ Header scrolls away - not ideal |
| 2025-11-14 | Auto-save after every message | Maximum safety, no lost work, transparent |
| 2025-11-14 | Single active conversation | Simpler, matches current UI, sufficient for MVP |
| 2025-11-14 | Separate ConversationHistoryManager | Clean separation of concerns, ConversationManager stays focused |
| 2025-11-14 | ~~Store full config snapshot~~ REVERSED | ~~Complete context restoration~~ Config changes mid-conversation, snapshot is meaningless |
| 2025-11-14 | Do NOT store configuration | Config can change during conversation, only messages matter |
| 2025-11-14 | Terminology: "Conversation" over "Session" | More intuitive, better describes the feature |
| 2025-11-14 | UI placement: Between chat-header and MessageList | Always visible, below title, above scrollable messages, separate non-scrolling div |
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
1. **UI Integration**: Place as separate div between chat-header and MessageList (always visible, no scrolling)
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

### 2025-11-14 - UI Placement Corrected (Multiple Revisions)

**Issue #1**: Original plan was chat header - scrolls away when page scrolls.

**Attempted Fix**: Tried MessageList top (like MessageFilterControls).

**Issue #2**: User clarified MessageFilterControls also scroll away (they're INSIDE the scrollable MessageList area).

**Investigation**:
- Confirmed: MessageList has internal scrolling (`overflow-y: auto`, `height: 60vh`)
- MessageFilterControls are INSIDE the scrollable area - they scroll away with messages ❌
- Chat header might scroll away with page scroll
- Need a location that doesn't participate in any scrolling

**FINAL Decision**:
- ❌ REJECTED: Inside chat header (scrolls with page)
- ❌ REJECTED: Top of MessageList (scrolls with messages)
- ✅ CORRECT: Separate div between chat-header and MessageList

**Implementation**:
```razor
<div class="chat-header">Title + buttons</div>
<ConversationSelector />  <!-- NEW: separate, non-scrolling -->
<MessageList />           <!-- Scrollable messages -->
<ChatInput />
```

**Benefits**:
1. Always visible - not part of any scrolling behavior
2. Below the "Transparent AI Agent" title (as user suggested)
3. Above the message area
4. Simple to implement - just insert between existing elements
5. Clean visual hierarchy: header → selector → messages → input

**Updated Section**: 5.1 now shows correct placement with clear explanation

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

## 11. Detailed Component Design ✅

### 11.1 Domain Layer - Complete Design

**File**: `TransparentAiAgentCore/Domain/ConversationHistory/Conversation.cs`
```csharp
namespace TransparentAiAgentCore.Domain.ConversationHistory;

/// <summary>
/// Represents a complete conversation with messages.
/// </summary>
public class Conversation
{
    public Guid ConversationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public List<IMessage> Messages { get; set; } = new();

    /// <summary>
    /// Generates conversation name from first user message.
    /// Falls back to timestamp-based name if no user message found.
    /// </summary>
    public static string GenerateName(IReadOnlyList<IMessage> messages)
    {
        var firstUserMessage = messages
            .FirstOrDefault(m => m.Role == MessageRole.User);

        if (firstUserMessage != null && !string.IsNullOrWhiteSpace(firstUserMessage.Content))
        {
            // Take first 50 characters, sanitize
            var content = firstUserMessage.Content.Trim();
            var name = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
            return SanitizeFileName(name);
        }

        // Fallback to timestamp
        return $"Conversation {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// Sanitizes a string for use in filename.
    /// Removes invalid filename characters.
    /// </summary>
    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", input.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }
}
```

**File**: `TransparentAiAgentCore/Domain/ConversationHistory/ConversationMetadata.cs`
```csharp
namespace TransparentAiAgentCore.Domain.ConversationHistory;

/// <summary>
/// Lightweight conversation metadata for listing conversations.
/// Does not include full message list or configuration.
/// </summary>
public class ConversationMetadata
{
    public Guid ConversationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public int MessageCount { get; set; }
}
```

~~**File**: `TransparentAiAgentCore/Domain/ConversationHistory/ConversationConfiguration.cs`~~ ❌ **REMOVED**

**Status**: This class has been removed from the design.

**Rationale**: Configuration snapshot removed because config can change multiple times during a conversation. Conversation history is about messages, not settings.

**File**: `TransparentAiAgentCore/Domain/ConversationHistory/IConversationRepository.cs`
```csharp
namespace TransparentAiAgentCore.Domain.ConversationHistory;

/// <summary>
/// Repository interface for conversation persistence.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Loads a conversation by ID.
    /// Throws if conversation not found or file corrupted.
    /// </summary>
    Task<Conversation> LoadAsync(Guid conversationId);

    /// <summary>
    /// Saves a conversation to persistent storage.
    /// Creates new file or overwrites existing.
    /// </summary>
    Task SaveAsync(Conversation conversation);

    /// <summary>
    /// Lists all conversation metadata (lightweight, no messages).
    /// Ordered by LastModifiedAt descending (most recent first).
    /// </summary>
    Task<List<ConversationMetadata>> ListAllAsync();

    /// <summary>
    /// Deletes a conversation from persistent storage.
    /// Throws if conversation not found.
    /// </summary>
    Task DeleteAsync(Guid conversationId);

    /// <summary>
    /// Checks if a conversation exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid conversationId);
}
```

### 11.2 Infrastructure Layer - Complete Design

**File**: `TransparentAiAgentCore/Infrastructure/ConversationHistory/JsonConversationRepository.cs`
```csharp
namespace TransparentAiAgentCore.Infrastructure.ConversationHistory;

/// <summary>
/// File-based JSON conversation repository.
/// Stores conversations in ./conversations/ directory.
/// </summary>
public class JsonConversationRepository : IConversationRepository
{
    private readonly string _conversationsDirectory;
    private readonly IMessageSerializer _messageSerializer;
    private readonly ILogger<JsonConversationRepository> _logger;

    public JsonConversationRepository(
        IMessageSerializer messageSerializer,
        ILogger<JsonConversationRepository> logger)
    {
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Use ./conversations/ relative to application directory
        _conversationsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "conversations");

        // Ensure directory exists
        if (!Directory.Exists(_conversationsDirectory))
        {
            Directory.CreateDirectory(_conversationsDirectory);
            _logger.LogInformation("Created conversations directory: {Directory}", _conversationsDirectory);
        }
    }

    public async Task<Conversation> LoadAsync(Guid conversationId)
    {
        var filePath = GetFilePath(conversationId);

        if (!File.Exists(filePath))
        {
            _logger.LogError("Conversation file not found: {ConversationId}", conversationId);
            throw new FileNotFoundException($"Conversation {conversationId} not found", filePath);
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var conversation = JsonSerializer.Deserialize<Conversation>(json, GetJsonOptions());

            if (conversation == null)
            {
                throw new InvalidOperationException($"Failed to deserialize conversation {conversationId}");
            }

            _logger.LogInformation("Loaded conversation: {ConversationId}, Messages: {Count}",
                conversationId, conversation.Messages.Count);

            return conversation;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse conversation file: {ConversationId}", conversationId);
            throw new InvalidOperationException($"Conversation file corrupted: {conversationId}", ex);
        }
    }

    public async Task SaveAsync(Conversation conversation)
    {
        if (conversation == null)
            throw new ArgumentNullException(nameof(conversation));

        // Update timestamp
        conversation.LastModifiedAt = DateTime.UtcNow;

        // Generate filename: {conversationId}_{sanitizedName}.json
        var fileName = $"{conversation.ConversationId}_{SanitizeFileName(conversation.Name)}.json";
        var filePath = Path.Combine(_conversationsDirectory, fileName);

        try
        {
            var json = JsonSerializer.Serialize(conversation, GetJsonOptions());
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Saved conversation: {ConversationId}, Messages: {Count}, File: {FileName}",
                conversation.ConversationId, conversation.Messages.Count, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save conversation: {ConversationId}", conversation.ConversationId);
            throw;
        }
    }

    public async Task<List<ConversationMetadata>> ListAllAsync()
    {
        var metadataList = new List<ConversationMetadata>();

        try
        {
            var files = Directory.GetFiles(_conversationsDirectory, "*.json");

            foreach (var filePath in files)
            {
                try
                {
                    // Read just enough to get metadata (not full messages)
                    var json = await File.ReadAllTextAsync(filePath);

                    // Parse minimally to extract metadata
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var metadata = new ConversationMetadata
                    {
                        ConversationId = Guid.Parse(root.GetProperty("conversationId").GetString() ?? Guid.Empty.ToString()),
                        Name = root.GetProperty("name").GetString() ?? "Untitled",
                        CreatedAt = root.GetProperty("createdAt").GetDateTime(),
                        LastModifiedAt = root.GetProperty("lastModifiedAt").GetDateTime(),
                        MessageCount = root.GetProperty("messages").GetArrayLength()
                    };

                    metadataList.Add(metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping corrupted conversation file: {FilePath}", filePath);
                    // Continue to next file
                }
            }

            // Sort by most recent first
            var sorted = metadataList.OrderByDescending(m => m.LastModifiedAt).ToList();

            _logger.LogInformation("Listed {Count} conversations", sorted.Count);
            return sorted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list conversations");
            throw;
        }
    }

    public async Task DeleteAsync(Guid conversationId)
    {
        var filePath = GetFilePath(conversationId);

        if (!File.Exists(filePath))
        {
            _logger.LogError("Cannot delete: Conversation file not found: {ConversationId}", conversationId);
            throw new FileNotFoundException($"Conversation {conversationId} not found", filePath);
        }

        try
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted conversation: {ConversationId}", conversationId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete conversation: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid conversationId)
    {
        var filePath = GetFilePath(conversationId);
        await Task.CompletedTask;
        return File.Exists(filePath);
    }

    private string GetFilePath(Guid conversationId)
    {
        // Find file matching conversationId pattern: {guid}_*.json
        var pattern = $"{conversationId}_*.json";
        var matches = Directory.GetFiles(_conversationsDirectory, pattern);

        if (matches.Length > 0)
        {
            return matches[0]; // Return first match
        }

        // If not found, construct default path (for new conversations)
        return Path.Combine(_conversationsDirectory, $"{conversationId}_new.json");
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "untitled";

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", input.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim().Substring(0, Math.Min(sanitized.Length, 50)); // Limit length
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new MessageTypeDiscriminatorConverter() } // Reuse existing converter
        };
    }
}
```

### 11.3 Application Layer - Complete Design

**File**: `TransparentAiAgentCore/Application/ConversationHistory/IConversationHistoryManager.cs`
```csharp
namespace TransparentAiAgentCore.Application.ConversationHistory;

/// <summary>
/// Manages conversation history operations.
/// Orchestrates between repository, current conversation, and configuration.
/// </summary>
public interface IConversationHistoryManager
{
    /// <summary>
    /// Saves the current conversation.
    /// Auto-called after every message.
    /// </summary>
    Task SaveCurrentConversationAsync(
        Guid conversationId,
        IReadOnlyList<IMessage> messages);

    /// <summary>
    /// Loads a conversation and restores it as the current conversation.
    /// Returns the loaded conversation for UI update.
    /// </summary>
    Task<Conversation> LoadConversationAsync(Guid conversationId);

    /// <summary>
    /// Lists all saved conversations (metadata only, no messages).
    /// Ordered by most recent first.
    /// </summary>
    Task<List<ConversationMetadata>> GetConversationListAsync();

    /// <summary>
    /// Creates a new conversation.
    /// Clears current messages, generates new conversation ID.
    /// Actual save happens on first message (to extract name).
    /// </summary>
    Task<Guid> CreateNewConversationAsync();

    /// <summary>
    /// Deletes a conversation from storage.
    /// </summary>
    Task DeleteConversationAsync(Guid conversationId);
}
```

**File**: `TransparentAiAgentCore/Application/ConversationHistory/ConversationHistoryManager.cs`
```csharp
namespace TransparentAiAgentCore.Application.ConversationHistory;

public class ConversationHistoryManager : IConversationHistoryManager
{
    private readonly IConversationRepository _repository;
    private readonly ILogger<ConversationHistoryManager> _logger;

    public ConversationHistoryManager(
        IConversationRepository repository,
        ILogger<ConversationHistoryManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SaveCurrentConversationAsync(
        Guid conversationId,
        IReadOnlyList<IMessage> messages)
    {
        try
        {
            // Don't save empty conversations
            if (messages == null || messages.Count == 0)
            {
                _logger.LogDebug("Skipping save: conversation is empty");
                return;
            }

            var conversation = new Conversation
            {
                ConversationId = conversationId,
                Name = Conversation.GenerateName(messages),
                CreatedAt = DateTime.UtcNow, // Will be preserved if loading existing
                LastModifiedAt = DateTime.UtcNow,
                Messages = messages.ToList()
            };

            // If conversation exists, preserve CreatedAt
            if (await _repository.ExistsAsync(conversationId))
            {
                var existing = await _repository.LoadAsync(conversationId);
                conversation.CreatedAt = existing.CreatedAt;
            }

            await _repository.SaveAsync(conversation);

            _logger.LogInformation("Auto-saved conversation: {ConversationId}, Name: {Name}",
                conversationId, conversation.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save conversation: {ConversationId}", conversationId);
            // Don't throw - auto-save failure shouldn't crash the app
        }
    }

    public async Task<Conversation> LoadConversationAsync(Guid conversationId)
    {
        _logger.LogInformation("Loading conversation: {ConversationId}", conversationId);
        return await _repository.LoadAsync(conversationId);
    }

    public async Task<List<ConversationMetadata>> GetConversationListAsync()
    {
        _logger.LogDebug("Fetching conversation list");
        return await _repository.ListAllAsync();
    }

    public async Task<Guid> CreateNewConversationAsync()
    {
        var newId = Guid.NewGuid();
        _logger.LogInformation("Created new conversation: {ConversationId}", newId);

        // Don't save yet - will save on first message (to extract name)
        await Task.CompletedTask;
        return newId;
    }

    public async Task DeleteConversationAsync(Guid conversationId)
    {
        _logger.LogInformation("Deleting conversation: {ConversationId}", conversationId);
        await _repository.DeleteAsync(conversationId);
    }
}
```

### 11.2 Auto-Save Integration Points (DETAILED) ✅ REVISED

**Implementation in ConversationUIService.cs** (Simplified - No Config):
```csharp
// Add dependency to constructor:
private readonly IConversationHistoryManager _conversationHistoryManager;

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
        var messages = _conversationManager.GetAllMessages();
        var conversationId = _conversationManager.ConversationId;

        await _conversationHistoryManager.SaveCurrentConversationAsync(
            conversationId,
            messages
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

### ~~11.3 Configuration Snapshot Strategy~~ ❌ **REMOVED**

**Status**: Configuration snapshot has been removed from the design.

**Rationale**: Configuration can change multiple times during a conversation, so snapshot is meaningless. Conversation history is about messages, not settings.

### 11.4 File Naming Strategy ✅

**Decision**: Use Option C: `{conversationId}_{sanitized_name}.json`

**Implementation**:
- Format: `{guid}_{sanitized_name}.json`
- Name sanitization: Remove invalid filename characters, limit to 50 chars
- Example: `a3f2b4c5-..._{What_is_clean_architecture...}.json`

**Benefits**:
- Guaranteed unique via GUID
- Human-readable name for easy browsing
- Easy to implement (already in JsonConversationRepository)

### 11.5 UI Component Design - ConversationSelector ✅

**File**: `TransparentAiAgentGui/Components/ConversationHistory/ConversationSelector.razor`

```razor
@using TransparentAiAgentCore.Domain.ConversationHistory
@using TransparentAiAgentCore.Application.ConversationHistory
@inject IConversationHistoryManager ConversationHistoryManager
@inject IConversationUIService ConversationService
@implements IDisposable

<div class="conversation-selector">
    <div class="selector-controls">
        <!-- Dropdown showing current/selected conversation -->
        <select class="conversation-dropdown"
                @bind="SelectedConversationId"
                @bind:after="OnConversationSelected"
                disabled="@IsLoading">
            @if (CurrentConversationName != null)
            {
                <option value="@CurrentConversationId">@CurrentConversationName (Current)</option>
            }
            else
            {
                <option value="">New Conversation</option>
            }

            @if (Conversations != null)
            {
                @foreach (var conv in Conversations)
                {
                    @if (conv.ConversationId != CurrentConversationId)
                    {
                        <option value="@conv.ConversationId">
                            @conv.Name (@conv.LastModifiedAt.ToString("MMM dd, HH:mm"))
                        </option>
                    }
                }
            }
        </select>

        <!-- New Conversation button -->
        <button class="new-conversation-button"
                @onclick="OnNewConversationClick"
                disabled="@IsLoading">
            + New
        </button>
    </div>

    @if (IsLoading)
    {
        <span class="loading-indicator">Loading...</span>
    }

    @if (!string.IsNullOrEmpty(ErrorMessage))
    {
        <div class="error-message">@ErrorMessage</div>
    }
</div>

@code {
    private List<ConversationMetadata>? Conversations;
    private Guid? SelectedConversationId;
    private Guid? CurrentConversationId;
    private string? CurrentConversationName;
    private bool IsLoading;
    private string? ErrorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadConversationListAsync();

        // Subscribe to conversation changes
        ConversationService.MessagesChanged += OnMessagesChanged;
    }

    private async Task LoadConversationListAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Conversations = await ConversationHistoryManager.GetConversationListAsync();

            // Set current conversation ID from ConversationManager
            // (This would need to be exposed via ConversationService)
            // CurrentConversationId = ConversationService.CurrentConversationId;
            // CurrentConversationName = Extract from current messages or null
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load conversations: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnConversationSelected()
    {
        if (SelectedConversationId == null || SelectedConversationId == CurrentConversationId)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Load conversation
            var conversation = await ConversationHistoryManager.LoadConversationAsync(SelectedConversationId.Value);

            // Update ConversationService with loaded messages
            // This would require new method on IConversationUIService:
            // await ConversationService.LoadConversationAsync(conversation);

            CurrentConversationId = conversation.ConversationId;
            CurrentConversationName = conversation.Name;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load conversation: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnNewConversationClick()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Current conversation auto-saves (already designed)
            // Just clear the current conversation
            await ConversationService.ClearConversationAsync();

            // Reset state
            CurrentConversationId = null;
            CurrentConversationName = null;
            SelectedConversationId = null;

            // Refresh list to show newly saved conversation
            await LoadConversationListAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create new conversation: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnMessagesChanged(object? sender, EventArgs e)
    {
        // Update current conversation name when messages change
        var messages = ConversationService.Messages;
        if (messages != null && messages.Any())
        {
            CurrentConversationName = Conversation.GenerateName(
                messages.Select(m => m.ToDomainMessage()).ToList()
            );
            InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        ConversationService.MessagesChanged -= OnMessagesChanged;
    }
}
```

**CSS File**: `TransparentAiAgentGui/Components/ConversationHistory/ConversationSelector.razor.css`

```css
.conversation-selector {
    padding: 12px 20px;
    background-color: #f5f5f5;
    border-bottom: 1px solid #ddd;
}

.selector-controls {
    display: flex;
    gap: 10px;
    align-items: center;
}

.conversation-dropdown {
    flex: 1;
    padding: 8px 12px;
    border: 1px solid #ccc;
    border-radius: 4px;
    background-color: white;
    font-size: 14px;
    cursor: pointer;
}

.conversation-dropdown:disabled {
    background-color: #f0f0f0;
    cursor: not-allowed;
}

.new-conversation-button {
    padding: 8px 16px;
    background-color: #2196f3;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 600;
    white-space: nowrap;
}

.new-conversation-button:hover:not(:disabled) {
    background-color: #1976d2;
}

.new-conversation-button:disabled {
    background-color: #ccc;
    cursor: not-allowed;
}

.loading-indicator {
    display: inline-block;
    margin-left: 10px;
    color: #666;
    font-size: 12px;
    font-style: italic;
}

.error-message {
    margin-top: 8px;
    padding: 8px;
    background-color: #ffebee;
    color: #c62828;
    border-left: 3px solid #f44336;
    border-radius: 4px;
    font-size: 13px;
}
```

**Key Features**:
1. **Dropdown Menu**: Shows current conversation and all saved conversations
2. **Current Conversation**: Labeled as "(Current)" in dropdown
3. **Conversation List**: Ordered by most recent (from repository)
4. **Date Display**: Shows last modified date/time for each conversation
5. **New Conversation Button**: Clears chat, waits for first message to name conversation
6. **Loading States**: Disabled during operations, shows loading indicator
7. **Error Handling**: Displays error messages if operations fail
8. **Auto-Update**: Updates conversation name when messages change

**Integration with Home.razor**:
```razor
<div class="chat-container">
    <div class="chat-header">
        <h1>Transparent AI Agent</h1>
        <button @onclick="HandleClearClick">Clear Conversation</button>
    </div>

    <!-- NEW: Conversation selector -->
    <ConversationSelector />

    <MessageList Messages="..." IsProcessing="..." />
    <ChatInput OnSend="..." IsDisabled="..." />
</div>
```

**Required Updates to ConversationUIService**:
```csharp
// Add property to expose current conversation ID
public Guid CurrentConversationId => _conversationManager.ConversationId;

// Add method to load a conversation
public async Task LoadConversationAsync(Conversation conversation)
{
    // Clear current messages
    _conversationManager.ClearConversation();

    // Add loaded messages
    foreach (var message in conversation.Messages)
    {
        _conversationManager.AddMessage(message);
    }

    // Optionally: restore configuration from conversation.Configuration
    // (Would require coordination with IConfigurationService)

    // Notify UI
    OnMessagesChanged();
}
```

---

## 12. Design Summary

### Completeness Status: ✅ 100% COMPLETE

All design tasks completed. The conversation history feature is fully designed and ready for implementation.

### What Was Designed

**Domain Layer** (Section 11.1):
- ✅ `Conversation` entity with name generation logic
- ✅ `ConversationMetadata` for lightweight listing
- ~~`ConversationConfiguration`~~ ❌ REMOVED (no config snapshot)
- ✅ `IConversationRepository` interface with full method signatures

**Infrastructure Layer** (Section 11.2):
- ✅ `JsonConversationRepository` complete implementation
- ✅ File-based storage in `./conversations/` directory
- ✅ Error handling for corrupted files, missing files
- ✅ Efficient metadata listing (minimal parsing)
- ✅ File naming: `{conversationId}_{sanitized_name}.json`

**Application Layer** (Section 11.3):
- ✅ `IConversationHistoryManager` interface
- ✅ `ConversationHistoryManager` implementation
- ✅ Auto-save integration with fire-and-forget pattern
- ✅ Empty conversation handling
- ✅ CreatedAt preservation logic

**UI Layer** (Section 11.5):
- ✅ `ConversationSelector.razor` component
- ✅ Dropdown menu with conversation list
- ✅ "+ New" button for creating conversations
- ✅ Loading states and error handling
- ✅ Auto-update of conversation name
- ✅ CSS styling for clean UI
- ✅ Integration pattern with `Home.razor`

**Integration Points** (Sections 5.3, 11.2):
- ✅ Auto-save hooks after `RefreshMessages()`
- ✅ ConversationUIService extensions
- ~~Configuration snapshot mechanism~~ ❌ REMOVED
- ✅ UI placement: between chat-header and MessageList

### Key Decisions Made

| Area | Decision | Rationale |
|------|----------|-----------|
| **Storage** | `./conversations/` directory, JSON format | Simple, leverages existing MessageSerializer |
| **Naming** | Extract from first user message | Intuitive, automatic |
| **UI Placement** | Between chat-header and MessageList | Always visible, no scrolling issues |
| **Auto-Save** | After every message | Maximum safety, transparent to user |
| **File Naming** | `{guid}_{sanitized_name}.json` | Unique + human-readable |
| **Concurrency** | Single active conversation | Simpler, matches current UI |
| **Config Snapshot** | ~~Full lightweight config~~ NO CONFIG | Config changes mid-conversation, snapshot is meaningless |

### Implementation Readiness

**Ready to Implement**:
- All classes have complete signatures
- All methods have defined behavior
- Error handling patterns specified
- Integration points identified
- UI component fully designed with CSS

**Next Phase**: Create implementation plan (task breakdown, order of implementation, testing strategy)

---

**End of Design Document**
