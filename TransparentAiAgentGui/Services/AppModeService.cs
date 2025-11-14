using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Configuration;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Application.Conversation;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentGui.Services;

/// <summary>
/// Implementation of application mode service coordinating mode switches.
/// Registered as Scoped service (per-user isolation, matches ConversationManager lifetime).
/// Thread-safe: Uses lock for mode switching operation.
/// </summary>
public class AppModeService : IAppModeService
{
    private readonly IUIControlService _uiControlService;
    private readonly IConfigurationService _configService;
    private readonly IConversationManager _conversationManager;
    private readonly AppConfiguration _appConfiguration;
    private readonly ILogger<AppModeService> _logger;
    private readonly object _modeLock = new object();

    public AppMode CurrentMode => _uiControlService.GetCurrentState().CurrentMode;

    public event EventHandler<AppMode>? ModeChanged;

    public AppModeService(
        IUIControlService uiControlService,
        IConfigurationService configService,
        IConversationManager conversationManager,
        AppConfiguration appConfiguration,
        ILogger<AppModeService> logger)
    {
        _uiControlService = uiControlService ?? throw new ArgumentNullException(nameof(uiControlService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("AppModeService created (Scoped)");
    }

    public Task SwitchModeAsync(AppMode newMode, bool clearConversation = false)
    {
        lock (_modeLock)
        {
            var currentMode = CurrentMode;

            // Don't switch if already in the target mode
            if (currentMode == newMode)
            {
                _logger.LogInformation("Already in {Mode} mode, skipping switch", newMode);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Switching from {CurrentMode} to {NewMode} mode", currentMode, newMode);
        }

        try
        {
            // Step 1: Switch UI state via UIControlService
            var uiResult = _uiControlService.SwitchMode(newMode);
            if (!uiResult.Success)
            {
                throw new InvalidOperationException($"Failed to switch UI state: {uiResult.Error}");
            }

            // Step 2: Get the appropriate system prompt for the new mode
            var systemPrompt = GetSystemPromptForMode(newMode);

            // Step 3: Update system prompt in conversation manager (in-memory only)
            // IMPORTANT: We do NOT persist to configuration here. Teaching mode uses a hardcoded
            // prompt that should never be saved to config. Normal mode always uses the original
            // config value from appsettings.json. Only manual user edits via Configuration page
            // should persist to config.
            _conversationManager.UpdateSystemPrompt(systemPrompt);
            _logger.LogInformation("Updated system prompt in ConversationManager (in-memory only)");

            // Step 4: Optionally clear conversation history
            if (clearConversation)
            {
                _conversationManager.ClearConversation();
                _logger.LogInformation("Cleared conversation history");
            }

            // Step 5: Fire mode changed event
            ModeChanged?.Invoke(this, newMode);
            _logger.LogInformation("Successfully switched to {Mode} mode", newMode);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to {Mode} mode", newMode);
            throw;
        }
    }

    private string GetSystemPromptForMode(AppMode mode)
    {
        return mode switch
        {
            AppMode.Normal => GetNormalModePrompt(),
            AppMode.Teaching => GetTeachingModePrompt(),
            _ => throw new ArgumentException($"Unknown mode: {mode}", nameof(mode))
        };
    }

    private string GetNormalModePrompt()
    {
        // Use the configured system prompt for normal mode
        return _appConfiguration.Agent.SystemPrompt;
    }

    private string GetTeachingModePrompt()
    {
        // Use hardcoded teaching mode prompt (not user-configurable for security)
        return TeachingModeConstants.TEACHING_MODE_SYSTEM_PROMPT;
    }
}
