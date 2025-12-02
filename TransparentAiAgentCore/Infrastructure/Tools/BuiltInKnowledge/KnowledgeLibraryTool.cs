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

        // Overview
        sb.AppendLine("Retrieve critical guardrails from the knowledge library for a specific topic.");
        sb.AppendLine("Returns essential principles, red lines, and corrections - NOT comprehensive documentation.");
        sb.AppendLine();

        // When to use
        sb.AppendLine("WHEN TO USE:");
        sb.AppendLine("- Teaching security, privacy, or safety-critical topics (ALWAYS query for guardrails)");
        sb.AppendLine("- Application-specific features (context windows, MCP, teaching mode)");
        sb.AppendLine("- When you need to correct potential misconceptions");
        sb.AppendLine("- Before teaching best practices (get the \"must-dos\" and \"must-not-dos\")");
        sb.AppendLine();

        // How to use
        sb.AppendLine("HOW TO USE:");
        sb.AppendLine("1. Query the library to get critical guardrails on a topic");
        sb.AppendLine("2. Use the guardrails to guide your teaching");
        sb.AppendLine("3. Fill in details using your built-in knowledge");
        sb.AppendLine("4. The library tells you what's CRITICAL; you provide the comprehensive teaching");
        sb.AppendLine();

        // After querying
        sb.AppendLine("AFTER QUERYING:");
        sb.AppendLine("- Treat the entry as GUARDRAILS, not exhaustive content");
        sb.AppendLine("- Teach comprehensively using the guardrails + your knowledge");
        sb.AppendLine("- Always respect warnings and red lines from the library");
        sb.AppendLine("- Use your judgment to expand on principles with relevant details");
        sb.AppendLine();

        // Knowledge Gap Likelihood explanation
        sb.AppendLine("KNOWLEDGE GAP LIKELIHOOD:");
        sb.AppendLine("Each topic includes a \"knowledgeGapLikelihood\" field indicating how reliable your built-in knowledge is:");
        sb.AppendLine("- Low: Your training data is likely current - rely on inner knowledge confidently");
        sb.AppendLine("- Medium: Your knowledge may be partially outdated - cross-reference with library");
        sb.AppendLine("- High: Your knowledge is very likely outdated - prefer web search if available");

        if (availableTopics.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("AVAILABLE TOPICS:");

            // Group by category for better organization
            var byCategory = availableTopics
                .GroupBy(t => t.Category)
                .OrderBy(g => g.Key);

            foreach (var category in byCategory)
            {
                sb.AppendLine($"  {category.Key}:");
                foreach (var topic in category.OrderBy(t => t.Topic))
                {
                    sb.AppendLine($"    - {topic.Id}: {topic.Topic} (gap likelihood: {topic.KnowledgeGapLikelihood})");
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
