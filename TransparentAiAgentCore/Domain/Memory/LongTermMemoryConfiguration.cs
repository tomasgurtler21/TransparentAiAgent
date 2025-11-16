namespace TransparentAiAgentCore.Domain.Memory;

/// <summary>
/// Configuration for long-term memory feature.
/// </summary>
public class LongTermMemoryConfiguration
{
    /// <summary>
    /// Enable/disable long-term memory feature globally.
    /// User can still toggle per-session via UI checkbox.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Directory path for memory files (relative to app root).
    /// </summary>
    public string StorageDirectory { get; set; } = "data/memory";

    /// <summary>
    /// Maximum memory file size in characters.
    /// </summary>
    public int MaxCharacters { get; set; } = 10_000;

    /// <summary>
    /// Whether to auto-load memory at conversation start.
    /// </summary>
    public bool AutoLoadOnStart { get; set; } = true;

    /// <summary>
    /// Whether to prompt for memory update on conversation end.
    /// </summary>
    public bool PromptUpdateOnEnd { get; set; } = true;

    /// <summary>
    /// Timeout for memory update prompt (seconds).
    /// </summary>
    public int UpdatePromptTimeoutSeconds { get; set; } = 30;
}
