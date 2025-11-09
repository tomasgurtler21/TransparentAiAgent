using TransparentAiAgentCore.Domain.Scenarios;

namespace TransparentAiAgentCore.Infrastructure.Scenarios;

/// <summary>
/// In-memory registry for managing teaching scenario definitions.
/// </summary>
public class ScenarioRegistry : IScenarioRegistry
{
    private readonly Dictionary<string, ScenarioDefinition> _scenarios = new();
    private readonly object _lock = new();

    public IReadOnlyList<ScenarioDefinition> GetAllScenarios()
    {
        lock (_lock)
        {
            return _scenarios.Values.ToList();
        }
    }

    public ScenarioDefinition? GetScenarioById(string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            return null;

        lock (_lock)
        {
            return _scenarios.TryGetValue(scenarioId, out var scenario) ? scenario : null;
        }
    }

    public IReadOnlyList<ScenarioDefinition> GetScenariosByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return new List<ScenarioDefinition>();

        lock (_lock)
        {
            return _scenarios.Values
                .Where(s => s.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }
    }

    public IReadOnlyList<ScenarioDefinition> GetScenariosByDifficulty(string difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty))
            return new List<ScenarioDefinition>();

        lock (_lock)
        {
            return _scenarios.Values
                .Where(s => s.Difficulty?.Equals(difficulty, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }
    }

    public void AddScenario(ScenarioDefinition scenario)
    {
        if (scenario == null)
            throw new ArgumentNullException(nameof(scenario));

        lock (_lock)
        {
            if (_scenarios.ContainsKey(scenario.Id))
            {
                throw new ArgumentException(
                    $"A scenario with ID '{scenario.Id}' already exists",
                    nameof(scenario));
            }

            _scenarios[scenario.Id] = scenario;
        }
    }
}
