# Session History Design Document

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

We want to add **session history** functionality to TransparentAiAgent, allowing users to:
- Save conversations as "sessions"
- Load previous sessions to continue conversations
- Switch between different sessions
- View a list of saved sessions

**Current State**:
- ConversationManager manages in-memory conversation (List<IMessage>)
- MessageSerializer already handles serialization/deserialization of IMessage instances
- Messages use MessageTypeDiscriminator for polymorphic serialization
- No persistence layer exists yet

**Goal**:
Design a session history system that integrates cleanly with existing architecture.

---

## 2. Design Considerations

### 2.1 Storage Format
- **Format**: JSON (leveraging existing MessageSerializer)
- **Location**: TBD (see Questions #1)
- **Structure**: Each session file contains:
  - Session metadata (ID, name, created date, last modified date)
  - Array of serialized IMessage objects
  - ConversationId for tracking

### 2.2 Session Naming
- **Initial Approach**: Use the start of the first user message as session name
- **Fallback**: "Session {timestamp}" if no user message exists
- **Future**: Allow user to rename sessions (later enhancement)

### 2.3 UI Integration
- **Location**: Session selector somewhere at chat window (see Questions #2)
- **Features**:
  - Dropdown or sidebar with session list
  - "New Session" button
  - Load session action
  - Current session indicator

### 2.4 Architecture Integration

**Domain Layer**:
- Session entity (metadata + message collection)
- ISessionRepository interface

**Infrastructure Layer**:
- JsonSessionRepository (file-based storage)
- Uses existing MessageSerializer

**Application Layer**:
- SessionManager service
- Coordinates between ConversationManager and SessionRepository

**UI Layer**:
- Session selector component
- Integration with existing chat UI

---

## 3. Key Questions & Clarifications

### Q1: Storage Location
**Question**: Where should session files be stored?
- Option A: `~/.transparentai/sessions/` (user home directory)
- Option B: `./sessions/` (application directory)
- Option C: Configurable path in appsettings.json
- **Decision**: PENDING

### Q2: UI Placement
**Question**: Where exactly should the session selector be placed?
- Option A: Top bar next to other controls
- Option B: Left sidebar panel
- Option C: Dropdown in chat header
- **Decision**: PENDING

### Q3: Session Lifecycle
**Question**: When should sessions be auto-saved?
- Option A: After every message
- Option B: On explicit "Save" action only
- Option C: Periodic auto-save (e.g., every N messages)
- Option D: On session switch/close
- **Decision**: PENDING

### Q4: ConversationManager Integration
**Question**: How should ConversationManager relate to sessions?
- Option A: ConversationManager is session-agnostic; SessionManager wraps it
- Option B: ConversationManager has session awareness built-in
- Option C: Separate concerns completely - SessionManager loads/saves, ConversationManager operates
- **Decision**: PENDING - Option A or C seem cleanest for separation of concerns

### Q5: Multiple Sessions in Memory
**Question**: Can multiple sessions exist in memory simultaneously?
- Option A: Only one active session at a time (simpler)
- Option B: Multiple sessions can be loaded (more complex, future tabs?)
- **Decision**: PENDING - Suggest starting with Option A

### Q6: System Messages & Context Window
**Question**: How do we handle system prompts and context window settings per session?
- Do sessions store the system prompt used?
- Do sessions remember context window size?
- **Decision**: PENDING

---

## 4. Ideas & Design Notes

### 4.1 Session Metadata Structure (Draft)
```json
{
  "sessionId": "guid",
  "name": "What is clean architecture?",
  "createdAt": "2025-11-14T10:30:00Z",
  "lastModifiedAt": "2025-11-14T11:45:00Z",
  "conversationId": "guid",
  "contextWindowSize": 50,
  "systemPrompt": "You are a helpful assistant...",
  "messages": [
    { /* serialized IMessage */ },
    { /* serialized IMessage */ }
  ]
}
```

### 4.2 Potential Issues to Consider
- **Concurrent Access**: What if multiple instances try to modify same session file?
- **File Corruption**: How to handle corrupted JSON files?
- **Migration**: If message schema changes, how to migrate old sessions?
- **Performance**: Loading large sessions with 1000s of messages?
- **Search**: Future feature - search across sessions?

### 4.3 Clean Architecture Alignment
- Domain should define Session entity and ISessionRepository interface
- Infrastructure implements JsonSessionRepository
- Application layer has SessionManager orchestrating operations
- UI layer only calls SessionManager, never repository directly

### 4.4 Future Enhancements (Out of Scope for Initial Design)
- Session search/filter functionality
- Export session to markdown/PDF
- Session tags/categories
- Session sharing/import
- Cloud sync
- Session branching (save multiple variations)

---

## 5. Technical Investigation Needed

### 5.1 Existing Components to Review
- ✅ ConversationManager - Reviewed: manages List<IMessage>, has context window logic
- ✅ MessageSerializer - Reviewed: handles IMessage serialization/deserialization
- ⬜ ISerializationService - Need to check if we should use this instead
- ⬜ Current UI structure - Need to understand Blazor component hierarchy
- ⬜ Configuration system - Check if session path should be configurable

### 5.2 Dependencies
- Existing: System.Text.Json (already used in MessageSerializer)
- Existing: File I/O (standard .NET)
- New: None identified yet

---

## 6. Open Design Topics

### 6.1 Session Lifecycle State Machine
Need to define states:
- New (unsaved)
- Active (current session)
- Saved (persisted to disk)
- Loaded (restored from disk)
- Modified (loaded session with changes)

### 6.2 Error Handling Strategy
- What happens if session file is missing?
- What happens if session file is corrupted?
- What happens if user tries to load session while current has unsaved changes?

### 6.3 Testing Strategy
- Unit tests for SessionRepository
- Unit tests for SessionManager
- Integration tests for save/load cycle
- UI tests for session switching

---

## 7. Next Steps

1. **Answer Questions #1-6** (above) through discussion
2. **Review existing UI** structure to determine best placement
3. **Finalize session metadata** structure
4. **Review ISerializationService** to see if we should leverage it
5. **Create detailed component design** once questions are answered
6. **Move to implementation planning** (separate document/phase)

---

## 8. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-11-14 | Use JSON for storage | Leverages existing MessageSerializer, human-readable, easy to debug |
| 2025-11-14 | Session name from first message | Simple, intuitive, good enough for MVP |
| | | |

---

## 9. Discussion Notes

_Add timestamped discussion points here as we iterate_

### 2025-11-14 - Initial Draft
- Created initial design document
- Identified key architectural layers needed
- Listed open questions for clarification
- Need user feedback on storage location, UI placement, and session lifecycle

---

## 10. References

- `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs` - Current conversation management
- `TransparentAiAgentCore/Infrastructure/Serialization/MessageSerializer.cs` - Message serialization
- `TransparentAiAgentCore/Domain/Models/IMessage.cs` - Message interface
- `docs/02-architecture/overview.md` - Clean Architecture guidelines (need to review)
