# Localization and Translation Guide

**Last Updated**: 2025-11-26

## Overview

This guide explains how to work with localization and translation files in the TransparentAiAgent project, including the semantic key naming convention for scenario translations.

---

## Translation File Structure

Translation files are located in:
```
TransparentAiAgentGui/data/translations/
├── cs.json  (Czech)
├── de.json  (German)
└── ...      (future languages)
```

Each translation file is a JSON object with key-value pairs where:
- **Key**: A semantic identifier for the translatable string
- **Value**: The translated text in the target language

---

## Semantic Key Naming Convention

### Why Semantic Keys?

Previously, we used step-based numbering (e.g., `step0`, `step1`, `step13`). This had problems:
- ❌ Fragile - inserting/removing steps broke all subsequent keys
- ❌ Required updating scenario JSON + all translation files
- ❌ No semantic meaning - `step13` doesn't indicate what it does

**Solution**: Use descriptive, semantic keys that describe the content's purpose.

### Key Structure

```
scenarios.{scenario-id}.{semantic-name}.{property}
```

**Components**:
- `scenarios` - Fixed prefix for all scenario translations
- `{scenario-id}` - The scenario's unique identifier (from scenario JSON)
- `{semantic-name}` - Descriptive name explaining what this content does
- `{property}` - Type of content: `name`, `description`, `content`, `annotation`, `pauseMessage`

### Naming Rules

1. **Use kebab-case** (lowercase with hyphens)
   - ✅ `introduce-name`
   - ❌ `introduceName`, `introduce_name`, `IntroduceName`

2. **Start with a verb when possible**
   - ✅ `ask-name`, `verify-memory`, `explain-truncation`
   - ⚠️ `name-question`, `memory-check` (acceptable but less clear)

3. **Keep it concise but descriptive** (3-5 words max)
   - ✅ `verify-name-remembered`
   - ❌ `verify-that-the-model-still-remembers-the-name`

4. **For similar steps, add context or numbers**
   - ✅ `demo-message-1`, `demo-message-2`, `demo-message-3`
   - ✅ `fill-context-first`, `fill-context-second`

5. **Be specific about the action or purpose**
   - ✅ `verify-name-forgotten` (checks if name was truncated)
   - ❌ `check-name` (too vague)

### Examples

#### Context Limits Advanced Scenario

```json
{
  "scenarios.context-limits-advanced.name": "Context Limits (Advanced)",
  "scenarios.context-limits-advanced.description": "Experience genuine context window truncation",

  "scenarios.context-limits-advanced.reduce-message-limit.annotation": "Message limit reduced to 8...",
  "scenarios.context-limits-advanced.introduce-name.content": "Hi, my name is John Doe.",
  "scenarios.context-limits-advanced.introduce-name.annotation": "The model will remember this...",
  "scenarios.context-limits-advanced.verify-name-remembered.content": "What is my name?",
  "scenarios.context-limits-advanced.verify-name-remembered.annotation": "Verifying the model still has...",
  "scenarios.context-limits-advanced.demo-message-1.content": "Demo message, just respond 'Confirmed'.",
  "scenarios.context-limits-advanced.demo-message-1.annotation": "Filling context to force truncation...",
  "scenarios.context-limits-advanced.demo-message-2.content": "Demo message, just respond 'Confirmed'.",
  "scenarios.context-limits-advanced.demo-message-3.content": "Demo message, just respond 'Confirmed'.",
  "scenarios.context-limits-advanced.count-demo-messages.content": "How many demo messages...",
  "scenarios.context-limits-advanced.verify-name-forgotten.content": "What is my name?",
  "scenarios.context-limits-advanced.explain-truncation.content": "Scenario 'Context Limits' has ended...",
  "scenarios.context-limits-advanced.pause-check-indicators.pauseMessage": "Take your time to check...",
  "scenarios.context-limits-advanced.restore-message-limit.annotation": "Message limit restored...",
  "scenarios.context-limits-advanced.ask-about-other-agents.content": "Thanks, disable the context..."
}
```

#### Coding Limits Question Scenario

```json
{
  "scenarios.coding-limits-question.name": "Why Doesn't Agent Follow Length Limits?",
  "scenarios.coding-limits-question.description": "Question about agent exceeding...",
  "scenarios.coding-limits-question.user-question-about-limits.content": "Why is my other coding agent..."
}
```

---

## Benefits of Semantic Keys

✅ **Order-independent**: Can reorder, insert, or remove steps without breaking keys
✅ **Self-documenting**: Keys explain what they contain
✅ **Easy to find**: Developers and translators can locate specific content quickly
✅ **Maintainable**: Future developers understand the purpose of each key
✅ **Industry standard**: Used by React i18n, Angular, Vue i18n, .NET resources

