using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Localization;
using TransparentAiAgentCore.Infrastructure.Localization;

namespace TransparentAiAgentCore_Tests.Infrastructure.Localization;

[TestClass]
public class LanguageMetadataServiceTests
{
    private const string TestDataPath = "./TestData/translations";
    private string _validMetadataPath = null!;
    private string _noDefaultMetadataPath = null!;
    private string _multipleDefaultsMetadataPath = null!;
    private string _emptyMetadataPath = null!;
    private string _invalidJsonPath = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create test data directory
        Directory.CreateDirectory(TestDataPath);

        // Create valid metadata file
        _validMetadataPath = Path.Combine(TestDataPath, "language-metadata-valid.json");
        CreateValidMetadataFile(_validMetadataPath);

        // Create metadata with no default
        _noDefaultMetadataPath = Path.Combine(TestDataPath, "language-metadata-no-default.json");
        CreateNoDefaultMetadataFile(_noDefaultMetadataPath);

        // Create metadata with multiple defaults
        _multipleDefaultsMetadataPath = Path.Combine(TestDataPath, "language-metadata-multiple-defaults.json");
        CreateMultipleDefaultsMetadataFile(_multipleDefaultsMetadataPath);

        // Create empty metadata file
        _emptyMetadataPath = Path.Combine(TestDataPath, "language-metadata-empty.json");
        CreateEmptyMetadataFile(_emptyMetadataPath);

