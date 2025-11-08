# Development Practices

**Last Updated**: 2025-11-08

---

## 📋 Document Scope

### ✅ This document covers:
- Core development practices and workflows
- Testing approach (Lean TDD)
- Documentation maintenance principles
- Development standards

### ❌ This document does NOT cover:
- **Step-by-step how-to guides** → See [05-guides/development/](05-guides/development/)
- **Component architecture** → See [04-components/](04-components/)
- **Contributing templates** → See [08-contributing/templates/](08-contributing/templates/)

---

## 🧪 Testing Approach: Lean TDD

We follow **Lean Test-Driven Development (TDD)** principles:

- Write tests for new features and bug fixes
- Keep tests focused and maintainable
- Avoid over-testing implementation details
- Focus on behavior, not internals

### For Claude Code CLI Users

When working with Claude Code CLI, use the **TDD skill** for automated test-driven workflows:

```
Use .claude/skills/tdd/ skill
```

This skill provides:
- Guided TDD workflow
- Test generation assistance
- Red-Green-Refactor cycles
- Test running and feedback

See `.claude/skills/tdd/` directory for complete TDD guidelines.

### For Claude Code Web Users

**Claude Code Web cannot compile or run tests**, which requires a modified TDD workflow:

**Workflow:**
1. **Claude reads TDD skill manually** from `.claude/skills/tdd/` directory
2. **RED Phase - Claude writes batch of tests**
   - Write multiple tests that compile successfully
   - Tests should FAIL when run (no implementation yet)
   - Commit & push the failing tests
3. **User runs tests** - Validates RED phase (tests fail as expected)
4. **GREEN Phase - Claude writes implementations**
   - Write minimal code to make tests pass
   - Commit & push the implementations
5. **User runs tests** - Validates GREEN phase (tests pass)
6. **Claude fixes failures** if any tests still fail
7. **Repeat** for next component

**Key Differences from CLI:**
- ❌ No automated RED-GREEN-REFACTOR cycle
- ❌ Claude cannot run tests or verify compilation
- ✅ User must be in the loop to run tests
- ✅ Tests written first in RED phase, implementation in GREEN phase
- ✅ Larger batches within each phase to reduce token costs

**CRITICAL - What "Batching" Means:**
- ✅ **Correct**: Write multiple tests in RED phase, THEN multiple implementations in GREEN phase
  - Example: Write 5 tests → commit → user validates they fail → write 5 implementations → commit → user validates they pass
- ❌ **WRONG**: Write tests + implementations together in one batch
  - This skips RED phase and defeats TDD's purpose of preventing false positives
- **Balance**: Batch size vs crash resistance
  - Small batches (1-3 tests): More resistant to crashes, higher token cost
  - Large batches (5-10 tests): Lower token cost, more work lost if crash
  - Recommended: 3-5 tests per batch for balance

**Critical**: Claude must read and follow `.claude/skills/tdd/SKILL.md` for Lean TDD principles even without test execution capability.

---

## 📚 Documentation Maintenance

**Golden Rule**: Source code is the source of truth for technical documentation.

### Immediate Update Policy

**When code changes, update docs immediately.**

If you discover a discrepancy between documentation and code:

1. **Assume the code is correct** (unless it's a bug)
2. **Update the affected documentation** right away
3. **Don't batch documentation updates** - do them as you code

### What Must Be Kept in Sync

- **Component documentation** (04-components/) - Must match actual implementation
- **Architecture docs** (02-architecture/) - Must reflect current structure
- **API references** (06-reference/) - Must match actual interfaces
- **How-to guides** (05-guides/) - Must work with current code

### Exceptions

These docs are **NOT** bound to current code state:
- **Roadmap** (07-planning/roadmap.md) - Future plans
- **Requirements** (07-planning/requirements.md) - Vision and goals
- **Archive** (09-archive/) - Historical records

---

## 🎯 Additional Development Practices

### Code Style Standards

**Type Declarations**
- ❌ **No `var` keyword** - Always use concrete types in implementation
- ✅ Example: `IConfigurationService service = new ConfigurationService();`
- ❌ Example: `var service = new ConfigurationService();`

**XML Documentation**
- ✅ **Every class and method must have XML documentation summary**
- Required for all public classes, methods, and properties
- Example:
  ```csharp
  /// <summary>
  /// Manages conversation state and message history.
  /// </summary>
  public class ConversationManager
  {
      /// <summary>
      /// Adds a new message to the conversation history.
      /// </summary>
      /// <param name="message">The message to add.</param>
      public void AddMessage(IMessage message)
      {
          // Implementation
      }
  }
  ```

### Development Philosophy

**Rapid Prototyping**
- This is a rapid prototype / "vibe coding" project
- Focus on working functionality over perfect formatting
- Don't slow down for formatting nitpicks or elaborate deployment configurations
- **However: TDD is non-negotiable** - it keeps AI agents in check and prevents regressions

**Note**: Many existing files in the codebase violate these standards - that's a known issue. New code should follow these practices.

