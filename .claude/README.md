# Claude Code Workspace Configuration

This directory contains configuration and context for Claude Code to work effectively with the TransparentAiAgent project.

## Project Context

This is a transparent AI agent framework built in C# with the following key characteristics:
- **Transparency**: Every decision, action, and reasoning step should be visible and auditable
- **Configurability**: Highly flexible configuration system
- **LLM Agnostic**: Should work with any LLM provider
- **Template-based**: Designed to serve as a template for building custom agents

## Project Structure

- `TransparentAiAgentCore/` - Core agent logic, abstractions, and business logic
- `TransparentAiAgentGui/` - Blazor-based GUI for agent interaction and monitoring
- `TransparentAiAgentCore_Tests/` - Unit and integration tests following TDD approach
- `docs/` - Project documentation including architecture and design decisions

## Development Approach

- **TDD (Test-Driven Development)**: Write tests first, then implement features
- **Clean Architecture**: Clear separation of concerns
- **SOLID Principles**: Maintainable and extensible design

## Working with Claude Code

When working on this project, Claude Code has access to:
- MCP Servers: context7 (documentation lookup), todo-list (task management)
- Full workspace access for reading/writing code
- Git operations for version control

Key documents to reference:
- `/docs/REQUIREMENTS.md` - User requirements and decisions
- `/docs/ARCHITECTURE.md` - High-level architecture and design patterns
- `/docs/DESIGN_DECISIONS.md` - Detailed design decisions and rationale
