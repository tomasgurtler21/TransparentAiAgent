using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Authentication method to use. Defaults to ApiKey for backward compatibility.
    /// </summary>
    public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.ApiKey;

    /// <summary>
    /// API Key for authentication. Required only when AuthenticationMode is ApiKey.
    /// Ignored when using DefaultAzureCredential.
    /// </summary>
    public string? ApiKey { get; set; } = null;

    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";

    /// <summary>
    /// Optional tenant ID for DefaultAzureCredential.
    /// Only used when AuthenticationMode is DefaultAzureCredential.
    /// </summary>
    public string? TenantId { get; set; } = null;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ConfigurationException("Azure OpenAI Endpoint cannot be null or whitespace");

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            throw new ConfigurationException("Azure OpenAI Endpoint must be a valid URI");

        // Conditionally validate ApiKey only when using ApiKey authentication
        if (AuthenticationMode == AuthenticationMode.ApiKey)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ConfigurationException(
                    "Azure OpenAI ApiKey is required when AuthenticationMode is ApiKey. " +
                    "Either provide an ApiKey or set AuthenticationMode to DefaultAzureCredential.");
        }

        if (string.IsNullOrWhiteSpace(DeploymentName))
            throw new ConfigurationException("Azure OpenAI DeploymentName cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(ApiVersion))
            throw new ConfigurationException("Azure OpenAI ApiVersion cannot be null or whitespace");
    }
}
