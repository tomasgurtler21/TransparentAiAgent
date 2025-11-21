using System;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Infrastructure.Authentication;

/// <summary>
/// Provides authentication credentials from configuration
/// </summary>
public class ConfigurationAuthenticationProvider : IAuthenticationProvider
{
    private readonly AppConfiguration _configuration;

    public ConfigurationAuthenticationProvider(AppConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string GetApiKey(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or whitespace", nameof(serviceName));

        return serviceName.ToLowerInvariant() switch
        {
            "azureopenai" => _configuration.LLM.AzureOpenAI?.ApiKey
                ?? throw new ConfigurationException("Azure OpenAI API key not configured"),
            "openai" => _configuration.LLM.OpenAI?.ApiKey
                ?? throw new ConfigurationException("OpenAI API key not configured"),
            "anthropic" => _configuration.LLM.Anthropic?.ApiKey
                ?? throw new ConfigurationException("Anthropic API key not configured"),
            _ => throw new ArgumentException($"Unknown service: {serviceName}", nameof(serviceName))
        };
    }

    public string GetEndpoint(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or whitespace", nameof(serviceName));

        return serviceName.ToLowerInvariant() switch
        {
            "azureopenai" => _configuration.LLM.AzureOpenAI?.Endpoint
                ?? throw new ConfigurationException("Azure OpenAI endpoint not configured"),
            "openai" => _configuration.LLM.OpenAI?.Endpoint
                ?? "https://api.openai.com", // OpenAI has default endpoint
            "anthropic" => _configuration.LLM.Anthropic?.Endpoint
                ?? "https://api.anthropic.com", // Anthropic has default endpoint
            _ => throw new ArgumentException($"Unknown service: {serviceName}", nameof(serviceName))
        };
    }
}
