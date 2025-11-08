# Anthropic API & SDK Documentation

**Last Updated:** January 2025
**Purpose:** Comprehensive reference for Anthropic Claude integration with C# .NET 8.0

---

## 🎯 Quick Start

**New to Anthropic integration?** Start here:

1. [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md) - Understand the API
2. [04_CSHARP_SDK_REFERENCE.md](04_CSHARP_SDK_REFERENCE.md) - Learn the C# SDK
3. [07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md) - Apply fixes to your code

**Implementing tool calling?**
- [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md)

**Implementing streaming?**
- [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md) ⚠️ **Critical Document**

**Debugging issues?**
- [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md)

---

## 📚 Complete Documentation

### Core API Documentation

#### [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md)
**Complete API specification covering:**
- Request/response structures
- All parameters (required and optional)
- Content block types
- Stop reasons
- Error responses
- Rate limits
- Token counting
- Context windows

**When to read:** Before starting implementation or when you need API details.

---

#### [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md)
**Everything about tool calling (function calling):**
- Tool definition format
- Tool use response structure
- Tool result format
- Multi-turn tool calling flows
- Tool choice control
- Parallel tool use
- Error handling
- Best practices
- Edge cases

**When to read:** Implementing or debugging tool calls.

---

#### [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md)
**⚠️ CRITICAL - Complete streaming implementation guide:**
- All event types and sequences
- Streaming with tool calls
- Fine-grained tool streaming (beta)
- Accumulation strategies
- Error handling
- Common bugs and solutions
- Complete C# implementation examples

**When to read:** Implementing streaming (especially with tools). This is the "insanely tough to debug" part made clear.

---

### SDK & Models Documentation

#### [04_CSHARP_SDK_REFERENCE.md](04_CSHARP_SDK_REFERENCE.md)
**Official Anthropic C# SDK reference:**
- Installation (local clone method)
- Client configuration
- Key classes and types
- Non-streaming requests
- Streaming requests
- Event type checking
- Exception hierarchy

**When to read:** When writing C# code using the SDK.

---

#### [05_HAIKU_VS_SONNET_COMPARISON.md](05_HAIKU_VS_SONNET_COMPARISON.md)
**Claude Haiku 4.5 vs Sonnet 4.5:**
- Model identifiers
- Capabilities comparison
- Performance characteristics
- Pricing
- Use case recommendations
- Multi-agent patterns

**When to read:** Choosing which model to use.

---

### Troubleshooting Documentation

#### [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md)
**Troubleshooting guide covering:**
- Tool calling issues
- Streaming issues
- Error message explanations
- Debugging techniques
- Common mistakes
- Testing strategies

**When to read:** When things don't work as expected.

---

#### [07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md)
**Specific fixes for your `AnthropicProvider.cs`:**
- Critical issues identified
- Complete fix for `StreamRequestAsync`
- Fix for usage information extraction
- Model identifier updates
- Test cases
- Verification checklist

**When to read:** NOW - apply these fixes to your code.

---

## 🔥 Most Important for Your Situation

Based on your requirements (streaming + tool calls working properly):

### Priority 1: Fix Your Code
1. **[07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md)** - Apply all fixes NOW
   - Tool calls lost in streaming (CRITICAL)
   - stop_reason hardcoded (CRITICAL)
   - Usage information missing

### Priority 2: Understand Streaming
2. **[03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md)** - Complete reference
   - All event types
   - Tool call accumulation
   - Common bugs (you had all of them!)

### Priority 3: Master Tool Calling
3. **[02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md)** - How tools work
   - Tool definitions
   - Multi-turn flows
   - Best practices

### Priority 4: Debugging
4. **[06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md)** - When stuck
   - Logging techniques
   - Common mistakes
   - Test scenarios

---

## 🎓 Learning Path

### Beginner (Just Starting)
1. Read: [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md)
2. Read: [04_CSHARP_SDK_REFERENCE.md](04_CSHARP_SDK_REFERENCE.md)
3. Read: [05_HAIKU_VS_SONNET_COMPARISON.md](05_HAIKU_VS_SONNET_COMPARISON.md)
4. Apply: [07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md)

### Intermediate (Implementing Features)
1. For Tools: [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md)
2. For Streaming: [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md)
3. Reference: [04_CSHARP_SDK_REFERENCE.md](04_CSHARP_SDK_REFERENCE.md)

