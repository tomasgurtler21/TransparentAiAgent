# LLM Provider Selector Guide

**Last Updated**: 2025-11-21
**Status**: ✅ Implemented

## 📋 Scope

### ✅ What's in this document
- Configuring multiple LLM providers
- Switching providers via UI
- Parameter inheritance and overrides
- Troubleshooting provider configuration

### ❌ What's NOT in this document
- Individual provider API details → See [06-reference/providers/](../../06-reference/providers/)
- LLM Provider implementation → See [04-components/llm/](../../04-components/llm/)
- Architecture decisions → See [02-architecture/design-decisions.md](../../02-architecture/design-decisions.md)

---

## Overview

The LLM Provider Selector allows you to configure multiple LLM providers (e.g., different Claude models, Azure OpenAI, OpenAI) and switch between them dynamically via a UI dropdown without restarting the application.

### Key Features

- ✅ **Multiple Providers**: Configure as many providers as needed
- ✅ **Dynamic Switching**: Change providers via UI dropdown
- ✅ **Parameter Inheritance**: Set default parameters, override per-provider
- ✅ **Persistent Selection**: Active provider saved to appsettings.json
- ✅ **Backward Compatible**: Existing code continues to work

---

## Quick Start

### 1. Update appsettings.json

Replace the old `LLM` configuration with the new multi-provider structure:

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
          "ApiKey": "YOUR_API_KEY_HERE"
        }
      }
    }
  }
}
```

### 2. Start the Application

The provider selector will appear in the UI header next to the conversation selector.

### 3. Switch Providers

Click the dropdown and select a different provider. The change takes effect immediately for new messages.

---

## Configuration

### Basic Structure

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "ActiveProvider": "provider-config-name",
      "DefaultParameters": { /* Default parameters for all providers */ },
      "Providers": {
        "provider-config-name": { /* Provider configuration */ }
      }
    }
  }
}
```

**Fields**:
- `ActiveProvider` (required): The config name of the currently active provider
- `DefaultParameters` (optional): Default parameters inherited by all providers
- `Providers` (required): Dictionary of provider configurations (at least one)

### Provider Configuration

Each provider requires:
- `Type` (required): Provider type - "Anthropic", "AzureOpenAI", or "OpenAI"
- `DisplayName` (required): Human-readable name shown in UI dropdown

Additional fields depend on provider type:

#### Anthropic Provider

**Basic Configuration:**
```json
{
  "Type": "Anthropic",
  "DisplayName": "Claude Haiku (Fast)",
  "Parameters": {
    "Model": "claude-haiku-4-5-20251001",
    "ApiKey": "sk-ant-xxx"
  }
}
```

**With Custom Endpoint (for proxies or alternative endpoints):**
```json
{
  "Type": "Anthropic",
  "DisplayName": "Claude via Custom Endpoint",
  "Parameters": {
    "Model": "claude-haiku-4-5-20251001",
    "ApiKey": "sk-ant-xxx",
    "Endpoint": "https://custom-anthropic-proxy.example.com"
  }
}
```

**Parameters:**
- `Model` (required): Claude model identifier
- `ApiKey` (required): Anthropic API key
- `Endpoint` (optional): Custom endpoint URL. Defaults to `https://api.anthropic.com` if not specified.

#### Azure OpenAI Provider

**With API Key Authentication:**
```json
{
  "Type": "AzureOpenAI",
  "DisplayName": "GPT-4 (Azure - API Key)",
  "Parameters": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "AuthenticationMode": "ApiKey",
    "ApiKey": "your-azure-api-key"
  }
}
```

**With DefaultAzureCredential (OAuth - Recommended for Production):**
```json
{
  "Type": "AzureOpenAI",
  "DisplayName": "GPT-4 (Azure - OAuth)",
  "Parameters": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "AuthenticationMode": "DefaultAzureCredential",
    "TenantId": "optional-tenant-id"
  }
}
```

**With InteractiveBrowserCredential (OAuth - Best for Desktop Apps):**
```json
{
  "Type": "AzureOpenAI",
  "DisplayName": "GPT-4 (Azure - Interactive)",
  "Parameters": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "AuthenticationMode": "InteractiveBrowserCredential",
    "TenantId": "optional-tenant-id"
  }
}
```

**Authentication Modes:**
- `ApiKey` - Static API key (simple, less secure)
- `DefaultAzureCredential` - Automatically discovers credentials from environment, managed identity, Azure CLI (`az login`), Visual Studio, etc. No API key needed!
- `InteractiveBrowserCredential` - Opens browser popup for interactive Microsoft account login. Best for GUI applications.

