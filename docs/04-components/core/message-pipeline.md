# Message Pipeline

**Last Updated**: 2025-11-14
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

**Four-Tier Message Hierarchy** (as of Phase 6.2):

The pipeline converts all message types from the four-tier hierarchy:

### User-Originated Messages → "user" role
- `DirectUserMessage` → LLMMessage(role: "user")
- `ScenarioUserMessage` → LLMMessage(role: "user") - **Note**: Annotation stripped during conversion

### LLM-Originated Messages → "assistant" role
- `LlmTextMessage` → LLMMessage(role: "assistant")
- `LlmToolCallMessage` → LLMMessage(role: "assistant") with tool calls array

### Application-Originated Messages → varies by subtype
- `ScenarioUserMessage` → LLMMessage(role: "user") - **Note**: Annotation stripped
- `ScenarioAssistantMessage` → LLMMessage(role: "assistant") - **Note**: Annotation stripped

### Tool-Originated Messages → "tool" role
- `ToolResultMessage` → LLMMessage(role: "tool") with tool_call_id
- `ToolErrorMessage` → LLMMessage(role: "tool") with tool_call_id

### Other Messages
- `SystemMessage` → LLMMessage(role: "system")

## Usage

Used by AgentOrchestrator to prepare messages for LLM requests.

## Related Documentation

- [Agent Orchestrator](agent-orchestrator.md) - Main consumer
- [LLM Provider Abstraction](../llm/provider-abstraction.md) - Consumes LLMMessage

---

**See Also**: [Core Components Overview](README.md)
