# Phase 7: Configuration UI - Detailed Plan

**Status**: Ready for Review
**Created**: 2025-11-02
**Phase**: 7 of 9
**Prerequisites**: Phases 1-6 complete (Full agent with tools and enhanced UI)

---

## Overview

This phase implements a comprehensive configuration UI that allows users to modify all agent settings in real-time without restarting the application. The focus is on **hot-reload** - making configuration changes take effect immediately while maintaining transparency.

### Goals

1. **Configuration UI**: Web-based interface for all settings
2. **Hot-Reload**: Changes apply immediately without restart
3. **Validation**: Prevent invalid configurations with clear feedback
4. **Persistence**: Save configurations to disk automatically
5. **Transparency**: All config changes logged in transparency system
6. **Security**: Handle API keys safely (masked display, secure storage)

### What's In Scope

✅ System prompt editing
✅ LLM parameter adjustment (temperature, max tokens, top-p, etc.)
✅ LLM provider switching (Azure OpenAI ↔ Anthropic)
✅ API key management (masked display, update capability)
✅ Tool/MCP server configuration (enable/disable, add/remove servers)
✅ Hot-reload for all settings
✅ Configuration validation with user feedback
✅ Persistence to appsettings.json

### What's Out of Scope

❌ Configuration presets/templates (future enhancement)
❌ Configuration history/versioning (future enhancement)
❌ Multi-user configuration profiles (future enhancement)
❌ Advanced tool parameter configuration (basic enable/disable only)

---

## Architecture Overview

### Hot-Reload Strategy

The key architectural challenge is **applying configuration changes without restart**. We solve this with targeted update mechanisms:

| Configuration Type | Hot-Reload Strategy | Complexity |
|-------------------|---------------------|------------|
| **System Prompt** | Update ConversationManager | Low |
| **LLM Parameters** | Update ConversationManager | Low |
| **LLM Provider** | Recreate ILLMProvider instance | Medium |
| **API Keys** | Recreate ILLMProvider instance | Medium |
| **MCP Servers** | Refresh IToolRegistry | Low |

### Key Architectural Principles

1. **Single Source of Truth**: `AppConfiguration` object holds all config
2. **Event-Driven**: Configuration changes emit transparency events
3. **Validation First**: No changes applied until validated
4. **Graceful Degradation**: Invalid configs don't crash, just show errors
5. **Persistence**: Auto-save after successful changes

### Layered Architecture

```
┌─────────────────────────────────────────────────┐
│  Presentation Layer (Blazor Components)         │
│  ┌───────────────────────────────────────────┐  │
│  │ ConfigurationOverview.razor               │  │
│  │  ├─ SystemPromptEditor.razor              │  │
│  │  ├─ LLMParametersEditor.razor             │  │
│  │  ├─ ProviderSettingsEditor.razor          │  │
│  │  └─ ToolsConfigurationEditor.razor        │  │
│  └───────────────────────────────────────────┘  │
└────────────────┬────────────────────────────────┘
                 │ HTTP (Minimal API)
┌────────────────▼────────────────────────────────┐
│  Application Layer (Configuration Service)      │
│  ┌───────────────────────────────────────────┐  │
│  │ ConfigurationService (NEW)                │  │
│  │  - Validates changes                      │  │
│  │  - Applies hot-reload                     │  │
│  │  - Persists to disk                       │  │
│  │  - Emits transparency events              │  │
│  └───────────────────────────────────────────┘  │
└────────────────┬────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────┐
│  Infrastructure Layer (Hot-Reload Handlers)     │
│  ┌───────────────────────────────────────────┐  │
│  │ ConversationManager                       │  │
│  │  - UpdateSystemPrompt()                   │  │
│  │  - UpdateLLMParameters()                  │  │
│  │                                           │  │
│  │ LLMProviderFactory (NEW)                  │  │
│  │  - RecreateProvider()                     │  │
│  │                                           │  │
│  │ ToolRegistry                              │  │
│  │  - RefreshAsync()                         │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## Detailed Component Design

### 1. Application Layer - Configuration Service

#### IConfigurationService Interface

**Location**: `TransparentAiAgentCore/Application/Configuration/IConfigurationService.cs`

```csharp
namespace TransparentAiAgentCore.Application.Configuration;

