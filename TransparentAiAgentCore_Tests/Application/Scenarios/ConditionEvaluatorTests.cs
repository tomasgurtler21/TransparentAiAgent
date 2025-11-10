using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore_Tests.Application.Scenarios;

[TestClass]
public class ConditionEvaluatorTests
{
    private Mock<IAgentOrchestrator> _mockOrchestrator;
    private Mock<IConversationManager> _mockConversationManager;
    private ConditionEvaluator _evaluator;

    [TestInitialize]
    public void Setup()
    {
        _mockConversationManager = new Mock<IConversationManager>();
        _mockOrchestrator = new Mock<IAgentOrchestrator>();
        _mockOrchestrator.Setup(o => o.ConversationManager).Returns(_mockConversationManager.Object);
        _evaluator = new ConditionEvaluator(_mockOrchestrator.Object);
    }

    [TestMethod]
    public async Task EvaluateAsync_NullCondition_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _evaluator.EvaluateAsync(null!, parameters, CancellationToken.None));
    }

    [TestMethod]
    public async Task EvaluateAsync_EmptyCondition_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _evaluator.EvaluateAsync("", parameters, CancellationToken.None));
    }

    [TestMethod]
    public async Task EvaluateAsync_NullParameters_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            _evaluator.EvaluateAsync("response_contains", null!, CancellationToken.None));
    }

    [TestMethod]
    public async Task EvaluateAsync_UnknownCondition_ThrowsNotSupportedException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() =>
            _evaluator.EvaluateAsync("unknown_condition", parameters, CancellationToken.None));
    }

    // response_contains condition tests

    [TestMethod]
    public async Task EvaluateAsync_ResponseContains_MissingKeywordsParameter_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _evaluator.EvaluateAsync("response_contains", parameters, CancellationToken.None));
    }

    [TestMethod]
    public async Task EvaluateAsync_ResponseContains_EmptyKeywordsList_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["keywords"] = new List<string>()
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _evaluator.EvaluateAsync("response_contains", parameters, CancellationToken.None));
    }

    [TestMethod]
    public async Task EvaluateAsync_ResponseContains_NoMessages_ReturnsFalse()
    {
        // Arrange
        _mockConversationManager.Setup(c => c.GetAllMessages())
            .Returns(new List<IMessage>());

        var parameters = new Dictionary<string, object>
        {
            ["keywords"] = new List<string> { "test" }
        };

        // Act
        var result = await _evaluator.EvaluateAsync("response_contains", parameters, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task EvaluateAsync_ResponseContains_LastMessageNotAssistant_ReturnsFalse()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new UserMessage("Hello")
        };
        _mockConversationManager.Setup(c => c.GetAllMessages()).Returns(messages);

        var parameters = new Dictionary<string, object>
        {
            ["keywords"] = new List<string> { "test" }
        };

        // Act
        var result = await _evaluator.EvaluateAsync("response_contains", parameters, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task EvaluateAsync_ResponseContains_KeywordPresent_ReturnsTrue()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new AssistantMessage("I don't know the answer to that question")
        };
        _mockConversationManager.Setup(c => c.GetAllMessages()).Returns(messages);

        var parameters = new Dictionary<string, object>
        {
            ["keywords"] = new List<string> { "don't know", "cannot recall" }
        };

        // Act
        var result = await _evaluator.EvaluateAsync("response_contains", parameters, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateAsync_ResponseContains_KeywordNotPresent_ReturnsFalse()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new AssistantMessage("The answer is 42")
        };
        _mockConversationManager.Setup(c => c.GetAllMessages()).Returns(messages);

        var parameters = new Dictionary<string, object>
        {
            ["keywords"] = new List<string> { "don't know", "cannot recall" }
        };

        // Act
        var result = await _evaluator.EvaluateAsync("response_contains", parameters, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task EvaluateAsync_ResponseContains_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new AssistantMessage("I DON'T KNOW that information")
        };
        _mockConversationManager.Setup(c => c.GetAllMessages()).Returns(messages);

        var parameters = new Dictionary<string, object>
        {
            ["keywords"] = new List<string> { "don't know" }
        };

        // Act
        var result = await _evaluator.EvaluateAsync("response_contains", parameters, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
    }

    // message_count condition tests

    [TestMethod]
    public async Task EvaluateAsync_MessageCount_MissingCountParameter_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _evaluator.EvaluateAsync("message_count", parameters, CancellationToken.None));
    }

    [TestMethod]
    public async Task EvaluateAsync_MessageCount_CountMet_ReturnsTrue()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new UserMessage("Message 1"),
            new AssistantMessage("Response 1"),
            new UserMessage("Message 2"),
            new AssistantMessage("Response 2")
        };
        _mockConversationManager.Setup(c => c.GetAllMessages()).Returns(messages);

        var parameters = new Dictionary<string, object>
        {
            ["count"] = 4
        };

        // Act
        var result = await _evaluator.EvaluateAsync("message_count", parameters, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateAsync_MessageCount_CountNotMet_ReturnsFalse()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new UserMessage("Message 1"),
            new AssistantMessage("Response 1")
        };
        _mockConversationManager.Setup(c => c.GetAllMessages()).Returns(messages);

        var parameters = new Dictionary<string, object>
        {
            ["count"] = 5
        };

        // Act
        var result = await _evaluator.EvaluateAsync("message_count", parameters, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task EvaluateAsync_MessageCount_ExceedsCount_ReturnsTrue()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new UserMessage("Message 1"),
            new AssistantMessage("Response 1"),
            new UserMessage("Message 2"),
            new AssistantMessage("Response 2"),
            new UserMessage("Message 3")
        };
        _mockConversationManager.Setup(c => c.GetAllMessages()).Returns(messages);

        var parameters = new Dictionary<string, object>
        {
            ["count"] = 3
        };

        // Act
        var result = await _evaluator.EvaluateAsync("message_count", parameters, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
    }

    // user_interaction and ui_state_changed - not yet implemented

    [TestMethod]
    public async Task EvaluateAsync_UserInteraction_ThrowsNotImplementedException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["timeout"] = 5000
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(() =>
            _evaluator.EvaluateAsync("user_interaction", parameters, CancellationToken.None));
    }

    [TestMethod]
    public async Task EvaluateAsync_UIStateChanged_ThrowsNotImplementedException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["component"] = "chat_filter",
            ["property"] = "visible",
            ["value"] = true
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(() =>
            _evaluator.EvaluateAsync("ui_state_changed", parameters, CancellationToken.None));
    }
}