### Advanced (Debugging & Optimization)
1. Debug: [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md)
2. Optimize: [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md) (rate limits, caching)
3. Fine-tune: [05_HAIKU_VS_SONNET_COMPARISON.md](05_HAIKU_VS_SONNET_COMPARISON.md)

---

## 🔍 Quick Reference by Topic

### API Basics
- **Endpoint:** https://api.anthropic.com/v1/messages
- **Models:** Sonnet 4.5 (`claude-sonnet-4-5-20250929`), Haiku 4.5 (`claude-haiku-4-5-20251001`)
- **Context:** 200K tokens (1M for Sonnet 4.5 with beta)
- **Max Output:** 64K tokens

### Authentication
- **Header:** `x-api-key: sk-ant-...`
- **Version:** `anthropic-version: 2023-06-01`
- **Environment:** `ANTHROPIC_API_KEY`

### Rate Limits (Tier 1)
- **Sonnet:** 50 RPM, 30K ITPM, 8K OTPM
- **Haiku:** 50 RPM, 50K ITPM, 10K OTPM

### Common Stop Reasons
- `end_turn` - Normal completion
- **`tool_use`** - **Model wants to call tools (MUST handle!)**
- `max_tokens` - Hit token limit
- `stop_sequence` - Custom sequence matched

---

## 💡 Key Insights

### Streaming is Different
- Tool calls are **NOT** in separate field
- Tool calls come as **JSON string fragments** (`input_json_delta`)
- Must accumulate JSON **per content block index**
- `stop_reason` comes in **`message_delta`**, not `message_stop`

### Tool Calling is Content-Based
- Tool calls are **content blocks**, not separate arrays
- Tool results go in **user messages**, not tool messages
- Must include **ALL tool results** in single message
- Tool results must come **BEFORE** any text content

### Models Matter
- **Sonnet 4.5:** Accuracy, complex reasoning, 1M context
- **Haiku 4.5:** Speed (3× faster), cost (1/3 price), scale

---

## 🚨 Critical Warnings

### DO NOT:
- ❌ Use `oneOf`, `allOf`, `anyOf` at top level of tool schemas
- ❌ Mix JSON from different content block indices
- ❌ Hardcode `stop_reason` instead of capturing from `message_delta`
- ❌ Finalize streaming response before `message_stop`
- ❌ Ignore `stop_reason == "tool_use"` (conversation incomplete!)

### DO:
- ✅ Use versioned model IDs in production
- ✅ Implement exponential backoff for rate limits
- ✅ Validate tool call JSON after accumulation
- ✅ Handle all streaming event types
- ✅ Log event sequences for debugging

---

## 📞 Getting Help

### Internal Resources
- This folder's documentation (you're reading it!)
- [07_IMPLEMENTATION_FIXES.md](07_IMPLEMENTATION_FIXES.md) - For code issues
- [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md) - For debugging

### External Resources
- **Official API Docs:** https://docs.anthropic.com/
- **C# SDK GitHub:** https://github.com/anthropics/anthropic-sdk-csharp
- **API Console:** https://console.anthropic.com/

---

## ✅ Quick Checklist

**Before implementing:**
- [ ] Read [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md)
- [ ] Choose model: [05_HAIKU_VS_SONNET_COMPARISON.md](05_HAIKU_VS_SONNET_COMPARISON.md)
- [ ] Review SDK: [04_CSHARP_SDK_REFERENCE.md](04_CSHARP_SDK_REFERENCE.md)

**For tool calling:**
- [ ] Read [02_TOOL_CALLING_DEEP_DIVE.md](02_TOOL_CALLING_DEEP_DIVE.md)
- [ ] Test tool definitions
- [ ] Handle multi-turn flows

**For streaming:**
- [ ] Read [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md) ⚠️
- [ ] Implement all event handlers
- [ ] Test with tool calls
- [ ] Verify JSON accumulation

**When debugging:**
- [ ] Check [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md)
- [ ] Enable verbose logging
- [ ] Track event sequences
- [ ] Verify accumulation

---

## 🎉 You're Ready!

With this documentation, you have:
- ✅ Complete API specification
- ✅ Every detail about tool calling
- ✅ Full streaming implementation guide
- ✅ C# SDK reference
- ✅ Model comparison
- ✅ Debugging techniques
- ✅ Specific fixes for your code

**No more endless researching in future sessions!**

---

**Created:** January 2025
**Purpose:** Never need to research Anthropic API/SDK again
**Status:** Complete and comprehensive
