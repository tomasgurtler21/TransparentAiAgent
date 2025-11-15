using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentGui.Services;

/// <summary>
/// Service for managing LLM provider state in the UI.
/// Provides a Blazor-friendly interface to the ILLMProviderManager.
/// </summary>
public class ProviderStateService : IProviderStateService
{
    private readonly ILLMProviderManager _providerManager;

    /// <summary>
    /// Raised when the active provider changes
    /// </summary>
    public event EventHandler? ProviderChanged;

    public ProviderStateService(ILLMProviderManager providerManager)
    {
        _providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));
    }

    /// <summary>
    /// Get all available providers
    /// </summary>
    public IReadOnlyList<ProviderInfo> GetAvailableProviders()
    {
        return _providerManager.GetAvailableProviders();
    }

    /// <summary>
    /// Get the currently active provider
    /// </summary>
    public ProviderInfo GetCurrentProvider()
    {
        return _providerManager.GetCurrentProviderInfo();
    }

    /// <summary>
    /// Change the active provider and notify subscribers
    /// </summary>
    public async Task ChangeProviderAsync(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
            throw new ArgumentException("Provider config name cannot be null or empty", nameof(configName));

        await _providerManager.SetActiveProviderAsync(configName);
        OnProviderChanged();
    }

    private void OnProviderChanged()
    {
        ProviderChanged?.Invoke(this, EventArgs.Empty);
    }
}