#### OpenAI Provider

**Basic Configuration:**
```json
{
  "Type": "OpenAI",
  "DisplayName": "GPT-4o",
  "Parameters": {
    "Model": "gpt-4o",
    "ApiKey": "sk-xxx",
    "IsReasoningModel": false
  }
}
```

**With Custom Endpoint (for OpenAI-compatible APIs):**
```json
{
  "Type": "OpenAI",
  "DisplayName": "LocalAI / vLLM / Ollama",
  "Parameters": {
    "Model": "gpt-4o",
    "ApiKey": "sk-xxx",
    "IsReasoningModel": false,
    "Endpoint": "https://custom-openai-compatible.example.com/v1"
  }
}
```

**Parameters:**
- `Model` (required): OpenAI model identifier
- `ApiKey` (required): OpenAI API key
- `IsReasoningModel` (required): Set to `true` for reasoning models (o1, o3, o4-mini), `false` for standard models
- `Endpoint` (optional): Custom endpoint URL. Defaults to `https://api.openai.com/v1` if not specified. Useful for:
  - OpenAI-compatible APIs (LocalAI, vLLM, Ollama with OpenAI compatibility)
  - Corporate proxies or API gateways
  - Development/testing environments

---

## Parameter Inheritance

### Default Parameters

Set common parameters once in `DefaultParameters`:

```json
{
  "DefaultParameters": {
    "Temperature": 0.7,
    "TopP": 1.0,
    "MaxTokens": 4096
  }
}
```

All providers inherit these values automatically.

### Per-Provider Overrides

Override defaults for specific providers using `Parameters`:

```json
{
  "claude-thinking": {
    "Type": "Anthropic",
    "DisplayName": "Claude with Extended Thinking",
    "Model": "claude-haiku-4-5-20251001",
    "ApiKey": "sk-ant-xxx",
    "Parameters": {
      "MaxTokens": 8192  // Override just MaxTokens
    },
    "ExtendedThinking": {
      "Enabled": true,
      "BudgetTokens": 10000
    }
  }
}
```

**Result**: `Temperature` and `TopP` come from defaults, `MaxTokens` is overridden to 8192.

---

## Azure OpenAI OAuth Authentication

Azure OpenAI supports **three authentication modes**: API Key, DefaultAzureCredential, and InteractiveBrowserCredential.

### Authentication Modes

| Mode | Use Case | Requires API Key | Setup |
|------|----------|------------------|-------|
| `ApiKey` | Simple development | ✅ Yes | Copy API key from Azure Portal |
| `DefaultAzureCredential` | Production, servers, CI/CD | ❌ No | Assign Azure RBAC role, run `az login` for local dev |
| `InteractiveBrowserCredential` | Desktop GUI apps | ❌ No | Assign Azure RBAC role, user logs in via browser |

### DefaultAzureCredential (Recommended for Production)

**What it does:** Automatically discovers credentials from multiple sources in this order:
1. Environment variables (service principal)
2. Workload Identity (Azure Kubernetes)
3. Managed Identity (Azure App Service, Container Apps, VMs)
4. Azure CLI (`az login`) - for local development
5. Visual Studio / VS Code credentials

**Configuration:**
```json
{
  "azure-gpt4-oauth": {
    "Type": "AzureOpenAI",
    "DisplayName": "Azure GPT-4 (OAuth)",
    "Parameters": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview",
      "AuthenticationMode": "DefaultAzureCredential",
      "TenantId": "optional-tenant-id"  // Only needed for multi-tenant scenarios
    }
  }
}
```

**Prerequisites:**
1. **Assign Azure RBAC Role** to your user/managed identity:
   - Role: `Cognitive Services OpenAI User` or `Cognitive Services OpenAI Contributor`
   - Scope: Your Azure OpenAI resource
   - Via Azure Portal: Resource → Access Control (IAM) → Add role assignment
   - Via CLI: `az role assignment create --role "Cognitive Services OpenAI User" --assignee your-email@company.com --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource-name}`

2. **For local development:**
   - Install Azure CLI: `winget install Microsoft.AzureCLI`
   - Login: `az login`
   - Your credentials are cached and used automatically

3. **For production (Azure-hosted apps):**
   - Enable Managed Identity on your App Service/Container/VM
   - Assign the RBAC role to the managed identity
   - No secrets needed in configuration!

### InteractiveBrowserCredential (Best for Desktop Apps)

**What it does:** Opens a browser popup for interactive Microsoft account login. User authenticates with their Microsoft/Azure AD account.

