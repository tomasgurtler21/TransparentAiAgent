# Scenario Localization Implementation Plan

**Created**: 2025-11-17
**Based On**: SCENARIO_LOCALIZATION_BRAINSTORM.md
**Target Languages**: English (en), German (de), Czech (cz)
**Approach**: Separate Translation Files + Base Scenario (Alternative 2 Variant)
**TDD Methodology**: Lean TDD (Red-Green-Refactor)

---

## Overview

This plan implements multi-language support for teaching scenarios using translation overlay files. Each session is independent and can be executed in isolation, following TDD principles.

**Architecture Pattern**:
- Base scenario JSON contains structure + English content + translation keys (e.g., `nameKey`, `contentKey`)
- Translation files (en.json, de.json, cz.json) are flat key-value dictionaries
- Loader looks up translations by key, falls back to inline English if key missing
- Industry-standard i18n pattern compatible with translation management tools

---

## Session 1: Language Service and Translation Schema

### Objective
Create language selection service and define translation file structure.

### Prerequisites
- Clean integration branch
- Existing scenario: `context-limits-advanced.json`

### TDD Components

#### 1.1: LanguageService (Domain/Application Layer)

**Location**: `TransparentAiAgentCore/Application/Localization/`

**Files to Create**:
- `ILanguageService.cs` (interface)
- `LanguageService.cs` (implementation)

**Tests Location**: `TransparentAiAgentCore_Tests/Application/Localization/LanguageServiceTests.cs`

**TDD Cycle**:

##### RED: Write tests first
```csharp
[TestClass]
public class LanguageServiceTests
{
    [TestMethod]
    public void CurrentLanguage_DefaultValue_IsEnglish()
    {
        // Arrange & Act
        var service = new LanguageService();

        // Assert
        Assert.AreEqual("en", service.CurrentLanguage);
    }

    [TestMethod]
    public void SetLanguage_ValidLanguage_UpdatesCurrentLanguage()
    {
        // Arrange
        var service = new LanguageService();

        // Act
        service.SetLanguage("de");

        // Assert
        Assert.AreEqual("de", service.CurrentLanguage);
    }

    [TestMethod]
    public void SetLanguage_DifferentLanguage_RaisesLanguageChangedEvent()
    {
        // Arrange
        var service = new LanguageService();
        string? eventLanguage = null;
        service.LanguageChanged += (sender, lang) => eventLanguage = lang;

        // Act
        service.SetLanguage("de");

        // Assert
        Assert.AreEqual("de", eventLanguage);
    }

    [TestMethod]
    public void SetLanguage_SameLanguage_DoesNotRaiseEvent()
    {
        // Arrange
        var service = new LanguageService();
        service.SetLanguage("de");
        int eventCount = 0;
        service.LanguageChanged += (sender, lang) => eventCount++;

        // Act
        service.SetLanguage("de"); // Same language

        // Assert
        Assert.AreEqual(0, eventCount);
    }

    [TestMethod]
    public void SetLanguage_NullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        var service = new LanguageService();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => service.SetLanguage(null!));
        Assert.ThrowsException<ArgumentException>(() => service.SetLanguage(""));
        Assert.ThrowsException<ArgumentException>(() => service.SetLanguage("   "));
    }
}
```

**Run Tests**: `dotnet test --filter "FullyQualifiedName~LanguageServiceTests"`
**Expected**: All tests FAIL (RED) - implementation doesn't exist yet

##### GREEN: Implement minimal code

**ILanguageService.cs**:
```csharp
namespace TransparentAiAgentCore.Application.Localization;

/// <summary>
/// Manages current UI language selection.
/// Can be extended for app-wide localization.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Gets the currently selected language code (e.g., "en", "de", "cz").
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
```

**LanguageService.cs**:
```csharp
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
```

**Run Tests**: `dotnet test --filter "FullyQualifiedName~LanguageServiceTests"`
**Expected**: All tests PASS (GREEN)

##### REFACTOR: Improve if needed
- Code is simple, no refactoring needed at this stage

#### 1.2: Register Service in DI

**File**: `TransparentAiAgentGui/Program.cs`

