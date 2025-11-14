namespace TransparentAiAgentCore_Tests.Domain.Knowledge;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Knowledge;

[TestClass]
public class KnowledgeContentTests
{
    [TestMethod]
    public void Constructor_NullOverview_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeContent(null!));
    }

    [TestMethod]
    public void Constructor_EmptyOverview_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeContent(""));
    }

    [TestMethod]
    public void Constructor_WhitespaceOverview_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeContent("   "));
    }

    [TestMethod]
    public void Constructor_ValidOverview_CreatesInstance()
    {
        // Arrange
        var overview = "This is an overview";

        // Act
        var content = new KnowledgeContent(overview);

        // Assert
        Assert.IsNotNull(content);
        Assert.AreEqual(overview, content.Overview);
    }

    [TestMethod]
    public void Constructor_NullKeyPoints_CreatesEmptyList()
    {
        // Arrange
        var overview = "Overview";

        // Act
        var content = new KnowledgeContent(overview, keyPoints: null);

        // Assert
        Assert.IsNotNull(content.KeyPoints);
        Assert.AreEqual(0, content.KeyPoints.Count);
    }

    [TestMethod]
    public void Constructor_NullExamples_CreatesEmptyList()
    {
        // Arrange
        var overview = "Overview";

        // Act
        var content = new KnowledgeContent(overview, examples: null);

        // Assert
        Assert.IsNotNull(content.Examples);
        Assert.AreEqual(0, content.Examples.Count);
    }

    [TestMethod]
    public void Constructor_NullWarnings_CreatesEmptyList()
    {
        // Arrange
        var overview = "Overview";

        // Act
        var content = new KnowledgeContent(overview, warnings: null);

        // Assert
        Assert.IsNotNull(content.Warnings);
        Assert.AreEqual(0, content.Warnings.Count);
    }

    [TestMethod]
    public void Constructor_NullBestPractices_CreatesEmptyList()
    {
        // Arrange
        var overview = "Overview";

        // Act
        var content = new KnowledgeContent(overview, bestPractices: null);

        // Assert
        Assert.IsNotNull(content.BestPractices);
        Assert.AreEqual(0, content.BestPractices.Count);
    }

    [TestMethod]
    public void Constructor_NullRelatedTopics_CreatesEmptyList()
    {
        // Arrange
        var overview = "Overview";

        // Act
        var content = new KnowledgeContent(overview, relatedTopics: null);

        // Assert
        Assert.IsNotNull(content.RelatedTopics);
        Assert.AreEqual(0, content.RelatedTopics.Count);
    }

    [TestMethod]
    public void Constructor_NullReferences_CreatesEmptyList()
    {
        // Arrange
        var overview = "Overview";

        // Act
        var content = new KnowledgeContent(overview, references: null);

        // Assert
        Assert.IsNotNull(content.References);
        Assert.AreEqual(0, content.References.Count);
    }

    [TestMethod]
    public void Constructor_WithAllFields_StoresAllData()
    {
        // Arrange
        var overview = "Overview";
        var keyPoints = new List<string> { "Point 1", "Point 2" };
        var examples = new List<KnowledgeExample>
        {
            new KnowledgeExample("Example 1", "Explanation 1")
        };
        var warnings = new List<string> { "Warning 1" };
        var bestPractices = new List<string> { "Practice 1" };
        var relatedTopics = new List<string> { "topic-1" };
        var references = new List<KnowledgeReference>
        {
            new KnowledgeReference("Ref 1")
        };

        // Act
        var content = new KnowledgeContent(
            overview,
            keyPoints,
            examples,
            warnings,
            bestPractices,
            relatedTopics,
            references);

        // Assert
        Assert.AreEqual(2, content.KeyPoints.Count);
        Assert.AreEqual(1, content.Examples.Count);
        Assert.AreEqual(1, content.Warnings.Count);
        Assert.AreEqual(1, content.BestPractices.Count);
        Assert.AreEqual(1, content.RelatedTopics.Count);
        Assert.AreEqual(1, content.References.Count);
    }

    [TestMethod]
    public void Constructor_ReturnsReadOnlyCollections()
    {
        // Arrange
        var overview = "Overview";
        var keyPoints = new List<string> { "Point 1" };

        // Act
        var content = new KnowledgeContent(overview, keyPoints);

        // Assert
        Assert.IsInstanceOfType(content.KeyPoints, typeof(IReadOnlyList<string>));
    }
}

[TestClass]
public class KnowledgeReferenceTests
{
    [TestMethod]
    public void Constructor_NullTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeReference(null!));
    }

    [TestMethod]
    public void Constructor_EmptyTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeReference(""));
    }

    [TestMethod]
    public void Constructor_WhitespaceTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeReference("   "));
    }

    [TestMethod]
    public void Constructor_ValidTitle_CreatesInstance()
    {
        // Arrange
        var title = "Reference Title";

        // Act
        var reference = new KnowledgeReference(title);

        // Assert
        Assert.IsNotNull(reference);
        Assert.AreEqual(title, reference.Title);
    }

    [TestMethod]
    public void Constructor_NullUrl_SetsNull()
    {
        // Arrange
        var title = "Reference Title";

        // Act
        var reference = new KnowledgeReference(title, url: null);

        // Assert
        Assert.IsNull(reference.Url);
    }

    [TestMethod]
    public void Constructor_ValidUrl_StoresUrl()
    {
        // Arrange
        var title = "Reference Title";
        var url = "https://example.com";

        // Act
        var reference = new KnowledgeReference(title, url);

        // Assert
        Assert.AreEqual(url, reference.Url);
    }
}
