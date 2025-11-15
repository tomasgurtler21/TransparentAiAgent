using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class OpenAIConfiguration
{
    /// <summary>
    /// API Key for authentication with OpenAI.
    /// Required for all OpenAI API requests.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name to use for requests.
    /// Examples: gpt-4o, gpt-4o-mini, gpt-4-turbo, gpt-3.5-turbo, o1-preview, o3-mini
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom endpoint URL.
    /// Defaults to api.openai.com if not specified.
    /// </summary>
    public string? Endpoint { get; set; } = null;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("OpenAI ApiKey cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Model))
            throw new ConfigurationException("OpenAI Model cannot be null or whitespace");

        // Validate custom endpoint if provided
        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
                throw new ConfigurationException("OpenAI Endpoint must be a valid URI");
        }
    }
}
