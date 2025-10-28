# Azure OpenAI Authentication Research

**Date**: 2025-10-28
**Researched By**: Claude Code
**Purpose**: Investigate OAuth/Microsoft Entra ID authentication support vs API key authentication

---

## Executive Summary

**Current State**: ❌ **OAuth/DefaultAzureCredential NOT Supported**

The codebase currently **ONLY** supports **API key authentication** for Azure OpenAI. OAuth-based authentication using Microsoft Entra ID (formerly Azure Active Directory) with `DefaultAzureCredential` is **not implemented**.

**Your Other Setup**: The interactive login popup you described is Microsoft Entra ID authentication using `DefaultAzureCredential` or similar token-based authentication.

---

## Current Implementation Analysis

### What's Implemented

**File**: `TransparentAiAgentCore/Infrastructure/LLM/AzureOpenAIProvider.cs:27-44`

```csharp
public AzureOpenAIProvider(
    IAuthenticationProvider authProvider,
    string deploymentName,
    ITransparencyService transparencyService)
{
    // ...
    var endpoint = authProvider.GetEndpoint("AzureOpenAI");
    var apiKey = authProvider.GetApiKey("AzureOpenAI");

    _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    //                                            ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                                            ONLY supports API key
}
```

**Key Points**:
- Uses `AzureKeyCredential(apiKey)` constructor
- Requires static API key from configuration
- No support for `TokenCredential`

### Configuration Structure

**File**: `TransparentAiAgentCore/Domain/Configuration/AzureOpenAIConfiguration.cs:5-29`

```csharp
public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;  // ← Only API key
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";

    public void Validate()
    {
        // Validation requires ApiKey to be present
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("Azure OpenAI ApiKey cannot be null or whitespace");
        // ...
    }
}
```

**Current appsettings.json structure**:
```json
{
  "TransparentAiAgent": {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "Endpoint": "https://your-resource.openai.azure.com/",
        "ApiKey": "your-api-key-here",
        "DeploymentName": "gpt-4",
        "ApiVersion": "2024-02-15-preview"
      }
    }
  }
}
```

### Package Dependencies

**File**: `TransparentAiAgentCore/TransparentAiAgentCore.csproj:11`

```xml
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />
```

**Missing Package**:
- ❌ `Azure.Identity` - NOT installed (required for `DefaultAzureCredential`)

---

## Azure OpenAI Authentication Methods

Microsoft Azure OpenAI supports two primary authentication methods:

### Method 1: API Key Authentication (Currently Implemented)

**How It Works**:
- Static API key stored in configuration
- Key passed in `api-key` HTTP header on every request
- Keys obtained from Azure Portal → OpenAI Resource → Keys and Endpoint

**Pros**:
- ✅ Simple to implement
- ✅ Works immediately with no Azure AD setup
- ✅ Good for development and testing

**Cons**:
- ⚠️ **Security risk**: Keys are long-lived and static
- ⚠️ **Rotation complexity**: Manual key rotation required
- ⚠️ **No RBAC**: Can't use Azure role-based access control
- ⚠️ **Exposure risk**: Keys can be accidentally committed to source control
- ⚠️ **No least privilege**: Key gives full access to resource

**Microsoft's Position**:
> "Keys aren't the recommended authentication option because they don't follow the principle of least privilege and can accidentally be checked into source control or stored in unsafe locations."

### Method 2: Microsoft Entra ID (OAuth) - NOT Implemented

**How It Works**:
- Uses Azure Active Directory tokens (OAuth 2.0)
- `DefaultAzureCredential` automatically discovers credentials:
  1. **Local Development**: Uses `az login` credentials
  2. **Azure-Hosted**: Uses Managed Identity
  3. **CI/CD**: Uses Service Principal or Workload Identity
- Tokens are short-lived and automatically refreshed
- Interactive browser popup for user authentication (local dev)

