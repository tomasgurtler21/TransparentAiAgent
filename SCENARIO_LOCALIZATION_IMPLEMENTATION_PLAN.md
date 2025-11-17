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
- Base scenario JSON contains structure + English content (backward compatible)
- Translation files (de.json, cz.json) contain only translated strings
- Loader merges translation overlay based on selected language
- Always fallback to English if translation missing

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
├── en.json (optional, for completeness)
├── de.json
└── cz.json
```

**Translation File Schema** (`de.json` example):
```json
{
  "$schema": "https://transparentaiagent.dev/schemas/scenario-translations/v1",
  "language": "de",
  "scenarios": {
    "context-limits-advanced": {
      "name": "Kontextgrenzen (Fortgeschritten)",
      "description": "Erleben Sie echtes Kontextfenster-Trunkieren",
      "steps": {
        "0": {
          "content": "Hallo, mein Name ist John Doe.",
          "annotation": "Das Modell wird sich diesen Namen merken... vorerst."
        },
        "1": {
          "content": "Wie ist mein Name?",
          "annotation": "Überprüfung, ob das Modell den Namen noch im Kontext hat."
        }
        // ... continue for all 10 steps
      }
    }
  }
}
```

**Create Placeholder Files** (Session 1 creates structure, Session 4 fills content):
- Create `en.json`, `de.json`, `cz.json` with empty scenario objects
- Document schema structure

**No Tests Needed**: This is data definition, validated by loader in Session 2

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

#### 2.1: TranslationLoader (Infrastructure Layer)

**Location**: `TransparentAiAgentCore/Infrastructure/Localization/`

**Files to Create**:
- `ScenarioTranslation.cs` (DTO for translation JSON)
- `TranslationLoader.cs` (loads and merges translations)

**Tests Location**: `TransparentAiAgentCore_Tests/Infrastructure/Localization/TranslationLoaderTests.cs`

**TDD Cycle**:

##### RED: Write tests first
```csharp
[TestClass]
public class TranslationLoaderTests
{
    private const string TestTranslationsPath = "./TestData/translations";

    [TestInitialize]
    public void Setup()
    {
        // Create test translation files
        Directory.CreateDirectory(TestTranslationsPath);
        CreateTestTranslationFile("de", "German Name", "German Description");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(TestTranslationsPath))
            Directory.Delete(TestTranslationsPath, true);
    }

    [TestMethod]
    public async Task LoadTranslation_ExistingFile_ReturnsTranslation()
    {
        // Arrange
        var loader = new TranslationLoader(TestTranslationsPath);

        // Act
        var translation = await loader.LoadTranslationAsync("de", "test-scenario");

        // Assert
        Assert.IsNotNull(translation);
        Assert.AreEqual("German Name", translation.Name);
    }

    [TestMethod]
    public async Task LoadTranslation_MissingFile_ReturnsNull()
    {
        // Arrange
        var loader = new TranslationLoader(TestTranslationsPath);

        // Act
        var translation = await loader.LoadTranslationAsync("fr", "test-scenario");

        // Assert
        Assert.IsNull(translation); // Fallback to null, caller uses English
    }

    [TestMethod]
    public async Task MergeTranslation_ValidTranslation_OverridesBaseFields()
    {
        // Arrange
        var loader = new TranslationLoader(TestTranslationsPath);
        var baseScenario = CreateTestScenario("English Name", "English Desc");
        var translation = await loader.LoadTranslationAsync("de", "test-scenario");

        // Act
        var merged = loader.MergeTranslation(baseScenario, translation);

        // Assert
        Assert.AreEqual("German Name", merged.Name);
        Assert.AreEqual("German Description", merged.Description);
    }

    [TestMethod]
    public void MergeTranslation_NullTranslation_ReturnsOriginal()
    {
        // Arrange
        var loader = new TranslationLoader(TestTranslationsPath);
        var baseScenario = CreateTestScenario("English Name", "English Desc");

        // Act
        var merged = loader.MergeTranslation(baseScenario, null);

        // Assert
        Assert.AreEqual("English Name", merged.Name); // Unchanged
    }

    // Helper methods
    private void CreateTestTranslationFile(string lang, string name, string desc)
    {
        var json = $$"""
        {
            "language": "{{lang}}",
            "scenarios": {
                "test-scenario": {
                    "name": "{{name}}",
                    "description": "{{desc}}"
                }
            }
        }
        """;
        File.WriteAllText(Path.Combine(TestTranslationsPath, $"{lang}.json"), json);
    }

    private ScenarioDefinition CreateTestScenario(string name, string desc)
    {
        return new ScenarioDefinition(
            id: "test-scenario",
            name: name,
            description: desc,
            steps: new[] { new ScenarioStep(ScenarioStepType.WaitForResponse) }
        );
    }
}
```

**Run Tests**: `dotnet test --filter "FullyQualifiedName~TranslationLoaderTests"`
**Expected**: All tests FAIL (RED) - TranslationLoader doesn't exist yet

##### GREEN: Implement minimal code

**ScenarioTranslation.cs** (DTO):
```csharp
using System.Text.Json.Serialization;

