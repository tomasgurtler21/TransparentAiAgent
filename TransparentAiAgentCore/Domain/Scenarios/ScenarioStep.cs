namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Represents a single step in a teaching scenario.
/// </summary>
public class ScenarioStep
{
    public ScenarioStepType Type { get; }
    public string? Content { get; }
    public int DelayMs { get; }

    /// <summary>
    /// Optional configuration overlay to apply during this step.
    /// Maps configuration keys to temporary values.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ConfigOverlay { get; }

    public ScenarioStep(
        ScenarioStepType type,
        string? content,
        int delayMs = 0,
        IReadOnlyDictionary<string, object>? configOverlay = null)
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

        Type = type;
        Content = content;
        DelayMs = delayMs;
        ConfigOverlay = configOverlay;
    }

    private static bool RequiresContent(ScenarioStepType type)
    {
        return type != ScenarioStepType.WaitForResponse;
    }
}
