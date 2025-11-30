using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.UIControl;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentCore_Tests.Application.Scenarios;

/// <summary>
/// Tests for ScenarioExecutor wait_for_tool_call and wait_for_tool_response functionality.
/// Tests meaningful behavior: synchronization between tool execution and scenario flow.
/// </summary>
[TestClass]
public class ScenarioExecutorToolWaitTests
{
    private Mock<IAgentOrchestrator> _mockOrchestrator = null!;
    private Mock<IConfigurationOverlay> _mockConfigOverlay = null!;
    private Mock<IConversationManager> _mockConversationManager = null!;
    private Mock<IConditionEvaluator> _mockConditionEvaluator = null!;
    private Mock<IScenarioToolRegistry> _mockScenarioToolRegistry = null!;
    private Mock<IUIControlService> _mockUIControlService = null!;
    private Mock<ILogger<ScenarioExecutor>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOrchestrator = new Mock<IAgentOrchestrator>();
        _mockConfigOverlay = new Mock<IConfigurationOverlay>();
        _mockConversationManager = new Mock<IConversationManager>();
        _mockConditionEvaluator = new Mock<IConditionEvaluator>();
        _mockScenarioToolRegistry = new Mock<IScenarioToolRegistry>();
        _mockUIControlService = new Mock<IUIControlService>();
        _mockLogger = new Mock<ILogger<ScenarioExecutor>>();

        // Setup orchestrator to return conversation manager
        _mockOrchestrator.Setup(o => o.ConversationManager).Returns(_mockConversationManager.Object);

        // Setup streaming to return empty (completed immediately)
        _mockOrchestrator
            .Setup(o => o.ProcessUserInputStreamingAsync(It.IsAny<UserMessage>(), It.IsAny<CancellationToken>()))
            .Returns(GetEmptyStreamingResponse());

