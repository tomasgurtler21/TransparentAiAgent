namespace TransparentAiAgentCore_Tests.Domain.Knowledge;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Knowledge;

[TestClass]
public class KnowledgeEntryTests
{
    private KnowledgeContent CreateValidContent()
    {
        return new KnowledgeContent("Valid overview");
    }

    [TestMethod]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry(null!, "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_InvalidIdFormat_UpperCase_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("Invalid-ID", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_InvalidIdFormat_Underscore_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("invalid_id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_InvalidIdFormat_Spaces_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("invalid id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_ValidKebabCaseId_CreatesInstance()
    {
        // Arrange
        var id = "valid-kebab-case-id";
        var content = CreateValidContent();

        // Act
        var entry = new KnowledgeEntry(id, "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content);

        // Assert
        Assert.AreEqual(id, entry.Id);
    }

    [TestMethod]
    public void Constructor_NullTopic_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("valid-id", null!, "category", "summary", "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_NullCategory_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("valid-id", "topic", null!, "summary", "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_NullSummary_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("valid-id", "topic", "category", null!, "low", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_InvalidGapLikelihood_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("valid-id", "topic", "category", "summary", "invalid", "2025-11-14", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_ValidGapLikelihood_Low_CreatesInstance()
    {
        // Arrange
        var content = CreateValidContent();

        // Act
        var entry = new KnowledgeEntry("valid-id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content);

        // Assert
        Assert.AreEqual("low", entry.KnowledgeGapLikelihood);
    }

    [TestMethod]
    public void Constructor_ValidGapLikelihood_Medium_CreatesInstance()
    {
        // Arrange
        var content = CreateValidContent();

        // Act
        var entry = new KnowledgeEntry("valid-id", "topic", "category", "summary", "medium", "2025-11-14", "2025-11-14", content);

        // Assert
        Assert.AreEqual("medium", entry.KnowledgeGapLikelihood);
    }

    [TestMethod]
    public void Constructor_ValidGapLikelihood_High_CreatesInstance()
    {
        // Arrange
        var content = CreateValidContent();

        // Act
        var entry = new KnowledgeEntry("valid-id", "topic", "category", "summary", "high", "2025-11-14", "2025-11-14", content);

        // Assert
        Assert.AreEqual("high", entry.KnowledgeGapLikelihood);
    }

    [TestMethod]
    public void Constructor_GapLikelihood_CaseInsensitive_NormalizesToLowercase()
    {
        // Arrange
        var content = CreateValidContent();

        // Act
        var entry = new KnowledgeEntry("valid-id", "topic", "category", "summary", "HIGH", "2025-11-14", "2025-11-14", content);

        // Assert
        Assert.AreEqual("high", entry.KnowledgeGapLikelihood);
    }

    [TestMethod]
    public void Constructor_InvalidDateFormat_LastUpdated_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("valid-id", "topic", "category", "summary", "low", "11/14/2025", "2025-11-14", content));
    }

    [TestMethod]
    public void Constructor_InvalidDateFormat_LastChecked_ThrowsArgumentException()
    {
        // Arrange
        var content = CreateValidContent();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntry("valid-id", "topic", "category", "summary", "low", "2025-11-14", "14-11-2025", content));
    }

    [TestMethod]
    public void Constructor_ValidDateFormat_CreatesInstance()
    {
        // Arrange
        var content = CreateValidContent();
        var lastUpdated = "2025-11-14";
        var lastChecked = "2025-11-14";

        // Act
        var entry = new KnowledgeEntry("valid-id", "topic", "category", "summary", "low", lastUpdated, lastChecked, content);

        // Assert
        Assert.AreEqual(lastUpdated, entry.LastUpdated);
        Assert.AreEqual(lastChecked, entry.LastChecked);
    }

    [TestMethod]
    public void Constructor_NullContent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new KnowledgeEntry("valid-id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", null!));
    }

    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var id = "valid-id";
        var topic = "Test Topic";
        var category = "Test Category";
        var summary = "Test Summary";
        var gapLikelihood = "low";
        var lastUpdated = "2025-11-14";
        var lastChecked = "2025-11-14";
        var content = CreateValidContent();

        // Act
        var entry = new KnowledgeEntry(id, topic, category, summary, gapLikelihood, lastUpdated, lastChecked, content);

        // Assert
        Assert.AreEqual(id, entry.Id);
        Assert.AreEqual(topic, entry.Topic);
        Assert.AreEqual(category, entry.Category);
        Assert.AreEqual(summary, entry.Summary);
        Assert.AreEqual(gapLikelihood, entry.KnowledgeGapLikelihood);
        Assert.AreEqual(lastUpdated, entry.LastUpdated);
        Assert.AreEqual(lastChecked, entry.LastChecked);
        Assert.AreEqual(content, entry.Content);
    }

    [TestMethod]
    public void Constructor_NullKeywords_CreatesEmptyList()
    {
        // Arrange
        var content = CreateValidContent();

        // Act
        var entry = new KnowledgeEntry("valid-id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content, keywords: null);

        // Assert
        Assert.IsNotNull(entry.Keywords);
        Assert.AreEqual(0, entry.Keywords.Count);
    }

    [TestMethod]
    public void Constructor_WithKeywords_StoresKeywords()
    {
        // Arrange
        var content = CreateValidContent();
        var keywords = new List<string> { "keyword1", "keyword2" };

        // Act
        var entry = new KnowledgeEntry("valid-id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", content, keywords);

        // Assert
        Assert.AreEqual(2, entry.Keywords.Count);
        Assert.AreEqual("keyword1", entry.Keywords[0]);
    }
}
