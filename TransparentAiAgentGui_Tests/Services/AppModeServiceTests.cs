using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Teaching;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Configuration;
using TransparentAiAgentGui.Services;

namespace TransparentAiAgentGui_Tests.Services;

[TestClass]
public class AppModeServiceTests
{
    private Mock<IUIControlService> _mockUIControlService = null!;
    private Mock<IConfigurationService> _mockConfigService = null!;
    private Mock<IConversationManager> _mockConversationManager = null!;
    private AppConfiguration _appConfiguration = null!;
    private TeachingModePromptBuilder _promptBuilder = null!;
    private Mock<ILongTermMemoryService> _mockMemoryService = null!;
    private Mock<IUserSettingsService> _mockUserSettingsService = null!;
    private Mock<ILogger<AppModeService>> _mockLogger = null!;
    private Mock<IKnowledgeLibrary> _mockKnowledgeLibrary = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUIControlService = new Mock<IUIControlService>();
        _mockConfigService = new Mock<IConfigurationService>();
        _mockConversationManager = new Mock<IConversationManager>();
        _appConfiguration = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "Test system prompt"
            }
        };
        _mockKnowledgeLibrary = new Mock<IKnowledgeLibrary>();
        _promptBuilder = new TeachingModePromptBuilder(_mockKnowledgeLibrary.Object);
        _mockMemoryService = new Mock<ILongTermMemoryService>();
        _mockUserSettingsService = new Mock<IUserSettingsService>();
        _mockLogger = new Mock<ILogger<AppModeService>>();

        // Setup default UI control state
        _mockUIControlService.Setup(x => x.GetCurrentState())
            .Returns(UIState.DefaultNormalMode());
    }

    [TestMethod]
    public async Task SwitchModeAsync_ToTeachingWithMemoryDisabled_DoesNotLoadMemory()
    {
        // Arrange
        var userSettings = new UserSettings { EnableMemory = false };
        _mockUserSettingsService.Setup(x => x.GetCurrentSettings())
            .Returns(userSettings);

        _mockUIControlService.Setup(x => x.SwitchMode(AppMode.Teaching))
            .Returns(Result<UIState>.Ok(UIState.DefaultTeachingMode()));

        var service = CreateService();

        // Act
        await service.SwitchModeAsync(AppMode.Teaching);

        // Assert - Memory service should NOT be called when memory is disabled
        _mockMemoryService.Verify(
            x => x.ReadMemoryAsync(AppMode.Teaching, It.IsAny<CancellationToken>()),
            Times.Never,
            "Memory should not be read when EnableMemory is false");
    }

    [TestMethod]
    public async Task SwitchModeAsync_ToTeachingWithMemoryEnabled_LoadsMemory()
    {
        // Arrange
        var userSettings = new UserSettings { EnableMemory = true };
        var memoryContent = "# User Preferences\n- Likes concise answers";

        _mockUserSettingsService.Setup(x => x.GetCurrentSettings())
            .Returns(userSettings);

        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Teaching, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryContent);

        _mockUIControlService.Setup(x => x.SwitchMode(AppMode.Teaching))
            .Returns(Result<UIState>.Ok(UIState.DefaultTeachingMode()));

        var service = CreateService();

        // Act
        await service.SwitchModeAsync(AppMode.Teaching);

        // Assert - Memory service should be called when memory is enabled
        _mockMemoryService.Verify(
            x => x.ReadMemoryAsync(AppMode.Teaching, It.IsAny<CancellationToken>()),
            Times.Once,
            "Memory should be read when EnableMemory is true");
    }

    [TestMethod]
    public async Task SwitchModeAsync_ToTeachingWithMemoryEnabledButEmpty_DoesNotInjectMemory()
    {
        // Arrange
        var userSettings = new UserSettings { EnableMemory = true };
        var emptyMemory = string.Empty;

        _mockUserSettingsService.Setup(x => x.GetCurrentSettings())
            .Returns(userSettings);

        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Teaching, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyMemory);

        _mockUIControlService.Setup(x => x.SwitchMode(AppMode.Teaching))
            .Returns(Result<UIState>.Ok(UIState.DefaultTeachingMode()));

        string? capturedPrompt = null;
        _mockConversationManager.Setup(x => x.UpdateSystemPrompt(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt);

        var service = CreateService();

        // Act
        await service.SwitchModeAsync(AppMode.Teaching);

        // Assert - System prompt should NOT contain memory section when memory is empty
        Assert.IsNotNull(capturedPrompt, "System prompt should have been updated");
        Assert.IsFalse(capturedPrompt.Contains("# LONG-TERM MEMORY"),
            "System prompt should not contain memory section when memory is empty");
    }

    [TestMethod]
    public async Task SwitchModeAsync_ToTeachingWithMemoryEnabledAndContent_InjectsMemory()
    {
        // Arrange
        var userSettings = new UserSettings { EnableMemory = true };
        var memoryContent = "# User Preferences\n- Likes concise answers";

        _mockUserSettingsService.Setup(x => x.GetCurrentSettings())
            .Returns(userSettings);

        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Teaching, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryContent);

        _mockUIControlService.Setup(x => x.SwitchMode(AppMode.Teaching))
            .Returns(Result<UIState>.Ok(UIState.DefaultTeachingMode()));

        string? capturedPrompt = null;
        _mockConversationManager.Setup(x => x.UpdateSystemPrompt(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt);

        var service = CreateService();

        // Act
        await service.SwitchModeAsync(AppMode.Teaching);

        // Assert - System prompt should contain memory section with content
        Assert.IsNotNull(capturedPrompt, "System prompt should have been updated");
        Assert.IsTrue(capturedPrompt.Contains("# LONG-TERM MEMORY"),
            "System prompt should contain memory section header");
        Assert.IsTrue(capturedPrompt.Contains(memoryContent),
            "System prompt should contain the actual memory content");
    }

    [TestMethod]
    public async Task SwitchModeAsync_ToNormalMode_UsesConfiguredPromptWithoutMemory()
    {
        // Arrange - Start in Teaching mode to test switch to Normal
        _mockUIControlService.Setup(x => x.GetCurrentState())
            .Returns(UIState.DefaultTeachingMode());

        var userSettings = new UserSettings { EnableMemory = false };

        _mockUserSettingsService.Setup(x => x.GetCurrentSettings())
            .Returns(userSettings);

        _mockUIControlService.Setup(x => x.SwitchMode(AppMode.Normal))
            .Returns(Result<UIState>.Ok(UIState.DefaultNormalMode()));

        string? capturedPrompt = null;
        _mockConversationManager.Setup(x => x.UpdateSystemPrompt(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt);

        var service = CreateService();

        // Act
        await service.SwitchModeAsync(AppMode.Normal);

        // Assert - Should use the configured system prompt
        Assert.IsNotNull(capturedPrompt, "System prompt should have been updated");
        Assert.AreEqual(_appConfiguration.Agent.SystemPrompt, capturedPrompt,
            "Normal mode should use the configured system prompt");
    }

    [TestMethod]
    public async Task SwitchModeAsync_MemoryServiceThrowsException_ContinuesWithoutMemory()
    {
        // Arrange
        var userSettings = new UserSettings { EnableMemory = true };

        _mockUserSettingsService.Setup(x => x.GetCurrentSettings())
            .Returns(userSettings);

        _mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Teaching, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Failed to read memory file"));

        _mockUIControlService.Setup(x => x.SwitchMode(AppMode.Teaching))
            .Returns(Result<UIState>.Ok(UIState.DefaultTeachingMode()));

        string? capturedPrompt = null;
        _mockConversationManager.Setup(x => x.UpdateSystemPrompt(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt);

        var service = CreateService();

        // Act - Should not throw, should continue without memory
        await service.SwitchModeAsync(AppMode.Teaching);

        // Assert - System prompt should be updated even though memory read failed
        Assert.IsNotNull(capturedPrompt, "System prompt should have been updated despite memory error");
        Assert.IsFalse(capturedPrompt.Contains("# LONG-TERM MEMORY"),
            "System prompt should not contain memory section when memory read fails");
    }

    private AppModeService CreateService()
    {
        return new AppModeService(
            _mockUIControlService.Object,
            _mockConfigService.Object,
            _mockConversationManager.Object,
            _appConfiguration,
            _promptBuilder,
            _mockMemoryService.Object,
            _mockUserSettingsService.Object,
            _mockLogger.Object);
    }
}
