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

    /// <summary>
    /// Optional custom endpoint URL.
    /// Defaults to https://api.anthropic.com if not specified.
    /// </summary>
    public string? Endpoint { get; set; } = null;

    /// <summary>
    /// Extended thinking configuration for Claude models
    /// </summary>
    public ExtendedThinkingConfiguration? ExtendedThinking { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("Anthropic ApiKey cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Model))
            throw new ConfigurationException("Anthropic Model cannot be null or whitespace");

        if (!string.IsNullOrWhiteSpace(Endpoint) && !Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            throw new ConfigurationException("Anthropic Endpoint must be a valid URI");

        ExtendedThinking?.Validate();
    }
}

public class ExtendedThinkingConfiguration
{
    /// <summary>
    /// Whether extended thinking is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Maximum tokens Claude can use for internal reasoning
    /// Minimum: 1024, Must be less than max_tokens
    /// Recommended starting point: 5000-10000
    /// </summary>
    public int BudgetTokens { get; set; } = 5000;

    public void Validate()
    {
        if (Enabled && BudgetTokens < 1024)
            throw new ConfigurationException("ExtendedThinking.BudgetTokens must be at least 1024 when enabled");
    }
}