/// <summary>
/// Service for runtime configuration updates with hot-reload support.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Updates the system prompt. Changes apply to next conversation.
    /// </summary>
    Task UpdateSystemPromptAsync(string newPrompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates LLM parameters (temperature, max tokens, etc.).
    /// </summary>
    Task UpdateLLMParametersAsync(LLMParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches the LLM provider (requires recreation of ILLMProvider).
    /// </summary>
    Task UpdateProviderAsync(LLMProviderType provider, string apiKey, string? endpoint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates MCP server configuration (requires tool registry refresh).
    /// </summary>
    Task UpdateMCPServersAsync(List<MCPServerConfig> servers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    AppConfiguration GetCurrentConfiguration();

    /// <summary>
    /// Validates a configuration change without applying it.
    /// </summary>
    ConfigurationValidationResult Validate(ConfigurationChange change);
}
```

#### ConfigurationService Implementation

**Location**: `TransparentAiAgentCore/Application/Configuration/ConfigurationService.cs`

**Key Responsibilities**:
- Validate all configuration changes
- Apply hot-reload updates to active services
- Persist changes to appsettings.json
- Emit transparency events for all changes

**Dependencies**:
- `AppConfiguration` - Current config (singleton)
- `IConversationManager` - For system prompt/LLM param updates
- `ILLMProviderFactory` - For provider recreation
- `IToolRegistry` - For tool configuration updates
- `ITransparencyService` - For logging changes
- `IConfigurationManager` - For persistence

**Hot-Reload Methods**:

```csharp
public async Task UpdateSystemPromptAsync(string newPrompt, CancellationToken ct)
{
    // 1. Validate
    if (string.IsNullOrWhiteSpace(newPrompt))
        throw new ConfigurationException("System prompt cannot be empty");

    // 2. Update config
    var oldPrompt = _appConfig.Agent.SystemPrompt;
    _appConfig.Agent.SystemPrompt = newPrompt;

    // 3. Apply hot-reload
    _conversationManager.UpdateSystemPrompt(newPrompt);

    // 4. Emit transparency event
    await _transparency.LogEventAsync(new ConfigurationChangeEvent
    {
        ChangeType = "SystemPrompt",
        OldValue = oldPrompt,
        NewValue = newPrompt
    });

    // 5. Persist to disk
    await _configManager.SaveConfigurationAsync(_appConfig, ct);
}
```

#### ConfigurationValidator

**Location**: `TransparentAiAgentCore/Application/Configuration/ConfigurationValidator.cs`

**Validation Rules**:

```csharp
public class ConfigurationValidator
{
    public ConfigurationValidationResult ValidateSystemPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return ConfigurationValidationResult.Fail("System prompt cannot be empty");

        if (prompt.Length > 50000)
            return ConfigurationValidationResult.Fail("System prompt too long (max 50,000 characters)");

        return ConfigurationValidationResult.Success();
    }

    public ConfigurationValidationResult ValidateLLMParameters(LLMParameters params)
    {
        var errors = new List<string>();

        if (params.Temperature < 0.0 || params.Temperature > 2.0)
            errors.Add("Temperature must be between 0.0 and 2.0");

        if (params.MaxTokens < 1 || params.MaxTokens > 128000)
            errors.Add("MaxTokens must be between 1 and 128,000");

        if (params.TopP < 0.0 || params.TopP > 1.0)
            errors.Add("TopP must be between 0.0 and 1.0");

        return errors.Any()
            ? ConfigurationValidationResult.Fail(errors)
            : ConfigurationValidationResult.Success();
    }

    public ConfigurationValidationResult ValidateProviderSettings(
        LLMProviderType provider, string apiKey, string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return ConfigurationValidationResult.Fail("API key is required");

        // Provider-specific validation
        switch (provider)
        {
            case LLMProviderType.AzureOpenAI:
                if (string.IsNullOrWhiteSpace(endpoint))
                    return ConfigurationValidationResult.Fail("Azure endpoint is required");
                if (!Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
                    return ConfigurationValidationResult.Fail("Invalid Azure endpoint URL");
                break;

            case LLMProviderType.Anthropic:
                if (!apiKey.StartsWith("sk-ant-"))
                    return ConfigurationValidationResult.Fail("Invalid Anthropic API key format");
                break;
        }

        return ConfigurationValidationResult.Success();
    }
}
```

### 2. Infrastructure Layer - Hot-Reload Support

#### ConversationManager Updates

**Location**: `TransparentAiAgentCore/Application/Conversation/ConversationManager.cs` (existing file)

**New Methods**:

```csharp
/// <summary>
/// Updates the system prompt for future conversations.
/// Does not affect current conversation history.
/// </summary>
public void UpdateSystemPrompt(string newPrompt)
{
    if (string.IsNullOrWhiteSpace(newPrompt))
        throw new ArgumentException("System prompt cannot be empty", nameof(newPrompt));

    _systemPrompt = newPrompt;

    // Note: Current conversation history is NOT modified
    // New system prompt will be used when GetMessagesForLLM() is called next
}

/// <summary>
/// Updates LLM parameters for future LLM calls.
/// </summary>
public void UpdateLLMParameters(LLMParameters parameters)
{
    if (parameters == null)
        throw new ArgumentNullException(nameof(parameters));

    _llmParameters = parameters;

    // Parameters will be used in next LLM call via agent orchestrator
}
```

#### LLMProviderFactory

**Location**: `TransparentAiAgentCore/Infrastructure/LLM/LLMProviderFactory.cs` (existing file, needs updates)

**Current State**: Factory exists but doesn't support hot-swapping.

**New Method**:

```csharp
/// <summary>
/// Recreates the LLM provider with new configuration.
/// Used for hot-reload when provider settings change.
/// </summary>
public ILLMProvider RecreateProvider(LLMProviderType provider, string apiKey, string? endpoint = null)
{
    // Dispose old provider if needed
    if (_currentProvider is IDisposable disposable)
    {
        disposable.Dispose();
    }

    // Create new provider based on type
    ILLMProvider newProvider = provider switch
    {
        LLMProviderType.AzureOpenAI => new AzureOpenAIProvider(
            apiKey,
            endpoint ?? throw new ArgumentNullException(nameof(endpoint)),
            _appConfig.LLM.AzureOpenAI.DeploymentName,
            _transparencyService),

        LLMProviderType.Anthropic => new AnthropicProvider(
            apiKey,
            _appConfig.LLM.Anthropic.Model,
            _transparencyService),

        _ => throw new NotSupportedException($"Provider {provider} not supported")
    };

    _currentProvider = newProvider;
    return newProvider;
}
```

**Challenge**: How to update the DI container's singleton instance?

**Solution**: Use `IServiceProvider` scoping and accessor pattern:

```csharp
// In Program.cs, register a provider accessor
builder.Services.AddSingleton<LLMProviderAccessor>();
builder.Services.AddSingleton<ILLMProvider>(sp =>
    sp.GetRequiredService<LLMProviderAccessor>().CurrentProvider);

// The accessor holds a reference that can be updated
public class LLMProviderAccessor
{
    public ILLMProvider CurrentProvider { get; set; }
}

// ConfigurationService can update the accessor
public async Task UpdateProviderAsync(...)
{
    var newProvider = _providerFactory.RecreateProvider(provider, apiKey, endpoint);
    _providerAccessor.CurrentProvider = newProvider;

    // Update config and persist...
}
```

### 3. Configuration API Endpoints

**Location**: `TransparentAiAgentGui/Program.cs` (add to existing file)

**Endpoints**:

```csharp
// Add after app.MapRazorComponents<App>()...

app.MapGet("/api/config", (IConfigurationService configService) =>
{
    var config = configService.GetCurrentConfiguration();

    // Mask API keys before sending to client
    return new
    {
        Agent = new
        {
            config.Agent.SystemPrompt,
            config.Agent.ContextWindowSize,
            config.Agent.EnableTools
        },
        LLM = new
        {
            config.LLM.Provider,
            Parameters = config.LLM.Parameters,
            AzureOpenAI = new
            {
                config.LLM.AzureOpenAI.Endpoint,
                config.LLM.AzureOpenAI.DeploymentName,
                ApiKey = MaskApiKey(config.LLM.AzureOpenAI.ApiKey)
            },
            Anthropic = new
            {
                config.LLM.Anthropic.Model,
                ApiKey = MaskApiKey(config.LLM.Anthropic.ApiKey)
            }
        },
        MCP = new
        {
            config.MCP.AutoDiscoverTools,
            Servers = config.MCP.Servers
        }
    };
});

app.MapPut("/api/config/system-prompt", async (
    [FromBody] SystemPromptUpdateRequest request,
    IConfigurationService configService) =>
{
    try
    {
        await configService.UpdateSystemPromptAsync(request.SystemPrompt);
        return Results.Ok(new { success = true });
    }
    catch (ConfigurationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/config/llm-parameters", async (
    [FromBody] LLMParametersUpdateRequest request,
    IConfigurationService configService) =>
{
    try
    {
        await configService.UpdateLLMParametersAsync(request.Parameters);
        return Results.Ok(new { success = true });
    }
    catch (ConfigurationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/config/provider", async (
    [FromBody] ProviderUpdateRequest request,
    IConfigurationService configService) =>
{
    try
    {
        await configService.UpdateProviderAsync(
            request.Provider,
            request.ApiKey,
            request.Endpoint);
        return Results.Ok(new { success = true });
    }
    catch (ConfigurationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/config/mcp-servers", async (
    [FromBody] MCPServersUpdateRequest request,
    IConfigurationService configService) =>
{
    try
    {
        await configService.UpdateMCPServersAsync(request.Servers);
        return Results.Ok(new { success = true });
    }
    catch (ConfigurationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Helper method
string MaskApiKey(string? apiKey)
{
    if (string.IsNullOrEmpty(apiKey)) return "";
    if (apiKey.Length <= 8) return "***";
    return $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}";
}
```

### 4. UI Components

#### ConfigurationOverview.razor

**Location**: `TransparentAiAgentGui/Components/Configuration/ConfigurationOverview.razor`

**Purpose**: Main configuration page with tabbed interface.

**Structure**:

```razor
@page "/configuration"
@using TransparentAiAgentCore.Domain.Configuration

<PageTitle>Configuration</PageTitle>

<div class="configuration-overview">
    <h3>Agent Configuration</h3>

    <div class="configuration-tabs">
        <button class="tab @(ActiveTab == "system-prompt" ? "active" : "")"
                @onclick='() => ActiveTab = "system-prompt"'>
            System Prompt
        </button>
        <button class="tab @(ActiveTab == "llm-params" ? "active" : "")"
                @onclick='() => ActiveTab = "llm-params"'>
            LLM Parameters
        </button>
        <button class="tab @(ActiveTab == "provider" ? "active" : "")"
                @onclick='() => ActiveTab = "provider"'>
            Provider Settings
        </button>
        <button class="tab @(ActiveTab == "tools" ? "active" : "")"
                @onclick='() => ActiveTab = "tools"'>
            Tools & MCP
        </button>
    </div>

    <div class="configuration-content">
        @if (ActiveTab == "system-prompt")
        {
            <SystemPromptEditor />
        }
        else if (ActiveTab == "llm-params")
        {
            <LLMParametersEditor />
        }
        else if (ActiveTab == "provider")
        {
            <ProviderSettingsEditor />
        }
        else if (ActiveTab == "tools")
        {
            <ToolsConfigurationEditor />
        }
    </div>
</div>

@code {
    private string ActiveTab { get; set; } = "system-prompt";
}
```

#### SystemPromptEditor.razor

**Location**: `TransparentAiAgentGui/Components/Configuration/SystemPromptEditor.razor`

**Features**:
- Multi-line text editor
- Character count display
- Real-time validation
- Save/Cancel buttons
- Success/error notifications

**Key Code**:

```razor
<div class="system-prompt-editor">
    <label>System Prompt</label>
    <textarea @bind="EditedPrompt"
              @bind:event="oninput"
              rows="20"
              placeholder="Enter system prompt..."
              class="prompt-textarea @(HasValidationError ? "error" : "")">
    </textarea>

    <div class="editor-footer">
        <span class="character-count">
            @EditedPrompt.Length / 50,000 characters
        </span>

        @if (HasValidationError)
        {
            <span class="validation-error">@ValidationError</span>
        }
    </div>

    <div class="editor-actions">
        <button @onclick="SaveAsync"
                disabled="@(HasValidationError || !HasChanges || IsSaving)"
                class="btn-primary">
            @(IsSaving ? "Saving..." : "Save")
        </button>
        <button @onclick="Cancel"
                disabled="@IsSaving"
                class="btn-secondary">
            Cancel
        </button>
    </div>

    @if (ShowSuccess)
    {
        <div class="success-message">System prompt updated successfully!</div>
    }
</div>

@code {
    private string EditedPrompt { get; set; } = "";
    private string OriginalPrompt { get; set; } = "";
    private bool IsSaving { get; set; }
    private bool ShowSuccess { get; set; }
    private bool HasValidationError { get; set; }
    private string ValidationError { get; set; } = "";

    private bool HasChanges => EditedPrompt != OriginalPrompt;

    protected override async Task OnInitializedAsync()
    {
        var response = await Http.GetFromJsonAsync<ConfigResponse>("/api/config");
        OriginalPrompt = response.Agent.SystemPrompt;
        EditedPrompt = OriginalPrompt;
    }

    private async Task SaveAsync()
    {
        IsSaving = true;
        ShowSuccess = false;

        try
        {
            var response = await Http.PutAsJsonAsync("/api/config/system-prompt",
                new { SystemPrompt = EditedPrompt });

            if (response.IsSuccessStatusCode)
            {
                OriginalPrompt = EditedPrompt;
                ShowSuccess = true;

                // Hide success message after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    ShowSuccess = false;
                    StateHasChanged();
                });
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                HasValidationError = true;
                ValidationError = error.Error;
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void Cancel()
    {
        EditedPrompt = OriginalPrompt;
        HasValidationError = false;
        ValidationError = "";
        ShowSuccess = false;
    }
}
```

#### LLMParametersEditor.razor

**Location**: `TransparentAiAgentGui/Components/Configuration/LLMParametersEditor.razor`

**Features**:
- Sliders for Temperature, TopP
- Number input for MaxTokens
- Real-time validation
- Value labels showing current settings
- Save/Cancel buttons

**Key Parameters**:
- **Temperature** (0.0 - 2.0): Controls randomness
- **MaxTokens** (1 - 128000): Maximum response length
- **TopP** (0.0 - 1.0): Nucleus sampling threshold

**UI Design**:

```razor
<div class="llm-parameters-editor">
    <h4>LLM Parameters</h4>

    <div class="parameter-group">
        <label>
            Temperature: @Temperature.ToString("F2")
            <span class="hint">(0 = deterministic, 2 = very random)</span>
        </label>
        <input type="range"
               @bind="Temperature"
               @bind:event="oninput"
               min="0.0"
               max="2.0"
               step="0.1"
               class="slider" />
    </div>

    <div class="parameter-group">
        <label>
            Max Tokens: @MaxTokens
            <span class="hint">(Maximum response length)</span>
        </label>
        <input type="number"
               @bind="MaxTokens"
               @bind:event="oninput"
               min="1"
               max="128000"
               class="number-input" />
    </div>

    <div class="parameter-group">
        <label>
            Top-P: @TopP.ToString("F2")
            <span class="hint">(0 = narrow, 1 = wide selection)</span>
        </label>
        <input type="range"
               @bind="TopP"
               @bind:event="oninput"
               min="0.0"
               max="1.0"
               step="0.05"
               class="slider" />
    </div>

    <div class="editor-actions">
        <button @onclick="SaveAsync"
                disabled="@(!HasChanges || IsSaving)"
                class="btn-primary">
            @(IsSaving ? "Saving..." : "Save Changes")
        </button>
        <button @onclick="Cancel"
                disabled="@IsSaving"
                class="btn-secondary">
            Cancel
        </button>
    </div>

    @if (ShowSuccess)
    {
        <div class="success-message">LLM parameters updated!</div>
    }

    @if (HasError)
    {
        <div class="error-message">@ErrorMessage</div>
    }
</div>
```

#### ProviderSettingsEditor.razor

**Location**: `TransparentAiAgentGui/Components/Configuration/ProviderSettingsEditor.razor`

**Features**:
- Provider dropdown (Azure OpenAI / Anthropic)
- Conditional fields based on provider
- API key input (type="password")
- Masked current key display
- "Update API Key" checkbox
- Endpoint input (Azure only)
- Model dropdown (Anthropic only)

**UI Design**:

```razor
<div class="provider-settings-editor">
    <h4>LLM Provider Settings</h4>

    <div class="parameter-group">
        <label>Provider</label>
        <select @bind="SelectedProvider" class="provider-dropdown">
            <option value="AzureOpenAI">Azure OpenAI</option>
            <option value="Anthropic">Anthropic Claude</option>
        </select>
    </div>

    @if (SelectedProvider == "AzureOpenAI")
    {
        <div class="parameter-group">
            <label>Azure Endpoint</label>
            <input type="text"
                   @bind="AzureEndpoint"
                   placeholder="https://your-resource.openai.azure.com"
                   class="text-input" />
        </div>

        <div class="parameter-group">
            <label>Deployment Name</label>
            <input type="text"
                   @bind="AzureDeploymentName"
                   placeholder="gpt-4"
                   class="text-input" />
        </div>
    }

    @if (SelectedProvider == "Anthropic")
    {
        <div class="parameter-group">
            <label>Model</label>
            <select @bind="AnthropicModel" class="model-dropdown">
                <option value="claude-3-5-sonnet-20241022">Claude 3.5 Sonnet</option>
                <option value="claude-3-opus-20240229">Claude 3 Opus</option>
                <option value="claude-3-haiku-20240307">Claude 3 Haiku</option>
            </select>
        </div>
    }

    <div class="parameter-group">
        <label>
            API Key
            @if (!string.IsNullOrEmpty(CurrentApiKeyMasked))
            {
                <span class="current-key">Current: @CurrentApiKeyMasked</span>
            }
        </label>
        <div class="api-key-input-group">
            <input type="@(ShowApiKey ? "text" : "password")"
                   @bind="ApiKey"
                   placeholder="@(UpdateApiKey ? "Enter new API key" : "••••••••")"
                   disabled="@(!UpdateApiKey)"
                   class="text-input" />
            <label class="checkbox-label">
                <input type="checkbox" @bind="UpdateApiKey" />
                Update API Key
            </label>
            <label class="checkbox-label">
                <input type="checkbox" @bind="ShowApiKey" />
                Show Key
            </label>
        </div>
    </div>

    <div class="editor-actions">
        <button @onclick="SaveAsync"
                disabled="@(!CanSave || IsSaving)"
                class="btn-primary">
            @(IsSaving ? "Saving..." : "Save & Switch Provider")
        </button>
        <button @onclick="Cancel"
                disabled="@IsSaving"
                class="btn-secondary">
            Cancel
        </button>
    </div>

    @if (ShowSuccess)
    {
        <div class="success-message">
            Provider settings updated! Using @SelectedProvider now.
        </div>
    }

    @if (HasError)
    {
        <div class="error-message">@ErrorMessage</div>
    }

    @if (IsSaving)
    {
        <div class="warning-message">
            ⚠️ Switching provider will interrupt any active conversations.
        </div>
    }
</div>
```

#### ToolsConfigurationEditor.razor

**Location**: `TransparentAiAgentGui/Components/Configuration/ToolsConfigurationEditor.razor`

**Features**:
- List of configured MCP servers
- Add/Remove server buttons
- Enable/Disable toggle for each server
- Server configuration fields (command, args, environment variables)
- Auto-discover tools toggle

**UI Design**:

```razor
<div class="tools-configuration-editor">
    <h4>MCP Server Configuration</h4>

    <div class="parameter-group">
        <label class="checkbox-label">
            <input type="checkbox" @bind="AutoDiscoverTools" />
            Auto-discover tools on startup
        </label>
    </div>

    <div class="servers-list">
        <h5>Configured Servers</h5>

        @if (!Servers.Any())
        {
            <p class="no-servers">No MCP servers configured</p>
        }
        else
        {
            @foreach (var server in Servers)
            {
                <div class="server-card">
                    <div class="server-header">
                        <input type="text"
                               @bind="server.Name"
                               class="server-name"
                               placeholder="Server name" />
                        <label class="toggle-label">
                            <input type="checkbox"
                                   @bind="server.Enabled"
                                   class="toggle-input" />
                            Enabled
                        </label>
                        <button @onclick="() => RemoveServer(server)"
                                class="btn-remove">
                            Remove
                        </button>
                    </div>

                    <div class="server-config">
                        <div class="config-field">
                            <label>Command</label>
                            <input type="text"
                                   @bind="server.Command"
                                   placeholder="node"
                                   class="text-input" />
                        </div>

                        <div class="config-field">
                            <label>Arguments</label>
                            <input type="text"
                                   @bind="server.Args"
                                   placeholder="server.js"
                                   class="text-input" />
                        </div>

                        <div class="config-field">
                            <label>Environment Variables (JSON)</label>
                            <textarea @bind="server.EnvJson"
                                      rows="3"
                                      placeholder='{"VAR": "value"}'
                                      class="textarea-input">
                            </textarea>
                        </div>
                    </div>
                </div>
            }
        }
    </div>

    <button @onclick="AddServer" class="btn-add">
        + Add MCP Server
    </button>

    <div class="editor-actions">
        <button @onclick="SaveAsync"
                disabled="@(!HasChanges || IsSaving)"
                class="btn-primary">
            @(IsSaving ? "Saving..." : "Save & Refresh Tools")
        </button>
        <button @onclick="Cancel"
                disabled="@IsSaving"
                class="btn-secondary">
            Cancel
        </button>
    </div>

    @if (ShowSuccess)
    {
        <div class="success-message">
            MCP servers updated! Discovered @DiscoveredToolCount tools.
        </div>
    }

    @if (HasError)
    {
        <div class="error-message">@ErrorMessage</div>
    }
</div>

@code {
    private List<MCPServerConfig> Servers { get; set; } = new();
    private bool AutoDiscoverTools { get; set; }

    private void AddServer()
    {
        Servers.Add(new MCPServerConfig
        {
            Name = $"Server {Servers.Count + 1}",
            Enabled = true,
            Command = "",
            Args = "",
            EnvJson = "{}"
        });
    }

    private void RemoveServer(MCPServerConfig server)
    {
        Servers.Remove(server);
    }
}
```

### 5. Navigation Integration

**Location**: `TransparentAiAgentGui/Components/Layout/NavMenu.razor` (existing file)

**Add Configuration Link**:

```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="configuration">
        <span class="bi bi-gear-fill" aria-hidden="true"></span> Configuration
    </NavLink>
</div>
```

---

## Implementation Steps (TDD Approach)

### Step 1: Configuration Validator (Backend - TDD)

**Test File**: `TransparentAiAgentCore_Tests/Application/Configuration/ConfigurationValidatorTests.cs`

**Tests to Write**:

```csharp
[TestClass]
public class ConfigurationValidatorTests
{
    private ConfigurationValidator _validator;

    [TestInitialize]
    public void Setup()
    {
        _validator = new ConfigurationValidator();
    }

    // System Prompt Tests
    [TestMethod]
    public void ValidateSystemPrompt_EmptyPrompt_ReturnsFail()
    {
        var result = _validator.ValidateSystemPrompt("");
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Contains("System prompt cannot be empty"));
    }

    [TestMethod]
    public void ValidateSystemPrompt_ValidPrompt_ReturnsSuccess()
    {
        var result = _validator.ValidateSystemPrompt("You are a helpful assistant");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateSystemPrompt_TooLong_ReturnsFail()
    {
        var longPrompt = new string('x', 50001);
        var result = _validator.ValidateSystemPrompt(longPrompt);
        Assert.IsFalse(result.IsValid);
    }

    // LLM Parameters Tests
    [TestMethod]
    public void ValidateLLMParameters_TemperatureOutOfRange_ReturnsFail()
    {
        var params = new LLMParameters { Temperature = 2.5, MaxTokens = 1000, TopP = 1.0 };
        var result = _validator.ValidateLLMParameters(params);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Temperature")));
    }

    [TestMethod]
    public void ValidateLLMParameters_ValidParams_ReturnsSuccess()
    {
        var params = new LLMParameters { Temperature = 0.7, MaxTokens = 1000, TopP = 1.0 };
        var result = _validator.ValidateLLMParameters(params);
        Assert.IsTrue(result.IsValid);
    }

    // Provider Settings Tests
    [TestMethod]
    public void ValidateProviderSettings_AzureWithoutEndpoint_ReturnsFail()
    {
        var result = _validator.ValidateProviderSettings(
            LLMProviderType.AzureOpenAI, "test-key", null);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateProviderSettings_AnthropicWithValidKey_ReturnsSuccess()
    {
        var result = _validator.ValidateProviderSettings(
            LLMProviderType.Anthropic, "sk-ant-test123", null);
        Assert.IsTrue(result.IsValid);
    }
}
```

**Implementation**: Create `ConfigurationValidator` class to pass all tests.

### Step 2: Configuration Service (Backend - TDD)

**Test File**: `TransparentAiAgentCore_Tests/Application/Configuration/ConfigurationServiceTests.cs`

**Key Tests**:

```csharp
[TestClass]
public class ConfigurationServiceTests
{
    private ConfigurationService _service;
    private Mock<IConversationManager> _mockConversationManager;
    private Mock<ILLMProviderFactory> _mockProviderFactory;
    private Mock<IToolRegistry> _mockToolRegistry;
    private Mock<ITransparencyService> _mockTransparency;
    private AppConfiguration _appConfig;

    [TestInitialize]
    public void Setup()
    {
        _mockConversationManager = new Mock<IConversationManager>();
        _mockProviderFactory = new Mock<ILLMProviderFactory>();
        _mockToolRegistry = new Mock<IToolRegistry>();
        _mockTransparency = new Mock<ITransparencyService>();
        _appConfig = new AppConfiguration();

        _service = new ConfigurationService(
            _appConfig,
            _mockConversationManager.Object,
            _mockProviderFactory.Object,
            _mockToolRegistry.Object,
            _mockTransparency.Object);
    }

    [TestMethod]
    public async Task UpdateSystemPromptAsync_ValidPrompt_UpdatesConversationManager()
    {
        // Arrange
        var newPrompt = "New system prompt";

        // Act
        await _service.UpdateSystemPromptAsync(newPrompt);

        // Assert
        _mockConversationManager.Verify(
            cm => cm.UpdateSystemPrompt(newPrompt),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateSystemPromptAsync_ValidPrompt_EmitsTransparencyEvent()
    {
        // Arrange
        var newPrompt = "New system prompt";

        // Act
        await _service.UpdateSystemPromptAsync(newPrompt);

        // Assert
        _mockTransparency.Verify(
            t => t.LogEventAsync(It.IsAny<ConfigurationChangeEvent>()),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateProviderAsync_ValidSettings_RecreatesProvider()
    {
        // Arrange
        var provider = LLMProviderType.Anthropic;
        var apiKey = "sk-ant-test123";

        // Act
        await _service.UpdateProviderAsync(provider, apiKey);

        // Assert
        _mockProviderFactory.Verify(
            f => f.RecreateProvider(provider, apiKey, null),
            Times.Once);
    }
}
```

**Implementation**: Create `ConfigurationService` class to pass all tests.

### Step 3: ConversationManager Hot-Reload Methods

**Test File**: `TransparentAiAgentCore_Tests/Application/Conversation/ConversationManagerTests.cs` (existing, add tests)

**New Tests**:

```csharp
[TestMethod]
public void UpdateSystemPrompt_ValidPrompt_UpdatesPrompt()
{
    // Arrange
    var manager = new ConversationManager(4000, _transparencyService);
    var newPrompt = "New system prompt";

    // Act
    manager.UpdateSystemPrompt(newPrompt);

    // Assert - verify new prompt is used in next GetMessagesForLLM call
    var messages = manager.GetMessagesForLLM();
    // System message should use new prompt
}

[TestMethod]
public void UpdateLLMParameters_ValidParams_UpdatesParameters()
{
    // Arrange
    var manager = new ConversationManager(4000, _transparencyService);
    var newParams = new LLMParameters { Temperature = 0.9 };

    // Act
    manager.UpdateLLMParameters(newParams);

    // Assert
    // Verify parameters are stored and returned correctly
}
```

**Implementation**: Add methods to `ConversationManager` to pass tests.

### Step 4: LLMProviderFactory Updates

**Test File**: `TransparentAiAgentCore_Tests/Infrastructure/LLM/LLMProviderFactoryTests.cs` (may exist, add tests)

**New Tests**:

```csharp
[TestMethod]
public void RecreateProvider_AzureOpenAI_CreatesNewProvider()
{
    // Arrange
    var factory = new LLMProviderFactory(_appConfig, _transparencyService);

    // Act
    var provider = factory.RecreateProvider(
        LLMProviderType.AzureOpenAI,
        "test-key",
        "https://test.openai.azure.com");

    // Assert
    Assert.IsNotNull(provider);
    Assert.IsInstanceOfType(provider, typeof(AzureOpenAIProvider));
}

[TestMethod]
public void RecreateProvider_Anthropic_CreatesNewProvider()
{
    // Arrange
    var factory = new LLMProviderFactory(_appConfig, _transparencyService);

    // Act
    var provider = factory.RecreateProvider(
        LLMProviderType.Anthropic,
        "sk-ant-test");

    // Assert
    Assert.IsNotNull(provider);
    Assert.IsInstanceOfType(provider, typeof(AnthropicProvider));
}
```

**Implementation**: Add `RecreateProvider` method to factory.

### Step 5: Configuration API Endpoints

**Manual Testing Required** (Minimal APIs are harder to unit test)

**Testing Approach**:
1. Start application
2. Use Postman or browser dev tools to test each endpoint
3. Verify responses and side effects

**Endpoints to Test**:
- `GET /api/config` - Returns current config with masked keys
- `PUT /api/config/system-prompt` - Updates system prompt
- `PUT /api/config/llm-parameters` - Updates LLM params
- `PUT /api/config/provider` - Switches provider
- `PUT /api/config/mcp-servers` - Updates MCP servers

### Step 6: UI Components (Bottom-Up Approach)

**Order**:
1. **SystemPromptEditor** (simplest - just textarea)
2. **LLMParametersEditor** (sliders and number input)
3. **ProviderSettingsEditor** (conditional fields, API key handling)
4. **ToolsConfigurationEditor** (complex - list management)
5. **ConfigurationOverview** (container with tabs)

**Testing Each Component**:
1. Build and run application
2. Navigate to Configuration page
3. Test all interactions (save, cancel, validation)
4. Verify hot-reload works (changes apply immediately)
5. Check transparency system logs changes

### Step 7: Integration Testing

**Test Scenarios**:

1. **System Prompt Hot-Reload**:
   - Change system prompt via UI
   - Start new conversation
   - Verify LLM uses new prompt

2. **LLM Parameters Hot-Reload**:
   - Adjust temperature slider
   - Send message to LLM
   - Verify response reflects new temperature

3. **Provider Switch**:
   - Switch from Azure OpenAI to Anthropic
   - Send message
   - Verify Anthropic is used
   - Check transparency logs

4. **MCP Server Configuration**:
   - Add new MCP server
   - Verify tools are discovered
   - Check Tools tab shows new tools

5. **Persistence**:
   - Change configuration
   - Restart application
   - Verify changes persisted

6. **Validation**:
   - Try invalid temperature (e.g., 3.0)
   - Verify error message
   - Verify change not applied

---

## Technical Details

### API Key Security

**Masking Strategy**:
```csharp
public static string MaskApiKey(string apiKey)
{
    if (string.IsNullOrEmpty(apiKey)) return "";
    if (apiKey.Length <= 8) return "***";
    return $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}";
}

// Example: "sk-ant-api03-abc123..." → "sk-a...123"
```

**Storage**:
- Store in `appsettings.json` (file system permissions protect it)
- Never log full keys in transparency system
- Never send full keys to browser (masked in GET /api/config)

**Update Flow**:
- Checkbox to enable API key update
- Only send new key if checkbox checked
- Backend uses existing key if not provided in request

### Hot-Reload Implementation

#### System Prompt & LLM Parameters

**When Changed**: Next LLM call
**How**: `ConversationManager` methods update internal state
**Impact**: Immediate for new conversations, current conversation unaffected

```csharp
// In ConfigurationService
await _conversationManager.UpdateSystemPromptAsync(newPrompt);
// Next call to GetMessagesForLLM() will include new prompt
```

#### LLM Provider Switch

**When Changed**: Immediately
**How**: Recreate provider instance and update accessor
**Impact**: Current streaming calls may fail, new calls use new provider

```csharp
// In ConfigurationService
var newProvider = await _providerFactory.RecreateProviderAsync(provider, apiKey, endpoint);
_providerAccessor.CurrentProvider = newProvider;

// All subsequent calls via ILLMProvider will use new provider
```

**Challenge**: Active streaming calls will fail when old provider is disposed.

**Solution**:
1. Cancel active conversations before switching (UI warning)
2. Or: Let current call finish, queue provider switch

**Implementation**: Option 1 (simpler) - show warning, user must confirm.

#### MCP Server Configuration

**When Changed**: After tool refresh
**How**: Call `IToolRegistry.RefreshAsync()`
**Impact**: Tool list updates immediately

```csharp
// In ConfigurationService
_appConfig.MCP.Servers = newServers;
await _toolRegistry.RefreshAsync();

// Tools tab and agent orchestrator see updated tool list immediately
```

### Transparency Events

**ConfigurationChangeEvent**:

```csharp
public class ConfigurationChangeEvent : TransparencyEvent
{
    public string ChangeType { get; set; }  // "SystemPrompt", "LLMParameters", etc.
    public string OldValue { get; set; }    // JSON or string representation
    public string NewValue { get; set; }    // JSON or string representation
    public DateTime Timestamp { get; set; }
}
```

**Logged For**:
- System prompt changes
- LLM parameter changes
- Provider switches
- MCP server configuration changes

**NOT Logged**:
- Full API keys (always masked)

### Configuration Persistence

**File**: `appsettings.json` in application root

**Format**:
```json
{
  "Agent": {
    "SystemPrompt": "You are a helpful assistant...",
    "ContextWindowSize": 4000,
    "EnableTools": true
  },
  "LLM": {
    "Provider": "AzureOpenAI",
    "Parameters": {
      "Temperature": 0.7,
      "MaxTokens": 1000,
      "TopP": 1.0
    },
    "AzureOpenAI": {
      "Endpoint": "https://...",
      "DeploymentName": "gpt-4",
      "ApiKey": "..."
    },
    "Anthropic": {
      "Model": "claude-3-5-sonnet-20241022",
      "ApiKey": "..."
    }
  },
  "MCP": {
    "AutoDiscoverTools": true,
    "Servers": [
      {
        "Name": "Example Server",
        "Command": "node",
        "Args": "server.js",
        "Enabled": true,
        "Environment": {
          "VAR": "value"
        }
      }
    ]
  }
}
```

**Save Method**:
```csharp
public async Task SaveConfigurationAsync(AppConfiguration config, CancellationToken ct)
{
    var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    var configPath = Path.Combine(_appRoot, "appsettings.json");
    await File.WriteAllTextAsync(configPath, json, ct);
}
```

---

## File Structure

### New Files to Create (~12 files)

```
TransparentAiAgentCore/
└── Application/
    └── Configuration/
        ├── IConfigurationService.cs                    (interface)
        ├── ConfigurationService.cs                     (hot-reload logic)
        ├── ConfigurationValidator.cs                   (validation rules)
        ├── ConfigurationValidationResult.cs            (result model)
        ├── ConfigurationChangeEvent.cs                 (transparency event)
        └── Models/
            ├── SystemPromptUpdateRequest.cs
            ├── LLMParametersUpdateRequest.cs
            ├── ProviderUpdateRequest.cs
            └── MCPServersUpdateRequest.cs

TransparentAiAgentGui/
└── Components/
    └── Configuration/
        ├── ConfigurationOverview.razor                 (main page with tabs)
        ├── ConfigurationOverview.razor.css             (styles)
        ├── SystemPromptEditor.razor                    (prompt editor)
        ├── SystemPromptEditor.razor.css                (styles)
        ├── LLMParametersEditor.razor                   (sliders/inputs)
        ├── LLMParametersEditor.razor.css               (styles)
        ├── ProviderSettingsEditor.razor                (provider switch)
        ├── ProviderSettingsEditor.razor.css            (styles)
        ├── ToolsConfigurationEditor.razor              (MCP config)
        └── ToolsConfigurationEditor.razor.css          (styles)

TransparentAiAgentCore_Tests/
└── Application/
    └── Configuration/
        ├── ConfigurationValidatorTests.cs
        └── ConfigurationServiceTests.cs
```

### Files to Modify (~6 files)

```
TransparentAiAgentCore/
├── Application/
│   └── Conversation/
│       └── ConversationManager.cs                      (add hot-reload methods)
└── Infrastructure/
    └── LLM/
        └── LLMProviderFactory.cs                       (add RecreateProvider)

TransparentAiAgentGui/
├── Program.cs                                          (add API endpoints, DI updates)
├── Components/
│   └── Layout/
│       └── NavMenu.razor                               (add Configuration link)
├── wwwroot/
│   └── css/
│       └── app.css                                     (add configuration styles)
└── _Imports.razor                                      (add using statements)
```

---

## Success Criteria

### Functional Requirements

✅ **Configuration UI Available**
- Navigation link visible in sidebar
- Configuration page loads without errors
- All four tabs accessible (System Prompt, LLM Params, Provider, Tools)

✅ **System Prompt Editing**
- Can view current system prompt
- Can edit in multi-line textarea
- Character count visible
- Save applies changes immediately
- Changes persist across restarts
- Next conversation uses new prompt

✅ **LLM Parameters Editing**
- Can adjust Temperature (0.0 - 2.0)
- Can adjust MaxTokens (1 - 128000)
- Can adjust TopP (0.0 - 1.0)
- Current values displayed accurately
- Changes apply immediately to next LLM call
- Changes persist across restarts

✅ **Provider Switching**
- Can switch between Azure OpenAI and Anthropic
- Provider-specific fields shown (endpoint for Azure, model for Anthropic)
- API key update optional (checkbox)
- Current API key masked
- Switch applies immediately
- Active conversations show warning
- Changes persist across restarts

✅ **MCP Server Configuration**
- Can view list of configured servers
- Can add new servers
- Can remove servers
- Can enable/disable servers
- Can edit server command, args, environment
- Changes trigger tool refresh
- Tools tab reflects new tools immediately
- Changes persist across restarts

✅ **Validation**
- Invalid system prompt rejected (empty, too long)
- Invalid LLM parameters rejected (out of range)
- Invalid provider settings rejected (missing endpoint, bad API key format)
- Clear error messages shown to user
- Invalid changes not applied

✅ **Transparency**
- All configuration changes logged to transparency system
- Timestamps recorded
- Old and new values shown (API keys masked)
- Visible in transparency view/logs

✅ **Persistence**
- All changes saved to `appsettings.json`
- Configuration loads on application startup
- No data loss on restart

### Non-Functional Requirements

✅ **Performance**
- Configuration loads in < 1 second
- Save operations complete in < 2 seconds
- No noticeable lag when switching tabs
- Hot-reload completes in < 1 second

✅ **Usability**
- Intuitive UI layout
- Clear labels and hints
- Responsive feedback (loading states, success/error messages)
- Keyboard accessible (tab navigation works)

✅ **Security**
- API keys never logged in full
- API keys masked in UI
- API keys not sent to browser unless explicitly updated
- Configuration file has appropriate permissions

✅ **Maintainability**
- Clean separation of concerns (service, API, UI)
- Well-tested validation logic
- Clear error handling
- Comprehensive logging

---

## Testing Strategy

### Unit Tests (TDD)

**Components**:
- `ConfigurationValidator` - All validation rules
- `ConfigurationService` - All hot-reload methods
- `ConversationManager` - Hot-reload methods
- `LLMProviderFactory` - Provider recreation

**Coverage Target**: 80%+ for business logic

### Integration Tests (Manual)

**Scenarios**:
1. Full configuration flow (edit all settings, save, verify)
2. Provider switch with active conversation
3. MCP server add/remove/refresh
4. Configuration persistence (restart app, verify settings)
5. Validation error handling (invalid inputs)
6. Transparency logging (verify all changes logged)

### End-to-End Tests (Manual)

**User Workflows**:
1. New user: Configure from defaults to working agent
2. Change system prompt, start conversation, verify behavior
3. Adjust temperature, compare responses
4. Switch providers, verify different LLM behavior
5. Add MCP server, use tool in conversation

---

## Implementation Timeline

### Day 1: Backend Foundation
- **Morning**: ConfigurationValidator with tests (TDD)
- **Afternoon**: ConfigurationService with tests (TDD)
- **Evening**: ConversationManager & LLMProviderFactory updates

**Deliverable**: All backend hot-reload logic working and tested

### Day 2: API & UI Basics
- **Morning**: Configuration API endpoints in Program.cs
- **Afternoon**: SystemPromptEditor component
- **Evening**: LLMParametersEditor component

**Deliverable**: Basic configuration UI working

### Day 3: Advanced UI & Integration
- **Morning**: ProviderSettingsEditor component
- **Afternoon**: ToolsConfigurationEditor component
- **Evening**: ConfigurationOverview container & integration testing

**Deliverable**: Complete configuration UI with all features

### Day 4: Polish & Testing (if needed)
- **Morning**: Bug fixes, styling improvements
- **Afternoon**: End-to-end testing, edge cases
- **Evening**: Documentation, code review

**Deliverable**: Production-ready Phase 7 implementation

---

## Known Challenges & Solutions

### Challenge 1: Provider Switch During Active Streaming

**Problem**: User switches provider while LLM is streaming a response.

**Solution**:
- Show warning dialog: "Active conversation in progress. Switch provider anyway?"
- If confirmed, cancel active streaming call
- Apply provider switch
- Show notification: "Provider switched. Previous response was interrupted."

**Implementation**: Add cancellation token to streaming calls, cancel on provider switch.

### Challenge 2: DI Container Singleton Update

**Problem**: ILLMProvider is registered as singleton in DI container. How to update it?

**Solution**: Use accessor pattern:
```csharp
public class LLMProviderAccessor
{
    public ILLMProvider CurrentProvider { get; set; }
}

// In DI
builder.Services.AddSingleton<LLMProviderAccessor>();
builder.Services.AddSingleton<ILLMProvider>(sp =>
    sp.GetRequiredService<LLMProviderAccessor>().CurrentProvider);

// ConfigurationService can update accessor
_providerAccessor.CurrentProvider = newProvider;
```

**Alternative**: Use IServiceProvider with scoping, but accessor is simpler.

### Challenge 3: API Key Security in Browser

**Problem**: Don't want full API keys sent to browser.

**Solution**:
- GET /api/config returns masked keys
- Only send full key when explicitly updated (checkbox in UI)
- Backend uses existing key if not provided in PUT request

### Challenge 4: Configuration File Locking

**Problem**: Multiple threads writing to appsettings.json simultaneously.

**Solution**:
- Use `SemaphoreSlim` to serialize writes
- Or: Use queue pattern for configuration saves

**Implementation**: Add lock to ConfigurationManager:
```csharp
private readonly SemaphoreSlim _saveLock = new(1, 1);

public async Task SaveConfigurationAsync(AppConfiguration config, CancellationToken ct)
{
    await _saveLock.WaitAsync(ct);
    try
    {
        // Write to file
    }
    finally
    {
        _saveLock.Release();
    }
}
```

---

## Dependencies

### External Libraries
- **System.Text.Json** - For JSON serialization (already used)
- **Microsoft.AspNetCore.Components.Web** - For Blazor components (already used)

### Internal Dependencies
- **Phase 1-6 Components**: All must be complete
- **ConversationManager**: Must support hot-reload methods
- **LLMProviderFactory**: Must support provider recreation
- **ToolRegistry**: Must support refresh

### Configuration Structure
- **AppConfiguration**: Must be comprehensive and serializable
- **ConfigurationManager**: Must support save/load

---

## Future Enhancements (Out of Scope)

### Configuration Presets
- Predefined configurations: "Creative", "Balanced", "Precise"
- One-click application of preset
- Custom preset creation

### Configuration History
- Version control for configuration changes
- Rollback to previous configuration
- Diff view between versions

### Advanced Validation
- Test provider connection before saving
- Estimate token costs based on parameters
- Warn about expensive configurations

### Multi-User Support
- User-specific configuration profiles
- Admin vs user settings
- Configuration templates

### Export/Import
- Export configuration to file
- Import configuration from file
- Share configurations between installations

---

## Appendix A: Example Configuration Flow

### Scenario: User Switches from Azure OpenAI to Anthropic

1. **User navigates to Configuration page**
2. **Clicks "Provider Settings" tab**
3. **Sees current provider**: Azure OpenAI
4. **Changes dropdown to**: Anthropic
5. **UI updates**: Shows "Model" dropdown, hides "Endpoint" field
6. **Checks "Update API Key" checkbox**
7. **Enters Anthropic API key**: `sk-ant-api03-abc123...`
8. **Clicks "Save & Switch Provider"**
9. **Warning shown**: "This will interrupt active conversations"
10. **User confirms**
11. **Backend flow**:
    - Validate API key format
    - Update AppConfiguration
    - Cancel active conversations
    - Recreate LLM provider (Anthropic)
    - Update provider accessor
    - Persist to appsettings.json
    - Emit transparency event
12. **UI shows**: "Provider switched to Anthropic"
13. **User starts new conversation**
14. **Agent uses Anthropic Claude**

---

## Appendix B: Configuration API Contract

### GET /api/config

**Response**:
```json
{
  "agent": {
    "systemPrompt": "You are a helpful assistant...",
    "contextWindowSize": 4000,
    "enableTools": true
  },
  "llm": {
    "provider": "AzureOpenAI",
    "parameters": {
      "temperature": 0.7,
      "maxTokens": 1000,
      "topP": 1.0
    },
    "azureOpenAI": {
      "endpoint": "https://test.openai.azure.com",
      "deploymentName": "gpt-4",
      "apiKey": "test...key"
    },
    "anthropic": {
      "model": "claude-3-5-sonnet-20241022",
      "apiKey": ""
    }
  },
  "mcp": {
    "autoDiscoverTools": true,
    "servers": [
      {
        "name": "Example",
        "command": "node",
        "args": "server.js",
        "enabled": true,
        "environment": {}
      }
    ]
  }
}
```

### PUT /api/config/system-prompt

**Request**:
```json
{
  "systemPrompt": "New system prompt text..."
}
```

**Response (Success)**:
```json
{
  "success": true
}
```

**Response (Error)**:
```json
{
  "error": "System prompt cannot be empty"
}
```

### PUT /api/config/llm-parameters

**Request**:
```json
{
  "parameters": {
    "temperature": 0.9,
    "maxTokens": 2000,
    "topP": 0.95
  }
}
```

**Response**: Same as system-prompt

### PUT /api/config/provider

**Request**:
```json
{
  "provider": "Anthropic",
  "apiKey": "sk-ant-api03-...",
  "endpoint": null,
  "model": "claude-3-5-sonnet-20241022"
}
```

**Response**: Same as system-prompt

### PUT /api/config/mcp-servers

**Request**:
```json
{
  "autoDiscoverTools": true,
  "servers": [
    {
      "name": "New Server",
      "command": "python",
      "args": "server.py",
      "enabled": true,
      "environment": {
        "API_KEY": "value"
      }
    }
  ]
}
```

**Response**: Same as system-prompt

---

## Summary

Phase 7 delivers a **complete configuration UI** with **hot-reload capabilities**, allowing users to modify all agent settings without restarting. The implementation follows TDD principles, maintains clean architecture, and ensures all changes are transparent and persistent.

**Key Achievements**:
- ✅ Runtime configuration updates (no restart needed)
- ✅ Comprehensive validation with user feedback
- ✅ Secure API key handling
- ✅ Full transparency logging
- ✅ Persistent configuration storage
- ✅ Intuitive web-based UI

**Next Phase**: Phase 8 - Anthropic Provider (expand LLM support)

---

**End of Phase 7 Detailed Plan**
