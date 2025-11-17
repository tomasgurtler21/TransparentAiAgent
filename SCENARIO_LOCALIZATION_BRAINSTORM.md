# Scenario Localization Brainstorm

**Created**: 2025-11-17
**Status**: Concept Analysis
**Languages Target**: English (en), German (de), Czech (cz)

---

## Current Architecture Analysis

### Scenario Structure (Existing)

The scenario system is well-structured with clear separation of concerns:

**Domain Layer** (`TransparentAiAgentCore/Domain/Scenarios/`):
- `ScenarioDefinition.cs` - Main scenario model with metadata
  - `Id`, `Name`, `Description` (string fields)
  - `Steps` (collection of ScenarioStep)
  - `Category`, `Difficulty`, `EstimatedDurationSeconds`
- `ScenarioStep.cs` - Individual step model
  - `Content` (string field - main text)
  - `Annotation` (string field - UI-only notes)
  - `Type` (enum determining step behavior)

**Infrastructure Layer** (`TransparentAiAgentCore/Infrastructure/Scenarios/`):
- `JsonScenarioLoader.cs` - Deserializes JSON to domain models
  - Uses DTOs with `JsonPropertyName` attributes
  - Validates required fields during deserialization
  - Supports both snake_case and camelCase JSON properties

**Application Layer** (`TransparentAiAgentCore/Application/Scenarios/`):
- `ScenarioExecutor.cs` - Executes scenario steps
  - Reads `Content` and `Annotation` fields for step execution
  - Sends messages to agent using step content

**UI Layer** (`TransparentAiAgentGui/Components/Scenarios/`):
- `ScenarioSelector.razor` - Displays scenario list with filters
  - Shows `Name`, `Description`, `Category`, `Difficulty`
- `ScenarioIndicator.razor` - Shows current scenario execution status

### Current JSON Schema Example

```json
{
  "id": "context-limits-advanced",
  "name": "Context Limits (Advanced)",
  "description": "Experience genuine context window truncation",
  "category": "context-management",
  "difficulty": "intermediate",
  "steps": [
    {
      "type": "scenario_user_message",
      "content": "Hi, my name is John Doe.",
      "annotation": "The model will remember this name... for now."
    }
  ]
}
```

---

## Naive Approach: Suffix-Based Localization

### Proposed JSON Structure

```json
{
  "id": "context-limits-advanced",
  "name_en": "Context Limits (Advanced)",
  "name_de": "Kontextgrenzen (Fortgeschritten)",
  "name_cz": "Limity kontextu (Pokročilé)",
  "description_en": "Experience genuine context window truncation",
  "description_de": "Erleben Sie echtes Kontextfenster-Trunkieren",
  "description_cz": "Zažijte skutečné zkrácení kontextového okna",
  "category": "context-management",
  "difficulty": "intermediate",
  "steps": [
    {
      "type": "scenario_user_message",
      "content_en": "Hi, my name is John Doe.",
      "content_de": "Hallo, mein Name ist John Doe.",
      "content_cz": "Ahoj, jmenuji se John Doe.",
      "annotation_en": "The model will remember this name... for now.",
      "annotation_de": "Das Modell wird sich diesen Namen merken... vorerst.",
      "annotation_cz": "Model si toto jméno zapamatuje... prozatím."
    }
  ]
}
```

### Fields Requiring Localization

**ScenarioDefinition level**:
- `name` → `name_en`, `name_de`, `name_cz`
- `description` → `description_en`, `description_de`, `description_cz`

**ScenarioStep level**:
- `content` → `content_en`, `content_de`, `content_cz`
- `annotation` → `annotation_en`, `annotation_de`, `annotation_cz`

**Non-localized fields** (remain as-is):
- `id` - technical identifier
- `category` - could be localized later or kept as technical key
- `difficulty` - could be localized later or kept as technical key
- `estimatedDurationSeconds` - numeric value
- `type` - technical enum value

---

## Integration Points & Required Changes

### 1. Domain Layer Changes

**Option A: Keep Single String Properties (Runtime Selection)**
- `ScenarioDefinition` and `ScenarioStep` remain unchanged
- Localization resolved during JSON loading based on selected language
- **Pro**: No domain model changes, clean separation
- **Con**: Language selection must happen at load time

**Option B: Add Localized Properties to Domain**
- Add `Dictionary<string, string>` properties (e.g., `LocalizedNames`)
- **Pro**: All translations available at runtime
- **Con**: More complex domain model, breaks single responsibility

**Recommendation**: Option A - resolve at load time

### 2. Infrastructure Layer Changes

**JsonScenarioLoader.cs** - MAIN INTEGRATION POINT:
```csharp
// Add language parameter to loader
public class JsonScenarioLoader
{
    private string _currentLanguage = "en"; // Default

    public void SetLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
    }

    // DTO modifications
    private class ScenarioDto
    {
        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }

        [JsonPropertyName("name_de")]
        public string? NameDe { get; set; }

        [JsonPropertyName("name_cz")]
        public string? NameCz { get; set; }

        // Selection logic during conversion
        public ScenarioDefinition ToScenarioDefinition(string language)
        {
            var name = language switch
            {
                "de" => NameDe ?? NameEn ?? throw new Exception("No name"),
                "cz" => NameCz ?? NameEn ?? throw new Exception("No name"),
                _ => NameEn ?? throw new Exception("No name")
            };

            return new ScenarioDefinition(name: name, ...);
        }
    }
}
```

