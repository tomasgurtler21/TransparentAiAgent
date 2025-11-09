namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// Provides a mechanism for temporarily overlaying configuration values
/// for scenarios and teaching mode demonstrations.
/// Uses a stack-based approach where overlays can be pushed and popped.
/// </summary>
public interface IConfigurationOverlay
{
    /// <summary>
    /// Pushes a configuration overlay onto the stack.
    /// Values in this overlay will take precedence over base configuration
    /// and previous overlays for the same keys.
    /// </summary>
    /// <param name="overlayValues">Configuration key-value pairs to overlay.</param>
    void PushOverlay(IReadOnlyDictionary<string, object> overlayValues);

    /// <summary>
    /// Pops the most recent overlay from the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no overlays are active.</exception>
    void PopOverlay();

    /// <summary>
    /// Gets the effective value for a configuration key, considering all active overlays.
    /// Returns the value from the most recent overlay containing the key,
    /// or the base configuration value if not found in any overlay.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <returns>The effective value, or default(T) if key is not found.</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Gets the effective value for a configuration key, or a default if not found.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found.</param>
    /// <returns>The effective value or default.</returns>
    T GetValue<T>(string key, T defaultValue);

    /// <summary>
    /// Checks if a configuration key exists in any overlay or base configuration.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>True if the key exists; otherwise false.</returns>
    bool HasKey(string key);

    /// <summary>
    /// Clears all overlays and restores original base configuration.
    /// </summary>
    void ClearAllOverlays();

    /// <summary>
    /// Gets the number of active configuration overlays.
    /// </summary>
    int OverlayCount { get; }
}
