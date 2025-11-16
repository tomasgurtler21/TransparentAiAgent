using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.BuiltInLongTermMemory;

/// <summary>
/// Tests for LongTermMemoryToolExecutor following Lean TDD principles.
/// Testing meaningful behavior: tool routing, argument parsing, service integration, error handling.
/// </summary>
[TestClass]
public class LongTermMemoryToolExecutorTests
{
    private Mock<ILongTermMemoryService> _mockMemoryService = null!;
    private Mock<IAppModeService> _mockAppModeService = null!;
    private Mock<ILogger<LongTermMemoryToolExecutor>> _mockLogger = null!;
    private LongTermMemoryToolExecutor _executor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockMemoryService = new Mock<ILongTermMemoryService>();
        _mockAppModeService = new Mock<IAppModeService>();
        _mockLogger = new Mock<ILogger<LongTermMemoryToolExecutor>>();

        _executor = new LongTermMemoryToolExecutor(
            _mockMemoryService.Object,
            _mockAppModeService.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public void SourceType_ReturnsBuiltInLongTermMemory()
    {
        // Act
        var sourceType = _executor.SourceType;

        // Assert
        Assert.AreEqual(ToolSourceType.BuiltInLongTermMemory, sourceType);
    }

    [TestMethod]
    public async Task ExecuteAsync_ReadTool_ReturnsMemoryContent()
    {
        // Arrange
        var tool = new LongTermMemoryReadTool();
        var arguments = "{}";
        var memoryContent = "# User Preferences\n- Likes concise responses\n- Prefers examples";

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
        _mockMemoryService
            .Setup(x => x.ReadMemoryAsync(AppMode.Normal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryContent);

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(memoryContent, result.Content);
        _mockMemoryService.Verify(x => x.ReadMemoryAsync(
            AppMode.Normal,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ReadTool_EmptyMemory_ReturnsHelpfulMessage()
    {
        // Arrange
        var tool = new LongTermMemoryReadTool();
        var arguments = "{}";

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
        _mockMemoryService
            .Setup(x => x.ReadMemoryAsync(AppMode.Normal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Memory does not exist yet. It will be created when you write to it for the first time using the long_term_memory_update tool.", result.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_UpdateTool_ValidContent_UpdatesMemory()
    {
        // Arrange
        var tool = new LongTermMemoryUpdateTool();
        var content = "# Updated Memory\n- New preference";
        var reason = "User shared preferences";
        var arguments = JsonSerializer.Serialize(new { content, reason });

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Teaching);
        _mockMemoryService
            .Setup(x => x.UpdateMemoryAsync(
                AppMode.Teaching,
                content,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryUpdateResult(
                Success: true,
                CharacterCount: content.Length,
                UpdatedAt: DateTime.UtcNow));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Content.Contains("successfully updated"));
        Assert.IsTrue(result.Content.Contains(content.Length.ToString()));
        _mockMemoryService.Verify(x => x.UpdateMemoryAsync(
            AppMode.Teaching,
            content,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_UpdateTool_ServiceFailure_ReturnsFailure()
    {
        // Arrange
        var tool = new LongTermMemoryUpdateTool();
        var content = new string('x', 15000); // Exceeds limit
        var arguments = JsonSerializer.Serialize(new
        {
            content,
            reason = "Testing size limit"
        });

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
        _mockMemoryService
            .Setup(x => x.UpdateMemoryAsync(
                AppMode.Normal,
                content,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryUpdateResult(
                Success: false,
                Error: "Content exceeds maximum size"));

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage?.Contains("exceeds maximum size") ?? false);
    }

    [TestMethod]
    public async Task ExecuteAsync_UpdateTool_MissingContent_ReturnsFailure()
    {
        // Arrange
        var tool = new LongTermMemoryUpdateTool();
        var arguments = JsonSerializer.Serialize(new { reason = "Missing content parameter" });

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage?.Contains("content") ?? false,
            "Error should mention missing 'content' parameter");
    }

    [TestMethod]
    public async Task ExecuteAsync_UpdateTool_MissingReason_ReturnsFailure()
    {
        // Arrange
        var tool = new LongTermMemoryUpdateTool();
        var arguments = JsonSerializer.Serialize(new { content = "Some content" });

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage?.Contains("reason") ?? false,
            "Error should mention missing 'reason' parameter");
    }

    [TestMethod]
    public async Task ExecuteAsync_UnknownTool_ReturnsFailure()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("unknown_memory_tool");
        mockTool.Setup(x => x.SourceType).Returns(ToolSourceType.BuiltInLongTermMemory);
        var arguments = "{}";

        // Act
        var result = await _executor.ExecuteAsync(mockTool.Object, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage?.Contains("Unknown tool") ?? false);
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange
        var tool = new LongTermMemoryUpdateTool();
        var arguments = "{ invalid json }";

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage?.Contains("JSON") ?? false);
    }

    [TestMethod]
    public async Task ExecuteAsync_RecordsExecutionTime()
    {
        // Arrange
        var tool = new LongTermMemoryReadTool();
        var arguments = "{}";

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
        _mockMemoryService
            .Setup(x => x.ReadMemoryAsync(AppMode.Normal, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test memory");

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.ExecutionTime >= TimeSpan.Zero);
        Assert.IsTrue(result.ExecutionTime < TimeSpan.FromSeconds(1),
            "Execution should be fast for file-based operations");
    }

    [TestMethod]
    public async Task ExecuteAsync_ReadTool_UsesCurrentMode()
    {
        // Arrange
        var tool = new LongTermMemoryReadTool();
        var arguments = "{}";

        _mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Teaching);
        _mockMemoryService
            .Setup(x => x.ReadMemoryAsync(AppMode.Teaching, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Teaching mode memory");

        // Act
        var result = await _executor.ExecuteAsync(tool, arguments);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockMemoryService.Verify(x => x.ReadMemoryAsync(
            AppMode.Teaching,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
