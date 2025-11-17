using System;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Infrastructure.Authentication;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Factory for creating LLM provider instances
/// </summary>
public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IAuthenticationProvider _authProvider;
    private readonly ITransparencyService _transparencyService;
    private readonly AppConfiguration _configuration;

    public LLMProviderFactory(
        IAuthenticationProvider authProvider,
        ITransparencyService transparencyService,
        AppConfiguration configuration)
    {
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Creates a provider instance from a ProviderConfig.
    /// </summary>
    /// <param name="configName">The configuration name.</param>
    /// <param name="config">The provider configuration.</param>
    /// <returns>An ILLMProvider instance.</returns>
    /// <exception cref="ConfigurationException">Thrown when provider type is unknown or required parameters are missing.</exception>
    public virtual ILLMProvider CreateProvider(string configName, ProviderConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        return config.Type.ToLowerInvariant() switch
        {
            "anthropic" => CreateAnthropicProviderFromConfig(config),
            "azureopenai" => CreateAzureOpenAIProviderFromConfig(config),
            "openai" => CreateOpenAIProviderFromConfig(config),
            _ => throw new ConfigurationException($"Unknown provider type: {config.Type}")
        };
    }

    /// <summary>
    /// Creates an Anthropic provider from ProviderConfig.
    /// </summary>
    private ILLMProvider CreateAnthropicProviderFromConfig(ProviderConfig config)
    {
        // Extract required parameters
        var model = GetRequiredStringParameter(config, "Model", "Anthropic");
        var apiKey = GetRequiredStringParameter(config, "ApiKey", "Anthropic");

        // Create a temporary AppConfiguration with Anthropic config for the provider constructor
        var tempConfig = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "Anthropic",
                Anthropic = new AnthropicConfiguration
                {
                    ApiKey = apiKey,
                    Model = model
                }
            }
        };

        // Use a temporary auth provider that returns the API key from config
        var tempAuthProvider = new ConfigurationAuthenticationProvider(tempConfig);

        return new AnthropicProvider(tempAuthProvider, model, _transparencyService, tempConfig);
    }

    /// <summary>
    /// Creates an Azure OpenAI provider from ProviderConfig.
    /// </summary>
    private ILLMProvider CreateAzureOpenAIProviderFromConfig(ProviderConfig config)
    {
        // Extract required parameters
        var endpoint = GetRequiredStringParameter(config, "Endpoint", "AzureOpenAI");
        var deploymentName = GetRequiredStringParameter(config, "DeploymentName", "AzureOpenAI");

        // Get optional parameters
        var apiVersion = config.Parameters.TryGetValue("ApiVersion", out var versionObj)
            ? versionObj?.ToString() ?? "2024-02-15-preview"
            : "2024-02-15-preview";

        var authMode = config.Parameters.TryGetValue("AuthenticationMode", out var authModeObj)
            ? authModeObj?.ToString() ?? "ApiKey"
            : "ApiKey";

        // Parse authentication mode
        var authenticationMode = authMode.ToLowerInvariant() switch
        {
            "apikey" => AuthenticationMode.ApiKey,
            "defaultazurecredential" => AuthenticationMode.DefaultAzureCredential,
            "interactivebrowsercredential" => AuthenticationMode.InteractiveBrowserCredential,
            _ => AuthenticationMode.ApiKey
        };

        // ApiKey is only required when using ApiKey authentication
        string? apiKey = null;
        if (authenticationMode == AuthenticationMode.ApiKey)
        {
            apiKey = GetRequiredStringParameter(config, "ApiKey", "AzureOpenAI");
        }
        else
        {
            // Optional for OAuth modes (DefaultAzureCredential, InteractiveBrowserCredential)
            if (config.Parameters.TryGetValue("ApiKey", out var apiKeyObj))
            {
                apiKey = apiKeyObj?.ToString();
            }
        }

        // Get optional TenantId for OAuth authentication
        string? tenantId = null;
        if (config.Parameters.TryGetValue("TenantId", out var tenantIdObj))
        {
            tenantId = tenantIdObj?.ToString();
        }

        // Extract IsReasoningModel parameter - REQUIRED, no silent defaults
        if (!config.Parameters.TryGetValue("IsReasoningModel", out var reasoningObj))
        {
            throw new ConfigurationException(
                $"AzureOpenAI provider requires 'IsReasoningModel' parameter. " +
                $"Set to true for reasoning models (o1, o3, o4-mini, gpt-5 series), false for standard models (gpt-4, gpt-4o, etc.). " +
                $"Add \"IsReasoningModel\": true or false to your provider parameters.");
        }

        // Support both boolean and string representations (from JSON deserialization)
        var isReasoningModel = reasoningObj is bool boolValue ? boolValue :
                              bool.TryParse(reasoningObj?.ToString(), out var parsedValue) && parsedValue;

        // Create a temporary AppConfiguration with AzureOpenAI config
        var tempConfig = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                AzureOpenAI = new AzureOpenAIConfiguration
                {
                    Endpoint = endpoint,
                    DeploymentName = deploymentName,
                    ApiKey = apiKey,
                    ApiVersion = apiVersion,
                    AuthenticationMode = authenticationMode,
                    TenantId = tenantId,
                    IsReasoningModel = isReasoningModel
                }
            }
        };

        var tempAuthProvider = new ConfigurationAuthenticationProvider(tempConfig);

        return new AzureOpenAIProvider(tempAuthProvider, deploymentName, _transparencyService, tempConfig);
    }

    /// <summary>
    /// Creates an OpenAI provider from ProviderConfig.
    /// </summary>
    private ILLMProvider CreateOpenAIProviderFromConfig(ProviderConfig config)
    {
        // Extract required parameters
        var model = GetRequiredStringParameter(config, "Model", "OpenAI");
        var apiKey = GetRequiredStringParameter(config, "ApiKey", "OpenAI");

        // Extract IsReasoningModel parameter - REQUIRED, no silent defaults
        if (!config.Parameters.TryGetValue("IsReasoningModel", out var reasoningObj))
        {
            throw new ConfigurationException(
                $"OpenAI provider requires 'IsReasoningModel' parameter. " +
                $"Set to true for reasoning models (o1, o3, o4-mini, gpt-5 series), false for standard models (gpt-4, gpt-4o, etc.). " +
                $"Add \"IsReasoningModel\": true or false to your provider parameters.");
        }

        // Support both boolean and string representations (from JSON deserialization)
        var isReasoningModel = reasoningObj is bool boolValue ? boolValue :
                              bool.TryParse(reasoningObj?.ToString(), out var parsedValue) && parsedValue;

        // Create a temporary AppConfiguration with OpenAI config
        var tempConfig = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "OpenAI",
                OpenAI = new OpenAIConfiguration
                {
                    ApiKey = apiKey,
                    Model = model,
                    IsReasoningModel = isReasoningModel  // ✅ Set from config
                }
            }
        };

        var tempAuthProvider = new ConfigurationAuthenticationProvider(tempConfig);

        return new OpenAIProvider(tempAuthProvider, model, _transparencyService, tempConfig);
    }

    /// <summary>
    /// Helper method to extract a required string parameter from ProviderConfig.
    /// </summary>
    /// <param name="config">The provider configuration.</param>
    /// <param name="parameterName">The name of the parameter to extract.</param>
    /// <param name="providerType">The provider type for error messages.</param>
    /// <returns>The parameter value as a string.</returns>
    /// <exception cref="ConfigurationException">Thrown when the parameter is missing or null.</exception>
    private static string GetRequiredStringParameter(ProviderConfig config, string parameterName, string providerType)
    {
        if (!config.Parameters.TryGetValue(parameterName, out var valueObj) || valueObj == null)
            throw new ConfigurationException($"{providerType} provider requires '{parameterName}' parameter");

        return valueObj.ToString() ?? throw new ConfigurationException($"{parameterName} parameter cannot be null");
    }
}
