using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class AnthropicConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>
    /// Claude model identifier. Use versioned IDs for production:
    /// - "claude-sonnet-4-5-20250929" (Sonnet 4.5 - recommended for accuracy)
    /// - "claude-haiku-4-5-20251001" (Haiku 4.5 - recommended for speed/cost)
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("Anthropic ApiKey cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Model))
            throw new ConfigurationException("Anthropic Model cannot be null or whitespace");
    }
}
