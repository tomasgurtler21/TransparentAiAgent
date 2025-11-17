namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Represents the current execution state of a teaching scenario.
/// </summary>
public enum ScenarioExecutionState
{
    /// <summary>
    /// No scenario is currently executing.
    /// </summary>
    NotRunning,

    /// <summary>
    /// A scenario is actively executing steps.
    /// </summary>
    Running,

    /// <summary>
    /// A scenario is paused and waiting for user to resume.
    /// Can be paused either by user action or by a PauseForUser step.
    /// </summary>
    Paused,

    /// <summary>
    /// Scenario has completed all steps successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Scenario execution failed with an error.
    /// </summary>
    Failed
}
