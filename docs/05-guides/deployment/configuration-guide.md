# Configuration Setup Guide

**TransparentAiAgent Configuration Instructions**

Last Updated: 2025-11-17
Current Phase: Phase 9 (Multi-Provider Support)

---

**⚠️ This Guide is Archived**

This guide documented the **legacy single-provider configuration** format which has been **removed** as of Phase 9.

**→ Use the Multi-Provider Configuration Instead:**
- **[LLM Provider Selector Guide](llm-provider-selector.md)** - Complete configuration guide
- **appsettings.Example.json** - Working configuration examples

**Why Multi-Provider is Better:**
- ✅ Configure multiple providers (Claude, Azure OpenAI, OpenAI)
- ✅ Switch providers via UI dropdown without restart
- ✅ Multi-region failover
- ✅ Different authentication modes per provider
- ✅ Cleaner, more flexible configuration structure

---

## Legacy Documentation (Archived)

The content below is preserved for historical reference but **no longer works** with the current version.

<details>
<summary>Click to expand legacy documentation</summary>

### Minimum Required Configuration (LEGACY - NO LONGER SUPPORTED)

To use TransparentAiAgent, you need to configure at least one LLM provider. Currently supported:
- ✅ **Azure OpenAI** (Phase 2)
- ⏳ **Anthropic** (Coming in Phase 8)

---

## Configuration Files

The application uses standard ASP.NET Core configuration with JSON files:

| File | Purpose | When Used |
|------|---------|-----------|
| `appsettings.json` | Base configuration template | Always loaded |
| `appsettings.Development.json` | Development environment overrides | When running in Development mode |
| `appsettings.Production.json` | Production environment overrides | When deployed to production |
| `appsettings.Example.json` | Fully commented reference | Copy from this for examples |

**Configuration Priority** (later overrides earlier):
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables (future)
4. User secrets (future)

---

## Step-by-Step Setup

### Azure OpenAI Authentication Methods

Azure OpenAI supports two authentication methods:

| Method | Security | Setup Complexity | Recommended For |
|--------|----------|------------------|----------------|
| **API Key** | Lower (static key) | Simple | Development, testing |
| **DefaultAzureCredential (OAuth)** | Higher (short-lived tokens) | Moderate | Production, secure environments |

**Choose one of the following options:**

---

### Option 1: Azure OpenAI with API Key

#### Prerequisites
1. Azure subscription
2. Azure OpenAI resource created
3. Model deployed (e.g., GPT-4, GPT-3.5-turbo)

#### Step 1: Get Your Azure OpenAI Credentials

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Click **"Keys and Endpoint"** in the left menu
4. Note down:
   - **Endpoint** (e.g., `https://your-resource.openai.azure.com/`)
   - **Key 1** or **Key 2** (your API key)
5. Go to **Azure OpenAI Studio** → **Deployments**
6. Note down your **Deployment Name** (e.g., `gpt-4`, `gpt-35-turbo`)

#### Step 2: Configure appsettings.Development.json

Open `TransparentAiAgentGui/appsettings.Development.json` and fill in your credentials:

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "ApiKey",
        "Endpoint": "https://your-resource.openai.azure.com/",
        "ApiKey": "your-actual-api-key-here",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

**Important**:
- ⚠️ **Never commit API keys to git!** Add `appsettings.Development.json` to `.gitignore`
- ✅ Keep `appsettings.json` with empty values as a template
- 📝 `AuthenticationMode` defaults to `ApiKey` if omitted (backward compatible)

#### Step 3: Verify Configuration

Run the application:

```bash
cd TransparentAiAgentGui
dotnet run
```

**Expected Console Output** (if configured correctly):
```
✓ LLM Provider configured: AzureOpenAI
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

**Warning Output** (if not configured):
```
Warning: LLM configuration is invalid: AzureOpenAI configuration is required when Provider is AzureOpenAI
The app will start but LLM features will not be available.
Please configure LLM settings in appsettings.json to use agent features.
⚠ LLM not configured. Agent will show configuration error when used.
  Configure LLM settings in appsettings.json to enable agent features.
  See docs/CONFIGURATION_SETUP.md for instructions.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

