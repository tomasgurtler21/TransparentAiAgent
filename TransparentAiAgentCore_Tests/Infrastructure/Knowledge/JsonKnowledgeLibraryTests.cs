namespace TransparentAiAgentCore_Tests.Infrastructure.Knowledge;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Infrastructure.Knowledge;

[TestClass]
public class JsonKnowledgeLibraryTests
{
    private string _testDataPath = null!;
    private ILogger<JsonKnowledgeLibrary> _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create unique temp directory for each test
        _testDataPath = Path.Combine(Path.GetTempPath(), $"KnowledgeLibraryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);

        // Create null logger for tests
        _logger = NullLogger<JsonKnowledgeLibrary>.Instance;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test directory
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
    }

    // Task 3.1: Directory Scanning and Index Loading Tests

    [TestMethod]
    public void Constructor_EmptyDirectory_LoadsEmptyIndex()
    {
        // Arrange: Empty directory (no entries folder)

        // Act
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);
        var topics = library.GetAllTopics();

        // Assert
        Assert.IsNotNull(topics);
        Assert.AreEqual(0, topics.Count);
    }

    [TestMethod]
    public void Constructor_ValidEntries_LoadsAllEntries()
    {
        // Arrange: Create test entries
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entry1Json = """
        {
          "id": "test-topic-1",
          "topic": "Test Topic 1",
          "category": "Testing",
          "summary": "First test summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "keywords": ["test"],
          "content": {
            "overview": "Overview 1"
          }
        }
        """;

        var entry2Json = """
        {
          "id": "test-topic-2",
          "topic": "Another Topic",
          "category": "Testing",
          "summary": "Second test summary",
          "knowledgeGapLikelihood": "medium",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "Overview 2"
          }
        }
        """;

        File.WriteAllText(Path.Combine(entriesPath, "test-topic-1.json"), entry1Json);
        File.WriteAllText(Path.Combine(entriesPath, "test-topic-2.json"), entry2Json);

        // Act
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);
        var topics = library.GetAllTopics();

        // Assert
        Assert.AreEqual(2, topics.Count);
    }

    [TestMethod]
    public void Constructor_CorruptedEntry_SkipsEntryAndContinues()
    {
        // Arrange: Mix of valid and corrupted entries
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var validJson = """
        {
          "id": "valid-topic",
          "topic": "Valid Topic",
          "category": "Testing",
          "summary": "Valid summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "Valid overview"
          }
        }
        """;

        var corruptedJson = """
        {
          "id": "corrupted-topic",
          "topic": "Corrupted"
          // Invalid JSON - missing closing braces
        """;

        File.WriteAllText(Path.Combine(entriesPath, "valid-topic.json"), validJson);
        File.WriteAllText(Path.Combine(entriesPath, "corrupted-topic.json"), corruptedJson);

        // Act
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);
        var topics = library.GetAllTopics();

        // Assert
        Assert.AreEqual(1, topics.Count, "Should load only valid entry");
        Assert.AreEqual("valid-topic", topics[0].Id);
    }

    [TestMethod]
    public void Constructor_MixedValidInvalid_LoadsOnlyValid()
    {
        // Arrange: Mix of valid and invalid (but parseable) entries
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var validJson = """
        {
          "id": "valid-topic",
          "topic": "Valid Topic",
          "category": "Testing",
          "summary": "Valid summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "Valid overview"
          }
        }
        """;

        // Invalid: missing required field (will parse but fail domain validation)
        var invalidJson = """
        {
          "id": "invalid-topic",
          "topic": "Invalid Topic",
          "category": "Testing",
          "summary": "Missing other required fields"
        }
        """;

        File.WriteAllText(Path.Combine(entriesPath, "valid-topic.json"), validJson);
        File.WriteAllText(Path.Combine(entriesPath, "invalid-topic.json"), invalidJson);

        // Act
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);
        var topics = library.GetAllTopics();

        // Assert
        Assert.AreEqual(1, topics.Count, "Should load only valid entry");
        Assert.AreEqual("valid-topic", topics[0].Id);
    }

    [TestMethod]
    public void Constructor_NonExistentDirectory_LoadsEmptyIndexWithoutCrashing()
    {
        // Arrange: Non-existent path
        var nonExistentPath = Path.Combine(_testDataPath, "does-not-exist");

        // Act
        var library = new JsonKnowledgeLibrary(nonExistentPath, _logger);
        var topics = library.GetAllTopics();

        // Assert
        Assert.IsNotNull(topics);
        Assert.AreEqual(0, topics.Count);
    }

    [TestMethod]
    public void GetAllTopics_ReturnsAllLoadedSummaries()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entry1Json = """
        {
          "id": "topic-1",
          "topic": "Topic 1",
          "category": "Cat1",
          "summary": "Summary 1",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "keywords": ["key1"],
          "content": {
            "overview": "Overview 1"
          }
        }
        """;

        File.WriteAllText(Path.Combine(entriesPath, "topic-1.json"), entry1Json);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var topics = library.GetAllTopics();

        // Assert
        Assert.AreEqual(1, topics.Count);
        Assert.AreEqual("topic-1", topics[0].Id);
        Assert.AreEqual("Topic 1", topics[0].Topic);
        Assert.AreEqual("Summary 1", topics[0].Summary);
    }

    [TestMethod]
    public void GetAllTopics_OrderedByTopic_ReturnsAlphabeticalOrder()
    {
        // Arrange: Add entries in non-alphabetical order
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entryZ = CreateTestEntryJson("z-topic", "Zebra Topic");
        var entryA = CreateTestEntryJson("a-topic", "Apple Topic");
        var entryM = CreateTestEntryJson("m-topic", "Mango Topic");

        File.WriteAllText(Path.Combine(entriesPath, "z-topic.json"), entryZ);
        File.WriteAllText(Path.Combine(entriesPath, "a-topic.json"), entryA);
        File.WriteAllText(Path.Combine(entriesPath, "m-topic.json"), entryM);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var topics = library.GetAllTopics();

        // Assert
        Assert.AreEqual(3, topics.Count);
        Assert.AreEqual("Apple Topic", topics[0].Topic);
        Assert.AreEqual("Mango Topic", topics[1].Topic);
        Assert.AreEqual("Zebra Topic", topics[2].Topic);
    }

    // Task 3.2: Lazy Loading and Caching Tests

    [TestMethod]
    public void GetTopic_ValidId_ReturnsFullEntry()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entryJson = """
        {
          "id": "test-topic",
          "topic": "Test Topic",
          "category": "Testing",
          "summary": "Test summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "keywords": ["test", "example"],
          "content": {
            "overview": "This is a test overview",
            "keyPoints": ["Point 1", "Point 2"],
            "warnings": ["Warning 1"]
          }
        }
        """;

        File.WriteAllText(Path.Combine(entriesPath, "test-topic.json"), entryJson);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var entry = library.GetTopic("test-topic");

        // Assert
        Assert.IsNotNull(entry);
        Assert.AreEqual("test-topic", entry.Id);
        Assert.AreEqual("Test Topic", entry.Topic);
        Assert.AreEqual("This is a test overview", entry.Content.Overview);
        Assert.AreEqual(2, entry.Content.KeyPoints.Count);
        Assert.AreEqual(1, entry.Content.Warnings.Count);
    }

    [TestMethod]
    public void GetTopic_InvalidId_ReturnsNull()
    {
        // Arrange
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var entry = library.GetTopic("non-existent-topic");

        // Assert
        Assert.IsNull(entry);
    }

    [TestMethod]
    public void GetTopic_NullTopicId_ThrowsArgumentException()
    {
        // Arrange
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopic(null!));
    }

    [TestMethod]
    public void GetTopic_EmptyTopicId_ThrowsArgumentException()
    {
        // Arrange
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopic(""));
    }

    [TestMethod]
    public void GetTopic_SecondCall_LoadsFromCache()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entryJson = CreateTestEntryJson("cached-topic", "Cached Topic");
        var filePath = Path.Combine(entriesPath, "cached-topic.json");
        File.WriteAllText(filePath, entryJson);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act - First call loads from file
        var entry1 = library.GetTopic("cached-topic");

        // Delete file to prove second call uses cache
        File.Delete(filePath);

        // Second call should still work (from cache)
        var entry2 = library.GetTopic("cached-topic");

        // Assert
        Assert.IsNotNull(entry1);
        Assert.IsNotNull(entry2);
        Assert.AreEqual(entry1.Id, entry2.Id);
        Assert.AreEqual(entry1.Topic, entry2.Topic);
    }

    [TestMethod]
    public void GetTopic_MultipleConcurrentCalls_ThreadSafe()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entryJson = CreateTestEntryJson("concurrent-topic", "Concurrent Topic");
        File.WriteAllText(Path.Combine(entriesPath, "concurrent-topic.json"), entryJson);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act - Multiple concurrent calls
        var tasks = new List<Task<KnowledgeEntry?>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => library.GetTopic("concurrent-topic")));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - All tasks should succeed
        foreach (var task in tasks)
        {
            Assert.IsNotNull(task.Result);
            Assert.AreEqual("concurrent-topic", task.Result.Id);
        }
    }

    [TestMethod]
    public void GetTopic_CorruptedFile_ReturnsNullAndLogs()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var corruptedJson = """
        {
          "id": "corrupted",
          "topic": "Corrupted"
          // Missing closing braces - invalid JSON
        """;

        File.WriteAllText(Path.Combine(entriesPath, "corrupted.json"), corruptedJson);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var entry = library.GetTopic("corrupted");

        // Assert
        Assert.IsNull(entry, "Corrupted file should return null");
    }

    // Task 3.3: Error Handling and Graceful Degradation Tests

    [TestMethod]
    public void GetTopicsByCategory_ValidCategory_ReturnsMatchingTopics()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entry1Json = CreateCategorizedEntryJson("topic-1", "Topic 1", "Security");
        var entry2Json = CreateCategorizedEntryJson("topic-2", "Topic 2", "Security");
        var entry3Json = CreateCategorizedEntryJson("topic-3", "Topic 3", "Testing");

        File.WriteAllText(Path.Combine(entriesPath, "topic-1.json"), entry1Json);
        File.WriteAllText(Path.Combine(entriesPath, "topic-2.json"), entry2Json);
        File.WriteAllText(Path.Combine(entriesPath, "topic-3.json"), entry3Json);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var securityTopics = library.GetTopicsByCategory("Security");

        // Assert
        Assert.AreEqual(2, securityTopics.Count);
        Assert.IsTrue(securityTopics.All(t => t.Category == "Security"));
    }

    [TestMethod]
    public void GetTopicsByCategory_NullCategory_ThrowsArgumentException()
    {
        // Arrange
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopicsByCategory(null!));
    }

    [TestMethod]
    public void GetTopicsByCategory_EmptyCategory_ThrowsArgumentException()
    {
        // Arrange
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => library.GetTopicsByCategory(""));
    }

    [TestMethod]
    public void GetTopicsByCategory_NonExistentCategory_ReturnsEmptyList()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entryJson = CreateCategorizedEntryJson("topic-1", "Topic 1", "Security");
        File.WriteAllText(Path.Combine(entriesPath, "topic-1.json"), entryJson);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var topics = library.GetTopicsByCategory("NonExistent");

        // Assert
        Assert.IsNotNull(topics);
        Assert.AreEqual(0, topics.Count);
    }

    [TestMethod]
    public void GetTopicsByCategory_CaseInsensitive_ReturnsMatchingTopics()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entryJson = CreateCategorizedEntryJson("topic-1", "Topic 1", "Security");
        File.WriteAllText(Path.Combine(entriesPath, "topic-1.json"), entryJson);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var topics = library.GetTopicsByCategory("security"); // lowercase

        // Assert
        Assert.AreEqual(1, topics.Count);
    }

    [TestMethod]
    public void GetCategories_ReturnsAllDistinctCategories()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entry1Json = CreateCategorizedEntryJson("topic-1", "Topic 1", "Security");
        var entry2Json = CreateCategorizedEntryJson("topic-2", "Topic 2", "Security");
        var entry3Json = CreateCategorizedEntryJson("topic-3", "Topic 3", "Testing");
        var entry4Json = CreateCategorizedEntryJson("topic-4", "Topic 4", "LLM");

        File.WriteAllText(Path.Combine(entriesPath, "topic-1.json"), entry1Json);
        File.WriteAllText(Path.Combine(entriesPath, "topic-2.json"), entry2Json);
        File.WriteAllText(Path.Combine(entriesPath, "topic-3.json"), entry3Json);
        File.WriteAllText(Path.Combine(entriesPath, "topic-4.json"), entry4Json);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var categories = library.GetCategories();

        // Assert
        Assert.AreEqual(3, categories.Count); // 3 distinct categories
        Assert.IsTrue(categories.Contains("Security"));
        Assert.IsTrue(categories.Contains("Testing"));
        Assert.IsTrue(categories.Contains("LLM"));
    }

    [TestMethod]
    public void GetCategories_NoEntries_ReturnsEmptyList()
    {
        // Arrange
        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var categories = library.GetCategories();

        // Assert
        Assert.IsNotNull(categories);
        Assert.AreEqual(0, categories.Count);
    }

    [TestMethod]
    public void GetCategories_Sorted_ReturnsAlphabeticalOrder()
    {
        // Arrange
        var entriesPath = Path.Combine(_testDataPath, "entries");
        Directory.CreateDirectory(entriesPath);

        var entry1Json = CreateCategorizedEntryJson("topic-1", "Topic 1", "Zebra");
        var entry2Json = CreateCategorizedEntryJson("topic-2", "Topic 2", "Apple");
        var entry3Json = CreateCategorizedEntryJson("topic-3", "Topic 3", "Mango");

        File.WriteAllText(Path.Combine(entriesPath, "topic-1.json"), entry1Json);
        File.WriteAllText(Path.Combine(entriesPath, "topic-2.json"), entry2Json);
        File.WriteAllText(Path.Combine(entriesPath, "topic-3.json"), entry3Json);

        var library = new JsonKnowledgeLibrary(_testDataPath, _logger);

        // Act
        var categories = library.GetCategories();

        // Assert
        Assert.AreEqual(3, categories.Count);
        Assert.AreEqual("Apple", categories[0]);
        Assert.AreEqual("Mango", categories[1]);
        Assert.AreEqual("Zebra", categories[2]);
    }

    // Helper methods to create test JSON entries
    private string CreateTestEntryJson(string id, string topic)
    {
        return CreateCategorizedEntryJson(id, topic, "Testing");
    }

    private string CreateCategorizedEntryJson(string id, string topic, string category)
    {
        return $$"""
        {
          "id": "{{id}}",
          "topic": "{{topic}}",
          "category": "{{category}}",
          "summary": "Test summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "Test overview"
          }
        }
        """;
    }
}
