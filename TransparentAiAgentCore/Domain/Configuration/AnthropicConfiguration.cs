using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class AnthropicConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("Anthropic ApiKey cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Model))
            throw new ConfigurationException("Anthropic Model cannot be null or whitespace");
    }
}
