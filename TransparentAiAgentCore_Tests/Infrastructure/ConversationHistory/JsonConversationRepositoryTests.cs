using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.ConversationHistory;
using TransparentAiAgentCore.Infrastructure.DataPath;
using TransparentAiAgentCore.Infrastructure.Serialization;

namespace TransparentAiAgentCore_Tests.Infrastructure.ConversationHistory;

[TestClass]
public class JsonConversationRepositoryTests
{
    private string _testDirectory = string.Empty;
    private MessageSerializer _messageSerializer = null!;
    private ILogger<JsonConversationRepository> _logger = null!;
    private Mock<IDataPathService> _mockDataPathService = null!;
    private JsonConversationRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Setup dependencies
        _messageSerializer = new MessageSerializer();
        _logger = new Mock<ILogger<JsonConversationRepository>>().Object;

        // Setup mock DataPathService
        _mockDataPathService = new Mock<IDataPathService>();
        _mockDataPathService.Setup(x => x.GetConversationsDirectory()).Returns(_testDirectory);

        // Create repository
        _repository = new JsonConversationRepository(_messageSerializer, _logger, _mockDataPathService.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    #region SaveAsync Tests

    [TestMethod]
    public async Task SaveAsync_NewConversation_CreatesFile()
    {
        // Arrange
        var conversation = CreateTestConversation();

        // Act
        await _repository.SaveAsync(conversation);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.AreEqual(1, files.Length);
        Assert.IsTrue(files[0].Contains(conversation.ConversationId.ToString()));
    }

    [TestMethod]
    public async Task SaveAsync_UpdatesLastModifiedAt()
    {
        // Arrange
        var conversation = CreateTestConversation();
        var originalTime = conversation.LastModifiedAt;
        await Task.Delay(100); // Ensure time difference

        // Act
        await _repository.SaveAsync(conversation);

        // Assert
        Assert.IsTrue(conversation.LastModifiedAt > originalTime);
    }

    [TestMethod]
    public async Task SaveAsync_ExistingConversation_OverwritesFile()
    {
        // Arrange
        var conversation = CreateTestConversation();
        await _repository.SaveAsync(conversation);

        // Modify conversation
        conversation.Messages.Add(new DirectUserMessage("Second message"));

        // Act
        await _repository.SaveAsync(conversation);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.AreEqual(1, files.Length); // Still only one file

        // Verify content has the new message
        var loaded = await _repository.LoadAsync(conversation.ConversationId);
        Assert.AreEqual(2, loaded.Messages.Count);
    }

    [TestMethod]
    public async Task SaveAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nested");
        var mockDataPath = new Mock<IDataPathService>();
        mockDataPath.Setup(x => x.GetConversationsDirectory()).Returns(nonExistentDir);
        var repository = new JsonConversationRepository(_messageSerializer, _logger, mockDataPath.Object);
        var conversation = CreateTestConversation();

        try
        {
            // Act
            await repository.SaveAsync(conversation);

            // Assert
            Assert.IsTrue(Directory.Exists(nonExistentDir));
            var files = Directory.GetFiles(nonExistentDir, "*.json");
            Assert.AreEqual(1, files.Length);
        }
        finally
        {
            // Cleanup nested directory
            var rootTestDir = Path.Combine(Path.GetTempPath(), Path.GetFileName(Path.GetDirectoryName(nonExistentDir)!));
            if (Directory.Exists(rootTestDir))
            {
                Directory.Delete(rootTestDir, recursive: true);
            }
        }
    }

    #endregion

    #region LoadAsync Tests

    [TestMethod]
    public async Task LoadAsync_ExistingConversation_ReturnsConversation()
    {
        // Arrange
        var original = CreateTestConversation();
        await _repository.SaveAsync(original);

        // Act
        var loaded = await _repository.LoadAsync(original.ConversationId);

        // Assert
        Assert.AreEqual(original.ConversationId, loaded.ConversationId);
        Assert.AreEqual(original.Name, loaded.Name);
        Assert.AreEqual(original.Messages.Count, loaded.Messages.Count);
        Assert.AreEqual(original.Messages[0].Content, loaded.Messages[0].Content);
    }