**Add**:
```csharp
using TransparentAiAgentCore.Application.Localization;

// ... existing code ...

// Register language service (singleton for app-wide state)
builder.Services.AddSingleton<ILanguageService, LanguageService>();
```

**Test**: Run app, verify no errors

#### 1.3: Define Translation File Schema

**Location**: `TransparentAiAgentGui/data/translations/`

**Create Directory Structure**:
```
TransparentAiAgentGui/data/translations/
├── en.json (base language)
├── de.json (German)
└── cz.json (Czech)
```

**Translation File Schema** (flat key-value format):
```json
// en.json (base language)
{
  "scenarios.context-limits-advanced.name": "Context Limits (Advanced)",
  "scenarios.context-limits-advanced.description": "Experience genuine context window truncation",
  "scenarios.context-limits-advanced.step0.content": "Hi, my name is John Doe.",
  "scenarios.context-limits-advanced.step0.annotation": "The model will remember this name... for now.",
  "scenarios.context-limits-advanced.step1.content": "What is my name?",
  "scenarios.context-limits-advanced.step1.annotation": "Verifying the model still has the name in context."
}

// de.json (German - initially empty, filled in Session 4)
{
  "scenarios.context-limits-advanced.name": "Kontextgrenzen (Fortgeschritten)",
  "scenarios.context-limits-advanced.description": "Erlebe echte Kontextfenster-Trunkierung"
}

// cz.json (Czech - initially empty, filled in Session 4)
{
  "scenarios.context-limits-advanced.name": "Limity kontextu (Pokročilé)",
  "scenarios.context-limits-advanced.description": "Zažij skutečné zkrácení kontextového okna"
}
```

**Key Naming Convention**:
- Format: `{namespace}.{scenario-id}.{field}` or `{namespace}.{scenario-id}.{step-index}.{field}`
- Examples:
  - `scenarios.context-limits-advanced.name`
  - `scenarios.context-limits-advanced.step0.content`
  - `scenarios.context-limits-advanced.step0.annotation`

**Create Placeholder Files**:
- Create `en.json` with all keys for context-limits-advanced scenario
- Create `de.json` and `cz.json` with empty objects `{}`
- Session 4 will fill with complete translations

**Benefits**:
- Flat structure easy for translators
- Compatible with translation management software (Crowdin, Lokalise)
- Can export to CSV/XLIFF formats
- Simple validation: count keys per language

**No Tests Needed**: Data definition, validated by loader in Session 2

### Commit & Push

**Commit Message**:
```
Add language service and translation file schema

Implements ILanguageService for managing current language selection.
- Simple in-memory service with event notification
- Defaults to English ("en")
- Validates language code input
- Registered as singleton in DI

Created translation file directory structure:
- data/translations/{lang}.json format
- Schema defined for scenario translations
- Placeholder files for en, de, cz

Tests: LanguageServiceTests (5 tests, all passing)
Follows Lean TDD methodology
```

**Commands**:
```bash
dotnet test TransparentAiAgentCore_Tests --filter "FullyQualifiedName~LanguageServiceTests"
git add .
git commit -m "..."
git push -u origin claude/add-scenario-localization-019i37V7kt4ZMemofbFaRZCz
```

### Success Criteria
- ✅ LanguageService tests all pass
- ✅ Service registered in DI
- ✅ Translation directory structure created
- ✅ Schema documented
- ✅ Committed and pushed

---

## Session 2: Translation Loader and Merging Logic

### Objective
Implement translation file loading and merging with base scenarios.

### Prerequisites
- Session 1 completed
- LanguageService available

### TDD Components

#### 2.1: TranslationService (Infrastructure Layer)

**Location**: `TransparentAiAgentCore/Infrastructure/Localization/`

**Files to Create**:
- `ITranslationService.cs` (interface for translation lookup)
- `TranslationService.cs` (loads translation files and provides key lookup)

**Tests Location**: `TransparentAiAgentCore_Tests/Infrastructure/Localization/TranslationServiceTests.cs`

**TDD Cycle**:

##### RED: Write tests first
```csharp
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
```