**Pros**:
- ✅ **Security**: Short-lived tokens (1 hour typical), auto-refresh
- ✅ **No secrets in config**: No API keys to manage
- ✅ **RBAC**: Fine-grained role assignment (e.g., "Cognitive Services OpenAI User")
- ✅ **Audit trail**: Azure AD logs all authentication events
- ✅ **Managed Identity**: Works seamlessly with Azure-hosted apps
- ✅ **Zero credential storage**: No secrets in source control or config files
- ✅ **Production-ready**: Microsoft's recommended approach

**Cons**:
- ⚠️ More complex initial setup
- ⚠️ Requires Azure AD role assignments
- ⚠️ Requires `az login` for local development

**Microsoft's Position**:
> "A secure, keyless authentication approach is to use Microsoft Entra ID with the Azure Identity library."

---

## SDK Support Analysis

### Azure.AI.OpenAI SDK v1.0.0-beta.12 (Current)

**Status**: ✅ Supports both authentication methods

The `OpenAIClient` class supports multiple constructors:

```csharp
// API Key (currently used)
OpenAIClient client = new OpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey));

// TokenCredential (NOT currently used)
OpenAIClient client = new OpenAIClient(
    new Uri(endpoint),
    new DefaultAzureCredential());
```

**Note**: Beta version, predates stable API versioning.

### Azure.AI.OpenAI SDK v2.1.0 (Latest Stable - December 2024)

**Status**: ✅ Full support for both methods with new API

Microsoft released stable version 2.0.0 in October 2024, with 2.1.0 following in December 2024.

**New Client API** (Breaking Changes):
```csharp
// API Key
AzureOpenAIClient azureClient = new AzureOpenAIClient(
    new Uri("https://your-resource.openai.azure.com"),
    new ApiKeyCredential(apiKey));
ChatClient chatClient = azureClient.GetChatClient("my-deployment");

// TokenCredential (Recommended)
AzureOpenAIClient azureClient = new AzureOpenAIClient(
    new Uri("https://your-resource.openai.azure.com"),
    new DefaultAzureCredential());
ChatClient chatClient = azureClient.GetChatClient("my-deployment");
```

**Key Changes from Beta**:
- `OpenAIClient` → `AzureOpenAIClient` (name change)
- `AzureKeyCredential` → `ApiKeyCredential` (rename)
- API version: 2024-10-21 (latest stable)
- Must explicitly call `GetChatClient(deploymentName)` to get chat-specific client

---

## What Your Other Setup Likely Uses

Based on your description of "endpoint only" and "popup login with Microsoft account":

**Setup**: OAuth with `DefaultAzureCredential`

**Configuration** (likely):
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    // NO ApiKey - relies on Azure AD authentication
    "DeploymentName": "gpt-4"
  }
}
```

**Code** (likely):
```csharp
using Azure.Identity;

var credential = new DefaultAzureCredential();
OpenAIClient client = new OpenAIClient(
    new Uri(endpoint),
    credential);
```

**Authentication Flow**:
1. Run `az login` in terminal (one-time setup)
2. Browser opens for Microsoft account login
3. Credentials cached locally
4. `DefaultAzureCredential` automatically uses cached credentials
5. No API key needed in configuration

**Azure Role Assignment**:
```bash
# You or admin ran something like:
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee your-email@company.com \
  --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{openai-resource}
```

---

## Implementation Gap Analysis

### What Needs to Change

To support OAuth authentication, the following changes are required:

#### 1. Add Azure.Identity Package

**File**: `TransparentAiAgentCore/TransparentAiAgentCore.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />
  <PackageReference Include="Azure.Identity" Version="1.12.0" />  <!-- ADD THIS -->
