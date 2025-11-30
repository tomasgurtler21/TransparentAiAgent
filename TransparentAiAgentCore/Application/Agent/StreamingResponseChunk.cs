namespace TransparentAiAgentCore.Application.Agent;

/// <summary>
/// Represents a chunk of streaming response
/// </summary>
public record StreamingResponseChunk(
    string? ContentDelta,
    bool IsComplete,
    StreamingStatus? Status = null)
{
    // Property for backwards compatibility and null safety
    public string ContentDeltaSafe => ContentDelta ?? string.Empty;
}

/// <summary>
/// Represents the status of streaming response
/// </summary>
public enum StreamingStatus
{
    /// <summary>
    /// Streaming text content from LLM
    /// </summary>
    Streaming,

    /// <summary>
    /// Executing tool calls after stream completes
    /// </summary>
    ExecutingTools,

    /// <summary>
    /// Tool execution completed, results are ready and added to conversation
    /// </summary>
    ToolResultsReady,

    /// <summary>
    /// All streaming and tool execution completed
    /// </summary>
    Completed,

    /// <summary>
    /// An error occurred during streaming or tool execution
    /// </summary>
    Error
}
