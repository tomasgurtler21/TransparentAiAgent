namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Registry for managing and retrieving teaching scenario definitions.
/// </summary>
public interface IScenarioRegistry
{
    /// <summary>
    /// Gets all available scenarios.
    /// </summary>
    /// <returns>Collection of all scenario definitions.</returns>
    IReadOnlyList<ScenarioDefinition> GetAllScenarios();

    /// <summary>
    /// Gets a specific scenario by its ID.
    /// </summary>
    /// <param name="scenarioId">The unique identifier of the scenario.</param>
    /// <returns>The scenario definition, or null if not found.</returns>
    ScenarioDefinition? GetScenarioById(string scenarioId);

    /// <summary>
    /// Gets scenarios filtered by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>Collection of matching scenarios.</returns>
    IReadOnlyList<ScenarioDefinition> GetScenariosByCategory(string category);

    /// <summary>
    /// Gets scenarios filtered by difficulty level.
    /// </summary>
    /// <param name="difficulty">The difficulty level to filter by.</param>
    /// <returns>Collection of matching scenarios.</returns>
    IReadOnlyList<ScenarioDefinition> GetScenariosByDifficulty(string difficulty);

    /// <summary>
    /// Adds a new scenario to the registry.
    /// </summary>
    /// <param name="scenario">The scenario to add.</param>
    /// <exception cref="ArgumentNullException">Scenario is null.</exception>
    /// <exception cref="ArgumentException">Scenario with same ID already exists.</exception>
    void AddScenario(ScenarioDefinition scenario);
}
