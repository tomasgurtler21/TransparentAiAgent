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

### 4.1 Conversation Metadata Structure (UPDATED)
```json
{
  "conversationId": "guid",
  "name": "What is clean architecture?",
  "createdAt": "2025-11-14T10:30:00Z",
  "lastModifiedAt": "2025-11-14T11:45:00Z",
  "configuration": {
    "modelName": "claude-3-5-sonnet-20241022",
    "endpoint": "https://api.anthropic.com/v1/messages",
    "systemPrompt": "You are a helpful assistant...",
    "contextWindowSize": 50,
    "temperature": 1.0,
    "maxTokens": 4096
  },
  "messages": [
    { /* serialized IMessage */ },
    { /* serialized IMessage */ }
  ]
}
```

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

## 5. Technical Investigation Needed

### 5.1 Existing Components to Review
- ✅ ConversationManager - Reviewed: manages List<IMessage>, has context window logic
- ✅ MessageSerializer - Reviewed: handles IMessage serialization/deserialization
- ⬜ ISerializationService - Need to check if we should use this instead
- ⬜ Current UI structure - Need to understand Blazor component hierarchy for chat header
- ⬜ Configuration system - How to capture current LLM configuration snapshot
- ⬜ Chat UI components - Where exactly in header should dropdown go

### 5.2 Dependencies
- Existing: System.Text.Json (already used in MessageSerializer)
- Existing: File I/O (standard .NET)
- New: None identified yet

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
2. ⬜ **Review existing UI structure** to determine exact placement in chat header
3. ⬜ **Finalize conversation metadata structure** (draft above looks good, pending config details)
4. ⬜ **Review ISerializationService** to see if we should leverage it
5. ⬜ **Design configuration snapshot mechanism** - how to capture current LLM config
6. ⬜ **Create detailed component design** - classes, interfaces, methods
7. ⬜ **Design auto-save hook** - where in message pipeline to trigger save
8. ⬜ **Move to implementation planning** (separate document/phase)

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

---

## 10. References

- `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs` - Current conversation management
- `TransparentAiAgentCore/Infrastructure/Serialization/MessageSerializer.cs` - Message serialization
- `TransparentAiAgentCore/Domain/Models/IMessage.cs` - Message interface
- `docs/02-architecture/overview.md` - Clean Architecture guidelines
- `TransparentAiAgentGui/` - Blazor UI components (need to review for chat header)

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

### 11.2 Auto-Save Integration Points
- Hook after ConversationManager.AddMessageAsync
- Hook after message processing complete
- Async, non-blocking
- Error handling with user notification

### 11.3 Configuration Snapshot Strategy
Need to determine:
- Where is current LLM configuration stored?
- How to access it for snapshot?
- Which configuration properties to include?

_These details will be fleshed out in next phase_

---

**End of Design Document**
