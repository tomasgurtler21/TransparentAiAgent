using TransparentAiAgentCore.Domain.Scenarios;

namespace TransparentAiAgentCore.Application.Scenarios;

/// <summary>
/// Executes teaching scenarios by orchestrating step-by-step execution.
/// </summary>
public interface IScenarioExecutor
{
    /// <summary>
    /// Gets the currently executing scenario, or null if no scenario is running.
    /// </summary>
    ScenarioDefinition? CurrentScenario { get; }

    /// <summary>
    /// Gets whether a scenario is currently executing.
    /// </summary>
    bool IsExecuting { get; }

    /// <summary>
    /// Gets the current step index (0-based) in the executing scenario.
    /// </summary>
    int CurrentStepIndex { get; }

    /// <summary>
    /// Event fired when scenario execution starts.
    /// </summary>
    event EventHandler<ScenarioExecutionEventArgs>? ScenarioStarted;

    /// <summary>
    /// Event fired when scenario execution completes.
    /// </summary>
    event EventHandler<ScenarioExecutionEventArgs>? ScenarioCompleted;

    /// <summary>
    /// Event fired when scenario execution fails.
    /// </summary>
    event EventHandler<ScenarioExecutionEventArgs>? ScenarioFailed;

    /// <summary>
    /// Event fired when a scenario step is executed.
    /// </summary>
    event EventHandler<ScenarioStepEventArgs>? StepExecuted;

    /// <summary>
    /// Event fired when an auto-message is sent by the scenario.
    /// Allows UI to distinguish scenario-sent messages from user-typed messages.
    /// </summary>
    event EventHandler<AutoMessageSentEventArgs>? AutoMessageSent;

    /// <summary>
    /// Event fired when a streaming update occurs during scenario execution.
    /// Allows UI to update in real-time as messages stream.
    /// </summary>
    event EventHandler<ScenarioStreamingUpdateEventArgs>? StreamingUpdate;

    /// <summary>
    /// Starts executing a scenario.
    /// </summary>
    /// <param name="scenario">The scenario to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when scenario finishes.</returns>
    Task ExecuteScenarioAsync(ScenarioDefinition scenario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the currently executing scenario.
    /// </summary>
    void StopScenario();
}

/// <summary>
/// Event args for scenario execution events.
/// </summary>
public class ScenarioExecutionEventArgs : EventArgs
{
    public ScenarioDefinition Scenario { get; }
    public string? ErrorMessage { get; }

    public ScenarioExecutionEventArgs(ScenarioDefinition scenario, string? errorMessage = null)
    {
        Scenario = scenario;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Event args for scenario step events.
/// </summary>
public class ScenarioStepEventArgs : EventArgs
{
    public ScenarioDefinition Scenario { get; }
    public ScenarioStep Step { get; }
    public int StepIndex { get; }

    public ScenarioStepEventArgs(ScenarioDefinition scenario, ScenarioStep step, int stepIndex)
    {
        Scenario = scenario;
        Step = step;
        StepIndex = stepIndex;
    }
}

/// <summary>
/// Event args for auto-message sent events.
/// </summary>
public class AutoMessageSentEventArgs : EventArgs
{
    public string MessageContent { get; }
    public DateTime SentAt { get; }

    public AutoMessageSentEventArgs(string messageContent, DateTime sentAt)
    {
        MessageContent = messageContent;
        SentAt = sentAt;
    }
}

/// <summary>
/// Event args for scenario streaming update events.
/// </summary>
public class ScenarioStreamingUpdateEventArgs : EventArgs
{
    public string ContentDelta { get; }
    public bool IsComplete { get; }

    public ScenarioStreamingUpdateEventArgs(string contentDelta, bool isComplete)
    {
        ContentDelta = contentDelta;
        IsComplete = isComplete;
    }
}
