# Knowledge Library - TDD Implementation Plan

**Created**: 2025-11-14
**Related Design**: KNOWLEDGE_LIBRARY_DESIGN.md
**Status**: Ready for Implementation

---

## Overview

This plan breaks down the Knowledge Library implementation into **7 independent sessions**, each following strict TDD (Test-Driven Development) principles. Each session can be completed independently, with clear deliverables and commit points.

**Key Principles:**
1. ✅ **Red-Green-Refactor** cycle for every feature
2. ✅ **Commit after each passing test** (crash-resistant progress)
3. ✅ **Independent sessions** (can resume from any completed session)
4. ✅ **Test-first** (no implementation without failing test first)

---

## Session Structure

Each session follows this pattern:

```
1. Setup Phase
   - Review previous session's deliverables
   - Understand what needs to be built

2. TDD Implementation Phase
   For each feature:
   a) RED: Write failing test
   b) GREEN: Implement minimal code to pass
   c) REFACTOR: Improve code quality
   d) COMMIT: Save progress

3. Integration Phase
   - Wire up components
   - Run all tests
   - Commit final state

4. Session Deliverable
   - Clear artifact (working code + passing tests)
   - Ready for next session or pause
```

---

## Session 1: Domain Models (Foundation)

**Goal**: Create knowledge entry domain models with validation logic

**Estimated Time**: 45-60 minutes

### Test Files to Create
- `TransparentAiAgentCore_Tests/Domain/Knowledge/KnowledgeEntryTests.cs`
- `TransparentAiAgentCore_Tests/Domain/Knowledge/KnowledgeContentTests.cs`
- `TransparentAiAgentCore_Tests/Domain/Knowledge/KnowledgeExampleTests.cs`
- `TransparentAiAgentCore_Tests/Domain/Knowledge/KnowledgeEntrySummaryTests.cs`

### TDD Tasks

#### Task 1.1: KnowledgeExample Model
**Test class**: `KnowledgeExampleTests.cs`

**RED Tests** (write these first):
```csharp
[TestMethod]
public void Constructor_NullTitle_ThrowsArgumentException()

[TestMethod]
public void Constructor_EmptyTitle_ThrowsArgumentException()

[TestMethod]
public void Constructor_ValidTitle_CreatesInstance()

[TestMethod]
public void Constructor_NullCode_SetsEmptyString()
```

**GREEN**: Implement `KnowledgeExample.cs` in `TransparentAiAgentCore/Domain/Knowledge/`
```csharp
namespace TransparentAiAgentCore.Domain.Knowledge;

public class KnowledgeExample
{
    public string Title { get; }
    public string Code { get; }
    public string Explanation { get; }

    public KnowledgeExample(string title, string explanation, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or whitespace", nameof(title));

        if (string.IsNullOrWhiteSpace(explanation))
            throw new ArgumentException("Explanation cannot be null or whitespace", nameof(explanation));

        Title = title;
        Explanation = explanation;
        Code = code ?? string.Empty;
    }
}
```

**COMMIT**: "feat: add KnowledgeExample domain model with validation"

---

#### Task 1.2: KnowledgeContent Model
**Test class**: `KnowledgeContentTests.cs`

**RED Tests**:
```csharp
[TestMethod]
public void Constructor_NullOverview_ThrowsArgumentException()

[TestMethod]
public void Constructor_ValidOverview_CreatesInstance()

[TestMethod]
public void Constructor_NullKeyPoints_CreatesEmptyList()

[TestMethod]
public void Constructor_NullExamples_CreatesEmptyList()

[TestMethod]
public void Constructor_NullWarnings_CreatesEmptyList()

[TestMethod]
public void Constructor_NullBestPractices_CreatesEmptyList()

[TestMethod]
public void Constructor_NullRelatedTopics_CreatesEmptyList()

[TestMethod]
public void Constructor_NullReferences_CreatesEmptyList()
```

**GREEN**: Implement `KnowledgeContent.cs`
```csharp
namespace TransparentAiAgentCore.Domain.Knowledge;

public class KnowledgeContent
{
    public string Overview { get; }
    public IReadOnlyList<string> KeyPoints { get; }
    public IReadOnlyList<KnowledgeExample> Examples { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> BestPractices { get; }
    public IReadOnlyList<string> RelatedTopics { get; }
    public IReadOnlyList<KnowledgeReference> References { get; }

    public KnowledgeContent(
        string overview,
        List<string>? keyPoints = null,
        List<KnowledgeExample>? examples = null,
        List<string>? warnings = null,
        List<string>? bestPractices = null,
        List<string>? relatedTopics = null,
        List<KnowledgeReference>? references = null)
    {
        if (string.IsNullOrWhiteSpace(overview))
            throw new ArgumentException("Overview cannot be null or whitespace", nameof(overview));

        Overview = overview;
        KeyPoints = (keyPoints ?? new List<string>()).AsReadOnly();
        Examples = (examples ?? new List<KnowledgeExample>()).AsReadOnly();
        Warnings = (warnings ?? new List<string>()).AsReadOnly();
        BestPractices = (bestPractices ?? new List<string>()).AsReadOnly();
        RelatedTopics = (relatedTopics ?? new List<string>()).AsReadOnly();
        References = (references ?? new List<KnowledgeReference>()).AsReadOnly();
    }
}

public class KnowledgeReference
{
    public string Title { get; }
    public string? Url { get; }

    public KnowledgeReference(string title, string? url = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or whitespace", nameof(title));

        Title = title;
        Url = url;
    }
}
```

**COMMIT**: "feat: add KnowledgeContent domain model with optional fields"

---

#### Task 1.3: KnowledgeEntry Model
**Test class**: `KnowledgeEntryTests.cs`

**RED Tests**:
```csharp
[TestMethod]
public void Constructor_NullId_ThrowsArgumentException()

[TestMethod]
public void Constructor_EmptyId_ThrowsArgumentException()

[TestMethod]
public void Constructor_InvalidIdFormat_ThrowsArgumentException()  // Not kebab-case

[TestMethod]
public void Constructor_NullTopic_ThrowsArgumentException()

[TestMethod]
public void Constructor_NullCategory_ThrowsArgumentException()

[TestMethod]
public void Constructor_NullSummary_ThrowsArgumentException()

[TestMethod]
public void Constructor_InvalidGapLikelihood_ThrowsArgumentException()  // Not "low", "medium", "high"

[TestMethod]
public void Constructor_InvalidDateFormat_ThrowsArgumentException()

[TestMethod]
public void Constructor_NullContent_ThrowsArgumentException()

[TestMethod]
public void Constructor_ValidParameters_CreatesInstance()

[TestMethod]
public void Constructor_NullKeywords_CreatesEmptyList()
```

