# Using Long-Term Memory

**Last Updated**: 2025-11-16
**Audience**: Users
**Difficulty**: Beginner

---

## 📋 Document Scope

**What belongs in this document**:
- Step-by-step guide for using long-term memory
- How to enable, view, edit, and clear memory
- Best practices for memory usage
- Common scenarios and examples
- Troubleshooting tips

**What does NOT belong here**:
- ❌ Technical implementation details (→ [Long-Term Memory Service](../../04-components/infrastructure/long-term-memory-service.md))
- ❌ Conceptual overview (→ [Long-Term Memory Concept](../../03-concepts/long-term-memory.md))
- ❌ Configuration details (→ [Configuration Guide](../deployment/configuration-guide.md))

---

## Overview

Long-term memory allows the AI agent to remember information about you across conversation sessions. This guide shows you how to use this feature effectively.

**What you'll learn**:
- How to enable and disable long-term memory
- How to view and edit your memory
- What information to store (and what to avoid)
- How to manage memory effectively

**Prerequisites**:
- Long-term memory feature must be enabled in configuration (ask your administrator if unsure)

---

## Quick Start

### Step 1: Enable Long-Term Memory

1. Open the TransparentAiAgent application
2. Look for the memory checkbox near the top of the page
3. Check the box labeled **"Use long-term memory"**

![Memory Checkbox Location]
```
┌────────────────────────────────────┐
│ Conversation: [Current Session ▼] │
│                                    │
│ ☑ Use long-term memory    ℹ️       │
│                                    │
│ [View] [Clear]                     │
└────────────────────────────────────┘
```

4. If you have existing memory, it will be loaded automatically
5. The agent now has access to your stored context

### Step 2: Have a Conversation

Start chatting with the agent. Introduce yourself or discuss what you're working on:

```
You: Hi! I'm Alex, a senior developer learning C# async programming.
     I prefer detailed explanations with code examples.

Agent: Nice to meet you, Alex! I'll remember your preferences.
       [Agent updates memory with your introduction]
       Let's dive into async programming...
```

### Step 3: End the Conversation

When you're done:

1. Click **"End Conversation"** button
2. The agent will review what it learned
3. Memory is automatically updated if needed
4. Start a new conversation anytime - the agent will remember you!

---

## Enabling and Disabling Memory

### Enabling Memory

**Method 1: Via Checkbox**
1. Check the **"Use long-term memory"** checkbox
2. If memory exists, it's loaded immediately
3. The agent can now read and update memory

**Method 2: Automatic (Configuration)**
- If configured, memory may be enabled by default
- Your preference is saved to browser localStorage
- Checkbox state persists across page reloads

### Disabling Memory

1. Uncheck the **"Use long-term memory"** checkbox
2. Memory is unloaded from the current conversation
3. The agent no longer has access to memory tools
4. **Note**: Memory files are NOT deleted - just not used

### When to Enable/Disable

**Enable When**:
- ✅ Having multi-session conversations about a project
- ✅ Learning something over time (tutorials, courses)
- ✅ Want personalized responses based on your preferences
- ✅ Need context continuity across sessions

**Disable When**:
- ❌ Asking quick, one-off questions
- ❌ Testing the agent with different personas
- ❌ Discussing sensitive topics you don't want stored
- ❌ Sharing someone else's computer temporarily

---

## Viewing Your Memory

### How to View

1. Ensure memory is enabled (checkbox checked)
2. Click the **"View"** button next to the checkbox
3. A modal overlay appears showing your memory

**Memory Viewer Features**:
- **Display Mode**: Rendered markdown view (formatted)
- **Edit Mode**: Raw markdown for editing
- **Character Counter**: Shows current size vs. limit (10,000 chars)
- **Actions**: Edit, Save, Close

### What You'll See

Example memory content:

