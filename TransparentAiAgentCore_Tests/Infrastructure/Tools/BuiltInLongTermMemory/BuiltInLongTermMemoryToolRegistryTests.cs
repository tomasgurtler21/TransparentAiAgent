using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInLongTermMemory;

/// <summary>
/// Tests for BuiltInLongTermMemoryToolRegistry following Lean TDD principles.
/// Testing meaningful behavior: tool discovery, tool lookup by name.
/// </summary>
[TestClass]
public class BuiltInLongTermMemoryToolRegistryTests
{
    [TestMethod]
    public void GetAllTools_ReturnsReadAndUpdateTools()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        var tools = registry.GetAllTools();

        // Assert
        Assert.AreEqual(2, tools.Count);
        Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_read"),
            "Registry should contain long_term_memory_read tool");
        Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_update"),
            "Registry should contain long_term_memory_update tool");
    }

    [TestMethod]
    public void GetAllTools_AllToolsHaveCorrectSourceType()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        var tools = registry.GetAllTools();

        // Assert
        foreach (var tool in tools)
        {
            Assert.AreEqual(ToolSourceType.BuiltInLongTermMemory, tool.SourceType,
                $"Tool {tool.Name} should have BuiltInLongTermMemory source type");
        }
    }

    [TestMethod]
    public void GetTool_ValidName_ReturnsTool()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        var readTool = registry.GetTool("long_term_memory_read");
        var updateTool = registry.GetTool("long_term_memory_update");

        // Assert
        Assert.IsNotNull(readTool, "Should find long_term_memory_read tool");
        Assert.AreEqual("long_term_memory_read", readTool.Name);

        Assert.IsNotNull(updateTool, "Should find long_term_memory_update tool");
        Assert.AreEqual("long_term_memory_update", updateTool.Name);
    }

    [TestMethod]
    public void GetTool_InvalidName_ReturnsNull()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        var tool = registry.GetTool("nonexistent_tool");

        // Assert
        Assert.IsNull(tool, "Should return null for nonexistent tool");
    }

    [TestMethod]
    public void GetTool_CaseInsensitive_ReturnsTool()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        var tool = registry.GetTool("LONG_TERM_MEMORY_READ");

        // Assert
        Assert.IsNotNull(tool, "Registry should be case-insensitive");
        Assert.AreEqual("long_term_memory_read", tool.Name);
    }

    [TestMethod]
    public void HasTool_ExistingTool_ReturnsTrue()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        var hasReadTool = registry.HasTool("long_term_memory_read");
        var hasUpdateTool = registry.HasTool("long_term_memory_update");

        // Assert
        Assert.IsTrue(hasReadTool);
        Assert.IsTrue(hasUpdateTool);
    }

    [TestMethod]
    public void HasTool_NonexistentTool_ReturnsFalse()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        var hasTool = registry.HasTool("nonexistent_tool");

        // Assert
        Assert.IsFalse(hasTool);
    }

    [TestMethod]
    public async Task RefreshAsync_CompletesSuccessfully()
    {
        // Arrange
        var registry = new BuiltInLongTermMemoryToolRegistry();

        // Act
        await registry.RefreshAsync();

        // Assert
        // Built-in tools are static, refresh should complete without error
        var tools = registry.GetAllTools();
        Assert.AreEqual(2, tools.Count, "Refresh should not change tool count");
    }
}
