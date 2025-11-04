using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class LLMConfiguration
{
    public string Provider { get; set; } = "AzureOpenAI";
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int MaxTokens { get; set; } = 4096;

    public AzureOpenAIConfiguration? AzureOpenAI { get; set; }
    public AnthropicConfiguration? Anthropic { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Provider))
            throw new ConfigurationException("Provider cannot be null or whitespace");

        if (Temperature.HasValue && (Temperature.Value < 0 || Temperature.Value > 2))
            throw new ConfigurationException("Temperature must be between 0 and 2");

        if (TopP.HasValue && (TopP.Value < 0 || TopP.Value > 1))
            throw new ConfigurationException("TopP must be between 0 and 1");

        if (MaxTokens <= 0)
            throw new ConfigurationException("MaxTokens must be greater than 0");

        // Validate provider-specific config
        if (Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            if (AzureOpenAI == null)
                throw new ConfigurationException("AzureOpenAI configuration is required when Provider is AzureOpenAI");
            AzureOpenAI.Validate();
        }
        else if (Provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            if (Anthropic == null)
                throw new ConfigurationException("Anthropic configuration is required when Provider is Anthropic");
            Anthropic.Validate();
        }
    }
}
