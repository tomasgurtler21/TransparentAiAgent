# LLM Integration Components

**Last Updated**: 2025-11-08
**Status**: Active
**Layer**: Domain + Infrastructure

---

## Overview

The LLM Integration layer provides provider-agnostic abstractions for interacting with Large Language Models. It supports multiple providers (Anthropic Claude, Azure OpenAI) through a unified interface, enabling switching providers via configuration without code changes.

## Components

### [Provider Abstraction](provider-abstraction.md)
**Layer**: Domain
**Files**: `Domain/LLM/*.cs`

Defines ILLMProvider interface and domain models (LLMRequest, LLMResponse, LLMMessage, LLMTool, etc.). Provides the contract all provider implementations must follow.

**Key Features**:
- Provider-agnostic interface
- Streaming and non-streaming support
- Tool calling abstraction
- Immutable domain models

---

### [Anthropic Provider](anthropic-provider.md)
**Layer**: Infrastructure
**File**: `Infrastructure/LLM/AnthropicProvider.cs`

Anthropic Claude API implementation using official SDK.

**Key Features**:
- Claude 3.5 Sonnet, Opus, Haiku support
- Streaming with tool call accumulation
- System message handling (separate parameter)
- Transparency logging

---

### [Azure OpenAI Provider](azure-openai-provider.md)
**Layer**: Infrastructure
**File**: `Infrastructure/LLM/AzureOpenAIProvider.cs`

Azure OpenAI Service implementation with multi-mode authentication.

**Key Features**:
- Multiple auth modes (API key, DefaultAzureCredential, InteractiveBrowserCredential)
- Reasoning model support (o1-preview, o1-mini)
- OAuth via Microsoft Entra ID
- Streaming with tool call accumulation

---

### [Streaming Utilities](streaming.md)
**Layer**: Infrastructure
**File**: `Infrastructure/Streaming/MarkdownStreamingBuffer.cs`

Utilities for handling partial streaming content.

**Key Features**:
- Markdown construct buffering
- Incomplete code block detection
- Force flush timeout

---

## Provider Comparison

| Feature | Anthropic | Azure OpenAI |
|---------|-----------|--------------|
| **Authentication** | API Key | API Key, OAuth (DefaultAzureCredential, InteractiveBrowserCredential) |
| **Models** | Claude 3.5 Sonnet, Opus, Haiku | GPT-4, GPT-4 Turbo, o1-preview, o1-mini |
| **Streaming** | ✅ Yes | ✅ Yes |
| **Tool Calling** | ✅ Yes | ✅ Yes (function calling) |
| **System Messages** | Separate `system` parameter | Part of messages array |
| **Reasoning Models** | ❌ No | ✅ Yes (o1-preview, o1-mini) |
| **SDK** | Anthropic.Client | Azure.AI.OpenAI |
| **Special Handling** | System message extraction | OAuth token refresh, reasoning model params |

**See**:
- `docs/06-reference/providers/anthropic/` - Anthropic API details
- `docs/06-reference/providers/azure-openai/` - Azure API details

---

## Architecture

```
┌─────────────────────────────────────────┐
│     Application Layer                   │
│  (AgentOrchestrator, MessagePipeline)   │
└──────────────────┬──────────────────────┘
                   │ Uses
                   ▼
         ┌─────────────────┐
         │  ILLMProvider   │  (Domain Interface)
         └─────────────────┘
                   ▲
                   │ Implements
       ┌───────────┴───────────┐
       │                       │
┌──────▼──────┐       ┌────────▼────────┐
│  Anthropic  │       │  Azure OpenAI   │
│  Provider   │       │  Provider       │
└─────────────┘       └─────────────────┘
       │                       │
       │                       │
       ▼                       ▼
┌─────────────┐       ┌─────────────────┐
│ Anthropic   │       │ Azure.AI.OpenAI │
│ SDK         │       │ SDK             │
└─────────────┘       └─────────────────┘
```

---

## Configuration

Providers are selected via `LLM.Provider` in application configuration.

### Anthropic Example

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

### Azure OpenAI Example

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4",
      "AuthenticationMode": "DefaultAzureCredential",
      "TenantId": "your-tenant-id"
    }
  }
}
```

**See**: `docs/05-guides/deployment/configuration-guide.md`

---

## Provider Selection

**File**: `Infrastructure/LLM/LLMProviderFactory.cs`

Factory pattern creates appropriate provider based on configuration:

```csharp
public static ILLMProvider CreateProvider(
    LLMConfiguration config,
    IAuthenticationProvider authProvider,
    ITransparencyService transparencyService,
    AppConfiguration appConfig)
{
    return config.Provider switch
    {
        "Anthropic" => new AnthropicProvider(authProvider, config.Anthropic.ModelName, transparencyService, appConfig),
        "AzureOpenAI" => new AzureOpenAIProvider(authProvider, config.AzureOpenAI.DeploymentName, transparencyService, appConfig),
        _ => throw new NotSupportedException($"Provider {config.Provider} not supported")
    };
}
```

---

## Usage Flow

```
User Request
    ↓
AgentOrchestrator
    ↓
MessagePipeline (converts domain messages to LLM format)
    ↓
ILLMProvider.SendRequestAsync() / StreamRequestAsync()
    ↓
Provider Implementation (Anthropic or Azure)
    ↓
LLM API Call
    ↓
Response Conversion (LLM format → domain models)
    ↓
TransparencyService (log request/response)
    ↓
Return LLMResponse to orchestrator
```

---

## Adding New Providers

To add a new LLM provider:

1. **Implement ILLMProvider**
   - `SendRequestAsync()` - Non-streaming
   - `StreamRequestAsync()` - Streaming
   - `ProviderName` property

2. **Create configuration class**
   - Add to `Domain/Configuration/LLMConfiguration.cs`
   - Include provider-specific settings

3. **Update factory**
   - Add case to `LLMProviderFactory.CreateProvider()`

4. **Add tests**
   - Unit tests for implementation
   - Integration tests with real API

5. **Document**
   - Create provider doc (follow template)
   - Update this README comparison table

---

## Testing

### Unit Tests

**Files**:
- `TransparentAiAgentCore_Tests/Domain/LLM/*Tests.cs` - Domain models
- `TransparentAiAgentCore_Tests/Infrastructure/LLM/AnthropicProviderTests.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/LLM/AzureOpenAIProviderTests.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/LLM/LLMProviderFactoryTests.cs`

### Integration Tests

Real API calls tested separately with valid credentials.

---

## Related Documentation

- [Agent Orchestrator](../core/agent-orchestrator.md) - Main LLM consumer
- [Message Pipeline](../core/message-pipeline.md) - Message format conversion
- [Configuration Service](../infrastructure/configuration-service.md) - Provider selection
- [Anthropic API Reference](../../06-reference/providers/anthropic/README.md)
- [Azure OpenAI Reference](../../06-reference/providers/azure-openai/authentication.md)

---

**See Also**: [Component Overview](../README.md)
