using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

/// <summary>
/// Validates provider configuration structure (NOT connectivity).
/// Ensures all required parameters are present and have valid values.
/// </summary>
public class ProviderConfigValidator
{
    /// <summary>
    /// Validates a provider configuration
    /// </summary>
    /// <param name="config">The provider configuration to validate</param>
    /// <returns>Validation result with any errors found</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="ConfigurationException">Thrown for unknown provider types</exception>
    public ValidationResult Validate(ProviderConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return config.Type.ToLowerInvariant() switch
        {
            "anthropic" => ValidateAnthropicConfig(config),
            "azureopenai" => ValidateAzureOpenAIConfig(config),
            "openai" => ValidateOpenAIConfig(config),
            _ => throw new ConfigurationException($"Unknown provider type: {config.Type}")
        };
    }

    /// <summary>
    /// Validates Anthropic provider configuration
    /// Required parameters: Model, ApiKey
    /// </summary>
    private ValidationResult ValidateAnthropicConfig(ProviderConfig config)
    {
        var result = new ValidationResult();

        ValidateRequiredParameter(result, config, "Model", "Anthropic");
        ValidateRequiredApiKey(result, config, "Anthropic");

        return result;
    }

    /// <summary>
    /// Validates Azure OpenAI provider configuration
    /// Required parameters: Endpoint (valid URL), DeploymentName, ApiKey
    /// </summary>
    private ValidationResult ValidateAzureOpenAIConfig(ProviderConfig config)
    {
        var result = new ValidationResult();

        // Validate Endpoint with URL format check
        if (!config.Parameters.ContainsKey("Endpoint"))
        {
            result.AddError("AzureOpenAI provider requires 'Endpoint' parameter");
        }
        else
        {
            var endpoint = config.Parameters["Endpoint"]?.ToString();
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            {
                result.AddError("AzureOpenAI 'Endpoint' must be a valid URL");
            }
        }

        ValidateRequiredParameter(result, config, "DeploymentName", "AzureOpenAI");
        ValidateRequiredApiKey(result, config, "AzureOpenAI");

        return result;
    }

    /// <summary>
    /// Validates OpenAI provider configuration
    /// Required parameters: Model, ApiKey
    /// </summary>
    private ValidationResult ValidateOpenAIConfig(ProviderConfig config)
    {
        var result = new ValidationResult();

        ValidateRequiredParameter(result, config, "Model", "OpenAI");
        ValidateRequiredApiKey(result, config, "OpenAI");

        return result;
    }

    /// <summary>
    /// Helper method to validate a required parameter exists
    /// </summary>
    private void ValidateRequiredParameter(
        ValidationResult result,
        ProviderConfig config,
        string parameterName,
        string providerName)
    {
        if (!config.Parameters.ContainsKey(parameterName))
        {
            result.AddError($"{providerName} provider requires '{parameterName}' parameter");
        }
    }

    /// <summary>
    /// Helper method to validate ApiKey parameter exists and is not empty
    /// </summary>
    private void ValidateRequiredApiKey(
        ValidationResult result,
        ProviderConfig config,
        string providerName)
    {
        if (!config.Parameters.ContainsKey("ApiKey"))
        {
            result.AddError($"{providerName} provider requires 'ApiKey' parameter");
        }
        else if (string.IsNullOrWhiteSpace(config.Parameters["ApiKey"]?.ToString()))
        {
            result.AddError($"{providerName} provider 'ApiKey' cannot be empty");
        }
    }
}
