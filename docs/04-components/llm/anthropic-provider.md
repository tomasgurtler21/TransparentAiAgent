# Anthropic Provider

**Last Updated**: 2025-11-21
**Status**: Active
**Phase**: Phase 8
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- AnthropicProvider implementation details
- Anthropic Claude API integration
- Anthropic-specific streaming and tool calling
- Authentication and model configuration
- Transparency event logging for Anthropic requests

**What does NOT belong here**:
- ❌ Provider abstraction (ILLMProvider interface) → See [provider-abstraction.md](provider-abstraction.md)
- ❌ Azure OpenAI implementation → See [azure-openai-provider.md](azure-openai-provider.md)
- ❌ Message pipeline logic → See [../core/message-pipeline.md](../core/message-pipeline.md)
- ❌ Anthropic API deep dive → See `docs/06-reference/providers/anthropic/`

---

## Overview

AnthropicProvider implements ILLMProvider for Anthropic's Claude API. It handles authentication, request building, streaming responses, tool calling, and transparency logging for all Anthropic interactions.

## Purpose

- **Integrate** with Anthropic Claude API using official SDK
- **Translate** domain LLM models to Anthropic API format
- **Support** streaming and non-streaming responses
- **Handle** tool calling (function calling) with Claude
- **Log** all requests/responses to transparency service
- **Manage** API authentication and model selection

## Responsibilities

- Initialize Anthropic SDK client with API credentials
- Convert LLMRequest to Anthropic MessageCreateParams
- Convert Anthropic responses to LLMResponse
- Handle streaming with proper tool call accumulation
- Log raw requests/responses for transparency
- Handle errors and throw LLMException on failures

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/LLM/AnthropicProvider.cs:25`

```csharp
public class AnthropicProvider : ILLMProvider
{
    private readonly AnthropicClient _client;
    private readonly string _modelName;
    private readonly ITransparencyService _transparencyService;
    private readonly AppConfiguration _appConfig;

    public string ProviderName => "Anthropic";
}
```

### Dependencies

**Depends on**:
- `Domain/LLM/ILLMProvider` - Interface contract
- `Domain/Authentication/IAuthenticationProvider` - API key retrieval
- `Domain/Configuration/AppConfiguration` - Application config
- `Infrastructure/Transparency/ITransparencyService` - Event logging
- `Anthropic.Client` NuGet package - Official Anthropic SDK

**Used by**:
- `Infrastructure/LLM/LLMProviderFactory.cs` - Provider instantiation
- `Application/Agent/AgentOrchestrator.cs` - Via ILLMProvider interface

### Key Components

#### Constructor

**Signature**: `AnthropicProvider.cs:41`

```csharp
public AnthropicProvider(
    IAuthenticationProvider authProvider,
    string modelName,
    ITransparencyService transparencyService,
    AppConfiguration appConfig)