**GREEN**: Implement `KnowledgeEntry.cs`
```csharp
namespace TransparentAiAgentCore.Domain.Knowledge;

using System.Text.RegularExpressions;

public class KnowledgeEntry
{
    private static readonly Regex KebabCaseRegex = new Regex(@"^[a-z0-9-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidGapLikelihoods = new() { "low", "medium", "high" };

    public string Id { get; }
    public string Topic { get; }
    public string Category { get; }
    public IReadOnlyList<string> Keywords { get; }
    public string Summary { get; }
    public string KnowledgeGapLikelihood { get; }
    public string LastUpdated { get; }
    public string LastChecked { get; }
    public KnowledgeContent Content { get; }

    public KnowledgeEntry(
        string id,
        string topic,
        string category,
        string summary,
        string knowledgeGapLikelihood,
        string lastUpdated,
        string lastChecked,
        KnowledgeContent content,
        List<string>? keywords = null)
    {
        // Validate id
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or whitespace", nameof(id));
        if (!KebabCaseRegex.IsMatch(id))
            throw new ArgumentException("Id must be in kebab-case format (lowercase letters, numbers, and hyphens only)", nameof(id));

        // Validate required string fields
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or whitespace", nameof(topic));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or whitespace", nameof(category));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Summary cannot be null or whitespace", nameof(summary));

        // Validate knowledgeGapLikelihood
        if (!ValidGapLikelihoods.Contains(knowledgeGapLikelihood?.ToLowerInvariant() ?? ""))
            throw new ArgumentException("KnowledgeGapLikelihood must be 'low', 'medium', or 'high'", nameof(knowledgeGapLikelihood));

        // Validate dates (YYYY-MM-DD format)
        if (!IsValidDateFormat(lastUpdated))
            throw new ArgumentException("LastUpdated must be in YYYY-MM-DD format", nameof(lastUpdated));
        if (!IsValidDateFormat(lastChecked))
            throw new ArgumentException("LastChecked must be in YYYY-MM-DD format", nameof(lastChecked));

        // Validate content
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        Id = id;
        Topic = topic;
        Category = category;
        Summary = summary;
        KnowledgeGapLikelihood = knowledgeGapLikelihood.ToLowerInvariant();
        LastUpdated = lastUpdated;
        LastChecked = lastChecked;
        Content = content;
        Keywords = (keywords ?? new List<string>()).AsReadOnly();
    }

    private static bool IsValidDateFormat(string date)
    {
        if (string.IsNullOrWhiteSpace(date)) return false;
        return DateTime.TryParseExact(date, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _);
    }
}
```

**COMMIT**: "feat: add KnowledgeEntry domain model with comprehensive validation"

---

#### Task 1.4: KnowledgeEntrySummary Model
**Test class**: `KnowledgeEntrySummaryTests.cs`

**RED Tests**:
```csharp
[TestMethod]
public void Constructor_NullId_ThrowsArgumentException()

[TestMethod]
public void Constructor_NullTopic_ThrowsArgumentException()

[TestMethod]
public void Constructor_ValidParameters_CreatesInstance()

[TestMethod]
public void Constructor_FromKnowledgeEntry_ExtractsCorrectFields()
```

**GREEN**: Implement `KnowledgeEntrySummary.cs`
```csharp
namespace TransparentAiAgentCore.Domain.Knowledge;

public class KnowledgeEntrySummary
{
    public string Id { get; }
    public string Topic { get; }
    public string Category { get; }
    public string Summary { get; }
    public IReadOnlyList<string> Keywords { get; }
    public string KnowledgeGapLikelihood { get; }
    public string LastUpdated { get; }
    public string LastChecked { get; }

    public KnowledgeEntrySummary(
        string id,
        string topic,
        string category,
        string summary,
        string knowledgeGapLikelihood,
        string lastUpdated,
        string lastChecked,
        List<string>? keywords = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or whitespace", nameof(id));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or whitespace", nameof(topic));

        Id = id;
        Topic = topic;
        Category = category;
        Summary = summary;
        KnowledgeGapLikelihood = knowledgeGapLikelihood;
        LastUpdated = lastUpdated;
        LastChecked = lastChecked;
        Keywords = (keywords ?? new List<string>()).AsReadOnly();
    }

    // Factory method to create from KnowledgeEntry
    public static KnowledgeEntrySummary FromEntry(KnowledgeEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        return new KnowledgeEntrySummary(
            entry.Id,
            entry.Topic,
            entry.Category,
            entry.Summary,
            entry.KnowledgeGapLikelihood,
            entry.LastUpdated,
            entry.LastChecked,
            entry.Keywords.ToList());
    }
}
```

**COMMIT**: "feat: add KnowledgeEntrySummary with factory method"

---

### Session 1 Deliverable

✅ **Completed**:
- 4 domain models with full validation
- 4 test classes with comprehensive test coverage
- All tests passing
- Models enforce guardrails (kebab-case IDs, valid dates, valid gap likelihood)

✅ **Files Created**:
- `TransparentAiAgentCore/Domain/Knowledge/KnowledgeExample.cs`
- `TransparentAiAgentCore/Domain/Knowledge/KnowledgeContent.cs`
- `TransparentAiAgentCore/Domain/Knowledge/KnowledgeReference.cs`
- `TransparentAiAgentCore/Domain/Knowledge/KnowledgeEntry.cs`
- `TransparentAiAgentCore/Domain/Knowledge/KnowledgeEntrySummary.cs`
- Test files (4 files)

✅ **Test Coverage**: ~95% for domain models (excludes trivial getters)

**FINAL COMMIT**: "feat(session-1): complete knowledge library domain models"

---

## Session 2: IKnowledgeLibrary Interface & JSON Deserialization

**Goal**: Define service interface and enable JSON deserialization of knowledge entries

**Estimated Time**: 30-45 minutes