**Fallback Strategy**:
- If `content_de` is missing, fall back to `content_en`
- If `content_en` is missing, throw validation error (English required as base)
- This allows incremental translation

### 3. Application Layer Changes

**ScenarioRegistry** - Add language support:
- Pass language selection to JsonScenarioLoader
- Reload scenarios when language changes

### 4. UI Layer Changes

**ScenarioSelector.razor** - Add language selector:
```razor
<div class="scenario-filters">
    <!-- Existing filters -->
    <select class="form-select form-select-sm" @bind="selectedCategory">
        <option value="">All Categories</option>
        ...
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

    private void OnLanguageChanged()
    {
        // Notify ScenarioRegistry to reload scenarios with new language
        // This would trigger re-rendering of the scenario list
    }
}
```

---

## Pros of Suffix Approach

✅ **Simple to implement** - straightforward JSON structure changes
✅ **Self-documenting** - clear what each field is for
✅ **No additional files** - all translations in one JSON
✅ **Easy to validate** - can check for missing translations
✅ **Compatible with existing tests** - minimal breaking changes
✅ **Gradual migration** - can support both old and new format during transition

---

## Cons of Suffix Approach

❌ **JSON file size increases** - 3x larger for fully translated scenarios
❌ **Not DRY** - violates "Don't Repeat Yourself" principle
❌ **Schema complexity** - 3x the number of properties
❌ **Harder to maintain** - adding a new language requires changing all scenarios
❌ **Not scalable** - adding 10 languages means 10 suffixes per field
❌ **Doesn't align with future app localization** - different pattern than typical .NET localization

---

## Alternative Approaches

### Alternative 1: Separate JSON Files Per Language

**Structure**:
```
/data/scenarios/
  en/
    context-limits-advanced.json
  de/
    context-limits-advanced.json
  cz/
    context-limits-advanced.json
```

**Pros**:
- Clean separation, no suffix bloat
- Easy to add new languages (just add folder)
- Smaller individual files
- Aligns with typical i18n patterns

**Cons**:
- More files to manage
- Risk of structure divergence between language versions
- Harder to ensure all languages have same steps

### Alternative 2: Separate Translation File + Base Scenario

**Structure**:
```json
// context-limits-advanced.json (structure only)
{
  "id": "context-limits-advanced",
  "category": "context-management",
  "steps": [
    { "type": "scenario_user_message", "translationKey": "step1_content" }
  ]
}

// translations/scenarios/context-limits-advanced.en.json
{
  "name": "Context Limits (Advanced)",
  "description": "Experience genuine context window truncation",
  "step1_content": "Hi, my name is John Doe."
}
```

**Pros**:
- True separation of content and structure
- Standard i18n pattern (similar to .resx, .json i18n files)
- Easy to outsource translation
- Aligns with future app-wide localization

**Cons**:
- More complex to implement
- Requires translation key management
- Two files to maintain per scenario

### Alternative 3: Hybrid Approach (Suffix Now, Migrate Later)

**Phase 1**: Use suffix approach for scenarios only (quick win)
**Phase 2**: When implementing app-wide localization, migrate scenarios to use same system

**Pros**:
- Get localization working quickly
- Learn from experience before app-wide implementation
- Low risk, incremental

**Cons**:
- Technical debt (will need to migrate)
- Two different systems temporarily

---

## Feasibility Assessment

### Technical Feasibility: ✅ HIGH

All approaches are technically feasible. The codebase is well-structured with:
- Clear separation of concerns (Domain/Infrastructure/Application/UI)
- Dependency injection making it easy to pass language context
- JSON-based configuration already in place

**Estimated Effort (Suffix Approach)**:
- Domain: 0 hours (no changes needed)
- Infrastructure (JsonScenarioLoader): 4-6 hours
- Application (ScenarioRegistry): 2-3 hours
- UI (Language selector): 3-4 hours
- Testing: 4-5 hours
- Translation work: Variable (depends on who does it)
- **Total**: ~15-20 hours development + translation time

### Compatibility Feasibility: ✅ HIGH

- Can support backward compatibility with fallback logic
- Existing scenario continues to work if we default to English
- No breaking changes to API contracts

### Maintenance Feasibility: ⚠️ MEDIUM

- Suffix approach adds maintenance burden (updating 3 fields per change)
- Need process for keeping translations in sync
- Translation quality control needed

---

## Future App-Wide Localization Considerations

### .NET Standard Localization Patterns

.NET typically uses:
1. **Resource files (.resx)** - compile-time resources
2. **IStringLocalizer** - runtime localization service
3. **JSON-based i18n** - more modern, flexible approach

### Recommendation: Plan for Convergence

**Scenario Localization (Now)**:
- Start with suffix approach for quick win
- Or use separate JSON files to match future pattern

