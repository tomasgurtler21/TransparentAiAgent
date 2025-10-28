using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Abstraction for LLM providers
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Send a request to the LLM and get a complete response
    /// </summary>
    Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a request to the LLM and stream the response
    /// </summary>
    IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the provider name
    /// </summary>
    string ProviderName { get; }
}
