using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

/// <summary>
/// Tool for updating long-term memory.
/// Allows agents to store persistent information about the user.
/// </summary>
public class LongTermMemoryUpdateTool : ITool
{
    private readonly IReadOnlyDictionary<string, string> _metadata;

    public LongTermMemoryUpdateTool()
    {
        var metadata = new Dictionary<string, string>
        {
            ["SourceType"] = "BuiltInLongTermMemory",
            ["Category"] = "Memory"
        };

        _metadata = metadata;
    }

    public string Name => "long_term_memory_update";

    public string Description =>
        "Update long-term memory about the user for the current mode (Normal or Teaching). " +
        "Store persistent information about user preferences, context, and background in markdown format. " +
        "GUARDRAILS: NEVER store sensitive information (passwords, API keys, personal identifiers like SSN, credit card numbers). " +
        "ONLY store preferences, context, and non-sensitive background information. " +
        "Memory is limited to 10,000 characters - be concise and summarize when needed.";

    public string ParametersSchema => """
    {
      "type": "object",
      "properties": {
        "content": {
          "type": "string",
          "description": "The complete memory content in markdown format. This REPLACES all existing memory (not append)."
        },
        "reason": {
          "type": "string",
          "description": "Brief explanation of why memory is being updated (for transparency)."
        }
      },
      "required": ["content", "reason"]
    }
    """;

    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;

    public IReadOnlyDictionary<string, string> Metadata => _metadata;
}
