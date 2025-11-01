using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class MCPConfiguration
{
    public List<MCPServerConfiguration> Servers { get; set; } = new();

    /// <summary>
    /// Auto-discover tools on startup (default: true)
    /// </summary>
    public bool AutoDiscoverTools { get; set; } = true;

    /// <summary>
    /// Timeout for tool execution (seconds)
    /// </summary>
    public int ToolExecutionTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Maximum tool call depth (prevent infinite loops)
    /// </summary>
    public int MaxToolCallDepth { get; set; } = 10;

    public void Validate()
    {
        foreach (var server in Servers)
        {
            server.Validate();
        }

        if (ToolExecutionTimeoutSeconds < 1)
            throw new ConfigurationException("ToolExecutionTimeoutSeconds must be at least 1");

        if (MaxToolCallDepth < 1 || MaxToolCallDepth > 50)
            throw new ConfigurationException("MaxToolCallDepth must be between 1 and 50");
    }
}
