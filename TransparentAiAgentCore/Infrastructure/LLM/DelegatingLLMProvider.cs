using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Proxy that delegates all ILLMProvider calls to the currently active provider from ILLMProviderManager.
/// This ensures that provider switching works correctly when ILLMProvider is registered as a singleton.
///
/// Background:
/// - ILLMProvider is registered as Singleton for backward compatibility
/// - Without this proxy, the singleton would cache the first provider and never update
/// - This proxy calls manager.GetActiveProvider() on EVERY request, ensuring current provider is used
/// </summary>
public class DelegatingLLMProvider : ILLMProvider
{
    private readonly ILLMProviderManager _manager;

    /// <summary>
    /// Initializes a new instance of the DelegatingLLMProvider.
    /// </summary>
    /// <param name="manager">The provider manager that tracks the currently active provider.</param>
    public DelegatingLLMProvider(ILLMProviderManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Sends a request to the currently active LLM provider.
    /// </summary>
    public Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var activeProvider = _manager.GetActiveProvider();
        return activeProvider.SendRequestAsync(request, cancellationToken);
    }

    /// <summary>
    /// Streams a request to the currently active LLM provider.
    /// </summary>
    public IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var activeProvider = _manager.GetActiveProvider();
        return activeProvider.StreamRequestAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets the name of the currently active LLM provider.
    /// </summary>
    public string ProviderName => _manager.GetActiveProvider().ProviderName;
}
