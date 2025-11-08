# Design Decision Template

**Note**: Design decisions are logged in `02-architecture/design-decisions.md` as sections, not separate files.

**File Scope** (for design-decisions.md):
- ✅ Architectural and technical decisions with rationale
- ✅ Context, options considered, consequences
- ❌ Implementation code (→ belongs in components)
- ❌ Session notes (→ belongs in archive)

---

## Template Format

Use this format when adding a new design decision to `02-architecture/design-decisions.md`:

```markdown
### DD-XXX: [Decision Title]

**Date**: YYYY-MM-DD
**Status**: Proposed | Accepted | Deprecated

**Context**:
Why this decision was needed (background, problem statement).

**Decision**:
What was decided (clear, concise statement).

**Options Considered**:
1. Option 1
   - Pros: ...
   - Cons: ...
2. Option 2
   - Pros: ...
   - Cons: ...
3. Option 3
   - Pros: ...
   - Cons: ...

**Rationale**:
Why this option was chosen over alternatives.

**Consequences**:
- Positive consequence 1
- Positive consequence 2
- Trade-off 1
- Trade-off 2

**Related Decisions**:
- DD-XXX
- DD-YYY

---
```
