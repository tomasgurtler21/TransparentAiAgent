# Streaming Utilities

**Last Updated**: 2025-11-08
**Status**: Active
**Phase**: Phase 2
**Layer**: Infrastructure

---

## Document Scope

**What belongs in this document**:
- Streaming response handling utilities
- MarkdownStreamingBuffer for incomplete chunk handling
- Streaming accumulation patterns

**What does NOT belong here**:
- ❌ Provider-specific streaming implementations → See provider docs
- ❌ LLM provider abstraction → See [provider-abstraction.md](provider-abstraction.md)
- ❌ UI rendering of streams → See [../ui/](../ui/)

---

## Overview

Streaming utilities handle partial content from LLM providers, buffering incomplete markdown constructs to prevent rendering glitches and ensuring smooth real-time display.

## Purpose

- **Buffer** incomplete markdown chunks during streaming
- **Prevent** rendering glitches from partial code blocks, tables, headings
- **Emit** only complete, renderable markdown units
- **Force flush** after timeout to prevent content hanging

## Components

### MarkdownStreamingBuffer

**File**: `TransparentAiAgentCore/Infrastructure/Streaming/MarkdownStreamingBuffer.cs:10`

Buffers streaming markdown content to prevent rendering glitches from incomplete constructs.

**Key Methods**:

```csharp
public string? AppendAndGetRenderable(string token)
public string Flush()
```

**Responsibilities**:
- Accumulate streaming tokens
- Detect incomplete markdown constructs
- Return content only when complete
- Force flush after 1 second timeout

### Incomplete Construct Detection

**Code Blocks**:
- Counts opening/closing fence markers (```)
- Holds content if odd number of fences (inside block)

**Table Rows**:
- Detects lines starting with `|`
- Holds if row doesn't end with `|` or newline

**Headings**:
- Detects lines starting with `#`
- Holds if no newline after heading text

**Timeout**:
- Force flushes after 1000ms to prevent indefinite buffering

## Usage

### Integration with LLM Providers

```csharp
var buffer = new MarkdownStreamingBuffer();

await foreach (var chunk in provider.StreamRequestAsync(request))
{
    var renderable = buffer.AppendAndGetRenderable(chunk.Content);
    if (renderable != null)
    {
        // Send to UI for rendering
        yield return renderable;
    }
}

// Flush remaining content
var final = buffer.Flush();
if (!string.IsNullOrEmpty(final))
{
    yield return final;
}
```

## Related Documentation

- [Provider Abstraction](provider-abstraction.md) - Streaming interface
- [Anthropic Provider](anthropic-provider.md) - Anthropic streaming
- [Azure OpenAI Provider](azure-openai-provider.md) - Azure streaming

---

**See Also**: [LLM Components Overview](README.md)
