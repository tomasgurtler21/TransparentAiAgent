using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.ConversationHistory;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentGui.Services;

namespace TransparentAiAgentGui_Tests.Services;

/// <summary>
/// Tests for long-term memory functionality in ConversationUIService
/// </summary>
[TestClass]
public class ConversationUIServiceMemoryTests
{
    private Mock<IAgentOrchestrator> _mockOrchestrator = null!;
    private Mock<IConversationManager> _mockConversationManager = null!;
    private Mock<IScenarioExecutor> _mockScenarioExecutor = null!;
    private Mock<IConversationHistoryManager> _mockHistoryManager = null!;
    private Mock<ILongTermMemoryService> _mockMemoryService = null!;
    private Mock<IAppModeService> _mockAppModeService = null!;
    private LongTermMemoryConfiguration _memoryConfig = null!;
    private ConversationUIService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOrchestrator = new Mock<IAgentOrchestrator>();
        _mockConversationManager = new Mock<IConversationManager>();
        _mockScenarioExecutor = new Mock<IScenarioExecutor>();
        _mockHistoryManager = new Mock<IConversationHistoryManager>();
        _mockMemoryService = new Mock<ILongTermMemoryService>();
        _mockAppModeService = new Mock<IAppModeService>();

        // Default configuration
        _memoryConfig = new LongTermMemoryConfiguration
        {
            Enabled = true,
            AutoLoadOnStart = true,
            PromptUpdateOnEnd = true,
            UpdatePromptTimeoutSeconds = 30
        };

