using Microsoft.Extensions.Logging;
using Moq;
using TransparentAiAgentCore.Application.ConversationHistory;
using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Application.ConversationHistory;

[TestClass]
public class ConversationHistoryManagerTests
{
    private Mock<IConversationRepository> _mockRepository;
    private Mock<ILogger<ConversationHistoryManager>> _mockLogger;
    private ConversationHistoryManager _manager;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IConversationRepository>();
        _mockLogger = new Mock<ILogger<ConversationHistoryManager>>();
        _manager = new ConversationHistoryManager(_mockRepository.Object, _mockLogger.Object);
    }

    #region SaveCurrentConversationAsync Tests

    [TestMethod]
    public async Task SaveCurrentConversationAsync_EmptyMessages_SkipsSave()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage>();

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages);

        // Assert
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_WithMessages_CallsRepository()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new DirectUserMessage("Test") };

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages);

        // Assert
        _mockRepository.Verify(
            r => r.SaveAsync(It.Is<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>(c =>
                c.ConversationId == conversationId &&
                c.Messages.Count == 1
            )),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_NewConversation_GeneratesName()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new DirectUserMessage("What is clean architecture?") };

        _mockRepository.Setup(r => r.ExistsAsync(conversationId))
            .ReturnsAsync(false);

        TransparentAiAgentCore.Domain.ConversationHistory.Conversation? savedConversation = null;
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>()))
            .Callback<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>(c => savedConversation = c)
            .Returns(Task.CompletedTask);

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages);

        // Assert
        Assert.IsNotNull(savedConversation);
        Assert.AreEqual("What is clean architecture_", savedConversation.Name); // ? is sanitized to _
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>()), Times.Once);
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_ExistingConversation_PreservesCreatedAt()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new DirectUserMessage("Test") };
        var originalCreatedAt = DateTime.UtcNow.AddDays(-7);

        var existingConversation = new TransparentAiAgentCore.Domain.ConversationHistory.Conversation
        {
            ConversationId = conversationId,
            Name = "Existing Name",
            CreatedAt = originalCreatedAt,
            LastModifiedAt = DateTime.UtcNow.AddDays(-7),
            Messages = new List<IMessage>()
        };

        _mockRepository.Setup(r => r.ExistsAsync(conversationId))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.LoadAsync(conversationId))
            .ReturnsAsync(existingConversation);

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages);

        // Assert
        _mockRepository.Verify(
            r => r.SaveAsync(It.Is<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>(c =>
                c.CreatedAt == originalCreatedAt
            )),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_ExistingConversation_PreservesName()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new DirectUserMessage("New message") };

        var existingConversation = new TransparentAiAgentCore.Domain.ConversationHistory.Conversation
        {
            ConversationId = conversationId,
            Name = "Original Name",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            LastModifiedAt = DateTime.UtcNow.AddDays(-7),
            Messages = new List<IMessage>()
        };

        _mockRepository.Setup(r => r.ExistsAsync(conversationId))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.LoadAsync(conversationId))
            .ReturnsAsync(existingConversation);

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages);

        // Assert
        _mockRepository.Verify(
            r => r.SaveAsync(It.Is<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>(c =>
                c.Name == "Original Name"
            )),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_SaveFails_DoesNotThrow()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new DirectUserMessage("Test") };

        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>()))
            .ThrowsAsync(new IOException("Disk full"));

        // Act - should not throw
        await _manager.SaveCurrentConversationAsync(conversationId, messages);

        // Assert - if we get here, test passed (no exception thrown)
    }

    #endregion

    #region LoadConversationAsync Tests

    [TestMethod]
    public async Task LoadConversationAsync_CallsRepository()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var expectedConversation = new TransparentAiAgentCore.Domain.ConversationHistory.Conversation
        {
            ConversationId = conversationId,
            Name = "Test Conversation"
        };
        _mockRepository.Setup(r => r.LoadAsync(conversationId))
            .ReturnsAsync(expectedConversation);

        // Act
        var result = await _manager.LoadConversationAsync(conversationId);

        // Assert
        Assert.AreEqual(conversationId, result.ConversationId);
        _mockRepository.Verify(r => r.LoadAsync(conversationId), Times.Once);
    }

    #endregion

    #region CreateNewConversationAsync Tests

    [TestMethod]
    public async Task CreateNewConversationAsync_ReturnsNewGuid()
    {
        // Act
        var id1 = await _manager.CreateNewConversationAsync();
        var id2 = await _manager.CreateNewConversationAsync();

        // Assert
        Assert.AreNotEqual(Guid.Empty, id1);
        Assert.AreNotEqual(Guid.Empty, id2);
        Assert.AreNotEqual(id1, id2); // Each call returns unique ID
    }

    [TestMethod]
    public async Task CreateNewConversationAsync_DoesNotSaveImmediately()
    {
        // Act
        await _manager.CreateNewConversationAsync();

        // Assert
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<TransparentAiAgentCore.Domain.ConversationHistory.Conversation>()), Times.Never);
    }

    #endregion

    #region GetConversationListAsync Tests

    [TestMethod]
    public async Task GetConversationListAsync_CallsRepository()
    {
        // Arrange
        var expectedList = new List<ConversationMetadata>
        {
            new ConversationMetadata
            {
                ConversationId = Guid.NewGuid(),
                Name = "Test 1",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                LastModifiedAt = DateTime.UtcNow
            },
            new ConversationMetadata
            {
                ConversationId = Guid.NewGuid(),
                Name = "Test 2",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedAt = DateTime.UtcNow
            }
        };
        _mockRepository.Setup(r => r.ListAllAsync())
            .ReturnsAsync(expectedList);

        // Act
        var result = await _manager.GetConversationListAsync();

        // Assert
        Assert.AreEqual(2, result.Count);
        _mockRepository.Verify(r => r.ListAllAsync(), Times.Once);
    }

    #endregion

    #region DeleteConversationAsync Tests

    [TestMethod]
    public async Task DeleteConversationAsync_CallsRepository()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        await _manager.DeleteConversationAsync(conversationId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(conversationId), Times.Once);
    }

    #endregion
}
