namespace TransparentAiAgentCore.Application.Localization;

/// <summary>
/// Manages current UI language selection.
/// Can be extended for app-wide localization.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Gets the currently selected language code (e.g., "en", "de", "cs").
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Sets the current language and raises LanguageChanged event if different.
    /// </summary>
    /// <param name="languageCode">ISO language code</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Raised when language changes to a different value.
    /// </summary>
    event EventHandler<string>? LanguageChanged;
}
