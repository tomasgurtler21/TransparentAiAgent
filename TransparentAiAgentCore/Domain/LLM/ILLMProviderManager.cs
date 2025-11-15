using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Manages multiple LLM provider instances with lazy-loading and caching.
/// Allows switching between configured providers at runtime.
/// </summary>
public interface ILLMProviderManager
{
    /// <summary>
    /// Gets the currently active LLM provider instance.
    /// Providers are lazy-loaded and cached on first access.
    /// </summary>
    /// <returns>The active provider instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no active provider is configured.</exception>
    ILLMProvider GetActiveProvider();

    /// <summary>
    /// Sets the active provider to the specified configuration name.
    /// </summary>
    /// <param name="configName">The configuration name of the provider to activate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when configName is invalid or not found.</exception>
    Task SetActiveProviderAsync(string configName);

    /// <summary>
    /// Gets information about all available providers.
    /// </summary>
    /// <returns>A read-only list of provider information.</returns>
    IReadOnlyList<ProviderInfo> GetAvailableProviders();

    /// <summary>
    /// Gets information about the currently active provider.
    /// </summary>
    /// <returns>The current provider information.</returns>
    ProviderInfo GetCurrentProviderInfo();
}
