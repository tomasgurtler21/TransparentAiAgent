# Teaching Mode: Future Enhancements

**Status**: Planning Phase
**Target**: Phase 10 (Post-Demo)
**Created**: 2025-11-09

---

## Scope

**✅ This document covers**:
- Planned enhancements to Teaching Mode
- Scenarios/Scripts system design
- Knowledge Library concept
- Implementation priorities

**❌ NOT in this document**:
- Current implementation (see [implementation-roadmap.md](implementation-roadmap.md))
- Architecture details (see [architecture.md](architecture.md))
- Vision and philosophy (see [vision.md](vision.md))

---

## Overview

This document outlines two major enhancements to Teaching Mode planned for Phase 10 and beyond. These features were identified during Phase 9d planning but deferred to focus on core teaching mode functionality for the demo.

---

## Enhancement 1: Scenarios/Scripts System

### Concept

A **Scenarios/Scripts system** allows users to select pre-defined teaching scenarios that guide them through specific features or concepts. The system automates part of the teaching interaction by sending prepared prompts and triggering the agent to teach specific topics.

### User Experience Flow

1. **Scenario Selection**:
   - User sees a list of available teaching scenarios (e.g., "Context Limits", "Tool Usage", "Transparency Logging")
   - User selects a scenario they want to learn about
   - System confirms selection

2. **Automated Demonstration**:
   - System sends pre-written prompts as if they were from the user
   - Each prompt reveals some aspect of the feature
   - UI shows visual indicators that this is a guided scenario

3. **Teaching Reveal**:
   - After showing examples, system prompts agent: "Now teach the user about [topic] by revealing the relevant UI features"
   - Agent uses UI control tools to reveal and explain features
   - User can interrupt or ask questions at any time

4. **Completion**:
   - Scenario completes after covering key points
   - User can explore further or select another scenario

### Example Scenario: "Context Limits"

**Scenario Definition**:
```json
{
  "name": "Context Limits",
  "description": "Learn about conversation context windows and message truncation",
  "steps": [
    {
      "type": "auto_message",
      "content": "Hello! I'm new to AI agents.",
      "delay": 1000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "auto_message",
      "content": "I noticed there are numbers next to some messages. What do those mean?",
      "delay": 2000
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "agent_prompt",
      "content": "Now use your UI control tools to teach the user about context management. Show them the context indicators and explain how messages move out of context.",
      "hidden": true
    },
    {
      "type": "wait_for_response"
    },
    {
      "type": "completion_message",
      "content": "Scenario complete! Feel free to continue exploring or try another scenario."
    }
  ]
}
```

**User Sees**:
1. Scenario starts with banner: "📚 Scenario: Context Limits"
2. Messages appear automatically with typing indicator
3. Agent responds naturally to each message
4. Agent reveals context indicators and explains
5. Completion message with option to exit scenario mode

### Technical Design

**Components Needed**:

1. **ScenarioDefinition** (Domain Model):
```csharp
public class ScenarioDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ScenarioStep> Steps { get; set; }
}

public class ScenarioStep
{
    public ScenarioStepType Type { get; set; }
    public string Content { get; set; }
    public int DelayMs { get; set; }
    public bool Hidden { get; set; } // Don't show to user
}

public enum ScenarioStepType
{
    AutoMessage,        // Send as user message
    WaitForResponse,    // Wait for agent to respond
    AgentPrompt,        // Send hidden prompt to agent
    CompletionMessage   // Show completion
}
```

2. **ScenarioRegistry** (Infrastructure):
```csharp
public interface IScenarioRegistry
{
    IReadOnlyList<ScenarioDefinition> GetAllScenarios();
    ScenarioDefinition GetScenario(string id);
}
```

3. **ScenarioExecutor** (Application):
```csharp
public interface IScenarioExecutor
{
    Task StartScenarioAsync(string scenarioId);
    Task StopScenarioAsync();
    bool IsRunning { get; }
    event EventHandler<ScenarioStep>? StepExecuting;
    event EventHandler? ScenarioCompleted;
}
```

4. **UI Components**:
   - `ScenarioSelector.razor` - List of scenarios
   - `ScenarioIndicator.razor` - Banner showing active scenario
   - `ScenarioControls.razor` - Pause/Stop controls

