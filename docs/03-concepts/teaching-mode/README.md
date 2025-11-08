# Teaching Mode Documentation

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 9

---

## 📋 Document Scope

**What belongs in this directory**:
- Teaching Mode vision and philosophy
- UI control architecture for Teaching Mode
- Implementation roadmap and phases
- How Teaching Mode works across components
- Use cases and examples

**What does NOT belong here**:
- ❌ Component-specific implementation (→ belongs in 04-components/ui/)
- ❌ Step-by-step guides (→ belongs in 05-guides/)
- ❌ General UI architecture (→ belongs in 02-architecture/)

---

## Overview

**Teaching Mode** is a transformational layer that enables agents to dynamically control UI components, creating interactive learning experiences while maintaining full transparency.

It transforms the transparent agent from **passive transparency** (showing what happens) to **active teaching** (teaching users why and how).

## Documents

### [vision.md](vision.md)
Philosophy and concepts behind Teaching Mode:
- Core principles (transparency, discoverability, interactivity)
- Two modes of operation (Normal vs. Teaching)
- Use cases and user journeys
- Design principles

### [architecture.md](architecture.md)
Technical architecture and implementation:
- UIState model and UIControlService
- Built-in UI control tools (7 tools)
- Data flow and integration with existing architecture
- Component specifications

### [implementation-roadmap.md](implementation-roadmap.md)
Phase-by-phase implementation plan:
- Phase 9a: Core Infrastructure
- Phase 9b: Chat History Control Tools
- Phase 9c: Additional UI Component Tools
- Phase 9d: Teaching Mode System
- Phase 9e: Polish & Documentation

---

## Quick Links

- [What is Teaching Mode?](vision.md#what-is-teaching-mode)
- [Key Use Cases](vision.md#use-cases--user-journeys)
- [Architecture Overview](architecture.md#architecture-extension)
- [Implementation Plan](implementation-roadmap.md#implementation-overview)

---

## Related Documentation

- [Architecture Overview](../../02-architecture/overview.md) - Base system architecture
- [UI Components](../../04-components/ui/README.md) - UI component details
- [Transparency Concept](../transparency.md) - Foundation for Teaching Mode

---
