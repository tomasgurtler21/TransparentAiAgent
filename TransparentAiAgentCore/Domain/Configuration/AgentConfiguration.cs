using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class AgentConfiguration
{
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";
    public int ContextWindowSize { get; set; } = 20;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SystemPrompt))
            throw new ConfigurationException("SystemPrompt cannot be null or whitespace");

        if (ContextWindowSize <= 0)
            throw new ConfigurationException("ContextWindowSize must be greater than 0");
    }
}
