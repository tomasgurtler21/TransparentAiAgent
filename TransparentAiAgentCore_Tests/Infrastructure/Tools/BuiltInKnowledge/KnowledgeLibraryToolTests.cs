using System.Text.Json;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInKnowledge;

[TestClass]
public class KnowledgeLibraryToolTests
{
    [TestMethod]
    public void Constructor_CreatesInstanceWithCorrectProperties()
    {
        // Arrange & Act
        var tool = new KnowledgeLibraryTool();

        // Assert
        Assert.IsNotNull(tool);
        Assert.AreEqual("knowledge_library_query", tool.Name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description));
        Assert.IsTrue(tool.Description.Contains("guardrail", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(ToolSourceType.BuiltInKnowledge, tool.SourceType);
    }

    [TestMethod]
    public void ParametersSchema_IsValidJson()
    {
        // Arrange
        var tool = new KnowledgeLibraryTool();

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
    public void Metadata_ContainsSourceTypeAndCategory()
    {
        // Arrange
        var tool = new KnowledgeLibraryTool();

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsNotNull(metadata);
        Assert.IsTrue(metadata.ContainsKey("SourceType"));
        Assert.AreEqual("BuiltInKnowledge", metadata["SourceType"]);
        Assert.IsTrue(metadata.ContainsKey("Category"));
    }
}
