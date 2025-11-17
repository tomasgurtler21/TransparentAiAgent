using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Localization;
using TransparentAiAgentCore.Infrastructure.Localization;

namespace TransparentAiAgentCore_Tests.Infrastructure.Localization;

[TestClass]
public class TranslationServiceTests
{
    private const string TestTranslationsPath = "./TestData/translations";

    [TestInitialize]
    public void Setup()
    {
        // Create test translation files
        Directory.CreateDirectory(TestTranslationsPath);
        CreateTestTranslationFile("en", new Dictionary<string, string>
        {
            { "test.key1", "English Value 1" },
            { "test.key2", "English Value 2" }
        });
        CreateTestTranslationFile("de", new Dictionary<string, string>
        {
            { "test.key1", "German Value 1" }
            // Missing test.key2 - should fallback to English
        });
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(TestTranslationsPath))
            Directory.Delete(TestTranslationsPath, true);
    }

    [TestMethod]
    public async Task GetTranslation_ExistingKey_ReturnsTranslation()
    {
        // Arrange
        var languageService = new LanguageService();
        languageService.SetLanguage("de");
        var translationService = new TranslationService(TestTranslationsPath, languageService);
        await translationService.LoadTranslationsAsync();

        // Act
        var result = translationService.GetTranslation("test.key1");

        // Assert
        Assert.AreEqual("German Value 1", result);
    }

    [TestMethod]
    public async Task GetTranslation_MissingKey_FallbackToEnglish()
    {
        // Arrange
        var languageService = new LanguageService();
        languageService.SetLanguage("de");
        var translationService = new TranslationService(TestTranslationsPath, languageService);
        await translationService.LoadTranslationsAsync();

        // Act
        var result = translationService.GetTranslation("test.key2");

        // Assert
        Assert.AreEqual("English Value 2", result); // Falls back to English
    }

    [TestMethod]
    public async Task GetTranslation_KeyNotInAnyLanguage_ReturnsNull()
    {
        // Arrange
        var languageService = new LanguageService();
        var translationService = new TranslationService(TestTranslationsPath, languageService);
        await translationService.LoadTranslationsAsync();

        // Act
        var result = translationService.GetTranslation("nonexistent.key");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetTranslation_EnglishLanguage_ReturnsEnglishValue()
    {
        // Arrange
        var languageService = new LanguageService(); // Default "en"
        var translationService = new TranslationService(TestTranslationsPath, languageService);
        await translationService.LoadTranslationsAsync();

        // Act
        var result = translationService.GetTranslation("test.key1");

        // Assert
        Assert.AreEqual("English Value 1", result);
    }

    [TestMethod]
    public async Task LanguageChange_ReloadsTranslations()
    {
        // Arrange
        var languageService = new LanguageService();
        var translationService = new TranslationService(TestTranslationsPath, languageService);
        await translationService.LoadTranslationsAsync();

        // Act
        languageService.SetLanguage("de");
        await Task.Delay(100); // Give event handler time to reload

        var result = translationService.GetTranslation("test.key1");

        // Assert
        Assert.AreEqual("German Value 1", result);
    }

    // Helper methods
    private void CreateTestTranslationFile(string lang, Dictionary<string, string> translations)
    {
        var json = JsonSerializer.Serialize(translations, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(Path.Combine(TestTranslationsPath, $"{lang}.json"), json);
    }
}
