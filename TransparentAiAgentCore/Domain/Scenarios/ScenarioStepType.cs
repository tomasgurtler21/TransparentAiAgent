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
    /// Sends a system message to the agent (visible to both user and LLM for transparency).
    /// </summary>
    AgentPrompt,

    // Advanced Step Types (Phase 10b)

    /// <summary>
    /// Sends a user-like message with optional annotation (advanced scenarios).
    /// Annotation is UI-only hint, not sent to LLM.
    /// </summary>
    ScenarioUserMessage,

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
    UIControl,

    /// <summary>
    /// Pauses scenario execution and waits for user to click Resume.
    /// Used to give users time to examine UI state, configuration, or other aspects.
    /// </summary>
    PauseForUser,

    /// <summary>
    /// Registers a scenario-specific mock tool.
    /// Tool will be available until explicitly unregistered or scenario completes.
    /// </summary>
    RegisterMockTool,

    /// <summary>
    /// Unregisters a previously registered mock tool.
    /// </summary>
    UnregisterMockTool,

    /// <summary>
    /// Waits for the LLM to request a tool call before allowing execution.
    /// Optional ToolName filter waits for specific tool; if omitted, waits for any tool.
    /// Pauses after LLM requests the tool but before the tool executes.
    /// </summary>
    WaitForToolCall,

    /// <summary>
    /// Waits for a tool execution to complete and return a response.
    /// Optional ToolName filter waits for specific tool; if omitted, waits for any tool.
    /// Pauses after tool completes execution.
    /// </summary>
    WaitForToolResponse
}
