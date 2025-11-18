namespace TransparentAiAgentCore.Infrastructure.Configuration;

/// <summary>
/// Specifies the type of configuration change that occurred.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// A configuration overlay was pushed onto the stack.
    /// </summary>
    Push,

    /// <summary>
    /// A configuration overlay was popped from the stack.
    /// </summary>
    Pop,

    /// <summary>
    /// All configuration overlays were cleared.
    /// </summary>
    Clear
}

/// <summary>
/// Event arguments for configuration overlay changes.
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of configuration change.
    /// </summary>
    public ChangeType Type { get; }

    /// <summary>
    /// Gets the configuration values that changed (if applicable).
    /// Null for Clear operations.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ChangedValues { get; }

    /// <summary>
    /// Initializes a new instance of ConfigurationChangedEventArgs.
    /// </summary>
    /// <param name="type">The type of configuration change.</param>
    /// <param name="changedValues">The configuration values that changed.</param>
    public ConfigurationChangedEventArgs(ChangeType type, IReadOnlyDictionary<string, object>? changedValues)
    {
        Type = type;
        ChangedValues = changedValues;
    }
}
