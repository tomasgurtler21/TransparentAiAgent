namespace TransparentAiAgentCore.Application.Localization;

/// <summary>
/// Simple in-memory language service with no persistence.
/// Defaults to English ("en").
/// </summary>
public class LanguageService : ILanguageService
{
    public string CurrentLanguage { get; private set; } = "en";

    public event EventHandler<string>? LanguageChanged;

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException(
                "Language code cannot be null or whitespace",
                nameof(languageCode));
        }

        if (CurrentLanguage != languageCode)
        {
            CurrentLanguage = languageCode;
            LanguageChanged?.Invoke(this, languageCode);
        }
    }
}
