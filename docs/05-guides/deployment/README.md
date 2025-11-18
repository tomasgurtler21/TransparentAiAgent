# Deployment Guides

**Last Updated**: 2025-11-18
**Status**: Active

---

## 📋 Document Scope

**What belongs in this directory**:
- Step-by-step deployment instructions
- Configuration guides and setup procedures
- Troubleshooting for deployment issues
- Local and production deployment guides
- Building and packaging instructions

**What does NOT belong here**:
- ❌ Architecture explanations (→ belongs in 02-architecture/)
- ❌ Component details (→ belongs in 04-components/)
- ❌ Development guides (→ belongs in 05-guides/development/)

---

## Guides

### For Developers: Building and Deploying

#### [building-for-deployment.md](building-for-deployment.md)
How to build the application for deployment on Windows:
- Using Visual Studio Publish
- Command line alternative
- Self-contained deployment (includes .NET runtime)
- Creating distribution ZIP files
- Build troubleshooting

#### [folder-structure.md](folder-structure.md)
Understanding the deployment folder structure:
- Complete directory layout
- File and folder purposes
- Data folder organization (`conversations/`, `memory/`, `knowledge/`, `scenarios/`)
- Backup recommendations
- Disk space management
- Portable installation

### For End Users: Installation and Configuration

#### [installation-guide.md](installation-guide.md)
Step-by-step installation guide for end users:
- System requirements (no .NET installation needed!)
- Download and extract
- Configure API keys (Anthropic, OpenAI)
- First run and verification
- Troubleshooting installation issues
- Upgrading and uninstallation

#### [configuration-guide.md](configuration-guide.md)
**⚠️ Archived** - Legacy single-provider configuration guide.
Use [llm-provider-selector.md](llm-provider-selector.md) instead for current multi-provider configuration.

#### [llm-provider-selector.md](llm-provider-selector.md)
**✅ Current** - Multi-provider LLM configuration:
- Configuring multiple LLM providers
- Switching providers via UI
- Parameter inheritance and overrides
- Azure OpenAI OAuth authentication
- Reasoning models configuration
- Troubleshooting provider configuration

#### [data-storage.md](data-storage.md)
**✅ New** - User data storage and management:
- Data storage locations (AppData)
- User settings vs application configuration
- Finding and backing up your data
- Migrating between installations
- Resetting user data
- Privacy and security considerations

### Troubleshooting

#### [troubleshooting.md](troubleshooting.md)
Troubleshooting deployment and runtime issues:
- Common errors and solutions
- Debugging techniques
- Log analysis
- Performance issues

---

## Quick Links

- [Architecture Overview](../../02-architecture/overview.md)
- [Requirements](../../07-planning/requirements.md)

---
