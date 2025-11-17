using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Application.Localization;
using TransparentAiAgentCore.Infrastructure.Localization;

namespace TransparentAiAgentCore.Infrastructure.Scenarios;

/// <summary>
/// In-memory registry for managing teaching scenario definitions.
/// Automatically reloads scenarios when language changes.
/// </summary>
public class ScenarioRegistry : IScenarioRegistry
{
    private readonly Dictionary<string, ScenarioDefinition> _scenarios = new();
    private readonly object _lock = new();
    private readonly JsonScenarioLoader _loader;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly string _scenariosPath;

    public ScenarioRegistry(
        JsonScenarioLoader loader,
        ILanguageService languageService,
        ITranslationService translationService,
        string scenariosPath)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        _scenariosPath = scenariosPath ?? throw new ArgumentNullException(nameof(scenariosPath));

        // Subscribe to language changes
        _languageService.LanguageChanged += OnLanguageChanged;

        // Load scenarios for current language (sync in constructor is acceptable for app startup)
        LoadScenariosAsync().Wait();
    }

    private async void OnLanguageChanged(object? sender, string newLanguage)
    {
        // TranslationService will reload automatically (subscribed to same event)
        // Wait a bit for translations to load, then reload scenarios
        await Task.Delay(50);
        await LoadScenariosAsync();
    }

    private async Task LoadScenariosAsync()
    {
        try
        {
            var scenarios = await _loader.LoadAllFromDirectoryAsync(_scenariosPath);

            lock (_lock)
            {
                // Clear existing scenarios
                _scenarios.Clear();

                // Add newly loaded scenarios
                foreach (var scenario in scenarios)
                {
                    _scenarios[scenario.Id] = scenario;
                }
            }
        }
        catch (Exception)
        {
            // Log error but don't throw - graceful degradation
            // Existing scenarios remain in registry
        }
    }

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
