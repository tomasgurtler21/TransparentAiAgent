namespace TransparentAiAgentCore.Domain.Enums;

/// <summary>
/// Indicates whether a message is currently in the LLM context window
/// </summary>
public enum MessageContextStatus
{
    /// <summary>
    /// Message is within the LLM context window and will be sent to the LLM
    /// </summary>
    InContext,

    /// <summary>
    /// Message was truncated from context due to token limits but remains visible in UI
    /// </summary>
    TruncatedFromContext
}