### Test Files to Create
- `TransparentAiAgentCore_Tests/Domain/Knowledge/IKnowledgeLibraryContractTests.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/Knowledge/JsonKnowledgeEntryDeserializationTests.cs`

### TDD Tasks

#### Task 2.1: IKnowledgeLibrary Interface
**Test class**: `IKnowledgeLibraryContractTests.cs`

**Note**: This is a contract test (tests the interface contract, not implementation)

**RED Tests**:
```csharp
[TestMethod]
public void GetTopic_NullTopicId_ThrowsArgumentException()

[TestMethod]
public void GetTopic_EmptyTopicId_ThrowsArgumentException()

[TestMethod]
public void GetTopic_NonExistentTopic_ReturnsNull()  // Graceful failure

[TestMethod]
public void GetAllTopics_ReturnsReadOnlyList()

[TestMethod]
public void GetTopicsByCategory_NullCategory_ThrowsArgumentException()

[TestMethod]
public void GetCategories_ReturnsDistinctCategories()
```

**GREEN**: Implement `IKnowledgeLibrary.cs`
```csharp
namespace TransparentAiAgentCore.Domain.Knowledge;

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
```

**COMMIT**: "feat: add IKnowledgeLibrary domain service interface"

---

#### Task 2.2: JSON Deserialization Support
**Test class**: `JsonKnowledgeEntryDeserializationTests.cs`

**Setup**: Create test JSON files in test project

**RED Tests**:
```csharp
[TestMethod]
public void Deserialize_ValidJson_CreatesKnowledgeEntry()

[TestMethod]
public void Deserialize_MinimalJson_CreatesKnowledgeEntry()  // Only required fields

[TestMethod]
public void Deserialize_InvalidJson_ThrowsJsonException()

[TestMethod]
public void Deserialize_MissingRequiredField_ThrowsJsonException()

[TestMethod]
public void Deserialize_CompleteEntry_AllFieldsPopulated()
```

**GREEN**: Add JSON attributes to domain models
```csharp
// Update KnowledgeEntry.cs
using System.Text.Json.Serialization;

public class KnowledgeEntry
{
    [JsonConstructor]
    public KnowledgeEntry(
        [JsonPropertyName("id")] string id,
        [JsonPropertyName("topic")] string topic,
        [JsonPropertyName("category")] string category,
        [JsonPropertyName("summary")] string summary,
        [JsonPropertyName("knowledgeGapLikelihood")] string knowledgeGapLikelihood,
        [JsonPropertyName("lastUpdated")] string lastUpdated,
        [JsonPropertyName("lastChecked")] string lastChecked,
        [JsonPropertyName("content")] KnowledgeContent content,
        [JsonPropertyName("keywords")] List<string>? keywords = null)
    {
        // ... existing validation ...
    }
}

// Similar updates for KnowledgeContent, KnowledgeExample, KnowledgeReference
```

**Test Implementation**:
```csharp
using System.Text.Json;

[TestClass]
public class JsonKnowledgeEntryDeserializationTests
{
    [TestMethod]
    public void Deserialize_ValidJson_CreatesKnowledgeEntry()
    {
        // Arrange
        var json = """
        {
          "id": "test-topic",
          "topic": "Test Topic",
          "category": "Testing",
          "summary": "A test summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "keywords": ["test", "sample"],
          "content": {
            "overview": "This is an overview"
          }
        }
        """;

        // Act
        var entry = JsonSerializer.Deserialize<KnowledgeEntry>(json);

        // Assert
        Assert.IsNotNull(entry);
        Assert.AreEqual("test-topic", entry.Id);
        Assert.AreEqual("Test Topic", entry.Topic);
    }

    // ... more tests ...
}
```

**COMMIT**: "feat: add JSON deserialization support for knowledge models"

---

### Session 2 Deliverable

✅ **Completed**:
- IKnowledgeLibrary interface defined
- JSON deserialization working for all models
- Validation still enforced during deserialization
- Contract tests ensure interface requirements

✅ **Files Created**:
- `TransparentAiAgentCore/Domain/Knowledge/IKnowledgeLibrary.cs`
- Updated domain models with JSON attributes
- Test files (2 files)

**FINAL COMMIT**: "feat(session-2): complete IKnowledgeLibrary interface and JSON support"

---

## Session 3: JsonKnowledgeLibrary Implementation (Infrastructure)

**Goal**: Implement IKnowledgeLibrary with file system scanning and in-memory caching

**Estimated Time**: 60-75 minutes

### Test Files to Create
- `TransparentAiAgentCore_Tests/Infrastructure/Knowledge/JsonKnowledgeLibraryTests.cs`

### TDD Tasks

#### Task 3.1: Directory Scanning and Index Loading
**Test class**: `JsonKnowledgeLibraryTests.cs`

**Setup**: Create test directory structure with sample JSON files

**RED Tests**:
```csharp
[TestMethod]
public void Constructor_EmptyDirectory_LoadsEmptyIndex()

[TestMethod]
public void Constructor_ValidEntries_LoadsAllEntries()

[TestMethod]
public void Constructor_CorruptedEntry_SkipsEntryAndContinues()

[TestMethod]
public void Constructor_MixedValidInvalid_LoadsOnlyValid()

[TestMethod]
public void Constructor_NonExistentDirectory_LogsWarningAndContinues()

[TestMethod]
public void GetAllTopics_ReturnsAllLoadedSummaries()

[TestMethod]
public void GetAllTopics_OrderedByTopic_ReturnsAlphabeticalOrder()
```

