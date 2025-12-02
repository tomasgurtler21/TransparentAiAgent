# Installation Guides

**Last Updated**: 2025-12-02
**Status**: Active

---

## 📋 Document Scope

**What belongs in this directory**:
- Step-by-step installation instructions for end users
- Configuration guides and setup procedures
- Troubleshooting for runtime issues
- User data management
- Upgrading and uninstallation procedures

**What does NOT belong here**:
- ❌ Building/packaging the application (→ belongs in 05-guides/development/)
- ❌ Architecture explanations (→ belongs in 02-architecture/)
- ❌ Component details (→ belongs in 04-components/)
- ❌ Development guides (→ belongs in 05-guides/development/)

---

## Guides for End Users

### Getting Started

#### [installation-guide.md](installation-guide.md)
**Complete installation guide** for end users:
- System requirements (no .NET installation needed!)
- Download and extract
- Configure API keys (Anthropic, OpenAI)
- First run and verification
- Troubleshooting installation issues
- Upgrading and uninstallation

**Start here** if you're installing TransparentAiAgent for the first time.

---

### Configuration

#### [llm-provider-selector.md](llm-provider-selector.md)
**✅ Current** - Multi-provider LLM configuration:
- Configuring multiple LLM providers
- Switching providers via UI
- Parameter inheritance and overrides
- Azure OpenAI OAuth authentication
- Reasoning models configuration
- Troubleshooting provider configuration

#### [configuration-guide.md](configuration-guide.md)
**⚠️ Archived** - Legacy single-provider configuration guide.

**Use `llm-provider-selector.md` instead** for current multi-provider configuration.

---

### Data Management

#### [data-storage.md](data-storage.md)
**User data storage and management**:
- Data storage locations (AppData on Windows)
- User settings vs application configuration
- Finding and backing up your data
- Migrating between installations
- Resetting user data
- Privacy and security considerations

---

### Troubleshooting

#### [troubleshooting.md](troubleshooting.md)
**Troubleshooting runtime issues** for installed applications:
- Quick reference table for common issues
- Connection problems (automatic port selection)
- API authentication and MCP server issues
- Memory usage and performance
- Data storage and persistence
- Browser-based debugging tips
- Security best practices

---

## Quick Start Path

**New users should follow this order**:

1. **[installation-guide.md](installation-guide.md)** - Install the application
2. **[llm-provider-selector.md](llm-provider-selector.md)** - Configure your LLM provider
3. **[data-storage.md](data-storage.md)** - Understand where your data is stored
4. **[troubleshooting.md](troubleshooting.md)** - If you encounter any issues

---

## Related Documentation

- [Development Guides](../development/README.md) - For developers building/modifying the app
- [Feature Guides](../features/README.md) - How to use specific features
- [Architecture Overview](../../02-architecture/overview.md) - How the system works
- [Requirements](../../07-planning/requirements.md) - What this project is about

---