```

**Initialization**:
1. Validates all parameters (fail-fast)
2. Retrieves API key from auth provider
3. Initializes Anthropic SDK client
4. Stores model name and dependencies

#### Non-Streaming Request

**Method**: `SendRequestAsync` - `AnthropicProvider.cs:76`

**Flow**:
1. Build Anthropic MessageCreateParams from LLMRequest
2. Generate correlation ID for request tracking
3. Log raw request to transparency service
4. Call `_client.Messages.Create()`
5. Convert Anthropic response to LLMResponse
6. Log raw response with latency
7. Return LLMResponse

#### Streaming Request

**Method**: `StreamRequestAsync` - `AnthropicProvider.cs:120`

**Flow**:
1. Build request parameters
2. Log raw request
3. Call `_client.Messages.CreateStreaming()`
4. Accumulate text and tool call chunks
5. Yield StreamingLLMChunk for each delta
6. Log complete response after streaming finishes

**Accumulators**:
- `textAccumulators` - Text content per block index
- `toolCallInfo` - Tool call ID and name per index
- `jsonAccumulators` - Tool argument JSON per index

## Implementation Notes

### Design Patterns

**Adapter Pattern**: Converts domain LLM models to Anthropic SDK format and vice versa.

**Dependency Injection**: Receives all dependencies via constructor (testable, configurable).

### Key Algorithms

#### Tool Call Streaming Accumulation

Anthropic streams tool calls in chunks across multiple events:

1. `content_block_start` - Indicates tool use block with ID and name
2. `content_block_delta` - Streams partial JSON arguments
3. `content_block_stop` - Signals end of tool call block

Implementation accumulates these into complete tool calls before yielding final chunks.

### Anthropic-Specific Considerations

1. **System messages**: Anthropic requires system messages in separate `system` parameter (not in messages array)
2. **Tool input format**: Uses `input_json` field (different from OpenAI's `arguments`)
3. **Streaming events**: Anthropic uses specific event types (`content_block_start`, `content_block_delta`, etc.)
4. **Model naming**: Uses format `claude-3-5-sonnet-20241022` (not versioned like GPT-4)

### Error Handling

- Wraps all exceptions in `LLMException` with original exception as inner
- Logs errors to transparency service before throwing
- Validates inputs at constructor and method entry points

### Transparency Logging

Logs comprehensive event data:
- **Raw requests**: Full MessageCreateParams JSON
- **Raw responses**: Complete response with metadata
- **Latency**: Calculated request-to-response time
- **Correlation ID**: Links requests to responses
- **Errors**: Exception details

**See**: `docs/06-reference/providers/anthropic/` for detailed API information

## Configuration

### Required Configuration

**Basic Configuration (in appsettings.json)**:

```json
{
  "LLM": {
    "Provider": "Anthropic",
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "ModelName": "claude-3-5-sonnet-20241022"
    }
  }
}
```

### Optional Configuration

**With Custom Endpoint (for proxies or alternative endpoints)**:

```json
{
  "LLM": {
    "Provider": "Anthropic",
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "ModelName": "claude-3-5-sonnet-20241022",
      "Endpoint": "https://custom-anthropic-proxy.example.com"
    }
  }
}
```

**Configuration Fields**:
- `ApiKey` (required): Anthropic API key
- `ModelName` (required): Claude model identifier
- `Endpoint` (optional): Custom endpoint URL. Defaults to `https://api.anthropic.com` if not specified. Useful for:
  - Anthropic proxies or API gateways
  - Regional endpoints
  - Development/testing environments
  - Corporate proxy servers

### Supported Models

- `claude-3-5-sonnet-20241022` - Latest Sonnet (recommended)
- `claude-3-opus-20240229` - Opus (most capable)
- `claude-3-haiku-20240307` - Haiku (fastest)

**See**: `docs/06-reference/providers/anthropic/05_HAIKU_VS_SONNET_COMPARISON.md`

## Testing Strategy

### Unit Tests

**File**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/AnthropicProviderTests.cs`

**Tests**:
- Constructor validation
- Message conversion (domain to Anthropic format)
- Response conversion (Anthropic to domain format)
- Error handling and exception wrapping
- Transparency event logging

### Integration Tests

Real API calls are tested in dedicated integration tests (configured separately to avoid API costs in CI/CD).

### Mocking

Uses mocked `IAuthenticationProvider`, `ITransparencyService`, and `AnthropicClient` for unit tests.

## Usage Examples

### Basic Integration Point

```csharp
// Provider factory creates instance
var provider = LLMProviderFactory.CreateProvider(
    config.LLM,
    authProvider,
    transparencyService,
    config
);

// Agent uses via interface
LLMResponse response = await provider.SendRequestAsync(request);
```

### Critical Logic: System Message Extraction

Anthropic requires system messages separate from conversation:

```csharp
// Extract system messages
var systemMessages = request.Messages
    .Where(m => m.Role == "system")
    .ToList();

// Build params with system field
var messageParams = new MessageCreateParams
{
    System = systemMessages.Any()
        ? string.Join("\n", systemMessages.Select(m => m.Content))
        : null,
    Messages = request.Messages
        .Where(m => m.Role != "system")
        .Select(ConvertToAnthropicMessage)
        .ToList()
};
```

## Related Documentation

- [Provider Abstraction](provider-abstraction.md) - ILLMProvider interface
- [Azure OpenAI Provider](azure-openai-provider.md) - Alternative implementation
- [Anthropic API Reference](../../06-reference/providers/anthropic/README.md) - Complete API docs
- [Configuration Guide](../../05-guides/installation/llm-provider-selector.md) - Setup instructions

---

**See Also**: [LLM Components Overview](README.md)
