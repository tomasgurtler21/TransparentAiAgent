# LLM Provider Selector Guide

**Last Updated**: 2025-11-15
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

```json
{
  "Type": "Anthropic",
  "DisplayName": "Claude Haiku (Fast)",
  "Model": "claude-haiku-4-5-20251001",
  "ApiKey": "sk-ant-xxx"
}
```

#### Azure OpenAI Provider

```json
{
  "Type": "AzureOpenAI",
  "DisplayName": "GPT-4 (Azure East US)",
  "Endpoint": "https://your-resource.openai.azure.com/",
  "DeploymentName": "gpt-4",
  "ApiVersion": "2024-02-15-preview",
  "ApiKey": "your-azure-api-key"
}
```

#### OpenAI Provider

```json
{
  "Type": "OpenAI",
  "DisplayName": "GPT-4o",
  "Model": "gpt-4o",
  "ApiKey": "sk-xxx"
}
```

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

### Example 2: Multi-Region Azure OpenAI

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
      "Endpoint": "https://eastus-resource.openai.azure.com/",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview",
      "ApiKey": "YOUR_AZURE_KEY"
    },
    "azure-westus": {
      "Type": "AzureOpenAI",
      "DisplayName": "GPT-4 (West US - Backup)",
      "Endpoint": "https://westus-resource.openai.azure.com/",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview",
      "ApiKey": "YOUR_AZURE_KEY"
    }
  }
}
```

### Example 3: Mixed Providers

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
      "Model": "claude-haiku-4-5-20251001",
      "ApiKey": "YOUR_ANTHROPIC_KEY"
    },
    "azure-gpt4": {
      "Type": "AzureOpenAI",
      "DisplayName": "Azure GPT-4",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview",
      "ApiKey": "YOUR_AZURE_KEY"
    },
    "openai-gpt4o": {
      "Type": "OpenAI",
      "DisplayName": "OpenAI GPT-4o",
      "Model": "gpt-4o",
      "ApiKey": "YOUR_OPENAI_KEY"
    }
  }
}
```

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
