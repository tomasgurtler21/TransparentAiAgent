using System.Text.Json;
using TransparentAiAgentCore.Application.Localization;

namespace TransparentAiAgentCore.Infrastructure.Localization;

/// <summary>
/// Loads flat key-value translation files and provides lookup.
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly string _translationsPath;
    private readonly ILanguageService _languageService;
    private Dictionary<string, string> _currentTranslations = new();
    private Dictionary<string, string> _baseTranslations = new(); // English fallback

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public TranslationService(string translationsPath, ILanguageService languageService)
    {
        _translationsPath = translationsPath ?? throw new ArgumentNullException(nameof(translationsPath));
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));

        // Subscribe to language changes
        _languageService.LanguageChanged += OnLanguageChanged;
    }

    private async void OnLanguageChanged(object? sender, string newLanguage)
    {
        await LoadTranslationsAsync();
    }

    public async Task LoadTranslationsAsync()
    {
        // Always load English as base/fallback
        _baseTranslations = await LoadTranslationFileAsync("en") ?? new Dictionary<string, string>();

        // Load current language (if not English)
        if (_languageService.CurrentLanguage != "en")
        {
            _currentTranslations = await LoadTranslationFileAsync(_languageService.CurrentLanguage)
                ?? new Dictionary<string, string>();
        }
        else
        {
            _currentTranslations = _baseTranslations;
        }
    }

    public string? GetTranslation(string key)
    {
        // Try current language first
        if (_currentTranslations.TryGetValue(key, out var translation))
            return translation;

        // Fallback to English
        if (_baseTranslations.TryGetValue(key, out var baseTranslation))
            return baseTranslation;

        // Key not found in any language
        return null;
    }

    private async Task<Dictionary<string, string>?> LoadTranslationFileAsync(string languageCode)
    {
        var filePath = Path.Combine(_translationsPath, $"{languageCode}.json");

        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
        }
        catch (Exception)
        {
            // Log error but don't throw - graceful degradation
            return null;
        }
    }
}
