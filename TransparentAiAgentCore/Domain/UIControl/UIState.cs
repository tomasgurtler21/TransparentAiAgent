namespace TransparentAiAgentCore.Domain.UIControl;

/// <summary>
/// Represents the complete state of all UI components controllable by the agent.
/// Immutable by design - updates create new instances.
/// </summary>
public record UIState
{
    /// <summary>
    /// Configuration for chat message filtering.
    /// </summary>
    public ChatFilterState ChatFilter { get; init; } = new();

    /// <summary>
    /// Configuration for transparency viewer.
    /// </summary>
    public TransparencyViewerState TransparencyViewer { get; init; } = new();

    /// <summary>
    /// Configuration for tools overview panel.
    /// </summary>
    public ToolsPanelState ToolsPanel { get; init; } = new();

    /// <summary>
    /// Configuration for context status indicators.
    /// </summary>
    public ContextIndicatorsState ContextIndicators { get; init; } = new();

    /// <summary>
    /// Configuration for the configuration page.
    /// </summary>
    public ConfigurationPageState ConfigurationPage { get; init; } = new();

    /// <summary>
    /// Configuration for the scenario selector overlay.
    /// </summary>
    public ScenarioSelectorState ScenarioSelector { get; init; } = new();

    /// <summary>
    /// Component highlighting state for teaching mode.
    /// Tracks which UI components are currently highlighted.
    /// </summary>
    public HighlightState HighlightState { get; init; } = new();

    /// <summary>
    /// Current application mode (Normal or Teaching).
    /// </summary>
    public AppMode CurrentMode { get; init; } = AppMode.Normal;

    /// <summary>
    /// Creates a default UI state for Normal Mode (overlays closed by default).
    /// </summary>
    public static UIState DefaultNormalMode() => new()
    {
        ChatFilter = new ChatFilterState
        {
            ShowUserMessages = true,
            ShowAssistantMessages = true,
            ShowSystemMessages = true,
            ShowToolCalls = true,
            ShowToolResults = true,
            ShowTruncatedMessages = true,
            FilterControlsVisible = true
        },
        TransparencyViewer = new TransparencyViewerState
        {
            Visible = false,
            ShowTimestamps = true,
            EventTypeFilters = new List<string>()
        },
        ToolsPanel = new ToolsPanelState
        {
            Visible = false,
            ExpandedTools = new List<string>(),
            HighlightedTool = null
        },
        ContextIndicators = new ContextIndicatorsState
        {
            Visible = true,
            Highlighted = false
        },
        ConfigurationPage = new ConfigurationPageState
        {
            Visible = false,
            HighlightedSection = null
        },
        ScenarioSelector = new ScenarioSelectorState
        {
            Visible = false
        },
        CurrentMode = AppMode.Normal
    };

    /// <summary>
    /// Creates a default UI state for Teaching Mode (controls hidden, progressive reveal).
    /// </summary>
    public static UIState DefaultTeachingMode() => new()
    {
        ChatFilter = new ChatFilterState
        {
            ShowUserMessages = true,
            ShowAssistantMessages = true,
            ShowSystemMessages = false,
            ShowToolCalls = false,
            ShowToolResults = false,
            ShowTruncatedMessages = true,
            FilterControlsVisible = false
        },
        TransparencyViewer = new TransparencyViewerState
        {
            Visible = false,
            ShowTimestamps = true,
            EventTypeFilters = new List<string>()
        },
        ToolsPanel = new ToolsPanelState
        {
            Visible = false,
            ExpandedTools = new List<string>(),
            HighlightedTool = null
        },
        ContextIndicators = new ContextIndicatorsState
        {
            Visible = false,
            Highlighted = false
        },
        ConfigurationPage = new ConfigurationPageState
        {
            Visible = false,
            HighlightedSection = null
        },
        ScenarioSelector = new ScenarioSelectorState
        {
            Visible = false
        },
        CurrentMode = AppMode.Teaching
    };
}

/// <summary>
/// Chat history filtering configuration.
/// </summary>
public record ChatFilterState
{
    public bool ShowUserMessages { get; init; } = true;
    public bool ShowAssistantMessages { get; init; } = true;
    public bool ShowSystemMessages { get; init; } = true;
    public bool ShowToolCalls { get; init; } = true;
    public bool ShowToolResults { get; init; } = true;
    public bool ShowTruncatedMessages { get; init; } = true;
    public bool FilterControlsVisible { get; init; } = true;
}

/// <summary>
/// Transparency viewer configuration.
/// </summary>
public record TransparencyViewerState
{
    public bool Visible { get; init; } = true;
    public List<string> EventTypeFilters { get; init; } = new();
    public bool ShowTimestamps { get; init; } = true;
}

/// <summary>
/// Tools overview panel configuration.
/// </summary>
public record ToolsPanelState
{
    public bool Visible { get; init; } = true;
    public List<string> ExpandedTools { get; init; } = new();
    public string? HighlightedTool { get; init; } = null;
}

/// <summary>
/// Context status indicators configuration.
/// </summary>
public record ContextIndicatorsState
{
    public bool Visible { get; init; } = true;
    public bool Highlighted { get; init; } = false;
}

/// <summary>
/// Configuration page state.
/// </summary>
public record ConfigurationPageState
{
    public bool Visible { get; init; } = true;
    public string? HighlightedSection { get; init; } = null;
}

/// <summary>
/// Scenario selector overlay state.
/// </summary>
public record ScenarioSelectorState
{
    public bool Visible { get; init; } = false;
}

/// <summary>
/// Application mode enumeration.
/// </summary>
public enum AppMode
{
    /// <summary>
    /// Normal transparent agent mode - all controls visible.
    /// </summary>
    Normal,

    /// <summary>
    /// Teaching mode - progressive reveal of features.
    /// </summary>
    Teaching
}
