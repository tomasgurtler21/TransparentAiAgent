namespace TransparentAiAgentCore.Infrastructure.Tools;

/// <summary>
/// Tracks tool usage statistics for all tools
/// </summary>
public class ToolUsageStatistics : IToolUsageStatistics
{
    private readonly Dictionary<string, ToolStats> _stats = new();
    private readonly object _lock = new();

    public void RecordToolCall(string toolName, bool success, TimeSpan duration)
    {
        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));

        lock (_lock)
        {
            if (!_stats.ContainsKey(toolName))
            {
                _stats[toolName] = new ToolStats { ToolName = toolName };
            }

            var stats = _stats[toolName];
            stats.CallCount++;
            if (success) stats.SuccessCount++;
            else stats.FailureCount++;
            stats.TotalDuration += duration;
            stats.LastUsed = DateTime.UtcNow;
        }
    }

    public ToolStats GetToolStats(string toolName)
    {
        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));

        lock (_lock)
        {
            return _stats.TryGetValue(toolName, out var stats)
                ? stats
                : new ToolStats { ToolName = toolName };
        }
    }

    public Dictionary<string, ToolStats> GetAllStats()
    {
        lock (_lock)
        {
            return new Dictionary<string, ToolStats>(_stats);
        }
    }
}
