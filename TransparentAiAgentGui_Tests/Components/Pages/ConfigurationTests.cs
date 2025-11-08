using Bunit;
using Moq;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentGui.Components.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace TransparentAiAgentGui_Tests.Components.Pages;

/// <summary>
/// Tests for Configuration page UIControl integration.
/// Following Lean TDD - testing meaningful behavior (UIControl integration).
/// These tests should COMPILE but FAIL until implementation is added.
/// </summary>
[TestClass]
public class ConfigurationTests : Bunit.TestContext
{
    private Mock<IUIControlService> _mockUIControlService = null!;
    private Mock<HttpClient> _mockHttpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUIControlService = new Mock<IUIControlService>();
        _mockHttpClient = new Mock<HttpClient>();

        // Setup default behavior
        _mockUIControlService.Setup(s => s.GetCurrentState())
            .Returns(UIState.DefaultNormalMode());

        Services.AddSingleton(_mockUIControlService.Object);
        Services.AddSingleton(_mockHttpClient.Object);
    }

    /// <summary>
    /// Tests that component subscribes to UIStateChanged on initialization.
    /// This test will FAIL until Configuration.razor subscribes to the event.
    /// </summary>
    [TestMethod]
    public void Configuration_OnInitialized_SubscribesToUIStateChanged()
    {
        // Arrange
        bool eventSubscribed = false;
        _mockUIControlService.SetupAdd(s => s.UIStateChanged += It.IsAny<EventHandler<UIState>>())
            .Callback(() => eventSubscribed = true);

        // Act
        IRenderedComponent<Configuration> cut = RenderComponent<Configuration>();

        // Assert - Should subscribe to UIStateChanged event
        Assert.IsTrue(eventSubscribed, "Configuration should subscribe to UIStateChanged event");
    }

    /// <summary>
    /// Tests that component applies highlighted section CSS class.
    /// This test will FAIL until Configuration.razor implements section highlighting.
    /// </summary>
    [TestMethod]
    public void Configuration_WhenSectionHighlighted_AppliesHighlightClass()
    {
        // Arrange
        UIState highlightedState = UIState.DefaultNormalMode() with
        {
            ConfigurationPage = new ConfigurationPageState
            {
                NavigateRequested = false,
                HighlightedSection = "SystemPrompt"
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(highlightedState);

        // Act
        IRenderedComponent<Configuration> cut = RenderComponent<Configuration>();

        // Assert - Should have highlighted class on the section
        Assert.IsTrue(cut.Markup.Contains("highlighted"),
            "Configuration should apply 'highlighted' class when section is specified");
    }

    /// <summary>
    /// Tests that component responds to UIStateChanged events.
    /// This test will FAIL until Configuration.razor handles UIStateChanged.
    /// </summary>
    [TestMethod]
    public void Configuration_OnUIStateChanged_UpdatesHighlighting()
    {
        // Arrange
        EventHandler<UIState>? capturedHandler = null;
        _mockUIControlService.SetupAdd(s => s.UIStateChanged += It.IsAny<EventHandler<UIState>>())
            .Callback<EventHandler<UIState>>(handler => capturedHandler = handler);

        UIState initialState = UIState.DefaultNormalMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(initialState);

        IRenderedComponent<Configuration> cut = RenderComponent<Configuration>();

        // Initially no highlighting
        Assert.IsFalse(cut.Markup.Contains("highlighted"), "Should not be highlighted initially");

        // Act - Simulate UIStateChanged event with highlighted section
        UIState newState = UIState.DefaultNormalMode() with
        {
            ConfigurationPage = new ConfigurationPageState
            {
                NavigateRequested = false,
                HighlightedSection = "AgentConfiguration"
            }
        };

        Assert.IsNotNull(capturedHandler, "Should have captured event handler");
        capturedHandler?.Invoke(_mockUIControlService.Object, newState);
        cut.Render();

        // Assert - Should now have highlighting
        Assert.IsTrue(cut.Markup.Contains("highlighted"),
            "Configuration should apply highlighting after UIStateChanged event");
    }

    /// <summary>
    /// Tests that component unsubscribes from UIStateChanged on disposal.
    /// This test will FAIL until Configuration.razor implements IDisposable.
    /// </summary>
    [TestMethod]
    public void Configuration_OnDispose_UnsubscribesFromUIStateChanged()
    {
        // Arrange
        bool eventUnsubscribed = false;
        _mockUIControlService.SetupRemove(s => s.UIStateChanged -= It.IsAny<EventHandler<UIState>>())
            .Callback(() => eventUnsubscribed = true);

        // Act
        IRenderedComponent<Configuration> cut = RenderComponent<Configuration>();
        cut.Dispose();

        // Assert - Should unsubscribe from UIStateChanged event
        Assert.IsTrue(eventUnsubscribed, "Configuration should unsubscribe from UIStateChanged on disposal");
    }
}
