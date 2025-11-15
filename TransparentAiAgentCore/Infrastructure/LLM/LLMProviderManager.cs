using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Manages multiple LLM provider instances with lazy-loading and caching.
/// </summary>
public class LLMProviderManager : ILLMProviderManager, IDisposable
{
    private readonly LLMConfiguration _configuration;
    private readonly ILLMProviderFactory _factory;
    private readonly Dictionary<string, ILLMProvider> _providerCache = new();
    private readonly object _lock = new();
    private string _activeProviderName;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMProviderManager"/> class.
    /// </summary>
    /// <param name="configuration">The LLM configuration.</param>
    /// <param name="factory">The provider factory.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration or factory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no active provider is configured.</exception>
    public LLMProviderManager(LLMConfiguration configuration, ILLMProviderFactory factory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        // Validate that active provider is configured
        if (string.IsNullOrWhiteSpace(configuration.ActiveProvider))
            throw new InvalidOperationException("No active provider configured");

        _activeProviderName = configuration.ActiveProvider;
    }

    public ILLMProvider GetActiveProvider()
    {
        lock (_lock)
        {
            // Check if provider is already cached
            if (!_providerCache.TryGetValue(_activeProviderName, out var provider))
            {
                // Lazy-load: create provider on first access
                var config = _configuration.Providers![_activeProviderName];
                provider = _factory.CreateProvider(_activeProviderName, config);
                _providerCache[_activeProviderName] = provider;
            }
            return provider;
        }
    }

    public Task SetActiveProviderAsync(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
            throw new ArgumentException("Provider name cannot be null or empty", nameof(configName));

        if (!_configuration.Providers!.ContainsKey(configName))
            throw new ArgumentException($"Provider '{configName}' not found in configuration");

        lock (_lock)
        {
            _activeProviderName = configName;
            _configuration.ActiveProvider = configName;
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<ProviderInfo> GetAvailableProviders()
    {
        return _configuration.Providers!.Select(kvp =>
            CreateProviderInfo(kvp.Key, kvp.Value, kvp.Key == _activeProviderName))
            .ToList();
    }

    public ProviderInfo GetCurrentProviderInfo()
    {
        var config = _configuration.Providers![_activeProviderName];
        return CreateProviderInfo(_activeProviderName, config, isActive: true);
    }

    private ProviderInfo CreateProviderInfo(string configName, ProviderConfig config, bool isActive)
    {
        // Extract model name from parameters (different providers use different keys)
        var modelName = config.Parameters.TryGetValue("Model", out var model)
            ? model?.ToString() ?? "Unknown"
            : config.Parameters.TryGetValue("DeploymentName", out var deployment)
                ? deployment?.ToString() ?? "Unknown"
                : "Unknown";

        return new ProviderInfo(
            configName: configName,
            displayName: config.DisplayName,
            providerType: config.Type,
            modelName: modelName,
            isActive: isActive
        );
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var provider in _providerCache.Values.OfType<IDisposable>())
            {
                provider.Dispose();
            }
            _providerCache.Clear();
        }
    }
}
