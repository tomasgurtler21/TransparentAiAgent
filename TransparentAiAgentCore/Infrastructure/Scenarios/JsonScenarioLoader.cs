using System.Text.Json;
using System.Text.Json.Serialization;
using TransparentAiAgentCore.Domain.Scenarios;

namespace TransparentAiAgentCore.Infrastructure.Scenarios;

/// <summary>
/// Loads scenario definitions from JSON files.
/// Responsible for deserializing JSON to ScenarioDefinition objects and validating the data.
/// </summary>
public class JsonScenarioLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

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
        return dto.ToScenarioDefinition();
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
            }
            catch (Exception)
            {
                // Skip files that fail to load (invalid JSON, missing fields, etc.)
                // This allows the system to be resilient to malformed scenario files
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

        [JsonPropertyName("name")]
        public string? Name { get; set; }

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

        public ScenarioDefinition ToScenarioDefinition()
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Scenario 'id' is required");
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("Scenario 'name' is required");
            if (Steps == null || Steps.Count == 0)
                throw new ArgumentException("Scenario must have at least one step");

            var domainSteps = Steps.Select(s => s.ToScenarioStep()).ToList();

            return new ScenarioDefinition(
                id: Id,
                name: Name,
                steps: domainSteps,
                description: Description,
                category: Category,
                difficulty: Difficulty,
                estimatedDurationSeconds: EstimatedDurationSeconds
            );
        }
    }

    /// <summary>
    /// DTO for scenario step JSON deserialization.
    /// </summary>
    private class ScenarioStepDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("delayMs")]
        public int? DelayMs { get; set; }

        [JsonPropertyName("config_overlay")]
        public Dictionary<string, object>? ConfigOverlay { get; set; }

        public ScenarioStep ToScenarioStep()
        {
            // Map JSON string to enum
            var stepType = Type?.ToLowerInvariant() switch
            {
                "auto_message" => ScenarioStepType.AutoMessage,
                "wait_for_response" => ScenarioStepType.WaitForResponse,
                "agent_prompt" => ScenarioStepType.AgentPrompt,
                "completion_message" => ScenarioStepType.CompletionMessage,
                _ => throw new ArgumentException($"Unknown scenario step type: {Type}")
            };

            return new ScenarioStep(
                type: stepType,
                content: Content,
                delayMs: DelayMs ?? 0,
                configOverlay: ConfigOverlay
            );
        }
    }

    #endregion
}