        // Setup default returns
        _mockConversationManager.Setup(x => x.GetAllMessages())
            .Returns(new List<IMessage>().AsReadOnly());
        _mockConversationManager.Setup(x => x.GetInContextMessages())
            .Returns(new List<IMessage>().AsReadOnly());
        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);

        // Create service with memory support
        _service = new ConversationUIService(
            _mockOrchestrator.Object,
            _mockConversationManager.Object,
            _mockScenarioExecutor.Object,
            _mockHistoryManager.Object,
            _mockMemoryService.Object,
            _mockAppModeService.Object,
            _memoryConfig);
    }

    [TestMethod]
    public async Task SetMemoryEnabledAsync_EnablesMemory_SetsIsMemoryEnabledTrue()
    {
        // Arrange
        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Normal, default))
            .ReturnsAsync(string.Empty);

        // Act
        await _service.SetMemoryEnabledAsync(true);

        // Assert
        Assert.IsTrue(_service.IsMemoryEnabled);
    }

    [TestMethod]
    public async Task SetMemoryEnabledAsync_DisablesMemory_SetsIsMemoryEnabledFalse()
    {
        // Arrange
        await _service.SetMemoryEnabledAsync(true);

        // Act
        await _service.SetMemoryEnabledAsync(false);

        // Assert
        Assert.IsFalse(_service.IsMemoryEnabled);
    }

    [TestMethod]
    public async Task SetMemoryEnabledAsync_AutoLoadEnabled_LoadsMemoryIntoSystemPrompt()
    {
        // Arrange
        var memoryContent = "# User Profile\n- Name: Alice\n- Preference: Detailed explanations";
        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Normal, default))
            .ReturnsAsync(memoryContent);

        // Setup existing messages with a system message
        var systemMessage = new SystemMessage("You are a helpful assistant.");
        _mockConversationManager.Setup(x => x.GetInContextMessages())
            .Returns(new List<IMessage> { systemMessage }.AsReadOnly());

        // Act
        await _service.SetMemoryEnabledAsync(true);

        // Assert
        _mockMemoryService.Verify(x => x.ReadMemoryAsync(AppMode.Normal, default), Times.Once);
        _mockConversationManager.Verify(
            x => x.UpdateSystemPrompt(It.Is<string>(s => s.Contains(memoryContent))),
            Times.Once);
    }

    [TestMethod]
    public async Task SetMemoryEnabledAsync_EmptyMemory_DoesNotUpdateSystemPrompt()
    {
        // Arrange
        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Normal, default))
            .ReturnsAsync(string.Empty);

        // Act
        await _service.SetMemoryEnabledAsync(true);

        // Assert
        _mockMemoryService.Verify(x => x.ReadMemoryAsync(AppMode.Normal, default), Times.Once);
        _mockConversationManager.Verify(
            x => x.UpdateSystemPrompt(It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task SetMemoryEnabledAsync_AutoLoadDisabled_DoesNotLoadMemory()
    {
        // Arrange
        _memoryConfig.AutoLoadOnStart = false;
        var serviceWithNoAutoLoad = new ConversationUIService(
            _mockOrchestrator.Object,
            _mockConversationManager.Object,
            _mockScenarioExecutor.Object,
            _mockHistoryManager.Object,
            _mockMemoryService.Object,
            _mockAppModeService.Object,
            _memoryConfig);

        // Act
        await serviceWithNoAutoLoad.SetMemoryEnabledAsync(true);

        // Assert
        Assert.IsTrue(serviceWithNoAutoLoad.IsMemoryEnabled);
        _mockMemoryService.Verify(x => x.ReadMemoryAsync(It.IsAny<AppMode>(), default), Times.Never);
    }

    [TestMethod]
    public async Task SetMemoryEnabledAsync_DisableMemory_RestoresOriginalSystemPrompt()
    {
        // Arrange
        var originalPrompt = "You are a helpful assistant.";
        var memoryContent = "# User Profile\n- Name: Alice";
        var systemMessage = new SystemMessage(originalPrompt);

        _mockConversationManager.Setup(x => x.GetInContextMessages())
            .Returns(new List<IMessage> { systemMessage }.AsReadOnly());

        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Normal, default))
            .ReturnsAsync(memoryContent);

        // Enable memory first
        await _service.SetMemoryEnabledAsync(true);
        _mockConversationManager.Invocations.Clear(); // Clear previous calls

        // Act - Disable memory
        await _service.SetMemoryEnabledAsync(false);

        // Assert
        _mockConversationManager.Verify(
            x => x.UpdateSystemPrompt(It.Is<string>(s => s == originalPrompt && !s.Contains(memoryContent))),
            Times.Once);
    }

    [TestMethod]
    public async Task EndConversationAsync_MemoryEnabledAndPromptUpdateEnabled_SendsUpdatePrompt()
    {
        // Arrange
        await _service.SetMemoryEnabledAsync(true);

        // Setup mock to return empty async enumerable for ProcessApplicationMessageAsync
        _mockOrchestrator.Setup(x => x.ProcessApplicationMessageAsync(
                It.IsAny<ApplicationMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(GetEmptyStreamingChunks());

        // Act
        await _service.EndConversationAsync();

        // Assert
        _mockOrchestrator.Verify(
            x => x.ProcessApplicationMessageAsync(
                It.Is<ApplicationMessage>(m =>
                    m.Content.Contains("review our conversation") &&
                    m.Content.Contains("long_term_memory_update")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private async IAsyncEnumerable<StreamingResponseChunk> GetEmptyStreamingChunks()
    {
        await Task.CompletedTask;
        yield break;
    }

    [TestMethod]
    public async Task EndConversationAsync_MemoryDisabled_DoesNotSendUpdatePrompt()
    {
        // Arrange - memory not enabled

        // Act
        await _service.EndConversationAsync();

        // Assert
        _mockOrchestrator.Verify(
            x => x.ProcessApplicationMessageAsync(It.IsAny<ApplicationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task EndConversationAsync_PromptUpdateDisabled_DoesNotSendUpdatePrompt()
    {
        // Arrange
        _memoryConfig.PromptUpdateOnEnd = false;
        var serviceWithNoPrompt = new ConversationUIService(
            _mockOrchestrator.Object,
            _mockConversationManager.Object,
            _mockScenarioExecutor.Object,
            _mockHistoryManager.Object,
            _mockMemoryService.Object,
            _mockAppModeService.Object,
            _memoryConfig);

        await serviceWithNoPrompt.SetMemoryEnabledAsync(true);

        // Act
        await serviceWithNoPrompt.EndConversationAsync();

        // Assert
        _mockOrchestrator.Verify(
            x => x.ProcessApplicationMessageAsync(It.IsAny<ApplicationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ModeChanged_MemoryEnabled_LoadsNewModeMemory()
    {
        // Arrange
        var normalMemory = "# Normal Mode Memory";
        var teachingMemory = "# Teaching Mode Memory";

        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Normal, default))
            .ReturnsAsync(normalMemory);
        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Teaching, default))
            .ReturnsAsync(teachingMemory);

        var systemMessage = new SystemMessage("You are a helpful assistant.");
        _mockConversationManager.Setup(x => x.GetInContextMessages())
            .Returns(new List<IMessage> { systemMessage }.AsReadOnly());

        await _service.SetMemoryEnabledAsync(true);

        // Act - Trigger mode change
        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Teaching);
        _mockAppModeService.Raise(x => x.ModeChanged += null, _mockAppModeService.Object, AppMode.Teaching);

        // Allow async event handler to complete
        await Task.Delay(100);

        // Assert
        _mockMemoryService.Verify(x => x.ReadMemoryAsync(AppMode.Teaching, default), Times.Once);
        _mockConversationManager.Verify(
            x => x.UpdateSystemPrompt(It.Is<string>(s => s.Contains(teachingMemory))),
            Times.Once);
    }

    [TestMethod]
    public async Task ModeChanged_MemoryDisabled_DoesNotLoadMemory()
    {
        // Arrange - memory not enabled
        var systemMessage = new SystemMessage("You are a helpful assistant.");
        _mockConversationManager.Setup(x => x.GetInContextMessages())
            .Returns(new List<IMessage> { systemMessage }.AsReadOnly());

        // Act - Trigger mode change
        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Teaching);
        _mockAppModeService.Raise(x => x.ModeChanged += null, _mockAppModeService.Object, AppMode.Teaching);

        // Allow async event handler to complete
        await Task.Delay(100);

        // Assert
        _mockMemoryService.Verify(x => x.ReadMemoryAsync(It.IsAny<AppMode>(), default), Times.Never);
    }
}
