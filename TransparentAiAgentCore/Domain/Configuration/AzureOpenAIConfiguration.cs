using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Authentication method to use. Must be explicitly specified.
    /// Default is Unspecified, which will fail validation.
    /// </summary>
    public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.Unspecified;

    /// <summary>
    /// API Key for authentication. Required only when AuthenticationMode is ApiKey.
    /// Ignored when using DefaultAzureCredential or InteractiveBrowserCredential.
    /// </summary>
    public string? ApiKey { get; set; } = null;

    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";

    /// <summary>
    /// Optional tenant ID for OAuth authentication.
    /// Only used when AuthenticationMode is DefaultAzureCredential or InteractiveBrowserCredential.
    /// </summary>
    public string? TenantId { get; set; } = null;

    /// <summary>
    /// Indicates whether the model is a reasoning model (GPT-5 series, o1, o3, o4-mini, etc.).
    /// Reasoning models require max_completion_tokens instead of max_tokens.
    /// Set to true for: gpt-5, gpt-5-mini, gpt-5-pro, gpt-5-nano, o1, o1-mini, o3, o3-mini, o3-pro, o4-mini.
    /// Default is false (traditional models like GPT-4, GPT-4o).
    /// </summary>
    public bool IsReasoningModel { get; set; } = false;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ConfigurationException("Azure OpenAI Endpoint cannot be null or whitespace");

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            throw new ConfigurationException("Azure OpenAI Endpoint must be a valid URI");

        // Validate AuthenticationMode is explicitly set
        if (AuthenticationMode == AuthenticationMode.Unspecified)
        {
            throw new ConfigurationException(
                "Azure OpenAI AuthenticationMode must be explicitly specified. " +
                "Set AuthenticationMode to 'ApiKey', 'DefaultAzureCredential', or 'InteractiveBrowserCredential' in your configuration.");
        }

        // Conditionally validate ApiKey only when using ApiKey authentication
        if (AuthenticationMode == AuthenticationMode.ApiKey)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ConfigurationException(
                    "Azure OpenAI ApiKey is required when AuthenticationMode is ApiKey. " +
                    "Either provide an ApiKey or set AuthenticationMode to 'DefaultAzureCredential' or 'InteractiveBrowserCredential'.");
        }

        if (string.IsNullOrWhiteSpace(DeploymentName))
            throw new ConfigurationException("Azure OpenAI DeploymentName cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(ApiVersion))
            throw new ConfigurationException("Azure OpenAI ApiVersion cannot be null or whitespace");
    }
}
