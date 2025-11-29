using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Infrastructure.Tools.ScenarioMock;
using Microsoft.Extensions.Logging;
using Moq;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.ScenarioMock;

/// <summary>
/// Tests for ScenarioMockToolExecutor following Lean TDD principles.
/// Testing meaningful behavior: execution logic, error handling, transparency logging, and cancellation.
/// </summary>
[TestClass]
public class ScenarioMockToolExecutorTests
{
    private Mock<IScenarioToolRegistry> _mockRegistry = null!;
    private Mock<ILogger<ScenarioMockToolExecutor>> _mockLogger = null!;
    private ScenarioMockToolExecutor _executor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockRegistry = new Mock<IScenarioToolRegistry>();
        _mockLogger = new Mock<ILogger<ScenarioMockToolExecutor>>();
        _executor = new ScenarioMockToolExecutor(_mockRegistry.Object, _mockLogger.Object);
    }

    #region Basic Execution Tests

    [TestMethod]
    public void SourceType_ReturnsScenarioMock()
    {
        // Act
        var sourceType = _executor.SourceType;

        // Assert
        Assert.AreEqual(ToolSourceType.ScenarioMock, sourceType);
    }

    [TestMethod]
    public async Task ExecuteAsync_SuccessResponse_ReturnsSuccess()
    {
        // Arrange
        var tool = CreateMockTool("test_tool");
        var mockResponse = MockToolResponse.Success("Success content");
        _mockRegistry.Setup(r => r.GetMockResponse("test_tool", "{}"))
            .Returns(mockResponse);

        // Act
        var result = await _executor.ExecuteAsync(tool, "{}", CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess, "Execution should succeed");
        Assert.AreEqual("Success content", result.Content);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsync_ErrorResponse_ReturnsFailure()
    {
        // Arrange
        var tool = CreateMockTool("test_tool");
        var mockResponse = MockToolResponse.Error("Error message");
        _mockRegistry.Setup(r => r.GetMockResponse("test_tool", "{}"))
            .Returns(mockResponse);

        // Act
        var result = await _executor.ExecuteAsync(tool, "{}", CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Execution should fail");
        Assert.IsNotNull(result.ErrorMessage);
        Assert.AreEqual("Error message", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsync_SimulatedDelay_DelaysExecution()
    {
        // Arrange
        var tool = CreateMockTool("test_tool");
        var delay = TimeSpan.FromMilliseconds(100);
        var mockResponse = MockToolResponse.Success("Content", delay);
        _mockRegistry.Setup(r => r.GetMockResponse("test_tool", "{}"))
            .Returns(mockResponse);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _executor.ExecuteAsync(tool, "{}", CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 100,
            $"Execution should take at least 100ms, but took {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public async Task ExecuteAsync_NoMockResponse_ReturnsFailure()
    {
        // Arrange
        var tool = CreateMockTool("test_tool");
        _mockRegistry.Setup(r => r.GetMockResponse("test_tool", "{}"))
            .Returns((MockToolResponse?)null);

        // Act
        var result = await _executor.ExecuteAsync(tool, "{}", CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should fail when no mock response is found");
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("No mock response defined"),
            $"Error message should indicate missing mock response: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task ExecuteAsync_NoSimulatedDelay_ReturnsImmediately()
    {
        // Arrange
        var tool = CreateMockTool("test_tool");
        var mockResponse = MockToolResponse.Success("Content", null); // No delay
        _mockRegistry.Setup(r => r.GetMockResponse("test_tool", "{}"))
            .Returns(mockResponse);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _executor.ExecuteAsync(tool, "{}", CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50,
            $"Execution should be fast (<50ms), but took {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Validation Tests

    [TestMethod]
    public async Task ExecuteAsync_NullTool_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            _executor.ExecuteAsync(null!, "{}", CancellationToken.None));
    }

    [TestMethod]
    public async Task ExecuteAsync_WrongSourceType_ThrowsArgumentException()
    {
        // Arrange - Create tool with wrong source type
        var wrongTool = new TestTool("wrong_tool", ToolSourceType.BuiltInUIControl);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _executor.ExecuteAsync(wrongTool, "{}", CancellationToken.None));
    }

    #endregion

    #region Cancellation Tests

    [TestMethod]
    public async Task ExecuteAsync_Cancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        var tool = CreateMockTool("test_tool");
        var delay = TimeSpan.FromSeconds(10); // Long delay
        var mockResponse = MockToolResponse.Success("Content", delay);
        _mockRegistry.Setup(r => r.GetMockResponse("test_tool", "{}"))
            .Returns(mockResponse);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        // Act & Assert - TaskCanceledException is thrown by Task.Delay when cancelled
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
            _executor.ExecuteAsync(tool, "{}", cts.Token));
    }

    #endregion

    #region Helper Methods

    private ScenarioMockTool CreateMockTool(string name)
    {
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("default") }
        };

        return new ScenarioMockTool(
            name: name,
            description: "Test tool",
            parametersSchema: "{}",
            responseMap: responseMap);
    }

    /// <summary>
    /// Test tool implementation for testing wrong source type scenario.
    /// </summary>
    private class TestTool : ITool
    {
        public TestTool(string name, ToolSourceType sourceType)
        {
            Name = name;
            SourceType = sourceType;
        }

        public string Name { get; }
        public string Description => "Test tool";
        public string ParametersSchema => "{}";
        public ToolSourceType SourceType { get; }
        public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>();
    }

    #endregion
}
