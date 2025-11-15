namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// Display information about an available LLM provider.
/// Used for UI rendering and provider selection.
/// </summary>
public class ProviderInfo
{
    /// <summary>
    /// Gets the configuration name (key in appsettings.json).
    /// </summary>
    public string ConfigName { get; }

    /// <summary>
    /// Gets the user-friendly display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the provider type (e.g., "Anthropic", "AzureOpenAI").
    /// </summary>
    public string ProviderType { get; }

    /// <summary>
    /// Gets the model name (e.g., "claude-haiku-4-5-20251001", "gpt-4").
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Gets a value indicating whether this provider is currently active.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderInfo"/> class.
    /// </summary>
    /// <param name="configName">The configuration name.</param>
    /// <param name="displayName">The user-friendly display name.</param>
    /// <param name="providerType">The provider type.</param>
    /// <param name="modelName">The model name.</param>
    /// <param name="isActive">Whether this provider is currently active.</param>
    /// <exception cref="ArgumentNullException">Thrown when any string parameter is null.</exception>
    public ProviderInfo(
        string configName,
        string displayName,
        string providerType,
        string modelName,
        bool isActive)
    {
        ConfigName = configName ?? throw new ArgumentNullException(nameof(configName));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
        ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        IsActive = isActive;
    }
}
