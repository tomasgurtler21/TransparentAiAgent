# Concepts Documentation

**Last Updated**: 2025-11-14
**Status**: Active

---

## 📋 Document Scope

**What belongs in this directory**:
- Cross-cutting features that span multiple components
- High-level concepts and philosophies
- How system features work across the architecture
- Use cases and best practices
- Conceptual explanations (not code)

**What does NOT belong here**:
- ❌ Component-specific implementation (→ belongs in 04-components/)
- ❌ Step-by-step guides (→ belongs in 05-guides/)
- ❌ Architecture diagrams (→ belongs in 02-architecture/)
- ❌ Code examples (→ belongs in components or guides)

---

## Overview

This directory contains documentation for **cross-cutting concepts** - features and ideas that transcend individual components and require understanding how multiple parts of the system work together.

## Concepts

### [Message Hierarchy](message-hierarchy.md) 📬
Four-tier message architecture categorizing messages by origin (User, LLM, Application, Tool).

**Key Topics**:
- Message origin categories
- Design benefits (stable API, extensibility)
- Common usage patterns
- Role vs. origin distinction

**Status**: ✅ Implemented

---

### [Teaching Mode](teaching-mode/README.md) 📚
Transformational layer enabling agents to control UI and teach users interactively.

**Key Documents**:
- [Vision & Philosophy](teaching-mode/vision.md)
- [Architecture](teaching-mode/architecture.md)
- [Implementation Roadmap](teaching-mode/implementation-roadmap.md)

**Status**: 📋 Planning Complete

---

### [Tool System](tool-system.md) 🔧
MCP-based tool integration and abstraction.

**Status**: ✅ Implemented

---

### [Knowledge Library](knowledge-library.md) 📚
Curated guardrails that guide the teaching agent when explaining critical concepts.

**Key Topics**:
- Guardrails vs. encyclopedias approach
- When to use the knowledge library
- Knowledge gap likelihood
- Integration with Teaching Mode

**Status**: ✅ Implemented

---

### [Long-Term Memory](long-term-memory.md) 🧠
Persistent agent memory that remembers user context, preferences, and background across conversation sessions.

**Key Topics**:
- Mode-aware memory storage
- Memory lifecycle and management
- Privacy considerations and guardrails
- Use cases and best practices

**Status**: ✅ Implemented

---

### Other Concepts (To be created)

The following concept documents will be created as the documentation reorganization continues:

- **Transparency** - What transparency means, how it's implemented across the system
- **Context Management** - How conversation history is managed, truncation, indicators
- **Streaming** - How LLM responses stream through the system
- **MCP Protocol** - What MCP is, why we use it, how it integrates

---

## When to Create a Concept Doc

Create a concept document when:
- ✅ Feature spans multiple components
- ✅ Understanding requires knowing how parts work together
- ✅ It's a core philosophy or principle
- ✅ Users need high-level understanding before diving into components

Do NOT create a concept doc for:
- ❌ Single-component features (use component docs)
- ❌ Step-by-step procedures (use guides)
- ❌ Implementation details (use component docs)

---

## Related Documentation

- [Architecture](../02-architecture/README.md) - System structure
- [Components](../04-components/README.md) - Component details
- [Guides](../05-guides/README.md) - How-to documentation

---
