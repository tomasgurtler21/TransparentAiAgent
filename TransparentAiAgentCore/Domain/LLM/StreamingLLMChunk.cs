namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a single chunk in a streaming LLM response
/// </summary>
public class StreamingLLMChunk
{
    public string ContentDelta { get; }
    public LLMToolCall? ToolCallDelta { get; }
    public bool IsComplete { get; }
    public string? FinishReason { get; }

    public StreamingLLMChunk(
        string contentDelta,
        LLMToolCall? toolCallDelta = null,
        bool isComplete = false,
        string? finishReason = null)
    {
        ContentDelta = contentDelta ?? string.Empty;
        ToolCallDelta = toolCallDelta;
        IsComplete = isComplete;
        FinishReason = finishReason;
    }
}