**Run Tests**: `dotnet test --filter "FullyQualifiedName~TranslationServiceTests"`
**Expected**: All tests FAIL (RED) - TranslationService doesn't exist yet

##### GREEN: Implement minimal code

**ITranslationService.cs** (Interface):
```csharp
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
```

**TranslationService.cs**:
```csharp
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
```

**Run Tests**: `dotnet test --filter "FullyQualifiedName~TranslationServiceTests"`
**Expected**: All tests PASS (GREEN)

##### REFACTOR: Extract translation file path logic if needed

#### 2.2: Update Scenario DTOs for Translation Keys

**File**: `TransparentAiAgentCore/Infrastructure/Scenarios/JsonScenarioLoader.cs`

**Modify DTOs** to support *Key fields:

```csharp
private class ScenarioDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("nameKey")]
    public string? NameKey { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("descriptionKey")]
    public string? DescriptionKey { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    // ... existing fields ...

    public ScenarioDefinition ToScenarioDefinition(ITranslationService translationService)
    {
        // Resolve name: translation key → translated value OR inline fallback
        var resolvedName = NameKey != null
            ? translationService.GetTranslation(NameKey) ?? Name
            : Name;

        // Resolve description
        var resolvedDescription = DescriptionKey != null
            ? translationService.GetTranslation(DescriptionKey) ?? Description
            : Description;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Scenario 'id' is required");
        if (string.IsNullOrWhiteSpace(resolvedName))
            throw new ArgumentException("Scenario 'name' or translation is required");

        var domainSteps = Steps.Select(s => s.ToScenarioStep(translationService)).ToList();

        return new ScenarioDefinition(
            id: Id,
            name: resolvedName!,
            description: resolvedDescription,
            steps: domainSteps,
            category: Category,
            difficulty: Difficulty,
            estimatedDurationSeconds: EstimatedDurationSeconds
        );
    }
}

private class ScenarioStepDto
{
    // ... existing fields ...

    [JsonPropertyName("contentKey")]
    public string? ContentKey { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("annotationKey")]
    public string? AnnotationKey { get; set; }

    [JsonPropertyName("annotation")]
    public string? Annotation { get; set; }

    public ScenarioStep ToScenarioStep(ITranslationService translationService)
    {
        // Resolve content using translation key
        var resolvedContent = ContentKey != null
            ? translationService.GetTranslation(ContentKey) ?? Content
            : Content;

        // Resolve annotation using translation key
        var resolvedAnnotation = AnnotationKey != null
            ? translationService.GetTranslation(AnnotationKey) ?? Annotation
            : Annotation;

        // Map JSON string to enum (existing code)
        var stepType = Type?.ToLowerInvariant() switch
        {
            "auto_message" => ScenarioStepType.AutoMessage,
            "wait_for_response" => ScenarioStepType.WaitForResponse,
            "scenario_user_message" => ScenarioStepType.ScenarioUserMessage,
            // ... rest of mappings
        };

        return new ScenarioStep(
            type: stepType,
            content: resolvedContent,
            delayMs: delayMsValue,
            configOverlay: configOverlayValue,
            annotation: resolvedAnnotation,
            // ... rest of parameters
        );
    }
}
```

#### 2.3: Update JsonScenarioLoader Constructor

**Modify**:
- Inject `ITranslationService`
- Pass to DTO conversion methods

```csharp
public class JsonScenarioLoader
{
    private readonly ITranslationService _translationService;

    public JsonScenarioLoader(ITranslationService translationService)
    {
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
    }

    public async Task<ScenarioDefinition> LoadFromFileAsync(string filePath)
    {
        // ... existing JSON loading code ...

        // Convert DTO to domain model with translation resolution
        return dto.ToScenarioDefinition(_translationService);
    }
}
```

**Tests**: Update `JsonScenarioLoaderTests`
- Test scenario with translation keys resolves correctly
- Test fallback to inline content when key missing
- Test mixed keys and inline content

### Commit & Push