```markdown
# User Context

## Background
- Name: Alex
- Role: Senior Developer (5+ years)
- Company: Tech startup

## Current Learning
- Topic: C# async/await patterns
- Level: Intermediate
- Progress: Completed basic async, working on advanced patterns

## Preferences
- Detail Level: Detailed explanations preferred
- Code Examples: Always include examples
- Learning Style: Hands-on with real-world scenarios

## Projects
- Blazor Dashboard: Real-time data visualization
  - Stack: Blazor Server, SignalR, EF Core
  - Goal: Learn WebSocket integration

## Notes
- Prefers TypeScript over JavaScript
- Familiar with SOLID principles
- Interested in Clean Architecture patterns
```

---

## Editing Your Memory

### Manual Editing

1. Click **"View"** to open memory viewer
2. Click **"Edit"** button
3. Edit the markdown directly in the text area
4. Watch the character counter (must stay under 10,000)
5. Click **"Save"** to persist changes
6. Click **"Cancel"** to discard changes

### When to Edit Manually

**Edit When**:
- ✅ Information becomes outdated
- ✅ You want to remove something
- ✅ Reorganizing for better clarity
- ✅ Correcting inaccurate information

**Example Edit**:
```markdown
Before:
- Current Learning: C# async/await patterns

After:
- Current Learning: Completed C# async/await
- Next Topic: Dependency injection patterns
```

### Editing Best Practices

1. **Use Markdown Formatting**: Headers, lists, emphasis
2. **Keep It Organized**: Use clear sections
3. **Be Concise**: Focus on important context only
4. **Update Regularly**: Remove outdated information
5. **Check Character Count**: Stay well below 10,000 limit

---

## Clearing Memory

### How to Clear

1. Ensure memory is enabled
2. Click the **"Clear"** button
3. Confirm the action (if prompted)
4. Memory is deleted for the current mode

**Result**: The agent starts fresh with no memory of previous conversations in this mode.

### When to Clear

**Clear Memory When**:
- ✅ Starting a completely new project/context
- ✅ Memory contains outdated or incorrect information
- ✅ Switching to a different use case entirely
- ✅ Privacy concern - want to remove all stored data

**Warning**: Clearing is **permanent** and cannot be undone. Consider editing instead if you only want to remove specific information.

---

## Mode-Specific Memory

### Understanding Modes

The application has different modes:
- **Normal Mode**: General tasks and conversations
- **Teaching Mode**: Learning and educational interactions

**Each mode has its own separate memory file.**

### How Mode Separation Works

```
Normal Mode Memory (memory-normal.md):
- Work projects
- Task-specific context
- Professional preferences

Teaching Mode Memory (memory-teaching.md):
- Learning topics and progress
- Educational preferences
- Tutorial history
```

### Switching Modes

When you switch modes:
1. Current mode's memory is saved
2. New mode's memory is loaded
3. Contexts remain isolated

**Example**:
```
[Normal Mode]
Agent: Let's continue with your dashboard project...

[Switch to Teaching Mode]
Agent: Ready to continue learning C# async patterns?
(Completely different context, no mixing)
```

### Best Practices for Modes

**Normal Mode - Use For**:
- Daily work and tasks
- Project-specific context
- Professional interactions

**Teaching Mode - Use For**:
- Learning new technologies
- Following tutorials
- Educational questions

**Tip**: Keeping contexts separate makes conversations more focused and relevant.

---

## What to Store (and What to Avoid)

### ✅ Good to Store

**Personal Context**:
- Name, role, experience level
- General background
- Communication preferences

**Project Information**:
- Project names and goals
- Technology stack
- Architectural decisions

**Learning Progress**:
- Topics studied
- Concepts mastered
- Areas needing more work

**Preferences**:
- Detail level (brief vs. detailed)
- Code example preferences
- Learning style

**Example**:
```markdown
# Alex - Senior Developer

## Current Projects
- Dashboard Application: Blazor + SignalR real-time updates
- Learning: Advanced C# patterns (async, DI, Clean Architecture)

## Preferences
- Detailed explanations with examples
- Hands-on, practical learning style
```

### ❌ Never Store

**Sensitive Information**:
- ❌ Passwords or API keys
- ❌ Personal identification numbers (SSN, passport, etc.)
- ❌ Credit card or banking information
- ❌ Health information
- ❌ Private keys or certificates

