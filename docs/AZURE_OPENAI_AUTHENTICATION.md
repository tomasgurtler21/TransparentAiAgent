# Azure OpenAI Authentication

This document explains the authentication options available for connecting to Azure OpenAI.

## Overview

The Transparent AI Agent supports three authentication methods for Azure OpenAI:

1. **API Key** - Static API key authentication
2. **DefaultAzureCredential** - Automated OAuth authentication (for server/automated scenarios)
3. **InteractiveBrowserCredential** - Interactive OAuth authentication with browser popup (for desktop GUI apps)

## Authentication Methods

### 1. API Key Authentication

**Use when:**
- Quick setup for development/testing
- You have an Azure OpenAI API key

**Configuration:**
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "ApiKey",
        "Endpoint": "https://your-resource.openai.azure.com/",
        "ApiKey": "your-api-key-here",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

**How to get API key:**
1. Go to Azure Portal
2. Navigate to your Azure OpenAI resource
3. Go to "Keys and Endpoint"
4. Copy either Key 1 or Key 2

**Pros:**
- Simple and quick to set up
- Works immediately

**Cons:**
- Less secure (static keys can be exposed)
- No fine-grained access control
- Manual key rotation required

### 2. DefaultAzureCredential (OAuth - Automated)

**Use when:**
- Running on Azure-hosted services (App Service, Container, VM)
- Running automated scripts/CI-CD pipelines
- Using Azure CLI (`az login`) for local development

**Configuration:**
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "DefaultAzureCredential",
        "Endpoint": "https://your-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

**Optional - Specify tenant:**
```json
"AzureOpenAI": {
  "AuthenticationMode": "DefaultAzureCredential",
  "Endpoint": "https://your-resource.openai.azure.com/",
  "DeploymentName": "gpt-4",
  "TenantId": "your-tenant-id"
}
```

**How it works:**
DefaultAzureCredential tries authentication methods in this order:
1. Environment variables
2. Workload identity (Kubernetes)
3. Managed identity
4. Visual Studio authentication
5. Azure CLI (`az login`)
6. Azure PowerShell
7. Azure Developer CLI

**Setup:**
```bash
# For local development, login with Azure CLI
az login

# Assign role to your user
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee your-email@company.com \
  --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource-name}
```

**Pros:**
- More secure (short-lived tokens, auto-refresh)
- No secrets in configuration
- Works seamlessly with managed identity in Azure
- Automatic credential discovery

**Cons:**
- Requires `az login` for local development
- Not suitable for GUI apps (no interactive prompt)

### 3. InteractiveBrowserCredential (OAuth - Interactive)

**Use when:**
- Building desktop GUI applications
- You want users to login interactively with their Microsoft account

**Configuration:**
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "InteractiveBrowserCredential",
        "Endpoint": "https://your-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

**Optional - Specify tenant:**
```json
"AzureOpenAI": {
  "AuthenticationMode": "InteractiveBrowserCredential",
  "Endpoint": "https://your-resource.openai.azure.com/",
  "DeploymentName": "gpt-4",
  "TenantId": "your-tenant-id"
}
```

**How it works:**
1. First time you send a request to Azure OpenAI, a browser window opens
2. You login with your Microsoft account
3. Credentials are cached locally
4. Subsequent requests use cached credentials (no popup)
5. Tokens are automatically refreshed

**Setup:**
```bash
# Assign role to your user
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee your-email@company.com \
  --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource-name}
```

**Or via Azure Portal:**
1. Navigate to your Azure OpenAI resource
2. Go to "Access Control (IAM)"
3. Click "Add role assignment"
4. Select "Cognitive Services OpenAI User"
5. Select your user
6. Click "Save"

**Pros:**
- User-friendly (browser popup for login)
- More secure than API keys
- Best for desktop GUI applications
- No need for `az login` command

**Cons:**
- Requires role assignment in Azure
- May prompt for login periodically (when tokens expire)

## Which Method to Choose?

| Scenario | Recommended Method |
|----------|-------------------|
| Desktop GUI app (like this project) | **InteractiveBrowserCredential** |
| Local development with CLI tools | DefaultAzureCredential |
| Azure-hosted app (App Service, VM) | DefaultAzureCredential |
| Quick testing/prototyping | ApiKey |
| CI/CD pipelines | DefaultAzureCredential |
| Production deployment | DefaultAzureCredential or InteractiveBrowserCredential |

## Required Azure Roles

For OAuth authentication (DefaultAzureCredential or InteractiveBrowserCredential), you need one of these roles:

| Role | Permissions |
|------|------------|
| Cognitive Services OpenAI User | Read and call inference APIs |
| Cognitive Services OpenAI Contributor | Full access including model management |

For most use cases, **Cognitive Services OpenAI User** is sufficient.

## Security Comparison

| Aspect | API Key | DefaultAzureCredential | InteractiveBrowserCredential |
|--------|---------|------------------------|------------------------------|
| Token Lifetime | Permanent (until rotated) | 1 hour (auto-refresh) | 1 hour (auto-refresh) |
| Stored in Config | Yes (security risk) | No | No |
| Rotation | Manual | Automatic | Automatic |
| RBAC | No | Yes | Yes |
| Audit Logs | Limited | Full Azure AD audit trail | Full Azure AD audit trail |
| User Experience | Immediate | Requires `az login` | Browser popup (one-time) |
| Best for | Quick testing | Automated/server scenarios | Desktop GUI apps |

