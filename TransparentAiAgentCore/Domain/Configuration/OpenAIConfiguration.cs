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

    /// <summary>
    /// Indicates whether the model is a reasoning model (o1, o3, o4-mini, etc.).
    /// Reasoning models require max_completion_tokens instead of max_tokens
    /// and do not support temperature/top_p parameters.
    /// Set to true for: o1, o1-mini, o3, o3-mini, o3-pro, o4-mini.
    /// Default is false (traditional models like GPT-4, GPT-4o).
    /// </summary>
    public bool IsReasoningModel { get; set; } = false;

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
