using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInKnowledge;

[TestClass]
public class KnowledgeLibraryToolExecutorTests
{
    private KnowledgeLibraryToolExecutor _executor = null!;
    private MockKnowledgeLibrary _mockLibrary = null!;
    private KnowledgeLibraryTool _tool = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLibrary = new MockKnowledgeLibrary();
        _executor = new KnowledgeLibraryToolExecutor(_mockLibrary, NullLogger<KnowledgeLibraryToolExecutor>.Instance);
        _tool = new KnowledgeLibraryTool(new List<KnowledgeEntrySummary>());
    }

    [TestMethod]
    public void Constructor_NullLibrary_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new KnowledgeLibraryToolExecutor(null!, NullLogger<KnowledgeLibraryToolExecutor>.Instance));
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new KnowledgeLibraryToolExecutor(_mockLibrary, null!));
    }

    [TestMethod]
    public void SourceType_ReturnsBuiltInKnowledge()
    {
        // Arrange & Act
        var sourceType = _executor.SourceType;

        // Assert
        Assert.AreEqual(ToolSourceType.BuiltInKnowledge, sourceType);
    }

    [TestMethod]
    public async Task ExecuteAsync_MissingTopicParameter_ReturnsFailure()
    {
        // Arrange
        var emptyArgs = "{}";

        // Act
        var result = await _executor.ExecuteAsync(_tool, emptyArgs);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("topic", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ExecuteAsync_EmptyTopicParameter_ReturnsFailure()
    {
        // Arrange
        var args = """{"topic": ""}""";

        // Act
        var result = await _executor.ExecuteAsync(_tool, args);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsync_NonExistentTopic_ReturnsFailure()
    {
        // Arrange
        var args = """{"topic": "non-existent"}""";
        _mockLibrary.SetReturnNull(true);

        // Act
        var result = await _executor.ExecuteAsync(_tool, args);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ExecuteAsync_ValidTopic_ReturnsFormattedMarkdown()
    {
        // Arrange
        var args = """{"topic": "test-topic"}""";
        var entry = CreateSampleEntry();
        _mockLibrary.AddEntry(entry);

        // Act
        var result = await _executor.ExecuteAsync(_tool, args);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Content.Contains("# Test Topic"));
        Assert.IsTrue(result.Content.Contains("## Overview"));
        Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero);
    }

    [TestMethod]
    public async Task ExecuteAsync_EntryWithExamples_IncludesExamplesInOutput()
    {
        // Arrange
        var args = """{"topic": "test-topic"}""";
        var entry = CreateSampleEntry();
        _mockLibrary.AddEntry(entry);

        // Act
        var result = await _executor.ExecuteAsync(_tool, args);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Content.Contains("## Examples"));
        Assert.IsTrue(result.Content.Contains("Example Title"));
    }

    [TestMethod]
    public async Task ExecuteAsync_EntryWithWarnings_IncludesWarningsInOutput()
    {
        // Arrange
        var args = """{"topic": "test-topic"}""";
        var entry = CreateSampleEntry();
        _mockLibrary.AddEntry(entry);

        // Act
        var result = await _executor.ExecuteAsync(_tool, args);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Content.Contains("⚠️ Warnings"));
        Assert.IsTrue(result.Content.Contains("Warning 1"));
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act
        var result = await _executor.ExecuteAsync(_tool, invalidJson);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsync_WrongToolName_ReturnsFailure()
    {
        // Arrange
        var wrongTool = new MockTool("wrong_tool");
        var args = """{"topic": "test"}""";

        // Act
        var result = await _executor.ExecuteAsync(wrongTool, args);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage!.Contains("Unknown tool", StringComparison.OrdinalIgnoreCase));
    }

    private static KnowledgeEntry CreateSampleEntry()
    {
        var example = new KnowledgeExample(
            "Example Title",
            "This is an explanation",
            "code sample");

        var reference = new KnowledgeReference("Ref Title", "https://example.com");

        var content = new KnowledgeContent(
            "This is the overview",
            keyPoints: new List<string> { "Key point 1", "Key point 2" },
            examples: new List<KnowledgeExample> { example },
            warnings: new List<string> { "Warning 1", "Warning 2" },
            bestPractices: new List<string> { "Best practice 1" },
            relatedTopics: new List<string> { "related-topic" },
            references: new List<KnowledgeReference> { reference }
        );

        return new KnowledgeEntry(
            "test-topic",
            "Test Topic",
            "Testing",
            "A test entry",
            "low",
            "2025-11-14",
            "2025-11-14",
            content,
            new List<string> { "test", "sample" }
        );
    }

    // Mock classes for testing
    private class MockKnowledgeLibrary : IKnowledgeLibrary
    {
        private readonly Dictionary<string, KnowledgeEntry> _entries = new();
        private bool _returnNull = false;

        public void AddEntry(KnowledgeEntry entry)
        {
            _entries[entry.Id] = entry;
        }

        public void SetReturnNull(bool returnNull)
        {
            _returnNull = returnNull;
        }

        public KnowledgeEntry? GetTopic(string topicId)
        {
            if (_returnNull) return null;
            _entries.TryGetValue(topicId, out var entry);
            return entry;
        }

        public IReadOnlyList<KnowledgeEntrySummary> GetAllTopics()
        {
            return _entries.Values
                .Select(KnowledgeEntrySummary.FromEntry)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category)
        {
            return _entries.Values
                .Where(e => e.Category == category)
                .Select(KnowledgeEntrySummary.FromEntry)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<string> GetCategories()
        {
            return _entries.Values
                .Select(e => e.Category)
                .Distinct()
                .ToList()
                .AsReadOnly();
        }
    }

    private class MockTool : ITool
    {
        public MockTool(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description => "Mock tool";
        public string ParametersSchema => "{}";
        public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;
        public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>();
    }
}
