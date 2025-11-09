using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentGui.Services;

namespace TransparentAiAgentGui_Tests.Services;

[TestClass]
public class ConversationUIServiceTests
{
    private Mock<IAgentOrchestrator> _mockOrchestrator = null!;
    private Mock<IConversationManager> _mockConversationManager = null!;
    private Mock<IScenarioExecutor> _mockScenarioExecutor = null!;
    private ConversationUIService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOrchestrator = new Mock<IAgentOrchestrator>();
        _mockConversationManager = new Mock<IConversationManager>();
        _mockScenarioExecutor = new Mock<IScenarioExecutor>();

        // Setup default return for GetAllMessages
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(new List<IMessage>().AsReadOnly());

        _service = new ConversationUIService(_mockOrchestrator.Object, _mockConversationManager.Object, _mockScenarioExecutor.Object);
    }

    [TestMethod]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        // Arrange
        _mockConversationManager.Setup(x => x.GetAllMessages()).Returns(new List<IMessage>().AsReadOnly());

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ConversationUIService(null!, _mockConversationManager.Object, _mockScenarioExecutor.Object));
    }

    [TestMethod]
    public void Constructor_NullConversationManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ConversationUIService(_mockOrchestrator.Object, null!, _mockScenarioExecutor.Object));
    }

    [TestMethod]
    public async Task SendMessageAsync_NullContent_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _service.SendMessageAsync(null!));
    }

    [TestMethod]
    public async Task SendMessageAsync_EmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _service.SendMessageAsync(string.Empty));
    }

    [TestMethod]
    public async Task SendMessageAsync_WhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _service.SendMessageAsync("   "));
    }

    [TestMethod]
    public async Task SendMessageAsync_ValidContent_CallsOrchestratorProcessUserInputAsync()
    {
        // Arrange
        var mockMessage = new AssistantMessage("Response");
        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessage);

        // Act
        await _service.SendMessageAsync("Hello");

        // Assert
        _mockOrchestrator.Verify(x => x.ProcessUserInputAsync("Hello", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task SendMessageAsync_ValidContent_RefreshesMessagesAfterProcessing()
    {
        // Arrange
        var userMessage = new UserMessage("Hello");
        var assistantMessage = new AssistantMessage("Response");

        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assistantMessage);

        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(new List<IMessage> { userMessage, assistantMessage }.AsReadOnly());

        // Act
        await _service.SendMessageAsync("Hello");

        // Assert
        Assert.AreEqual(2, _service.Messages.Count);
    }

    [TestMethod]
    public async Task SendMessageAsync_RaisesProcessingStateChangedEvents()
    {
        // Arrange
        var stateChanges = new List<bool>();
        _service.ProcessingStateChanged += (sender, isProcessing) => stateChanges.Add(isProcessing);

        var mockMessage = new AssistantMessage("Response");
        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessage);

        // Act
        await _service.SendMessageAsync("Hello");

        // Assert - Should have been set to true then false
        Assert.AreEqual(2, stateChanges.Count);
        Assert.IsTrue(stateChanges[0]); // Set to true
        Assert.IsFalse(stateChanges[1]); // Set to false
    }

    [TestMethod]
    public async Task ClearConversationAsync_ClearsMessagesAndRaisesEvent()
    {
        // Arrange
        var messages = new List<IMessage> { new UserMessage("Hello") };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Create new service to pick up the message
        var service = new ConversationUIService(_mockOrchestrator.Object, _mockConversationManager.Object, _mockScenarioExecutor.Object);
        Assert.AreEqual(1, service.Messages.Count); // Verify message was loaded

        var eventRaised = false;
        service.MessagesChanged += (sender, e) => eventRaised = true;

        // Act
        await service.ClearConversationAsync();

        // Assert
        Assert.AreEqual(0, service.Messages.Count);
        Assert.IsTrue(eventRaised);
        _mockConversationManager.Verify(x => x.ClearConversation(), Times.Once);
    }

    [TestMethod]
    public void Messages_InitiallyEmpty_WhenNoMessagesInConversation()
    {
        // Assert
        Assert.AreEqual(0, _service.Messages.Count);
    }

    [TestMethod]
    public void Messages_LoadsExistingMessages_OnConstruction()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new UserMessage("Hello"),
            new AssistantMessage("Hi there")
        };

        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Act
        var service = new ConversationUIService(_mockOrchestrator.Object, _mockConversationManager.Object, _mockScenarioExecutor.Object);

        // Assert
        Assert.AreEqual(2, service.Messages.Count);
        Assert.AreEqual("Hello", service.Messages[0].Content);
        Assert.AreEqual("Hi there", service.Messages[1].Content);
    }

    [TestMethod]
    public void IsProcessing_InitiallyFalse()
    {
        // Assert
        Assert.IsFalse(_service.IsProcessing);
    }
}