**GREEN**: Implement `JsonKnowledgeLibrary.cs`
```csharp
namespace TransparentAiAgentCore.Infrastructure.Knowledge;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Knowledge;

public class JsonKnowledgeLibrary : IKnowledgeLibrary
{
    private readonly string _entriesPath;
    private readonly ILogger<JsonKnowledgeLibrary> _logger;
    private readonly Dictionary<string, KnowledgeEntry> _entriesCache = new();
    private readonly List<KnowledgeEntrySummary> _index = new();
    private readonly object _cacheLock = new object();

    public JsonKnowledgeLibrary(string knowledgeBasePath, ILogger<JsonKnowledgeLibrary> logger)
    {
        if (string.IsNullOrWhiteSpace(knowledgeBasePath))
            throw new ArgumentException("Knowledge base path cannot be null or whitespace", nameof(knowledgeBasePath));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _entriesPath = Path.Combine(knowledgeBasePath, "entries");

        LoadIndex();
    }

    private void LoadIndex()
    {
        try
        {
            if (!Directory.Exists(_entriesPath))
            {
                _logger.LogWarning("Knowledge library entries directory not found at {Path}", _entriesPath);
                return;
            }

            var entryFiles = Directory.GetFiles(_entriesPath, "*.json");
            var loadedCount = 0;
            var failedCount = 0;

            foreach (var filePath in entryFiles)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var entry = JsonSerializer.Deserialize<KnowledgeEntry>(json);

                    if (entry != null && !string.IsNullOrEmpty(entry.Id))
                    {
                        // Add to index
                        _index.Add(KnowledgeEntrySummary.FromEntry(entry));
                        loadedCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid entry in file {Path}: missing required fields", filePath);
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse knowledge entry file: {Path}", filePath);
                    failedCount++;
                }
            }

            // Sort index by topic name for consistent ordering
            _index.Sort((a, b) => string.Compare(a.Topic, b.Topic, StringComparison.OrdinalIgnoreCase));

            _logger.LogInformation("Knowledge library initialized: {Loaded} entries loaded, {Failed} failed",
                loadedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan knowledge library directory");
        }
    }

    public KnowledgeEntry? GetTopic(string topicId)
    {
        if (string.IsNullOrWhiteSpace(topicId))
            throw new ArgumentException("Topic ID cannot be null or whitespace", nameof(topicId));

        // Check cache first
        lock (_cacheLock)
        {
            if (_entriesCache.TryGetValue(topicId, out var cached))
            {
                return cached;
            }
        }

        // Load from file
        try
        {
            var entryPath = Path.Combine(_entriesPath, $"{topicId}.json");
            if (!File.Exists(entryPath))
            {
                _logger.LogWarning("Knowledge entry not found: {TopicId}", topicId);
                return null;
            }

            var json = File.ReadAllText(entryPath);
            var entry = JsonSerializer.Deserialize<KnowledgeEntry>(json);

            // Validate
            if (entry == null || entry.Id != topicId)
            {
                _logger.LogError("Invalid knowledge entry file: {TopicId}", topicId);
                return null;
            }

            // Cache and return
            lock (_cacheLock)
            {
                _entriesCache[topicId] = entry;
            }

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load knowledge entry: {TopicId}", topicId);
            return null;
        }
    }

    public IReadOnlyList<KnowledgeEntrySummary> GetAllTopics()
    {
        return _index.AsReadOnly();
    }

    public IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or whitespace", nameof(category));

        return _index
            .Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<string> GetCategories()
    {
        return _index
            .Select(e => e.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList()
            .AsReadOnly();
    }
}
```

**COMMIT**: "feat: implement JsonKnowledgeLibrary with directory scanning"

---

#### Task 3.2: Lazy Loading and Caching
**RED Tests**:
```csharp
[TestMethod]
public void GetTopic_FirstCall_LoadsFromFile()

[TestMethod]
public void GetTopic_SecondCall_LoadsFromCache()

[TestMethod]
public void GetTopic_MultipleConcurrentCalls_ThreadSafe()

[TestMethod]
public void GetTopic_ValidId_ReturnsFullEntry()

[TestMethod]
public void GetTopic_InvalidId_ReturnsNull()
```

**GREEN**: Already implemented in previous task (lazy loading logic in `GetTopic`)

**REFACTOR**: Add thread-safety tests and verify caching behavior

**COMMIT**: "test: add caching and thread-safety tests for JsonKnowledgeLibrary"

---

#### Task 3.3: Error Handling and Graceful Degradation
**RED Tests**:
```csharp
[TestMethod]
public void GetTopicsByCategory_EmptyCategory_ReturnsEmptyList()

[TestMethod]
public void GetTopicsByCategory_NonExistentCategory_ReturnsEmptyList()

[TestMethod]
public void GetCategories_NoEntries_ReturnsEmptyList()

[TestMethod]
public void GetTopic_CorruptedFile_ReturnsNullAndLogs()
```

**GREEN**: Verify error handling is correct in existing implementation

**COMMIT**: "test: add error handling tests for JsonKnowledgeLibrary"

---

### Session 3 Deliverable