namespace TransparentAiAgentCore.Infrastructure.Localization;

/// <summary>
/// DTO for translation JSON files.
/// Represents translated strings for a single scenario.
/// </summary>
public class ScenarioTranslation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("steps")]
    public Dictionary<string, StepTranslation>? Steps { get; set; }
}

public class StepTranslation
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("annotation")]
    public string? Annotation { get; set; }
}

/// <summary>
/// Root DTO for translation file.
/// </summary>
public class TranslationFile
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("scenarios")]
    public Dictionary<string, ScenarioTranslation>? Scenarios { get; set; }
}
```

**TranslationLoader.cs**:
```csharp
using System.Text.Json;
using TransparentAiAgentCore.Domain.Scenarios;

namespace TransparentAiAgentCore.Infrastructure.Localization;

/// <summary>
/// Loads translation files and merges them with base scenarios.
/// </summary>
public class TranslationLoader
{
    private readonly string _translationsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public TranslationLoader(string translationsPath)
    {
        _translationsPath = translationsPath ?? throw new ArgumentNullException(nameof(translationsPath));
    }

    /// <summary>
    /// Loads translation for a specific language and scenario.
    /// Returns null if translation file doesn't exist or scenario not found.
    /// </summary>
    public async Task<ScenarioTranslation?> LoadTranslationAsync(string languageCode, string scenarioId)
    {
        var filePath = Path.Combine(_translationsPath, $"{languageCode}.json");

        if (!File.Exists(filePath))
            return null; // Fallback to base language

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var translationFile = JsonSerializer.Deserialize<TranslationFile>(json, JsonOptions);

            if (translationFile?.Scenarios == null)
                return null;

            translationFile.Scenarios.TryGetValue(scenarioId, out var translation);
            return translation;
        }
        catch (Exception)
        {
            // Log error but don't throw - graceful degradation
            return null;
        }
    }

    /// <summary>
    /// Merges translation onto base scenario definition.
    /// Returns new ScenarioDefinition with translated strings.
    /// </summary>
    public ScenarioDefinition MergeTranslation(
        ScenarioDefinition baseScenario,
        ScenarioTranslation? translation)
    {
        if (translation == null)
            return baseScenario; // No translation, return original

        // Merge scenario-level fields
        var name = translation.Name ?? baseScenario.Name;
        var description = translation.Description ?? baseScenario.Description;

        // Merge step-level fields
        var translatedSteps = new List<ScenarioStep>();
        for (int i = 0; i < baseScenario.Steps.Count; i++)
        {
            var baseStep = baseScenario.Steps[i];
            var stepTranslation = translation.Steps?.GetValueOrDefault(i.ToString());

            if (stepTranslation != null)
            {
                // Create new step with translated content
                var translatedStep = new ScenarioStep(
                    type: baseStep.Type,
                    content: stepTranslation.Content ?? baseStep.Content,
                    delayMs: baseStep.DelayMs,
                    configOverlay: baseStep.ConfigOverlay,
                    annotation: stepTranslation.Annotation ?? baseStep.Annotation,
                    visibleTo: baseStep.VisibleTo,
                    condition: baseStep.Condition,
                    conditionParameters: baseStep.ConditionParameters,
                    onTimeout: baseStep.OnTimeout,
                    uiControlTool: baseStep.UIControlTool,
                    uiControlArguments: baseStep.UIControlArguments
                );
                translatedSteps.Add(translatedStep);
            }
            else
            {
                translatedSteps.Add(baseStep); // No translation for this step
            }
        }

        // Return new scenario with merged translations
        return new ScenarioDefinition(
            id: baseScenario.Id,
            name: name,
            description: description,
            steps: translatedSteps,
            category: baseScenario.Category,
            difficulty: baseScenario.Difficulty,
            estimatedDurationSeconds: baseScenario.EstimatedDurationSeconds
        );
    }
}
```

**Run Tests**: `dotnet test --filter "FullyQualifiedName~TranslationLoaderTests"`
**Expected**: All tests PASS (GREEN)

##### REFACTOR: Extract translation file path logic if needed

#### 2.2: Integrate with JsonScenarioLoader

**File**: `TransparentAiAgentCore/Infrastructure/Scenarios/JsonScenarioLoader.cs`

**Modify**:
- Add constructor parameter for `ILanguageService` and `TranslationLoader`
- After loading base scenario, load translation and merge

**Changes**:
```csharp
public class JsonScenarioLoader
{
    private readonly ILanguageService _languageService;
    private readonly TranslationLoader _translationLoader;

