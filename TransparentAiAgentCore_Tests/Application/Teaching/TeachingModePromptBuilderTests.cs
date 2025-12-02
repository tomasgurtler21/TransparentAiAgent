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
    public void BuildCompletePrompt_EmptyLibrary_ContainsKnowledgeSourcesSection()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary(); // Empty library
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildCompletePrompt();

        // Assert
        Assert.IsTrue(result.Contains("# KNOWLEDGE SOURCES"));
        Assert.IsTrue(result.Contains("Inner Knowledge"));
        Assert.IsTrue(result.Contains("Knowledge Library Tool"));
        Assert.IsTrue(result.Contains("Web Search"));
    }

    [TestMethod]
    public void BuildCompletePrompt_WithTopics_DoesNotListTopics()
    {
        // Arrange - Topics are now in tool description, not system prompt
        var library = new FakeKnowledgeLibrary();
        library.AddTopic(CreateSummary("api-key-security", "API Key Security", "Security", "Critical security guardrails", "low"));
        library.AddTopic(CreateSummary("context-windows", "Context Windows", "LLM Concepts", "Context window limits", "low"));
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildCompletePrompt();

        // Assert - Topics should NOT be in system prompt (they're in tool description now)
        Assert.IsTrue(result.Contains("# KNOWLEDGE SOURCES"));
        Assert.IsTrue(result.Contains("Knowledge Library Tool"));
        // Topics themselves should not be listed in the prompt
        Assert.IsFalse(result.Contains("api-key-security"));
        Assert.IsFalse(result.Contains("context-windows"));
    }

    [TestMethod]
    public void BuildCompletePrompt_ReferencesKnowledgeGapLikelihood()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildCompletePrompt();

        // Assert - Prompt should mention knowledgeGapLikelihood concept
        Assert.IsTrue(result.Contains("knowledgeGapLikelihood"));
        Assert.IsTrue(result.Contains("Knowledge Library Tool"));
    }

    [TestMethod]
    public void BuildCompletePrompt_IncludesPrioritizationStrategy()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildCompletePrompt();

        // Assert
        Assert.IsTrue(result.Contains("Prioritization Strategy"));
        Assert.IsTrue(result.Contains("Combine sources intelligently"));
        Assert.IsTrue(result.Contains("web search"));
    }

    [TestMethod]
    public void BuildCompletePrompt_DoesNotIncludeSpecificTopicsInPrompt()
    {
        // Arrange - Topics moved to tool description, not system prompt
        var library = new FakeKnowledgeLibrary();
        library.AddTopic(CreateSummary("test-topic", "Test Topic", "Testing", "Test summary", "medium"));
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildCompletePrompt();

        // Assert - Specific topics should not be in system prompt
        Assert.IsFalse(result.Contains("test-topic"));
        Assert.IsFalse(result.Contains("Testing:"));
        // But should reference the knowledge library system
        Assert.IsTrue(result.Contains("Knowledge Library Tool"));
    }

    [TestMethod]
    public void BuildCompletePrompt_IncludesAllThreeKnowledgeSources()
    {
        // Arrange
        var library = new FakeKnowledgeLibrary();
        library.AddTopic(CreateSummary("test-topic", "Test Topic", "Testing", "Test summary", "low"));
        var builder = new TeachingModePromptBuilder(library);

        // Act
        var result = builder.BuildCompletePrompt();

        // Assert - Verify all three sources mentioned
        Assert.IsTrue(result.Contains("Inner Knowledge"));
        Assert.IsTrue(result.Contains("Knowledge Library Tool"));
        Assert.IsTrue(result.Contains("Web Search"));
        Assert.IsTrue(result.Contains("Optional - May Not Be Available"));
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
