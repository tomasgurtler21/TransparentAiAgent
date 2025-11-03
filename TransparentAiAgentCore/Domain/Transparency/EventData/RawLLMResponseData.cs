namespace TransparentAiAgentCore.Domain.Transparency.EventData;

/// <summary>
/// Contains raw LLM response data for transparency logging
/// </summary>
public class RawLLMResponseData
{
    public string CorrelationId { get; }
    public string Provider { get; }
    public string ResponseJson { get; }
    public int? StatusCode { get; }
    public TimeSpan Latency { get; }
    public DateTime ReceivedAt { get; }

    public RawLLMResponseData(string correlationId, string provider, string responseJson, int? statusCode, TimeSpan latency)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or whitespace", nameof(correlationId));

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be null or whitespace", nameof(provider));

        if (responseJson == null)
            throw new ArgumentException("Response JSON cannot be null", nameof(responseJson));

        if (latency < TimeSpan.Zero)
            throw new ArgumentException("Latency cannot be negative", nameof(latency));

        CorrelationId = correlationId;
        Provider = provider;
        ResponseJson = responseJson;
        StatusCode = statusCode;
        Latency = latency;
        ReceivedAt = DateTime.UtcNow;
    }
}
