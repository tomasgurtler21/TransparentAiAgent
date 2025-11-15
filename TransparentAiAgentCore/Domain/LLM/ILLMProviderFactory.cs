using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Factory interface for creating LLM provider instances.
/// </summary>
public interface ILLMProviderFactory
{
    /// <summary>
    /// Creates a provider instance using the default configured provider.
    /// </summary>
    /// <returns>An ILLMProvider instance.</returns>
    ILLMProvider CreateProvider();

    /// <summary>
    /// Creates a provider instance by provider name.
    /// </summary>
    /// <param name="providerName">The provider name (e.g., "Anthropic", "AzureOpenAI").</param>
    /// <returns>An ILLMProvider instance.</returns>
    ILLMProvider CreateProvider(string providerName);

    /// <summary>
    /// Creates a provider instance from a ProviderConfig.
    /// </summary>
    /// <param name="configName">The configuration name.</param>
    /// <param name="config">The provider configuration.</param>
    /// <returns>An ILLMProvider instance.</returns>
    ILLMProvider CreateProvider(string configName, ProviderConfig config);
}
