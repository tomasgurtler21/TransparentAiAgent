using Moq;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentGui.Services;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentGui.Tests.Services;

/// <summary>
/// Tests for UIControlService following Lean TDD principles.
/// Tests meaningful behavior: state updates, event firing, error handling.
/// </summary>
[TestClass]
public class UIControlServiceTests
{
    private Mock<ITransparencyService> _mockTransparency = null!;
    private Mock<ILogger<UIControlService>> _mockLogger = null!;
    private UIControlService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockTransparency = new Mock<ITransparencyService>();
        _mockLogger = new Mock<ILogger<UIControlService>>();
        _service = new UIControlService(_mockTransparency.Object, _mockLogger.Object);
    }

    [TestMethod]
    public void UpdateChatFilter_UpdatesStateAndFiresEvent()
    {
        // Arrange
        UIState? capturedState = null;
        _service.UIStateChanged += (sender, state) => capturedState = state;

        // Act
        var result = _service.UpdateChatFilter(showSystemMessages: true, showToolCalls: false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsTrue(capturedState.ChatFilter.ShowSystemMessages);
        Assert.IsFalse(capturedState.ChatFilter.ShowToolCalls);
    }

    [TestMethod]
    public void UpdateChatFilter_OnlyUpdatesSpecifiedParameters()
    {
        // Arrange
        UIState? capturedState = null;
        _service.UIStateChanged += (sender, state) => capturedState = state;

        // Act
        var result = _service.UpdateChatFilter(showSystemMessages: false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsFalse(capturedState.ChatFilter.ShowSystemMessages);
        // Other parameters should remain at default (true for Normal Mode)
        Assert.IsTrue(capturedState.ChatFilter.ShowToolCalls);
        Assert.IsTrue(capturedState.ChatFilter.ShowToolResults);
    }

    [TestMethod]
    public void UpdateChatFilter_LogsToTransparency()
    {
        // Act
        _service.UpdateChatFilter(showSystemMessages: true);

        // Assert
        _mockTransparency.Verify(t => t.LogEvent(
            It.Is<TransparencyEvent>(evt => evt.EventType == TransparencyEventType.UIControlAction)),
            Times.Once);
    }

    [TestMethod]
    public void GetCurrentState_ReturnsCurrentState()
    {
        // Arrange
        _service.UpdateChatFilter(showSystemMessages: false);

        // Act
        var state = _service.GetCurrentState();

        // Assert
        Assert.IsFalse(state.ChatFilter.ShowSystemMessages);
    }

    [TestMethod]
    public void UpdateFilterControlVisibility_UpdatesVisibility()
    {
        // Arrange
        UIState? capturedState = null;
        _service.UIStateChanged += (sender, state) => capturedState = state;

        // Act
        var result = _service.UpdateFilterControlVisibility(false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsFalse(capturedState.ChatFilter.FilterControlsVisible);
    }

    [TestMethod]
    public void UpdateTransparencyViewer_UpdatesViewerState()
    {
        // Arrange
        UIState? capturedState = null;
        _service.UIStateChanged += (sender, state) => capturedState = state;
        var filters = new List<string> { "LLMRequest", "ToolCallStarted" };

        // Act
        var result = _service.UpdateTransparencyViewer(visible: false, eventTypeFilters: filters);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsFalse(capturedState.TransparencyViewer.Visible);
        CollectionAssert.AreEqual(filters, capturedState.TransparencyViewer.EventTypeFilters);
    }

    [TestMethod]
    public void UpdateToolsPanel_UpdatesToolsState()
    {
        // Arrange
        UIState? capturedState = null;
        _service.UIStateChanged += (sender, state) => capturedState = state;
        var expandedTools = new List<string> { "ui_control_chat_filter", "web_search" };

        // Act
        var result = _service.UpdateToolsPanel(
            visible: true,
            expandedTools: expandedTools,
            highlightedTool: "ui_control_chat_filter");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsTrue(capturedState.ToolsPanel.Visible);
        CollectionAssert.AreEqual(expandedTools, capturedState.ToolsPanel.ExpandedTools);
        Assert.AreEqual("ui_control_chat_filter", capturedState.ToolsPanel.HighlightedTool);
    }

    [TestMethod]
    public void UpdateContextIndicators_UpdatesIndicatorsState()
    {
        // Arrange
        UIState? capturedState = null;
        _service.UIStateChanged += (sender, state) => capturedState = state;

        // Act
        var result = _service.UpdateContextIndicators(visible: true, highlighted: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsTrue(capturedState.ContextIndicators.Visible);
        Assert.IsTrue(capturedState.ContextIndicators.Highlighted);
    }

    [TestMethod]
    public void UpdateConfigurationPage_UpdatesConfigState()
    {
        // Arrange
        UIState? capturedState = null;
        _service.UIStateChanged += (sender, state) => capturedState = state;

        // Act
        var result = _service.UpdateConfigurationPage(
            visible: true,
            highlightSection: "system-prompt");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsTrue(capturedState.ConfigurationPage.Visible);
        Assert.AreEqual("system-prompt", capturedState.ConfigurationPage.HighlightedSection);
    }

    [TestMethod]
    public void SwitchMode_ToTeaching_ResetsToTeachingDefaults()
    {
        // Act
        var result = _service.SwitchMode(AppMode.Teaching);

        // Assert
        Assert.IsTrue(result.Success);
        var state = _service.GetCurrentState();
        Assert.AreEqual(AppMode.Teaching, state.CurrentMode);
        Assert.IsFalse(state.ChatFilter.FilterControlsVisible);
        Assert.IsFalse(state.ChatFilter.ShowSystemMessages);
        Assert.IsFalse(state.ChatFilter.ShowToolCalls);
    }

    [TestMethod]
    public void SwitchMode_ToNormal_ResetsToNormalDefaults()
    {
        // Arrange - Start in Teaching Mode
        _service.SwitchMode(AppMode.Teaching);

        // Act
        var result = _service.SwitchMode(AppMode.Normal);

        // Assert
        Assert.IsTrue(result.Success);
        var state = _service.GetCurrentState();
        Assert.AreEqual(AppMode.Normal, state.CurrentMode);
        Assert.IsTrue(state.ChatFilter.FilterControlsVisible);
        Assert.IsTrue(state.ChatFilter.ShowSystemMessages);
        Assert.IsTrue(state.ChatFilter.ShowToolCalls);
    }

    [TestMethod]
    public void ResetToDefaults_ResetsBasedOnCurrentMode()
    {
        // Arrange - Start in Normal Mode and make changes
        _service.UpdateChatFilter(showSystemMessages: false);

        // Act
        var result = _service.ResetToDefaults();

        // Assert
        Assert.IsTrue(result.Success);
        var state = _service.GetCurrentState();
        // Should reset to Normal Mode defaults
        Assert.IsTrue(state.ChatFilter.ShowSystemMessages);
        Assert.IsTrue(state.ChatFilter.ShowToolCalls);
    }

    [TestMethod]
    public void MultipleUpdates_MaintainStateCorrectly()
    {
        // Arrange
        int eventCount = 0;
        _service.UIStateChanged += (sender, state) => eventCount++;

        // Act
        _service.UpdateChatFilter(showSystemMessages: false);
        _service.UpdateFilterControlVisibility(false);
        _service.UpdateContextIndicators(highlighted: true);

        // Assert
        Assert.AreEqual(3, eventCount);
        var finalState = _service.GetCurrentState();
        Assert.IsFalse(finalState.ChatFilter.ShowSystemMessages);
        Assert.IsFalse(finalState.ChatFilter.FilterControlsVisible);
        Assert.IsTrue(finalState.ContextIndicators.Highlighted);
    }
}
