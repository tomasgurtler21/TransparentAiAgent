using Bunit;
using Moq;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentGui.Components.Chat;
using TransparentAiAgentGui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace TransparentAiAgentGui_Tests.Components.Chat;

/// <summary>
/// Tests for MessageList filtering logic.
/// Following Lean TDD - testing meaningful behavior (smart filtering for tool calls).
/// </summary>
[TestClass]
public class MessageListFilteringTests : Bunit.TestContext
{
    private Mock<IUIControlService> _mockUIControlService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUIControlService = new Mock<IUIControlService>();

        // Default state: ShowToolCalls = false (Teaching Mode)
        var defaultState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(defaultState);

        Services.AddSingleton(_mockUIControlService.Object);
    }

    /// <summary>
    /// Tests that tool call messages with content are visible even when ShowToolCalls is false.
    /// This is the core fix - we show the text, hide the tool call details.
    /// </summary>
    [TestMethod]
    public void MessageList_ToolCallWithContent_ShowsWhenToolCallsFiltered()
    {
        // Arrange - Teaching mode (ShowToolCalls = false)
        var teachingState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(teachingState);

        var messages = new List<UIMessage>
        {
            new UIMessage
            {
                Id = Guid.NewGuid(),
                Role = MessageRole.Assistant,
                Content = "I'll help you check the weather.",  // HAS CONTENT
                IsToolCall = true,
                ToolCalls = new List<UIMessage.UIToolCall>
                {
                    new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
                }
            }
        };

        // Act
        var cut = RenderComponent<MessageList>(parameters => parameters
            .Add(p => p.Messages, messages)
            .Add(p => p.IsProcessing, false));

        // Assert - Message should be visible (we show the content, hide the tool call details)
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("I'll help you check the weather"),
            "Tool call message with content should be visible when ShowToolCalls is false");
    }

    /// <summary>
    /// Tests that tool call messages with thinking content are visible when ShowToolCalls is false.
    /// Thinking content is valuable for understanding agent reasoning.
    /// </summary>
    [TestMethod]
    public void MessageList_ToolCallWithThinking_ShowsWhenToolCallsFiltered()
    {
        // Arrange - Teaching mode (ShowToolCalls = false)
        var teachingState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(teachingState);

        var messages = new List<UIMessage>
        {
            new UIMessage
            {
                Id = Guid.NewGuid(),
                Role = MessageRole.Assistant,
                Content = "",  // No regular content
                Thinking = "I need to call the weather API to get current conditions.",  // HAS THINKING
                IsToolCall = true,
                ToolCalls = new List<UIMessage.UIToolCall>
                {
                    new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
                }
            }
        };

        // Act
        var cut = RenderComponent<MessageList>(parameters => parameters
            .Add(p => p.Messages, messages)
            .Add(p => p.IsProcessing, false));

        // Assert - Message should be visible (thinking is valuable content)
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("I need to call the weather API"),
            "Tool call message with thinking should be visible when ShowToolCalls is false");
    }

    /// <summary>
    /// Tests that tool call messages without any content/thinking are hidden when ShowToolCalls is false.
    /// This avoids showing empty message headers in the UI.
    /// </summary>
    [TestMethod]
    public void MessageList_ToolCallWithoutContent_HidesWhenToolCallsFiltered()
    {
        // Arrange - Teaching mode (ShowToolCalls = false)
        var teachingState = UIState.DefaultTeachingMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(teachingState);

        var messages = new List<UIMessage>
        {
            new UIMessage
            {
                Id = Guid.NewGuid(),
                Role = MessageRole.Assistant,
                Content = "",  // No content
                Thinking = null,  // No thinking
                IsToolCall = true,
                ToolCalls = new List<UIMessage.UIToolCall>
                {
                    new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
                }
            }
        };

        // Act
        var cut = RenderComponent<MessageList>(parameters => parameters
            .Add(p => p.Messages, messages)
            .Add(p => p.IsProcessing, false));

        // Assert - Message should be hidden (no content to show)
        var markup = cut.Markup;
        Assert.IsFalse(markup.Contains("get_weather"),
            "Tool call message without content/thinking should be hidden when ShowToolCalls is false");
    }

    /// <summary>
    /// Tests that all tool call messages are visible when ShowToolCalls is true.
    /// This is existing behavior - should not break.
    /// </summary>
    [TestMethod]
    public void MessageList_ToolCall_ShowsWhenToolCallsNotFiltered()
    {
        // Arrange - Normal mode (ShowToolCalls = true)
        var normalState = UIState.DefaultNormalMode();
        _mockUIControlService.Setup(s => s.GetCurrentState()).Returns(normalState);

        var messages = new List<UIMessage>
        {
            new UIMessage
            {
                Id = Guid.NewGuid(),
                Role = MessageRole.Assistant,
                Content = "",  // No content - should still show
                IsToolCall = true,
                ToolCalls = new List<UIMessage.UIToolCall>
                {
                    new() { Id = "call_1", Name = "get_weather", Arguments = "{}" }
                }
            }
        };

        // Act
        var cut = RenderComponent<MessageList>(parameters => parameters
            .Add(p => p.Messages, messages)
            .Add(p => p.IsProcessing, false));

        // Assert - Message should be visible
        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("get_weather"),
            "Tool call messages should always be visible when ShowToolCalls is true");
    }
}