**Configuration:**
```json
{
  "azure-gpt4-interactive": {
    "Type": "AzureOpenAI",
    "DisplayName": "Azure GPT-4 (Interactive)",
    "Parameters": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview",
      "AuthenticationMode": "InteractiveBrowserCredential",
      "TenantId": "optional-tenant-id"
    }
  }
}
```

**Prerequisites:**
1. Same RBAC role assignment as DefaultAzureCredential
2. User must have permission to access the Azure OpenAI resource
3. Browser available for popup (doesn't work in headless environments)

**When to use:**
- Desktop GUI applications (like this Blazor app)
- User-facing tools where each user has their own Azure credentials
- Development environments where you want explicit login control

### Multi-Region Failover with OAuth

You can configure multiple Azure OpenAI providers in different regions, all using OAuth:

```json
{
  "Providers": {
    "azure-eastus": {
      "Type": "AzureOpenAI",
      "DisplayName": "Azure GPT-4 (East US)",
      "Parameters": {
        "Endpoint": "https://eastus-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "AuthenticationMode": "DefaultAzureCredential"
      }
    },
    "azure-westus": {
      "Type": "AzureOpenAI",
      "DisplayName": "Azure GPT-4 (West US - Backup)",
      "Parameters": {
        "Endpoint": "https://westus-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "AuthenticationMode": "DefaultAzureCredential"
      }
    }
  }
}
```

Switch between regions via the UI dropdown if one region experiences issues!

---

## Reasoning Models (o1, o3, GPT-5)

### What Are Reasoning Models?

Reasoning models are a class of LLMs that use extended internal reasoning processes:
- **OpenAI**: o1, o1-mini, o3, o3-mini, o3-pro, o4-mini
- **Azure OpenAI**: GPT-5 series (gpt-5, gpt-5-mini, gpt-5-pro), o1, o3 models
- **Anthropic**: Extended Thinking mode (different implementation, no restrictions)

### API Restrictions

Reasoning models have **special requirements**:
- ✅ **MUST set** `IsReasoningModel: true` in provider Parameters
- ❌ **DO NOT set** `Temperature` in DefaultParameters or ParameterOverrides
- ❌ **DO NOT set** `TopP` in DefaultParameters or ParameterOverrides
- ✅ Use `MaxTokens` normally (converted to `max_completion_tokens` internally)
- ⚠️ Streaming may be limited or unavailable

**Why?** OpenAI's API will reject requests with 400 Bad Request if temperature/top_p are included for reasoning models.

### Configuring Reasoning Models

#### Azure OpenAI o3-mini

```json
{
  "azure-o3-mini": {
    "Type": "AzureOpenAI",
    "DisplayName": "Azure o3-mini (Reasoning)",
    "Parameters": {
      "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
      "DeploymentName": "o3-mini",
      "ApiKey": "YOUR_API_KEY",
      "ApiVersion": "2024-02-15-preview",
      "AuthenticationMode": "ApiKey",
      "IsReasoningModel": true  // ✅ Required!
    },
    "ParameterOverrides": {
      "Temperature": null,  // ✅ Override default to null
      "TopP": null          // ✅ Override default to null
    }
  }
}
```

#### OpenAI o1-preview

```json
{
  "openai-o1": {
    "Type": "OpenAI",
    "DisplayName": "OpenAI o1-preview (Reasoning)",
    "Parameters": {
      "Model": "o1-preview",
      "ApiKey": "YOUR_API_KEY",
      "IsReasoningModel": true  // ✅ Required!
    },
    "ParameterOverrides": {
      "Temperature": null,
      "TopP": null
    }
  }
}
```

#### Azure OpenAI GPT-5

```json
{
  "azure-gpt5": {
    "Type": "AzureOpenAI",
    "DisplayName": "Azure GPT-5 (Reasoning)",
    "Parameters": {
      "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
      "DeploymentName": "gpt-5",
      "ApiKey": "YOUR_API_KEY",
      "ApiVersion": "2024-02-15-preview",
      "AuthenticationMode": "ApiKey",
      "IsReasoningModel": true
    },
    "ParameterOverrides": {
      "Temperature": null,
      "TopP": null
    }
  }
}
```

### Anthropic Extended Thinking

**Note**: Anthropic's Extended Thinking mode does **NOT** have the same restrictions as OpenAI reasoning models. You **CAN** use Temperature and TopP with Extended Thinking.

```json
{
  "claude-thinking": {
    "Type": "Anthropic",
    "DisplayName": "Claude with Extended Thinking",
    "Parameters": {
      "Model": "claude-sonnet-4-5-20251001",
      "ApiKey": "YOUR_API_KEY",
      "ExtendedThinking": {
        "Enabled": true,
        "BudgetTokens": 10000
      }
    }
    // ℹ️ No IsReasoningModel needed for Anthropic
    // ℹ️ Temperature/TopP work normally with Extended Thinking
  }
}
```

### Troubleshooting Reasoning Models

**Error: "400 Bad Request" when using o1/o3 models**

**Cause**: Temperature or TopP sent to reasoning model API

**Solution**:
1. Add `"IsReasoningModel": true` to provider Parameters
2. Override Temperature/TopP to `null` in ParameterOverrides
3. Remove Temperature/TopP from DefaultParameters (if you only use reasoning models)

**Example Configuration with Both Model Types**:

```json
{
  "DefaultParameters": {
    "Temperature": 0.7,  // Used by non-reasoning models
    "TopP": 1.0,
    "MaxTokens": 4096
  },
  "Providers": {
    "gpt-4o": {
      "Type": "OpenAI",
      "DisplayName": "GPT-4o (Standard)",
      "Parameters": {
        "Model": "gpt-4o",
        "ApiKey": "YOUR_KEY"
      }
      // Uses Temperature/TopP from defaults
    },
    "o1-preview": {
      "Type": "OpenAI",
      "DisplayName": "o1-preview (Reasoning)",
      "Parameters": {
        "Model": "o1-preview",
        "ApiKey": "YOUR_KEY",
        "IsReasoningModel": true
      },
      "ParameterOverrides": {
        "Temperature": null,  // Override default
        "TopP": null          // Override default
      }
    }
  }
}
```

---

## Configuration Examples

### Example 1: Multiple Claude Models

```json
{
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
    "claude-thinking": {
      "Type": "Anthropic",
      "DisplayName": "Claude with Extended Thinking",
      "Model": "claude-haiku-4-5-20251001",
      "ApiKey": "YOUR_API_KEY",
      "ExtendedThinking": {
        "Enabled": true,
        "BudgetTokens": 10000
      },
      "Parameters": {
        "MaxTokens": 8192
      }
    },
    "claude-creative": {
      "Type": "Anthropic",
      "DisplayName": "Claude (Creative Mode)",
      "Model": "claude-haiku-4-5-20251001",
      "ApiKey": "YOUR_API_KEY",
      "Parameters": {
        "Temperature": 1.0
      }
    }
  }
}
```

### Example 2: Multi-Region Azure OpenAI with OAuth

```json
{
  "ActiveProvider": "azure-eastus",
  "DefaultParameters": {
    "Temperature": 0.7,
    "TopP": 1.0,
    "MaxTokens": 4096
  },
  "Providers": {
    "azure-eastus": {
      "Type": "AzureOpenAI",
      "DisplayName": "GPT-4 (East US)",
      "Parameters": {
        "Endpoint": "https://eastus-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "AuthenticationMode": "DefaultAzureCredential"
      }
    },
    "azure-westus": {
      "Type": "AzureOpenAI",
      "DisplayName": "GPT-4 (West US - Backup)",
      "Parameters": {
        "Endpoint": "https://westus-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "AuthenticationMode": "DefaultAzureCredential"
      }
    }
  }
}
```

**Note:** Using OAuth (DefaultAzureCredential) means no API keys in configuration! Just run `az login` locally or enable Managed Identity in production.

### Example 3: Mixed Providers with Different Auth Modes

```json
{
  "ActiveProvider": "claude-fast",
  "DefaultParameters": {
    "Temperature": 0.7,
    "TopP": 1.0,
    "MaxTokens": 4096
  },
  "Providers": {
    "claude-fast": {
      "Type": "Anthropic",
      "DisplayName": "Claude Haiku",
      "Parameters": {
        "Model": "claude-haiku-4-5-20251001",
        "ApiKey": "YOUR_ANTHROPIC_KEY"
      }
    },
    "azure-gpt4-oauth": {
      "Type": "AzureOpenAI",
      "DisplayName": "Azure GPT-4 (OAuth)",
      "Parameters": {
        "Endpoint": "https://your-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "AuthenticationMode": "DefaultAzureCredential"
      }
    },
    "azure-gpt4-apikey": {
      "Type": "AzureOpenAI",
      "DisplayName": "Azure GPT-4 (API Key)",
      "Parameters": {
        "Endpoint": "https://your-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "AuthenticationMode": "ApiKey",
        "ApiKey": "YOUR_AZURE_KEY"
      }
    },
    "openai-gpt4o": {
      "Type": "OpenAI",
      "DisplayName": "OpenAI GPT-4o",
      "Parameters": {
        "Model": "gpt-4o",
        "ApiKey": "YOUR_OPENAI_KEY"
      }
    }
  }
}
```

**Note:** This example shows Azure with both OAuth and API Key authentication methods. You can switch between them via the UI dropdown.

---

## Using the Provider Selector

### UI Location

The provider selector appears in the top section of the home page, next to the conversation selector.

### Switching Providers

1. Click the provider dropdown
2. Select a different provider from the list
3. The UI shows "Switching..." while changing
4. New messages will use the selected provider
5. Existing conversation history is preserved

### Mid-Conversation Switching

You can switch providers during a conversation:
- ✅ Conversation history is preserved
- ✅ New messages use the new provider
- ✅ Previous messages remain unchanged
- ✅ Selection is saved to configuration

---

## Troubleshooting

### Provider Dropdown is Empty

**Cause**: No providers configured in appsettings.json

**Solution**:
1. Check that `Providers` section exists in appsettings.json
2. Ensure at least one provider is configured
3. Verify JSON syntax is valid

### Error: "Provider not found in configuration"

**Cause**: `ActiveProvider` references a provider that doesn't exist

**Solution**:
1. Check `ActiveProvider` value matches a key in `Providers`
2. Provider names are case-sensitive
3. Update `ActiveProvider` to an existing provider name

### Error: "Failed to switch provider"

**Common Causes**:
1. **Invalid API Key**: Check API key is correct for the provider
2. **Network Issues**: Verify internet connection
3. **Endpoint Issues** (Azure): Check endpoint URL is correct
4. **Model Not Available**: Verify model name is valid

**Solution**:
1. Check application logs for detailed error
2. Verify provider configuration fields
3. Test API key using provider's API directly

### Configuration File Permission Error

**Cause**: Application can't write to appsettings.json

**Effect**: Provider switches work in-memory but aren't persisted

**Solution**:
1. Check file permissions on appsettings.json
2. Run application with appropriate permissions
3. Or manually update appsettings.json

### Provider Validation Errors on Startup

**Error Messages**:
- "Anthropic provider requires 'Model' parameter"
- "AzureOpenAI provider requires 'Endpoint' parameter"
- "Unknown provider type: XYZ"

**Solution**:
1. Check the validation error message
2. Verify all required fields are present for the provider type
3. Correct the configuration and restart

---

## Best Practices

### Naming Providers

Use descriptive config names that indicate:
- Provider type
- Model or deployment
- Special configuration (e.g., "claude-thinking", "azure-eastus")

### Organizing Multiple Providers

Group similar providers:
```json
{
  "claude-fast": { "Model": "claude-haiku..." },
  "claude-thinking": { "Model": "claude-haiku...", "ExtendedThinking": {...} },
  "claude-creative": { "Model": "claude-haiku...", "Temperature": 1.0 },
  "azure-eastus": { "Endpoint": "eastus..." },
  "azure-westus": { "Endpoint": "westus..." }
}
```

### API Key Management

**For Development**:
- Store API keys directly in appsettings.json
- Add appsettings.json to .gitignore

**For Production**:
- Use environment variables
- Use Azure Key Vault or similar secrets management
- Reference via `${ENV_VAR}` syntax (if supported by your deployment)

### Testing Multiple Providers

1. Configure 2-3 providers initially
2. Test each provider works correctly
3. Verify switching between providers
4. Add more providers as needed

---

## Related Documentation

- **Provider API References**: [06-reference/providers/](../../06-reference/providers/)
- **LLM Components**: [04-components/llm/](../../04-components/llm/)
- **Configuration Service**: [04-components/infrastructure/configuration-service.md](../../04-components/infrastructure/configuration-service.md)
- **Design Decisions**: [02-architecture/design-decisions.md](../../02-architecture/design-decisions.md)

---

## FAQ

### Can I add providers without restarting?

No, provider configurations are loaded at startup. To add a new provider:
1. Stop the application
2. Add provider to appsettings.json
3. Restart the application
4. New provider will appear in dropdown

### Does switching providers clear the conversation?

No, conversation history is preserved when switching providers.

### Can I have multiple providers of the same type?

Yes! You can configure multiple Anthropic, Azure OpenAI, or OpenAI providers with different configurations (models, regions, parameters, etc.).

### What happens to the old LLM configuration format?

The old format is still validated for backward compatibility during transition, but the new multi-provider format is recommended for all deployments.

---

**Happy configuring!** 🚀