✅ **Completed**:
- JsonKnowledgeLibrary fully implemented
- Directory scanning at startup
- Lazy loading with in-memory caching
- Thread-safe implementation
- Graceful error handling (corrupted files don't crash)
- Comprehensive test coverage

✅ **Files Created**:
- `TransparentAiAgentCore/Infrastructure/Knowledge/JsonKnowledgeLibrary.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/Knowledge/JsonKnowledgeLibraryTests.cs`

✅ **Test Coverage**: ~90% (infrastructure layer)

**FINAL COMMIT**: "feat(session-3): complete JsonKnowledgeLibrary implementation"

---

## Session 4: KnowledgeLibraryTool (Built-in Tool)

**Goal**: Implement the tool that LLM uses to query the knowledge library

**Estimated Time**: 45-60 minutes

### Test Files to Create
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInKnowledge/KnowledgeLibraryToolTests.cs`
- `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInKnowledge/KnowledgeLibraryToolExecutorTests.cs`

### TDD Tasks

#### Task 4.1: KnowledgeLibraryTool (ITool Implementation)
**Test class**: `KnowledgeLibraryToolTests.cs`

**RED Tests**:
```csharp
[TestMethod]
public void Name_ReturnsCorrectToolName()

[TestMethod]
public void Description_ContainsGuardrailsGuidance()

[TestMethod]
public void ParametersSchema_ValidJsonSchema()

[TestMethod]
public void SourceType_ReturnsBuiltInKnowledge()

[TestMethod]
public void Metadata_ContainsCorrectSourceType()
```

**GREEN**: Implement `KnowledgeLibraryTool.cs`
```csharp
namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

using TransparentAiAgentCore.Domain.Tools;

public class KnowledgeLibraryTool : ITool
{
    private readonly IReadOnlyDictionary<string, string> _metadata;

    public KnowledgeLibraryTool()
    {
        var metadata = new Dictionary<string, string>
        {
            ["SourceType"] = "BuiltInKnowledge",
            ["Category"] = "Teaching"
        };

        _metadata = metadata;
    }

    public string Name => "knowledge_library_query";

    public string Description =>
        "Retrieve critical guardrails from the knowledge library for a specific topic. " +
        "Returns essential principles and red lines. Use when teaching security, privacy, or safety-critical concepts. " +
        "The guardrails guide your teaching; fill in details using your own knowledge.";

    public string ParametersSchema => """
    {
      "type": "object",
      "properties": {
        "topic": {
          "type": "string",
          "description": "The topic ID to query (e.g., 'api-key-security'). Available topics are listed in your system instructions."
        }
      },
      "required": ["topic"]
    }
    """;

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public IReadOnlyDictionary<string, string> Metadata => _metadata;
}
```

**Note**: You'll need to add `BuiltInKnowledge` to the `ToolSourceType` enum:
```csharp
// In TransparentAiAgentCore/Domain/Tools/ToolSourceType.cs
public enum ToolSourceType
{
    MCP,
    BuiltInUIControl,
    BuiltInKnowledge  // Add this
}
```

**COMMIT**: "feat: add KnowledgeLibraryTool as built-in tool"

---

#### Task 4.2: KnowledgeLibraryToolExecutor
**Test class**: `KnowledgeLibraryToolExecutorTests.cs`

**RED Tests**:
```csharp
[TestMethod]
public async Task ExecuteAsync_MissingTopicParameter_ReturnsError()

[TestMethod]
public async Task ExecuteAsync_EmptyTopicParameter_ReturnsError()

[TestMethod]
public async Task ExecuteAsync_ValidTopic_ReturnsFormattedContent()

[TestMethod]
public async Task ExecuteAsync_NonExistentTopic_ReturnsNotFoundError()

[TestMethod]
public async Task ExecuteAsync_ValidTopic_FormatsMarkdown()

[TestMethod]
public async Task ExecuteAsync_EntryWithExamples_IncludesExamplesInOutput()

[TestMethod]
public async Task ExecuteAsync_EntryWithWarnings_IncludesWarningsInOutput()
```

**GREEN**: Implement `KnowledgeLibraryToolExecutor.cs`
```csharp
namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

using System.Text;
using System.Text.Json;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Domain.Tools;

public class KnowledgeLibraryToolExecutor : IToolExecutor
{
    private readonly IKnowledgeLibrary _library;

    public KnowledgeLibraryToolExecutor(IKnowledgeLibrary library)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
    }

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public async Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        if (toolName != "knowledge_library_query")
        {
            return ToolExecutionResult.Error($"Unknown tool: {toolName}");
        }

        // Extract topic parameter
        if (!arguments.TryGetValue("topic", out var topicObj))
        {
            return ToolExecutionResult.Error("Missing required parameter 'topic'");
        }

        var topicId = topicObj?.ToString();
        if (string.IsNullOrWhiteSpace(topicId))
        {
            return ToolExecutionResult.Error("Parameter 'topic' cannot be empty");
        }

        // Query library
        var entry = _library.GetTopic(topicId);
        if (entry == null)
        {
            return ToolExecutionResult.Error($"Topic '{topicId}' not found in knowledge library");
        }

        // Format entry for LLM consumption
        var formattedContent = FormatKnowledgeEntry(entry);
        return ToolExecutionResult.Success(formattedContent);
    }

    private static string FormatKnowledgeEntry(KnowledgeEntry entry)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {entry.Topic}");
        sb.AppendLine();
        sb.AppendLine($"**Category:** {entry.Category}");
        sb.AppendLine($"**Knowledge Gap Likelihood:** {entry.KnowledgeGapLikelihood}");
        sb.AppendLine($"**Last Updated:** {entry.LastUpdated}");
        sb.AppendLine();

        // Overview
        sb.AppendLine("## Overview");
        sb.AppendLine(entry.Content.Overview);
        sb.AppendLine();

        // Key Points
        if (entry.Content.KeyPoints.Any())
        {
            sb.AppendLine("## Key Points");
            foreach (var point in entry.Content.KeyPoints)
            {
                sb.AppendLine($"- {point}");
            }
            sb.AppendLine();
        }

        // Examples
        if (entry.Content.Examples.Any())
        {
            sb.AppendLine("## Examples");
            foreach (var example in entry.Content.Examples)
            {
                sb.AppendLine($"### {example.Title}");
                if (!string.IsNullOrEmpty(example.Code))
                {
                    sb.AppendLine("```");
                    sb.AppendLine(example.Code);
                    sb.AppendLine("```");
                }
                sb.AppendLine(example.Explanation);
                sb.AppendLine();
            }
        }

        // Warnings
        if (entry.Content.Warnings.Any())
        {
            sb.AppendLine("## ⚠️ Warnings");
            foreach (var warning in entry.Content.Warnings)
            {
                sb.AppendLine($"- {warning}");
            }
            sb.AppendLine();
        }

        // Best Practices
        if (entry.Content.BestPractices.Any())
        {
            sb.AppendLine("## Best Practices");
            foreach (var practice in entry.Content.BestPractices)
            {
                sb.AppendLine($"- {practice}");
            }
            sb.AppendLine();
        }

        // Related Topics
        if (entry.Content.RelatedTopics.Any())
        {
            sb.AppendLine("## Related Topics");
            sb.AppendLine($"For more information, you can also query: {string.Join(", ", entry.Content.RelatedTopics)}");
            sb.AppendLine();
        }

        // References
        if (entry.Content.References.Any())
        {
            sb.AppendLine("## References");
            foreach (var reference in entry.Content.References)
            {
                if (!string.IsNullOrEmpty(reference.Url))
                {
                    sb.AppendLine($"- [{reference.Title}]({reference.Url})");
                }
                else
                {
                    sb.AppendLine($"- {reference.Title}");
                }
            }
        }

        return sb.ToString();
    }
}
```

**COMMIT**: "feat: implement KnowledgeLibraryToolExecutor with markdown formatting"

---

#### Task 4.3: Tool Registry Integration
**Test class**: `BuiltInKnowledgeToolRegistryTests.cs`

**RED Tests**:
```csharp
[TestMethod]
public void GetTools_ReturnsKnowledgeLibraryTool()

[TestMethod]
public void GetTool_KnowledgeLibraryQuery_ReturnsTool()

