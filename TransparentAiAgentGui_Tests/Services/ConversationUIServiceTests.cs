using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Moq;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.ConversationHistory;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentGui.Services;

namespace TransparentAiAgentGui_Tests.Services;

[TestClass]
public class ConversationUIServiceTests
{
    private Mock<IAgentOrchestrator> _mockOrchestrator = null!;
    private Mock<IConversationManager> _mockConversationManager = null!;
    private Mock<IScenarioExecutor> _mockScenarioExecutor = null!;
    private Mock<IConversationHistoryManager> _mockHistoryManager = null!;
    private Mock<ITransparencyService> _mockTransparencyService = null!;
    private Mock<ILogger<ConversationUIService>> _mockLogger = null!;
    private ConversationUIService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOrchestrator = new Mock<IAgentOrchestrator>();
        _mockConversationManager = new Mock<IConversationManager>();
        _mockScenarioExecutor = new Mock<IScenarioExecutor>();
        _mockHistoryManager = new Mock<IConversationHistoryManager>();
        _mockTransparencyService = new Mock<ITransparencyService>();
        _mockLogger = new Mock<ILogger<ConversationUIService>>();

        // Setup default return for GetAllMessages
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(new List<IMessage>().AsReadOnly());

        // Note: Constructor will fail until we update ConversationUIService to accept IConversationHistoryManager
        // This is expected in RED phase
        _service = new ConversationUIService(
            _mockOrchestrator.Object,
            _mockConversationManager.Object,
            _mockScenarioExecutor.Object,
            _mockHistoryManager.Object,
            _mockTransparencyService.Object,
            logger: _mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        // Arrange
        _mockConversationManager.Setup(x => x.GetAllMessages()).Returns(new List<IMessage>().AsReadOnly());

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ConversationUIService(null!, _mockConversationManager.Object, _mockScenarioExecutor.Object, _mockHistoryManager.Object, _mockTransparencyService.Object, logger: _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_NullConversationManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ConversationUIService(_mockOrchestrator.Object, null!, _mockScenarioExecutor.Object, _mockHistoryManager.Object, _mockTransparencyService.Object, logger: _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_NullHistoryManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ConversationUIService(_mockOrchestrator.Object, _mockConversationManager.Object, _mockScenarioExecutor.Object, null!, _mockTransparencyService.Object, logger: _mockLogger.Object));
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
        var mockMessage = new LlmTextMessage("Response");
        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessage);

        // Act
        await _service.SendMessageAsync("Hello");

        // Assert
        _mockOrchestrator.Verify(x => x.ProcessUserInputAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task SendMessageAsync_ValidContent_RefreshesMessagesAfterProcessing()
    {
        // Arrange
        var userMessage = new DirectUserMessage("Hello");
        var assistantMessage = new LlmTextMessage("Response");

        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
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

        var mockMessage = new LlmTextMessage("Response");
        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
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
        var messages = new List<IMessage> { new DirectUserMessage("Hello") };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Create new service to pick up the message
        var service = new ConversationUIService(_mockOrchestrator.Object, _mockConversationManager.Object, _mockScenarioExecutor.Object, _mockHistoryManager.Object, _mockTransparencyService.Object);
        Assert.AreEqual(1, service.Messages.Count); // Verify message was loaded

        var eventRaised = false;
        service.MessagesChanged += (sender, e) => eventRaised = true;

        // Act
        await service.ClearConversationAsync();

        // Assert
        Assert.AreEqual(0, service.Messages.Count);
        Assert.IsTrue(eventRaised);
        _mockConversationManager.Verify(x => x.ResetConversation(), Times.Once);
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
            new DirectUserMessage("Hello"),
            new LlmTextMessage("Hi there")
        };

        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Act
        var service = new ConversationUIService(_mockOrchestrator.Object, _mockConversationManager.Object, _mockScenarioExecutor.Object, _mockHistoryManager.Object, _mockTransparencyService.Object);

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

    [TestMethod]
    public async Task LoadConversationAsync_ClearsCurrentConversation()
    {
        // Arrange
        var existingMessages = new List<IMessage> { new DirectUserMessage("Old message") };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(existingMessages.AsReadOnly());

        // Create service with existing messages
        var service = new ConversationUIService(_mockOrchestrator.Object, _mockConversationManager.Object, _mockScenarioExecutor.Object, _mockHistoryManager.Object, _mockTransparencyService.Object);
        Assert.AreEqual(1, service.Messages.Count); // Verify old message is there

        // Setup conversation to load
        var newConversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            Name = "New Conversation",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Messages = new List<IMessage>
            {
                new DirectUserMessage("New message")
            }
        };

        // Update mock to return new messages after ClearConversation and AddMessage calls
        var newMessages = new List<IMessage> { new DirectUserMessage("New message") };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(newMessages.AsReadOnly());

        // Act
        await service.LoadConversationAsync(newConversation);

        // Assert
        Assert.AreEqual(1, service.Messages.Count);
        Assert.AreEqual("New message", service.Messages[0].Content);
        _mockConversationManager.Verify(x => x.ClearConversation(), Times.Once);
    }

    [TestMethod]
    public async Task LoadConversationAsync_RaisesMessagesChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.MessagesChanged += (sender, args) => eventRaised = true;

        var conversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Messages = new List<IMessage>
            {
                new DirectUserMessage("Test")
            }
        };

