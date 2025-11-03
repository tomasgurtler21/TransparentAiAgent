namespace TransparentAiAgentCore.Domain.Transparency.EventData;

/// <summary>
/// Contains raw LLM request data for transparency logging
/// </summary>
public class RawLLMRequestData
{
    public string CorrelationId { get; }
    public string Provider { get; }
    public string RequestJson { get; }
    public int MessageCount { get; }
    public DateTime SentAt { get; }

    public RawLLMRequestData(string correlationId, string provider, string requestJson, int messageCount)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or whitespace", nameof(correlationId));

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be null or whitespace", nameof(provider));

        if (requestJson == null)
            throw new ArgumentException("Request JSON cannot be null", nameof(requestJson));

        if (messageCount < 0)
            throw new ArgumentException("Message count cannot be negative", nameof(messageCount));

        CorrelationId = correlationId;
        Provider = provider;
        RequestJson = requestJson;
        MessageCount = messageCount;
        SentAt = DateTime.UtcNow;
    }
}
