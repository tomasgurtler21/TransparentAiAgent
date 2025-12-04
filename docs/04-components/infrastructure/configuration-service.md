# Configuration Service

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 1-2
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- ConfigurationService implementation
- Configuration loading and saving
- AppConfiguration management

**What does NOT belong here**:
- ❌ Configuration models → See Domain/Configuration
- ❌ Configuration guide → See `docs/05-guides/installation/llm-provider-selector.md`

---

## Overview

ConfigurationService manages application configuration loading from files, runtime updates, and persistence.

## Purpose

- **Load** configuration from appsettings.json
- **Validate** configuration at startup
- **Provide** access to AppConfiguration
- **Save** runtime configuration changes
- **Support** hot-reload where appropriate

## Implementation

**File**: `TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationService.cs`

**Key Methods**:
- `LoadConfiguration()` - Load from file
- `SaveConfiguration()` - Persist to file
- `GetConfiguration()` - Access current config

## Configuration Sources

1. `appsettings.json` - Main configuration
2. `appsettings.Development.json` - Development overrides
3. Environment variables - Runtime overrides

## Related Documentation

- [Configuration Guide](../../05-guides/installation/llm-provider-selector.md)
- [AppConfiguration](../../02-architecture/overview.md#configuration)

---

**See Also**: [Infrastructure Overview](README.md)
