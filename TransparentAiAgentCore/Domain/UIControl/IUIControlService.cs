namespace TransparentAiAgentCore.Domain.UIControl;

/// <summary>
/// Service for managing UI state and handling agent-driven UI control.
/// Registered as Singleton (shared across all render contexts in the app).
/// </summary>
public interface IUIControlService
{
    /// <summary>
    /// Raised when the UI state changes (either by agent or user).
    /// </summary>
    event EventHandler<UIState>? UIStateChanged;

    /// <summary>
    /// Gets the current UI state.
    /// </summary>
    UIState GetCurrentState();

    /// <summary>
    /// Updates the chat filter configuration.
    /// Only specified parameters are updated; null values leave existing settings unchanged.
    /// </summary>
    /// <param name="showUserMessages">Whether to show user messages.</param>
    /// <param name="showAssistantMessages">Whether to show assistant messages.</param>
    /// <param name="showSystemMessages">Whether to show system messages.</param>
    /// <param name="showToolCalls">Whether to show tool calls.</param>
    /// <param name="showToolResults">Whether to show tool results.</param>
    /// <param name="showTruncatedMessages">Whether to show truncated messages.</param>
    /// <returns>Success result with updated state, or failure with error message.</returns>
    Result<UIState> UpdateChatFilter(
        bool? showUserMessages = null,
        bool? showAssistantMessages = null,
        bool? showSystemMessages = null,
        bool? showToolCalls = null,
        bool? showToolResults = null,
        bool? showTruncatedMessages = null);

    /// <summary>
    /// Controls visibility of filter controls themselves.
    /// Useful in Teaching Mode to progressively reveal UI controls.
    /// </summary>
    /// <param name="visible">Whether filter controls should be visible to the user.</param>
    /// <returns>Success result with updated state, or failure with error message.</returns>
    Result<UIState> UpdateFilterControlVisibility(bool visible);

    /// <summary>
    /// Updates transparency viewer configuration.
    /// </summary>
    /// <param name="visible">Show or hide the transparency viewer panel.</param>
    /// <param name="eventTypeFilters">List of event types to display. Empty list shows all.</param>
    /// <param name="showTimestamps">Show timestamps for each event.</param>
    /// <returns>Success result with updated state, or failure with error message.</returns>
    Result<UIState> UpdateTransparencyViewer(
        bool? visible = null,
        List<string>? eventTypeFilters = null,
        bool? showTimestamps = null);

    /// <summary>
    /// Updates tools panel configuration.
    /// </summary>
    /// <param name="visible">Show or hide the tools panel.</param>
    /// <param name="expandedTools">List of tool names that should be expanded.</param>
    /// <param name="highlightedTool">Tool name to highlight (visual emphasis).</param>
    /// <returns>Success result with updated state, or failure with error message.</returns>
    Result<UIState> UpdateToolsPanel(
        bool? visible = null,
        List<string>? expandedTools = null,
        string? highlightedTool = null);

    /// <summary>
    /// Updates context indicators configuration.
    /// </summary>
    /// <param name="visible">Show or hide context status indicators.</param>
    /// <param name="highlighted">Add visual emphasis to context indicators (e.g., glow, animation).</param>
    /// <returns>Success result with updated state, or failure with error message.</returns>
    Result<UIState> UpdateContextIndicators(
        bool? visible = null,
        bool? highlighted = null);

    /// <summary>
    /// Updates configuration page state.
    /// </summary>
    /// <param name="visible">Show or hide the configuration page overlay.</param>
    /// <param name="navigate">Navigate to the Configuration page.</param>
    /// <param name="highlightSection">Section ID to highlight on the Configuration page.</param>
    /// <returns>Success result with updated state, or failure with error message.</returns>
    Result<UIState> UpdateConfigurationPage(
        bool? visible = null,
        bool? navigate = null,
        string? highlightSection = null);

    /// <summary>
    /// Resets UI state to defaults based on current mode.
    /// </summary>
    /// <returns>Success result with reset state, or failure with error message.</returns>
    Result<UIState> ResetToDefaults();

    /// <summary>
    /// Switches application mode and updates UI state accordingly.
    /// </summary>
    /// <param name="newMode">The mode to switch to (Normal or Teaching).</param>
    /// <returns>Success result with mode-appropriate state, or failure with error message.</returns>
    Result<UIState> SwitchMode(AppMode newMode);
}

/// <summary>
/// Result type for UI control operations.
/// Encapsulates success/failure state with optional value or error message.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public record Result<T>
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The result value if successful.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// The error message if failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<T> Ok(T value) => new() { Success = true, Value = value };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static Result<T> Fail(string error) => new() { Success = false, Error = error };
}
