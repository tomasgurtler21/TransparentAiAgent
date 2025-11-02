using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Tests for UIControlToolExecutor following Lean TDD principles.
/// Testing meaningful behavior: tool execution routing, argument parsing, error handling.
/// </summary>
[TestClass]
public class UIControlToolExecutorTests
{
    private Mock<IUIControlService> _mockUIControlService = null!;
    private Mock<ILogger<UIControlToolExecutor>> _mockLogger = null!;
    private UIControlToolExecutor _executor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUIControlService = new Mock<IUIControlService>();
        _mockLogger = new Mock<ILogger<UIControlToolExecutor>>();
        _executor = new UIControlToolExecutor(_mockUIControlService.Object, _mockLogger.Object);
    }

    [TestMethod]
    public void SourceType_ReturnsBuiltInUIControl()
    {
        // Act
        var sourceType = _executor.SourceType;

        // Assert
        Assert.AreEqual(ToolSourceType.BuiltInUIControl, sourceType);
    }

    [TestMethod]
    public async Task ExecuteAsync_ChatFilterTool_CallsUpdateChatFilter()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_chat_filter",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new
        {
            show_system_messages = true,
            show_tool_calls = false
        });

        var expectedState = UIState.DefaultNormalMode() with
        {
            ChatFilter = new ChatFilterState { ShowSystemMessages = true, ShowToolCalls = false }
        };

        _mockUIControlService
            .Setup(s => s.UpdateChatFilter(null, null, true, false, null, null))
            .Returns(Result<UIState>.Ok(expectedState));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockUIControlService.Verify(s => s.UpdateChatFilter(
            null, null, true, false, null, null), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_FilterVisibilityTool_CallsUpdateFilterControlVisibility()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_filter_visibility",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new { visible = true });

        _mockUIControlService
            .Setup(s => s.UpdateFilterControlVisibility(true))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockUIControlService.Verify(s => s.UpdateFilterControlVisibility(true), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_GetStateTool_ReturnsCurrentState()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_get_state",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new { component = "all" });

        var currentState = UIState.DefaultTeachingMode();
        _mockUIControlService
            .Setup(s => s.GetCurrentState())
            .Returns(currentState);

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Content.Contains("CurrentMode"));
        _mockUIControlService.Verify(s => s.GetCurrentState(), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_TransparencyViewerTool_CallsUpdateTransparencyViewer()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_transparency_viewer",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new
        {
            visible = true,
            show_timestamps = false
        });

        _mockUIControlService
            .Setup(s => s.UpdateTransparencyViewer(true, null, false))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockUIControlService.Verify(s => s.UpdateTransparencyViewer(
            true, null, false), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ToolsPanelTool_CallsUpdateToolsPanel()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_tools_panel",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new
        {
            visible = true,
            highlight_tool = "test_tool"
        });

        _mockUIControlService
            .Setup(s => s.UpdateToolsPanel(true, null, "test_tool"))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockUIControlService.Verify(s => s.UpdateToolsPanel(
            true, null, "test_tool"), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ContextIndicatorsTool_CallsUpdateContextIndicators()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_context_indicators",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new
        {
            visible = true,
            highlighted = true
        });

        _mockUIControlService
            .Setup(s => s.UpdateContextIndicators(true, true))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockUIControlService.Verify(s => s.UpdateContextIndicators(
            true, true), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ConfigurationTool_CallsUpdateConfigurationPage()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_configuration",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new
        {
            navigate = true,
            highlight_section = "system-prompt"
        });

        _mockUIControlService
            .Setup(s => s.UpdateConfigurationPage(true, "system-prompt"))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockUIControlService.Verify(s => s.UpdateConfigurationPage(
            true, "system-prompt"), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnknownTool_ReturnsFailure()
    {
        // Arrange
        var tool = new UIControlTool(
            "unknown_tool",
            "Test tool",
            "{}");

        var arguments = "{}";

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage?.Contains("Unknown tool") ?? false);
    }

    [TestMethod]
    public async Task ExecuteAsync_ServiceFailure_ReturnsFailure()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_chat_filter",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new { show_system_messages = true });

        _mockUIControlService
            .Setup(s => s.UpdateChatFilter(null, null, true, null, null, null))
            .Returns(Result<UIState>.Fail("Service error occurred"));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage?.Contains("Service error occurred") ?? false);
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_chat_filter",
            "Test tool",
            "{}");

        var arguments = "{ invalid json }";

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsync_SuccessfulExecution_ReturnsSerializedState()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_filter_visibility",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new { visible = true });

        var expectedState = UIState.DefaultNormalMode() with
        {
            ChatFilter = new ChatFilterState { FilterControlsVisible = true }
        };

        _mockUIControlService
            .Setup(s => s.UpdateFilterControlVisibility(true))
            .Returns(Result<UIState>.Ok(expectedState));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Content.Contains("FilterControlsVisible"));
        Assert.IsTrue(result.Content.Contains("true"));
    }

    [TestMethod]
    public async Task ExecuteAsync_RecordsExecutionTime()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_filter_visibility",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new { visible = true });

        _mockUIControlService
            .Setup(s => s.UpdateFilterControlVisibility(true))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.ExecutionTime >= TimeSpan.Zero);
        Assert.IsTrue(result.ExecutionTime < TimeSpan.FromSeconds(1),
            "Execution should be fast for in-memory operations");
    }

    [TestMethod]
    public async Task ExecuteAsync_GetStateTool_ChatFilterComponent_ReturnsFilteredState()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_get_state",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new { component = "chat_filter" });

        var currentState = UIState.DefaultNormalMode() with
        {
            ChatFilter = new ChatFilterState { ShowSystemMessages = true }
        };

        _mockUIControlService
            .Setup(s => s.GetCurrentState())
            .Returns(currentState);

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Content.Contains("ChatFilter"));
    }

    [TestMethod]
    public async Task ExecuteAsync_TransparencyViewerWithFilters_PassesFiltersCorrectly()
    {
        // Arrange
        var tool = new UIControlTool(
            "ui_control_transparency_viewer",
            "Test tool",
            "{}");

        var arguments = JsonSerializer.Serialize(new
        {
            visible = true,
            event_type_filters = new[] { "LLMRequest", "ToolCallStarted" }
        });

        _mockUIControlService
            .Setup(s => s.UpdateTransparencyViewer(
                true,
                It.Is<List<string>>(list => list.Count == 2 && list.Contains("LLMRequest")),
                null))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockUIControlService.Verify(s => s.UpdateTransparencyViewer(
            true,
            It.Is<List<string>>(list => list.Count == 2),
            null), Times.Once);
    }
}