**Configuration** (appsettings.json):
```json
{
  "TeachingMode": {
    "Scenarios": [
      {
        "id": "context-limits",
        "name": "Context Limits",
        "description": "Learn about context windows",
        "stepsFile": "scenarios/context-limits.json"
      },
      {
        "id": "transparency",
        "name": "Transparency Logging",
        "description": "Understand transparency features",
        "stepsFile": "scenarios/transparency.json"
      }
    ]
  }
}
```

**Scenarios Storage**: JSON files in `wwwroot/scenarios/` directory

### Implementation Priority

**Phase**: 10a (Post-Demo)
**Estimated Effort**: 2-3 days
**Dependencies**: Phase 9d complete

**Tasks**:
1. Define scenario JSON schema
2. Implement ScenarioDefinition models
3. Create ScenarioRegistry (load from files)
4. Implement ScenarioExecutor
5. Build ScenarioSelector UI
6. Create 3-5 example scenarios
7. Add scenario controls to teaching mode
8. Test end-to-end scenario flows

---

## Enhancement 1b: Advanced Scenarios with Environment Manipulation

### Concept

While basic scenarios (Enhancement 1) use scripted message flows, **advanced scenarios** go further by manipulating the actual system environment to create genuine teaching moments. Instead of just sending pre-written messages, these scenarios temporarily change configuration, limits, or state to let users *experience* concepts firsthand.

**Key Difference:**
- **Basic Scenario**: "Let me tell you about context limits" (scripted explanation)
- **Advanced Scenario**: "Experience context limits" (genuinely reduce message limit, user sees real truncation)

### Why Advanced Scenarios?

**Learning by Experience:**
- Users don't just hear about features—they experience them
- Model genuinely encounters the limitation (not simulated)
- Creates "aha moments" through authentic demonstration
- More memorable and impactful than explanations alone

**Example Use Cases:**
1. **Context Limits**: Temporarily reduce message limit to demonstrate truncation in real-time
2. **Rate Limiting**: Introduce artificial delays to show token rate limits
3. **Tool Failures**: Temporarily make a tool return errors to teach error handling
4. **Privacy Controls**: Temporarily redact certain information to demonstrate privacy features

### Architecture: Three Core Systems

Advanced scenarios require three new capabilities beyond basic scenarios:

#### 1. Config Overlay System

**Purpose:** Temporarily override system configuration during scenarios

**Concept:**
```
Normal Config (base)
  ↓
+ Scenario Overlay (temporary changes)
  ↓
= Effective Config (what system actually uses)
```

**Key Features:**
- Stack-based overlays (can layer multiple)
- Automatic restoration when scenario ends
- Thread-safe for per-user scenarios
- Can override: message limits, system prompt additions, tool availability, etc.

**Example:**
```csharp
// Scenario starts
ConfigOverlayService.PushOverlay(new ConfigOverlay {
    MessageLimit = 10,  // Reduce from default (e.g., 50)
    SystemPromptAddition = "Note: You are experiencing a teaching scenario."
});

// ... scenario runs with limited context ...

// Scenario ends
ConfigOverlayService.PopOverlay();  // Restores normal limits
```

See [config-overlay-service.md](config-overlay-service.md) for API details.

#### 2. Scenario Message Types

**Purpose:** Distinguish scenario-generated messages from real user messages in the UI

