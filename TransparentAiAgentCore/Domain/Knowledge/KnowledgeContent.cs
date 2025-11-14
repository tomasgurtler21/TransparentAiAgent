namespace TransparentAiAgentCore.Domain.Knowledge;

/// <summary>
/// Represents the detailed content of a knowledge entry.
/// </summary>
public class KnowledgeContent
{
    public string Overview { get; }
    public IReadOnlyList<string> KeyPoints { get; }
    public IReadOnlyList<KnowledgeExample> Examples { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> BestPractices { get; }
    public IReadOnlyList<string> RelatedTopics { get; }
    public IReadOnlyList<KnowledgeReference> References { get; }

    public KnowledgeContent(
        string overview,
        List<string>? keyPoints = null,
        List<KnowledgeExample>? examples = null,
        List<string>? warnings = null,
        List<string>? bestPractices = null,
        List<string>? relatedTopics = null,
        List<KnowledgeReference>? references = null)
    {
        if (string.IsNullOrWhiteSpace(overview))
            throw new ArgumentException("Overview cannot be null or whitespace", nameof(overview));

        Overview = overview;
        KeyPoints = (keyPoints ?? new List<string>()).AsReadOnly();
        Examples = (examples ?? new List<KnowledgeExample>()).AsReadOnly();
        Warnings = (warnings ?? new List<string>()).AsReadOnly();
        BestPractices = (bestPractices ?? new List<string>()).AsReadOnly();
        RelatedTopics = (relatedTopics ?? new List<string>()).AsReadOnly();
        References = (references ?? new List<KnowledgeReference>()).AsReadOnly();
    }
}

/// <summary>
/// Represents a reference to external documentation or resources.
/// </summary>
public class KnowledgeReference
{
    public string Title { get; }
    public string? Url { get; }

    public KnowledgeReference(string title, string? url = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or whitespace", nameof(title));

        Title = title;
        Url = url;
    }
}
