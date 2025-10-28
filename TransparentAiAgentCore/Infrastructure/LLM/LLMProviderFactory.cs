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
            "anthropic" => throw new NotImplementedException("Anthropic provider will be implemented in Phase 8"),
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
}
