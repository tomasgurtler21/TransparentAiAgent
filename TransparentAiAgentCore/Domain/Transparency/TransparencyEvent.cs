namespace TransparentAiAgentCore.Domain.Transparency;

public class TransparencyEvent
{
    public Guid Id { get; }
    public TransparencyEventType EventType { get; }
    public DateTime Timestamp { get; }
    public string Data { get; } // JSON serialized data
    public string? AdditionalInfo { get; }

    public TransparencyEvent(TransparencyEventType eventType, string data, string? additionalInfo = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        AdditionalInfo = additionalInfo;
        Timestamp = DateTime.UtcNow;
    }

    // For testing/reconstruction
    public TransparencyEvent(Guid id, TransparencyEventType eventType, DateTime timestamp,
        string data, string? additionalInfo = null)
    {
        Id = id;
        EventType = eventType;
        Timestamp = timestamp;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        AdditionalInfo = additionalInfo;
    }
}