        var messages = new List<IMessage> { new DirectUserMessage("Test") };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Act
        await _service.LoadConversationAsync(conversation);

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void CurrentConversationId_ReturnsConversationManagerId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        _mockConversationManager.Setup(x => x.ConversationId)
            .Returns(expectedId);

        // Act
        var actualId = _service.CurrentConversationId;

        // Assert
        Assert.AreEqual(expectedId, actualId);
    }

    // Auto-Save Tests

    [TestMethod]
    public async Task SendMessageAsync_CallsAutoSave()
    {
        // Arrange
        var mockMessage = new LlmTextMessage("Response");
        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessage);

        var conversationId = Guid.NewGuid();
        _mockConversationManager.Setup(x => x.ConversationId)
            .Returns(conversationId);

        var messages = new List<IMessage> { new DirectUserMessage("Hello"), mockMessage };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Act
        await _service.SendMessageAsync("Hello");

        // Wait for potential fire-and-forget auto-save
        await Task.Delay(200);

        // Assert
        _mockHistoryManager.Verify(
            h => h.SaveCurrentConversationAsync(
                conversationId,
                It.Is<IReadOnlyList<IMessage>>(m => m.Count == 2)
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SendMessageStreamingAsync_CallsAutoSave()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _mockConversationManager.Setup(x => x.ConversationId)
            .Returns(conversationId);

        var streamingChunks = new[]
        {
            new StreamingResponseChunk("Hello", false),
            new StreamingResponseChunk(" world", true)
        };

        _mockOrchestrator.Setup(x => x.ProcessUserInputStreamingAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(streamingChunks));

        var messages = new List<IMessage>
        {
            new DirectUserMessage("Test"),
            new LlmTextMessage("Hello world")
        };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Act
        await _service.SendMessageStreamingAsync("Test");

        // Wait for potential fire-and-forget auto-save
        await Task.Delay(200);

        // Assert
        _mockHistoryManager.Verify(
            h => h.SaveCurrentConversationAsync(
                conversationId,
                It.Is<IReadOnlyList<IMessage>>(m => m.Count == 2)
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task AutoSave_SaveFails_DoesNotThrow()
    {
        // Arrange
        _mockHistoryManager
            .Setup(h => h.SaveCurrentConversationAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<IMessage>>()
            ))
            .ThrowsAsync(new IOException("Save failed"));

        var mockMessage = new LlmTextMessage("Response");
        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessage);

        var messages = new List<IMessage> { new DirectUserMessage("Hello"), mockMessage };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Act & Assert - should not throw despite auto-save failure
        await _service.SendMessageAsync("Hello");

        // Wait for fire-and-forget auto-save to fail
        await Task.Delay(200);

        // No exception should be thrown - failure is logged
    }

    [TestMethod]
    public async Task AutoSave_SkipsEmptyConversation()
    {
        // Arrange
        var mockMessage = new LlmTextMessage("Response");
        _mockOrchestrator.Setup(x => x.ProcessUserInputAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMessage);

        // Return empty message list
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(new List<IMessage>().AsReadOnly());

        // Act
        await _service.SendMessageAsync("Hello");

        // Wait for potential auto-save
        await Task.Delay(200);

        // Assert - auto-save should not be called for empty conversations
        _mockHistoryManager.Verify(
            h => h.SaveCurrentConversationAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<IMessage>>()
            ),
            Times.Never
        );
    }

    [TestMethod]
    public async Task SendMessageStreamingAsync_MultipleToolCalls_PlaceholderContentDoesNotAccumulate()
    {
        // Arrange: Simulate two sequential LLM responses with tool calls (error recovery scenario)
        // This tests that streaming placeholders don't accumulate content across tool call cycles

        var streamingChunks = new[]
        {
            // First LLM response with text before tool call
            new StreamingResponseChunk("First response text", false, StreamingStatus.Streaming),
            new StreamingResponseChunk(null, false, StreamingStatus.ExecutingTools),

            // Second LLM response (after tool execution) with different text
            new StreamingResponseChunk("Second response text", false, StreamingStatus.Streaming),
            new StreamingResponseChunk(null, true, StreamingStatus.Completed)
        };

        // Use AsyncEnumerableWithDelay to space chunks by 35ms to allow throttling (30ms threshold) to pass
        _mockOrchestrator.Setup(x => x.ProcessUserInputStreamingAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerableWithDelay(streamingChunks, 35));

        // Setup conversation manager to return appropriate messages
        var messages = new List<IMessage>
        {
            new DirectUserMessage("test"),
            new LlmToolCallMessage("First response text", new List<ToolCall>
            {
                new ToolCall("tool1", "test_tool", "{}")
            }, null),
            new ToolResultMessage("tool1", "test_tool", "result"),
            new LlmTextMessage("Second response text")
        };
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(messages.AsReadOnly());

        // Track streaming updates to verify content
        var streamingUpdates = new List<(Guid MessageId, string Content)>();
        _service.StreamingMessageUpdated += (s, e) => streamingUpdates.Add((e.MessageId, e.Content));

        // Act
        await _service.SendMessageStreamingAsync("test message");

        // Assert: Verify that second streaming placeholder does NOT contain first response text
        // Group updates by message ID to track each streaming placeholder separately
        var updatesByMessage = streamingUpdates.GroupBy(u => u.MessageId).ToList();

        // Should have updates for at least 2 different streaming placeholders
        // (one before first tool call, one after first tool execution)
        Assert.IsTrue(updatesByMessage.Count >= 2,
            $"Expected at least 2 streaming placeholders, got {updatesByMessage.Count}");

        // Get the second streaming placeholder's updates (after first tool execution)
        var secondPlaceholderUpdates = updatesByMessage
            .Skip(1)  // Skip first placeholder
            .FirstOrDefault();

        Assert.IsNotNull(secondPlaceholderUpdates, "Should have a second streaming placeholder");

        // Verify that second placeholder's content does NOT contain first response text
        foreach (var (_, content) in secondPlaceholderUpdates)
        {
            Assert.IsFalse(content.Contains("First response text"),
                $"Second streaming placeholder should not contain 'First response text', but content was: '{content}'");

            // It should only contain second response text
            if (content.Contains("Second"))
            {
                Assert.IsTrue(content.Contains("Second response text") || content.StartsWith("Second"),
                    "Second placeholder should contain 'Second response text'");
            }
        }
    }

    // Helper method to create async enumerable for streaming tests
    private static async IAsyncEnumerable<T> AsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    // Helper method to create async enumerable with delays between items (for throttling tests)
    private static async IAsyncEnumerable<T> AsyncEnumerableWithDelay<T>(IEnumerable<T> items, int delayMs)
    {
        foreach (var item in items)
        {
            await Task.Delay(delayMs);
            yield return item;
        }
    }
}
