namespace TransparentAiAgentCore_Tests.Infrastructure.Knowledge;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Infrastructure.Knowledge;

/// <summary>
/// Tests for JSON deserialization of knowledge entry models using DTOs.
/// Verifies that JSON can be deserialized to DTOs and converted to domain models with validation.
/// </summary>
[TestClass]
public class JsonKnowledgeEntryDeserializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    [TestMethod]
    public void Deserialize_ValidCompleteEntry_CreatesKnowledgeEntry()
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
            "overview": "This is an overview",
            "keyPoints": ["Point 1", "Point 2"],
            "examples": [
              {
                "title": "Example 1",
                "explanation": "Explanation text",
                "code": "var x = 1;"
              }
            ],
            "warnings": ["Warning 1"],
            "bestPractices": ["Practice 1"],
            "relatedTopics": ["related-topic"],
            "references": [
              {
                "title": "Reference 1",
                "url": "https://example.com"
              }
            ]
          }
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        var entry = dto.ToDomain(); // Validation happens here

        // Assert
        Assert.IsNotNull(entry);
        Assert.AreEqual("test-topic", entry.Id);
        Assert.AreEqual("Test Topic", entry.Topic);
        Assert.AreEqual("Testing", entry.Category);
        Assert.AreEqual("A test summary", entry.Summary);
        Assert.AreEqual("low", entry.KnowledgeGapLikelihood);
        Assert.AreEqual("2025-11-14", entry.LastUpdated);
        Assert.AreEqual("2025-11-14", entry.LastChecked);
        Assert.AreEqual(2, entry.Keywords.Count);
        Assert.AreEqual("test", entry.Keywords[0]);

        Assert.IsNotNull(entry.Content);
        Assert.AreEqual("This is an overview", entry.Content.Overview);
        Assert.AreEqual(2, entry.Content.KeyPoints.Count);
        Assert.AreEqual(1, entry.Content.Examples.Count);
        Assert.AreEqual("Example 1", entry.Content.Examples[0].Title);
        Assert.AreEqual(1, entry.Content.Warnings.Count);
        Assert.AreEqual(1, entry.Content.BestPractices.Count);
        Assert.AreEqual(1, entry.Content.RelatedTopics.Count);
        Assert.AreEqual(1, entry.Content.References.Count);
        Assert.AreEqual("Reference 1", entry.Content.References[0].Title);
        Assert.AreEqual("https://example.com", entry.Content.References[0].Url);
    }

    [TestMethod]
    public void Deserialize_MinimalEntry_CreatesKnowledgeEntry()
    {
        // Arrange - Only required fields
        var json = """
        {
          "id": "minimal-topic",
          "topic": "Minimal Topic",
          "category": "Test",
          "summary": "Minimal summary",
          "knowledgeGapLikelihood": "medium",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "Minimal overview"
          }
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        var entry = dto.ToDomain();

        // Assert
        Assert.IsNotNull(entry);
        Assert.AreEqual("minimal-topic", entry.Id);
        Assert.AreEqual("Minimal Topic", entry.Topic);
        Assert.AreEqual(0, entry.Keywords.Count);
        Assert.AreEqual(0, entry.Content.KeyPoints.Count);
        Assert.AreEqual(0, entry.Content.Examples.Count);
    }

    [TestMethod]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        Assert.ThrowsException<JsonException>(() =>
            JsonSerializer.Deserialize<KnowledgeEntryDto>(json, JsonOptions));
    }

    [TestMethod]
    public void Deserialize_MissingRequiredField_ThrowsException()
    {
        // Arrange - Missing 'topic' field
        var json = """
        {
          "id": "test-topic",
          "category": "Testing",
          "summary": "A test summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "This is an overview"
          }
        }
        """;

        // Act & Assert - DTO deserializes, but ToDomain() should throw
        var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        Assert.ThrowsException<ArgumentException>(() => dto.ToDomain());
    }

    [TestMethod]
    public void Deserialize_InvalidIdFormat_ThrowsArgumentException()
    {
        // Arrange - ID not in kebab-case
        var json = """
        {
          "id": "InvalidID",
          "topic": "Test Topic",
          "category": "Testing",
          "summary": "A test summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "This is an overview"
          }
        }
        """;

        // Act & Assert
        var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        Assert.ThrowsException<ArgumentException>(() => dto.ToDomain());
    }

    [TestMethod]
    public void Deserialize_InvalidGapLikelihood_ThrowsArgumentException()
    {
        // Arrange - Invalid gap likelihood value
        var json = """
        {
          "id": "test-topic",
          "topic": "Test Topic",
          "category": "Testing",
          "summary": "A test summary",
          "knowledgeGapLikelihood": "invalid",
          "lastUpdated": "2025-11-14",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "This is an overview"
          }
        }
        """;

        // Act & Assert
        var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        Assert.ThrowsException<ArgumentException>(() => dto.ToDomain());
    }

    [TestMethod]
    public void Deserialize_InvalidDateFormat_ThrowsArgumentException()
    {
        // Arrange - Invalid date format
        var json = """
        {
          "id": "test-topic",
          "topic": "Test Topic",
          "category": "Testing",
          "summary": "A test summary",
          "knowledgeGapLikelihood": "low",
          "lastUpdated": "11/14/2025",
          "lastChecked": "2025-11-14",
          "content": {
            "overview": "This is an overview"
          }
        }
        """;

        // Act & Assert
        var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        Assert.ThrowsException<ArgumentException>(() => dto.ToDomain());
    }

    [TestMethod]
    public void Deserialize_KnowledgeExample_AllFields()
    {
        // Arrange
        var json = """
        {
          "title": "Example Title",
          "explanation": "Example explanation",
          "code": "var x = 1;"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<KnowledgeExampleDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        var example = dto.ToDomain();

        // Assert
        Assert.IsNotNull(example);
        Assert.AreEqual("Example Title", example.Title);
        Assert.AreEqual("Example explanation", example.Explanation);
        Assert.AreEqual("var x = 1;", example.Code);
    }

    [TestMethod]
    public void Deserialize_KnowledgeExample_NoCode()
    {
        // Arrange
        var json = """
        {
          "title": "Example Title",
          "explanation": "Example explanation"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<KnowledgeExampleDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        var example = dto.ToDomain();

        // Assert
        Assert.IsNotNull(example);
        Assert.AreEqual("Example Title", example.Title);
        Assert.AreEqual("Example explanation", example.Explanation);
        Assert.AreEqual(string.Empty, example.Code);
    }

    [TestMethod]
    public void Deserialize_KnowledgeReference_WithUrl()
    {
        // Arrange
        var json = """
        {
          "title": "Reference Title",
          "url": "https://example.com"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<KnowledgeReferenceDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        var reference = dto.ToDomain();

        // Assert
        Assert.IsNotNull(reference);
        Assert.AreEqual("Reference Title", reference.Title);
        Assert.AreEqual("https://example.com", reference.Url);
    }

    [TestMethod]
    public void Deserialize_KnowledgeReference_NoUrl()
    {
        // Arrange
        var json = """
        {
          "title": "Reference Title"
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<KnowledgeReferenceDto>(json, JsonOptions);
        Assert.IsNotNull(dto);
        var reference = dto.ToDomain();

        // Assert
        Assert.IsNotNull(reference);
        Assert.AreEqual("Reference Title", reference.Title);
        Assert.IsNull(reference.Url);
    }
}
