using System.Text.Json;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

public class ConfigurationService : IConfigurationService
{
    private AppConfiguration _currentConfiguration;
    private readonly string _defaultConfigPath = "appsettings.json";
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService()
    {
        _currentConfiguration = new AppConfiguration();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public AppConfiguration LoadConfiguration()
    {
        return LoadConfiguration(_defaultConfigPath);
    }

    public AppConfiguration LoadConfiguration(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));

        if (!File.Exists(filePath))
            throw new ConfigurationException($"Configuration file not found: {filePath}");

        try
        {
            var json = File.ReadAllText(filePath);

            // Parse the JSON to get the TransparentAiAgent section
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Look for TransparentAiAgent section
            if (root.TryGetProperty("TransparentAiAgent", out var agentSection))
            {
                var config = JsonSerializer.Deserialize<AppConfiguration>(agentSection.GetRawText(), _jsonOptions);
                if (config == null)
                    throw new ConfigurationException("Failed to deserialize configuration");

                _currentConfiguration = config;
                return config;
            }

            // If no TransparentAiAgent section, return default
            return new AppConfiguration();
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Error reading configuration file: {ex.Message}", ex);
        }
    }

    public void SaveConfiguration(AppConfiguration config, string filePath)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));

        // Validate first
        config.Validate();

        try
        {
            // Create wrapper object with TransparentAiAgent section
            var wrapper = new
            {
                TransparentAiAgent = config
            };

            var json = JsonSerializer.Serialize(wrapper, _jsonOptions);
            File.WriteAllText(filePath, json);

            _currentConfiguration = config;
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Error serializing configuration: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Error writing configuration file: {ex.Message}", ex);
        }
    }

    public AppConfiguration GetConfiguration()
    {
        return _currentConfiguration;
    }

    public void UpdateConfiguration(AppConfiguration config)
    {
        config.Validate();
        _currentConfiguration = config;
    }

    public async Task<AppConfiguration> UpdateSystemPromptAsync(string newPrompt, string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(newPrompt))
            throw new ArgumentException("System prompt cannot be null or whitespace", nameof(newPrompt));

        // Update in-memory configuration
        _currentConfiguration.Agent.SystemPrompt = newPrompt;

        // Validate the updated configuration
        _currentConfiguration.Validate();

        // Save to file
        var targetPath = filePath ?? _defaultConfigPath;
        await Task.Run(() => SaveConfiguration(_currentConfiguration, targetPath));

        return _currentConfiguration;
    }

    public async Task<AppConfiguration> UpdateLLMParametersAsync(double temperature, int maxTokens, double topP, string? filePath = null)
    {
        // Validate parameters first
        if (temperature < 0 || temperature > 2)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0 and 2");

        if (maxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "MaxTokens must be greater than 0");

        if (topP < 0 || topP > 1)
            throw new ArgumentOutOfRangeException(nameof(topP), "TopP must be between 0 and 1");

        // Update in-memory configuration
        _currentConfiguration.LLM.Temperature = temperature;
        _currentConfiguration.LLM.MaxTokens = maxTokens;
        _currentConfiguration.LLM.TopP = topP;

        // Validate the updated configuration
        _currentConfiguration.Validate();

        // Save to file
        var targetPath = filePath ?? _defaultConfigPath;
        await Task.Run(() => SaveConfiguration(_currentConfiguration, targetPath));

        return _currentConfiguration;
    }
}
