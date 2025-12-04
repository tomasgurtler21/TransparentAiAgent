# Long-Term Memory

**Last Updated**: 2025-11-16
**Status**: Implemented
**Audience**: All

---

## 📋 Document Scope

**What belongs in this document**:
- Explanation of the long-term memory concept
- High-level philosophy and principles
- How long-term memory works across the application
- Use cases and examples
- Best practices and privacy considerations
- Related documentation links

**What does NOT belong here**:
- ❌ Component-specific implementation details (→ belongs in [Long-Term Memory Service](../04-components/infrastructure/long-term-memory-service.md))
- ❌ Step-by-step how-to guides (→ belongs in [Using Long-Term Memory](../05-guides/features/using-long-term-memory.md))
- ❌ Code snippets (→ belongs in component docs)
- ❌ Configuration details (→ belongs in [Configuration Guide](../05-guides/installation/llm-provider-selector.md))

---

## What is Long-Term Memory?

Long-term memory is a feature that enables the AI agent to **remember information about you across conversations**. Unlike regular conversation history which is cleared when you start a new session, long-term memory persists important context, preferences, and background information that helps the agent provide more personalized and contextual assistance.

Think of it as the agent's notebook where it writes down important things about you - your preferences, work context, goals, and other relevant information that makes future interactions more helpful.

## Why is this Important?

Without long-term memory, every conversation starts from scratch. The agent doesn't remember:
- Your name or background
- Your skill level or learning preferences
- Project context you've discussed before
- Common tasks or workflows you use
- Preferences you've stated in previous sessions

With long-term memory enabled, the agent can:
- **Personalize responses** based on your experience level and preferences
- **Skip repetitive explanations** for concepts you already understand
- **Maintain context** about your projects and goals across sessions
- **Provide continuity** in long-term learning or project work
- **Save time** by not having to re-introduce yourself each session

## How it Works

### Memory Storage

Long-term memory is stored as **markdown files** on your local system in the `data/memory/` directory. Each mode (Normal, Teaching) has its own separate memory file:

- `memory-normal.md` - Memories from Normal mode conversations
- `memory-teaching.md` - Memories from Teaching mode conversations

This separation ensures that different contexts don't mix - for example, code you're learning in Teaching mode stays separate from work projects in Normal mode.

### Key Principles

1. **User Control**: You have complete control over memory - you can view, edit, or clear it at any time
2. **Transparency**: Memory is stored as readable markdown files, never hidden or opaque
3. **Mode Isolation**: Each mode maintains separate memories to avoid context mixing
4. **Size Limits**: Memory is limited to 10,000 characters to keep it focused and manageable
5. **Privacy First**: The agent follows guardrails to avoid storing sensitive information
6. **Local Storage**: All memory files remain on your local system, never sent elsewhere

### Memory Lifecycle

#### 1. Enabling Memory

When you enable long-term memory (via checkbox in the UI):
- The agent gains access to two tools: `long_term_memory_read` and `long_term_memory_update`
- If memory exists for the current mode, it's automatically loaded into the conversation
- The agent can now reference past context from previous sessions

#### 2. During Conversation