</ItemGroup>
```

#### 2. Extend Configuration Model

**Option A: Add Authentication Mode Selection**

**File**: `AzureOpenAIConfiguration.cs`

```csharp
public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;

    // NEW: Authentication method selector
    public string AuthenticationMode { get; set; } = "ApiKey"; // "ApiKey" | "DefaultAzureCredential"

    // Optional: Only required if AuthenticationMode = "ApiKey"
    public string? ApiKey { get; set; } = null;

    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";

    // Optional: For specific tenant
    public string? TenantId { get; set; } = null;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ConfigurationException("Azure OpenAI Endpoint cannot be null or whitespace");

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            throw new ConfigurationException("Azure OpenAI Endpoint must be a valid URI");

        // NEW: Conditional validation
        if (AuthenticationMode == "ApiKey")
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ConfigurationException(
                    "Azure OpenAI ApiKey is required when AuthenticationMode is 'ApiKey'");
        }
        else if (AuthenticationMode != "DefaultAzureCredential")
        {
            throw new ConfigurationException(
                $"Invalid AuthenticationMode: '{AuthenticationMode}'. Must be 'ApiKey' or 'DefaultAzureCredential'");
        }

        if (string.IsNullOrWhiteSpace(DeploymentName))
            throw new ConfigurationException("Azure OpenAI DeploymentName cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(ApiVersion))
            throw new ConfigurationException("Azure OpenAI ApiVersion cannot be null or whitespace");
    }
}
```

#### 3. Modify AzureOpenAIProvider Constructor

**File**: `AzureOpenAIProvider.cs`

```csharp
using Azure.Identity;
using Azure.Core;

public AzureOpenAIProvider(
    IAuthenticationProvider authProvider,
    string deploymentName,
    ITransparencyService transparencyService,
    AppConfiguration appConfig)  // NEW: Need config to check auth mode
{
    if (authProvider == null)
        throw new ArgumentNullException(nameof(authProvider));
    if (string.IsNullOrWhiteSpace(deploymentName))
        throw new ArgumentException("Deployment name cannot be null or whitespace", nameof(deploymentName));

    _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
    _deploymentName = deploymentName;

    var endpoint = authProvider.GetEndpoint("AzureOpenAI");
    var azureConfig = appConfig.LLM.AzureOpenAI;

    // NEW: Conditional client creation based on auth mode
    if (azureConfig.AuthenticationMode == "DefaultAzureCredential")
    {
        TokenCredential credential = string.IsNullOrWhiteSpace(azureConfig.TenantId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                TenantId = azureConfig.TenantId
            });

        _client = new OpenAIClient(new Uri(endpoint), credential);
    }
    else // ApiKey
    {
        var apiKey = authProvider.GetApiKey("AzureOpenAI");
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }
}
```

**Alternative**: Could create separate provider classes:
- `AzureOpenAIProviderApiKey`
- `AzureOpenAIProviderEntraId`

#### 4. Update Factory

**File**: `LLMProviderFactory.cs`

Update provider instantiation to pass `appConfig` to constructor.

#### 5. Update appsettings.json Structure

**Example 1: API Key Authentication (Current)**
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

**Example 2: DefaultAzureCredential (OAuth)**
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

**Example 3: DefaultAzureCredential with Specific Tenant**
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

---

## DefaultAzureCredential - How It Works

`DefaultAzureCredential` tries authentication methods in this order:

1. **EnvironmentCredential**: Environment variables (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`)
2. **WorkloadIdentityCredential**: Azure Kubernetes workload identity
3. **ManagedIdentityCredential**: System or user-assigned managed identity
4. **SharedTokenCacheCredential**: Cached credentials from developer tools
5. **VisualStudioCredential**: Visual Studio authentication
6. **VisualStudioCodeCredential**: VS Code Azure Account extension
7. **AzureCliCredential**: `az login` credentials ← **This is what shows popup**
8. **AzurePowerShellCredential**: Azure PowerShell
9. **AzureDeveloperCliCredential**: Azure Developer CLI

**Local Development** (your scenario):
- You run `az login`
- Browser popup asks for Microsoft account login
- Credentials cached in `~/.azure/`
- `DefaultAzureCredential` automatically finds and uses them
- No API key needed!

**Azure-Hosted Apps**:
- Enable Managed Identity on App Service / Container / VM
- Assign "Cognitive Services OpenAI User" role to the identity
- `DefaultAzureCredential` automatically uses Managed Identity
- No secrets in configuration!

---

## Setup Requirements for OAuth

### 1. Install Azure CLI (If Not Already)

```bash
# Windows
winget install Microsoft.AzureCLI

# Or download from: https://aka.ms/installazurecliwindows
```

### 2. Login to Azure

