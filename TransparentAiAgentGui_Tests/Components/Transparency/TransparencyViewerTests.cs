using Bunit;
using Moq;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentGui.Components.Transparency;
using Microsoft.Extensions.DependencyInjection;

namespace TransparentAiAgentGui_Tests.Components.Transparency;

/// <summary>
/// Tests for TransparencyViewer component UIControl integration.
/// Following Lean TDD - testing meaningful behavior (UIControl integration).
/// </summary>
[TestClass]
public class TransparencyViewerTests : Bunit.TestContext
{
    private Mock<ITransparencyService> _mockTransparencyService = null!;
    private Mock<IUIControlService> _mockUIControlService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockTransparencyService = new Mock<ITransparencyService>();
        _mockUIControlService = new Mock<IUIControlService>();

        // Setup default behavior
        _mockTransparencyService.Setup(s => s.GetRecentEvents(It.IsAny<int>()))
            .Returns(new List<TransparencyEvent>());
        _mockUIControlService.Setup(s => s.GetCurrentState())
            .Returns(UIState.DefaultNormalMode());

        Services.AddSingleton(_mockTransparencyService.Object);
        Services.AddSingleton(_mockUIControlService.Object);
    }

    /// <summary>
    /// Tests that viewer respects Visible state from UIControl.
    /// </summary>
    [TestMethod]
    public void TransparencyViewer_WhenVisibleFalse_HidesViewer()
    {
        // Arrange
        UIState hiddenState = UIState.DefaultNormalMode() with
        {
            TransparencyViewer = new TransparencyViewerState
            {
                Visible = false,
                ShowTimestamps = true,
                EventTypeFilters = new List<string>()
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(hiddenState);

        // Act
        IRenderedComponent<TransparencyViewer> cut = RenderComponent<TransparencyViewer>();

        // Assert
        Assert.IsFalse(cut.Markup.Contains("transparency-viewer"));
    }

    /// <summary>
    /// Tests that viewer is shown when Visible is true.
    /// </summary>
    [TestMethod]
    public void TransparencyViewer_WhenVisibleTrue_ShowsViewer()
    {
        // Arrange
        UIState visibleState = UIState.DefaultNormalMode() with
        {
            TransparencyViewer = new TransparencyViewerState
            {
                Visible = true,
                ShowTimestamps = true,
                EventTypeFilters = new List<string>()
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(visibleState);

        // Act
        IRenderedComponent<TransparencyViewer> cut = RenderComponent<TransparencyViewer>();

        // Assert
        Assert.IsTrue(cut.Markup.Contains("transparency-viewer"));
    }

    /// <summary>
    /// Tests that viewer hides timestamps when ShowTimestamps is false.
    /// </summary>
    [TestMethod]
    public void TransparencyViewer_WhenShowTimestampsFalse_HidesTimestamps()
    {
        // Arrange
        List<TransparencyEvent> testEvents = new List<TransparencyEvent>
        {
            new TransparencyEvent(TransparencyEventType.AgentStarted, "data", "Test event")
        };
        _mockTransparencyService.Setup(s => s.GetRecentEvents(It.IsAny<int>()))
            .Returns(testEvents);

        UIState noTimestampsState = UIState.DefaultNormalMode() with
        {
            TransparencyViewer = new TransparencyViewerState
            {
                Visible = true,
                ShowTimestamps = false,
                EventTypeFilters = new List<string>()
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(noTimestampsState);

        // Act
        IRenderedComponent<TransparencyViewer> cut = RenderComponent<TransparencyViewer>();

        // Assert - timestamp elements should not be rendered
        Assert.IsFalse(cut.Markup.Contains("event-timestamp"));
    }

    /// <summary>
    /// Tests that viewer shows timestamps when ShowTimestamps is true.
    /// </summary>
    [TestMethod]
    public void TransparencyViewer_WhenShowTimestampsTrue_ShowsTimestamps()
    {
        // Arrange
        List<TransparencyEvent> testEvents = new List<TransparencyEvent>
        {
            new TransparencyEvent(TransparencyEventType.AgentStarted, "data", "Test event")
        };
        _mockTransparencyService.Setup(s => s.GetRecentEvents(It.IsAny<int>()))
            .Returns(testEvents);

        UIState withTimestampsState = UIState.DefaultNormalMode() with
        {
            TransparencyViewer = new TransparencyViewerState
            {
                Visible = true,
                ShowTimestamps = true,
                EventTypeFilters = new List<string>()
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(withTimestampsState);

        // Act
        IRenderedComponent<TransparencyViewer> cut = RenderComponent<TransparencyViewer>();

        // Assert - timestamp elements should be rendered
        Assert.IsTrue(cut.Markup.Contains("event-timestamp"));
    }

    /// <summary>
    /// Tests that viewer filters events based on EventTypeFilters.
    /// </summary>
    [TestMethod]
    public void TransparencyViewer_WithEventTypeFilters_FiltersEvents()
    {
        // Arrange
        List<TransparencyEvent> testEvents = new List<TransparencyEvent>
        {
            new TransparencyEvent(TransparencyEventType.AgentStarted, "data1", "Event 1"),
            new TransparencyEvent(TransparencyEventType.ToolExecuted, "data2", "Event 2"),
            new TransparencyEvent(TransparencyEventType.LLMRequest, "data3", "Event 3")
        };
        _mockTransparencyService.Setup(s => s.GetRecentEvents(It.IsAny<int>()))
            .Returns(testEvents);

        UIState filteredState = UIState.DefaultNormalMode() with
        {
            TransparencyViewer = new TransparencyViewerState
            {
                Visible = true,
                ShowTimestamps = true,
                EventTypeFilters = new List<string> { "AgentStarted", "ToolExecuted" }
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(filteredState);

        // Act
        IRenderedComponent<TransparencyViewer> cut = RenderComponent<TransparencyViewer>();

        // Assert - should show only filtered events
        Assert.IsTrue(cut.Markup.Contains("Event 1"));
        Assert.IsTrue(cut.Markup.Contains("Event 2"));
        Assert.IsFalse(cut.Markup.Contains("Event 3"));
    }

    /// <summary>
    /// Tests that component responds to UIStateChanged events.
    /// </summary>
    [TestMethod]
    public void TransparencyViewer_OnUIStateChanged_UpdatesDisplay()
    {
        // Arrange
        EventHandler<UIState>? capturedHandler = null;
        _mockUIControlService.Setup(s => s.UIStateChanged += It.IsAny<EventHandler<UIState>>())
            .Callback<EventHandler<UIState>>(handler => capturedHandler = handler);

        UIState initialState = UIState.DefaultNormalMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(initialState);

        IRenderedComponent<TransparencyViewer> cut = RenderComponent<TransparencyViewer>();

        // Initially visible
        Assert.IsTrue(cut.Markup.Contains("transparency-viewer"));

        // Act - Simulate UIStateChanged event
        UIState newState = UIState.DefaultNormalMode() with
        {
            TransparencyViewer = new TransparencyViewerState
            {
                Visible = false,
                ShowTimestamps = true,
                EventTypeFilters = new List<string>()
            }
        };
        capturedHandler?.Invoke(_mockUIControlService.Object, newState);
        cut.Render();

        // Assert - Should now be hidden
        Assert.IsFalse(cut.Markup.Contains("transparency-viewer"));
    }
}
