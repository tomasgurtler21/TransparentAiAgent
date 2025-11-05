# Claude Haiku 4.5 vs Sonnet 4.5 Comparison

**Last Updated:** January 2025

---

## Model Identifiers

### Sonnet 4.5
- **API ID (Use in Production):** `claude-sonnet-4-5-20250929`
- **API Alias:** `claude-sonnet-4-5`
- **AWS Bedrock ID:** `anthropic.claude-sonnet-4-5-20250929-v1:0`
- **GCP Vertex AI ID:** `claude-sonnet-4-5@20250929`

### Haiku 4.5
- **API ID (Use in Production):** `claude-haiku-4-5-20251001`
- **API Alias:** `claude-haiku-4-5`
- **AWS Bedrock ID:** `anthropic.claude-haiku-4-5-20251001-v1:0`
- **GCP Vertex AI ID:** `claude-haiku-4-5@20251001`

**Important:** Always use versioned IDs in production for consistent behavior.

---

## Quick Comparison

| Feature | Sonnet 4.5 | Haiku 4.5 |
|---------|------------|-----------|
| **Speed** | Moderate | **3× faster** |
| **Latency (Small Prompts)** | Fast | **Sub-200ms** |
| **Accuracy** | **Highest** | Near-Sonnet |
| **Cost** | Baseline | **1/3 of Sonnet** |
| **Context Window** | 200K (1M beta) | 200K |
| **Max Output** | 64K tokens | 64K tokens |
| **Tool Calling** | ✅ Yes | ✅ Yes |
| **Extended Thinking** | ✅ Yes | ✅ Yes |
| **Best For** | Complex reasoning, accuracy | Speed, cost, scale |

---

## Capabilities

Both models support:
- ✅ Tool calling (all features)
- ✅ Extended thinking
- ✅ Computer use
- ✅ Context awareness
- ✅ Vision (image processing)
- ✅ Multilingual support
- ✅ Fine-grained tool streaming (beta)
- ✅ Priority tier support

**Sonnet 4.5 Only:**
- ✅ 1M context window (beta with `context-1m-2025-08-07` header)

---

## Performance

### Speed
- **Haiku 4.5:** Up to 3× faster than Sonnet 4.5
- **Haiku 4.5:** Sub-200ms response time for small prompts
- **Haiku 4.5:** Fastest latency in Claude family

### Accuracy
- **Sonnet 4.5:** Stronger reasoning, coding, analytical depth
- **Sonnet 4.5:** SWE-bench Verified: 77.2%
- **Haiku 4.5:** SWE-bench Verified: 73.3%
- **Haiku 4.5:** Near-frontier intelligence at 1/3 cost

### Quality Trade-offs
- **Sonnet 4.5:** Top-tier in reasoning, coding, multilingual, long-context, image processing
- **Haiku 4.5:** Similar coding performance to Sonnet 4 but 2× faster and 1/3 cost

---

## Pricing

| Model | Input (per 1M tokens) | Output (per 1M tokens) |
|-------|----------------------|------------------------|
| **Sonnet 4.5** | $3 | $15 |
| **Haiku 4.5** | $1 | $5 |

**Cost Ratio:** Haiku 4.5 is 1/3 the cost of Sonnet 4.5

---

## Context Windows

| Model | Standard | Extended (Beta) |
|-------|----------|-----------------|
| **Sonnet 4.5** | 200K tokens | 1M tokens |
| **Haiku 4.5** | 200K tokens | N/A |

---

## Knowledge Cutoff

| Model | Reliable Cutoff | Training Data Cutoff |
|-------|----------------|----------------------|
| **Sonnet 4.5** | January 2025 | July 2025 |
| **Haiku 4.5** | February 2025 | July 2025 |

---

## Use Case Recommendations

### Choose Sonnet 4.5 When:
- ✅ Complex reasoning required
- ✅ Agentic precision needed
- ✅ Long-context reasoning (especially 1M context)
- ✅ Multi-step planning and orchestration
- ✅ High-stakes coding tasks
- ✅ Maximum accuracy is priority

