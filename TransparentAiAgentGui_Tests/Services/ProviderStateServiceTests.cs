using Moq;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentGui.Services;

namespace TransparentAiAgentGui.Tests.Services;

/// <summary>
/// Tests for ProviderStateService following Lean TDD principles.
/// Tests meaningful behavior: delegating to manager, event firing, validation.
/// </summary>
[TestClass]
public class ProviderStateServiceTests
{
    private Mock<ILLMProviderManager> _mockManager = null!;
    private ProviderStateService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockManager = new Mock<ILLMProviderManager>();
        _service = new ProviderStateService(_mockManager.Object);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_NullManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new ProviderStateService(null!));
    }

    #endregion

    #region GetAvailableProviders Tests

    [TestMethod]
    public void GetAvailableProviders_ReturnsProvidersFromManager()
    {
        // Arrange
        var expectedProviders = new List<ProviderInfo>
        {
            new ProviderInfo("claude-fast", "Claude Fast", "Anthropic", "claude-haiku", true),
            new ProviderInfo("azure-gpt4", "Azure GPT-4", "AzureOpenAI", "gpt-4", false)
        };

        _mockManager.Setup(m => m.GetAvailableProviders())
            .Returns(expectedProviders);

        // Act
        var result = _service.GetAvailableProviders();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("claude-fast", result[0].ConfigName);
        Assert.AreEqual("azure-gpt4", result[1].ConfigName);
        _mockManager.Verify(m => m.GetAvailableProviders(), Times.Once);
    }

    [TestMethod]
    public void GetAvailableProviders_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        _mockManager.Setup(m => m.GetAvailableProviders())
            .Returns(new List<ProviderInfo>());

        // Act
        var result = _service.GetAvailableProviders();

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region GetCurrentProvider Tests

    [TestMethod]
    public void GetCurrentProvider_ReturnsCurrentProviderFromManager()
    {
        // Arrange
        var expectedProvider = new ProviderInfo(
            "claude-fast", "Claude Fast", "Anthropic", "claude-haiku", true);

        _mockManager.Setup(m => m.GetCurrentProviderInfo())
            .Returns(expectedProvider);

        // Act
        var result = _service.GetCurrentProvider();

        // Assert
        Assert.AreEqual("claude-fast", result.ConfigName);
        Assert.AreEqual("Claude Fast", result.DisplayName);
        Assert.IsTrue(result.IsActive);
        _mockManager.Verify(m => m.GetCurrentProviderInfo(), Times.Once);
    }

    #endregion

    #region ChangeProviderAsync Tests

    [TestMethod]
    public async Task ChangeProviderAsync_ValidProvider_CallsManagerAndFiresEvent()
    {
        // Arrange
        bool eventFired = false;
        EventArgs? capturedArgs = null;
        _service.ProviderChanged += (sender, args) =>
        {
            eventFired = true;
            capturedArgs = args;
        };

        _mockManager.Setup(m => m.SetActiveProviderAsync("azure-gpt4"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ChangeProviderAsync("azure-gpt4");

        // Assert
        Assert.IsTrue(eventFired);
        Assert.IsNotNull(capturedArgs);
        _mockManager.Verify(m => m.SetActiveProviderAsync("azure-gpt4"), Times.Once);
    }

    [TestMethod]
    public async Task ChangeProviderAsync_NullConfigName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _service.ChangeProviderAsync(null!));
    }

    [TestMethod]
    public async Task ChangeProviderAsync_EmptyConfigName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _service.ChangeProviderAsync(string.Empty));
    }

    [TestMethod]
    public async Task ChangeProviderAsync_WhitespaceConfigName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _service.ChangeProviderAsync("   "));
    }

    [TestMethod]
    public async Task ChangeProviderAsync_ManagerThrows_PropagatesException()
    {
        // Arrange
        _mockManager.Setup(m => m.SetActiveProviderAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Provider not found"));

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            _service.ChangeProviderAsync("nonexistent"));

        Assert.AreEqual("Provider not found", ex.Message);
    }

    [TestMethod]
    public async Task ChangeProviderAsync_NoSubscribers_DoesNotThrow()
    {
        // Arrange - No event subscribers
        _mockManager.Setup(m => m.SetActiveProviderAsync("azure-gpt4"))
            .Returns(Task.CompletedTask);

        // Act & Assert - Should not throw
        await _service.ChangeProviderAsync("azure-gpt4");

        _mockManager.Verify(m => m.SetActiveProviderAsync("azure-gpt4"), Times.Once);
    }

    #endregion

    #region Event Tests

    [TestMethod]
    public async Task ProviderChanged_MultipleSubscribers_AllNotified()
    {
        // Arrange
        int subscriber1Count = 0;
        int subscriber2Count = 0;

        _service.ProviderChanged += (sender, args) => subscriber1Count++;
        _service.ProviderChanged += (sender, args) => subscriber2Count++;

        _mockManager.Setup(m => m.SetActiveProviderAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ChangeProviderAsync("azure-gpt4");

        // Assert
        Assert.AreEqual(1, subscriber1Count);
        Assert.AreEqual(1, subscriber2Count);
    }

    [TestMethod]
    public async Task ProviderChanged_SenderIsService()
    {
        // Arrange
        object? capturedSender = null;
        _service.ProviderChanged += (sender, args) => capturedSender = sender;

        _mockManager.Setup(m => m.SetActiveProviderAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ChangeProviderAsync("azure-gpt4");

        // Assert
        Assert.AreSame(_service, capturedSender);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public async Task MultipleProviderChanges_FiresEventEachTime()
    {
        // Arrange
        int eventCount = 0;
        _service.ProviderChanged += (sender, args) => eventCount++;

        _mockManager.Setup(m => m.SetActiveProviderAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ChangeProviderAsync("claude-fast");
        await _service.ChangeProviderAsync("azure-gpt4");
        await _service.ChangeProviderAsync("claude-thinking");

        // Assert
        Assert.AreEqual(3, eventCount);
        _mockManager.Verify(m => m.SetActiveProviderAsync(It.IsAny<string>()), Times.Exactly(3));
    }

    #endregion
}