        // Create invalid JSON file
        _invalidJsonPath = Path.Combine(TestDataPath, "language-metadata-invalid.json");
        CreateInvalidJsonFile(_invalidJsonPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(TestDataPath))
            Directory.Delete(TestDataPath, true);
    }

    [TestMethod]
    public async Task GetAvailableLanguages_ValidMetadata_ReturnsAllLanguages()
    {
        // Arrange
        var service = new LanguageMetadataService(_validMetadataPath);

        // Act
        var languages = await service.GetAvailableLanguagesAsync();

        // Assert
        Assert.IsNotNull(languages);
        Assert.AreEqual(3, languages.Count);

        // Verify English
        var english = languages.FirstOrDefault(l => l.Code == "en");
        Assert.IsNotNull(english);
        Assert.AreEqual("English", english.DisplayName);
        Assert.AreEqual("English", english.NativeName);
        Assert.AreEqual("🇬🇧", english.Flag);
        Assert.IsTrue(english.IsDefault);
        Assert.IsFalse(english.RequiresTranslationFile);

        // Verify German
        var german = languages.FirstOrDefault(l => l.Code == "de");
        Assert.IsNotNull(german);
        Assert.AreEqual("German", german.DisplayName);
        Assert.AreEqual("Deutsch", german.NativeName);
        Assert.AreEqual("🇩🇪", german.Flag);
        Assert.IsFalse(german.IsDefault);
        Assert.IsTrue(german.RequiresTranslationFile);

        // Verify Czech
        var czech = languages.FirstOrDefault(l => l.Code == "cs");
        Assert.IsNotNull(czech);
        Assert.AreEqual("Czech", czech.DisplayName);
        Assert.AreEqual("Čeština", czech.NativeName);
        Assert.AreEqual("🇨🇿", czech.Flag);
        Assert.IsFalse(czech.IsDefault);
        Assert.IsTrue(czech.RequiresTranslationFile);
    }

    [TestMethod]
    public async Task GetAvailableLanguages_CalledTwice_ReturnsCachedResults()
    {
        // Arrange
        var service = new LanguageMetadataService(_validMetadataPath);

        // Act
        var languages1 = await service.GetAvailableLanguagesAsync();
        var languages2 = await service.GetAvailableLanguagesAsync();

        // Assert
        Assert.AreSame(languages1, languages2, "Should return same cached instance");
    }

    [TestMethod]
    public async Task GetLanguageByCode_ExistingCode_ReturnsLanguage()
    {
        // Arrange
        var service = new LanguageMetadataService(_validMetadataPath);

        // Act
        var language = await service.GetLanguageByCodeAsync("de");

        // Assert
        Assert.IsNotNull(language);
        Assert.AreEqual("de", language.Code);
        Assert.AreEqual("German", language.DisplayName);
        Assert.AreEqual("Deutsch", language.NativeName);
    }

    [TestMethod]
    public async Task GetLanguageByCode_NonExistingCode_ReturnsNull()
    {
        // Arrange
        var service = new LanguageMetadataService(_validMetadataPath);

        // Act
        var language = await service.GetLanguageByCodeAsync("fr");

        // Assert
        Assert.IsNull(language);
    }

    [TestMethod]
    public async Task GetDefaultLanguage_ValidMetadata_ReturnsEnglish()
    {
        // Arrange
        var service = new LanguageMetadataService(_validMetadataPath);

        // Act
        var defaultLanguage = await service.GetDefaultLanguageAsync();

        // Assert
        Assert.IsNotNull(defaultLanguage);
        Assert.AreEqual("en", defaultLanguage.Code);
        Assert.IsTrue(defaultLanguage.IsDefault);
    }

    [TestMethod]
    public async Task GetAvailableLanguages_MissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = new LanguageMetadataService("./nonexistent/metadata.json");

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            async () => await service.GetAvailableLanguagesAsync());
    }

    [TestMethod]
    public async Task GetAvailableLanguages_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var service = new LanguageMetadataService(_invalidJsonPath);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<JsonException>(
            async () => await service.GetAvailableLanguagesAsync());
    }

    [TestMethod]
    public async Task GetAvailableLanguages_NoDefaultLanguage_ThrowsArgumentException()
    {
        // Arrange
        var service = new LanguageMetadataService(_noDefaultMetadataPath);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await service.GetAvailableLanguagesAsync());

        Assert.IsTrue(exception.Message.Contains("Found 0 default languages"));
    }

    [TestMethod]
    public async Task GetAvailableLanguages_MultipleDefaults_ThrowsArgumentException()
    {
        // Arrange
        var service = new LanguageMetadataService(_multipleDefaultsMetadataPath);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await service.GetAvailableLanguagesAsync());

        Assert.IsTrue(exception.Message.Contains("Found 2 default languages"));
    }

    [TestMethod]
    public async Task GetAvailableLanguages_EmptyLanguagesList_ThrowsArgumentException()
    {
        // Arrange
        var service = new LanguageMetadataService(_emptyMetadataPath);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await service.GetAvailableLanguagesAsync());

        Assert.IsTrue(exception.Message.Contains("must contain at least one language"));
    }

    // Helper methods
    private void CreateValidMetadataFile(string path)
    {
        var metadata = new
        {
            languages = new[]
            {
                new
                {
                    code = "en",
                    displayName = "English",
                    nativeName = "English",
                    flag = "🇬🇧",
                    isDefault = true,
                    requiresTranslationFile = false
                },
                new
                {
                    code = "de",
                    displayName = "German",
                    nativeName = "Deutsch",
                    flag = "🇩🇪",
                    isDefault = false,
                    requiresTranslationFile = true
                },
                new
                {
                    code = "cs",
                    displayName = "Czech",
                    nativeName = "Čeština",
                    flag = "🇨🇿",
                    isDefault = false,
                    requiresTranslationFile = true
                }
            }
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    private void CreateNoDefaultMetadataFile(string path)
    {
        var metadata = new
        {
            languages = new[]
            {
                new
                {
                    code = "de",
                    displayName = "German",
                    nativeName = "Deutsch",
                    flag = "🇩🇪",
                    isDefault = false,
                    requiresTranslationFile = true
                }
            }
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    private void CreateMultipleDefaultsMetadataFile(string path)
    {
        var metadata = new
        {
            languages = new[]
            {
                new
                {
                    code = "en",
                    displayName = "English",
                    nativeName = "English",
                    flag = "🇬🇧",
                    isDefault = true,
                    requiresTranslationFile = false
                },
                new
                {
                    code = "de",
                    displayName = "German",
                    nativeName = "Deutsch",
                    flag = "🇩🇪",
                    isDefault = true, // Second default - invalid!
                    requiresTranslationFile = true
                }
            }
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    private void CreateEmptyMetadataFile(string path)
    {
        var metadata = new
        {
            languages = Array.Empty<object>()
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    private void CreateInvalidJsonFile(string path)
    {
        File.WriteAllText(path, "{ invalid json content }");
    }
}
