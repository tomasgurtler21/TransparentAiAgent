# Conversation Manager

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 3
**Layer**: Application

---

## Document Scope

**What belongs in this document**:
- ConversationManager implementation
- Conversation history management
- Context window handling
- Message addition and retrieval

**What does NOT belong here**:
- ❌ Message models → See Domain/Models
- ❌ Message transformation → See [message-pipeline.md](message-pipeline.md)
- ❌ Agent orchestration → See [agent-orchestrator.md](agent-orchestrator.md)

---

## Overview

ConversationManager maintains conversation history and manages the context window by tracking which messages are "in-context" (sent to LLM) versus archived.

## Purpose

- **Maintain** chronological conversation history
- **Manage** context window (in-context vs archived messages)
- **Provide** message addition and retrieval
- **Fire** events on context status changes

## Implementation

**File**: `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs`

**Key Methods**:
- `AddMessage(IMessage)` - Add to conversation
- `GetAllMessages()` - All messages (chronological)
- `GetInContextMessages()` - Only in-context messages
- Event: `ContextStatusChanged` - When messages archived

## Context Window Management

**Current**: All messages are in-context (no limit implemented yet).

**Future**: Will implement context summarization and message archiving based on token limits.

## Related Documentation

- [Agent Orchestrator](agent-orchestrator.md) - Main consumer
- [Message Pipeline](message-pipeline.md) - Message transformation

---

**See Also**: [Core Components Overview](README.md)