## Troubleshooting

### InteractiveBrowserCredential Issues

**Error: "DefaultAzureCredential failed to retrieve a token"**
- This error can appear if you mistakenly used DefaultAzureCredential instead of InteractiveBrowserCredential
- Switch to InteractiveBrowserCredential for GUI apps

**Browser popup doesn't appear:**
- Check firewall settings
- Ensure you're not running in a restricted environment
- Try running the app as administrator

**Login fails:**
- Verify you have the correct role assigned (Cognitive Services OpenAI User)
- Check that your Azure account has access to the subscription
- Verify the endpoint URL is correct

### DefaultAzureCredential Issues

**Error: "No response received from the managed identity endpoint"**
- You're not running on Azure, and `az login` hasn't been run
- Run `az login` in your terminal

**Error: "Azure CLI not installed"**
- Install Azure CLI: https://aka.ms/installazurecliwindows
- After installation, run `az login`

### API Key Issues

**Error: "Access denied" or "401 Unauthorized"**
- Verify the API key is correct
- Check if the key has been rotated
- Ensure the endpoint URL is correct

## Example Configurations

### Desktop GUI App (Recommended)
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "InteractiveBrowserCredential",
        "Endpoint": "https://my-openai.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

### Local Development with Azure CLI
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "DefaultAzureCredential",
        "Endpoint": "https://my-openai.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

### Quick Testing with API Key
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "ApiKey",
        "Endpoint": "https://my-openai.openai.azure.com/",
        "ApiKey": "abc123...",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

## Reasoning Models Configuration

### What are Reasoning Models?

Reasoning models (GPT-5 series, o1, o3, o4-mini, etc.) are advanced models designed for complex problem-solving, code generation, and analytical tasks. They use a different API parameter structure than traditional models.

### Model Identification

**Reasoning Models** (require `IsReasoningModel: true`):
- GPT-5 series: `gpt-5`, `gpt-5-mini`, `gpt-5-pro`, `gpt-5-nano`, `gpt-5-codex`
- O-series: `o1`, `o1-mini`, `o3`, `o3-mini`, `o3-pro`, `o4-mini`, `codex-mini`

**Traditional Models** (default `IsReasoningModel: false`):
- GPT-4 series: `gpt-4`, `gpt-4-turbo`, `gpt-4o`, `gpt-4o-mini`
- GPT-3.5 series: `gpt-3.5-turbo`

### Configuration for Reasoning Models

When using reasoning models, you must set `IsReasoningModel` to `true`:

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "InteractiveBrowserCredential",
        "Endpoint": "https://my-openai.openai.azure.com/",
        "DeploymentName": "gpt-5-2025-08-07",
        "ApiVersion": "2025-01-01-preview",
        "IsReasoningModel": true
      }
    }
  }
}
```

### API Version Requirements

Reasoning models typically require newer API versions:
- **Minimum for GPT-5/o-series**: `2024-10-21` or later
- **Recommended**: `2025-01-01-preview` or later for full feature support

### What Changes Internally?

When `IsReasoningModel` is `true`, the SDK internally uses `max_completion_tokens` instead of `max_tokens` in the API request. This is transparent to your code - you still use the same `MaxTokens` configuration, and the SDK handles the mapping automatically.

### Common Errors

**Error: "Unsupported parameter: 'max_tokens' is not supported with this model"**
- **Cause**: Using a reasoning model without setting `IsReasoningModel: true`
- **Solution**: Add `"IsReasoningModel": true` to your AzureOpenAI configuration

**Error: "DefaultAzureCredential failed to retrieve a token"**
- **Cause**: Mistakenly used `DefaultAzureCredential` when you wanted interactive browser login
- **Solution**: Switch to `InteractiveBrowserCredential` for GUI apps

### Complete Example: GPT-5 with OAuth

```json
{
  "TransparentAiAgent": {
    "Agent": {
      "SystemPrompt": "You are a helpful AI assistant.",
      "ContextWindowSize": 20
    },
    "LLM": {
      "Provider": "AzureOpenAI",
      "Temperature": 0.7,
      "TopP": 1.0,
      "MaxTokens": 4096,
      "AzureOpenAI": {
        "AuthenticationMode": "InteractiveBrowserCredential",
        "Endpoint": "https://my-resource.openai.azure.com/",
        "DeploymentName": "gpt-5-2025-08-07",
        "ApiVersion": "2025-01-01-preview",
        "IsReasoningModel": true
      }
    }
  }
}
```

## SDK Information

This project uses **Azure.AI.OpenAI SDK 2.5.0-beta.1**, which provides:
- Support for reasoning models (GPT-5, o-series)
- Interactive browser authentication
- Automatic parameter mapping (max_tokens vs max_completion_tokens)
- Latest Azure OpenAI API features

## References

- [Azure OpenAI Authentication Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/managed-identity)
- [DefaultAzureCredential Documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [InteractiveBrowserCredential Documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.interactivebrowsercredential)
- [Azure RBAC Roles](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/role-based-access-control)
