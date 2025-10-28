# LLM Integration Components

This directory contains documentation for LLM provider integration components.

## Components

### Provider Abstraction
**File**: `ProviderAbstraction.md` (to be created during implementation)

Core abstraction for LLM providers.

**Key Responsibilities**:
- Define `ILLMProvider` interface
- Standardize LLM interactions
- Support streaming responses
- Handle tool calling

---

### Azure OpenAI Provider
**File**: `AzureOpenAIProvider.md` (to be created during implementation)

Azure OpenAI implementation.

**Key Responsibilities**:
- Implement Azure OpenAI API calls
- Handle Azure-specific authentication
- Support streaming
- Map Azure responses to common format

---

### Anthropic Provider
**File**: `AnthropicProvider.md` (to be created during implementation)

Anthropic Claude implementation.

**Key Responsibilities**:
- Implement Anthropic API calls
- Handle Anthropic-specific authentication
- Support streaming
- Map Anthropic responses to common format

---

### Streaming Handler
**File**: `StreamingHandler.md` (to be created during implementation)

Manages streaming from LLM providers to UI.

**Key Responsibilities**:
- Handle `IAsyncEnumerable<T>` from providers
- Buffer and format stream chunks
- Coordinate with Transparency System
- Manage SSE connections to UI

**Note**: This is a known pain point - special attention needed

---

### Token Counter
**File**: `TokenCounter.md` (to be created during implementation)

Tracks token usage and costs (low priority).

**Key Responsibilities**:
- Count tokens in requests/responses
- Track costs per provider
- Provide usage statistics

---

**Status**: Structure defined - detailed docs to be created during implementation
**Last Updated**: 2025-10-28
