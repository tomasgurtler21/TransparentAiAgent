using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

/// <summary>
/// Registry for built-in knowledge library tools.
/// Provides the knowledge_library_query tool for LLM access to curated knowledge entries.
/// </summary>
public class BuiltInKnowledgeToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<ITool> _tools;

    public BuiltInKnowledgeToolRegistry()
    {
        _tools = new List<ITool>
        {
            new KnowledgeLibraryTool()
        }.AsReadOnly();
    }

    /// <summary>
    /// Gets all registered knowledge library tools.
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
