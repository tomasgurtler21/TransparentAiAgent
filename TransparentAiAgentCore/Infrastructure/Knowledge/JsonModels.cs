namespace TransparentAiAgentCore.Infrastructure.Knowledge;

using TransparentAiAgentCore.Domain.Knowledge;

/// <summary>
/// JSON DTO for KnowledgeEntry deserialization.
/// Maps JSON structure to domain model.
/// </summary>
public class KnowledgeEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string KnowledgeGapLikelihood { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
    public string LastChecked { get; set; } = string.Empty;
    public List<string>? Keywords { get; set; }
    public KnowledgeContentDto Content { get; set; } = new();

    /// <summary>
    /// Converts this DTO to a domain model, applying validation.
    /// </summary>
    public KnowledgeEntry ToDomain()
    {
        var domainContent = Content.ToDomain();

        return new KnowledgeEntry(
            Id,
            Topic,
            Category,
            Summary,
            KnowledgeGapLikelihood,
            LastUpdated,
            LastChecked,
            domainContent,
            Keywords);
    }
}

/// <summary>
/// JSON DTO for KnowledgeContent deserialization.
/// </summary>
public class KnowledgeContentDto
{
    public string Overview { get; set; } = string.Empty;
    public List<string>? KeyPoints { get; set; }
    public List<KnowledgeExampleDto>? Examples { get; set; }
    public List<string>? Warnings { get; set; }
    public List<string>? BestPractices { get; set; }
    public List<string>? RelatedTopics { get; set; }
    public List<KnowledgeReferenceDto>? References { get; set; }

    public KnowledgeContent ToDomain()
    {
        var domainExamples = Examples?.Select(e => e.ToDomain()).ToList();
        var domainReferences = References?.Select(r => r.ToDomain()).ToList();

        return new KnowledgeContent(
            Overview,
            KeyPoints,
            domainExamples,
            Warnings,
            BestPractices,
            RelatedTopics,
            domainReferences);
    }
}

/// <summary>
/// JSON DTO for KnowledgeExample deserialization.
/// </summary>
public class KnowledgeExampleDto
{
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? Code { get; set; }

    public KnowledgeExample ToDomain()
    {
        return new KnowledgeExample(Title, Explanation, Code);
    }
}

/// <summary>
/// JSON DTO for KnowledgeReference deserialization.
/// </summary>
public class KnowledgeReferenceDto
{
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }

    public KnowledgeReference ToDomain()
    {
        return new KnowledgeReference(Title, Url);
    }
}
