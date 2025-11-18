using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Models;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentCore_Tests.Application.Scenarios;

/// <summary>
/// Tests for ScenarioExecutor pause/resume functionality.
/// </summary>
[TestClass]
public class ScenarioExecutorPauseResumeTests
{
    private Mock<IAgentOrchestrator> _mockOrchestrator = null!;
    private Mock<IConfigurationOverlay> _mockConfigOverlay = null!;
    private Mock<IConversationManager> _mockConversationManager = null!;
    private Mock<IConditionEvaluator> _mockConditionEvaluator = null!;
    private Mock<ILogger<ScenarioExecutor>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOrchestrator = new Mock<IAgentOrchestrator>();
        _mockConfigOverlay = new Mock<IConfigurationOverlay>();
        _mockConversationManager = new Mock<IConversationManager>();
        _mockConditionEvaluator = new Mock<IConditionEvaluator>();
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
            _mockLogger.Object);
    }

    private ScenarioDefinition CreateScenarioWithMultipleSteps(int stepCount = 3)
    {
        var steps = new List<ScenarioStep>();
        for (int i = 0; i < stepCount; i++)
        {
            steps.Add(new ScenarioStep(ScenarioStepType.ScenarioUserMessage, $"Step {i}", delayMs: 50));
        }

        return new ScenarioDefinition("test", "Test Scenario", null, steps);
    }

    #region Initial State Tests

    [TestMethod]
    public void ScenarioExecutor_InitialState_NotRunning()
    {
        // Arrange & Act
        var executor = CreateExecutor();

        // Assert
        Assert.AreEqual(ScenarioExecutionState.NotRunning, executor.State);
        Assert.IsFalse(executor.IsPaused);
    }

    #endregion

    #region PauseScenario Tests

    [TestMethod]
    public async Task PauseScenario_WhileRunning_ChangesStateToPaused()
    {
        // Arrange: Start a scenario
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithMultipleSteps(5);

        var pausedEventFired = false;
        executor.ScenarioPaused += (s, e) => pausedEventFired = true;

        // Start scenario (don't await - let it run in background)
        var executionTask = executor.ExecuteScenarioAsync(scenario);

        // Wait for scenario to start running
        await Task.Delay(100);
        Assert.AreEqual(ScenarioExecutionState.Running, executor.State);

        // Act: Pause the scenario
        executor.PauseScenario();

        // Assert
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
        Assert.IsTrue(executor.IsPaused);
        Assert.IsTrue(pausedEventFired);

        // Cleanup: Stop the scenario
        executor.StopScenario();
        try { await executionTask; } catch { }
    }

    [TestMethod]
    public void PauseScenario_WhenNotRunning_NoOp()
    {
        // Arrange
        var executor = CreateExecutor();

        var pausedEventFired = false;
        executor.ScenarioPaused += (s, e) => pausedEventFired = true;

        // Act: Try to pause when not running
        executor.PauseScenario();

        // Assert: State remains NotRunning, event not fired
        Assert.AreEqual(ScenarioExecutionState.NotRunning, executor.State);
        Assert.IsFalse(pausedEventFired);
    }

    [TestMethod]
    public async Task PauseScenario_WithMessage_FiresEventWithMessage()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithMultipleSteps(5);

        string? receivedMessage = null;
        executor.ScenarioPaused += (s, e) => receivedMessage = e.PauseMessage;

        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100);

        // Act: Pause with a message
        executor.PauseScenario("Check the UI state");

        // Assert
        Assert.AreEqual("Check the UI state", receivedMessage);

        // Cleanup
        executor.StopScenario();
        try { await executionTask; } catch { }
    }

    #endregion

    #region ResumeScenario Tests

    [TestMethod]
    public async Task ResumeScenario_WhenPaused_ChangesStateToRunning()
    {
        // Arrange: Start and pause a scenario
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithMultipleSteps(5);

        var resumedEventFired = false;
        executor.ScenarioResumed += (s, e) => resumedEventFired = true;

        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100);

        executor.PauseScenario();
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);

        // Act: Resume the scenario
        executor.ResumeScenario();

        // Assert
        Assert.AreEqual(ScenarioExecutionState.Running, executor.State);
        Assert.IsFalse(executor.IsPaused);
        Assert.IsTrue(resumedEventFired);

        // Cleanup
        executor.StopScenario();
        try { await executionTask; } catch { }
    }

    [TestMethod]
    public void ResumeScenario_WhenNotPaused_NoOp()
    {
        // Arrange
        var executor = CreateExecutor();

        var resumedEventFired = false;
        executor.ScenarioResumed += (s, e) => resumedEventFired = true;

        // Act: Try to resume when not paused
        executor.ResumeScenario();

        // Assert: Event not fired
        Assert.IsFalse(resumedEventFired);
    }

    #endregion

    #region Pause During Execution Tests

    [TestMethod]
    public async Task ExecuteScenario_PausedBetweenSteps_WaitsForResume()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithMultipleSteps(5);

        int stepExecutedCount = 0;
        executor.StepExecuted += (s, e) =>
        {
            stepExecutedCount++;
            // Pause after second step
            if (e.StepIndex == 1)
            {
                executor.PauseScenario();
            }
        };

        // Act: Start scenario
        var executionTask = executor.ExecuteScenarioAsync(scenario);

        // Wait for pause to occur
        await Task.Delay(500);

        // Assert: Scenario is paused after step 1
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
        Assert.AreEqual(2, stepExecutedCount); // Steps 0 and 1 executed

        // Resume and let it complete
        executor.ResumeScenario();
        await executionTask;

        // Assert: All steps completed
        Assert.AreEqual(5, stepExecutedCount);
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    #endregion

    #region StopScenario While Paused Tests

    [TestMethod]
    public async Task StopScenario_WhilePaused_StopsExecution()
    {
        // Arrange: Start and pause a scenario
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithMultipleSteps(5);

        var executionTask = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(100);

        executor.PauseScenario();
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);

        // Act: Stop while paused
        executor.StopScenario();

        // Assert: Wait for execution to complete (should be cancelled)
        try
        {
            await executionTask;
        }
        catch (OperationCanceledException)
        {
            // Expected - scenario was cancelled
        }

        Assert.AreEqual(ScenarioExecutionState.NotRunning, executor.State);
    }

    #endregion

    #region PauseForUser Step Execution Tests

    [TestMethod]
    public async Task ExecuteScenario_PauseForUserStep_PausesWithMessage()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "First step"),
                new ScenarioStep(ScenarioStepType.PauseForUser, content: "Check the UI state now"),
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "Last step")
            });

        string? receivedPauseMessage = null;
        executor.ScenarioPaused += (s, e) => receivedPauseMessage = e.PauseMessage;

        // Act: Start scenario
        var executionTask = executor.ExecuteScenarioAsync(scenario);

        // Wait for first step and pause to occur
        await Task.Delay(200);

        // Assert: Scenario is paused with correct message
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
        Assert.AreEqual("Check the UI state now", receivedPauseMessage);
        Assert.AreEqual(1, executor.CurrentStepIndex); // Stopped at PauseForUser step (index 1)

        // Resume and complete
        executor.ResumeScenario();
        await executionTask;

        // Assert: Scenario completed successfully
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task ExecuteScenario_PauseForUserStepWithMessage_Pauses()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.PauseForUser, content: "Scenario paused")
            });

        bool pausedEventFired = false;
        executor.ScenarioPaused += (s, e) => pausedEventFired = true;

        // Act: Start scenario
        var executionTask = executor.ExecuteScenarioAsync(scenario);

        // Wait for pause to occur
        await Task.Delay(100);

        // Assert: Scenario is paused
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
        Assert.IsTrue(pausedEventFired);

        // Resume and complete
        executor.ResumeScenario();
        await executionTask;

        // Assert: Scenario completed
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    [TestMethod]
    public async Task ExecuteScenario_MultiplePauseForUserSteps_PausesAtEach()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = new ScenarioDefinition(
            "test",
            "Test Scenario",
            null,
            new[]
            {
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "Step 1"),
                new ScenarioStep(ScenarioStepType.PauseForUser, content: "First pause"),
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "Step 2"),
                new ScenarioStep(ScenarioStepType.PauseForUser, content: "Second pause"),
                new ScenarioStep(ScenarioStepType.ScenarioUserMessage, "Step 3")
            });

        var pauseMessages = new List<string?>();
        executor.ScenarioPaused += (s, e) => pauseMessages.Add(e.PauseMessage);

        // Act: Start scenario
        var executionTask = executor.ExecuteScenarioAsync(scenario);

        // Wait for first pause
        await Task.Delay(200);
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
        Assert.AreEqual(1, pauseMessages.Count);
        Assert.AreEqual("First pause", pauseMessages[0]);

        // Resume and wait for second pause
        executor.ResumeScenario();
        await Task.Delay(200);
        Assert.AreEqual(ScenarioExecutionState.Paused, executor.State);
        Assert.AreEqual(2, pauseMessages.Count);
        Assert.AreEqual("Second pause", pauseMessages[1]);

        // Resume and complete
        executor.ResumeScenario();
        await executionTask;

        // Assert: Scenario completed
        Assert.AreEqual(ScenarioExecutionState.Completed, executor.State);
    }

    #endregion
}