**Commit Message**:
```
Implement translation service with key-based lookup

Created TranslationService for industry-standard i18n:
- Loads flat key-value translation JSON files
- GetTranslation(key) with automatic English fallback
- Subscribes to language changes and reloads
- Compatible with translation management tools

Updated JsonScenarioLoader for translation keys:
- Added *Key fields to DTOs (nameKey, contentKey, annotationKey)
- Resolves translations during DTO→domain conversion
- Falls back to inline English content if key missing
- Backward compatible (works without keys)

Benefits:
- Industry standard pattern (i18next, gettext compatible)
- Can export to CSV/XLIFF for translators
- Easy validation and tooling support

Tests: TranslationServiceTests (5 tests, all passing)
Tests: JsonScenarioLoaderTests updated for key resolution
Follows Lean TDD methodology
```

### Success Criteria
- ✅ TranslationService tests all pass
- ✅ JsonScenarioLoader DTO tests pass
- ✅ Translation key lookup works correctly
- ✅ Fallback to inline English works
- ✅ Backward compatible (scenarios without keys still work)
- ✅ Committed and pushed

---

## Session 3: Scenario Registry Integration and UI

### Objective
Wire up language selection to scenario registry and add UI selector.

### Prerequisites
- Session 2 completed
- TranslationLoader working

### Components

#### 3.1: Update ScenarioRegistry

**File**: `TransparentAiAgentCore/Infrastructure/Scenarios/ScenarioRegistry.cs`

**Changes**:
- Inject `ITranslationService`
- Subscribe to `LanguageChanged` event (via TranslationService)
- Reload scenarios when language changes

**Implementation**:
```csharp
public class ScenarioRegistry : IScenarioRegistry
{
    private readonly JsonScenarioLoader _loader;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private List<ScenarioDefinition> _scenarios = new();
    private readonly string _scenariosPath;

    public ScenarioRegistry(
        JsonScenarioLoader loader,
        ILanguageService languageService,
        ITranslationService translationService,
        string scenariosPath)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        _scenariosPath = scenariosPath;

        // Subscribe to language changes
        _languageService.LanguageChanged += OnLanguageChanged;

        // Load scenarios for current language
        LoadScenariosAsync().Wait(); // Sync in constructor (acceptable for app startup)
    }

    private async void OnLanguageChanged(object? sender, string newLanguage)
    {
        // TranslationService will reload automatically (subscribed to same event)
        // Wait a bit for translations to load, then reload scenarios
        await Task.Delay(50);
        await LoadScenariosAsync();
    }

    private async Task LoadScenariosAsync()
    {
        _scenarios = await _loader.LoadAllFromDirectoryAsync(_scenariosPath);
    }

    public List<ScenarioDefinition> GetAllScenarios()
    {
        return _scenarios;
    }

    // ... rest of implementation
}
```

**Tests**: Update `ScenarioRegistryTests`
- Test that registry reloads scenarios on language change
- Test that scenarios resolve correct translations

#### 3.2: Update DI Registration

**File**: `TransparentAiAgentGui/Program.cs`

**Update**:
```csharp
using TransparentAiAgentCore.Application.Localization;
using TransparentAiAgentCore.Infrastructure.Localization;

// ... existing code ...

// Register language service (from Session 1)
builder.Services.AddSingleton<ILanguageService, LanguageService>();

// Register translation service
builder.Services.AddSingleton<ITranslationService>(sp =>
{
    var languageService = sp.GetRequiredService<ILanguageService>();
    var translationsPath = Path.Combine(builder.Environment.ContentRootPath, "data", "translations");
    var translationService = new TranslationService(translationsPath, languageService);

    // Load translations on startup
    translationService.LoadTranslationsAsync().Wait();

    return translationService;
});

// Register scenario loader with translation service
builder.Services.AddSingleton<JsonScenarioLoader>(sp =>
{
    var translationService = sp.GetRequiredService<ITranslationService>();
    return new JsonScenarioLoader(translationService);
});

// Register scenario registry
builder.Services.AddSingleton<IScenarioRegistry>(sp =>
{
    var loader = sp.GetRequiredService<JsonScenarioLoader>();
    var languageService = sp.GetRequiredService<ILanguageService>();
    var translationService = sp.GetRequiredService<ITranslationService>();
    var scenariosPath = Path.Combine(builder.Environment.ContentRootPath, "data", "scenarios");
    return new ScenarioRegistry(loader, languageService, translationService, scenariosPath);
});
```