**New Message Roles:**
- `ScenarioUser`: Message sent by scenario, but treated as user message by model
- `ScenarioSystem`: Hidden system message to trigger teaching (model sees, user doesn't—or vice versa)

**UI Rendering:**
Scenario messages render differently for the observing user:
- Icon: 🎬 (indicates scenario-generated)
- Color: Slightly lighter shade than normal user messages
- Expandable annotation: Shows explanation visible only to real user

**Example:**
```json
{
  "type": "scenario_user_message",
  "content": "Hi, my name is John Doe.",
  "annotation": "Note to observer: The model will remember this name for now, but it will be truncated from context in a few steps.",
  "delay": 1000
}
```

**To the model:** Appears as normal `User` role message
**To the real user:** Shows annotation explaining what's happening

#### 3. Conditional Execution & Environment Control

**Purpose:** Scenarios can react to model behavior and manipulate environment dynamically

**New Capabilities:**
- **Wait for condition**: Pause scenario until model response matches criteria
- **Apply environment changes**: Modify config, limits, tool availability mid-scenario
- **Restore environment**: Undo changes at appropriate moment
- **Enable/disable user input**: Control when real user can interact

**Example:**
```json
{
  "type": "wait_for_condition",
  "condition": "response_contains",
  "parameters": {
    "keywords": ["don't know", "cannot recall", "didn't tell"],
    "timeout": 30000
  },
  "annotation": "Waiting for model to indicate it doesn't know the name..."
}
```

This allows scenarios to adapt: if model doesn't respond as expected, scenario can adjust.

### Reference Scenario: Context Limits (Advanced)

For a complete, detailed example of an advanced scenario, see:
**[reference-scenarios/context-limits-advanced.md](reference-scenarios/context-limits-advanced.md)**

**High-Level Flow:**
1. User selects "Context Limits (Advanced)" scenario
2. Scenario reduces message limit to 10 (via config overlay)
3. Scenario sends auto-messages including user's name
4. Context fills up, early messages (including name) genuinely truncate
5. Scenario asks model for the name—model truly doesn't know (not simulated!)
6. User sees 🎬-marked messages with annotations explaining what's happening
7. After model expresses confusion, limits restored
8. System message triggers teaching: "Explain what happened, show context indicators"
9. Model teaches about context windows using UI control tools
10. Scenario completes, user can continue exploring

**Key Innovation:** The model genuinely loses the information (message truncated), creating an authentic demonstration of context limits.

### JSON Schema for Advanced Scenarios

For complete JSON schema documentation, see:
**[scenario-schema.md](scenario-schema.md)**

**New Step Types:**
- `apply_config_overlay`: Push temporary config changes
- `restore_config_overlay`: Remove overlay, restore previous config
- `scenario_user_message`: Auto-message with annotation
- `scenario_system_message`: System message with visibility control
- `wait_for_condition`: Pause until condition met
- `enable_user_input` / `disable_user_input`: Control real user interaction

### Design Philosophy: Experimentation-Friendly

**Important:** Advanced scenarios are experimental by nature. We don't know exactly what will work best, so the design prioritizes:

✅ **Flexibility:** JSON-driven configuration, easy to change scenarios without code changes
✅ **Extensibility:** Easy to add new step types, conditions, config overlay options
✅ **Observable:** Clear UI indicators, annotations visible to user
✅ **Recoverable:** Automatic cleanup if scenario fails or is aborted
✅ **Iterative:** Quick to test variations (e.g., when to restore message limits)

**Open Questions to Explore During Implementation:**
- **Restoration Timing**: Should message limits auto-restore after model confusion, or require user action to see correct message status icons first?
- **System Prompt Hints**: How explicit should the "you're in a scenario" hint be? Too explicit = less genuine. Too subtle = model might not teach correctly.
- **User Control**: ✅ **IMPLEMENTED** - Pause/resume functionality has been implemented. Users can pause scenarios manually via UI button, and scenarios can pause themselves with `pause_for_user` step type. See `SCENARIO_PAUSE_RESUME_DESIGN.md` for details.
- **Failure Handling**: What if model doesn't respond as expected? Timeout and fallback? Let user manually trigger next step?

These questions will be resolved through experimentation, which the flexible JSON schema enables.

### Technical Design Notes

**Config Overlay Service:**
- Simple interface (see [config-overlay-service.md](config-overlay-service.md))
- Detailed implementation deferred to implementation phase
- Focus: stack-based overrides, automatic cleanup

**Scenario Executor:**
- Extends basic scenario executor (Enhancement 1)
- Adds environment manipulation capabilities
- Conditional execution engine
- State management for complex scenarios

**Message Rendering:**
- Modify `MessageDisplay.razor` to handle scenario message types
- Add annotation expansion UI
- Icon + color coding for scenario messages

**Integration Points:**
- `ConversationManager`: Use `GetEffectiveConfig()` instead of base config
- `AgentOrchestrator`: Respect config overlays for message limits
- `UIControlService`: May be used by scenarios to pre-configure UI state

### Implementation Priority

**Phase:** 10b (Post-Demo, after basic scenarios in 10a)
**Estimated Effort:** 6-10 days
**Dependencies:**
- Basic scenarios system (Enhancement 1) implemented
- UI control tools (Phase 9d) working

**Rationale for Sequencing:**
1. Basic scenarios (10a) prove the JSON schema and executor pattern
2. Advanced scenarios (10b) extend proven foundation
3. Lessons from basic scenarios inform advanced implementation
4. Lower risk: can ship basic scenarios alone if advanced proves too complex

### Success Criteria

**Functional:**
✅ Config overlay system works reliably
✅ Message limits can be temporarily changed and restored
✅ Scenario messages render distinctly in UI
✅ Annotations visible to user, not model
✅ Conditional execution (wait for response keywords) works
✅ Context limits scenario demonstrates genuine truncation
✅ Teaching trigger reliably prompts model to teach

**User Experience:**
✅ Scenarios feel authentic, not scripted
✅ UI clearly indicates scenario vs. normal mode
✅ User understands what's happening via annotations
✅ "Aha moment" when experiencing concept firsthand
✅ Model teaches effectively after scenario

**Technical:**
✅ No config leakage between users (scoped properly)
✅ Automatic cleanup if scenario fails
✅ Performance acceptable (no noticeable lag)
✅ Easy to create new scenarios (JSON editing)

### Comparison: Basic vs Advanced Scenarios

| Aspect | Basic Scenarios (1) | Advanced Scenarios (1b) |
|--------|---------------------|-------------------------|
| **Complexity** | Low | Medium-High |
| **Setup Effort** | Simple JSON | JSON + environment design |
| **Teaching Method** | Tell (scripted messages) | Show (genuine experience) |
| **Model Awareness** | Fully aware, follows script | Genuine experience + teaching trigger |
| **Environment Changes** | None | Config overlays, state manipulation |
| **Implementation Risk** | Low | Medium |
| **Learning Impact** | Good | Excellent (experiential) |
| **Use Cases** | Explanatory topics | Demonstrable concepts |

**Recommendation:** Implement basic scenarios first to validate patterns, then extend to advanced scenarios for high-impact teaching moments like context limits.

---

## Enhancement 2: Knowledge Library

### Concept

A **Knowledge Library** is a curated collection of topics and information that the agent can access to provide consistent, accurate teaching on critical subjects. It serves as a "textbook" for the teaching agent, ensuring it teaches important concepts correctly and comprehensively.

### Purpose

1. **Guardrails**: Ensure agent teaches critical concepts correctly (e.g., security, privacy, proper tool usage)
2. **Consistency**: Same topics taught the same way regardless of conversation
3. **Depth**: Provide detailed information beyond what fits in the system prompt
4. **Expandability**: Easy to add new teaching topics without modifying code

### User Experience

**Hidden from User** - The knowledge library works behind the scenes:

1. **Agent Query**: When agent needs to teach a topic, it can query the knowledge library
2. **Information Retrieval**: Library returns structured information about the topic
3. **Teaching**: Agent uses retrieved information to teach accurately
4. **User**: Receives accurate, comprehensive teaching without knowing about the library

### Example: Teaching About API Keys

**Without Knowledge Library**:
- Agent might give incomplete security advice
- Inconsistent recommendations across conversations
- May not cover all important points

**With Knowledge Library**:
- Agent queries: "api-key-security"
- Library returns comprehensive security guidelines
- Agent teaches all key points consistently
- User gets complete, accurate information

### Knowledge Entry Structure

```json
{
  "id": "api-key-security",
  "topic": "API Key Security",
  "category": "Security",
  "keywords": ["api", "key", "security", "authentication", "secrets"],
  "summary": "Best practices for handling API keys securely",
  "content": {
    "overview": "API keys are sensitive credentials that must be protected...",
    "keyPoints": [
      "Never commit API keys to version control",
      "Use environment variables or secure vaults",
      "Rotate keys regularly",
      "Limit key permissions to minimum required",
      "Monitor key usage for anomalies"
    ],
    "examples": [
      {
        "title": "Storing API Key in Environment Variable",
        "code": "var apiKey = Environment.GetEnvironmentVariable(\"API_KEY\");",
        "explanation": "This approach keeps the key out of source code"
      }
    ],
    "warnings": [
      "Never share API keys in screenshots or demos",
      "Revoke keys immediately if compromised"
    ],
    "relatedTopics": ["authentication", "environment-variables", "security-best-practices"]
  }
}
```

### Technical Design

**Components Needed**:

1. **KnowledgeEntry** (Domain Model):
```csharp
public class KnowledgeEntry
{
    public string Id { get; set; }
    public string Topic { get; set; }
    public string Category { get; set; }
    public List<string> Keywords { get; set; }
    public string Summary { get; set; }
    public KnowledgeContent Content { get; set; }
}

public class KnowledgeContent
{
    public string Overview { get; set; }
    public List<string> KeyPoints { get; set; }
    public List<Example> Examples { get; set; }
    public List<string> Warnings { get; set; }
    public List<string> RelatedTopics { get; set; }
}
```

2. **IKnowledgeLibrary** (Domain Service):
```csharp
public interface IKnowledgeLibrary
{
    KnowledgeEntry? GetTopic(string topicId);
    IReadOnlyList<KnowledgeEntry> SearchTopics(string query);
    IReadOnlyList<string> GetCategories();
    IReadOnlyList<KnowledgeEntry> GetByCategory(string category);
}
```

3. **Knowledge Library Tool** (Built-in Tool):
```json
{
  "name": "knowledge_library_query",
  "description": "Query the knowledge library for accurate teaching information on specific topics",
  "inputSchema": {
    "type": "object",
    "properties": {
      "topic": {
        "type": "string",
        "description": "The topic to query (e.g., 'api-key-security', 'context-window')"
      }
    },
    "required": ["topic"]
  }
}
```

4. **Teaching Mode Prompt Integration**:
```text
# Knowledge Library

You have access to a knowledge library containing detailed, accurate information on key topics.
When teaching critical concepts (security, privacy, best practices), use the `knowledge_library_query` tool to retrieve authoritative information.

Available topics: [dynamically list topics]

Example:
User: "How should I handle API keys?"
You: Let me get the best practices...
[Use knowledge_library_query with topic="api-key-security"]
[Teach based on retrieved information]
```

**Knowledge Storage**: JSON files in `wwwroot/knowledge/` directory, organized by category

```
wwwroot/knowledge/
├── security/
│   ├── api-key-security.json
│   ├── authentication.json
│   └── data-privacy.json
├── tools/
│   ├── mcp-overview.json
│   ├── tool-best-practices.json
│   └── tool-security.json
├── transparency/
│   ├── logging-overview.json
│   └── transparency-benefits.json
└── index.json  # Topic index for quick lookup
```

### Agent Integration

**Teaching Mode Prompt Addition**:
```text
# Teaching with Knowledge Library

When you need to teach a topic in depth, especially regarding security, privacy, or best practices:
1. Use `knowledge_library_query` to retrieve authoritative information
2. Present the information in a friendly, non-technical way
3. Include examples from the library when available
4. Mention warnings or cautions from the library
5. Suggest related topics the user might be interested in

Critical topics that REQUIRE knowledge library lookup:
- API key handling
- Security best practices
- Data privacy
- Authentication methods
- Tool security
```

### Implementation Priority

**Phase**: 10b (Post-Demo)
**Estimated Effort**: 3-4 days
**Dependencies**: Phase 9d complete, scenario system optional

**Tasks**:
1. Define knowledge entry JSON schema
2. Implement KnowledgeEntry models
3. Create KnowledgeLibrary service
4. Implement knowledge_library_query tool
5. Build knowledge indexing for fast lookup
6. Create 10-15 initial knowledge entries
7. Update teaching mode prompt to use library
8. Add knowledge entry management UI (optional)
9. Test teaching with knowledge library

---

## Combined Usage: Scenarios + Knowledge Library

When both systems are implemented, they work together powerfully:

**Example Flow**:

1. **User** selects "API Security Best Practices" scenario
2. **Scenario** sends automated prompts about API keys
3. **Agent** receives teaching prompt
4. **Agent** uses `knowledge_library_query` to get comprehensive API security info
5. **Agent** teaches using authoritative information from library
6. **Agent** uses UI control tools to show relevant configuration
7. **Scenario** completes with summary

**Benefits**:
- Consistent teaching on critical topics
- Comprehensive coverage via scenarios
- Accurate information via knowledge library
- Interactive learning via UI control tools

---

## Implementation Roadmap

### Phase 10a: Scenarios System (Week 1-2 post-demo)
**Focus**: User-selected learning paths

**Deliverables**:
- Scenario definition schema
- Scenario executor
- 5 initial scenarios
- UI for scenario selection
- Testing with real scenarios

**Success Criteria**:
- User can select and complete scenarios
- Scenarios trigger teaching behavior
- Smooth integration with teaching mode

### Phase 10b: Knowledge Library (Week 3-4 post-demo)
**Focus**: Authoritative teaching information

**Deliverables**:
- Knowledge entry schema
- Knowledge library service
- 15 knowledge entries
- knowledge_library_query tool
- Updated teaching prompt

**Success Criteria**:
- Agent queries library for critical topics
- Consistent teaching across conversations
- Accurate information on key topics

### Phase 10c: Polish & Integration (Week 5 post-demo)
**Focus**: Seamless user experience

**Deliverables**:
- Scenarios using knowledge library
- Advanced scenario features (branching, user choices)
- Knowledge library UI for browsing
- Analytics and usage tracking
- Comprehensive testing

**Success Criteria**:
- Scenarios and library work together seamlessly
- Users can explore knowledge independently
- Teaching quality measurably improved

---

## Design Considerations

### Scenarios System

**Pros**:
- ✅ Guided learning experiences
- ✅ Reproducible teaching flows
- ✅ Easy to add new scenarios
- ✅ Non-technical users can follow along

**Cons**:
- ❌ Less flexible than free-form conversation
- ❌ May feel scripted if not done well
- ❌ Requires maintenance of scenario definitions

**Mitigations**:
- Allow users to interrupt and ask questions
- Keep scenarios short (3-5 steps)
- Make scenarios optional, not required
- Provide smooth exit from scenario mode

### Knowledge Library

**Pros**:
- ✅ Ensures accurate teaching
- ✅ Consistent information
- ✅ Easy to update and expand
- ✅ Guardrails for critical topics

**Cons**:
- ❌ Requires curation and maintenance
- ❌ May make responses feel less natural
- ❌ Overhead of querying library

**Mitigations**:
- Use library only for critical topics
- Keep entries concise and well-structured
- Allow agent discretion on when to query
- Make library information blend naturally into conversation

---

## Success Metrics

### Scenarios System Metrics
- Scenario completion rate
- User satisfaction with scenarios
- Number of scenarios created
- Average scenario duration
- Interruption/question rate during scenarios

### Knowledge Library Metrics
- Library query frequency
- Topics most queried
- Teaching consistency (same topic, similar responses)
- User comprehension (via follow-up questions)
- Knowledge entry coverage (% of teaching topics)

---

## Alternative Approaches Considered

### For Scenarios
1. **Hard-coded Flows**: Rejected - not flexible enough
2. **Video Tutorials**: Rejected - not interactive
3. **Interactive Tooltips**: Rejected - not comprehensive enough
4. **Agent-Generated Scenarios**: Deferred - too complex for Phase 10

### For Knowledge Library
1. **Embedding in System Prompt**: Rejected - prompt too long
2. **RAG System**: Deferred - complex, may be Phase 11
3. **External API**: Rejected - adds dependency
4. **Agent Memory**: Rejected - not consistent enough

---

## Next Steps

### Immediate (Post-Demo)
1. **Gather Requirements**: Validate scenarios and library concepts with users
2. **Prioritize**: Confirm scenarios before library, or adjust based on feedback
3. **Design Details**: Finalize JSON schemas and API designs
4. **Resource Planning**: Allocate time for implementation

### Before Implementation
1. **Create Example Scenarios**: Draft 10+ scenarios to validate schema
2. **Create Example Knowledge Entries**: Draft 20+ entries to validate structure
3. **Prototype**: Quick prototype to validate technical approach
4. **User Testing**: Test prototypes with target audience

---

## Questions to Resolve

### Scenarios System
- [ ] Should scenarios be user-created or developer-only?
- [ ] How to handle user deviating from scenario script?
- [ ] Should scenarios support branching paths?
- [ ] How to track scenario progress/completion?

### Knowledge Library
- [ ] Who curates knowledge entries? (Developers? Users? AI-assisted?)
- [ ] Should library be searchable by users directly?
- [ ] How often to update entries?
- [ ] Should entries support versioning?

### Integration
- [ ] Can scenarios reference knowledge entries directly?
- [ ] Should teaching mode remember what topics were taught?
- [ ] How to avoid over-reliance on library (keeping conversation natural)?

---

## Conclusion

Both **Scenarios/Scripts** and **Knowledge Library** represent significant enhancements to Teaching Mode that will improve consistency, accuracy, and guided learning. The scenarios system enables structured learning paths, while the knowledge library ensures accurate teaching on critical topics.

**Recommendation**: Implement scenarios first (Phase 10a) to enable guided learning experiences, then add knowledge library (Phase 10b) to improve teaching accuracy. This staged approach allows earlier value delivery and reduces implementation risk.

**Status**: Ready for Phase 10 planning and implementation after successful Phase 9d demo.