    [TestMethod]
    public async Task LoadAsync_MissingConversation_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _repository.LoadAsync(nonExistentId)
        );
    }

    [TestMethod]
    public async Task LoadAsync_CorruptedJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var filePath = Path.Combine(_testDirectory, $"{conversationId}_test.json");

        // Write corrupted JSON
        await File.WriteAllTextAsync(filePath, "{ corrupted json content }");

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _repository.LoadAsync(conversationId)
        );
    }

    [TestMethod]
    public async Task LoadAsync_PreservesMessageTypes()
    {
        // Arrange
        var conversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            Name = "Test",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Messages = new List<IMessage>
            {
                new DirectUserMessage("User message"),
                new LlmTextMessage("Assistant message"),
                new SystemMessage("System message")
            }
        };
        await _repository.SaveAsync(conversation);

        // Act
        var loaded = await _repository.LoadAsync(conversation.ConversationId);

        // Assert
        Assert.AreEqual(3, loaded.Messages.Count);
        Assert.IsInstanceOfType(loaded.Messages[0], typeof(DirectUserMessage));
        Assert.IsInstanceOfType(loaded.Messages[1], typeof(LlmTextMessage));
        Assert.IsInstanceOfType(loaded.Messages[2], typeof(SystemMessage));
    }

    #endregion

    #region ListAllAsync Tests

    [TestMethod]
    public async Task ListAllAsync_MultipleConversations_SortsByMostRecent()
    {
        // Arrange
        var conv1 = CreateTestConversation();
        conv1.LastModifiedAt = DateTime.UtcNow.AddHours(-2);
        await _repository.SaveAsync(conv1);

        var conv2 = CreateTestConversation();
        conv2.LastModifiedAt = DateTime.UtcNow.AddHours(-1);
        await _repository.SaveAsync(conv2);

        var conv3 = CreateTestConversation();
        conv3.LastModifiedAt = DateTime.UtcNow;
        await _repository.SaveAsync(conv3);

        // Act
        var list = await _repository.ListAllAsync();

        // Assert
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(conv3.ConversationId, list[0].ConversationId); // Most recent first
        Assert.AreEqual(conv2.ConversationId, list[1].ConversationId);
        Assert.AreEqual(conv1.ConversationId, list[2].ConversationId);
    }

    [TestMethod]
    public async Task ListAllAsync_NoConversations_ReturnsEmptyList()
    {
        // Act
        var list = await _repository.ListAllAsync();

        // Assert
        Assert.IsNotNull(list);
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public async Task ListAllAsync_ExtractsMetadataWithoutLoadingMessages()
    {
        // Arrange
        var conversation = CreateTestConversation();
        conversation.Messages.Add(new DirectUserMessage("Second message"));
        conversation.Messages.Add(new LlmTextMessage("Third message"));
        await _repository.SaveAsync(conversation);

        // Act
        var list = await _repository.ListAllAsync();

        // Assert
        Assert.AreEqual(1, list.Count);
        var metadata = list[0];
        Assert.AreEqual(conversation.ConversationId, metadata.ConversationId);
        Assert.AreEqual(conversation.Name, metadata.Name);
        Assert.AreEqual(conversation.CreatedAt.ToString("o"), metadata.CreatedAt.ToString("o"));
        Assert.AreEqual(3, metadata.MessageCount);
    }

    [TestMethod]
    public async Task ListAllAsync_SkipsCorruptedFiles()
    {
        // Arrange
        var validConversation = CreateTestConversation();
        await _repository.SaveAsync(validConversation);

        // Create corrupted file
        var corruptedFilePath = Path.Combine(_testDirectory, $"{Guid.NewGuid()}_corrupted.json");
        await File.WriteAllTextAsync(corruptedFilePath, "{ corrupted json }");

        // Act
        var list = await _repository.ListAllAsync();

        // Assert - should only return the valid conversation
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(validConversation.ConversationId, list[0].ConversationId);
    }

    #endregion

    #region DeleteAsync and ExistsAsync Tests

    [TestMethod]
    public async Task DeleteAsync_ExistingConversation_RemovesFile()
    {
        // Arrange
        var conversation = CreateTestConversation();
        await _repository.SaveAsync(conversation);

        // Verify file exists
        var filesBefore = Directory.GetFiles(_testDirectory, "*.json");
        Assert.AreEqual(1, filesBefore.Length);

        // Act
        await _repository.DeleteAsync(conversation.ConversationId);

        // Assert
        var filesAfter = Directory.GetFiles(_testDirectory, "*.json");
        Assert.AreEqual(0, filesAfter.Length);
    }

    [TestMethod]
    public async Task DeleteAsync_MissingConversation_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _repository.DeleteAsync(nonExistentId)
        );
    }

    [TestMethod]
    public async Task ExistsAsync_ExistingConversation_ReturnsTrue()
    {
        // Arrange
        var conversation = CreateTestConversation();
        await _repository.SaveAsync(conversation);

        // Act
        var exists = await _repository.ExistsAsync(conversation.ConversationId);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_MissingConversation_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        Assert.IsFalse(exists);
    }

    #endregion

    #region Helper Methods

    private Conversation CreateTestConversation()
    {
        return new Conversation
        {
            ConversationId = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Messages = new List<IMessage>
            {
                new DirectUserMessage("Test message")
            }
        };
    }

    #endregion
}