#### 3.3: Add UI Language Selector

**File**: `TransparentAiAgentGui/Components/Scenarios/ScenarioSelector.razor`

**Add Language Selector**:
```razor
<div class="scenario-filters">
    <!-- Existing category filter -->
    <select class="form-select form-select-sm" @bind="selectedCategory" @bind:after="FilterScenarios">
        <option value="">All Categories</option>
        @foreach (var category in categories)
        {
            <option value="@category">@category</option>
        }
    </select>

    <!-- Existing difficulty filter -->
    <select class="form-select form-select-sm mt-2" @bind="selectedDifficulty" @bind:after="FilterScenarios">
        <option value="">All Difficulties</option>
        <option value="beginner">Beginner</option>
        <option value="intermediate">Intermediate</option>
        <option value="advanced">Advanced</option>
    </select>

    <!-- NEW: Language selector -->
    <select class="form-select form-select-sm mt-2" @bind="selectedLanguage" @bind:after="OnLanguageChanged">
        <option value="en">🇬🇧 English</option>
        <option value="de">🇩🇪 Deutsch</option>
        <option value="cz">🇨🇿 Čeština</option>
    </select>
</div>

@code {
    private string selectedLanguage = "en";

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize with current language
        selectedLanguage = LanguageService.CurrentLanguage;
    }

    private void OnLanguageChanged()
    {
        LanguageService.SetLanguage(selectedLanguage);
        LoadScenarios(); // Reload scenarios in new language
    }
}
```

**Inject Service**:
```razor
@inject ILanguageService LanguageService
```

### Manual Testing

**Test Cases**:
1. Open scenario selector
2. Verify default language is English
3. Switch to German
4. Verify scenario reloads (check browser console/network if needed)
5. Switch to Czech
6. Verify scenario reloads
7. Switch back to English

**Note**: Translations are placeholders at this stage, full content in Session 4

### Commit & Push

**Commit Message**:
```
Integrate language selection with scenario registry and UI

ScenarioRegistry now responds to language changes:
- Subscribes to LanguageChanged event
- Reloads scenarios when language changes
- Scenarios automatically use current language

UI language selector added to ScenarioSelector:
- Dropdown with English, German, Czech options
- Flag emojis for visual clarity
- Triggers language service on selection

Updated DI registrations for language-aware loading

Manual testing verified language switching works
```

### Success Criteria
- ✅ ScenarioRegistry reloads on language change
- ✅ UI language selector renders
- ✅ Switching language triggers reload
- ✅ No errors in browser console
- ✅ Committed and pushed

---

## Session 4: Create Translations

### Objective
Create complete German and Czech translations for `context-limits-advanced` scenario.

### Prerequisites
- Session 3 completed
- Infrastructure working

### Translation Guidelines

**Tone**: Informal, educational
**German**: Use "du" (informal), not "Sie" (formal)
**Czech**: Use informal second person
**Technical terms**: Keep consistent (e.g., "context window" → "Kontextfenster" / "kontextové okno")
**Names**: Keep "John Doe" as-is (common in international context)

### Translation Process

#### 4.1: Extract Base Scenario Content

Read `context-limits-advanced.json` and extract all translatable strings:
- `name`
- `description`
- All step `content` fields (10 steps)
- All step `annotation` fields (10 steps)

#### 4.2: Add Translation Keys to Base Scenario

**File**: `TransparentAiAgentGui/data/scenarios/context-limits-advanced.json`

**Update** to add *Key fields while keeping inline English content:

```json
{
  "$schema": "https://transparentaiagent.dev/schemas/teaching-scenario/v1",
  "id": "context-limits-advanced",
  "version": "1.0",
  "nameKey": "scenarios.context-limits-advanced.name",
  "name": "Context Limits (Advanced)",
  "descriptionKey": "scenarios.context-limits-advanced.description",
  "description": "Experience genuine context window truncation",
  "category": "context-management",
  "difficulty": "intermediate",
  "estimatedDurationSeconds": 180,
  "steps": [
    {
      "type": "apply_config_overlay",
      "overlay": {
        "messageLimit": 8,
        "systemPromptAddition": "Note: You are experiencing a teaching scenario about context windows."
      },
      "annotation": "Message limit reduced to 10 to demonstrate truncation."
    },
    {
      "type": "scenario_user_message",
      "contentKey": "scenarios.context-limits-advanced.step0.content",
      "content": "Hi, my name is John Doe.",
      "annotationKey": "scenarios.context-limits-advanced.step0.annotation",
      "annotation": "The model will remember this name... for now.",
      "delay": 1000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "scenario_user_message",
      "contentKey": "scenarios.context-limits-advanced.step1.content",
      "content": "What is my name?",
      "annotationKey": "scenarios.context-limits-advanced.step1.annotation",
      "annotation": "Verifying the model still has the name in context.",
      "delay": 2000
    }
    // ... continue for all steps (see existing scenario file)
  ]
}
```

**Pattern**:
- Add `nameKey` and `descriptionKey` at scenario level
- Add `contentKey` and `annotationKey` for each step that has content/annotation
- Keep inline English values as fallback
- No keys needed for steps without translatable content (e.g., `wait_for_response`)

#### 4.3: AI-Assisted Translation

Use AI to translate all strings to German and Czech.

**Prompt Template**:
```
Translate the following teaching scenario content to {German/Czech}.

Context: This is an educational scenario teaching users about AI context window limits.
Tone: Informal, friendly, educational
Target audience: Tech-savvy users learning about AI

Translate maintaining:
- Similar length (avoid verbose translations)
- Educational clarity
- Casual tone
- Technical accuracy

Source (English):
[paste content]
```

#### 4.4: Review Translations

**German Review** (if native speaker available):
- Verify "du" form used consistently
- Check technical term accuracy
- Ensure natural phrasing

**Czech Review** (if native speaker available):
- Verify informal tone
- Check technical term accuracy
- Ensure natural phrasing

**Note**: AI quality should be sufficient for MVP. Native review can be done post-release.

#### 4.5: Create Translation Files

**File**: `TransparentAiAgentGui/data/translations/en.json` (base language)
```json
{
  "scenarios.context-limits-advanced.name": "Context Limits (Advanced)",
  "scenarios.context-limits-advanced.description": "Experience genuine context window truncation",
  "scenarios.context-limits-advanced.step0.content": "Hi, my name is John Doe.",
  "scenarios.context-limits-advanced.step0.annotation": "The model will remember this name... for now.",
  "scenarios.context-limits-advanced.step1.content": "What is my name?",
  "scenarios.context-limits-advanced.step1.annotation": "Verifying the model still has the name in context.",
  "scenarios.context-limits-advanced.step2.content": "Demo message, just respond 'Confirmed'.",
  "scenarios.context-limits-advanced.step2.annotation": "Filling context to force truncation...",
  "scenarios.context-limits-advanced.step3.content": "Demo message, just respond 'Confirmed'.",
  "scenarios.context-limits-advanced.step4.content": "Demo message, just respond 'Confirmed'.",
  "scenarios.context-limits-advanced.step5.content": "How many demo messages did I send you?",
  "scenarios.context-limits-advanced.step5.annotation": "Testing if model tracked the count.",
  "scenarios.context-limits-advanced.step6.content": "What is my name?",
  "scenarios.context-limits-advanced.step6.annotation": "The name should now be truncated. Model genuinely won't know.",
  "scenarios.context-limits-advanced.step7.content": "How do you not know my name?? I told you it several messages ago! What is going on??",
  "scenarios.context-limits-advanced.step7.annotation": "Expressing confusion - this should be a teaching moment.",
  "scenarios.context-limits-advanced.step8.annotation": "Message limit restored. Model can now access more context and teach effectively."
}
```