**App-Wide Localization (Later)**:
- Implement IStringLocalizer or similar service
- Migrate scenarios to use same system
- Centralize language selection (user preference)

**Shared Infrastructure**:
- Create `ILocalizationService` interface
- Scenarios can use it now, rest of app later
- Single source of truth for current language

---

## Open Questions for Discussion

### 1. Translation Strategy
- **Q**: Who will do the translations? Human translators or AI-assisted?
- **Q**: What's the quality bar? Native speaker review needed?
- **Q**: Do we need professional translation for educational content?

### 2. Language Selection Persistence
- **Q**: Should language preference be saved per-user or per-session?
- **Q**: Should it be stored in browser localStorage or in database?
- **Q**: Should it affect only scenarios or be app-wide from day one?

### 3. Category & Difficulty Localization
- **Q**: Should `category` values be localized? (e.g., "context-management" → "Kontextverwaltung")
- **Q**: Or keep as technical keys and localize display only?
- **Q**: Same question for `difficulty` values?

### 4. Fallback Behavior
- **Q**: If German translation is incomplete, should we:
  - A) Fall back to English for missing strings (mixed language)?
  - B) Require 100% translation before showing scenario in that language?
  - C) Show warning that translation is incomplete?

### 5. Migration Path
- **Q**: Should we implement suffix approach as "quick and dirty" with plan to migrate?
- **Q**: Or invest more time upfront in proper i18n infrastructure?
- **Q**: What's the priority/timeline for app-wide localization?

### 6. Scenario ID vs Name
- **Q**: Currently scenario ID is technical (kebab-case). Should it remain language-agnostic?
- **Q**: Or allow localized IDs? (probably not recommended)

### 7. Content vs Structure
- **Q**: Are there scenarios where step structure differs by language/culture?
- **Q**: Or can we assume identical step sequences across all languages?
- **Q**: Should delays/timing differ by language (e.g., longer text = longer read time)?

### 8. Model Interaction Language
- **Q**: Should the AI model's responses also be in the selected language?
- **Q**: Do we need to inject language context into system prompts?
- **Q**: Example: "Respond in German to this teaching scenario"?

### 9. Testing Strategy
- **Q**: How do we test translations? Automated or manual?
- **Q**: Do we need native speakers to validate each scenario?
- **Q**: Should we have smoke tests for all language versions?

### 10. Scope for Initial Release
- **Q**: Start with all 3 languages or just English + one other?
- **Q**: Translate all scenarios or just the most popular one(s)?
- **Q**: Beta test with specific language community first?

---

## Recommended Next Steps

1. **Decide on approach** (suffix vs. separate files vs. hybrid)
2. **Answer key questions** (especially #5, #8, #10)
3. **Create proof-of-concept** with one scenario in 2 languages
4. **Test with native speaker** for quality validation
5. **Define translation workflow** (who, how, review process)
6. **Implement infrastructure** (loader, language selector)
7. **Translate scenarios** (prioritize most used ones)
8. **User testing** with target language communities
9. **Document pattern** for future scenario authors

---

## Risk Assessment

### Low Risk
- Technical implementation (straightforward)
- Backward compatibility (can be maintained)

### Medium Risk
- Translation quality (need good process)
- Maintenance overhead (3x the content to keep in sync)

### High Risk
- Creating technical debt if approach doesn't align with future app localization
- User confusion if partial translations or inconsistent quality

### Mitigation Strategies
- Start small (one scenario, two languages)
- Get native speaker validation
- Plan migration path from day one
- Document translation guidelines
- Use AI tools for initial translation + human review

---

## My Initial Thoughts & Reactions

### What I Like About the Suffix Approach

The simplicity is appealing for a quick win. You can get scenarios localized without major architectural changes. It's easy to understand and implement.

### What Concerns Me

1. **Doesn't scale well** - If you later want 10 languages, the JSON becomes unwieldy
2. **Different pattern than .NET conventions** - When you add app-wide localization, you'll likely use IStringLocalizer or similar, creating two different systems
3. **Maintenance burden** - Every content change requires updating 3 fields

### Alternative Recommendation: Separate Files

I'd lean toward **separate JSON files per language** because:
- Cleaner, more maintainable
- Aligns with standard i18n patterns
- Easier to outsource translation work
- Scales better (adding language = adding folder)
- Each translator works on their own file without merge conflicts

**Trade-off**: Need to ensure structure consistency across language files (can be validated with schema + tests)

### Questions I Have for You

1. **Timeline**: How soon do you need this? (affects whether "quick and dirty" is acceptable)
2. **Scope**: Is app-wide localization planned for next few months? (affects architecture decision)
3. **Maintenance**: Who will maintain translations long-term?
4. **AI Response Language**: Should the AI model respond in the selected language during scenarios? This is a bigger question that affects system prompts.

---

## Session Crash Context

**Note**: This document was created because the session environment is prone to crashes during user responses. If the session crashes, start a new session and refer to this document to continue the brainstorming discussion.

**Status**: Awaiting feedback on questions and approach selection.

**Next Session Should**:
- Review this document
- Answer open questions
- Decide on approach (suffix vs. separate files vs. other)
- Create implementation plan if approved
