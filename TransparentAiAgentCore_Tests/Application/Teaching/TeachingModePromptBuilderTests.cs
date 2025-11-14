namespace TransparentAiAgentCore_Tests.Application.Teaching;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Teaching;
using TransparentAiAgentCore.Domain.Knowledge;

[TestClass]
public class TeachingModePromptBuilderTests
{
    [TestMethod]
    public void Constructor_NullLibrary_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new TeachingModePromptBuilder(null!));
    }

    [TestMethod]
    public void BuildKnowledgeLibrarySection_EmptyLibrary_ReturnsHeaderOnly()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary(); // Empty library
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildKnowledgeLibrarySection();

        // Assert
        Assert.IsTrue(result.Contains("# Knowledge Library"));
        Assert.IsTrue(result.Contains("How to use:"));
        Assert.IsFalse(result.Contains("- `")); // No topic entries
    }

    [TestMethod]
    public void BuildKnowledgeLibrarySection_WithTopics_ListsAllTopics()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        library.AddTopic(CreateSummary("api-key-security", "API Key Security", "Security", "Critical security guardrails", "low"));
        library.AddTopic(CreateSummary("context-windows", "Context Windows", "LLM Concepts", "Context window limits", "low"));
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildKnowledgeLibrarySection();

        // Assert
        Assert.IsTrue(result.Contains("- `api-key-security`:"));
        Assert.IsTrue(result.Contains("- `context-windows`:"));
        Assert.IsTrue(result.Contains("API Key Security"));
        Assert.IsTrue(result.Contains("Context Windows"));
    }

    [TestMethod]
    public void BuildKnowledgeLibrarySection_IncludesGapLikelihoodGuidance()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildKnowledgeLibrarySection();

        // Assert
        Assert.IsTrue(result.Contains("Understanding Knowledge Gap Likelihood"));
        Assert.IsTrue(result.Contains("Low"));
        Assert.IsTrue(result.Contains("Medium"));
        Assert.IsTrue(result.Contains("High"));
    }

    [TestMethod]
    public void BuildKnowledgeLibrarySection_IncludesUsageInstructions()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildKnowledgeLibrarySection();

        // Assert
        Assert.IsTrue(result.Contains("How to use:"));
        Assert.IsTrue(result.Contains("Query the library"));
        Assert.IsTrue(result.Contains("When to query the library"));
        Assert.IsTrue(result.Contains("After querying"));
    }

    [TestMethod]
    public void BuildKnowledgeLibrarySection_IncludesGapLikelihoodInTopicList()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        library.AddTopic(CreateSummary("test-topic", "Test Topic", "Testing", "Test summary", "medium"));
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildKnowledgeLibrarySection();

        // Assert
        Assert.IsTrue(result.Contains("gap likelihood: medium"));
    }

    [TestMethod]
    public void BuildKnowledgeLibrarySection_FormatsCorrectlyForLLM()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        library.AddTopic(CreateSummary("test-topic", "Test Topic", "Testing", "Test summary", "low"));
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildKnowledgeLibrarySection();

        // Assert - Verify markdown structure
        Assert.IsTrue(result.Contains("# Knowledge Library"));
        Assert.IsTrue(result.Contains("**How to use:**"));
        Assert.IsTrue(result.Contains("**Available knowledge topics:**"));
        Assert.IsTrue(result.Contains("**When to query the library:**"));
        Assert.IsTrue(result.Contains("**After querying:**"));
        Assert.IsTrue(result.Contains("**Understanding Knowledge Gap Likelihood:**"));
    }

    // Helper method to create test summaries
    private KnowledgeEntrySummary CreateSummary(
        string id,
        string topic,
        string category,
        string summary,
        string gapLikelihood)
    {
        return new KnowledgeEntrySummary(
            id,
            topic,
            category,
            summary,
            gapLikelihood,
            "2025-11-14",
            "2025-11-14",
            new List<string>());
    }

    /// <summary>
    /// Simple fake implementation for testing.
    /// </summary>
    private class FakeKnowledgeLibrary : IKnowledgeLibrary
    {
        private readonly List<KnowledgeEntrySummary> _topics = new();

        public void AddTopic(KnowledgeEntrySummary summary)
        {
            _topics.Add(summary);
        }

        public KnowledgeEntry? GetTopic(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                throw new ArgumentException("Topic ID cannot be null or whitespace", nameof(topicId));

            return null;
        }

        public IReadOnlyList<KnowledgeEntrySummary> GetAllTopics()
        {
            return _topics.AsReadOnly();
        }

        public IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be null or whitespace", nameof(category));

            return _topics
                .Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<string> GetCategories()
        {
            return _topics
                .Select(t => t.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList()
                .AsReadOnly();
        }
    }
}
