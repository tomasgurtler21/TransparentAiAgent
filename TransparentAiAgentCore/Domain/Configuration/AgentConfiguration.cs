using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class AgentConfiguration
{
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";

    /// <summary>
    /// System prompt used when in Teaching Mode (default: uses SystemPrompt if not specified)
    /// </summary>
    public string? TeachingModePrompt { get; set; }

    public int ContextWindowSize { get; set; } = 20;

    /// <summary>
    /// Enable tool calling (default: true)
    /// </summary>
    public bool EnableTools { get; set; } = true;

    /// <summary>
    /// Tool execution mode: Sequential or Parallel
    /// </summary>
    public ToolExecutionMode ToolExecutionMode { get; set; } = ToolExecutionMode.Sequential;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SystemPrompt))
            throw new ConfigurationException("SystemPrompt cannot be null or whitespace");

        if (ContextWindowSize <= 0)
            throw new ConfigurationException("ContextWindowSize must be greater than 0");
    }
}

public enum ToolExecutionMode
{
    /// <summary>
    /// Execute tools one at a time (simpler, easier to debug)
    /// </summary>
    Sequential,

    /// <summary>
    /// Execute all tool calls in parallel (faster, but more complex)
    /// </summary>
    Parallel
}
