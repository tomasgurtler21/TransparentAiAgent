using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentGui.Services;

/// <summary>
/// Service for managing application mode (Normal vs Teaching) and coordinating mode switches.
/// </summary>
public interface IAppModeService
{
    /// <summary>
    /// Gets the current application mode.
    /// </summary>
    AppMode CurrentMode { get; }

    /// <summary>
    /// Switches to a new application mode.
    /// This coordinates:
    /// - UI state changes via UIControlService
    /// - System prompt updates via ConversationManager
    /// - Configuration persistence via ConfigurationService
    /// </summary>
    /// <param name="newMode">The mode to switch to</param>
    /// <param name="clearConversation">Whether to clear the conversation history on mode switch (default: true)</param>
    Task SwitchModeAsync(AppMode newMode, bool clearConversation = true);

    /// <summary>
    /// Event raised when the application mode changes.
    /// </summary>
    event EventHandler<AppMode>? ModeChanged;
}