---

## Creating a New Scenario

When creating a new scenario with translations:

### 1. Design Your Scenario

Create the scenario JSON file in `TransparentAiAgentGui/data/scenarios/`:

```json
{
  "$schema": "https://transparentaiagent.dev/schemas/teaching-scenario/v1",
  "id": "my-new-scenario",
  "version": "1.0",
  "nameKey": "scenarios.my-new-scenario.name",
  "name": "My New Scenario",
  "descriptionKey": "scenarios.my-new-scenario.description",
  "description": "A brief description",
  "steps": [
    {
      "type": "scenario_user_message",
      "contentKey": "scenarios.my-new-scenario.greeting.content",
      "content": "Hello! Let's begin.",
      "annotationKey": "scenarios.my-new-scenario.greeting.annotation",
      "annotation": "Initial greeting to start the scenario.",
      "delay": 1000
    }
  ]
}
```

### 2. Choose Semantic Names

For each step, choose a semantic name:
- What is this step doing?
- What action or purpose does it serve?
- How would you explain it to someone?

**Good examples**:
- User introduces themselves → `introduce-user`
- Asking the AI to recall information → `verify-recall`
- Explaining a concept → `explain-context-limits`
- Pausing for user interaction → `pause-for-review`

### 3. Add to All Translation Files

Add entries to **each** translation file (`cs.json`, `de.json`, etc.):

```json
{
  "scenarios.my-new-scenario.name": "Translated scenario name",
  "scenarios.my-new-scenario.description": "Translated description",
  "scenarios.my-new-scenario.greeting.content": "Translated greeting",
  "scenarios.my-new-scenario.greeting.annotation": "Translated annotation"
}
```

---

## Updating Existing Scenarios

When modifying a scenario:

### Adding a New Step

1. Choose a semantic name for the new step
2. Add the key to the scenario JSON
3. Add translations to all language files

**No need to renumber anything!** Just insert the step where it belongs.

### Removing a Step

1. Remove the step from the scenario JSON
2. Remove the keys from all translation files (optional but recommended for cleanup)

### Reordering Steps

Simply reorder the steps in the scenario JSON - no key changes needed!

---

## Translation Workflow

### Translating to a New Language

1. Copy an existing translation file (e.g., `de.json`)
2. Rename it to the new language code (e.g., `fr.json` for French)
3. Translate all values (keep keys unchanged!)
4. Test the scenario in the new language

### Updating Translations

When the English content changes:

1. Update the `content` or `annotation` in the scenario JSON file
2. Update the corresponding value in each translation file
3. Keys remain the same - only values change

### Translation Best Practices

- **Keep formatting**: Preserve `\n` for line breaks
- **Maintain tone**: Match the friendliness/formality of the original
- **Context matters**: Read the scenario flow to understand context
- **Test thoroughly**: Run scenarios in each language to verify

---

## Common Patterns

### Configuration Changes

```
reduce-message-limit
restore-message-limit
apply-custom-settings
reset-configuration
```

### User Messages

```
introduce-name
verify-name-remembered
verify-name-forgotten
ask-question
provide-answer
```

### Demo/Fill Messages

```
demo-message-1
demo-message-2
fill-context-step-1
placeholder-message
```

### Explanations

```
explain-truncation
explain-concept
describe-behavior
clarify-expectations
```

### User Pauses

```
pause-for-review
pause-check-indicators
pause-before-proceeding
```

---

## Troubleshooting

### Missing Translation

**Symptom**: UI shows the key instead of translated text (e.g., "scenarios.my-scenario.step.content")

**Solution**: Ensure the key exists in all translation files with translated values.

### Wrong Translation Displayed

**Symptom**: Translation doesn't match the current scenario content

**Solution**: Update the translation values in all language files to match the latest English content.

### Key Not Found Error

**Symptom**: Application logs show "Key not found" errors

**Solution**: Verify the `contentKey` in scenario JSON exactly matches the key in translation files (case-sensitive, check for typos).

---

## Summary

- ✅ Use semantic, descriptive keys instead of step numbers
- ✅ Follow the pattern: `scenarios.{scenario-id}.{semantic-name}.{property}`
- ✅ Use kebab-case with verbs when possible
- ✅ Keep keys concise but meaningful
- ✅ Update all translation files when adding/modifying scenarios
- ✅ Test scenarios in all supported languages

This approach makes translations maintainable, scalable, and developer-friendly!

---

## Related Documentation

- [Adding Knowledge Entries](adding-knowledge-entries.md)
- [Development Guide](05-development.md)
- [Scenario Schema](../../TransparentAiAgentGui/data/scenarios/README.md) *(if exists)*
