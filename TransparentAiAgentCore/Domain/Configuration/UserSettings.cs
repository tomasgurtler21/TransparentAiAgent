namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// User-specific settings that can be modified at runtime.
/// Stored in user's AppData directory.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Context window size for the conversation.
    /// </summary>
    public int ContextWindowSize { get; set; } = 200;

    /// <summary>
    /// Enable/disable long-term memory feature.
    /// </summary>
    public bool EnableMemory { get; set; } = false;
}