As you chat with the agent:
- The agent can read current memory at any time
- The agent can update memory when it learns something important about you
- Updates require a **reason** (the agent must explain why it's updating memory)
- All memory operations are logged to the transparency system

#### 3. Ending Conversation

When you click "End Conversation":
- The agent is prompted to review the conversation
- If the agent learned anything important, it updates memory
- This happens automatically (configurable timeout: 30 seconds)
- You can then start a new conversation with updated context

#### 4. Mode Switching

When you switch between Normal and Teaching modes:
- The current mode's memory is unloaded
- The new mode's memory is loaded
- This ensures context isolation between different activities

## Use Cases

### Use Case 1: Learning Journey

**Scenario**: You're learning C# and .NET development over several weeks.

**Without Memory**:
```
Week 1: "I'm new to C#, can you explain async/await?"
Week 2: "I'm new to C#, can you explain LINQ?"
Week 3: "I'm new to C#, can you explain dependency injection?"
[Agent repeats basic explanations each time]
```

**With Memory**:
```
Week 1: "I'm new to C#, can you explain async/await?"
[Agent learns: User is beginner, learning C#]

Week 2: "Can you explain LINQ?"
[Agent remembers: "Based on what we covered last week about async/await,
LINQ is another powerful C# feature..."]

Week 3: "Can you explain dependency injection?"
[Agent remembers: "You've been learning C# for a few weeks now.
DI builds on concepts like interfaces that you're familiar with..."]
```

### Use Case 2: Project Context

**Scenario**: You're working on a Blazor application over multiple sessions.

**With Memory**:
- Agent remembers your project structure and architecture
- Knows which patterns and libraries you're using
- Recalls previous decisions and their rationale
- Provides consistent guidance aligned with your project

### Use Case 3: Personal Preferences

**Scenario**: You prefer detailed explanations with examples.

**With Memory**:
- Agent remembers your learning style
- Consistently provides code examples
- Adjusts verbosity to match your preference
- Doesn't ask about preferences repeatedly

## Implementation

### Components Involved

The long-term memory feature involves several components:

1. **[LongTermMemoryService](../04-components/infrastructure/long-term-memory-service.md)** - Core service managing file I/O
2. **Memory Tools** - Two built-in tools for reading and updating memory
3. **Tool Executor** - Executes memory tool operations
4. **ConversationUIService** - Coordinates memory lifecycle with conversations
5. **UI Components** - Checkbox, viewer overlay, memory management

See component documentation for technical details.

### Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│ User enables memory                                         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ ConversationUIService loads memory from file                │
│ (if exists) and injects into system prompt                  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Conversation proceeds with memory context                   │
│                                                              │
│ Agent can:                                                   │
│  • long_term_memory_read → Read current memory              │
│  • long_term_memory_update → Update memory                  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ User ends conversation                                      │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Agent prompted to review and update memory                  │
│ (if anything important was learned)                         │
└─────────────────────────────────────────────────────────────┘
```

## Best Practices

### For Users

**DO**:
- ✅ Review memory periodically to ensure accuracy
- ✅ Clear sensitive information if accidentally stored
- ✅ Use different modes for different contexts (work vs learning)
- ✅ Enable memory for long-term projects or learning journeys
- ✅ Trust the transparency logs to see what's being stored

**DON'T**:
- ❌ Store passwords, API keys, or secrets in memory (agent should refuse)
- ❌ Mix unrelated contexts - use mode separation
- ❌ Let memory grow too large - keep it focused on what's important

### For the Agent

**DO**:
- ✅ Store user preferences, background, and context
- ✅ Update memory when learning something important
- ✅ Provide reasons when updating memory
- ✅ Keep memory concise and focused
- ✅ Use markdown formatting for readability

**DON'T**:
- ❌ Store sensitive information (passwords, keys, personal IDs, health data)
- ❌ Store temporary conversation details
- ❌ Duplicate information that's obvious or common
- ❌ Exceed the 10,000 character limit

## Privacy Considerations

### What's Safe to Store

**Generally Safe**:
- Name, job role, skill level
- Project context and goals
- Learning preferences
- Technical stack and tools you use
- General background and interests

**Context-Dependent**:
- Company/organization name (public knowledge usually OK)
- Project names (generic OK, confidential requires caution)
- Code patterns and architectures (non-proprietary)

### What Should NEVER Be Stored

**Absolutely Never**:
- Passwords or passphrases
- API keys or secrets
- Personal identification numbers (SSN, driver's license, etc.)
- Credit card or banking information
- Health information
- Private keys or certificates
- Confidential business data

### Agent Guardrails

The long-term memory tools include explicit guardrails in their descriptions:

```
GUARDRAILS:
- NEVER store sensitive information (passwords, API keys, personal IDs, health data)
- NEVER store confidential or proprietary information
- NEVER store data the user hasn't explicitly shared
- Focus on: preferences, context, learning progress, technical background
```

The agent is instructed to follow these guardrails strictly.

### User Safety Nets

Even with guardrails, you have multiple safety nets:

1. **Transparency Logging**: All memory updates are logged with reasons
2. **View Memory**: Click "View" to see exactly what's stored
3. **Edit Memory**: Manually edit to remove anything concerning
4. **Clear Memory**: One-click to delete all memory for a mode
5. **Local Storage**: Memory never leaves your machine

## Common Pitfalls

### Pitfall 1: Treating Memory as a Database

**Problem**: Trying to store every detail of every conversation.

**Solution**: Memory should store **context and preferences**, not conversation transcripts. Keep it high-level and focused on what helps future conversations.

**Example**:
- ❌ Bad: "On Nov 12 at 3:45pm user asked about async/await and I explained tasks and continuations..."
- ✅ Good: "User learning C# async programming, understands async/await basics"

### Pitfall 2: Mixing Contexts

**Problem**: Storing unrelated information in the same memory (work + personal projects).

**Solution**: Use mode separation. Normal mode for work, Teaching mode for learning. Each maintains separate memory.

### Pitfall 3: Over-Relying on Memory

**Problem**: Assuming the agent remembers everything perfectly.

**Solution**: Memory has limits (10,000 chars). The agent may need to summarize or forget older information. If something critical isn't remembered, re-state it.

### Pitfall 4: Storing Sensitive Data

**Problem**: Accidentally including passwords or keys in conversation, which get stored.

**Solution**: The agent should refuse to store sensitive data, but as a safety net, regularly review memory and clear anything concerning.

## Configuration

Long-term memory can be configured in `appsettings.json`:

```json
{
  "LongTermMemory": {
    "Enabled": false,
    "StorageDirectory": "data/memory",
    "MaxCharacters": 10000,
    "AutoLoadOnStart": true,
    "PromptUpdateOnEnd": true,
    "UpdatePromptTimeoutSeconds": 30
  }
}
```

See [Configuration Guide](../05-guides/installation/llm-provider-selector.md) for details.

## Limitations

### Current Limitations

1. **No Hot Reload**: Editing memory files manually requires app restart to take effect
2. **No Versioning**: No automatic versioning or history of memory changes
3. **No Sharing**: Memory cannot be shared between users or synced across devices
4. **No Search**: No built-in search within memory files
5. **No Smart Summarization**: Agent must manually summarize if approaching size limit

### Known Issues

- Mode switching delay: Brief moment before new mode's memory is loaded
- Large memory files (>5000 chars) may slow conversation startup slightly

### Future Enhancements

Potential future improvements:
- **Automatic Summarization**: Agent auto-summarizes when approaching limits
- **Memory Versioning**: Track changes over time
- **Memory Search**: Built-in search across memory content
- **Memory Sharing**: Export/import memory for backup or sharing
- **Smart Reminders**: Agent proactively reminds you of past context

## Benefits

### For Users

- **Continuity**: Seamless experience across multiple sessions
- **Personalization**: Responses tailored to your level and preferences
- **Time Savings**: No need to re-introduce yourself or re-explain context
- **Better Learning**: Agent tracks your progress and adapts teaching
- **Context Retention**: Long-term projects maintain continuity

### For Development

- **Simple Implementation**: File-based storage, no database required
- **Transparent**: Markdown files are human-readable and version-controllable
- **Debuggable**: Easy to inspect and modify memory files
- **Testable**: File-based approach simplifies testing

## Success Metrics

Long-term memory is successful when:

1. ✅ Users report more personalized interactions
2. ✅ Conversations build on previous context naturally
3. ✅ Users don't need to repeat background information
4. ✅ Learning journeys show clear progression
5. ✅ No sensitive data is stored (verified through transparency logs)
6. ✅ Memory files remain manageable in size
7. ✅ Users trust and regularly review their memory

## Related Documentation

- **Component**: [Long-Term Memory Service](../04-components/infrastructure/long-term-memory-service.md) - Technical implementation
- **Tools**: [Long-Term Memory Tools](../04-components/tools/builtin/long-term-memory-tools.md) - Built-in tools
- **Guide**: [Using Long-Term Memory](../05-guides/features/using-long-term-memory.md) - Step-by-step usage guide
- **Configuration**: [Configuration Guide](../05-guides/installation/llm-provider-selector.md#long-term-memory) - Setup and configuration
- **Design**: `LONG_TERM_MEMORY_DESIGN.md` (project root) - Design decisions
- **Implementation**: `LONG_TERM_MEMORY_IMPLEMENTATION_PLAN.md` (project root) - Implementation details

---

**Remember**: Long-term memory is a tool to enhance continuity and personalization. You have complete control and transparency over what's stored. Review it regularly and clear anything that doesn't serve your needs. 🧠
