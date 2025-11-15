using System.Text.Json;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

/// <summary>
/// Service for loading and validating LLM configuration from JSON sources.
/// Supports multi-provider configuration with validation.
/// </summary>
public class LLMConfigurationLoader
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the LLMConfigurationLoader.
    /// </summary>
    public LLMConfigurationLoader()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Loads and validates LLM configuration from a JSON string.
    /// </summary>
    /// <param name="json">JSON string containing the TransparentAiAgent configuration section</param>
    /// <returns>Loaded and validated LLMConfiguration</returns>
    /// <exception cref="ArgumentException">Thrown when JSON is null or whitespace</exception>
    /// <exception cref="ConfigurationException">Thrown when configuration is invalid or missing required sections</exception>
    public LLMConfiguration LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON cannot be null or whitespace", nameof(json));
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var llmSection = ExtractLLMSection(root);
            var llmConfig = DeserializeLLMConfiguration(llmSection);

            ValidateConfiguration(llmConfig);

            return llmConfig;
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Invalid JSON in configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extracts the LLM configuration section from the JSON root element.
    /// </summary>
    private JsonElement ExtractLLMSection(JsonElement root)
    {
        if (!root.TryGetProperty("TransparentAiAgent", out var agentSection))
        {
            throw new ConfigurationException("TransparentAiAgent configuration section not found");
        }

        if (!agentSection.TryGetProperty("LLM", out var llmSection))
        {
            throw new ConfigurationException("LLM configuration section not found");
        }

        return llmSection;
    }

    /// <summary>
    /// Deserializes the LLM configuration from a JSON element.
    /// </summary>
    private LLMConfiguration DeserializeLLMConfiguration(JsonElement llmSection)
    {
        var llmConfig = JsonSerializer.Deserialize<LLMConfiguration>(llmSection.GetRawText(), _jsonOptions);

        if (llmConfig == null)
        {
            throw new ConfigurationException("Failed to deserialize LLM configuration");
        }

        return llmConfig;
    }

    /// <summary>
    /// Validates the LLM configuration structure and ensures required properties are present.
    /// </summary>
    /// <param name="config">LLMConfiguration to validate</param>
    /// <exception cref="ConfigurationException">Thrown when validation fails</exception>
    private void ValidateConfiguration(LLMConfiguration config)
    {
        if (config.Providers == null || config.Providers.Count == 0)
        {
            throw new ConfigurationException(
                "No providers configured. At least one provider must be defined in the 'Providers' section.");
        }

        if (string.IsNullOrWhiteSpace(config.ActiveProvider))
        {
            throw new ConfigurationException(
                "ActiveProvider not specified. The 'ActiveProvider' property must be set to a valid provider name.");
        }

        if (!config.Providers.ContainsKey(config.ActiveProvider))
        {
            var availableProviders = string.Join(", ", config.Providers.Keys);
            throw new ConfigurationException(
                $"ActiveProvider '{config.ActiveProvider}' not found in Providers. " +
                $"Available providers: {availableProviders}");
        }
    }
}
