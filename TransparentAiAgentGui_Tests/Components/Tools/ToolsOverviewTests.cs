using Bunit;
using Moq;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Tools;
using TransparentAiAgentGui.Components.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace TransparentAiAgentGui_Tests.Components.Tools;

/// <summary>
/// Tests for ToolsOverview component UIControl integration.
/// Following Lean TDD - testing meaningful behavior (UIControl integration).
/// </summary>
[TestClass]
public class ToolsOverviewTests : Bunit.TestContext
{
    private Mock<IToolRegistry> _mockToolRegistry = null!;
    private Mock<IToolUsageStatistics> _mockToolStats = null!;
    private Mock<IUIControlService> _mockUIControlService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockToolRegistry = new Mock<IToolRegistry>();
        _mockToolStats = new Mock<IToolUsageStatistics>();
        _mockUIControlService = new Mock<IUIControlService>();

        // Setup default behavior
        _mockToolRegistry.Setup(r => r.GetAllTools()).Returns(new List<ITool>());
        _mockUIControlService.Setup(s => s.GetCurrentState())
            .Returns(UIState.DefaultNormalMode());

        Services.AddSingleton(_mockToolRegistry.Object);
        Services.AddSingleton(_mockToolStats.Object);
        Services.AddSingleton(_mockUIControlService.Object);
    }

    /// <summary>
    /// Tests that tools panel respects Visible state from UIControl.
    /// </summary>
    [TestMethod]
    public void ToolsOverview_WhenVisibleFalse_HidesPanel()
    {
        // Arrange
        UIState hiddenState = UIState.DefaultNormalMode() with
        {
            ToolsPanel = new ToolsPanelState
            {
                Visible = false,
                ExpandedTools = new List<string>(),
                HighlightedTool = null
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(hiddenState);

        // Act
        IRenderedComponent<ToolsOverview> cut = RenderComponent<ToolsOverview>();

        // Assert
        Assert.IsFalse(cut.Markup.Contains("tools-overview"));
    }

    /// <summary>
    /// Tests that tools panel is shown when Visible is true.
    /// </summary>
    [TestMethod]
    public void ToolsOverview_WhenVisibleTrue_ShowsPanel()
    {
        // Arrange
        UIState visibleState = UIState.DefaultNormalMode() with
        {
            ToolsPanel = new ToolsPanelState
            {
                Visible = true,
                ExpandedTools = new List<string>(),
                HighlightedTool = null
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(visibleState);

        // Act
        IRenderedComponent<ToolsOverview> cut = RenderComponent<ToolsOverview>();

        // Assert
        Assert.IsTrue(cut.Markup.Contains("tools-overview"));
    }

    /// <summary>
    /// Tests that component responds to UIStateChanged events.
    /// </summary>
    [TestMethod]
    public void ToolsOverview_OnUIStateChanged_UpdatesDisplay()
    {
        // Arrange
        EventHandler<UIState>? capturedHandler = null;
        _mockUIControlService.SetupAdd(s => s.UIStateChanged += It.IsAny<EventHandler<UIState>>())
            .Callback<EventHandler<UIState>>(handler => capturedHandler = handler);

        UIState initialState = UIState.DefaultNormalMode() with
        {
            ToolsPanel = new ToolsPanelState
            {
                Visible = true,
                ExpandedTools = new List<string>(),
                HighlightedTool = null
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(initialState);

        IRenderedComponent<ToolsOverview> cut = RenderComponent<ToolsOverview>();

        // Initially visible
        Assert.IsTrue(cut.Markup.Contains("tools-overview"));

        // Act - Simulate UIStateChanged event
        UIState newState = UIState.DefaultNormalMode() with
        {
            ToolsPanel = new ToolsPanelState
            {
                Visible = false,
                ExpandedTools = new List<string>(),
                HighlightedTool = null
            }
        };
        capturedHandler?.Invoke(_mockUIControlService.Object, newState);

        // Assert - Should now be hidden (wait for async state change)
        cut.WaitForAssertion(() =>
            Assert.IsFalse(cut.Markup.Contains("tools-overview")));
    }
}
