namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Represents a single step in a teaching scenario.
/// Supports both basic and advanced scenario features.
/// </summary>
public class ScenarioStep
{
    // Basic properties
    public ScenarioStepType Type { get; }
    public string? Content { get; }
    public int DelayMs { get; }

    /// <summary>
    /// Optional configuration overlay to apply during this step.
    /// Maps configuration keys to temporary values.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ConfigOverlay { get; }

    // Advanced properties (Phase 10b)

    /// <summary>
    /// Annotation text visible to the user but hidden from the model.
    /// Used in ScenarioUserMessage and ScenarioSystemMessage steps.
    /// </summary>
    public string? Annotation { get; }

    /// <summary>
    /// Controls visibility of ScenarioSystemMessage.
    /// </summary>
    public MessageVisibility? VisibleTo { get; }

    /// <summary>
    /// Condition type for WaitForCondition steps.
    /// Examples: "response_contains", "message_count", "ui_state_changed"
    /// </summary>
    public string? Condition { get; }

    /// <summary>
    /// Parameters for the condition evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ConditionParameters { get; }

    /// <summary>
    /// Action to take if condition timeout occurs: "fail", "skip", "continue"
    /// </summary>
    public string? OnTimeout { get; }

    /// <summary>
    /// UI control tool name for UIControl steps.
    /// </summary>
    public string? UIControlTool { get; }

    /// <summary>
    /// Arguments for UI control tool execution.
    /// </summary>
    public IReadOnlyDictionary<string, object>? UIControlArguments { get; }

    public ScenarioStep(
        ScenarioStepType type,
        string? content = null,
        int delayMs = 0,
        IReadOnlyDictionary<string, object>? configOverlay = null,
        string? annotation = null,
        MessageVisibility? visibleTo = null,
        string? condition = null,
        IReadOnlyDictionary<string, object>? conditionParameters = null,
        string? onTimeout = null,
        string? uiControlTool = null,
        IReadOnlyDictionary<string, object>? uiControlArguments = null)
    {
        // Validate content for step types that require it
        if (RequiresContent(type) && string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                $"Content cannot be null or whitespace for {type} step type",
                nameof(content));
        }

        // Validate delay
        if (delayMs < 0)
        {
            throw new ArgumentException(
                "Delay cannot be negative",
                nameof(delayMs));
        }

        // Validate advanced step types
        ValidateAdvancedStep(type, condition, uiControlTool);

        Type = type;
        Content = content;
        DelayMs = delayMs;
        ConfigOverlay = configOverlay;
        Annotation = annotation;
        VisibleTo = visibleTo;
        Condition = condition;
        ConditionParameters = conditionParameters;
        OnTimeout = onTimeout ?? "continue";
        UIControlTool = uiControlTool;
        UIControlArguments = uiControlArguments;
    }

    private static bool RequiresContent(ScenarioStepType type)
    {
        return type != ScenarioStepType.WaitForResponse
            && type != ScenarioStepType.WaitForCondition
            && type != ScenarioStepType.ApplyConfigOverlay
            && type != ScenarioStepType.RestoreConfigOverlay
            && type != ScenarioStepType.EnableUserInput
            && type != ScenarioStepType.DisableUserInput
            && type != ScenarioStepType.Delay
            && type != ScenarioStepType.UIControl;
    }

    private static void ValidateAdvancedStep(ScenarioStepType type, string? condition, string? uiControlTool)
    {
        if (type == ScenarioStepType.WaitForCondition && string.IsNullOrWhiteSpace(condition))
        {
            throw new ArgumentException(
                "Condition cannot be null or whitespace for WaitForCondition step type",
                nameof(condition));
        }

        if (type == ScenarioStepType.UIControl && string.IsNullOrWhiteSpace(uiControlTool))
        {
            throw new ArgumentException(
                "UIControlTool cannot be null or whitespace for UIControl step type",
                nameof(uiControlTool));
        }
    }
}