**Important**: The app **will start successfully** even without LLM configuration. When you try to send a message in the UI, you'll see a helpful error message directing you to configure the LLM settings.

---

### Option 2: Azure OpenAI with DefaultAzureCredential (OAuth)

**Recommended for production environments!** This method uses Microsoft Entra ID (formerly Azure AD) for authentication with short-lived tokens.

#### Prerequisites
1. Azure subscription
2. Azure OpenAI resource created
3. Model deployed (e.g., GPT-4, GPT-3.5-turbo)
4. Azure CLI installed
5. **Azure RBAC role assigned** (see below)

#### Step 1: Install Azure CLI (if not already installed)

**Windows:**
```bash
winget install Microsoft.AzureCLI
```

Or download from: https://aka.ms/installazurecliwindows

**Verify installation:**
```bash
az --version
```

#### Step 2: Login to Azure

```bash
az login
```

This opens a browser for interactive Microsoft login (the popup you mentioned). Your credentials are cached locally.

#### Step 3: Assign Azure RBAC Role

You (or your Azure admin) must assign the **Cognitive Services OpenAI User** role to your user account or managed identity.

**Via Azure Portal:**
1. Navigate to your Azure OpenAI resource
2. Go to **Access Control (IAM)**
3. Click **Add role assignment**
4. Select **Cognitive Services OpenAI User**
5. Select your user account
6. Click **Save**

**Via Azure CLI:**
```bash
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee your-email@company.com \
  --scope /subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.CognitiveServices/accounts/{openai-resource-name}
```

**Available Roles:**
- `Cognitive Services OpenAI User` - Read and call inference APIs (recommended)
- `Cognitive Services OpenAI Contributor` - Full access including model management

#### Step 4: Configure appsettings.Development.json

Open `TransparentAiAgentGui/appsettings.Development.json`:

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

**Notice**: No `ApiKey` field needed! Authentication uses your Azure CLI login.

**Optional: Specify Tenant ID** (multi-tenant scenarios):
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "AuthenticationMode": "DefaultAzureCredential",
        "Endpoint": "https://your-resource.openai.azure.com/",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "TenantId": "12345678-1234-1234-1234-123456789012"
      }
    }
  }
}
```

#### Step 5: Run Application

```bash
cd TransparentAiAgentGui
dotnet run
```

**Expected Console Output**:
```
✓ LLM Provider configured: AzureOpenAI
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

The application automatically uses your `az login` credentials. No API key in configuration!

#### How DefaultAzureCredential Works

`DefaultAzureCredential` tries authentication methods in this order:
1. **Environment variables** (service principal)
2. **Workload Identity** (Azure Kubernetes)
3. **Managed Identity** (Azure-hosted apps - App Service, Container Apps, VMs)
4. **Azure CLI** (local development - `az login`) ← This is what you're using
5. **Visual Studio** / **VS Code** credentials

**For Production (Azure-hosted):**
Enable Managed Identity on your App Service/Container and assign the role. No secrets needed!

---

### Option 3: Anthropic Claude (Phase 8 - Not Yet Available)

This will be available in Phase 8. Configuration structure:

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "Anthropic",
      "Anthropic": {
        "ApiKey": "sk-ant-your-api-key-here",
        "Model": "claude-3-sonnet-20240229"
      }
    }
  }
}
```

---

## Complete Configuration Reference

### Full Configuration Structure

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
        "AuthenticationMode": "ApiKey",
        "Endpoint": "https://your-resource.openai.azure.com/",
        "ApiKey": "your-api-key-here",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview",
        "TenantId": null
      },
      "Anthropic": {
        "ApiKey": "sk-ant-your-key-here",
        "Model": "claude-3-sonnet-20240229"
      }
    },
    "MCP": {
      "Enabled": false,
      "ServerPaths": []
    }
  }
}
```

---

## Configuration Options Explained

### Agent Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `SystemPrompt` | string | "You are a helpful AI assistant." | System prompt sent to LLM at conversation start |
| `ContextWindowSize` | int | 20 | Max messages in LLM context (older messages truncated but visible in UI) |