### Choose Haiku 4.5 When:
- ✅ Speed is critical
- ✅ Cost optimization important
- ✅ Large-scale deployments
- ✅ Developer tools needing low latency
- ✅ Real-time applications
- ✅ Sub-200ms response time needed
- ✅ Straightforward tool use
- ✅ Parallel task execution

---

## Multi-Agent Pattern (Recommended)

**Best of Both Worlds:**

```
Sonnet 4.5 (Orchestrator)
   ↓
   Breaks down complex problem into multi-step plan
   ↓
   ├── Haiku 4.5 Worker 1: Analyze codebase (parallel)
   ├── Haiku 4.5 Worker 2: Generate tests (parallel)
   └── Haiku 4.5 Worker 3: Refactor code (parallel)
   ↓
Sonnet 4.5 (Synthesizer)
   ↓
   Combines results and provides recommendations
```

**Benefits:**
- Intelligent planning (Sonnet)
- Fast execution (Haiku workers)
- Cost-effective at scale

---

## Tool Calling

**No differences in tool calling support:**
- Both support all tool calling features
- Both support parallel tool use
- Both support fine-grained tool streaming

**Performance Notes:**
- Straightforward tools: Haiku works great
- Complex multi-tool scenarios requiring reasoning: Sonnet recommended
- Anthropic recommends Sonnet 4.5 or Opus 4.1 for reliability across complex scenarios

---

## Streaming

**No differences in streaming behavior:**
- Both follow same event sequence
- Both emit same event types
- Both support fine-grained tool streaming beta

**Performance Differences:**
- Haiku 4.5: Faster first token (sub-200ms for small prompts)
- Sonnet 4.5: Longer time-to-first-token but comparable once started

---

## Safety Classification

| Model | ASL Level | Description |
|-------|-----------|-------------|
| **Sonnet 4.5** | ASL-3 | More restrictive safety controls |
| **Haiku 4.5** | ASL-2 | Less restrictive safety controls |

**Implication:** Haiku 4.5 may be suitable for wider range of use cases without triggering safety guardrails.

---

## Rate Limits (Tier 1 Example)

| Model | RPM | ITPM | OTPM |
|-------|-----|------|------|
| **Sonnet 4.x** | 50 | 30,000 | 8,000 |
| **Haiku 4.5** | 50 | 50,000 | 10,000 |

**Note:** Haiku has 67% higher input token limit.

---

## Decision Matrix

| Scenario | Recommended Model |
|----------|------------------|
| Complex multi-step reasoning | Sonnet 4.5 |
| Real-time chat application | Haiku 4.5 |
| Code generation for production | Sonnet 4.5 |
| Rapid prototyping | Haiku 4.5 |
| Long document analysis | Sonnet 4.5 (with 1M context) |
| High-volume API calls | Haiku 4.5 |
| Agentic workflows (orchestrator) | Sonnet 4.5 |
| Agentic workflows (workers) | Haiku 4.5 |
| Critical accuracy needed | Sonnet 4.5 |
| Budget constraints | Haiku 4.5 |

---

## Configuration Examples

### Sonnet 4.5
```json
{
  "LLM": {
    "Provider": "Anthropic",
    "Anthropic": {
      "Model": "claude-sonnet-4-5-20250929",
      "ApiKey": "sk-ant-..."
    },
    "Temperature": 0.7,
    "MaxTokens": 4096
  }
}
```

### Haiku 4.5
```json
{
  "LLM": {
    "Provider": "Anthropic",
    "Anthropic": {
      "Model": "claude-haiku-4-5-20251001",
      "ApiKey": "sk-ant-..."
    },
    "Temperature": 0.7,
    "MaxTokens": 4096
  }
}
```

---

## Additional Resources

- [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md)
- [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md)
- [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md)
