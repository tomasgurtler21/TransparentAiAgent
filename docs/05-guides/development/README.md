# Development Guides

**Last Updated**: 2025-12-02
**Status**: Active

---

## 📋 Document Scope

**What belongs in this directory**:
- Developer-focused how-to guides
- Building and packaging instructions
- Adding new components and features
- Testing strategies and workflows
- Code contribution guidelines

**What does NOT belong here**:
- ❌ End-user installation instructions (→ belongs in 05-guides/installation/)
- ❌ Conceptual explanations (→ belongs in 03-concepts/)
- ❌ Architecture overviews (→ belongs in 02-architecture/)
- ❌ Component API details (→ belongs in 04-components/)

---

## Guides for Developers

### Building and Packaging

#### [building-for-deployment.md](building-for-deployment.md)
**How to build the application for distribution** (Windows):
- Using Visual Studio Publish
- Command line alternative with `dotnet publish`
- Self-contained deployment (includes .NET runtime)
- Creating distribution ZIP files
- Build troubleshooting
- Output verification

**Use this** when preparing a release or distributable package.

#### [folder-structure.md](folder-structure.md)
**Understanding the deployment folder structure**:
- Complete directory layout after build
- File and folder purposes
- Data folder organization (`conversations/`, `memory/`, `knowledge/`, `scenarios/`)
- Backup recommendations
- Disk space management
- Portable installation considerations

**Use this** to understand what files go where in a built application.

---

### Adding Features

#### [adding-knowledge-entries.md](adding-knowledge-entries.md)
**How to add new knowledge library entries**:
- Knowledge entry structure
- JSON schema and validation
- Category organization
- Testing knowledge entries
- Best practices

**Use this** when adding new built-in knowledge to teach the agent.

#### [scenario-schema-reference.md](scenario-schema-reference.md)
**Complete JSON schema for teaching scenarios**:
- Scenario structure and step types
- JSON format specification
- Configuration overlay system
- Validation rules
- Example scenarios

**Use this** when creating new teaching mode scenarios.

---

### Internationalization

#### [localization-guide.md](localization-guide.md)
**Adding translations and localization**:
- Translation file structure
- Supported languages
- Adding new translations
- Testing localization
- Language fallback behavior

**Use this** when adding support for new languages.

---

### Testing

**Note**: Test-Driven Development (TDD) workflow is covered by the `.claude/skills/tdd/` skill.

For TDD guidance:
- See `.claude/skills/tdd/README.md` for the TDD workflow
- The skill provides step-by-step test-first development process
- Follows Lean TDD principles (testing meaningful behavior, not compiler features)

---

## Development Workflow Overview

### Building the Application

1. **Development Build** (for testing):
   ```bash
   dotnet build
   dotnet run --project TransparentAiAgentGui
   ```

2. **Release Build** (for distribution):
   - See [building-for-deployment.md](building-for-deployment.md)

### Adding a New Feature

1. **Plan**: Review [03-concepts/](../../03-concepts/) for architectural patterns
2. **Test**: Use `.claude/skills/tdd/` for test-driven development
3. **Implement**: Follow patterns in [04-components/](../../04-components/)
4. **Document**: Update relevant docs in this directory

### Contributing

1. **Code Style**: Follow existing patterns in codebase
2. **Testing**: Write tests for meaningful behavior (see `.claude/skills/tdd/`)
3. **Documentation**: Update guides if adding new capabilities
4. **Commit**: Use clear, descriptive commit messages

---

## Related Documentation

### For Developers:
- [Architecture Overview](../../02-architecture/overview.md) - System design and structure
- [Components](../../04-components/README.md) - Component details and APIs
- [Reference](../../06-reference/README.md) - Provider APIs and technical specs
- [TDD Skill](../../../.claude/skills/tdd/README.md) - Test-driven development workflow

### For End Users:
- [Installation Guides](../installation/README.md) - Installing and configuring the app
- [Feature Guides](../features/README.md) - Using specific features

### For Integrations:
- [Integration Guides](../integration/README.md) - MCP servers and custom tools

---

## Quick Reference

| Task | Guide |
|------|-------|
| Build distributable package | [building-for-deployment.md](building-for-deployment.md) |
| Understand build output | [folder-structure.md](folder-structure.md) |
| Add knowledge entry | [adding-knowledge-entries.md](adding-knowledge-entries.md) |
| Create teaching scenario | [scenario-schema-reference.md](scenario-schema-reference.md) |
| Add translation | [localization-guide.md](localization-guide.md) |
| Write tests | `.claude/skills/tdd/README.md` |

---
