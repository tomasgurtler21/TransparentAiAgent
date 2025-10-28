# Claude Code Skills

This directory contains project-specific skills for Claude Code.

## Available Skills

### `tdd` - Lean Test-Driven Development

Guides implementation of Test-Driven Development for .NET projects using Lean TDD principles.

**Usage**: The skill activates automatically when implementing code with business logic.

**Contents**:
- `SKILL.md` - Main skill instructions with YAML frontmatter
- `examples.md` - Comprehensive code examples (good vs bad)
- `reference.md` - Quick reference guide for MSTest patterns

**Key Features**:
- Red-Green-Refactor workflow
- Decision framework for what to test
- MSTest patterns and best practices
- Common pitfalls and solutions

## Skill Structure

Each skill follows the Claude Code skill format:

```
skill-name/
├── SKILL.md       (required) - Main instructions with YAML frontmatter
├── examples.md    (optional) - Code examples
├── reference.md   (optional) - Quick reference
└── templates/     (optional) - Code templates
```

### SKILL.md Format

The main skill file must have YAML frontmatter:

```yaml
---
name: skill-name  # lowercase, hyphens only, max 64 chars
description: Brief description of what this skill does and when to use it (max 1024 chars)
---

# Skill Name

## Instructions
...
```

## Adding New Skills

1. Create a new folder: `.claude/skills/your-skill-name/`
2. Create `SKILL.md` with proper YAML frontmatter
3. Add optional supporting files (`examples.md`, `reference.md`, etc.)
4. Test the skill by invoking it

## Documentation

For more information on Claude Code skills, see:
https://docs.claude.com/en/docs/claude-code/skills
