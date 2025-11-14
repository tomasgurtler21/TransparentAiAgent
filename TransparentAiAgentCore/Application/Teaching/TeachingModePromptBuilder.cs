namespace TransparentAiAgentCore.Application.Teaching;

using System.Text;
using TransparentAiAgentCore.Domain.Knowledge;

/// <summary>
/// Builds system prompt sections for teaching mode, including knowledge library integration.
/// </summary>
public class TeachingModePromptBuilder
{
    private readonly IKnowledgeLibrary _library;

    public TeachingModePromptBuilder(IKnowledgeLibrary library)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
    }

    public string BuildKnowledgeLibrarySection()
    {
        var topics = _library.GetAllTopics();
        var sb = new StringBuilder();

        sb.AppendLine("# Knowledge Library - Guardrails for Teaching");
        sb.AppendLine();
        sb.AppendLine("You have access to a knowledge library containing GUARDRAILS for teaching critical concepts.");
        sb.AppendLine("These entries provide essential principles, red lines, and corrections - NOT comprehensive documentation.");
        sb.AppendLine();
        sb.AppendLine("**How to use:**");
        sb.AppendLine("1. Query the library to get critical guardrails on a topic");
        sb.AppendLine("2. Use the guardrails to guide your teaching");
        sb.AppendLine("3. Fill in details using your built-in knowledge");
        sb.AppendLine("4. The library tells you what's CRITICAL; you provide the comprehensive teaching");
        sb.AppendLine();

        if (topics.Any())
        {
            sb.AppendLine("**Available knowledge topics:**");
            foreach (var topic in topics)
            {
                sb.AppendLine($"- `{topic.Id}`: {topic.Topic} - {topic.Summary} (gap likelihood: {topic.KnowledgeGapLikelihood})");
            }
            sb.AppendLine();
        }

        sb.AppendLine("**When to query the library:**");
        sb.AppendLine("1. Teaching security, privacy, or safety-critical topics (ALWAYS query for guardrails)");
        sb.AppendLine("2. Application-specific features (context windows, MCP, teaching mode)");
        sb.AppendLine("3. When you need to correct potential misconceptions");
        sb.AppendLine("4. Before teaching best practices (get the \"must-dos\" and \"must-not-dos\")");
        sb.AppendLine();

        sb.AppendLine("**After querying:**");
        sb.AppendLine("- Treat the entry as GUARDRAILS, not exhaustive content");
        sb.AppendLine("- Teach comprehensively using the guardrails + your knowledge");
        sb.AppendLine("- Always respect warnings and red lines from the library");
        sb.AppendLine("- Use your judgment to expand on principles with relevant details");
        sb.AppendLine();

        sb.AppendLine("**Understanding Knowledge Gap Likelihood:**");
        sb.AppendLine("Each topic includes a \"knowledgeGapLikelihood\" field indicating how reliable your built-in knowledge is:");
        sb.AppendLine("- **Low**: Your training data is likely current - rely on inner knowledge confidently");
        sb.AppendLine("- **Medium**: Your knowledge may be partially outdated - cross-reference with library");
        sb.AppendLine("- **High**: Your knowledge is very likely outdated - prefer web search if available; if not, warn user about potential outdated information");

        return sb.ToString();
    }
}
