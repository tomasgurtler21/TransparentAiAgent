using TransparentAiAgentCore.Domain.Localization;

namespace TransparentAiAgentCore.Infrastructure.Localization;

/// <summary>
/// Service for discovering and accessing language metadata.
/// Provides information about available languages in the application.
/// </summary>
public interface ILanguageMetadataService
{
    /// <summary>
    /// Gets all available languages from metadata.
    /// Results are cached after first load for performance.
    /// </summary>
    /// <returns>Read-only list of all language metadata.</returns>
    /// <exception cref="FileNotFoundException">If language-metadata.json file is not found.</exception>
    /// <exception cref="System.Text.Json.JsonException">If the metadata file contains invalid JSON.</exception>
    /// <exception cref="ArgumentException">If metadata validation fails (no default, multiple defaults, empty list).</exception>
    Task<IReadOnlyList<LanguageMetadata>> GetAvailableLanguagesAsync();

    /// <summary>
    /// Gets a specific language by its ISO 639-1 language code.
    /// </summary>
    /// <param name="code">The language code (e.g., "en", "de", "cs").</param>
    /// <returns>The language metadata if found; otherwise, null.</returns>
    Task<LanguageMetadata?> GetLanguageByCodeAsync(string code);

    /// <summary>
    /// Gets the default language (where IsDefault = true).
    /// Typically returns English in the current implementation.
    /// </summary>
    /// <returns>The default language metadata.</returns>
    /// <exception cref="ArgumentException">If no default language is configured in metadata.</exception>
    Task<LanguageMetadata> GetDefaultLanguageAsync();
}
