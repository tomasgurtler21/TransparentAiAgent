namespace TransparentAiAgentCore_Tests.Domain.Knowledge;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Knowledge;

[TestClass]
public class KnowledgeEntrySummaryTests
{
    [TestMethod]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntrySummary(null!, "topic", "category", "summary", "low", "2025-11-14", "2025-11-14"));
    }

    [TestMethod]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntrySummary("", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14"));
    }

    [TestMethod]
    public void Constructor_NullTopic_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntrySummary("id", null!, "category", "summary", "low", "2025-11-14", "2025-11-14"));
    }

    [TestMethod]
    public void Constructor_EmptyTopic_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new KnowledgeEntrySummary("id", "", "category", "summary", "low", "2025-11-14", "2025-11-14"));
    }

    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var id = "test-id";
        var topic = "Test Topic";
        var category = "Test Category";
        var summary = "Test Summary";
        var gapLikelihood = "low";
        var lastUpdated = "2025-11-14";
        var lastChecked = "2025-11-14";

        // Act
        var entrySummary = new KnowledgeEntrySummary(id, topic, category, summary, gapLikelihood, lastUpdated, lastChecked);

        // Assert
        Assert.IsNotNull(entrySummary);
        Assert.AreEqual(id, entrySummary.Id);
        Assert.AreEqual(topic, entrySummary.Topic);
        Assert.AreEqual(category, entrySummary.Category);
        Assert.AreEqual(summary, entrySummary.Summary);
        Assert.AreEqual(gapLikelihood, entrySummary.KnowledgeGapLikelihood);
        Assert.AreEqual(lastUpdated, entrySummary.LastUpdated);
        Assert.AreEqual(lastChecked, entrySummary.LastChecked);
    }

    [TestMethod]
    public void Constructor_NullKeywords_CreatesEmptyList()
    {
        // Act
        var entrySummary = new KnowledgeEntrySummary("id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", keywords: null);

        // Assert
        Assert.IsNotNull(entrySummary.Keywords);
        Assert.AreEqual(0, entrySummary.Keywords.Count);
    }

    [TestMethod]
    public void Constructor_WithKeywords_StoresKeywords()
    {
        // Arrange
        var keywords = new List<string> { "keyword1", "keyword2" };

        // Act
        var entrySummary = new KnowledgeEntrySummary("id", "topic", "category", "summary", "low", "2025-11-14", "2025-11-14", keywords);

        // Assert
        Assert.AreEqual(2, entrySummary.Keywords.Count);
        Assert.AreEqual("keyword1", entrySummary.Keywords[0]);
        Assert.AreEqual("keyword2", entrySummary.Keywords[1]);
    }

    [TestMethod]
    public void FromEntry_NullEntry_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => KnowledgeEntrySummary.FromEntry(null!));
    }

    [TestMethod]
    public void FromEntry_ValidEntry_ExtractsCorrectFields()
    {
        // Arrange
        var content = new KnowledgeContent("Overview");
        var keywords = new List<string> { "keyword1", "keyword2" };
        var entry = new KnowledgeEntry(
            "test-id",
            "Test Topic",
            "Test Category",
            "Test Summary",
            "medium",
            "2025-11-14",
            "2025-11-14",
            content,
            keywords);

        // Act
        var summary = KnowledgeEntrySummary.FromEntry(entry);

        // Assert
        Assert.AreEqual(entry.Id, summary.Id);
        Assert.AreEqual(entry.Topic, summary.Topic);
        Assert.AreEqual(entry.Category, summary.Category);
        Assert.AreEqual(entry.Summary, summary.Summary);
        Assert.AreEqual(entry.KnowledgeGapLikelihood, summary.KnowledgeGapLikelihood);
        Assert.AreEqual(entry.LastUpdated, summary.LastUpdated);
        Assert.AreEqual(entry.LastChecked, summary.LastChecked);
        Assert.AreEqual(2, summary.Keywords.Count);
    }

    [TestMethod]
    public void FromEntry_ValidEntry_DoesNotIncludeContent()
    {
        // Arrange
        var content = new KnowledgeContent("Overview with lots of detail");
        var entry = new KnowledgeEntry(
            "test-id",
            "Test Topic",
            "Test Category",
            "Test Summary",
            "high",
            "2025-11-14",
            "2025-11-14",
            content);

        // Act
        var summary = KnowledgeEntrySummary.FromEntry(entry);

        // Assert - Summary type doesn't have Content property (design verification)
        Assert.IsNotNull(summary);
        Assert.AreEqual("Test Summary", summary.Summary);
        // Content is not part of the summary
    }
}
