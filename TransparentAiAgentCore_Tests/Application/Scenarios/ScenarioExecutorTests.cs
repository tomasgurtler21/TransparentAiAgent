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

[TestClass]
public class ScenarioExecutorTests
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

        // Setup conversation manager to return a completed conversation (for WaitForResponse)
        var messages = new List<IMessage>
        {
            new LlmTextMessage("Response") // Add an assistant message so WaitForResponse completes
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

    [TestMethod]
    public void ScenarioExecutor_InitialState_NotExecuting()
    {
        // Arrange & Act
        var executor = CreateExecutor();

        // Assert
        Assert.IsFalse(executor.IsExecuting);
        Assert.IsNull(executor.CurrentScenario);
        Assert.AreEqual(-1, executor.CurrentStepIndex);
    }

    [TestMethod]
    public async Task ExecuteScenarioAsync_NullScenario_ThrowsArgumentNullException()
    {
        // Arrange
        var executor = CreateExecutor();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await executor.ExecuteScenarioAsync(null!));
    }

    [TestMethod]
    public async Task ExecuteScenarioAsync_AlreadyExecuting_ThrowsInvalidOperationException()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateSimpleScenario();

        // Start first execution (don't await)
        var firstTask = executor.ExecuteScenarioAsync(scenario);

        // Act & Assert - try to start second while first is running
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            await executor.ExecuteScenarioAsync(scenario));

        // Cleanup
        executor.StopScenario();
        try { await firstTask; } catch { }
    }

    [TestMethod]
    public async Task ExecuteScenarioAsync_ValidScenario_FiresScenarioStartedEvent()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateSimpleScenario();
        ScenarioExecutionEventArgs? eventArgs = null;

        executor.ScenarioStarted += (sender, args) => eventArgs = args;

        // Act
        var task = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(50); // Give time for event to fire
        executor.StopScenario();
        try { await task; } catch { }

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(scenario.Id, eventArgs.Scenario.Id);
    }

    [TestMethod]
    public async Task ExecuteScenarioAsync_CompletesSuccessfully_FiresScenarioCompletedEvent()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithSingleStep();
        ScenarioExecutionEventArgs? completedArgs = null;

        executor.ScenarioCompleted += (sender, args) => completedArgs = args;

        // Act
        await executor.ExecuteScenarioAsync(scenario);

        // Assert
        Assert.IsNotNull(completedArgs);
        Assert.AreEqual(scenario.Id, completedArgs.Scenario.Id);
        Assert.IsFalse(executor.IsExecuting);
    }

    [TestMethod]
    public async Task StopScenario_WhileExecuting_StopsExecution()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithDelay();

        // Act
        var task = executor.ExecuteScenarioAsync(scenario);
        await Task.Delay(50); // Let it start

        Assert.IsTrue(executor.IsExecuting);

        executor.StopScenario();
        await Task.Delay(50); // Give time to stop

        // Assert
        Assert.IsFalse(executor.IsExecuting);
    }

    [TestMethod]
    public async Task ExecuteScenarioAsync_ExecutesSteps_FiresStepExecutedEvents()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithMultipleSteps();
        var stepsFired = new List<ScenarioStepEventArgs>();

        executor.StepExecuted += (sender, args) => stepsFired.Add(args);

        // Act
        await executor.ExecuteScenarioAsync(scenario);

        // Assert
        Assert.AreEqual(3, stepsFired.Count);
        Assert.AreEqual(0, stepsFired[0].StepIndex);
        Assert.AreEqual(1, stepsFired[1].StepIndex);
        Assert.AreEqual(2, stepsFired[2].StepIndex);
    }

    [TestMethod]
    public async Task ExecuteScenarioAsync_UpdatesCurrentStepIndex()
    {
        // Arrange
        var executor = CreateExecutor();
        var scenario = CreateScenarioWithMultipleSteps();
        var indices = new List<int>();

        executor.StepExecuted += (sender, args) => indices.Add(executor.CurrentStepIndex);

        // Act
        await executor.ExecuteScenarioAsync(scenario);

        // Assert
        Assert.AreEqual(3, indices.Count);
        Assert.AreEqual(0, indices[0]);
        Assert.AreEqual(1, indices[1]);
        Assert.AreEqual(2, indices[2]);
    }

    // Helper methods to create test scenarios
    private ScenarioDefinition CreateSimpleScenario()
    {
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test message", delayMs: 10000) // Long delay so we can test stopping
        };
        return new ScenarioDefinition("test-scenario", "Test Scenario", "Test", steps);
    }

    private ScenarioDefinition CreateScenarioWithSingleStep()
    {
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Quick message", delayMs: 1)
        };
        return new ScenarioDefinition("quick-scenario", "Quick Scenario", "Test", steps);
    }

    private ScenarioDefinition CreateScenarioWithDelay()
    {
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Message 1", delayMs: 5000)
        };
        return new ScenarioDefinition("delay-scenario", "Delay Scenario", "Test", steps);
    }

    private ScenarioDefinition CreateScenarioWithMultipleSteps()
    {
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Step 1", delayMs: 1),
            new ScenarioStep(ScenarioStepType.WaitForResponse, null, delayMs: 1),
            new ScenarioStep(ScenarioStepType.AgentPrompt, "Teach something", delayMs: 1)
        };
        return new ScenarioDefinition("multi-step-scenario", "Multi-Step Scenario", "Test", steps);
    }
}