**Example Custom System Prompt**:
```json
"SystemPrompt": "You are an expert software developer specializing in C# and .NET. Provide clear, concise answers with code examples."
```

---

### LLM Settings (Global)

| Setting | Type | Default | Range | Description |
|---------|------|---------|-------|-------------|
| `Provider` | string | "AzureOpenAI" | "AzureOpenAI" \| "Anthropic" | Which LLM provider to use |
| `Temperature` | double | 0.7 | 0.0 - 2.0 | Response randomness (0=deterministic, 2=very random) |
| `TopP` | double | 1.0 | 0.0 - 1.0 | Nucleus sampling threshold |
| `MaxTokens` | int | 4096 | 1+ | Maximum tokens in response |

**Temperature Guidelines**:
- `0.0 - 0.3`: Focused, deterministic (good for code, facts)
- `0.4 - 0.7`: Balanced creativity and consistency (recommended)
- `0.8 - 1.5`: Creative, varied responses (good for brainstorming)
- `1.6 - 2.0`: Very creative, less predictable

---

### Azure OpenAI Settings

| Setting | Type | Required | Default | Description |
|---------|------|----------|---------|-------------|
| `AuthenticationMode` | enum | No | `ApiKey` | Authentication method: `ApiKey` or `DefaultAzureCredential` |
| `Endpoint` | string | ✅ Yes | - | Azure OpenAI resource endpoint URL |
| `ApiKey` | string | Conditional | - | API key (required if `AuthenticationMode` is `ApiKey`) |
| `DeploymentName` | string | ✅ Yes | - | Name of your deployed model |
| `ApiVersion` | string | ✅ Yes | `"2024-02-15-preview"` | API version |
| `TenantId` | string | No | - | Azure AD tenant ID (optional, for `DefaultAzureCredential`) |

**AuthenticationMode Values**:
- `ApiKey` - Use static API key (default for backward compatibility)
- `DefaultAzureCredential` - Use OAuth/Microsoft Entra ID authentication

**Where to Find**:
- **Endpoint**: Azure Portal → Your OpenAI Resource → Keys and Endpoint
- **ApiKey** (if using): Azure Portal → Your OpenAI Resource → Keys and Endpoint
- **DeploymentName**: Azure OpenAI Studio → Deployments tab
- **ApiVersion**: Use `2024-02-15-preview` (latest stable)
- **TenantId** (optional): Azure Portal → Azure Active Directory → Overview

**Valid Endpoint Format**:
```
https://{your-resource-name}.openai.azure.com/
```

---

### Anthropic Settings (Phase 8)

| Setting | Type | Required | Description |
|---------|------|----------|-------------|
| `ApiKey` | string | ✅ Yes | Anthropic API key |
| `Model` | string | ✅ Yes | Model identifier |

**Available Models** (as of Phase 8):
- `claude-3-opus-20240229` - Most capable
- `claude-3-sonnet-20240229` - Balanced (recommended)
- `claude-3-haiku-20240307` - Fastest, most economical

---

### MCP Settings (Phase 5+)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Enabled` | bool | false | Enable MCP tool integration |
| `ServerPaths` | string[] | [] | Paths to MCP server executables |

**Note**: MCP features are not yet implemented (coming in Phase 5).

---

## App Behavior Without Configuration

**The app starts successfully even without LLM configuration.** This is intentional design to allow:
- Configuration directly in the UI (future feature)
- Deployment before credentials are available
- Testing without API usage

**What Happens**:
1. ✅ App starts and web UI loads normally
2. ✅ You can navigate to `http://localhost:5000`
3. ✅ Chat interface is visible
4. ⚠️ When you send a message, you'll see this error:

```
LLM is not configured. Please configure Azure OpenAI or Anthropic settings in appsettings.json.
See docs/CONFIGURATION_SETUP.md for instructions.
```

**To Fix**: Configure LLM settings following the steps in this document.

---

## Troubleshooting

### App Starts But Shows Warning

**Symptom**:
```
⚠ LLM not configured. Agent will show configuration error when used.
  Configure LLM settings in appsettings.json to enable agent features.
```

