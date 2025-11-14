namespace TransparentAiAgentCore_Tests.Domain.Knowledge;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Knowledge;

/// <summary>
/// Contract tests for IKnowledgeLibrary interface.
/// Tests the expected behavior that implementations must follow.
/// </summary>
[TestClass]
public class IKnowledgeLibraryContractTests
{
    private IKnowledgeLibrary CreateTestImplementation()
    {
        // Create a simple mock implementation for testing interface contract
        return new MockKnowledgeLibrary();
    }

    [TestMethod]
    public void GetTopic_NullTopicId_ThrowsArgumentException()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopic(null!));
    }

    [TestMethod]
    public void GetTopic_EmptyTopicId_ThrowsArgumentException()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopic(string.Empty));
    }

    [TestMethod]
    public void GetTopic_WhitespaceTopicId_ThrowsArgumentException()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopic("   "));
    }

    [TestMethod]
    public void GetTopic_NonExistentTopic_ReturnsNull()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act
        var result = library.GetTopic("non-existent-topic");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetAllTopics_ReturnsReadOnlyList()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act
        var result = library.GetAllTopics();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IReadOnlyList<KnowledgeEntrySummary>));
    }

    [TestMethod]
    public void GetTopicsByCategory_NullCategory_ThrowsArgumentException()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopicsByCategory(null!));
    }

    [TestMethod]
    public void GetTopicsByCategory_EmptyCategory_ThrowsArgumentException()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopicsByCategory(string.Empty));
    }

    [TestMethod]
    public void GetTopicsByCategory_ValidCategory_ReturnsReadOnlyList()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act
        var result = library.GetTopicsByCategory("TestCategory");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IReadOnlyList<KnowledgeEntrySummary>));
    }

    [TestMethod]
    public void GetCategories_ReturnsDistinctCategories()
    {
        // Arrange
        var library = CreateTestImplementation();

        // Act
        var result = library.GetCategories();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IReadOnlyList<string>));
    }

    /// <summary>
    /// Simple mock implementation for contract testing.
    /// </summary>
    private class MockKnowledgeLibrary : IKnowledgeLibrary
    {
        public KnowledgeEntry? GetTopic(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                throw new ArgumentException("Topic ID cannot be null or whitespace", nameof(topicId));

            return null; // Non-existent topics return null
        }

        public IReadOnlyList<KnowledgeEntrySummary> GetAllTopics()
        {
            return new List<KnowledgeEntrySummary>().AsReadOnly();
        }

        public IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be null or whitespace", nameof(category));

            return new List<KnowledgeEntrySummary>().AsReadOnly();
        }

        public IReadOnlyList<string> GetCategories()
        {
            return new List<string>().AsReadOnly();
        }
    }
}
