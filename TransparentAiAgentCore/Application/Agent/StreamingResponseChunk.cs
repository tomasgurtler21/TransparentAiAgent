namespace TransparentAiAgentCore.Application.Agent;

/// <summary>
/// Represents a chunk of streaming response
/// </summary>
public class StreamingResponseChunk
{
    public string ContentDelta { get; }
    public bool IsComplete { get; }

    public StreamingResponseChunk(string contentDelta, bool isComplete = false)
    {
        ContentDelta = contentDelta ?? string.Empty;
        IsComplete = isComplete;
    }
}
