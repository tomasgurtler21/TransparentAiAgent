using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class LLMConfiguration
{
    // New multi-provider properties
    public string? ActiveProvider { get; set; }
    public ProviderParameters? DefaultParameters { get; set; }
    public Dictionary<string, ProviderConfig>? Providers { get; set; }

    // Existing properties (kept for backward compatibility during transition)
    public string Provider { get; set; } = "AzureOpenAI";
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int MaxTokens { get; set; } = 4096;

    public AzureOpenAIConfiguration? AzureOpenAI { get; set; }
    public OpenAIConfiguration? OpenAI { get; set; }
    public AnthropicConfiguration? Anthropic { get; set; }

    public void Validate()
    {
        // Check if using new multi-provider structure
        if (Providers != null && Providers.Count > 0)
        {
            // Validate new structure
            if (string.IsNullOrWhiteSpace(ActiveProvider))
                throw new ConfigurationException("ActiveProvider cannot be null or whitespace when using multi-provider configuration");

            if (!Providers.ContainsKey(ActiveProvider))
                throw new ConfigurationException($"ActiveProvider '{ActiveProvider}' not found in Providers dictionary");

            // Validate default parameters if present
            if (DefaultParameters != null)
            {
                if (DefaultParameters.Temperature.HasValue && (DefaultParameters.Temperature.Value < 0 || DefaultParameters.Temperature.Value > 2))
                    throw new ConfigurationException("DefaultParameters Temperature must be between 0 and 2");

                if (DefaultParameters.TopP.HasValue && (DefaultParameters.TopP.Value < 0 || DefaultParameters.TopP.Value > 1))
                    throw new ConfigurationException("DefaultParameters TopP must be between 0 and 1");

                if (DefaultParameters.MaxTokens.HasValue && DefaultParameters.MaxTokens.Value <= 0)
                    throw new ConfigurationException("DefaultParameters MaxTokens must be greater than 0");
            }

            // Note: ProviderConfig validation is done by ProviderConfigValidator
            return;
        }

        // Validate old structure (backward compatibility)
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
        else if (Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            if (OpenAI == null)
                throw new ConfigurationException("OpenAI configuration is required when Provider is OpenAI");
            OpenAI.Validate();
        }
        else if (Provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            if (Anthropic == null)
                throw new ConfigurationException("Anthropic configuration is required when Provider is Anthropic");
            Anthropic.Validate();
        }
    }
}
