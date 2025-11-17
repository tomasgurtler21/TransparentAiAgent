using System.Text.Json;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInKnowledge;

[TestClass]
public class KnowledgeLibraryToolTests
{
    private static List<KnowledgeEntrySummary> CreateTestTopics()
    {
        return new List<KnowledgeEntrySummary>
        {
            new KnowledgeEntrySummary(
                "api-key-security",
                "API Key Security",
                "Security",
                "Best practices for API key handling",
                "HIGH",
                "2024-01-01",
                "2024-01-01",
                new List<string> { "api", "security" }),
            new KnowledgeEntrySummary(
                "llm-basics",
                "LLM Basics",
                "AI",
                "Introduction to LLMs",
                "MEDIUM",
                "2024-01-01",
                "2024-01-01",
                new List<string> { "llm", "ai" })
        };
    }

    [TestMethod]
    public void Constructor_CreatesInstanceWithCorrectProperties()
    {
        // Arrange
        var topics = CreateTestTopics();

        // Act
        var tool = new KnowledgeLibraryTool(topics);

        // Assert
        Assert.IsNotNull(tool);
        Assert.AreEqual("knowledge_library_query", tool.Name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description));
        Assert.IsTrue(tool.Description.Contains("guardrail", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(ToolSourceType.BuiltInKnowledge, tool.SourceType);
    }

    [TestMethod]
    public void Constructor_ThrowsArgumentNullException_WhenTopicsIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new KnowledgeLibraryTool(null!));
    }

    [TestMethod]
    public void Description_IncludesAvailableTopics()
    {
        // Arrange
        var topics = CreateTestTopics();

        // Act
        var tool = new KnowledgeLibraryTool(topics);

        // Assert
        Assert.IsTrue(tool.Description.Contains("Available topics:"));
        Assert.IsTrue(tool.Description.Contains("api-key-security"));
        Assert.IsTrue(tool.Description.Contains("llm-basics"));
        Assert.IsTrue(tool.Description.Contains("Security:"));
        Assert.IsTrue(tool.Description.Contains("AI:"));
    }

    [TestMethod]
    public void Description_GroupsTopicsByCategory()
    {
        // Arrange
        var topics = CreateTestTopics();

        // Act
        var tool = new KnowledgeLibraryTool(topics);

        // Assert
        var description = tool.Description;
        var securityIndex = description.IndexOf("Security:", StringComparison.Ordinal);
        var aiIndex = description.IndexOf("AI:", StringComparison.Ordinal);
        var apiKeyIndex = description.IndexOf("api-key-security", StringComparison.Ordinal);
        var llmIndex = description.IndexOf("llm-basics", StringComparison.Ordinal);

        // Security category should come before api-key-security topic
        Assert.IsTrue(securityIndex < apiKeyIndex);
        // AI category should come before llm-basics topic
        Assert.IsTrue(aiIndex < llmIndex);
    }

    [TestMethod]
    public void ParametersSchema_IsValidJson()
    {
        // Arrange
        var topics = CreateTestTopics();
        var tool = new KnowledgeLibraryTool(topics);

        // Act & Assert - Should not throw
        var doc = JsonDocument.Parse(tool.ParametersSchema);
        Assert.IsNotNull(doc);

        // Verify it's an object schema with required "topic" property
        var root = doc.RootElement;
        Assert.AreEqual(JsonValueKind.Object, root.ValueKind);

        Assert.IsTrue(root.TryGetProperty("type", out var typeProperty));
        Assert.AreEqual("object", typeProperty.GetString());

        Assert.IsTrue(root.TryGetProperty("properties", out var properties));
        Assert.IsTrue(properties.TryGetProperty("topic", out _));

        Assert.IsTrue(root.TryGetProperty("required", out var required));
        Assert.AreEqual(JsonValueKind.Array, required.ValueKind);
    }

    [TestMethod]
    public void ParametersSchema_IncludesAvailableTopics()
    {
        // Arrange
        var topics = CreateTestTopics();

        // Act
        var tool = new KnowledgeLibraryTool(topics);

        // Assert
        Assert.IsTrue(tool.ParametersSchema.Contains("'api-key-security'"));
        Assert.IsTrue(tool.ParametersSchema.Contains("'llm-basics'"));
    }

    [TestMethod]
    public void Metadata_ContainsSourceTypeAndCategory()
    {
        // Arrange
        var topics = CreateTestTopics();
        var tool = new KnowledgeLibraryTool(topics);

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsNotNull(metadata);
        Assert.IsTrue(metadata.ContainsKey("SourceType"));
        Assert.AreEqual("BuiltInKnowledge", metadata["SourceType"]);
        Assert.IsTrue(metadata.ContainsKey("Category"));
    }
}
