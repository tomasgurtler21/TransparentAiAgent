using System.Globalization;
using System.Text.Json;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

public class ConfigurationService : IConfigurationService
{
    private AppConfiguration _currentConfiguration;
    private readonly string _defaultConfigPath = "appsettings.json";
    private readonly string _defaultToolsPath = "tools.json";
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
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
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
            // Force InvariantCulture for JSON parsing to avoid culture-specific number formatting issues
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

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

                    // Load tools configuration from separate file
                    LoadToolsConfiguration(config, filePath);

                    _currentConfiguration = config;
                    return config;
                }

                // If no TransparentAiAgent section, return default
                return new AppConfiguration();
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
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

    private void LoadToolsConfiguration(AppConfiguration config, string configFilePath)
    {
        // Determine tools.json path based on appsettings.json location
        var configDir = Path.GetDirectoryName(configFilePath);
        var toolsPath = string.IsNullOrEmpty(configDir)
            ? _defaultToolsPath
            : Path.Combine(configDir, "tools.json");

        // If tools.json doesn't exist, use default tools configuration
        if (!File.Exists(toolsPath))
        {
            config.Tools = new ToolsConfiguration();
            return;
        }

        try
        {
            var json = File.ReadAllText(toolsPath);
            var toolsConfig = JsonSerializer.Deserialize<ToolsConfiguration>(json, _jsonOptions);
            if (toolsConfig != null)
            {
                config.Tools = toolsConfig;
            }
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Invalid JSON in tools configuration file: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Error reading tools configuration file: {ex.Message}", ex);
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
            // Save tools configuration to separate file
            SaveToolsConfiguration(config, filePath);

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

    private void SaveToolsConfiguration(AppConfiguration config, string configFilePath)
    {
        // Determine tools.json path based on appsettings.json location
        var configDir = Path.GetDirectoryName(configFilePath);
        var toolsPath = string.IsNullOrEmpty(configDir)
            ? _defaultToolsPath
            : Path.Combine(configDir, "tools.json");

        try
        {
            var json = JsonSerializer.Serialize(config.Tools, _jsonOptions);
            File.WriteAllText(toolsPath, json);
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Error serializing tools configuration: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Error writing tools configuration file: {ex.Message}", ex);
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

    public async Task<AppConfiguration> UpdateAgentConfigAsync(int contextWindowSize, string? filePath = null)
    {
        // Validate parameter
        if (contextWindowSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(contextWindowSize), "ContextWindowSize must be greater than 0");

        // Update in-memory configuration
        _currentConfiguration.Agent.ContextWindowSize = contextWindowSize;

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

    /// <summary>
    /// Updates the active LLM provider.
    /// </summary>
    /// <param name="providerName">The name of the provider to activate.</param>
    /// <param name="filePath">Optional file path to save the configuration. If not specified, uses default path.</param>
    /// <returns>The updated configuration.</returns>
    public async Task<AppConfiguration> UpdateActiveProviderAsync(string providerName, string? filePath = null)
    {
        // Validate parameter
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or whitespace", nameof(providerName));

        // Verify provider exists in configuration
        if (_currentConfiguration.LLM.Providers == null || _currentConfiguration.LLM.Providers.Count == 0)
            throw new ConfigurationException("No providers configured in LLM configuration");

        if (!_currentConfiguration.LLM.Providers.ContainsKey(providerName))
            throw new ConfigurationException($"Provider '{providerName}' not found in configured providers");

        // Update in-memory configuration
        _currentConfiguration.LLM.ActiveProvider = providerName;

        // Validate the updated configuration
        _currentConfiguration.Validate();

        // Save to file
        var targetPath = filePath ?? _defaultConfigPath;
        await Task.Run(() => SaveConfiguration(_currentConfiguration, targetPath));

        return _currentConfiguration;
    }
}