    public JsonScenarioLoader(
        ILanguageService languageService,
        string translationsPath)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _translationLoader = new TranslationLoader(translationsPath);
    }

    public async Task<ScenarioDefinition> LoadFromFileAsync(string filePath)
    {
        // ... existing code to load base scenario ...

        var baseScenario = dto.ToScenarioDefinition();

        // Load and merge translation if not English
        if (_languageService.CurrentLanguage != "en")
        {
            var translation = await _translationLoader.LoadTranslationAsync(
                _languageService.CurrentLanguage,
                baseScenario.Id);

            if (translation != null)
            {
                return _translationLoader.MergeTranslation(baseScenario, translation);
            }
        }

        return baseScenario;
    }
}
```

**Tests**: Update `JsonScenarioLoaderTests` to verify language integration
- Test loading with English (no translation)
- Test loading with German (translation applied)
- Test loading with missing translation (fallback to English)

### Commit & Push

**Commit Message**:
```
Implement translation loader and merging logic

Created TranslationLoader to load and merge scenario translations:
- Loads translation JSON files by language code
- Merges translations onto base scenario (overlay pattern)
- Graceful fallback if translation missing
- Returns null for missing files/scenarios

Integrated with JsonScenarioLoader:
- Uses ILanguageService to determine current language
- Automatically applies translations when loading scenarios
- English scenarios loaded directly (no translation file needed)

Tests: TranslationLoaderTests (4 tests, all passing)
Tests: JsonScenarioLoaderTests updated for language integration
Follows Lean TDD methodology
```

### Success Criteria
- ✅ TranslationLoader tests all pass
- ✅ JsonScenarioLoader integration tests pass
- ✅ Translation merging works correctly
- ✅ Fallback to English works
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
- Inject `ILanguageService`
- Subscribe to `LanguageChanged` event
- Reload scenarios when language changes

**Implementation**:
```csharp
public class ScenarioRegistry : IScenarioRegistry
{
    private readonly JsonScenarioLoader _loader;
    private readonly ILanguageService _languageService;
    private List<ScenarioDefinition> _scenarios = new();
    private readonly string _scenariosPath;

    public ScenarioRegistry(
        JsonScenarioLoader loader,
        ILanguageService languageService,
        string scenariosPath)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _scenariosPath = scenariosPath;

        // Subscribe to language changes
        _languageService.LanguageChanged += OnLanguageChanged;

        // Load scenarios for current language
        LoadScenariosAsync().Wait(); // Sync in constructor (acceptable for app startup)
    }

    private async void OnLanguageChanged(object? sender, string newLanguage)
    {
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
- Test that scenarios are in current language

#### 3.2: Update DI Registration

**File**: `TransparentAiAgentGui/Program.cs`

**Update**:
```csharp
// Register scenario loader with language service
builder.Services.AddSingleton<JsonScenarioLoader>(sp =>
{
    var languageService = sp.GetRequiredService<ILanguageService>();
    var translationsPath = Path.Combine(builder.Environment.ContentRootPath, "data", "translations");
    return new JsonScenarioLoader(languageService, translationsPath);
});

// Register scenario registry
builder.Services.AddSingleton<IScenarioRegistry>(sp =>
{
    var loader = sp.GetRequiredService<JsonScenarioLoader>();
    var languageService = sp.GetRequiredService<ILanguageService>();
    var scenariosPath = Path.Combine(builder.Environment.ContentRootPath, "data", "scenarios");
    return new ScenarioRegistry(loader, languageService, scenariosPath);
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

#### 4.2: AI-Assisted Translation

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

#### 4.3: Review Translations

**German Review** (if native speaker available):
- Verify "du" form used consistently
- Check technical term accuracy
- Ensure natural phrasing

**Czech Review** (if native speaker available):
- Verify informal tone
- Check technical term accuracy
- Ensure natural phrasing

**Note**: AI quality should be sufficient for MVP. Native review can be done post-release.

#### 4.4: Create Translation Files

**File**: `TransparentAiAgentGui/data/translations/de.json`
```json
{
  "$schema": "https://transparentaiagent.dev/schemas/scenario-translations/v1",
  "language": "de",
  "scenarios": {
    "context-limits-advanced": {
      "name": "Kontextgrenzen (Fortgeschritten)",
      "description": "Erlebe echte Kontextfenster-Trunkierung",
      "steps": {
        "0": {
          "content": "Hallo, mein Name ist John Doe.",
          "annotation": "Das Modell wird sich diesen Namen merken... vorerst."
        },
        "1": {
          "content": "Wie ist mein Name?",
          "annotation": "Überprüfung, ob das Modell den Namen noch im Kontext hat."
        }
        // ... complete all 10 steps
      }
    }
  }
}
```

**File**: `TransparentAiAgentGui/data/translations/cz.json`
```json
{
  "$schema": "https://transparentaiagent.dev/schemas/scenario-translations/v1",
  "language": "cz",
  "scenarios": {
    "context-limits-advanced": {
      "name": "Limity kontextu (Pokročilé)",
      "description": "Zažij skutečné zkrácení kontextového okna",
      "steps": {
        "0": {
          "content": "Ahoj, jmenuji se John Doe.",
          "annotation": "Model si toto jméno zapamatuje... prozatím."
        },
        "1": {
          "content": "Jaké je moje jméno?",
          "annotation": "Ověření, zda model má jméno stále v kontextu."
        }
        // ... complete all 10 steps
      }
    }
  }
}
```

**Optional**: Create `en.json` for completeness (copy from base scenario)

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