**Temporary Information**:
- ❌ Conversation transcripts
- ❌ Specific dates and times
- ❌ Temporary debugging notes

**Proprietary Information**:
- ❌ Confidential business data
- ❌ Trade secrets
- ❌ Customer-specific details

**Why?**: The agent has guardrails to refuse storing sensitive information, but you should also be mindful of what you share.

---

## Common Scenarios

### Scenario 1: First-Time User

**Situation**: You're using the agent for the first time and want it to remember you.

**Steps**:
1. Enable long-term memory
2. Introduce yourself naturally in conversation
3. Share relevant context (role, goals, preferences)
4. Let the agent update memory
5. End conversation
6. Next session: Agent remembers you!

**Example Conversation**:
```
You: Hi! I'm new here. I'm Alex, a senior developer with 5 years experience.
     I'm currently learning Blazor and prefer detailed explanations.

Agent: Welcome, Alex! I'll remember that you're an experienced developer
       learning Blazor. I'll make sure to provide detailed explanations.
       [Updates memory]

[End Conversation]

[Next Session]

Agent: Hi Alex! Ready to continue learning Blazor? Last time we were
       discussing component lifecycle...
```

### Scenario 2: Updating Preferences

**Situation**: Your preferences change (e.g., you now prefer brief explanations).

**Steps**:
1. Tell the agent about the change
2. Agent updates memory
3. Future responses reflect new preference

**Example**:
```
You: I've changed my mind - I prefer brief explanations now, not detailed ones.

Agent: Got it! I'll update my memory to note you prefer concise responses.
       [Updates memory: "Prefers brief, concise explanations"]
       I'll keep that in mind going forward.
```

Or manually edit:
1. Click "View"
2. Click "Edit"
3. Change: `- Detail Level: Detailed` → `- Detail Level: Brief`
4. Save

### Scenario 3: Project Context Over Time

**Situation**: Working on a project over multiple weeks.

**Steps**:
1. Enable memory at project start
2. Share project context in first session
3. Each session builds on previous context
4. Agent maintains continuity

**Example Timeline**:
```
Week 1:
You: Starting a Blazor dashboard with SignalR
Agent: [Stores project context]

Week 2:
Agent: How's the Blazor dashboard coming? Last week you were setting up SignalR.
You: Great! Now working on authentication.
Agent: [Updates: "Added authentication to dashboard"]

Week 3:
Agent: Your dashboard now has SignalR and auth. What's next?
```

### Scenario 4: Learning Journey

**Situation**: Learning a technology over several sessions.

**Steps**:
1. Use Teaching Mode
2. Enable memory
3. Agent tracks your progress
4. Builds on previous lessons

**Example**:
```
Session 1:
You: Teach me C# async/await
Agent: [Teaches basics]
[Updates memory: "Learned async/await basics"]

Session 2:
You: What's next in async programming?
Agent: Since you understand async/await, let's cover Task.WhenAll and
       parallel patterns...
```

### Scenario 5: Privacy Concern

**Situation**: You accidentally shared sensitive information.

**Steps**:
1. Click "View" to check memory
2. If sensitive data is present:
   - Click "Edit"
   - Remove the sensitive information
   - Save changes
3. Or click "Clear" to delete everything

**Important**: The agent should refuse to store sensitive data, but always verify if concerned.

---

## Troubleshooting

### Problem: Memory Not Loading

**Symptoms**: Checkbox is checked but agent doesn't remember you.

**Solutions**:
1. **Check Transparency Logs**: Look for memory load events
2. **View Memory**: Click "View" to see if content exists
3. **Restart Conversation**: End and start new conversation
4. **Check Configuration**: Verify `Enabled: true` in settings

### Problem: Memory Update Failed

**Symptoms**: Agent tries to update but gets error.

**Solutions**:
1. **Check Character Limit**: Memory might exceed 10,000 chars
   - Solution: Edit and summarize/remove old content
2. **Check File Permissions**: Ensure app can write to `data/memory/`
3. **Check Disk Space**: Ensure sufficient disk space

