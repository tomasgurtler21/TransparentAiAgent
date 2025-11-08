# Authentication

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 1-2
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- IAuthenticationProvider interface
- ConfigurationAuthenticationProvider implementation
- API key and endpoint retrieval

**What does NOT belong here**:
- ❌ OAuth setup → See `docs/06-reference/providers/azure-openai/authentication.md`
- ❌ Configuration → See `docs/05-guides/deployment/configuration-guide.md`

---

## Overview

Authentication components provide API credentials to LLM providers and other services.

## Components

### IAuthenticationProvider
**File**: `Domain/Authentication/IAuthenticationProvider.cs`

Domain interface for authentication.

**Methods**:
- `GetApiKey(serviceName)` - Retrieve API key
- `GetEndpoint(serviceName)` - Retrieve endpoint URL

### ConfigurationAuthenticationProvider
**File**: `Infrastructure/Authentication/ConfigurationAuthenticationProvider.cs`

Retrieves credentials from AppConfiguration.

**Implementation**: Reads from LLM.Anthropic.ApiKey, LLM.AzureOpenAI.ApiKey, etc.

## Usage

LLM providers use IAuthenticationProvider to get credentials at initialization.

## Related Documentation

- [Azure OpenAI Auth](../../06-reference/providers/azure-openai/authentication.md)
- [Configuration Guide](../../05-guides/deployment/configuration-guide.md)

---

**See Also**: [Infrastructure Overview](README.md)
