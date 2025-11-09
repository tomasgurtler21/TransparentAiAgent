using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

/// <summary>
/// Implements a stack-based configuration overlay system for temporarily
/// overriding configuration values in scenarios and teaching mode.
/// Thread-safe for concurrent access.
/// </summary>
public class ConfigurationOverlayService : IConfigurationOverlay
{
    private readonly Dictionary<string, object> _baseConfiguration;
    private readonly Stack<Dictionary<string, object>> _overlayStack;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of ConfigurationOverlayService.
    /// </summary>
    /// <param name="baseConfiguration">The base configuration to use when no overlays are active.</param>
    public ConfigurationOverlayService(Dictionary<string, object> baseConfiguration)
    {
        _baseConfiguration = baseConfiguration ?? throw new ArgumentNullException(nameof(baseConfiguration));
        _overlayStack = new Stack<Dictionary<string, object>>();
    }

    /// <inheritdoc />
    public void PushOverlay(IReadOnlyDictionary<string, object> overlayValues)
    {
        if (overlayValues == null)
            throw new ArgumentNullException(nameof(overlayValues));

        lock (_lock)
        {
            // Create a mutable copy for the stack
            var overlay = new Dictionary<string, object>(overlayValues);
            _overlayStack.Push(overlay);
        }
    }

    /// <inheritdoc />
    public void PopOverlay()
    {
        lock (_lock)
        {
            if (_overlayStack.Count == 0)
                throw new InvalidOperationException("No configuration overlays to pop.");

            _overlayStack.Pop();
        }
    }

    /// <inheritdoc />
    public T? GetValue<T>(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        lock (_lock)
        {
            // Search from most recent overlay to oldest
            foreach (var overlay in _overlayStack)
            {
                if (overlay.TryGetValue(key, out var value))
                {
                    return ConvertValue<T>(value);
                }
            }

            // Fall back to base configuration
            if (_baseConfiguration.TryGetValue(key, out var baseValue))
            {
                return ConvertValue<T>(baseValue);
            }

            // Key not found anywhere
            return default;
        }
    }

    /// <inheritdoc />
    public T GetValue<T>(string key, T defaultValue)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        lock (_lock)
        {
            // Search from most recent overlay to oldest
            foreach (var overlay in _overlayStack)
            {
                if (overlay.TryGetValue(key, out var value))
                {
                    return ConvertValue<T>(value) ?? defaultValue;
                }
            }

            // Fall back to base configuration
            if (_baseConfiguration.TryGetValue(key, out var baseValue))
            {
                return ConvertValue<T>(baseValue) ?? defaultValue;
            }

            // Key not found anywhere
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public bool HasKey(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        lock (_lock)
        {
            // Check overlays first
            foreach (var overlay in _overlayStack)
            {
                if (overlay.ContainsKey(key))
                    return true;
            }

            // Check base configuration
            return _baseConfiguration.ContainsKey(key);
        }
    }

    /// <inheritdoc />
    public void ClearAllOverlays()
    {
        lock (_lock)
        {
            _overlayStack.Clear();
        }
    }

    /// <inheritdoc />
    public int OverlayCount
    {
        get
        {
            lock (_lock)
            {
                return _overlayStack.Count;
            }
        }
    }

    /// <summary>
    /// Converts a value to the requested type with proper null handling.
    /// </summary>
    private T? ConvertValue<T>(object value)
    {
        if (value == null)
            return default;

        try
        {
            // Handle direct type match
            if (value is T typedValue)
                return typedValue;

            // Handle convertible types
            if (value is IConvertible)
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            // Try casting
            return (T)value;
        }
        catch
        {
            // If conversion fails, return default
            return default;
        }
    }
}
