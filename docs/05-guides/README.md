# Guides Documentation

**Last Updated**: 2025-12-02
**Status**: Active

---

## 📋 Document Scope

**What belongs in this directory**:
- Step-by-step how-to guides
- Task-oriented documentation
- Troubleshooting procedures
- Setup and configuration instructions

**What does NOT belong here**:
- ❌ Conceptual explanations (→ belongs in 03-concepts/)
- ❌ Architecture overviews (→ belongs in 02-architecture/)
- ❌ Component details (→ belongs in 04-components/)
- ❌ API reference (→ belongs in 06-reference/)

---

## Overview

This directory contains practical, task-oriented guides organized by audience and purpose.

---

## 📖 Guide Categories

### [installation/](installation/) - For End Users
**Installing, configuring, and running the application**:
- [Installation Guide](installation/installation-guide.md) - Complete installation walkthrough
- [LLM Provider Configuration](installation/llm-provider-selector.md) - Multi-provider setup
- [Data Storage](installation/data-storage.md) - Understanding where your data lives
- [Troubleshooting](installation/troubleshooting.md) - Fixing common issues

**Start here** if you're a user wanting to install and use TransparentAiAgent.

**→ See [installation/README.md](installation/README.md) for complete guide index**

---

### [development/](development/) - For Developers
**Building, testing, and extending the application**:
- [Building for Deployment](development/building-for-deployment.md) - Package for distribution
- [Folder Structure](development/folder-structure.md) - Understand build output
- [Adding Knowledge Entries](development/adding-knowledge-entries.md) - Extend knowledge library
- [Scenario Schema Reference](development/scenario-schema-reference.md) - Create teaching scenarios
- [Localization Guide](development/localization-guide.md) - Add translations

**TDD Workflow**: See `.claude/skills/tdd/` for test-driven development guidance

**Start here** if you're contributing code or building custom versions.

**→ See [development/README.md](development/README.md) for complete guide index**

---

### [features/](features/) - Feature Usage Guides
**How to use specific features**:
- [Using Long-Term Memory](features/using-long-term-memory.md) - Enable and manage agent memory
- Additional feature guides as they're added

**Start here** to learn about specific capabilities.

---

### [integration/](integration/) - Integration Guides
**Connecting external services and tools**:
- MCP server integration
- Custom tool development
- OAuth setup
- API integrations

**Start here** if you're adding external tools or services.

---

## 🎯 Quick Navigation

### I want to...

**Install the application**
→ [installation/installation-guide.md](installation/installation-guide.md)

**Configure my LLM provider**
→ [installation/llm-provider-selector.md](installation/llm-provider-selector.md)

**Fix an issue**
→ [installation/troubleshooting.md](installation/troubleshooting.md)

**Build a release package**
→ [development/building-for-deployment.md](development/building-for-deployment.md)

**Add a knowledge entry**
→ [development/adding-knowledge-entries.md](development/adding-knowledge-entries.md)

**Create a teaching scenario**
→ [development/scenario-schema-reference.md](development/scenario-schema-reference.md)

**Use long-term memory**
→ [features/using-long-term-memory.md](features/using-long-term-memory.md)

**Write tests**
→ `.claude/skills/tdd/README.md`

---

## 📚 Guide Structure

All guides in this directory follow a consistent structure:

1. **Purpose**: What this guide helps you accomplish
2. **Prerequisites**: What you need before starting
3. **Steps**: Clear, numbered instructions
4. **Verification**: How to confirm it worked
5. **Troubleshooting**: Common issues and solutions
6. **Related**: Links to related guides

---

## 🔗 Related Documentation

- [Concepts](../03-concepts/README.md) - Understand core concepts before implementing
- [Components](../04-components/README.md) - Component details and APIs
- [Architecture](../02-architecture/README.md) - System design and structure
- [Reference](../06-reference/README.md) - API and provider specifications

---

## 💡 Documentation Philosophy

**Guides are task-oriented**: They answer "How do I...?" questions with concrete steps.

**For conceptual understanding**, see:
- [03-concepts/](../03-concepts/) - What things are and why they exist
- [02-architecture/](../02-architecture/) - How the system is structured

**For reference information**, see:
- [04-components/](../04-components/) - Component APIs and details
- [06-reference/](../06-reference/) - Provider APIs and specs

---
