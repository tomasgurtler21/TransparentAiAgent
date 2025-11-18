using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

/// <summary>
/// Service for managing user-specific settings.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Loads user settings from AppData directory.
    /// Returns default settings if file doesn't exist.
    /// </summary>
    UserSettings LoadSettings();

    /// <summary>
    /// Saves user settings to AppData directory.
    /// </summary>
    void SaveSettings(UserSettings settings);

    /// <summary>
    /// Updates context window size and saves.
    /// </summary>
    void UpdateContextWindowSize(int contextWindowSize);

    /// <summary>
    /// Updates enable memory setting and saves.
    /// </summary>
    void UpdateEnableMemory(bool enableMemory);

    /// <summary>
    /// Gets current in-memory settings without reloading from disk.
    /// </summary>
    UserSettings GetCurrentSettings();
}
