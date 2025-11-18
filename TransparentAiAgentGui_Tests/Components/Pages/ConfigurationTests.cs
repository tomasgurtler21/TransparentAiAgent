using Bunit;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
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
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUIControlService = new Mock<IUIControlService>();

        // Setup mock HTTP client with proper response
        Mock<HttpMessageHandler> mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"agent\":{\"systemPrompt\":\"test\",\"contextWindowSize\":20},\"llm\":{\"temperature\":0.7,\"maxTokens\":1000,\"topP\":1.0}}")
            });

        _httpClient = new HttpClient(mockHttpHandler.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        // Setup default behavior
        _mockUIControlService.Setup(s => s.GetCurrentState())
            .Returns(UIState.DefaultNormalMode());

        Services.AddSingleton(_mockUIControlService.Object);
        Services.AddSingleton(_httpClient);
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
    public async Task Configuration_WhenSectionHighlighted_AppliesHighlightClass()
    {
        // Arrange
        UIState highlightedState = UIState.DefaultNormalMode() with
        {
            ConfigurationPage = new ConfigurationPageState
            {
                Visible = true,
                HighlightedSection = "SystemPrompt"
            }
        };
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(highlightedState);

        // Act
        IRenderedComponent<Configuration> cut = RenderComponent<Configuration>();

        // Wait for async initialization to complete
        await Task.Delay(100);
        cut.Render();

        // Assert - Should have highlighted class on the section
        Assert.IsTrue(cut.Markup.Contains("highlighted"),
            "Configuration should apply 'highlighted' class when section is specified");
    }

    /// <summary>
    /// Tests that component responds to UIStateChanged events.
    /// This test will FAIL until Configuration.razor handles UIStateChanged.
    /// </summary>
    [TestMethod]
    public async Task Configuration_OnUIStateChanged_UpdatesHighlighting()
    {
        // Arrange
        EventHandler<UIState>? capturedHandler = null;
        _mockUIControlService.SetupAdd(s => s.UIStateChanged += It.IsAny<EventHandler<UIState>>())
            .Callback<EventHandler<UIState>>(handler => capturedHandler = handler);

        UIState initialState = UIState.DefaultNormalMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(initialState);

        IRenderedComponent<Configuration> cut = RenderComponent<Configuration>();

        // Wait for async initialization
        await Task.Delay(100);
        cut.Render();

        // Initially no highlighting
        Assert.IsFalse(cut.Markup.Contains("highlighted"), "Should not be highlighted initially");

        // Act - Simulate UIStateChanged event with highlighted section
        UIState newState = UIState.DefaultNormalMode() with
        {
            ConfigurationPage = new ConfigurationPageState
            {
                Visible = true,
                HighlightedSection = "AgentConfiguration"
            }
        };

        Assert.IsNotNull(capturedHandler, "Should have captured event handler");
        capturedHandler?.Invoke(_mockUIControlService.Object, newState);

        // Wait for state update
        await Task.Delay(50);
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

        // Track both subscription and unsubscription
        _mockUIControlService.SetupAdd(s => s.UIStateChanged += It.IsAny<EventHandler<UIState>>())
            .Callback(() => { /* Subscribed */ });

        _mockUIControlService.SetupRemove(s => s.UIStateChanged -= It.IsAny<EventHandler<UIState>>())
            .Callback(() => eventUnsubscribed = true);

        // Act
        IRenderedComponent<Configuration> cut = RenderComponent<Configuration>();

        // Explicitly dispose the component (bUnit may not call IDisposable automatically)
        (cut.Instance as IDisposable)?.Dispose();

        // Assert - Should unsubscribe from UIStateChanged event
        Assert.IsTrue(eventUnsubscribed, "Configuration should unsubscribe from UIStateChanged on disposal");
    }
}
