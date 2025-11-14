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

    /// <summary>
    /// Extended thinking delta from the model (e.g., Anthropic's thinking blocks).
    /// This represents a chunk of the model's internal reasoning process.
    /// </summary>
    public string? ThinkingDelta { get; }

    /// <summary>
    /// Gets the fully accumulated thinking content. Only populated in the final chunk (when IsComplete is true).
    /// </summary>
    public string? AccumulatedThinking { get; }

    public StreamingLLMChunk(
        string contentDelta,
        LLMToolCall? toolCallDelta = null,
        bool isComplete = false,
        string? finishReason = null,
        List<LLMToolCall>? accumulatedToolCalls = null,
        string? thinkingDelta = null,
        string? accumulatedThinking = null)
    {
        ContentDelta = contentDelta ?? string.Empty;
        ToolCallDelta = toolCallDelta;
        IsComplete = isComplete;
        FinishReason = finishReason;
        AccumulatedToolCalls = accumulatedToolCalls;
        ThinkingDelta = thinkingDelta;
        AccumulatedThinking = accumulatedThinking;
    }
}