### Problem: Sensitive Data Stored

**Symptoms**: Memory contains sensitive information.

**Solutions**:
1. **Immediate Action**: View → Edit → Remove sensitive data → Save
2. **Or**: Click "Clear" to delete all memory
3. **Prevention**: Be mindful of what you share in conversations

### Problem: Memory Too Large

**Symptoms**: Character counter shows close to 10,000 limit.

**Solutions**:
1. **Edit and Summarize**: Remove old/outdated information
2. **Focus on Essentials**: Keep only important context
3. **Use Clear Sections**: Organize for easier pruning

**Example Cleanup**:
```markdown
Before (8,500 chars):
[Lots of old project details, completed learning topics, etc.]

After (3,000 chars):
# Current Context
- Active Projects: Dashboard (Blazor + SignalR)
- Learning: Advanced C# patterns
- Preferences: Brief explanations, TypeScript preferred
```

### Problem: Mode Switch Not Working

**Symptoms**: Switching modes doesn't load different memory.

**Solutions**:
1. **Disable and Re-enable**: Uncheck and recheck memory box
2. **Check Files**: Verify separate files exist (`memory-normal.md`, `memory-teaching.md`)
3. **Restart App**: Sometimes requires app restart

---

## Tips and Best Practices

### For Effective Memory Use

1. **Start Simple**: Begin with basic introduction, add details over time
2. **Review Regularly**: Periodically view and update memory
3. **Keep It Current**: Remove outdated information
4. **Use Markdown**: Structure content with headers and lists
5. **Be Specific**: "Learning C# async" is better than "Learning programming"

### For Privacy

1. **Review Before Sharing**: Think before sharing sensitive context
2. **Check Periodically**: View memory monthly to verify content
3. **Use Mode Separation**: Keep work and personal contexts separate
4. **Clear When Done**: Delete memory if no longer needed

### For Long-Term Projects

1. **Document Decisions**: Store architectural choices and rationale
2. **Track Progress**: Note milestones and completed features
3. **Maintain Context**: Keep project goals and constraints updated
4. **Prune Regularly**: Remove completed tasks and old notes

---

## Advanced Usage

### Memory as Documentation

Use memory to maintain living documentation:

```markdown
# Project: Blazor Dashboard

## Architecture Decisions
- Why Blazor Server: Real-time requirements favor SignalR
- Why EF Core: Existing team expertise, simple data model
- State Management: Singleton services for global state

## Patterns Used
- Repository pattern for data access
- CQRS for complex commands
- Observer pattern for real-time updates

## Lessons Learned
- SignalR circuit management critical for stability
- State container needs careful lifecycle management
```

### Memory for Team Onboarding

If multiple people use the same instance:

```markdown
# Team Context

## Team Members
- Alex: Backend lead, C# expert
- Jordan: Frontend specialist, Blazor
- Sam: Database, EF Core

## Team Preferences
- Architecture: Clean Architecture + CQRS
- Testing: TDD with MSTest
- Code Style: Microsoft C# conventions
```

**Warning**: Be cautious with shared memory - ensure no personal information is stored.

---

## Related Documentation

- **Concept**: [Long-Term Memory](../../03-concepts/long-term-memory.md) - Understanding the feature
- **Component**: [Long-Term Memory Service](../../04-components/infrastructure/long-term-memory-service.md) - Technical details
- **Tools**: [Long-Term Memory Tools](../../04-components/tools/builtin/long-term-memory-tools.md) - How tools work
- **Configuration**: [Configuration Guide](../deployment/configuration-guide.md#long-term-memory) - Setup and settings

---

## Summary

Long-term memory enhances your experience by:
- ✅ Maintaining context across sessions
- ✅ Personalizing responses to your preferences
- ✅ Tracking learning progress over time
- ✅ Remembering project details and decisions

**Remember**:
- You have full control (view, edit, clear anytime)
- Memory is transparent (readable markdown files)
- Privacy is protected (local storage, guardrails)
- Use modes to separate contexts

**Happy conversing!** 🧠
