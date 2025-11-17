using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInKnowledge;

[TestClass]
public class BuiltInKnowledgeToolRegistryTests
{
    private BuiltInKnowledgeToolRegistry _registry = null!;
    private TestKnowledgeLibrary _knowledgeLibrary = null!;

    [TestInitialize]
    public void Setup()
    {
        _knowledgeLibrary = new TestKnowledgeLibrary();
        _registry = new BuiltInKnowledgeToolRegistry(_knowledgeLibrary);
    }

    private class TestKnowledgeLibrary : IKnowledgeLibrary
    {
        private readonly List<KnowledgeEntrySummary> _topics = new()
        {
            new KnowledgeEntrySummary(
                "test-topic-1",
                "Test Topic 1",
                "TestCategory",
                "Test summary",
                "HIGH",
                "2024-01-01",
                "2024-01-01",
                new List<string> { "test" })
        };

        public KnowledgeEntry? GetTopic(string topicId) => null;
        public IReadOnlyList<KnowledgeEntrySummary> GetAllTopics() => _topics.AsReadOnly();
        public IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category) => _topics.AsReadOnly();
        public IReadOnlyList<string> GetCategories() => new List<string> { "TestCategory" }.AsReadOnly();
    }

    [TestMethod]
    public void Constructor_ThrowsArgumentNullException_WhenKnowledgeLibraryIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new BuiltInKnowledgeToolRegistry(null!));
    }

    [TestMethod]
    public void GetAllTools_ReturnsKnowledgeLibraryTool()
    {
        // Act
        var tools = _registry.GetAllTools();

        // Assert
        Assert.IsNotNull(tools);
        Assert.AreEqual(1, tools.Count);
        Assert.AreEqual("knowledge_library_query", tools[0].Name);
        Assert.AreEqual(ToolSourceType.BuiltInKnowledge, tools[0].SourceType);
    }

    [TestMethod]
    public void GetTool_KnowledgeLibraryQuery_ReturnsTool()
    {
        // Act
        var tool = _registry.GetTool("knowledge_library_query");

        // Assert
        Assert.IsNotNull(tool);
        Assert.AreEqual("knowledge_library_query", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInKnowledge, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_CaseInsensitive_ReturnsTool()
    {
        // Act
        var tool = _registry.GetTool("KNOWLEDGE_LIBRARY_QUERY");

        // Assert
        Assert.IsNotNull(tool);
        Assert.AreEqual("knowledge_library_query", tool.Name);
    }

    [TestMethod]
    public void GetTool_UnknownTool_ReturnsNull()
    {
        // Act
        var tool = _registry.GetTool("unknown_tool");

        // Assert
        Assert.IsNull(tool);
    }

    [TestMethod]
    public void GetTool_NullToolName_ReturnsNull()
    {
        // Act
        var tool = _registry.GetTool(null!);

        // Assert
        Assert.IsNull(tool);
    }

    [TestMethod]
    public void GetTool_EmptyToolName_ReturnsNull()
    {
        // Act
        var tool = _registry.GetTool("");

        // Assert
        Assert.IsNull(tool);
    }

    [TestMethod]
    public void HasTool_KnowledgeLibraryQuery_ReturnsTrue()
    {
        // Act
        var hasTool = _registry.HasTool("knowledge_library_query");

        // Assert
        Assert.IsTrue(hasTool);
    }

    [TestMethod]
    public void HasTool_UnknownTool_ReturnsFalse()
    {
        // Act
        var hasTool = _registry.HasTool("unknown_tool");

        // Assert
        Assert.IsFalse(hasTool);
    }

    [TestMethod]
    public async Task RefreshAsync_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await _registry.RefreshAsync();
    }
}
