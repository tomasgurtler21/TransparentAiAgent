namespace TransparentAiAgentCore.Domain.Transparency.EventData;

/// <summary>
/// Contains data about message parsing success or failure for transparency logging
/// </summary>
public class MessageParsingEventData
{
    public string RawData { get; }
    public string? ParsedData { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }

    public MessageParsingEventData(string rawData, string? parsedData, bool success, string? errorMessage)
    {
        if (rawData == null)
            throw new ArgumentException("Raw data cannot be null", nameof(rawData));

        RawData = rawData;
        ParsedData = parsedData;
        Success = success;
        ErrorMessage = errorMessage;
    }
}
