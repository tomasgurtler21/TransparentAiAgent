namespace TransparentAiAgentCore.Domain.Knowledge;

/// <summary>
/// Domain service for accessing the knowledge library.
/// Provides methods to query knowledge entries and metadata.
/// </summary>
public interface IKnowledgeLibrary
{
    /// <summary>
    /// Gets a knowledge entry by ID.
    /// </summary>
    /// <param name="topicId">The unique identifier of the topic.</param>
    /// <returns>The knowledge entry, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when topicId is null or whitespace.</exception>
    KnowledgeEntry? GetTopic(string topicId);

    /// <summary>
    /// Gets all available knowledge entry summaries (for index/listing).
    /// </summary>
    /// <returns>List of entry metadata (ID, topic, category, summary).</returns>
    IReadOnlyList<KnowledgeEntrySummary> GetAllTopics();

    /// <summary>
    /// Gets all topics in a specific category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>List of entry summaries in that category.</returns>
    /// <exception cref="ArgumentException">Thrown when category is null or whitespace.</exception>
    IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category);

    /// <summary>
    /// Gets all unique categories in the library.
    /// </summary>
    /// <returns>List of category names.</returns>
    IReadOnlyList<string> GetCategories();
}
