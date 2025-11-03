using System;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Factory for creating LLM provider instances
/// </summary>
public class LLMProviderFactory
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
            "anthropic" => CreateAnthropicProvider(),
            _ => throw new ConfigurationException($"Unknown LLM provider: {providerName}")
        };
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
