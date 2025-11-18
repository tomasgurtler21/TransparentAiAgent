using Microsoft.Extensions.Logging;
using System.Text.Json;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Infrastructure.DataPath;

namespace TransparentAiAgentCore.Infrastructure.Configuration;

/// <summary>
/// Implementation of IUserSettingsService that stores settings in AppData.
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private readonly IDataPathService _dataPathService;
    private readonly ILogger<UserSettingsService> _logger;
    private UserSettings _currentSettings;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserSettingsService(
        IDataPathService dataPathService,
        ILogger<UserSettingsService> logger)
    {
        _dataPathService = dataPathService ?? throw new ArgumentNullException(nameof(dataPathService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        _currentSettings = LoadSettings();

        // Ensure settings file exists with defaults
        var filePath = GetSettingsFilePath();
        if (!File.Exists(filePath))
        {
            SaveSettings(_currentSettings);
            _logger.LogInformation("Created default user settings file at {FilePath}", filePath);
        }
    }

    private string GetSettingsFilePath()
    {
        return Path.Combine(_dataPathService.GetUserDataRoot(), "user-settings.json");
    }

    public UserSettings LoadSettings()
    {
        var filePath = GetSettingsFilePath();

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("User settings file not found, using defaults");
            _currentSettings = new UserSettings();
            return _currentSettings;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, _jsonOptions);
            _currentSettings = settings ?? new UserSettings();
            _logger.LogInformation("Loaded user settings from {FilePath}", filePath);
            return _currentSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user settings from {FilePath}, using defaults", filePath);
            _currentSettings = new UserSettings();
            return _currentSettings;
        }
    }

    public void SaveSettings(UserSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        var filePath = GetSettingsFilePath();

        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(_dataPathService.GetUserDataRoot());

            // Serialize and save
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(filePath, json);

            _currentSettings = settings;
            _logger.LogInformation("Saved user settings to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user settings to {FilePath}", filePath);
            throw;
        }
    }

    public void UpdateContextWindowSize(int contextWindowSize)
    {
        _currentSettings.ContextWindowSize = contextWindowSize;
        SaveSettings(_currentSettings);
    }

    public void UpdateEnableMemory(bool enableMemory)
    {
        _currentSettings.EnableMemory = enableMemory;
        SaveSettings(_currentSettings);
    }

    public UserSettings GetCurrentSettings()
    {
        return _currentSettings;
    }
}
