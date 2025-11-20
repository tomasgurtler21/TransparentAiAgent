using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// LLM configuration using multi-provider structure.
/// Allows defining multiple LLM providers and switching between them dynamically.
/// </summary>
public class LLMConfiguration
{
    // ===== USER-FACING MULTI-PROVIDER CONFIGURATION =====

    /// <summary>
    /// The configuration name of the currently active provider.
    /// Must match a key in the Providers dictionary.
    /// </summary>
    public string? ActiveProvider { get; set; }

    /// <summary>
    /// Default parameters inherited by all providers.
    /// Can be overridden per-provider using ParameterOverrides.
    /// </summary>
    public ProviderParameters? DefaultParameters { get; set; }

    /// <summary>
    /// Dictionary of provider configurations.
    /// Key = configuration name, Value = provider configuration.
    /// At least one provider must be defined.
    /// </summary>
    public Dictionary<string, ProviderConfig>? Providers { get; set; }

    // ===== INTERNAL PROPERTIES (Used by provider factory pattern - DO NOT SET IN appsettings.json) =====
    // These properties are used internally when the factory creates temporary AppConfiguration
    // objects to pass to provider constructors. They should NOT be configured by users.

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// </summary>
    public string Provider { get; set; } = "AzureOpenAI";

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// Whether to use REST API instead of streaming (null or false = streaming, true = REST).
    /// </summary>
    public bool UseRest { get; set; } = false;

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// </summary>
    public AzureOpenAIConfiguration? AzureOpenAI { get; set; }

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// </summary>
    public OpenAIConfiguration? OpenAI { get; set; }

    /// <summary>
    /// INTERNAL USE ONLY. Do not set in configuration files.
    /// Used by factory pattern when creating temporary config objects.
    /// </summary>
    public AnthropicConfiguration? Anthropic { get; set; }

    /// <summary>
    /// Validates the LLM configuration.
    /// </summary>
    /// <exception cref="ConfigurationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        // Require Providers dictionary
        if (Providers == null || Providers.Count == 0)
        {
            throw new ConfigurationException(
                "LLM configuration requires at least one provider. " +
                "Define providers in the 'Providers' dictionary. " +
                "See appsettings.Example.json or docs/05-guides/deployment/llm-provider-selector.md for examples.");
        }

        // Require ActiveProvider
        if (string.IsNullOrWhiteSpace(ActiveProvider))
        {
            throw new ConfigurationException(
                "ActiveProvider cannot be null or whitespace. " +
                "Specify which provider to use from the Providers dictionary.");
        }

        // ActiveProvider must exist in Providers
        if (!Providers.ContainsKey(ActiveProvider))
        {
            throw new ConfigurationException(
                $"ActiveProvider '{ActiveProvider}' not found in Providers dictionary. " +
                $"Available providers: {string.Join(", ", Providers.Keys)}");
        }

        // Validate default parameters if present
        if (DefaultParameters != null)
        {
            if (DefaultParameters.Temperature.HasValue &&
                (DefaultParameters.Temperature.Value < 0 || DefaultParameters.Temperature.Value > 2))
            {
                throw new ConfigurationException("DefaultParameters Temperature must be between 0 and 2");
            }

            if (DefaultParameters.TopP.HasValue &&
                (DefaultParameters.TopP.Value < 0 || DefaultParameters.TopP.Value > 1))
            {
                throw new ConfigurationException("DefaultParameters TopP must be between 0 and 1");
            }

            if (DefaultParameters.MaxTokens.HasValue && DefaultParameters.MaxTokens.Value <= 0)
            {
                throw new ConfigurationException("DefaultParameters MaxTokens must be greater than 0");
            }
        }

        // Note: Individual ProviderConfig validation is done by ProviderConfigValidator
    }
}
