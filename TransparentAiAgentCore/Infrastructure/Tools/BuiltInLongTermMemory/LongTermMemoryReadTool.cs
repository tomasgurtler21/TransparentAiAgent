using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

/// <summary>
/// Tool for reading long-term memory.
/// Allows agents to retrieve persistent information stored about the user.
/// </summary>
public class LongTermMemoryReadTool : ITool
{
    private readonly IReadOnlyDictionary<string, string> _metadata;

    public LongTermMemoryReadTool()
    {
        var metadata = new Dictionary<string, string>
        {
            ["SourceType"] = "BuiltInLongTermMemory",
            ["Category"] = "Memory"
        };

        _metadata = metadata;
    }

    public string Name => "long_term_memory_read";

    public string Description =>
        "Read long-term memory about the user for the current mode (Normal or Teaching). " +
        "Returns markdown-formatted persistent information about user preferences, context, and background. " +
        "Use this to personalize interactions and maintain continuity across conversations.";

    public string ParametersSchema => """
    {
      "type": "object",
      "properties": {}
    }
    """;

    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;

    public IReadOnlyDictionary<string, string> Metadata => _metadata;
}
