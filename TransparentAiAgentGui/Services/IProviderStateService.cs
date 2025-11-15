using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentGui.Services;

/// <summary>
/// Service for managing LLM provider state in the UI
/// </summary>
public interface IProviderStateService
{
    /// <summary>
    /// Get all available providers
    /// </summary>
    IReadOnlyList<ProviderInfo> GetAvailableProviders();

    /// <summary>
    /// Get the currently active provider
    /// </summary>
    ProviderInfo GetCurrentProvider();

    /// <summary>
    /// Change the active provider
    /// </summary>
    Task ChangeProviderAsync(string configName);

    /// <summary>
    /// Raised when the active provider changes
    /// </summary>
    event EventHandler? ProviderChanged;
}
