using System.Text.Json;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInLongTermMemory;

[TestClass]
public class LongTermMemoryToolsTests
{
    [TestMethod]
    public void LongTermMemoryReadTool_HasCorrectMetadata()
    {
        // Arrange & Act
        var tool = new LongTermMemoryReadTool();

        // Assert
        Assert.AreEqual("long_term_memory_read", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInLongTermMemory, tool.SourceType);
        Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description));
        Assert.IsTrue(tool.Description.Contains("memory", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LongTermMemoryReadTool_ParametersSchema_IsValidJson()
    {
        // Arrange
        var tool = new LongTermMemoryReadTool();

        // Act & Assert - Should not throw
        var doc = JsonDocument.Parse(tool.ParametersSchema);
        Assert.IsNotNull(doc);

        var root = doc.RootElement;
        Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
        Assert.IsTrue(root.TryGetProperty("type", out var typeProperty));
        Assert.AreEqual("object", typeProperty.GetString());
    }

    [TestMethod]
    public void LongTermMemoryUpdateTool_HasCorrectMetadata()
    {
        // Arrange & Act
        var tool = new LongTermMemoryUpdateTool();

        // Assert
        Assert.AreEqual("long_term_memory_update", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInLongTermMemory, tool.SourceType);
        Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description));
        Assert.IsTrue(tool.Description.Contains("GUARDRAILS", StringComparison.Ordinal),
            "Description must contain 'GUARDRAILS' to ensure privacy guidelines are visible to LLM");
    }

    [TestMethod]
    public void LongTermMemoryUpdateTool_ParametersSchema_IsValidJson()
    {
        // Arrange
        var tool = new LongTermMemoryUpdateTool();

        // Act & Assert - Should not throw
        var doc = JsonDocument.Parse(tool.ParametersSchema);
        Assert.IsNotNull(doc);

        var root = doc.RootElement;
        Assert.AreEqual(JsonValueKind.Object, root.ValueKind);

        // Verify required properties
        Assert.IsTrue(root.TryGetProperty("properties", out var properties));
        Assert.IsTrue(properties.TryGetProperty("content", out _),
            "Schema must have 'content' parameter");
        Assert.IsTrue(properties.TryGetProperty("reason", out _),
            "Schema must have 'reason' parameter");

        // Verify required array
        Assert.IsTrue(root.TryGetProperty("required", out var required));
        Assert.AreEqual(JsonValueKind.Array, required.ValueKind);
    }

    [TestMethod]
    public void LongTermMemoryReadTool_Metadata_ContainsSourceType()
    {
        // Arrange
        var tool = new LongTermMemoryReadTool();

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsNotNull(metadata);
        Assert.IsTrue(metadata.ContainsKey("SourceType"));
        Assert.AreEqual("BuiltInLongTermMemory", metadata["SourceType"]);
    }

    [TestMethod]
    public void LongTermMemoryUpdateTool_Metadata_ContainsSourceType()
    {
        // Arrange
        var tool = new LongTermMemoryUpdateTool();

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsNotNull(metadata);
        Assert.IsTrue(metadata.ContainsKey("SourceType"));
        Assert.AreEqual("BuiltInLongTermMemory", metadata["SourceType"]);
    }
}
