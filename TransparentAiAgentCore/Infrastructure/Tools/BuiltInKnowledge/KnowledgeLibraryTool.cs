using System.Text;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

/// <summary>
/// Represents the knowledge library query tool.
/// Allows agents to retrieve critical guardrails from the curated knowledge library.
/// </summary>
public class KnowledgeLibraryTool : ITool
{
    private readonly IReadOnlyDictionary<string, string> _metadata;
    private readonly string _description;
    private readonly string _parametersSchema;

    public KnowledgeLibraryTool(IReadOnlyList<KnowledgeEntrySummary> availableTopics)
    {
        if (availableTopics == null)
            throw new ArgumentNullException(nameof(availableTopics));

        var metadata = new Dictionary<string, string>
        {
            ["SourceType"] = "BuiltInKnowledge",
            ["Category"] = "Teaching"
        };

        _metadata = metadata;
        _description = BuildDescription(availableTopics);
        _parametersSchema = BuildParametersSchema(availableTopics);
    }

    public string Name => "knowledge_library_query";

    public string Description => _description;

    public string ParametersSchema => _parametersSchema;

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    private static string BuildDescription(IReadOnlyList<KnowledgeEntrySummary> availableTopics)
    {
        var sb = new StringBuilder();
        sb.Append("Retrieve critical guardrails from the knowledge library for a specific topic. ");
        sb.Append("Returns essential principles and red lines. Use when teaching security, privacy, or safety-critical concepts. ");
        sb.Append("The guardrails guide your teaching; fill in details using your own knowledge.");

        if (availableTopics.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Available topics:");

            // Group by category for better organization
            var byCategory = availableTopics
                .GroupBy(t => t.Category)
                .OrderBy(g => g.Key);

            foreach (var category in byCategory)
            {
                sb.AppendLine($"  {category.Key}:");
                foreach (var topic in category.OrderBy(t => t.Topic))
                {
                    sb.AppendLine($"    - {topic.Id}: {topic.Topic}");
                }
            }
        }

        return sb.ToString();
    }

    private static string BuildParametersSchema(IReadOnlyList<KnowledgeEntrySummary> availableTopics)
    {
        var topicIds = string.Join(", ", availableTopics.Select(t => $"'{t.Id}'").OrderBy(id => id));
        var exampleId = availableTopics.FirstOrDefault()?.Id ?? "example-topic";

        return $$"""
        {
          "type": "object",
          "properties": {
            "topic": {
              "type": "string",
              "description": "The topic ID to query (e.g., '{{exampleId}}'). Available topics: {{topicIds}}"
            }
          },
          "required": ["topic"]
        }
        """;
    }
}
