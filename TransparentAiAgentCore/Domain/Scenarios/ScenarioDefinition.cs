namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Defines a complete teaching scenario with metadata and steps.
/// </summary>
public class ScenarioDefinition
{
    /// <summary>
    /// Gets the unique identifier for this scenario.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the scenario.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional description explaining what this scenario teaches.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the ordered list of steps to execute in this scenario.
    /// </summary>
    public IReadOnlyList<ScenarioStep> Steps { get; }

    /// <summary>
    /// Gets the optional category for grouping scenarios in the UI.
    /// </summary>
    public string? Category { get; }

    /// <summary>
    /// Gets the optional difficulty level (beginner, intermediate, advanced).
    /// </summary>
    public string? Difficulty { get; }

    /// <summary>
    /// Gets the estimated duration in seconds for completing this scenario.
    /// </summary>
    public int? EstimatedDurationSeconds { get; }

    public ScenarioDefinition(
        string id,
        string name,
        string? description,
        IEnumerable<ScenarioStep> steps,
        string? category = null,
        string? difficulty = null,
        int? estimatedDurationSeconds = null)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id cannot be null or whitespace", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace", nameof(name));
        }

        if (steps == null)
        {
            throw new ArgumentNullException(nameof(steps), "Steps cannot be null");
        }

        var stepsList = steps.ToList();
        if (stepsList.Count == 0)
        {
            throw new ArgumentException("Scenario must have at least one step", nameof(steps));
        }

        // Validate duration
        if (estimatedDurationSeconds.HasValue && estimatedDurationSeconds.Value < 0)
        {
            throw new ArgumentException(
                "Estimated duration cannot be negative",
                nameof(estimatedDurationSeconds));
        }

        Id = id;
        Name = name;
        Description = description;
        Steps = stepsList;
        Category = category;
        Difficulty = difficulty;
        EstimatedDurationSeconds = estimatedDurationSeconds;
    }
}
