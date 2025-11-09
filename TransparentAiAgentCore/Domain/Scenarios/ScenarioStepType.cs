namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Defines the types of steps that can be executed in a teaching scenario.
/// </summary>
public enum ScenarioStepType
{
    /// <summary>
    /// Sends a message as if from the user (shown with 🎬 icon).
    /// </summary>
    AutoMessage,

    /// <summary>
    /// Waits for the agent to respond before continuing.
    /// </summary>
    WaitForResponse,

    /// <summary>
    /// Sends a hidden teaching trigger to the agent (system message).
    /// </summary>
    AgentPrompt,

    /// <summary>
    /// Shows a completion message/banner to the user.
    /// </summary>
    CompletionMessage
}
