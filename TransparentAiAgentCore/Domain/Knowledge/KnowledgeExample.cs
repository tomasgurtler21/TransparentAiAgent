namespace TransparentAiAgentCore.Domain.Knowledge;

/// <summary>
/// Represents a code or configuration example in a knowledge entry.
/// </summary>
public class KnowledgeExample
{
    public string Title { get; }
    public string Code { get; }
    public string Explanation { get; }

    public KnowledgeExample(string title, string explanation, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or whitespace", nameof(title));

        if (string.IsNullOrWhiteSpace(explanation))
            throw new ArgumentException("Explanation cannot be null or whitespace", nameof(explanation));

        Title = title;
        Explanation = explanation;
        Code = code ?? string.Empty;
    }
}
