namespace TransparentAiAgentCore.Infrastructure.Tools;

/// <summary>
/// Interface for tracking tool usage statistics
/// </summary>
public interface IToolUsageStatistics
{
    /// <summary>
    /// Record a tool call with its outcome and duration
    /// </summary>
    void RecordToolCall(string toolName, bool success, TimeSpan duration);

    /// <summary>
    /// Get statistics for a specific tool
    /// </summary>
    ToolStats GetToolStats(string toolName);

    /// <summary>
    /// Get statistics for all tools
    /// </summary>
    Dictionary<string, ToolStats> GetAllStats();
}

/// <summary>
/// Statistics for a single tool
/// </summary>
public class ToolStats
{
    public string ToolName { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate => CallCount > 0 ? (double)SuccessCount / CallCount : 0;
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration => CallCount > 0 ? TotalDuration / CallCount : TimeSpan.Zero;
    public DateTime? LastUsed { get; set; }
}
