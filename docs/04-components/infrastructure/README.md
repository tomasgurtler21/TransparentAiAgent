# Infrastructure Components

**Last Updated**: 2025-11-08
**Status**: Active
**Layer**: Infrastructure

---

## Overview

Infrastructure components implement external integrations and cross-cutting concerns. They provide concrete implementations of domain interfaces for LLM providers, configuration, authentication, transparency logging, and serialization.

## Components

### [Configuration Service](configuration-service.md)
Manages application configuration loading, validation, and persistence.

**Key Features**:
- Load from appsettings.json
- Runtime updates
- Configuration validation

---

### [Authentication](authentication.md)
Provides API credentials to LLM providers and services.

**Key Features**:
- API key retrieval
- Endpoint configuration
- Multiple auth modes (API key, OAuth)

---

### [Transparency Service](transparency-service.md)
Logs and stores transparency events for debugging and education.

**Key Features**:
- Event logging (LLM, tools, errors)
- In-memory storage
- Real-time event notifications

---

### [Serialization Service](serialization-service.md)
Standardized JSON serialization using System.Text.Json.

**Key Features**:
- Object to/from JSON
- CamelCase naming
- Pretty printing

---

## Related Documentation

- [LLM Providers](../llm/README.md) - LLM integration infrastructure
- [Tools](../tools/README.md) - Tool execution infrastructure
- [Configuration Guide](../../05-guides/deployment/configuration-guide.md)

---

**See Also**: [Component Overview](../README.md)