```bash
az login
```

This opens browser for interactive login (the popup you described).

### 3. Assign RBAC Role

You (or Azure admin) must assign one of these roles:

| Role | Role ID | Permissions |
|------|---------|-------------|
| Cognitive Services OpenAI User | `5e0bd9bd-7b93-4f28-af87-19fc36ad61bd` | Read, call inference APIs |
| Cognitive Services OpenAI Contributor | `a001fd3d-188f-4b5d-821b-7da978bf7442` | Full access including model management |

**Command**:
```bash
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee your-email@company.com \
  --scope /subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.CognitiveServices/accounts/{openai-resource-name}
```

**Or via Azure Portal**:
1. Navigate to your Azure OpenAI resource
2. Go to **Access Control (IAM)**
3. Click **Add role assignment**
4. Select **Cognitive Services OpenAI User**
5. Select your user or managed identity
6. Click **Save**

### 4. Configure Application

Update `appsettings.Development.json`:

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

**Notice**: No `ApiKey` field needed!

### 5. Run Application

```bash
cd TransparentAiAgentGui
dotnet run
```

Application will automatically use your `az login` credentials.

---

## Migration Path

If you want to add OAuth support, here's the recommended approach:

### Phase 1: Add OAuth Support (Keep API Key Working)

1. ✅ Add `Azure.Identity` package
2. ✅ Add `AuthenticationMode` field to `AzureOpenAIConfiguration`
3. ✅ Make `ApiKey` optional (nullable)
4. ✅ Update validation to check auth mode
5. ✅ Modify `AzureOpenAIProvider` constructor to support both modes
6. ✅ Update documentation with both examples
7. ✅ Test with both authentication modes

**Status**: Backward compatible - existing API key configs continue working.

### Phase 2: Consider SDK Update (Optional)

The codebase uses `Azure.AI.OpenAI 1.0.0-beta.12` (old beta).

**Latest**: `Azure.AI.OpenAI 2.1.0` (stable, December 2024)

**Breaking Changes**:
- `OpenAIClient` → `AzureOpenAIClient`
- `AzureKeyCredential` → `ApiKeyCredential`
- New API version: 2024-10-21
- Different client acquisition pattern

**Recommendation**: Implement Phase 1 with current SDK first, then consider upgrade separately.

---

## Security Comparison

| Aspect | API Key | DefaultAzureCredential |
|--------|---------|------------------------|
| **Token Lifetime** | Permanent (until rotated) | 1 hour (auto-refresh) |
| **Stored in Config** | Yes (security risk) | No |
| **Rotation** | Manual | Automatic |
| **RBAC** | No | Yes (fine-grained) |
| **Audit Logs** | Limited | Full Azure AD audit trail |
| **Revocation** | Regenerate key (downtime) | Instant (revoke role) |
| **Least Privilege** | No (full resource access) | Yes (assign minimal role) |
| **Source Control Risk** | High (key in config) | None (no secrets) |
| **Managed Identity** | N/A | Full support |
| **Microsoft Recommendation** | Not recommended | Recommended |

**Microsoft's Guidance**:
> "For production deployments, it's recommended to use Microsoft Entra ID authentication instead of key-based authentication for enhanced security."

---

## Recommendation

### For Your Situation

Based on your description:

1. **Your Other Setup**: Definitely using OAuth/`DefaultAzureCredential`
2. **This Codebase**: Currently only supports API key
3. **Missing Implementation**: OAuth support needs to be added

### Priority

- **High Priority**: If you want to:
  - Use same auth method across environments
  - Improve security posture
  - Avoid managing API keys
  - Use in production

- **Low Priority**: If you want to:
  - Keep it simple for now
  - Focus on other features
  - Add later when needed

### Effort Estimate

**Small Changes Required**:
- Add 1 NuGet package
- Modify 3-4 files
- Add ~50 lines of code
- Update configuration examples

**Time**: 1-2 hours implementation + testing

### Next Steps

**Option 1: Implement OAuth Support Now**

1. Add `Azure.Identity` package
2. Extend configuration model
3. Modify provider constructor
4. Update documentation
5. Test both auth modes

