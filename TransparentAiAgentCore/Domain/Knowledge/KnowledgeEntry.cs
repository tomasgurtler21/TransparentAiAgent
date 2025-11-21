namespace TransparentAiAgentCore.Domain.Knowledge;

using System.Text.RegularExpressions;

/// <summary>
/// Represents a complete knowledge entry with metadata and content.
/// </summary>
public class KnowledgeEntry
{
    private static readonly Regex KebabCaseRegex = new Regex(@"^[a-z0-9-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidGapLikelihoods = new() { "low", "medium", "high" };

    public string Id { get; }
    public string Topic { get; }
    public string Category { get; }
    public IReadOnlyList<string> Keywords { get; }
    public string Summary { get; }
    public string KnowledgeGapLikelihood { get; }
    public string LastUpdated { get; }
    public string LastChecked { get; }
    public KnowledgeContent Content { get; }

    public KnowledgeEntry(
        string id,
        string topic,
        string category,
        string summary,
        string knowledgeGapLikelihood,
        string lastUpdated,
        string lastChecked,
        KnowledgeContent content,
        List<string>? keywords = null)
    {
        // Validate id
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or whitespace", nameof(id));
        if (!KebabCaseRegex.IsMatch(id))
            throw new ArgumentException("Id must be in kebab-case format (lowercase letters, numbers, and hyphens only)", nameof(id));

        // Validate required string fields
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or whitespace", nameof(topic));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or whitespace", nameof(category));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Summary cannot be null or whitespace", nameof(summary));

        // Validate knowledgeGapLikelihood
        if (!ValidGapLikelihoods.Contains(knowledgeGapLikelihood?.ToLowerInvariant() ?? ""))
            throw new ArgumentException("KnowledgeGapLikelihood must be 'low', 'medium', or 'high'", nameof(knowledgeGapLikelihood));

        // Validate dates (YYYY-MM-DD format)
        if (!IsValidDateFormat(lastUpdated))
            throw new ArgumentException("LastUpdated must be in YYYY-MM-DD format", nameof(lastUpdated));
        if (!IsValidDateFormat(lastChecked))
            throw new ArgumentException("LastChecked must be in YYYY-MM-DD format", nameof(lastChecked));

        // Validate content
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        Id = id;
        Topic = topic;
        Category = category;
        Summary = summary;
        KnowledgeGapLikelihood = knowledgeGapLikelihood!.ToLowerInvariant();
        LastUpdated = lastUpdated;
        LastChecked = lastChecked;
        Content = content;
        Keywords = (keywords ?? new List<string>()).AsReadOnly();
    }

    private static bool IsValidDateFormat(string date)
    {
        if (string.IsNullOrWhiteSpace(date)) return false;
        return DateTime.TryParseExact(date, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _);
    }
}
