namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Defines a complete teaching scenario with metadata and steps.
/// </summary>
public class ScenarioDefinition
{
    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public IReadOnlyList<ScenarioStep> Steps { get; }
    public string? Category { get; }
    public string? Difficulty { get; }
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
