namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// LLM generation parameters that can be applied at default or provider level.
/// Supports parameter inheritance where provider-specific values override defaults.
/// </summary>
public class ProviderParameters
{
    /// <summary>
    /// Gets the temperature parameter (controls randomness, typically 0-2).
    /// </summary>
    public double? Temperature { get; }

    /// <summary>
    /// Gets the top-p parameter (nucleus sampling threshold, typically 0-1).
    /// </summary>
    public double? TopP { get; }

    /// <summary>
    /// Gets the maximum tokens to generate in the response.
    /// </summary>
    public int? MaxTokens { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderParameters"/> class.
    /// </summary>
    /// <param name="temperature">The temperature parameter.</param>
    /// <param name="topP">The top-p parameter.</param>
    /// <param name="maxTokens">The maximum tokens parameter.</param>
    public ProviderParameters(double? temperature = null, double? topP = null, int? maxTokens = null)
    {
        Temperature = temperature;
        TopP = topP;
        MaxTokens = maxTokens;
    }

    /// <summary>
    /// Merges this parameters with defaults, using this parameters where specified, defaults otherwise.
    /// This enables parameter inheritance where provider-level values override system-level defaults.
    /// </summary>
    /// <param name="defaults">The default parameters to use as fallback.</param>
    /// <returns>A new <see cref="ProviderParameters"/> with merged values.</returns>
    public ProviderParameters GetEffectiveParameters(ProviderParameters defaults)
    {
        return new ProviderParameters(
            temperature: Temperature ?? defaults.Temperature,
            topP: TopP ?? defaults.TopP,
            maxTokens: MaxTokens ?? defaults.MaxTokens
        );
    }
}