**Option 2: Document Current Limitation**

1. Update `CONFIGURATION_SETUP.md` to note API key requirement
2. Create GitHub issue to track OAuth feature request
3. Implement in future phase (e.g., Phase 7 - Security Enhancements)

---

## References

### Microsoft Documentation

- [Authenticate to Azure OpenAI using .NET](https://learn.microsoft.com/en-us/dotnet/ai/azure-ai-services-authentication)
- [Azure OpenAI Client Library for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme)
- [Azure OpenAI Managed Identity Guide](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/how-to/managed-identity)
- [DefaultAzureCredential Documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)

### SDK Repositories

- [Azure SDK for .NET - OpenAI](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI)
- [Azure.AI.OpenAI Changelog](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/CHANGELOG.md)

### NuGet Packages

- [Azure.AI.OpenAI 2.1.0 (Latest Stable)](https://www.nuget.org/packages/Azure.AI.OpenAI)
- [Azure.Identity 1.12.0](https://www.nuget.org/packages/Azure.Identity)

---

## Appendix: Code Example for Full Implementation

### Minimal OAuth Implementation

**1. Add Package**
```xml
<PackageReference Include="Azure.Identity" Version="1.12.0" />
```

**2. Update Configuration**
```csharp
public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string AuthenticationMode { get; set; } = "ApiKey"; // "ApiKey" | "DefaultAzureCredential"
    public string? ApiKey { get; set; } = null;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";
    public string? TenantId { get; set; } = null;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ConfigurationException("Endpoint required");

        if (AuthenticationMode == "ApiKey" && string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("ApiKey required when AuthenticationMode is 'ApiKey'");

        if (AuthenticationMode != "ApiKey" && AuthenticationMode != "DefaultAzureCredential")
            throw new ConfigurationException($"Invalid AuthenticationMode: {AuthenticationMode}");

        // ... other validation
    }
}
```

**3. Update Provider**
```csharp
using Azure.Identity;

public AzureOpenAIProvider(
    IAuthenticationProvider authProvider,
    string deploymentName,
    ITransparencyService transparencyService,
    AppConfiguration config)
{
    _deploymentName = deploymentName;
    _transparencyService = transparencyService;

    var endpoint = new Uri(authProvider.GetEndpoint("AzureOpenAI"));
    var azureConfig = config.LLM.AzureOpenAI;

    if (azureConfig.AuthenticationMode == "DefaultAzureCredential")
    {
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(azureConfig.TenantId))
        {
            credentialOptions.TenantId = azureConfig.TenantId;
        }

        _client = new OpenAIClient(endpoint, new DefaultAzureCredential(credentialOptions));
    }
    else
    {
        var apiKey = authProvider.GetApiKey("AzureOpenAI");
        _client = new OpenAIClient(endpoint, new AzureKeyCredential(apiKey));
    }
}
```

**4. Update Factory Registration**
```csharp
// In LLMProviderFactory or Program.cs
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var factory = sp.GetRequiredService<LLMProviderFactory>();
    var config = sp.GetRequiredService<AppConfiguration>();  // Pass config
    return factory.CreateProvider(config);  // Pass to factory
});
```

---

## Conclusion

**Answer to Your Question**:

**Q**: "Do we have this other auth option available in codebase, and can we switch between them in config?"

**A**:
- ❌ **Not currently available** - only API key authentication is implemented
- ✅ **Can be added** - SDK supports it, requires small code changes (~50 lines)
- ✅ **Can make it switchable** - by adding `AuthenticationMode` config field

**Your Other Setup** is using OAuth (`DefaultAzureCredential`), which is:
- More secure (no static keys)
- Microsoft's recommended approach
- Requires role assignment but no API key
- Shows interactive login popup via `az login`

**Recommendation**: Add OAuth support (small effort, significant security benefit)

---

**Last Updated**: 2025-10-28
**SDK Version Researched**: Azure.AI.OpenAI 1.0.0-beta.12 (current) and 2.1.0 (latest stable)
**Status**: Implementation guide ready for Phase 7 or immediate implementation
