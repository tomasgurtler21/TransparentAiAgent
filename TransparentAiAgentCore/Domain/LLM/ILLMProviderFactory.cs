using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Factory interface for creating LLM provider instances from multi-provider configuration.
/// </summary>
public interface ILLMProviderFactory
{
    /// <summary>
    /// Creates a provider instance from a ProviderConfig.
    /// This is used for the multi-provider configuration structure where providers
    /// are defined in the Providers dictionary.
    /// </summary>
    /// <param name="configName">The configuration name (key in the Providers dictionary).</param>
    /// <param name="config">The provider configuration containing type, parameters, and overrides.</param>
    /// <returns>An ILLMProvider instance configured according to the ProviderConfig.</returns>
    /// <exception cref="ConfigurationException">Thrown when provider type is unknown or required parameters are missing.</exception>
    ILLMProvider CreateProvider(string configName, ProviderConfig config);
}
