using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Tests for BuiltInUIControlToolRegistry following Lean TDD principles.
/// Testing meaningful behavior: tool registration, retrieval, and verification.
/// </summary>
[TestClass]
public class BuiltInUIControlToolRegistryTests
{
    [TestMethod]
    public void GetAllTools_ReturnsSevenTools()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tools = registry.GetAllTools();

        // Assert
        Assert.AreEqual(7, tools.Count, "Registry should contain exactly 7 UI control tools");
    }

    [TestMethod]
    public void GetTool_ChatFilterTool_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("ui_control_chat_filter");

        // Assert
        Assert.IsNotNull(tool, "ui_control_chat_filter should be registered");
        Assert.AreEqual("ui_control_chat_filter", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_FilterVisibilityTool_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("ui_control_filter_visibility");

        // Assert
        Assert.IsNotNull(tool, "ui_control_filter_visibility should be registered");
        Assert.AreEqual("ui_control_filter_visibility", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_GetStateTool_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("ui_get_state");

        // Assert
        Assert.IsNotNull(tool, "ui_get_state should be registered");
        Assert.AreEqual("ui_get_state", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_TransparencyViewerTool_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("ui_control_transparency_viewer");

        // Assert
        Assert.IsNotNull(tool, "ui_control_transparency_viewer should be registered");
        Assert.AreEqual("ui_control_transparency_viewer", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_ToolsPanelTool_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("ui_control_tools_panel");

        // Assert
        Assert.IsNotNull(tool, "ui_control_tools_panel should be registered");
        Assert.AreEqual("ui_control_tools_panel", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_ContextIndicatorsTool_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("ui_control_context_indicators");

        // Assert
        Assert.IsNotNull(tool, "ui_control_context_indicators should be registered");
        Assert.AreEqual("ui_control_context_indicators", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_ConfigurationTool_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("ui_control_configuration");

        // Assert
        Assert.IsNotNull(tool, "ui_control_configuration should be registered");
        Assert.AreEqual("ui_control_configuration", tool.Name);
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, tool.SourceType);
    }

    [TestMethod]
    public void GetTool_NonExistentTool_ReturnsNull()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("non_existent_tool");

        // Assert
        Assert.IsNull(tool, "Non-existent tool should return null");
    }

    [TestMethod]
    public void GetTool_NullToolName_ReturnsNull()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool(null!);

        // Assert
        Assert.IsNull(tool, "Null tool name should return null");
    }

    [TestMethod]
    public void GetTool_EmptyToolName_ReturnsNull()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool(string.Empty);

        // Assert
        Assert.IsNull(tool, "Empty tool name should return null");
    }

    [TestMethod]
    public void GetTool_CaseInsensitiveMatch_ReturnsCorrectTool()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tool = registry.GetTool("UI_CONTROL_CHAT_FILTER");

        // Assert
        Assert.IsNotNull(tool, "Tool lookup should be case-insensitive");
        Assert.AreEqual("ui_control_chat_filter", tool.Name);
    }

    [TestMethod]
    public void AllTools_HaveBuiltInUIControlSourceType()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tools = registry.GetAllTools();

        // Assert
        Assert.IsTrue(tools.All(t => t.SourceType == ToolSourceType.BuiltInUIControl),
            "All tools should have BuiltInUIControl source type");
    }

    [TestMethod]
    public void AllTools_HaveNonEmptyDescription()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tools = registry.GetAllTools();

        // Assert
        Assert.IsTrue(tools.All(t => !string.IsNullOrWhiteSpace(t.Description)),
            "All tools should have a non-empty description");
    }

    [TestMethod]
    public void AllTools_HaveValidParametersSchema()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tools = registry.GetAllTools();

        // Assert
        Assert.IsTrue(tools.All(t => !string.IsNullOrWhiteSpace(t.ParametersSchema)),
            "All tools should have a valid parameters schema");
    }

    [TestMethod]
    public void HasTool_ExistingTool_ReturnsTrue()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act & Assert
        Assert.IsTrue(registry.HasTool("ui_control_chat_filter"));
        Assert.IsTrue(registry.HasTool("ui_control_filter_visibility"));
        Assert.IsTrue(registry.HasTool("ui_get_state"));
        Assert.IsTrue(registry.HasTool("ui_control_transparency_viewer"));
        Assert.IsTrue(registry.HasTool("ui_control_tools_panel"));
        Assert.IsTrue(registry.HasTool("ui_control_context_indicators"));
        Assert.IsTrue(registry.HasTool("ui_control_configuration"));
    }

    [TestMethod]
    public void HasTool_NonExistentTool_ReturnsFalse()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var result = registry.HasTool("non_existent_tool");

        // Assert
        Assert.IsFalse(result, "Non-existent tool should return false");
    }

    [TestMethod]
    public async Task RefreshAsync_DoesNothing_ToolsAreStaticallyDefined()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();
        var toolsBeforeRefresh = registry.GetAllTools();

        // Act
        await registry.RefreshAsync();
        var toolsAfterRefresh = registry.GetAllTools();

        // Assert
        Assert.AreEqual(toolsBeforeRefresh.Count, toolsAfterRefresh.Count,
            "RefreshAsync should not change tools count for static registry");
    }

    [TestMethod]
    public void GetAllTools_ReturnsSameToolsOnMultipleCalls()
    {
        // Arrange
        var registry = new BuiltInUIControlToolRegistry();

        // Act
        var tools1 = registry.GetAllTools();
        var tools2 = registry.GetAllTools();

        // Assert
        Assert.AreEqual(tools1.Count, tools2.Count, "GetAllTools should return consistent results");
        CollectionAssert.AreEqual(
            tools1.Select(t => t.Name).ToList(),
            tools2.Select(t => t.Name).ToList(),
            "Tool names should be identical across calls");
    }
}
