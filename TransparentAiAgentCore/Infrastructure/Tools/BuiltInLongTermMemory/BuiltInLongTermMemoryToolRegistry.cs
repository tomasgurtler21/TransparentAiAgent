using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

/// <summary>
/// Registry for built-in long-term memory tools.
/// Provides long_term_memory_read and long_term_memory_update tools for LLM access to persistent user memory.
/// </summary>
public class BuiltInLongTermMemoryToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<ITool> _tools;

    public BuiltInLongTermMemoryToolRegistry()
    {
        _tools = new List<ITool>
        {
            new LongTermMemoryReadTool(),
            new LongTermMemoryUpdateTool()
        }.AsReadOnly();
    }

    /// <summary>
    /// Gets all registered long-term memory tools.
    /// </summary>
    public IReadOnlyList<ITool> GetAllTools()
    {
        return _tools;
    }

    /// <summary>
    /// Gets a tool by name (case-insensitive).
    /// </summary>
    public ITool? GetTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        return _tools.FirstOrDefault(t =>
            t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    public bool HasTool(string toolName)
    {
        return GetTool(toolName) != null;
    }

    /// <summary>
    /// Refreshes the tool registry.
    /// For static built-in tools, this is a no-op.
    /// </summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Built-in tools are static, no refresh needed
        return Task.CompletedTask;
    }
}
