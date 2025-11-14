using Moq;
using TransparentAiAgentCore.Application.Tools;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Infrastructure.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.Validation;

namespace TransparentAiAgentCore_Tests.Application.Tools;

[TestClass]
public class ToolManagerTests
{
    private Mock<IToolRegistry> _mockRegistry = null!;
    private Mock<ITransparencyService> _mockTransparency = null!;
    private Mock<IToolUsageStatistics> _mockStatistics = null!;
    private Mock<ToolSchemaValidator> _mockValidator = null!;
    private List<IToolExecutor> _executors = null!;
    private ToolManager _toolManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockRegistry = new Mock<IToolRegistry>();
        _mockTransparency = new Mock<ITransparencyService>();
        _mockStatistics = new Mock<IToolUsageStatistics>();
        _mockValidator = new Mock<ToolSchemaValidator>();
        _executors = new List<IToolExecutor>();
        _toolManager = new ToolManager(_mockRegistry.Object, _executors, _mockTransparency.Object, _mockStatistics.Object, _mockValidator.Object);
    }

    [TestMethod]
    public void ExecuteToolCallAsync_ToolNotFound_ReturnsFailureResult()
    {
        // Arrange
        var toolCall = new LLMToolCall("call-1", "nonexistent_tool", "{}");
        _mockRegistry.Setup(r => r.GetTool("nonexistent_tool")).Returns((ITool?)null);

        // Act
        var result = _toolManager.ExecuteToolCallAsync(toolCall).Result;

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("not found"));
    }

    [TestMethod]
    public void ExecuteToolCallAsync_NoExecutorForSourceType_ReturnsFailureResult()
    {
        // Arrange
        var toolCall = new LLMToolCall("call-1", "test_tool", "{}");
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns("test_tool");
        mockTool.Setup(t => t.SourceType).Returns(ToolSourceType.MCP);
        mockTool.Setup(t => t.Metadata).Returns(new Dictionary<string, string>().AsReadOnly());

        _mockRegistry.Setup(r => r.GetTool("test_tool")).Returns(mockTool.Object);
        // No executor added to _executors list

        // Act
        var result = _toolManager.ExecuteToolCallAsync(toolCall).Result;

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("No executor"));
    }

    [TestMethod]
    public void ExecuteToolCallAsync_ValidToolCall_RoutesToCorrectExecutor()
    {
        // Arrange
        var toolCall = new LLMToolCall("call-1", "test_tool", "{\"arg\": \"value\"}");
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns("test_tool");
        mockTool.Setup(t => t.SourceType).Returns(ToolSourceType.MCP);
        mockTool.Setup(t => t.Metadata).Returns(new Dictionary<string, string>().AsReadOnly());
        mockTool.Setup(t => t.ParametersSchema).Returns("{}");

        var mockExecutor = new Mock<IToolExecutor>();
        mockExecutor.Setup(e => e.SourceType).Returns(ToolSourceType.MCP);
        mockExecutor.Setup(e => e.ExecuteAsync(mockTool.Object, "{\"arg\": \"value\"}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResult.Success("result", TimeSpan.FromMilliseconds(100)));

        _mockRegistry.Setup(r => r.GetTool("test_tool")).Returns(mockTool.Object);
        _executors.Add(mockExecutor.Object);
        _mockValidator.Setup(v => v.ValidateArguments(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ValidationResult.Success());

        // Act
        var result = _toolManager.ExecuteToolCallAsync(toolCall).Result;

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("result", result.Content);
        mockExecutor.Verify(e => e.ExecuteAsync(mockTool.Object, "{\"arg\": \"value\"}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public void ExecuteToolCallAsync_SuccessfulExecution_LogsEvents()
    {
        // Arrange
        var toolCall = new LLMToolCall("call-1", "test_tool", "{}");
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns("test_tool");
        mockTool.Setup(t => t.SourceType).Returns(ToolSourceType.MCP);
        mockTool.Setup(t => t.Metadata).Returns(new Dictionary<string, string>().AsReadOnly());
        mockTool.Setup(t => t.ParametersSchema).Returns("{}");

        var mockExecutor = new Mock<IToolExecutor>();
        mockExecutor.Setup(e => e.SourceType).Returns(ToolSourceType.MCP);
        mockExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ITool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolExecutionResult.Success("result", TimeSpan.FromMilliseconds(100)));

        _mockRegistry.Setup(r => r.GetTool("test_tool")).Returns(mockTool.Object);
        _executors.Add(mockExecutor.Object);
        _mockValidator.Setup(v => v.ValidateArguments(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ValidationResult.Success());

        // Act
        var result = _toolManager.ExecuteToolCallAsync(toolCall).Result;

        // Assert
        Assert.IsTrue(result.IsSuccess);
        // Verify that transparency events were logged (at least 2: start and complete)
        _mockTransparency.Verify(t => t.LogEvent(It.IsAny<TransparencyEvent>()), Times.AtLeast(2));
    }

    [TestMethod]
    public void ExecuteToolCallAsync_ExecutorThrowsException_ReturnsFailureAndLogsError()
    {
        // Arrange
        var toolCall = new LLMToolCall("call-1", "test_tool", "{}");
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns("test_tool");
        mockTool.Setup(t => t.SourceType).Returns(ToolSourceType.MCP);
        mockTool.Setup(t => t.Metadata).Returns(new Dictionary<string, string>().AsReadOnly());

        var mockExecutor = new Mock<IToolExecutor>();
        mockExecutor.Setup(e => e.SourceType).Returns(ToolSourceType.MCP);
        mockExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ITool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tool execution failed"));

        _mockRegistry.Setup(r => r.GetTool("test_tool")).Returns(mockTool.Object);
        _executors.Add(mockExecutor.Object);

        // Act
        var result = _toolManager.ExecuteToolCallAsync(toolCall).Result;

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage); // Test meaningful behavior: error message exists, not exact wording
        // Verify error event was logged
        _mockTransparency.Verify(t => t.LogEvent(It.Is<TransparencyEvent>(e =>
            e.EventType == TransparencyEventType.Error || e.EventType == TransparencyEventType.ToolResult)),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public void GetLLMToolDefinitions_ReturnsToolsFromRegistry()
    {
        // Arrange
        var mockTool1 = new Mock<ITool>();
        mockTool1.Setup(t => t.Name).Returns("tool1");
        mockTool1.Setup(t => t.Description).Returns("Tool 1 description");
        mockTool1.Setup(t => t.ParametersSchema).Returns("{\"type\": \"object\"}");

        var mockTool2 = new Mock<ITool>();
        mockTool2.Setup(t => t.Name).Returns("tool2");
        mockTool2.Setup(t => t.Description).Returns("Tool 2 description");
        mockTool2.Setup(t => t.ParametersSchema).Returns("{\"type\": \"object\"}");

        _mockRegistry.Setup(r => r.GetAllTools()).Returns(new List<ITool> { mockTool1.Object, mockTool2.Object });

        // Act
        var llmTools = _toolManager.GetLLMToolDefinitions();

        // Assert
        Assert.AreEqual(2, llmTools.Count);
        Assert.AreEqual("tool1", llmTools[0].Name);
        Assert.AreEqual("tool2", llmTools[1].Name);
    }

    [TestMethod]
    public void Registry_ReturnsInjectedRegistry()
    {
        // Act
        var registry = _toolManager.Registry;

        // Assert
        Assert.AreSame(_mockRegistry.Object, registry);
    }
}