[TestMethod]
public void GetTool_UnknownTool_ReturnsNull()
```

**GREEN**: Implement `BuiltInKnowledgeToolRegistry.cs`
```csharp
namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

using TransparentAiAgentCore.Domain.Tools;

public class BuiltInKnowledgeToolRegistry : IToolRegistry
{
    private readonly KnowledgeLibraryTool _tool;

    public BuiltInKnowledgeToolRegistry()
    {
        _tool = new KnowledgeLibraryTool();
    }

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public async Task<IReadOnlyList<ITool>> GetToolsAsync()
    {
        return await Task.FromResult(new List<ITool> { _tool }.AsReadOnly());
    }

    public async Task<ITool?> GetToolAsync(string toolName)
    {
        if (toolName == _tool.Name)
        {
            return await Task.FromResult(_tool);
        }
        return null;
    }
}
```

**COMMIT**: "feat: add BuiltInKnowledgeToolRegistry for tool discovery"

---

### Session 4 Deliverable

✅ **Completed**:
- KnowledgeLibraryTool implementing ITool
- KnowledgeLibraryToolExecutor with markdown formatting
- BuiltInKnowledgeToolRegistry for tool discovery
- Comprehensive test coverage for all components

✅ **Files Created**:
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInKnowledge/KnowledgeLibraryTool.cs`
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInKnowledge/KnowledgeLibraryToolExecutor.cs`
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInKnowledge/BuiltInKnowledgeToolRegistry.cs`
- Test files (3 files)

✅ **Test Coverage**: ~90% for tool implementation

**FINAL COMMIT**: "feat(session-4): complete KnowledgeLibraryTool implementation"

---

## Session 5: System Prompt Integration & DI Setup

**Goal**: Integrate knowledge library into teaching mode system prompt and configure dependency injection

**Estimated Time**: 45-60 minutes

### Test Files to Create
- `TransparentAiAgentCore_Tests/Application/Teaching/TeachingModePromptBuilderTests.cs`
- Integration tests for DI setup

### TDD Tasks

#### Task 5.1: System Prompt Builder
**Test class**: `TeachingModePromptBuilderTests.cs`

**RED Tests**:
```csharp
[TestMethod]
public void BuildKnowledgeLibrarySection_EmptyLibrary_ReturnsHeaderOnly()

[TestMethod]
public void BuildKnowledgeLibrarySection_WithTopics_ListsAllTopics()

[TestMethod]
public void BuildKnowledgeLibrarySection_IncludesGapLikelihoodGuidance()

[TestMethod]
public void BuildKnowledgeLibrarySection_TopicsSortedAlphabetically()

[TestMethod]
public void BuildKnowledgeLibrarySection_IncludesCriticalTopicsList()

[TestMethod]
public void BuildKnowledgeLibrarySection_FormatsCorrectlyForLLM()
```

**GREEN**: Implement `TeachingModePromptBuilder.cs`
```csharp
namespace TransparentAiAgentCore.Application.Teaching;

using System.Text;
using TransparentAiAgentCore.Domain.Knowledge;

public class TeachingModePromptBuilder
{
    private readonly IKnowledgeLibrary _library;

    public TeachingModePromptBuilder(IKnowledgeLibrary library)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
    }

    public string BuildKnowledgeLibrarySection()
    {
        var topics = _library.GetAllTopics();
        var sb = new StringBuilder();

        sb.AppendLine("# Knowledge Library - Guardrails for Teaching");
        sb.AppendLine();
        sb.AppendLine("You have access to a knowledge library containing GUARDRAILS for teaching critical concepts.");
        sb.AppendLine("These entries provide essential principles, red lines, and corrections - NOT comprehensive documentation.");
        sb.AppendLine();
        sb.AppendLine("**How to use:**");
        sb.AppendLine("1. Query the library to get critical guardrails on a topic");
        sb.AppendLine("2. Use the guardrails to guide your teaching");
        sb.AppendLine("3. Fill in details using your built-in knowledge");
        sb.AppendLine("4. The library tells you what's CRITICAL; you provide the comprehensive teaching");
        sb.AppendLine();

        if (topics.Any())
        {
            sb.AppendLine("**Available knowledge topics:**");
            foreach (var topic in topics)
            {
                sb.AppendLine($"- `{topic.Id}`: {topic.Topic} - {topic.Summary} (gap likelihood: {topic.KnowledgeGapLikelihood})");
            }
            sb.AppendLine();
        }

        sb.AppendLine("**When to query the library:**");
        sb.AppendLine("1. Teaching security, privacy, or safety-critical topics (ALWAYS query for guardrails)");
        sb.AppendLine("2. Application-specific features (context windows, MCP, teaching mode)");
        sb.AppendLine("3. When you need to correct potential misconceptions");
        sb.AppendLine("4. Before teaching best practices (get the \"must-dos\" and \"must-not-dos\")");
        sb.AppendLine();

        sb.AppendLine("**After querying:**");
        sb.AppendLine("- Treat the entry as GUARDRAILS, not exhaustive content");
        sb.AppendLine("- Teach comprehensively using the guardrails + your knowledge");
        sb.AppendLine("- Always respect warnings and red lines from the library");
        sb.AppendLine("- Use your judgment to expand on principles with relevant details");
        sb.AppendLine();

        sb.AppendLine("**Understanding Knowledge Gap Likelihood:**");
        sb.AppendLine("Each topic includes a \"knowledgeGapLikelihood\" field indicating how reliable your built-in knowledge is:");
        sb.AppendLine("- **Low**: Your training data is likely current - rely on inner knowledge confidently");
        sb.AppendLine("- **Medium**: Your knowledge may be partially outdated - cross-reference with library");
        sb.AppendLine("- **High**: Your knowledge is very likely outdated - prefer web search if available; if not, warn user about potential outdated information");

        return sb.ToString();
    }
}
```

**COMMIT**: "feat: add TeachingModePromptBuilder for system prompt integration"

---

#### Task 5.2: Dependency Injection Configuration
**Test**: Manual testing / integration test

**Implementation**: Update DI configuration in your Blazor app

