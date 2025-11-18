using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Infrastructure.Localization;

namespace TransparentAiAgentCore.Infrastructure.Scenarios;

/// <summary>
/// Loads scenario definitions from JSON files.
/// Responsible for deserializing JSON to ScenarioDefinition objects and validating the data.
/// </summary>
public class JsonScenarioLoader
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<JsonScenarioLoader> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public JsonScenarioLoader(ITranslationService translationService, ILogger<JsonScenarioLoader> logger)
    {
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads a single scenario from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    /// <returns>The loaded ScenarioDefinition</returns>
    /// <exception cref="FileNotFoundException">File does not exist</exception>
    /// <exception cref="JsonException">Invalid JSON format</exception>
    /// <exception cref="ArgumentException">Missing required fields or validation failed</exception>
    public async Task<ScenarioDefinition> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Scenario file not found: {filePath}", filePath);
        }

        string jsonContent = await File.ReadAllTextAsync(filePath);

        ScenarioDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ScenarioDto>(jsonContent, JsonOptions);
        }
        catch (JsonException)
        {
            throw; // Re-throw JSON parsing errors
        }

        if (dto == null)
        {
            throw new JsonException($"Failed to deserialize scenario from {filePath}");
        }

        // Convert DTO to domain model (this will validate required fields)
        return dto.ToScenarioDefinition(_translationService);
    }

    /// <summary>
    /// Loads all scenarios from JSON files in a directory.
    /// Only processes *.json files. Skips files that fail to load.
    /// </summary>
    /// <param name="directoryPath">Path to directory containing JSON scenario files</param>
    /// <returns>List of successfully loaded ScenarioDefinitions</returns>
    /// <exception cref="DirectoryNotFoundException">Directory does not exist</exception>
    public async Task<List<ScenarioDefinition>> LoadAllFromDirectoryAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Scenario directory not found: {directoryPath}");
        }

        var scenarios = new List<ScenarioDefinition>();
        var jsonFiles = Directory.GetFiles(directoryPath, "*.json");

        foreach (var filePath in jsonFiles)
        {
            try
            {
                var scenario = await LoadFromFileAsync(filePath);
                scenarios.Add(scenario);
                _logger.LogInformation("Successfully loaded scenario '{ScenarioId}' from {FilePath}", scenario.Id, filePath);
            }
            catch (Exception ex)
            {
                // Log the error with details but continue loading other scenarios
                // This allows the system to be resilient to malformed scenario files
                _logger.LogError(ex,
                    "Failed to load scenario from {FilePath}. Error: {ErrorMessage}",
                    filePath, ex.Message);
                continue;
            }
        }

        return scenarios;
    }

    #region DTOs for JSON Deserialization

    /// <summary>
    /// DTO for JSON deserialization. Maps snake_case JSON to C# objects.
    /// </summary>
    private class ScenarioDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("nameKey")]
        public string? NameKey { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("descriptionKey")]
        public string? DescriptionKey { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }

        [JsonPropertyName("estimatedDurationSeconds")]
        public int? EstimatedDurationSeconds { get; set; }

        [JsonPropertyName("steps")]
        public List<ScenarioStepDto>? Steps { get; set; }

        public ScenarioDefinition ToScenarioDefinition(ITranslationService translationService)
        {
            // Resolve name: translation key → translated value OR inline fallback
            var resolvedName = NameKey != null
                ? translationService.GetTranslation(NameKey) ?? Name
                : Name;

            // Resolve description
            var resolvedDescription = DescriptionKey != null
                ? translationService.GetTranslation(DescriptionKey) ?? Description
                : Description;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Scenario 'id' is required");
            if (string.IsNullOrWhiteSpace(resolvedName))
                throw new ArgumentException("Scenario 'name' or translation is required");
            if (Steps == null || Steps.Count == 0)
                throw new ArgumentException("Scenario must have at least one step");

            var domainSteps = Steps.Select(s => s.ToScenarioStep(translationService)).ToList();

            return new ScenarioDefinition(
                id: Id,
                name: resolvedName!,
                steps: domainSteps,
                description: resolvedDescription,
                category: Category,
                difficulty: Difficulty,
                estimatedDurationSeconds: EstimatedDurationSeconds
            );
        }
    }

    /// <summary>
    /// DTO for scenario step JSON deserialization.
    /// Supports both basic and advanced (Phase 10b) scenario features.
    /// </summary>
    private class ScenarioStepDto
    {
        // Basic properties
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("contentKey")]
        public string? ContentKey { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("delayMs")]
        public int? DelayMs { get; set; }

        [JsonPropertyName("delay")]
        public int? Delay { get; set; }

        [JsonPropertyName("config_overlay")]
        public Dictionary<string, object>? ConfigOverlay { get; set; }

        [JsonPropertyName("overlay")]
        public Dictionary<string, object>? Overlay { get; set; }

        // Advanced properties (Phase 10b)
        [JsonPropertyName("annotationKey")]
        public string? AnnotationKey { get; set; }

        [JsonPropertyName("annotation")]
        public string? Annotation { get; set; }

        [JsonPropertyName("visibleTo")]
        public string? VisibleTo { get; set; }

        [JsonPropertyName("condition")]
        public string? Condition { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object>? Parameters { get; set; }

        [JsonPropertyName("onTimeout")]
        public string? OnTimeout { get; set; }

        [JsonPropertyName("tool")]
        public string? Tool { get; set; }

        [JsonPropertyName("arguments")]
        public Dictionary<string, object>? Arguments { get; set; }

        public ScenarioStep ToScenarioStep(ITranslationService translationService)
        {
            // Resolve content using translation key
            var resolvedContent = ContentKey != null
                ? translationService.GetTranslation(ContentKey) ?? Content
                : Content;

            // Resolve annotation using translation key
            var resolvedAnnotation = AnnotationKey != null
                ? translationService.GetTranslation(AnnotationKey) ?? Annotation
                : Annotation;

            // Map JSON string to enum
            var stepType = Type?.ToLowerInvariant() switch
            {
                "auto_message" => ScenarioStepType.AutoMessage,
                "wait_for_response" => ScenarioStepType.WaitForResponse,
                "agent_prompt" => ScenarioStepType.AgentPrompt,
                // Advanced step types (Phase 10b)
                "scenario_user_message" => ScenarioStepType.ScenarioUserMessage,
                "wait_for_condition" => ScenarioStepType.WaitForCondition,
                "apply_config_overlay" => ScenarioStepType.ApplyConfigOverlay,
                "restore_config_overlay" => ScenarioStepType.RestoreConfigOverlay,
                "disable_user_input" => ScenarioStepType.DisableUserInput,
                "enable_user_input" => ScenarioStepType.EnableUserInput,
                "delay" => ScenarioStepType.Delay,
                "ui_control" => ScenarioStepType.UIControl,
                "pause_for_user" => ScenarioStepType.PauseForUser,
                _ => throw new ArgumentException($"Unknown scenario step type: {Type}")
            };

            // Parse MessageVisibility
            MessageVisibility? visibleTo = null;
            if (!string.IsNullOrWhiteSpace(VisibleTo))
            {
                visibleTo = VisibleTo.ToLowerInvariant() switch
                {
                    "model_only" => MessageVisibility.ModelOnly,
                    "user_only" => MessageVisibility.UserOnly,
                    "both" => MessageVisibility.Both,
                    _ => throw new ArgumentException($"Unknown visibility type: {VisibleTo}")
                };
            }

            // Support both "delay" and "delayMs" for flexibility
            var delayMsValue = DelayMs ?? Delay ?? 0;

            // Support both "overlay" and "config_overlay" for flexibility
            var configOverlayValue = ConfigOverlay ?? Overlay;

            return new ScenarioStep(
                type: stepType,
                content: resolvedContent,
                delayMs: delayMsValue,
                configOverlay: configOverlayValue,
                annotation: resolvedAnnotation,
                visibleTo: visibleTo,
                condition: Condition,
                conditionParameters: Parameters,
                onTimeout: OnTimeout,
                uiControlTool: Tool,
                uiControlArguments: Arguments
            );
        }
    }

    #endregion
}
