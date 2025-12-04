# LLM Provider Abstraction

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 2
**Layer**: Domain

---

## Document Scope

**What belongs in this document**:
- ILLMProvider interface definition and contract
- LLM domain models (LLMRequest, LLMResponse, LLMMessage, LLMTool, LLMToolCall, LLMUsage, StreamingLLMChunk)
- Provider abstraction purpose and design
- Integration contract for LLM implementations

**What does NOT belong here**:
- ❌ Specific provider implementations → See [anthropic-provider.md](anthropic-provider.md), [azure-openai-provider.md](azure-openai-provider.md)
- ❌ Provider factory logic → See Infrastructure layer
- ❌ Message pipeline transformations → See [../core/message-pipeline.md](../core/message-pipeline.md)
- ❌ Usage examples with specific providers → See provider-specific docs

---

## Overview

The LLM Provider Abstraction defines a provider-agnostic interface for interacting with Large Language Models. It enables the system to support multiple LLM providers (Anthropic, Azure OpenAI, etc.) through a unified contract.

## Purpose

- **Decouple** agent logic from specific LLM provider implementations
- **Enable** swapping LLM providers via configuration
- **Standardize** request/response formats across providers
- **Support** both streaming and non-streaming interactions
- **Abstract** provider-specific API details

## Responsibilities

- Define provider interface contract (ILLMProvider)
- Specify LLM request/response data models
- Enforce validation rules for LLM parameters
- Support tool calling abstraction
- Enable streaming response handling

## Architecture

### Core Interface

**File**: `TransparentAiAgentCore/Domain/LLM/ILLMProvider.cs:10`

```csharp
public interface ILLMProvider
{
    Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(LLMRequest request, CancellationToken cancellationToken = default);
    string ProviderName { get; }
}
```

### Domain Models

#### LLMRequest

**File**: `TransparentAiAgentCore/Domain/LLM/LLMRequest.cs:9`

Represents a request to an LLM provider.

**Properties**:
- `List<LLMMessage> Messages` - Conversation history
- `List<LLMTool>? Tools` - Available tools for LLM to call
- `double? Temperature` - Sampling temperature (0-2)
- `double? TopP` - Nucleus sampling (0-1)
- `int MaxTokens` - Maximum response tokens
- `bool Stream` - Enable streaming response

**Validation**:
- Messages cannot be null or empty
- Temperature: 0-2 range
- TopP: 0-1 range
- MaxTokens: > 0

#### LLMResponse

**File**: `TransparentAiAgentCore/Domain/LLM/LLMResponse.cs:9`

Represents a complete response from an LLM provider.

**Properties**:
- `string Content` - Response text content
- `List<LLMToolCall>? ToolCalls` - Tool calls requested by LLM
- `string? FinishReason` - Why generation stopped
- `LLMUsage? Usage` - Token usage statistics

**Validation**:
- Must have either content or tool calls (not both empty)

#### Other Models

- **LLMMessage** - Individual message in conversation (role + content)
- **LLMTool** - Tool definition for LLM (name, description, schema)
- **LLMToolCall** - Tool call from LLM (id, name, arguments)
- **LLMUsage** - Token usage tracking (input, output, total)
- **StreamingLLMChunk** - Partial response during streaming

### Dependencies

**Depends on**: None (pure domain models)

**Used by**:
- `Application/Agent/AgentOrchestrator.cs` - Main LLM consumer
- `Application/Pipeline/MessagePipeline.cs` - Converts domain messages to LLM format
- `Infrastructure/LLM/*Provider.cs` - Provider implementations
- `Infrastructure/LLM/LLMProviderFactory.cs` - Provider creation

## Implementation Notes

### Design Patterns

**Strategy Pattern**: ILLMProvider allows swapping provider implementations at runtime based on configuration.

**Immutability**: LLMRequest and LLMResponse are immutable after construction, ensuring thread-safety and preventing accidental mutations.

### Key Design Decisions

1. **Separate streaming method**: `StreamRequestAsync` returns `IAsyncEnumerable<StreamingLLMChunk>` to support real-time response handling
2. **Optional tool support**: Tools are optional to support both tool-enabled and simple chat scenarios
3. **Validation in constructors**: Domain models validate parameters at construction time, failing fast on invalid input
4. **Provider-agnostic**: Models use generic terms (not Anthropic/Azure-specific terminology)

### Important Considerations

- **Thread-safety**: All models are immutable after construction
- **Streaming vs non-streaming**: Providers must support both modes; streaming aggregates to same result as non-streaming
- **Tool call format**: Tool arguments are JSON strings, validated by provider implementations
- **Error handling**: Implementations should throw LLMException for provider-specific errors

## Configuration

No direct configuration. Providers are selected via `LLMConfiguration` in application config.

See: `docs/05-guides/installation/llm-provider-selector.md`

## Testing Strategy

### Unit Tests

Domain models have validation tests:
- `TransparentAiAgentCore_Tests/Domain/LLM/LLMRequestTests.cs` - Parameter validation
- `TransparentAiAgentCore_Tests/Domain/LLM/LLMResponseTests.cs` - Response validation
- `TransparentAiAgentCore_Tests/Domain/LLM/LLMMessageTests.cs` - Message model tests

### Integration Tests

Provider implementations test interface contract compliance in their respective test files.

## Usage Examples

### Basic Request (Non-Streaming)

```csharp
var request = new LLMRequest(
    messages: new List<LLMMessage>
    {
        new LLMMessage("user", "Hello, how are you?")
    },
    temperature: 0.7,
    maxTokens: 1000
);

LLMResponse response = await provider.SendRequestAsync(request);
Console.WriteLine(response.Content);
```

### Streaming Request

```csharp
var request = new LLMRequest(
    messages: messages,
    stream: true
);

await foreach (var chunk in provider.StreamRequestAsync(request))
{
    Console.Write(chunk.Content);
}
```

### Request with Tools

```csharp
var tools = new List<LLMTool>
{
    new LLMTool("get_weather", "Get weather for location", schemaJson)
};

var request = new LLMRequest(
    messages: messages,
    tools: tools
);

var response = await provider.SendRequestAsync(request);
if (response.ToolCalls != null)
{
    // Handle tool calls
}
```

## Related Documentation

- [Anthropic Provider](anthropic-provider.md) - Anthropic Claude implementation
- [Azure OpenAI Provider](azure-openai-provider.md) - Azure OpenAI implementation
- [Message Pipeline](../core/message-pipeline.md) - Domain-to-LLM message conversion
- [Agent Orchestrator](../core/agent-orchestrator.md) - Main LLM consumer

---

**See Also**: [LLM Components Overview](README.md)