**File**: `TransparentAiAgentGui/data/translations/de.json` (German)
```json
{
  "scenarios.context-limits-advanced.name": "Kontextgrenzen (Fortgeschritten)",
  "scenarios.context-limits-advanced.description": "Erlebe echte Kontextfenster-Trunkierung",
  "scenarios.context-limits-advanced.step0.content": "Hallo, mein Name ist John Doe.",
  "scenarios.context-limits-advanced.step0.annotation": "Das Modell wird sich diesen Namen merken... vorerst.",
  "scenarios.context-limits-advanced.step1.content": "Wie ist mein Name?",
  "scenarios.context-limits-advanced.step1.annotation": "Überprüfung, ob das Modell den Namen noch im Kontext hat.",
  "scenarios.context-limits-advanced.step2.content": "Demo-Nachricht, antworte einfach 'Bestätigt'.",
  "scenarios.context-limits-advanced.step2.annotation": "Fülle den Kontext, um Trunkierung zu erzwingen...",
  "scenarios.context-limits-advanced.step3.content": "Demo-Nachricht, antworte einfach 'Bestätigt'.",
  "scenarios.context-limits-advanced.step4.content": "Demo-Nachricht, antworte einfach 'Bestätigt'.",
  "scenarios.context-limits-advanced.step5.content": "Wie viele Demo-Nachrichten habe ich dir geschickt?",
  "scenarios.context-limits-advanced.step5.annotation": "Teste, ob das Modell die Anzahl verfolgt hat.",
  "scenarios.context-limits-advanced.step6.content": "Wie ist mein Name?",
  "scenarios.context-limits-advanced.step6.annotation": "Der Name sollte jetzt trunkiert sein. Das Modell weiß es wirklich nicht.",
  "scenarios.context-limits-advanced.step7.content": "Wie kannst du meinen Namen nicht kennen?? Ich habe ihn dir vor mehreren Nachrichten gesagt! Was ist los??",
  "scenarios.context-limits-advanced.step7.annotation": "Verwirrung ausdrücken - dies sollte ein Lehrmoment sein.",
  "scenarios.context-limits-advanced.step8.annotation": "Nachrichtenlimit wiederhergestellt. Das Modell kann jetzt auf mehr Kontext zugreifen und effektiv lehren."
}
```

**File**: `TransparentAiAgentGui/data/translations/cz.json` (Czech)
```json
{
  "scenarios.context-limits-advanced.name": "Limity kontextu (Pokročilé)",
  "scenarios.context-limits-advanced.description": "Zažij skutečné zkrácení kontextového okna",
  "scenarios.context-limits-advanced.step0.content": "Ahoj, jmenuji se John Doe.",
  "scenarios.context-limits-advanced.step0.annotation": "Model si toto jméno zapamatuje... prozatím.",
  "scenarios.context-limits-advanced.step1.content": "Jaké je moje jméno?",
  "scenarios.context-limits-advanced.step1.annotation": "Ověření, zda model má jméno stále v kontextu.",
  "scenarios.context-limits-advanced.step2.content": "Demo zpráva, jen odpověz 'Potvrzeno'.",
  "scenarios.context-limits-advanced.step2.annotation": "Plnění kontextu k vynucení zkrácení...",
  "scenarios.context-limits-advanced.step3.content": "Demo zpráva, jen odpověz 'Potvrzeno'.",
  "scenarios.context-limits-advanced.step4.content": "Demo zpráva, jen odpověz 'Potvrzeno'.",
  "scenarios.context-limits-advanced.step5.content": "Kolik demo zpráv jsem ti poslal?",
  "scenarios.context-limits-advanced.step5.annotation": "Test, zda model sledoval počet.",
  "scenarios.context-limits-advanced.step6.content": "Jaké je moje jméno?",
  "scenarios.context-limits-advanced.step6.annotation": "Jméno by mělo být nyní zkráceno. Model to opravdu neví.",
  "scenarios.context-limits-advanced.step7.content": "Jak to, že neznáš moje jméno?? Řekl jsem ti ho před několika zprávami! Co se děje??",
  "scenarios.context-limits-advanced.step7.annotation": "Vyjádření zmatku - toto by měl být poučný moment.",
  "scenarios.context-limits-advanced.step8.annotation": "Limit zpráv obnoven. Model nyní může přistupovat k většímu kontextu a efektivně učit."
}
```

