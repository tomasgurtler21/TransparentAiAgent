namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// Configuration for a single LLM provider instance.
/// Supports multi-provider setup where each provider can have distinct settings.
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// Gets the provider type (e.g., "Anthropic", "AzureOpenAI", "OpenAI").
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the user-friendly display name shown in UI.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the provider-specific parameters (e.g., ApiKey, Model, Endpoint).
    /// </summary>
    public Dictionary<string, object> Parameters { get; }

    /// <summary>
    /// Gets optional parameter overrides that take precedence over DefaultParameters.
    /// </summary>
    public ProviderParameters? ParameterOverrides { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderConfig"/> class.
    /// </summary>
    /// <param name="type">The provider type (e.g., "Anthropic", "AzureOpenAI").</param>
    /// <param name="displayName">The user-friendly display name.</param>
    /// <param name="parameters">Provider-specific parameters (e.g., ApiKey, Model).</param>
    /// <param name="parameterOverrides">Optional parameter overrides for this provider.</param>
    /// <exception cref="ArgumentException">Thrown when type or displayName is null or whitespace.</exception>
    public ProviderConfig(
        string type,
        string displayName,
        Dictionary<string, object> parameters,
        ProviderParameters? parameterOverrides = null)
    {
        // Validate Type
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be null or empty", nameof(type));

        // Validate DisplayName
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName cannot be null or empty", nameof(displayName));

        Type = type;
        DisplayName = displayName;
        Parameters = parameters ?? new Dictionary<string, object>();
        ParameterOverrides = parameterOverrides;
    }
}
