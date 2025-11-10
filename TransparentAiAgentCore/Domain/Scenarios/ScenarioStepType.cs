namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Defines the types of steps that can be executed in a teaching scenario.
/// </summary>
public enum ScenarioStepType
{
    // Basic Step Types

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
    CompletionMessage,

    // Advanced Step Types (Phase 10b)

    /// <summary>
    /// Sends a user-like message with annotation (advanced scenarios).
    /// Annotation shown to observer, hidden from model.
    /// </summary>
    ScenarioUserMessage,

    /// <summary>
    /// Sends a system message with visibility control (advanced scenarios).
    /// Can be visible to model only, user only, or both.
    /// </summary>
    ScenarioSystemMessage,

    /// <summary>
    /// Waits until a specific condition is met (advanced scenarios).
    /// Supports response_contains, message_count, ui_state_changed, etc.
    /// </summary>
    WaitForCondition,

    /// <summary>
    /// Applies a configuration overlay to temporarily change system settings.
    /// </summary>
    ApplyConfigOverlay,

    /// <summary>
    /// Removes the current configuration overlay, restoring previous settings.
    /// </summary>
    RestoreConfigOverlay,

    /// <summary>
    /// Disables user input to prevent real user from sending messages.
    /// </summary>
    DisableUserInput,

    /// <summary>
    /// Enables user input, allowing real user to send messages.
    /// </summary>
    EnableUserInput,

    /// <summary>
    /// Pauses scenario execution for a specified duration.
    /// </summary>
    Delay,

    /// <summary>
    /// Directly manipulates UI state via UI control tools.
    /// </summary>
    UIControl
}