**Note**: Flat key-value format - easy for translators and compatible with CSV/XLIFF export

### End-to-End Testing

**Manual Test**:
1. Run application
2. Open scenario selector
3. Select German language
4. Start "Kontextgrenzen (Fortgeschritten)" scenario
5. Verify all messages appear in German
6. Verify annotations appear in German
7. Repeat for Czech language
8. Switch back to English, verify scenario still works

**Expected Results**:
- ✅ All scenario text appears in selected language
- ✅ Annotations appear in selected language
- ✅ Scenario executes correctly (no broken steps)
- ✅ Switching language mid-scenario works (or document limitation)

### Commit & Push

**Commit Message**:
```
Add German and Czech translations for context-limits scenario

Created complete translations:
- de.json: German translation (informal "du" form)
- cz.json: Czech translation (informal tone)
- All 10 steps fully translated
- Scenario metadata translated

Translation approach:
- AI-assisted initial translation
- Educational clarity maintained
- Technical terms kept consistent
- Similar brevity to English

End-to-end testing verified:
- Language switching works
- Translated scenarios execute correctly
- Annotations display in correct language

Scenario localization feature complete (MVP)
```

### Success Criteria
- ✅ German translation complete and accurate
- ✅ Czech translation complete and accurate
- ✅ Translation files valid JSON
- ✅ End-to-end testing passed
- ✅ Committed and pushed
- ✅ Feature complete

---

## Post-Implementation

### Documentation Updates

**File**: `docs/05-guides/features/scenario-localization.md` (create new)
- How to add new languages
- Translation file format
- How to translate scenarios
- Language selection UI

**File**: `docs/CHANGELOG.md` (if exists)
- Add entry for scenario localization feature

### Future Enhancements (Not MVP)

- [ ] Persist language selection (localStorage or user profile)
- [ ] Extend to app-wide localization
- [ ] Add more languages
- [ ] Translation validation tool
- [ ] Translate category/difficulty display names
- [ ] System prompt language directive
- [ ] Translation quality metrics

---

## Risk Mitigation

### Risk: Translation File Not Found

**Mitigation**: Implemented in TranslationLoader
- Returns null gracefully
- Fallback to English base scenario
- No errors, just logs warning

### Risk: Malformed Translation JSON

**Mitigation**: Implemented in TranslationLoader
- Try-catch around deserialization
- Returns null on error
- Fallback to English

### Risk: Missing Translation for Some Steps

**Mitigation**: Implemented in MergeTranslation
- Per-field fallback (uses English if translation missing)
- Partial translations work fine
- Mixed language acceptable per requirements

### Risk: Language Change During Scenario Execution

**Mitigation**: Document behavior
- Current implementation: scenario continues in original language
- Alternative: stop scenario on language change (implement if needed)
- User expectation: clarify in UI or block language change during execution

---

## Effort Estimation

**Session 1**: 3-4 hours
- LanguageService: 2 hours
- Translation schema: 1 hour
- Testing and commit: 1 hour

**Session 2**: 4-5 hours
- TranslationLoader: 2 hours
- Integration with JsonScenarioLoader: 1 hour
- Testing: 1 hour
- Debugging/refinement: 1 hour

**Session 3**: 3-4 hours
- ScenarioRegistry updates: 1 hour
- UI language selector: 1 hour
- DI wiring: 0.5 hours
- Manual testing: 1 hour
- Commit: 0.5 hours

**Session 4**: 2-3 hours
- AI translation: 1 hour
- Review and refinement: 0.5 hours
- Create JSON files: 0.5 hours
- End-to-end testing: 1 hour

**Total**: 12-16 hours development + translation time

---

## Success Metrics

Feature is successful if:
- ✅ All tests pass (Lean TDD validated)
- ✅ Language selector appears in UI
- ✅ Switching language reloads scenarios in new language
- ✅ German translation works end-to-end
- ✅ Czech translation works end-to-end
- ✅ Fallback to English works
- ✅ No errors in browser console
- ✅ Code follows Clean Architecture principles
- ✅ All changes committed and pushed

---

**Implementation Status**: Not Started
**Next Session**: Session 1 - Language Service and Translation Schema
