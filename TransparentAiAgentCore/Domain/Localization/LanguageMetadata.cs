namespace TransparentAiAgentCore.Domain.Localization;

/// <summary>
/// Represents metadata for a language available in the application.
/// Defines language properties including display information and translation requirements.
/// </summary>
public class LanguageMetadata
{
    /// <summary>
    /// ISO 639-1 language code (e.g., "en", "de", "cs").
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// English name for the language (e.g., "German", "Czech").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Native language name as speakers refer to it (e.g., "Deutsch", "Čeština").
    /// Used for UI display.
    /// </summary>
    public required string NativeName { get; init; }

    /// <summary>
    /// Unicode emoji flag for visual identification (e.g., "🇩🇪", "🇨🇿").
    /// </summary>
    public required string Flag { get; init; }

    /// <summary>
    /// Indicates whether this is the default/fallback language.
    /// Only one language should have this set to true (typically English).
    /// </summary>
    public required bool IsDefault { get; init; }

    /// <summary>
    /// Indicates whether a {code}.json translation file is required for this language.
    /// Set to false for languages where content is hardcoded (e.g., English).
    /// </summary>
    public required bool RequiresTranslationFile { get; init; }
}