```csharp
// In Program.cs or wherever services are configured
services.AddSingleton<IKnowledgeLibrary>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<JsonKnowledgeLibrary>>();
    var knowledgeBasePath = Path.Combine(env.WebRootPath, "knowledge");
    return new JsonKnowledgeLibrary(knowledgeBasePath, logger);
});

services.AddSingleton<IToolRegistry, BuiltInKnowledgeToolRegistry>();
services.AddSingleton<IToolExecutor, KnowledgeLibraryToolExecutor>();
services.AddSingleton<TeachingModePromptBuilder>();
```

**COMMIT**: "feat: configure DI for knowledge library services"

---

#### Task 5.3: Integration with Teaching Mode
**Test**: Integration test to verify prompt includes knowledge section

**Implementation**: Update your teaching mode initialization to include the knowledge library section

**COMMIT**: "feat: integrate knowledge library into teaching mode prompt"

---

### Session 5 Deliverable

✅ **Completed**:
- TeachingModePromptBuilder with comprehensive system prompt
- Dependency injection configured
- Knowledge library integrated into teaching mode
- System prompt dynamically includes all available topics

✅ **Files Created**:
- `TransparentAiAgentCore/Application/Teaching/TeachingModePromptBuilder.cs`
- Updated DI configuration
- Test files

**FINAL COMMIT**: "feat(session-5): complete system prompt integration and DI setup"

---

## Session 6: Seed Content Creation

**Goal**: Create initial high-quality knowledge entries (10-15 entries)

**Estimated Time**: 90-120 minutes

### No Code Changes - Content Creation Only

#### Task 6.1: Create File Structure
```bash
mkdir -p TransparentAiAgentGui/wwwroot/knowledge/entries
```

**COMMIT**: "chore: create knowledge library directory structure"

---

#### Task 6.2: Create JSON Schema
Create `wwwroot/knowledge/_schema.json` (validation tool for developers)

**COMMIT**: "docs: add JSON schema for knowledge entries"

---

#### Task 6.3: Create README for Librarians
Create `wwwroot/knowledge/README.md` with:
- Content guidelines
- How to add/update entries
- Validation checklist
- Gap likelihood decision criteria

**COMMIT**: "docs: add knowledge library maintenance guide"

---

#### Task 6.4: Create Priority Entries (Following Guardrails Approach)

Create these entries (each ~200-400 tokens):

1. **api-key-security.json** (Security - Priority 1)
   - Critical "never-dos" for API key handling
   - Gap likelihood: low

2. **tool-security.json** (Security - Priority 1)
   - Red lines for MCP tool usage
   - Gap likelihood: medium

3. **context-windows.json** (LLM Concepts - Priority 2)
   - Key facts about context limits
   - Gap likelihood: low

4. **mcp-overview.json** (Application-Specific - Priority 3)
   - Essential MCP concepts for this app
   - Gap likelihood: high

5. **transparency-logging.json** (Application-Specific - Priority 3)
   - How transparency features work
   - Gap likelihood: high (app-specific)

6. **teaching-mode.json** (Application-Specific - Priority 3)
   - What is teaching mode and how to use it
   - Gap likelihood: high (app-specific)

7. **environment-variables.json** (Security - Priority 1)
   - Configuration and secrets management
   - Gap likelihood: low

8. **prompt-engineering.json** (LLM Concepts - Priority 2)
   - Effective prompting techniques
   - Gap likelihood: medium

9. **multi-agent-orchestration.json** (Advanced - Priority 5)
   - Agent coordination principles
   - Gap likelihood: high

10. **conversation-management.json** (LLM Concepts - Priority 2)
    - Managing long conversations
    - Gap likelihood: medium

**Each entry should**:
- Follow the guardrails philosophy (critical principles only)
- Target ~200-400 tokens based on complexity
- Include knowledgeGapLikelihood field
- Have clear, actionable key points
- Minimal but illustrative examples
- Critical warnings only

**Example Entry** (api-key-security.json):
```json
{
  "id": "api-key-security",
  "topic": "API Key Security",
  "category": "Security",
  "keywords": ["api", "key", "secrets", "security", "authentication"],
  "summary": "Critical security guardrails for API key handling",
  "knowledgeGapLikelihood": "low",
  "lastUpdated": "2025-11-14",
  "lastChecked": "2025-11-14",
  "content": {
    "overview": "API keys are sensitive credentials. Mishandling leads to security breaches and unauthorized access.",
    "keyPoints": [
      "NEVER commit API keys to version control - this is the #1 mistake",
      "Store in environment variables or secure vaults only",
      "Rotate immediately if exposed (assume compromise)"
    ],
    "examples": [
      {
        "title": "Correct: Environment Variable",
        "code": "var key = Environment.GetEnvironmentVariable(\"ANTHROPIC_API_KEY\");",
        "explanation": "Keeps key out of source code"
      }
    ],
    "warnings": [
      "Never log API keys (even partially masked)",
      "Never share in screenshots or documentation",
      "Revoke immediately if accidentally committed"
    ],
    "relatedTopics": ["environment-variables"],
    "references": [
      {
        "title": "OWASP API Security",
        "url": "https://owasp.org/www-project-api-security/"
      }
    ]
  }
}
```

**COMMIT after each entry**: "content: add {topic-id} knowledge entry"

**FINAL COMMIT**: "content(session-6): complete seed knowledge entries (10 entries)"

---

### Session 6 Deliverable

✅ **Completed**:
- 10 high-quality knowledge entries
- JSON schema for validation
- README for maintainers
- All entries follow guardrails philosophy
- All entries validated against schema

✅ **Files Created**:
- `wwwroot/knowledge/_schema.json`
- `wwwroot/knowledge/README.md`
- `wwwroot/knowledge/entries/*.json` (10 files)

---

## Session 7: End-to-End Testing & Documentation

**Goal**: Verify the complete feature works and update project documentation

**Estimated Time**: 60-75 minutes

### Test Files to Create
- `TransparentAiAgentCore_Tests/Integration/KnowledgeLibraryIntegrationTests.cs`

### TDD Tasks

#### Task 7.1: Integration Tests
**Test class**: `KnowledgeLibraryIntegrationTests.cs`

