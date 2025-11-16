# LLM Integration Components

**Last Updated**: 2025-11-16
**Status**: Active
**Layer**: Domain + Infrastructure

---

## Overview

The LLM Integration layer provides provider-agnostic abstractions for interacting with Large Language Models. It supports multiple providers (Anthropic Claude, Azure OpenAI, OpenAI) through a unified interface, enabling dynamic provider switching via UI without application restart.

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

### [LLM Provider Manager](llm-provider-manager.md) ✨ NEW
**Layer**: Domain + Infrastructure
**Files**: `Domain/LLM/ILLMProviderManager.cs`, `Infrastructure/LLM/LLMProviderManager.cs`

Central service for managing multiple LLM provider instances with lazy-loading and caching.

**Key Features**:
- Dynamic provider switching at runtime
- Lazy-loading (providers created on first use)
- Provider instance caching
- Thread-safe provider management
- Provider discovery for UI integration

---

### [Anthropic Provider](anthropic-provider.md)
**Layer**: Infrastructure
**File**: `Infrastructure/LLM/AnthropicProvider.cs`

Anthropic Claude API implementation using official SDK.

**Key Features**:
- Claude 3.5 Sonnet, Opus, Haiku support
- Extended Thinking mode
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
- Reasoning model support (o1, o3, GPT-5 series)
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

### Multi-Provider Architecture

```
┌─────────────────────────────────────────┐
│     Application Layer                   │
│  (AgentOrchestrator, MessagePipeline)   │
└──────────────────┬──────────────────────┘
                   │ GetActiveProvider()
                   ▼
         ┌─────────────────────┐
         │ ILLMProviderManager │  (Domain Interface)
         │                     │
         │ - Lazy Loading      │
         │ - Caching           │
         │ - Provider Switching│
         └──────────┬──────────┘
                    │ Implements
                    ▼
         ┌─────────────────────┐
         │ LLMProviderManager  │  (Infrastructure)
         │                     │
         │ Provider Cache      │
         └──────────┬──────────┘
                    │ Creates via
                    ▼
         ┌─────────────────────┐
         │ LLMProviderFactory  │
         └──────────┬──────────┘
                    │ Creates
        ┌───────────┼──────────┐
        ▼           ▼          ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  Anthropic   │ │ Azure OpenAI │ │   OpenAI     │
│  Provider    │ │  Provider    │ │  Provider    │
└──────┬───────┘ └──────┬───────┘ └──────┬───────┘
       │                │                │
       ▼                ▼                ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Anthropic    │ │ Azure.AI     │ │ OpenAI       │
│ SDK          │ │ .OpenAI SDK  │ │ SDK          │
└──────────────┘ └──────────────┘ └──────────────┘
                    All Implement
                    ↓
         ┌─────────────────┐
         │  ILLMProvider   │  (Domain Interface)
         └─────────────────┘
```

---

## Configuration

### Multi-Provider Configuration

Providers are configured in the `LLM.Providers` dictionary with the active provider specified by `LLM.ActiveProvider`.

**New Multi-Provider Structure**:

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "ActiveProvider": "claude-fast",
      "DefaultParameters": {
        "Temperature": 0.7,
        "TopP": 1.0,
        "MaxTokens": 4096
      },
      "Providers": {
        "claude-fast": {
          "Type": "Anthropic",
          "DisplayName": "Claude Haiku (Fast)",
          "Model": "claude-haiku-4-5-20251001",
          "ApiKey": "YOUR_API_KEY"
        },
        "azure-gpt4": {
          "Type": "AzureOpenAI",
          "DisplayName": "Azure GPT-4",
          "Endpoint": "https://your-resource.openai.azure.com/",
          "DeploymentName": "gpt-4",
          "ApiKey": "YOUR_API_KEY",
          "ApiVersion": "2024-02-15-preview"
        },
        "openai-gpt4o": {
          "Type": "OpenAI",
          "DisplayName": "OpenAI GPT-4o",
          "Model": "gpt-4o",
          "ApiKey": "YOUR_API_KEY"
        }
      }
    }
  }
}
```

**Key Elements**:
- `ActiveProvider`: Currently active provider configuration name
- `DefaultParameters`: Shared default parameters for all providers
- `Providers`: Dictionary of named provider configurations
- `Type`: Provider type ("Anthropic", "AzureOpenAI", "OpenAI")
- `DisplayName`: Human-readable name shown in UI
- Provider-specific fields (Model, Endpoint, ApiKey, etc.)

**Parameter Inheritance**: Each provider inherits from `DefaultParameters` and can override specific values using the `Parameters` field.

**See**: [LLM Provider Selector Guide](../../05-guides/deployment/llm-provider-selector.md)

---

## Provider Management

### Provider Manager

**File**: `Infrastructure/LLM/LLMProviderManager.cs`

The Provider Manager handles provider lifecycle and switching:

```csharp
public interface ILLMProviderManager
{
    ILLMProvider GetActiveProvider();
    Task SetActiveProviderAsync(string configName);
    IReadOnlyList<ProviderInfo> GetAvailableProviders();
    ProviderInfo GetCurrentProviderInfo();
}
```

**Features**:
- **Lazy Loading**: Providers created only when first accessed
- **Caching**: Provider instances reused for performance
- **Thread-Safe**: Concurrent access handled safely
- **Persistence**: Active provider selection saved to configuration

**See**: [LLM Provider Manager](llm-provider-manager.md)

### Provider Factory

**File**: `Infrastructure/LLM/LLMProviderFactory.cs`

Factory creates provider instances from configuration:

```csharp
public ILLMProvider CreateProvider(string configName, ProviderConfig config)
{
    return config.Type.ToLowerInvariant() switch
    {
        "anthropic" => CreateAnthropicProvider(config),
        "azureopenai" => CreateAzureOpenAIProvider(config),
        "openai" => CreateOpenAIProvider(config),
        _ => throw new ConfigurationException($"Unknown provider type: {config.Type}")
    };
}
```

**Parameter Handling**: Factory applies parameter inheritance (default → provider-specific overrides) when creating providers.

---

## Usage Flow

```
User Request
    ↓
