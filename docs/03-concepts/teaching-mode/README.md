# Teaching Mode Documentation

**Last Updated**: 2025-12-04
**Status**: Active - Core Complete, Ongoing Enhancements

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
Implementation plan and progress tracking:
- Core Infrastructure (Complete)
- Chat History Control Tools (Complete)
- Additional UI Component Tools (Complete)
- Teaching Mode System (Complete)
- Documentation (Complete)

### [future-enhancements.md](future-enhancements.md)
Planned future enhancements:
- **Enhancement 1**: Basic Scenarios/Scripts System
- **Enhancement 1b**: Advanced Scenarios with Environment Manipulation
- **Enhancement 2**: Knowledge Library
- Implementation priorities and success metrics

### [scenario-schema.md](scenario-schema.md)
Complete JSON schema for teaching scenarios:
- Basic and advanced step types
- Scenario definition format
- Validation rules and extensibility
- Example scenarios

### [config-overlay-service.md](config-overlay-service.md)
Config Overlay Service API design:
- Stack-based temporary configuration overrides
- Usage patterns and integration points
- High-level interface design (detailed implementation TBD)

### [reference-scenarios/](reference-scenarios/)
Reference scenario implementations:
- **[context-limits-advanced.md](reference-scenarios/context-limits-advanced.md)**: Complete example of advanced scenario with environment manipulation

---

## Quick Links

**Core Teaching Mode:**
- [What is Teaching Mode?](vision.md#what-is-teaching-mode)
- [Key Use Cases](vision.md#use-cases--user-journeys)
- [Architecture Overview](architecture.md#system-overview)
- [Implementation Plan](implementation-roadmap.md#implementation-overview)

**Future Enhancements:**
- [Advanced Scenarios Concept](future-enhancements.md#enhancement-1b-advanced-scenarios-with-environment-manipulation)
- [Scenario JSON Schema](scenario-schema.md#schema-version)
- [Config Overlay API](config-overlay-service.md#api-design-high-level)
- [Context Limits Reference Scenario](reference-scenarios/context-limits-advanced.md)

---

## Related Documentation

- [Architecture Overview](../../02-architecture/overview.md) - Base system architecture
- [UI Components](../../04-components/ui/README.md) - UI component details
- [Transparency Concept](../transparency.md) - Foundation for Teaching Mode

---