**Tests**:
```csharp
[TestMethod]
public async Task CompleteFlow_QueryKnowledgeLibrary_ReturnsFormattedContent()
{
    // Arrange: Set up full stack (DI container, knowledge library, tool executor)
    // Act: Execute tool with valid topic
    // Assert: Verify markdown formatted output
}

[TestMethod]
public async Task SystemPrompt_IncludesAllKnowledgeTopics()
{
    // Arrange: Initialize teaching mode
    // Act: Get system prompt
    // Assert: Verify all topics from library are listed
}

[TestMethod]
public async Task TeachingMode_KnowledgeLibraryToolAvailable()
{
    // Arrange: Initialize teaching mode
    // Act: Get available tools
    // Assert: Verify knowledge_library_query is present
}

[TestMethod]
public async Task LoadTest_10ConcurrentQueries_AllSucceed()
{
    // Arrange: Set up library
    // Act: Execute 10 concurrent queries
    // Assert: All return valid results
}
```

**COMMIT**: "test: add end-to-end integration tests for knowledge library"

---

#### Task 7.2: Manual Testing Checklist

Create `docs/testing/knowledge-library-manual-test-plan.md`:

**Test Cases**:
1. ✅ Start teaching mode conversation
2. ✅ Ask about API key security
3. ✅ Verify agent queries knowledge_library_query tool
4. ✅ Verify agent uses library content in teaching
5. ✅ Verify agent includes warnings from library
6. ✅ Ask about related topic
7. ✅ Verify agent suggests related topics appropriately
8. ✅ Ask about non-existent topic
9. ✅ Verify graceful degradation (fallback to built-in knowledge)
10. ✅ Verify all 10 seed entries are accessible

**Execute these tests manually and document results**

**COMMIT**: "docs: add manual testing plan for knowledge library"

---

#### Task 7.3: Update Project Documentation

**Files to Update**:

1. **docs/04-components/tools/knowledge-library-tool.md** (NEW)
   - Component overview
   - Architecture
   - How it works
   - Developer guide

2. **docs/03-concepts/knowledge-library.md** (NEW)
   - Concept overview
   - Guardrails philosophy
   - When to use
   - Content guidelines

3. **docs/05-guides/development/adding-knowledge-entries.md** (NEW)
   - Step-by-step guide
   - Validation checklist
   - Gap likelihood decision criteria
   - Examples

4. **docs/README.md** (UPDATE)
   - Add knowledge library to component list
   - Add to concepts list
   - Add to guides list

5. **CHANGELOG.md** (UPDATE)
   - Document new feature
   - List all changes
   - Migration notes (if any)

**COMMIT**: "docs: add comprehensive documentation for knowledge library"

---

#### Task 7.4: Create Development Guide
Create `docs/05-guides/development/knowledge-library-maintenance.md`:

**Contents**:
- Maintenance responsibilities
- How to update entries
- When to check for staleness
- Gap likelihood guidelines
- Quality checklist

**COMMIT**: "docs: add knowledge library maintenance guide"

---

### Session 7 Deliverable

✅ **Completed**:
- End-to-end integration tests passing
- Manual test plan executed and validated
- Comprehensive project documentation
- Development guides for maintainers
- Feature fully documented and tested

✅ **Files Created/Updated**:
- Integration tests
- Manual test plan
- Component documentation
- Concept documentation
- Development guides
- Updated README and CHANGELOG

**FINAL COMMIT**: "feat(session-7): complete knowledge library feature with tests and docs"

---

## Summary: All Sessions

### Session Checklist

- [ ] **Session 1**: Domain Models (foundation)
  - Domain models with validation
  - Comprehensive unit tests
  - ~60 minutes

- [ ] **Session 2**: IKnowledgeLibrary Interface & JSON
  - Service interface
  - JSON deserialization
  - ~45 minutes

- [ ] **Session 3**: JsonKnowledgeLibrary Implementation
  - File system scanning
  - Caching and lazy loading
  - Error handling
  - ~75 minutes

- [ ] **Session 4**: KnowledgeLibraryTool
  - Tool implementation
  - Tool executor
  - Registry integration
  - ~60 minutes

- [ ] **Session 5**: System Prompt & DI
  - Prompt builder
  - DI configuration
  - Teaching mode integration
  - ~60 minutes

- [ ] **Session 6**: Seed Content
  - File structure
  - 10 knowledge entries
  - Maintenance guides
  - ~120 minutes

- [ ] **Session 7**: Testing & Documentation
  - Integration tests
  - Manual testing
  - Project documentation
  - ~75 minutes

### Total Estimated Time
**7-9 hours** (spread across 7 independent sessions)

---

## Success Criteria

### Functional Requirements
- ✅ LLM can query knowledge library via tool
- ✅ Tool returns accurate, formatted content
- ✅ System prompt lists all available topics
- ✅ Library gracefully handles missing entries
- ✅ Index loads at startup without errors

### Quality Requirements
- ✅ 10+ high-quality knowledge entries created
- ✅ All entries validate against JSON schema
- ✅ Entry content follows guardrails philosophy
- ✅ Code examples are tested and correct

### Teaching Quality
- ✅ Agent teaching measurably improved when using library
- ✅ Critical topics (security) taught consistently
- ✅ Agent includes warnings from library
- ✅ Agent suggests related topics appropriately

### Technical Quality
- ✅ Unit tests cover all components (>80% coverage)
- ✅ Integration tests verify end-to-end flow
- ✅ No performance regressions (index + entries load <100ms)
- ✅ Errors logged but don't crash application

---

## Crash Recovery

If a session is interrupted:

1. **Check last commit**: `git log --oneline -5`
2. **Identify completed session**: Look for "feat(session-N)" commits
3. **Resume from next session**: Start with "Setup Phase" of next session
4. **Verify tests pass**: `dotnet test` before continuing
5. **Continue TDD cycle**: Always write tests first

**Each session is independent** - you can resume from any completed session without loss of progress.

---

## Notes for Claude Code Sessions

When implementing this plan:

1. **Always start with RED phase** - Write failing tests first
2. **Commit frequently** - After each passing test
3. **Investigate unexpected results** - Stop and understand before continuing
4. **Don't skip tests** - Even if you think you know the implementation
5. **Refactor with confidence** - Tests ensure you don't break anything
6. **Session boundaries are safe points** - Commit and push at end of each session

**Remember**: The goal is **confidence in correctness**, not speed. TDD gives you that confidence.

---

**Document Version**: 1.0
**Last Updated**: 2025-11-14
**Ready for Implementation**: YES ✅
