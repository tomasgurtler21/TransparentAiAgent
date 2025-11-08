# Transparency Service

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 3
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- TransparencyService implementation
- Event logging and retrieval
- In-memory event storage

**What does NOT belong here**:
- ❌ Transparency concept → See `docs/02-architecture/overview.md#transparency`
- ❌ UI transparency viewer → See `../ui/transparency-viewer.md`

---

## Overview

TransparencyService logs and stores transparency events (LLM requests/responses, tool calls, errors) for debugging and educational purposes.

## Purpose

- **Log** transparency events from all components
- **Store** events in-memory (chronological order)
- **Provide** event retrieval for UI
- **Fire** EventLogged event for real-time updates

## Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Transparency/TransparencyService.cs`

**Key Methods**:
- `LogEvent(TransparencyEvent)` - Add event
- `GetEvents()` - Retrieve all events
- Event: `EventLogged` - Real-time notification

## Event Types

- `LLMRequest` - Raw LLM requests
- `LLMResponse` - Raw LLM responses
- `ToolCall` - Tool execution start
- `ToolResult` - Tool execution result
- `Error` - Errors and exceptions
- `MessageParsing` - Message format conversions

## Usage

All major components log events:
- LLM providers (requests/responses)
- Tool manager (tool calls/results)
- Agent orchestrator (errors)

## Related Documentation

- [Transparency Viewer](../ui/transparency-viewer.md)
- [Architecture](../../02-architecture/overview.md#transparency)

---

**See Also**: [Infrastructure Overview](README.md)
