namespace TransparentAiAgentCore.Infrastructure.Localization;

/// <summary>
/// Service for looking up translations by key.
/// Supports fallback to base language (English).
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Gets translation for the specified key in current language.
    /// Falls back to English if key not found in current language.
    /// Returns null if key not found in any language.
    /// </summary>
    string? GetTranslation(string key);

    /// <summary>
    /// Loads translation files for current and base language.
    /// Called automatically on initialization and language change.
    /// </summary>
    Task LoadTranslationsAsync();
}
