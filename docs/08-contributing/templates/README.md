# Documentation Templates

This directory contains templates for creating new documentation files.

## Available Templates

### 1. Component Template (`component-template.md`)
Use when documenting a specific component (Agent Orchestrator, LLM Provider, etc.)

**Includes**:
- Scope header (what belongs/doesn't belong)
- Purpose and responsibilities
- Architecture and interfaces
- Testing strategy
- Usage examples

### 2. Concept Template (`concept-template.md`)
Use when documenting cross-cutting concepts (Transparency, Teaching Mode, etc.)

**Includes**:
- Scope header
- What/why/how explanations
- Use cases
- Best practices and pitfalls

### 3. Guide Template (`guide-template.md`)
Use when creating step-by-step how-to guides

**Includes**:
- Scope header
- Prerequisites
- Detailed steps
- Verification and troubleshooting

### 4. Decision Template (`decision-template.md`)
Use when adding design decisions to `02-architecture/design-decisions.md`

**Includes**:
- Context and rationale
- Options considered
- Consequences

## Critical: Scope Headers

**Every template includes a scope header** that defines:
- ✅ What belongs in this type of document
- ❌ What does NOT belong (with redirects)

**Why**: Prevents AI agents and humans from putting content in wrong places.

## Usage

1. Copy the appropriate template
2. Rename to match your topic
3. Fill in all sections
4. Keep the scope header - it's critical for maintaining doc quality

---

**Last Updated**: 2025-12-04
