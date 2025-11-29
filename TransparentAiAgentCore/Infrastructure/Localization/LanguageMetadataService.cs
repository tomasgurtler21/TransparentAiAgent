using System.Text.Json;
using TransparentAiAgentCore.Domain.Localization;

namespace TransparentAiAgentCore.Infrastructure.Localization;

/// <summary>
/// Service for discovering and accessing language metadata from a JSON configuration file.
/// Implements lazy loading with caching for performance.
/// </summary>
public class LanguageMetadataService : ILanguageMetadataService
{
    private readonly string _metadataFilePath;
    private readonly Lazy<Task<IReadOnlyList<LanguageMetadata>>> _languagesLazy;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageMetadataService"/> class.
    /// </summary>
    /// <param name="metadataFilePath">Absolute path to the language-metadata.json file.</param>
    /// <exception cref="ArgumentNullException">If metadataFilePath is null.</exception>
    public LanguageMetadataService(string metadataFilePath)
    {
        _metadataFilePath = metadataFilePath ?? throw new ArgumentNullException(nameof(metadataFilePath));
        _languagesLazy = new Lazy<Task<IReadOnlyList<LanguageMetadata>>>(LoadAndValidateMetadataAsync);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LanguageMetadata>> GetAvailableLanguagesAsync()
    {
        return _languagesLazy.Value;
    }

    /// <inheritdoc/>
    public async Task<LanguageMetadata?> GetLanguageByCodeAsync(string code)
    {
        var languages = await GetAvailableLanguagesAsync();
        return languages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<LanguageMetadata> GetDefaultLanguageAsync()
    {
        var languages = await GetAvailableLanguagesAsync();
        var defaultLanguage = languages.FirstOrDefault(l => l.IsDefault);

        if (defaultLanguage == null)
        {
            throw new ArgumentException(
                "No default language is configured in metadata. " +
                "Exactly one language must have IsDefault=true.");
        }

        return defaultLanguage;
    }

    private async Task<IReadOnlyList<LanguageMetadata>> LoadAndValidateMetadataAsync()
    {
        // Check if file exists
        if (!File.Exists(_metadataFilePath))
        {
            throw new FileNotFoundException(
                $"Language metadata file not found at: {_metadataFilePath}");
        }

        // Read and deserialize JSON
        string json;
        try
        {
            json = await File.ReadAllTextAsync(_metadataFilePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to read language metadata file: {_metadataFilePath}", ex);
        }

        LanguageMetadataFile? metadataFile;
        try
        {
            metadataFile = JsonSerializer.Deserialize<LanguageMetadataFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException(
                $"Invalid JSON in language metadata file: {_metadataFilePath}", ex);
        }

        if (metadataFile?.Languages == null)
        {
            throw new ArgumentException(
                $"Language metadata file must contain a 'languages' array: {_metadataFilePath}");
        }

        var languages = metadataFile.Languages;

        // Validate: cannot be empty
        if (languages.Count == 0)
        {
            throw new ArgumentException(
                "Language metadata file must contain at least one language.");
        }

        // Validate: exactly one default language
        var defaultCount = languages.Count(l => l.IsDefault);
        if (defaultCount == 0)
        {
            throw new ArgumentException(
                "Language metadata must contain exactly one language with IsDefault=true. " +
                "Found 0 default languages.");
        }
        if (defaultCount > 1)
        {
            throw new ArgumentException(
                $"Language metadata must contain exactly one language with IsDefault=true. " +
                $"Found {defaultCount} default languages.");
        }

        // Validate: unique language codes
        var duplicateCodes = languages
            .GroupBy(l => l.Code, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateCodes.Any())
        {
            throw new ArgumentException(
                $"Language metadata contains duplicate language codes: {string.Join(", ", duplicateCodes)}");
        }

        return languages;
    }

    /// <summary>
    /// Internal class for deserializing the language-metadata.json file.
    /// </summary>
    private class LanguageMetadataFile
    {
        public required List<LanguageMetadata> Languages { get; init; }
    }
}
