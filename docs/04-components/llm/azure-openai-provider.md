# Azure OpenAI Provider

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 2
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- AzureOpenAIProvider implementation details
- Azure OpenAI API integration
- Azure-specific authentication modes (OAuth, API key)
- Reasoning model support
- Streaming and tool calling for Azure

**What does NOT belong here**:
- ❌ Provider abstraction (ILLMProvider interface) → See [provider-abstraction.md](provider-abstraction.md)
- ❌ Anthropic implementation → See [anthropic-provider.md](anthropic-provider.md)
- ❌ Azure OpenAI API deep dive → See `docs/06-reference/providers/azure-openai/`

---

## Overview

AzureOpenAIProvider implements ILLMProvider for Azure OpenAI Service. It supports multiple authentication modes (API key, OAuth), handles reasoning models (o1-preview), and integrates with Azure SDK for streaming and tool calling.

## Purpose

- **Integrate** with Azure OpenAI Service using official SDK
- **Support** multiple authentication modes (API key, DefaultAzureCredential, InteractiveBrowserCredential)
- **Handle** reasoning models with special processing requirements
- **Translate** domain LLM models to Azure OpenAI format
- **Log** all requests/responses to transparency service

## Responsibilities

- Initialize Azure OpenAI client with appropriate authentication
- Convert LLMRequest to Azure ChatCompletion format
- Convert Azure responses to LLMResponse
- Handle streaming with tool call accumulation
- Support reasoning models (o1-preview, o1-mini)
- Log raw requests/responses for transparency

## Architecture

### Implementation

**File**: `TransparentAiAgentCore/Infrastructure/LLM/AzureOpenAIProvider.cs:26`

```csharp
public class AzureOpenAIProvider : ILLMProvider
{
    private readonly ChatClient _chatClient;
    private readonly ITransparencyService _transparencyService;
    private readonly bool _isReasoningModel;

    public string ProviderName => "AzureOpenAI";
}
```

### Dependencies

**Depends on**:
- `Domain/LLM/ILLMProvider` - Interface contract
- `Domain/Authentication/IAuthenticationProvider` - Credential retrieval
- `Domain/Configuration/AppConfiguration` - Application config
- `Infrastructure/Transparency/ITransparencyService` - Event logging
- `Azure.AI.OpenAI` NuGet package - Official Azure SDK
- `Azure.Identity` NuGet package - OAuth authentication

**Used by**:
- `Infrastructure/LLM/LLMProviderFactory.cs` - Provider instantiation
- `Application/Agent/AgentOrchestrator.cs` - Via ILLMProvider interface

### Key Components

#### Constructor with Multi-Mode Authentication

**File**: `AzureOpenAIProvider.cs:39`

**Authentication Modes**:

1. **API Key** (`AuthenticationMode.ApiKey`)
   - Traditional API key authentication
   - Simplest for development and testing

2. **DefaultAzureCredential** (`AuthenticationMode.DefaultAzureCredential`)
   - OAuth via Microsoft Entra ID
   - Auto-discovers credentials (environment, CLI, managed identity, VS Code)
   - Recommended for production

3. **InteractiveBrowserCredential** (`AuthenticationMode.InteractiveBrowserCredential`)
   - OAuth with browser popup
   - For local development with user login

**Initialization Flow**:
1. Validate parameters
2. Extract endpoint and deployment name
3. Determine authentication mode from config
4. Create appropriate credential (ApiKeyCredential or TokenCredential)
5. Initialize AzureOpenAIClient
6. Get ChatClient for deployment

#### Reasoning Model Support

**Property**: `_isReasoningModel`

Reasoning models (o1-preview, o1-mini) have special requirements:
- No `temperature` or `top_p` parameters allowed
- Different request processing
- Extended thinking time before responses

**Detection**: Set via `appConfig.LLM.AzureOpenAI.IsReasoningModel`

## Implementation Notes

### Design Patterns

**Adapter Pattern**: Converts domain LLM models to Azure OpenAI SDK format.

**Strategy Pattern**: Authentication mode selection at runtime based on configuration.

### Azure-Specific Considerations

1. **Endpoint format**: `https://<resource>.openai.azure.com`
2. **Deployment names**: Uses Azure deployment names (not model names directly)
3. **API versioning**: Requires explicit API version in requests
4. **Tool calling**: Uses OpenAI function calling format (different from Anthropic)
5. **OAuth support**: Full Microsoft Entra ID integration

### Streaming Tool Call Accumulation

**File**: `AzureStreamingToolCallAccumulator.cs`

Azure streams tool calls incrementally:
- Function name arrives first
- Arguments stream as JSON fragments
- Accumulator reassembles complete tool calls

**Key Challenge**: Tool call arguments can split mid-JSON token, requiring careful reassembly.

### Error Handling

- Wraps SDK exceptions in `LLMException`
- Logs errors to transparency service
- Validates configuration at startup (fail-fast)
- Handles OAuth token refresh automatically (via Azure SDK)

## Configuration

### API Key Authentication

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4",
      "ApiKey": "your-api-key",
      "ApiVersion": "2024-02-15-preview",
      "AuthenticationMode": "ApiKey"
    }
  }
}
```

### OAuth Authentication (DefaultAzureCredential)

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

### Reasoning Model

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "DeploymentName": "o1-preview",
      "IsReasoningModel": true,
      "AuthenticationMode": "ApiKey"
    }
  }
}
```

**See**: `docs/06-reference/providers/azure-openai/authentication.md`

## Testing Strategy

### Unit Tests

**File**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/AzureOpenAIProviderTests.cs`

**Tests**:
- Constructor validation
- Authentication mode selection
- Message conversion (domain to Azure format)
- Response conversion (Azure to domain format)
- Error handling

**File**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/AzureOpenAIProviderStreamingTests.cs`

**Tests**:
- Streaming response handling
- Tool call accumulation during streaming
- Error handling in streaming scenarios

### Integration Tests

Real Azure API calls tested separately with valid credentials (not in CI/CD to avoid costs).

## Usage Examples

### Critical Logic: Authentication Mode Selection

```csharp
// Factory creates appropriate client based on config
switch (azureConfig.AuthenticationMode)
{
    case AuthenticationMode.DefaultAzureCredential:
        var tokenCredential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { TenantId = azureConfig.TenantId }
        );
        azureClient = new AzureOpenAIClient(endpoint, tokenCredential);
        break;

    case AuthenticationMode.ApiKey:
        azureClient = new AzureOpenAIClient(
            endpoint,
            new ApiKeyCredential(authProvider.GetApiKey("AzureOpenAI"))
        );
        break;
}
```

### Integration Point: Provider Factory

```csharp
// LLMProviderFactory creates instance
var provider = new AzureOpenAIProvider(
    authProvider,
    config.LLM.AzureOpenAI.DeploymentName,
    transparencyService,
    config
);

// Used via interface
LLMResponse response = await provider.SendRequestAsync(request);
```

## Related Documentation

- [Provider Abstraction](provider-abstraction.md) - ILLMProvider interface
- [Anthropic Provider](anthropic-provider.md) - Alternative implementation
- [Azure OpenAI Auth Guide](../../06-reference/providers/azure-openai/authentication.md) - OAuth setup
- [Configuration Guide](../../05-guides/deployment/configuration-guide.md) - Setup instructions

---

**See Also**: [LLM Components Overview](README.md)
