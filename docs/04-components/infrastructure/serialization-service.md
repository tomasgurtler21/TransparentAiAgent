# Serialization Service

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 1-2
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- SerializationService implementation
- JSON serialization/deserialization
- System.Text.Json usage

**What does NOT belong here**:
- ❌ Transparency event formatting → See [transparency-service.md](transparency-service.md)
- ❌ Tool argument parsing → See [../tools/](../tools/)

---

## Overview

SerializationService provides standardized JSON serialization using System.Text.Json.

## Purpose

- **Serialize** objects to JSON (transparency events, tool arguments)
- **Deserialize** JSON to objects
- **Standardize** JSON options (camelCase, indent)

## Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Serialization/SerializationService.cs`

**Key Methods**:
- `Serialize<T>(obj)` - Object to JSON
- `Deserialize<T>(json)` - JSON to object

**JSON Options**:
- PropertyNamingPolicy: CamelCase
- WriteIndented: true (for readability)

## Usage

Used by:
- Transparency service (event serialization)
- Tool system (argument parsing)
- Configuration service (config persistence)

## Related Documentation

- [Transparency Service](transparency-service.md)

---

**See Also**: [Infrastructure Overview](README.md)
