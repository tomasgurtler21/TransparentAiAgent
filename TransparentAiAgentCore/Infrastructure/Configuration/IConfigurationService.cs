using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

public interface IConfigurationService
{
    /// <summary>
    /// Load configuration from default sources
    /// </summary>
    AppConfiguration LoadConfiguration();

    /// <summary>
    /// Load configuration from specific file
    /// </summary>
    AppConfiguration LoadConfiguration(string filePath);

    /// <summary>
    /// Save configuration to file
    /// </summary>
    void SaveConfiguration(AppConfiguration config, string filePath);

    /// <summary>
    /// Get current active configuration
    /// </summary>
    AppConfiguration GetConfiguration();

    /// <summary>
    /// Update configuration at runtime
    /// </summary>
    void UpdateConfiguration(AppConfiguration config);

    /// <summary>
    /// Updates the system prompt in configuration and saves to file.
    /// Returns the updated configuration.
    /// </summary>
    Task<AppConfiguration> UpdateSystemPromptAsync(string newPrompt, string? filePath = null);

    /// <summary>
    /// Updates agent configuration (context window size) and saves to file.
    /// Returns the updated configuration.
    /// </summary>
    Task<AppConfiguration> UpdateAgentConfigAsync(int contextWindowSize, string? filePath = null);

    /// <summary>
    /// Updates LLM parameters in configuration and saves to file.
    /// Returns the updated configuration.
    /// </summary>
    Task<AppConfiguration> UpdateLLMParametersAsync(double temperature, int maxTokens, double topP, string? filePath = null);
}