        // Setup application message streaming
        _mockOrchestrator
            .Setup(o => o.ProcessApplicationMessageAsync(It.IsAny<ApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(GetEmptyStreamingResponse());

        // Setup conversation manager to return a completed conversation (for WaitForResponse)
        var messages = new List<IMessage>
        {
            new LlmTextMessage("Response")
        };
        _mockConversationManager.Setup(m => m.GetAllMessages()).Returns(messages);
    }

    private async IAsyncEnumerable<StreamingResponseChunk> GetEmptyStreamingResponse()
    {
        yield return new StreamingResponseChunk("", true, StreamingStatus.Completed);
        await Task.CompletedTask;
    }

    private ScenarioExecutor CreateExecutor()
    {
        return new ScenarioExecutor(
            _mockOrchestrator.Object,
            _mockConfigOverlay.Object,
            _mockConditionEvaluator.Object,
            _mockScenarioToolRegistry.Object,
            _mockUIControlService.Object,
            _mockLogger.Object);
    }

    #region WaitForToolCall Tests

    [TestMethod]
    public async Task WaitForToolCall_SpecificTool_ReleasesWhenCorrectToolCalled()
    {
        // Arrange: Scenario that waits for specific tool
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolCall, content: null, toolName: "get_weather")
            });

        // Act: Start scenario (it will block on WaitForToolCall)
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100); // Let it start waiting

        // Assert: Scenario is running and waiting
        Assert.IsTrue(executor.IsExecuting);

        // Act: Simulate orchestrator calling the wait hook for the correct tool
        await executor.WaitBeforeToolExecutionAsync("get_weather");
        await executionTask; // Should complete now

        // Assert: Scenario completed
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task WaitForToolCall_SpecificTool_DoesNotReleaseWhenWrongToolCalled()
    {
        // Arrange: Scenario that waits for specific tool
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolCall, content: null, toolName: "get_weather")
            });

        // Act: Start scenario (it will block on WaitForToolCall)
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100); // Let it start waiting

        // Act: Notify that a DIFFERENT tool was called
        await executor.WaitBeforeToolExecutionAsync("get_time");
        await Task.Delay(200); // Wait to see if it incorrectly releases

        // Assert: Scenario is still running and waiting (not completed)
        Assert.IsTrue(executor.IsExecuting);
        Assert.AreEqual(ScenarioExecutionState.Running, executor.State);

        // Cleanup: Send correct tool to unblock
        await executor.WaitBeforeToolExecutionAsync("get_weather");
        await executionTask;
    }

    [TestMethod]
    public async Task WaitForToolCall_AnyTool_ReleasesOnAnyToolCall()
    {
        // Arrange: Scenario that waits for any tool (no tool_name filter)
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolCall, content: null, toolName: null)
            });

        // Act: Start scenario (it will block on WaitForToolCall)
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100); // Let it start waiting

        // Act: Notify with any tool name
        await executor.WaitBeforeToolExecutionAsync("some_random_tool");
        await executionTask; // Should complete

        // Assert: Scenario completed
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task NotifyToolCallRequested_WhenNotWaiting_NoEffect()
    {
        // Arrange: Executor with no scenario running
        var executor = CreateExecutor();

        // Act: Send notification when not waiting
        await executor.WaitBeforeToolExecutionAsync("get_weather");

        // Assert: No exception thrown, executor still not executing
        Assert.IsFalse(executor.IsExecuting);
    }

    #endregion

    #region WaitForToolResponse Tests

    [TestMethod]
    public async Task WaitForToolResponse_SpecificTool_ReleasesWhenCorrectToolResponds()
    {
        // Arrange: Scenario that waits for specific tool response
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolResponse, content: null, toolName: "get_weather")
            });

        // Act: Start scenario (it will block on WaitForToolResponse)
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100); // Let it start waiting

        // Assert: Scenario is running and waiting
        Assert.IsTrue(executor.IsExecuting);

        // Act: Notify that the correct tool responded
        await executor.WaitAfterToolExecutionAsync("get_weather");
        await executionTask; // Should complete now

        // Assert: Scenario completed
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task WaitForToolResponse_SpecificTool_DoesNotReleaseWhenWrongToolResponds()
    {
        // Arrange: Scenario that waits for specific tool response
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolResponse, content: null, toolName: "get_weather")
            });

        // Act: Start scenario (it will block on WaitForToolResponse)
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100); // Let it start waiting

        // Act: Notify that a DIFFERENT tool responded
        await executor.WaitAfterToolExecutionAsync("get_time");
        await Task.Delay(200); // Wait to see if it incorrectly releases

        // Assert: Scenario is still running and waiting (not completed)
        Assert.IsTrue(executor.IsExecuting);
        Assert.AreEqual(ScenarioExecutionState.Running, executor.State);

        // Cleanup: Send correct tool to unblock
        await executor.WaitAfterToolExecutionAsync("get_weather");
        await executionTask;
    }

    [TestMethod]
    public async Task WaitForToolResponse_AnyTool_ReleasesOnAnyToolResponse()
    {
        // Arrange: Scenario that waits for any tool response (no tool_name filter)
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolResponse, content: null, toolName: null)
            });

        // Act: Start scenario (it will block on WaitForToolResponse)
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100); // Let it start waiting

        // Act: Notify with any tool name
        await executor.WaitAfterToolExecutionAsync("some_random_tool");
        await executionTask; // Should complete

        // Assert: Scenario completed
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task NotifyToolResponseReceived_WhenNotWaiting_NoEffect()
    {
        // Arrange: Executor with no scenario running
        var executor = CreateExecutor();

        // Act: Send notification when not waiting
        await executor.WaitAfterToolExecutionAsync("get_weather");

        // Assert: No exception thrown, executor still not executing
        Assert.IsFalse(executor.IsExecuting);
    }

    #endregion

    #region Integration Flow Tests

    [TestMethod]
    public async Task FullFlow_WaitForBothCallAndResponse_CompletesInCorrectOrder()
    {
        // Arrange: Scenario that waits for both call and response
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "Ask weather"),
                new ScenarioStep(ScenarioStepType.WaitForToolCall, content: null, toolName: "get_weather"),
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "Tool requested, waiting for execution..."),
                new ScenarioStep(ScenarioStepType.WaitForToolResponse, content: null, toolName: "get_weather"),
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "Tool completed!")
            });

        var stepIndices = new List<int>();
        executor.StepExecuted += (s, e) => stepIndices.Add(e.StepIndex);

        // Act: Start scenario
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(200); // Let it get to first wait

        // Assert: Step 0 completed, now waiting at WaitForToolCall (step 1)
        // StepExecuted fires after step completion, so step 1 hasn't fired yet
        Assert.AreEqual(ScenarioExecutionState.Running, executor.State);
        Assert.AreEqual(1, stepIndices.Count); // Only step 0 executed

        // Act: Notify tool call requested
        await executor.WaitBeforeToolExecutionAsync("get_weather");
        await Task.Delay(200); // Let it advance to next wait

        // Assert: Steps 0, 1, 2 completed, now waiting at WaitForToolResponse (step 3)
        Assert.AreEqual(3, stepIndices.Count); // Steps 0, 1, 2 executed

        // Act: Notify tool response received
        await executor.WaitAfterToolExecutionAsync("get_weather");
        await executionTask; // Should complete

        // Assert: All steps completed
        Assert.AreEqual(5, stepIndices.Count);
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task WaitForToolCall_MultipleToolsCalled_OnlyReleasesOnCorrectTool()
    {
        // Arrange: Scenario waiting for specific tool
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolCall, content: null, toolName: "target_tool")
            });

        // Act: Start scenario
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100);

        // Act: Call multiple wrong tools
        await executor.WaitBeforeToolExecutionAsync("tool1");
        await Task.Delay(50);
        await executor.WaitBeforeToolExecutionAsync("tool2");
        await Task.Delay(50);
        await executor.WaitBeforeToolExecutionAsync("tool3");
        await Task.Delay(50);

        // Assert: Still waiting
        Assert.IsTrue(executor.IsExecuting);

        // Act: Call correct tool
        await executor.WaitBeforeToolExecutionAsync("target_tool");
        await executionTask;

        // Assert: Completed
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task WaitForToolCall_CancelledWhileWaiting_CleansUpCorrectly()
    {
        // Arrange: Scenario that will wait indefinitely
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolCall, content: null, toolName: "never_called_tool")
            });

        // Act: Start and cancel
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100);

        executor.StopScenario();

        // Assert: Should complete via cancellation
        try
        {
            await executionTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Assert.AreEqual(ScenarioExecutionState.NotRunning, executor.State);
    }

    [TestMethod]
    public async Task WaitForToolResponse_CancelledWhileWaiting_CleansUpCorrectly()
    {
        // Arrange: Scenario that will wait indefinitely
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.WaitForToolResponse, content: null, toolName: "never_responds_tool")
            });

        // Act: Start and cancel
        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100);

        executor.StopScenario();

        // Assert: Should complete via cancellation
        try
        {
            await executionTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Assert.AreEqual(ScenarioExecutionState.NotRunning, executor.State);
    }

    #endregion
}
