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

    /// <summary>
    /// Gets the fully accumulated tool calls. Only populated in the final chunk (when IsComplete is true).
    /// </summary>
    public List<LLMToolCall>? AccumulatedToolCalls { get; }

    public StreamingLLMChunk(
        string contentDelta,
        LLMToolCall? toolCallDelta = null,
        bool isComplete = false,
        string? finishReason = null,
        List<LLMToolCall>? accumulatedToolCalls = null)
    {
        ContentDelta = contentDelta ?? string.Empty;
        ToolCallDelta = toolCallDelta;
        IsComplete = isComplete;
        FinishReason = finishReason;
        AccumulatedToolCalls = accumulatedToolCalls;
    }
}