AgentOrchestrator
    ↓
ILLMProviderManager.GetActiveProvider()
    ├─→ Check cache (if cached, return immediately)
    └─→ Lazy-load via Factory (if not cached)
        ↓
    Active ILLMProvider Instance
        ↓
MessagePipeline (converts domain messages to LLM format)
    ↓
ILLMProvider.SendRequestAsync() / StreamRequestAsync()
    ↓
Provider Implementation (Anthropic, Azure OpenAI, or OpenAI)
    ↓
LLM API Call
    ↓
Response Conversion (LLM format → domain models)
    ↓
TransparencyService (log request/response)
    ↓
Return LLMResponse to orchestrator
```

### Provider Switching Flow

```
User Selects New Provider in UI
    ↓
ProviderStateService.ChangeProviderAsync()
    ↓
ILLMProviderManager.SetActiveProviderAsync(configName)
    ├─→ Update in-memory active provider
    ├─→ Persist to appsettings.json
    └─→ Notify UI (OnProviderChanged event)
        ↓
Next Request
    ↓
GetActiveProvider() returns new provider
    ├─→ If cached: immediate return
    └─→ If not cached: lazy-load and cache
```

---

## Adding New Providers

To add a new LLM provider type (e.g., Google Gemini, Cohere):

1. **Implement ILLMProvider**
   - `SendRequestAsync()` - Non-streaming
   - `StreamRequestAsync()` - Streaming
   - `ProviderName` property
   - Message format conversion
   - Tool calling translation

2. **Update LLMProviderFactory**
   - Add case to `CreateProvider()` switch statement
   - Add provider-specific creation logic
   - Handle parameter extraction from `ProviderConfig`

3. **Add configuration validation**
   - Update `ProviderConfigValidator` with provider-specific rules
   - Validate required fields (API keys, endpoints, model names)
   - Validate field formats

4. **Add tests**
   - Unit tests for provider implementation
   - Factory tests for provider creation
   - Validation tests
   - Integration tests with real API

5. **Document**
   - Create provider doc in `docs/04-components/llm/` (follow existing templates)
   - Update this README comparison table
   - Add configuration examples to [LLM Provider Selector Guide](../../05-guides/deployment/llm-provider-selector.md)
   - Add API reference in `docs/06-reference/providers/`

### Adding a New Provider Configuration

To add a new configuration of an existing provider (e.g., another Claude model):

1. **Add to appsettings.json**:
   ```json
   {
     "Providers": {
       "new-config-name": {
         "Type": "Anthropic",
         "DisplayName": "Display Name in UI",
         "Model": "model-name",
         "ApiKey": "your-api-key",
         "Parameters": {
           "Temperature": 1.0
         }
       }
     }
   }
   ```

2. **Restart application** - New provider will appear in UI dropdown

3. **No code changes required** - Configuration-driven architecture

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

- **Component Documentation**:
  - [LLM Provider Manager](llm-provider-manager.md) - Provider lifecycle management
  - [Provider Abstraction](provider-abstraction.md) - ILLMProvider interface
  - [Anthropic Provider](anthropic-provider.md) - Claude implementation
  - [Azure OpenAI Provider](azure-openai-provider.md) - Azure implementation
  - [Agent Orchestrator](../core/agent-orchestrator.md) - Main LLM consumer
  - [Message Pipeline](../core/message-pipeline.md) - Message format conversion
  - [Configuration Service](../infrastructure/configuration-service.md) - Configuration persistence

- **Guides**:
  - [LLM Provider Selector Guide](../../05-guides/deployment/llm-provider-selector.md) - Multi-provider configuration
  - [Configuration Guide](../../05-guides/deployment/configuration-guide.md) - General configuration

- **Reference**:
  - [Anthropic API Reference](../../06-reference/providers/anthropic/README.md)
  - [Azure OpenAI Reference](../../06-reference/providers/azure-openai/authentication.md)

- **Architecture**:
  - [Design Decision DD-026](../../02-architecture/design-decisions.md#dd-026-llm-provider-selector-architecture) - Provider selector design

---

**See Also**: [Component Overview](../README.md)
