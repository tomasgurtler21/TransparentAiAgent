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

    public ILLMProvider CreateProvider()
    {
        return CreateProvider(_configuration.LLM.Provider);
    }

    public ILLMProvider CreateProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or whitespace", nameof(providerName));

        return providerName.ToLowerInvariant() switch
        {
            "azureopenai" => CreateAzureOpenAIProvider(),
            "openai" => CreateOpenAIProvider(),
            "anthropic" => CreateAnthropicProvider(),
            _ => throw new ConfigurationException($"Unknown LLM provider: {providerName}")
        };
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
        var apiKey = GetRequiredStringParameter(config, "ApiKey", "AzureOpenAI");

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
                    AuthenticationMode = authenticationMode
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

        // Create a temporary AppConfiguration with OpenAI config
        var tempConfig = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "OpenAI",
                OpenAI = new OpenAIConfiguration
                {
                    ApiKey = apiKey,
                    Model = model
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

    private ILLMProvider CreateAzureOpenAIProvider()
    {
        if (_configuration.LLM.AzureOpenAI == null)
            throw new ConfigurationException("Azure OpenAI configuration is missing");

        return new AzureOpenAIProvider(
            _authProvider,
            _configuration.LLM.AzureOpenAI.DeploymentName,
            _transparencyService,
            _configuration);
    }

    private ILLMProvider CreateOpenAIProvider()
    {
        if (_configuration.LLM.OpenAI == null)
            throw new ConfigurationException("OpenAI configuration is missing");

        return new OpenAIProvider(
            _authProvider,
            _configuration.LLM.OpenAI.Model,
            _transparencyService,
            _configuration);
    }

    private ILLMProvider CreateAnthropicProvider()
    {
        if (_configuration.LLM.Anthropic == null)
            throw new ConfigurationException("Anthropic configuration is missing");

        return new AnthropicProvider(
            _authProvider,
            _configuration.LLM.Anthropic.Model,
            _transparencyService,
            _configuration);
    }
}
