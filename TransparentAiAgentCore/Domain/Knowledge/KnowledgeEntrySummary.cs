namespace TransparentAiAgentCore.Domain.Knowledge;

/// <summary>
/// Lightweight summary of a knowledge entry (for indexing and listing).
/// </summary>
public class KnowledgeEntrySummary
{
    public string Id { get; }
    public string Topic { get; }
    public string Category { get; }
    public string Summary { get; }
    public IReadOnlyList<string> Keywords { get; }
    public string KnowledgeGapLikelihood { get; }
    public string LastUpdated { get; }
    public string LastChecked { get; }

    public KnowledgeEntrySummary(
        string id,
        string topic,
        string category,
        string summary,
        string knowledgeGapLikelihood,
        string lastUpdated,
        string lastChecked,
        List<string>? keywords = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or whitespace", nameof(id));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or whitespace", nameof(topic));

        Id = id;
        Topic = topic;
        Category = category;
        Summary = summary;
        KnowledgeGapLikelihood = knowledgeGapLikelihood;
        LastUpdated = lastUpdated;
        LastChecked = lastChecked;
        Keywords = (keywords ?? new List<string>()).AsReadOnly();
    }

    // Factory method to create from KnowledgeEntry
    public static KnowledgeEntrySummary FromEntry(KnowledgeEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        return new KnowledgeEntrySummary(
            entry.Id,
            entry.Topic,
            entry.Category,
            entry.Summary,
            entry.KnowledgeGapLikelihood,
            entry.LastUpdated,
            entry.LastChecked,
            entry.Keywords.ToList());
    }
}