**Solutions**:
1. ✅ Check that `appsettings.Development.json` exists
2. ✅ Verify configuration structure matches examples above
3. ✅ Ensure all required fields are filled (Endpoint, ApiKey, DeploymentName)
4. ✅ Check for JSON syntax errors (trailing commas, quotes, brackets)

### App Crashes on Startup with DI Exception

**Symptom**:
```
System.AggregateException: Some services are not able to be constructed
Unable to resolve service for type 'IAgentOrchestrator'...
```

**Status**: ✅ **Fixed in latest version**

**This should no longer happen**. The app now uses `NotConfiguredAgentOrchestrator` stub when LLM is not configured, allowing the app to start successfully.

If you still see this error:
1. Pull the latest code changes
2. Verify `Program.cs` has the conditional `isLLMConfigured` logic (lines 77-98)
3. Verify `NotConfiguredAgentOrchestrator.cs` exists in Core project
4. Run `dotnet build` to ensure clean build

### Invalid JSON Error

**Symptom**:
```
Warning: Could not load configuration from appsettings.json: Invalid JSON...
```

**Solutions**:
1. Validate JSON syntax using [JSONLint](https://jsonlint.com)
2. Check for:
   - Missing or extra commas
   - Unmatched brackets `{}` or `[]`
   - Unquoted strings
   - Comments (use `appsettings.Example.json` for commented version)
3. Compare your file to `appsettings.Example.json`

### LLM API Errors

**Symptom**: App starts fine but errors when sending messages

**Common Causes**:
1. **Invalid API Key**: Check key is copied correctly, no extra spaces
2. **Wrong Endpoint**: Ensure URL matches your Azure resource
3. **Deployment Not Found**: Verify deployment name matches Azure OpenAI Studio
4. **Quota Exceeded**: Check Azure OpenAI resource quotas and limits
5. **Wrong API Version**: Try `2024-02-15-preview` or latest from [Azure docs](https://learn.microsoft.com/azure/ai-services/openai/reference)

---

## Security Best Practices

### Development

1. ✅ **Use appsettings.Development.json for secrets**
2. ✅ **Add to .gitignore**:
   ```gitignore
   appsettings.Development.json
   appsettings.Production.json
   ```
3. ✅ **Keep appsettings.json as empty template** (commit this)

### Production (Future)

1. ⏳ Use Azure Key Vault (Phase 7+)
2. ⏳ Use environment variables
3. ⏳ Use ASP.NET Core User Secrets for local dev
4. ⏳ Rotate API keys regularly

---

## Configuration Loading Order

The application loads configuration in this order:

1. **Default in-memory config** (hardcoded defaults in `AppConfiguration.cs`)
2. **appsettings.json** (if exists, overrides defaults)
3. **appsettings.{Environment}.json** (if exists, overrides previous)

**Implementation** (Program.cs:30-44):
```csharp
try
{
    var configPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
    if (File.Exists(configPath))
    {
        var loadedConfig = configService.LoadConfiguration(configPath);
        appConfig = loadedConfig;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not load configuration...");
}
```

**Validation** (Program.cs:48-60):
```csharp
bool isLLMConfigured = false;
try
{
    appConfig.LLM.Validate();
    isLLMConfigured = true;
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: LLM configuration is invalid: {ex.Message}");
}
```

**Conditional Registration** (Program.cs:77-92):
```csharp
if (isLLMConfigured)
{
    builder.Services.AddSingleton<ILLMProvider>(...);
    builder.Services.AddSingleton<IAgentOrchestrator>(...);
}
```

---

## Example Configurations

### Minimal Azure OpenAI with API Key

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "AzureOpenAI": {
        "Endpoint": "https://my-resource.openai.azure.com/",
        "ApiKey": "abc123...",
        "DeploymentName": "gpt-4"
      }
    }
  }
}
```

All other settings will use defaults (including `AuthenticationMode: ApiKey`).

### Minimal Azure OpenAI with OAuth

```json
{
  "TransparentAiAgent": {
    "LLM": {
      "AzureOpenAI": {
        "AuthenticationMode": "DefaultAzureCredential",
        "Endpoint": "https://my-resource.openai.azure.com/",
        "DeploymentName": "gpt-4"
      }
    }
  }
}
```

No API key needed! Requires `az login` and RBAC role assignment.

### Custom Agent Configuration

```json
{
  "TransparentAiAgent": {
    "Agent": {
      "SystemPrompt": "You are a coding tutor. Explain concepts clearly with examples.",
      "ContextWindowSize": 30
    },
    "LLM": {
      "Temperature": 0.5,
      "MaxTokens": 2048,
      "AzureOpenAI": {
        "Endpoint": "https://my-resource.openai.azure.com/",
        "ApiKey": "abc123...",
        "DeploymentName": "gpt-35-turbo"
      }
    }
  }
}
```

### Production Configuration (Template)

```json
{
  "TransparentAiAgent": {
    "Agent": {
      "SystemPrompt": "Production system prompt here",
      "ContextWindowSize": 20
    },
    "LLM": {
      "Provider": "AzureOpenAI",
      "Temperature": 0.7,
      "TopP": 1.0,
      "MaxTokens": 4096,
      "AzureOpenAI": {
        "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
        "ApiKey": "${AZURE_OPENAI_KEY}",
        "DeploymentName": "${AZURE_OPENAI_DEPLOYMENT}",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

**Note**: Environment variable substitution coming in later phase.

---

## Testing Your Configuration

### Method 1: Console Output

When you run `dotnet run`, check the console:

**✅ Success**:
```
✓ LLM Provider configured: AzureOpenAI
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

**⚠️ Warning** (app works but LLM unavailable):
```
Warning: LLM configuration is invalid: Azure OpenAI API key not configured
⚠ LLM services not registered. Configure appsettings.json to enable agent features.
```

### Method 2: Use the Application

1. Navigate to `http://localhost:5000`
2. Try sending a message in the chat UI
3. If configured correctly, you should get an LLM response

---

## Configuration File Locations

```
TransparentAiAgentGui/
├── appsettings.json              ← Base template (commit to git)
├── appsettings.Development.json  ← Your dev config (add to .gitignore)
├── appsettings.Production.json   ← Production config (add to .gitignore)
└── appsettings.Example.json      ← Fully commented reference (commit to git)
```

---

## Next Steps After Configuration

Once configured, you can:

1. ✅ **Start the application**: `dotnet run` in `TransparentAiAgentGui/`
2. ✅ **Open browser**: Navigate to `http://localhost:5000`
3. ✅ **Test chat**: Send messages and receive AI responses
4. ✅ **View transparency**: See context status, message history, etc.

---

## Future Configuration Features

These will be added in later phases:

| Feature | Phase | Description |
|---------|-------|-------------|
| Environment variables | Phase 7 | Override config with env vars |
| Azure Key Vault | Phase 7 | Secure secret storage |
| User Secrets | Phase 7 | Local development secrets |
| UI-based config | Phase 7 | Configure from web UI |
| MCP server config | Phase 5 | Configure MCP tool servers |
| Multi-provider | Phase 8 | Switch between providers dynamically |

---

## Related Documentation

- [START_HERE.md](START_HERE.md) - Project overview
- [DEPLOYMENT_TROUBLESHOOTING.md](DEPLOYMENT_TROUBLESHOOTING.md) - Running and debugging guide
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Development reference
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture

---

## Getting Help

### Configuration Issues

1. Check this document for troubleshooting section
2. Validate JSON syntax at [JSONLint](https://jsonlint.com)
3. Compare your config to `appsettings.Example.json`
4. Check console output for specific error messages

### Azure OpenAI Issues

1. Verify credentials in Azure Portal
2. Check deployment status in Azure OpenAI Studio
3. Review [Azure OpenAI documentation](https://learn.microsoft.com/azure/ai-services/openai/)
4. Check API version compatibility

---

**Last Updated**: 2025-10-28
**Current Phase**: Phase 4 (Basic UI Complete)
**Configuration Version**: 1.1 (OAuth support added)

---

</details>
