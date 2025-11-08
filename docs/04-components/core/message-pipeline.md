# Message Pipeline

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 3
**Layer**: Application

---

## Document Scope

**What belongs in this document**:
- MessagePipeline implementation
- Domain message to LLM message conversion
- Message format transformations

**What does NOT belong here**:
- ❌ Domain message models → See Domain/Models
- ❌ LLM models → See Domain/LLM
- ❌ Provider-specific conversions → See [../llm/](../llm/)

---

## Overview

MessagePipeline transforms between domain message models (IMessage) and LLM message format (LLMMessage) used by providers.

## Purpose

- **Convert** domain messages to LLM format
- **Handle** different message types (user, assistant, system, tool results)
- **Standardize** message format for LLM providers

## Implementation

**File**: `TransparentAiAgentCore/Application/Pipeline/MessagePipeline.cs`

**Key Methods**:
- `ConvertToLLMMessages(IMessage[])` - Domain to LLM format

## Conversion Logic

**Mapping**:
- `UserMessage` → LLMMessage(role: "user")
- `AssistantMessage` → LLMMessage(role: "assistant")
- `SystemMessage` → LLMMessage(role: "system")
- `AssistantToolCallMessage` → LLMMessage with tool calls
- `ToolResultMessage` → LLMMessage(role: "tool")

## Usage

Used by AgentOrchestrator to prepare messages for LLM requests.

## Related Documentation

- [Agent Orchestrator](agent-orchestrator.md) - Main consumer
- [LLM Provider Abstraction](../llm/provider-abstraction.md) - Consumes LLMMessage

---

**See Also**: [Core Components Overview](README.md)
