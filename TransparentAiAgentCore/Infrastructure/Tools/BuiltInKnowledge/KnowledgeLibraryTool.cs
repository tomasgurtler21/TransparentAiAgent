using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

/// <summary>
/// Represents the knowledge library query tool.
/// Allows agents to retrieve critical guardrails from the curated knowledge library.
/// </summary>
public class KnowledgeLibraryTool : ITool
{
    private readonly IReadOnlyDictionary<string, string> _metadata;

    public KnowledgeLibraryTool()
    {
        var metadata = new Dictionary<string, string>
        {
            ["SourceType"] = "BuiltInKnowledge",
            ["Category"] = "Teaching"
        };

        _metadata = metadata;
    }

    public string Name => "knowledge_library_query";

    public string Description =>
        "Retrieve critical guardrails from the knowledge library for a specific topic. " +
        "Returns essential principles and red lines. Use when teaching security, privacy, or safety-critical concepts. " +
        "The guardrails guide your teaching; fill in details using your own knowledge.";

    public string ParametersSchema => """
    {
      "type": "object",
      "properties": {
        "topic": {
          "type": "string",
          "description": "The topic ID to query (e.g., 'api-key-security'). Available topics are listed in your system instructions."
        }
      },
      "required": ["topic"]
    }
    """;

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public IReadOnlyDictionary<string, string> Metadata => _metadata;
}
