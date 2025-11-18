using Bunit;
using Moq;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentGui.Components.Chat;
using TransparentAiAgentGui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace TransparentAiAgentGui_Tests.Components.Chat;

/// <summary>
/// Tests for MessageDisplay conditional rendering based on filter state.
/// Following Lean TDD - testing meaningful behavior (conditional tool call rendering).
/// </summary>
[TestClass]
public class MessageDisplayFilteringTests : Bunit.TestContext
{
    private Mock<IUIControlService> _mockUIControlService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUIControlService = new Mock<IUIControlService>();
        Services.AddSingleton(_mockUIControlService.Object);
    }

    /// <summary>
    /// Tests that tool call details are not rendered when ShowToolCalls is false.
    /// This is the core rendering change.
    /// </summary>
    [TestMethod]
    public void MessageDisplay_ToolCall_HidesDetailsWhenFiltered()
    {
        // Arrange - Teaching mode (ShowToolCalls = false)
        var teachingState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(teachingState);

        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "I'll check the weather.",
            IsToolCall = true,
            ToolCalls = new List<UIMessage.UIToolCall>
            {
                new() { Id = "call_1", Name = "get_weather", Arguments = "{\"city\":\"Prague\"}" }
            }
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Tool call details should NOT be visible
        var markup = cut.Markup;
        Assert.IsFalse(markup.Contains("get_weather"),
            "Tool call name should not be visible when ShowToolCalls is false");
        Assert.IsFalse(markup.Contains("Prague"),
            "Tool call arguments should not be visible when ShowToolCalls is false");
    }

    /// <summary>
    /// Tests that message content is always visible, even when ShowToolCalls is false.
    /// Content is separate from tool call details.
    /// </summary>
    [TestMethod]
    public void MessageDisplay_ToolCall_ShowsContentWhenFiltered()
    {
        // Arrange - Teaching mode (ShowToolCalls = false)
        var teachingState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(teachingState);

        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "I'll check the weather for you.",
            IsToolCall = true,
            ToolCalls = new List<UIMessage.UIToolCall>
            {
                new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
            }
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Content should be visible
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("I'll check the weather for you"),
            "Message content should always be visible, even when ShowToolCalls is false");
    }

    /// <summary>
    /// Tests that thinking content is always visible, even when ShowToolCalls is false.
    /// Thinking provides valuable insight into agent reasoning.
    /// </summary>
    [TestMethod]
    public void MessageDisplay_ToolCall_ShowsThinkingWhenFiltered()
    {
        // Arrange - Teaching mode (ShowToolCalls = false)
        var teachingState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(teachingState);

        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "",
            Thinking = "I need to fetch weather data from the API.",
            IsToolCall = true,
            ToolCalls = new List<UIMessage.UIToolCall>
            {
                new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
            }
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Thinking should be visible
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("I need to fetch weather data"),
            "Thinking content should always be visible, even when ShowToolCalls is false");
    }

    /// <summary>
    /// Tests that tool call details are rendered when ShowToolCalls is true.
    /// This is existing behavior - should not break.
    /// </summary>
    [TestMethod]
    public void MessageDisplay_ToolCall_ShowsDetailsWhenNotFiltered()
    {
        // Arrange - Normal mode (ShowToolCalls = true)
        var normalState = UIState.DefaultNormalMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(normalState);

        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "I'll check the weather.",
            IsToolCall = true,
            ToolCalls = new List<UIMessage.UIToolCall>
            {
                new() { Id = "call_1", Name = "get_weather", Arguments = "{\"city\":\"Prague\"}" }
            }
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Tool call details should be visible
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("get_weather"),
            "Tool call name should be visible when ShowToolCalls is true");
    }

    /// <summary>
    /// Tests that message header shows "Assistant" when ShowToolCalls is false.
    /// This makes the UI less confusing for users in Teaching Mode.
    /// </summary>
    [TestMethod]
    public void MessageDisplay_ToolCall_ShowsAssistantLabelWhenFiltered()
    {
        // Arrange - Teaching mode (ShowToolCalls = false)
        var teachingState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(teachingState);

        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "I'll check the weather.",
            IsToolCall = true,
            ToolCalls = new List<UIMessage.UIToolCall>
            {
                new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
            }
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Header should show "Assistant"
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("Assistant"),
            "Message header should show 'Assistant' when ShowToolCalls is false");
        Assert.IsFalse(markup.Contains("Tool Call"),
            "Message header should not show 'Tool Call' when ShowToolCalls is false");
    }

    /// <summary>
    /// Tests that message header shows "Tool Call" when ShowToolCalls is true.
    /// This is existing behavior - should not break.
    /// </summary>
    [TestMethod]
    public void MessageDisplay_ToolCall_ShowsToolCallLabelWhenNotFiltered()
    {
        // Arrange - Normal mode (ShowToolCalls = true)
        var normalState = UIState.DefaultNormalMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(normalState);

        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "I'll check the weather.",
            IsToolCall = true,
            ToolCalls = new List<UIMessage.UIToolCall>
            {
                new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
            }
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Header should show "Tool Call"
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("Tool Call"),
            "Message header should show 'Tool Call' when ShowToolCalls is true");
    }
}
