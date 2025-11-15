# Knowledge Library Tool

**Last Updated**: 2025-11-15
**Status**: Active
**Phase**: Phase 9 (Knowledge Library Feature)
**Layer**: Infrastructure (Tools)

---

## Document Scope

**What belongs in this document**:
- KnowledgeLibraryTool implementation
- KnowledgeLibraryToolExecutor implementation
- BuiltInKnowledgeToolRegistry implementation
- Tool integration with teaching mode
- JSON knowledge entry structure

**What does NOT belong here**:
- ❌ Knowledge Library philosophy → See [../../../03-concepts/knowledge-library.md](../../../03-concepts/knowledge-library.md)
- ❌ How to add knowledge entries → See [../../../05-guides/development/adding-knowledge-entries.md](../../../05-guides/development/adding-knowledge-entries.md)
- ❌ Domain models → See `KNOWLEDGE_LIBRARY_DESIGN.md` (project root)
- ❌ Tool Manager orchestration → See [../tool-manager.md](../tool-manager.md)

---

## Overview

The Knowledge Library Tool provides the teaching agent with access to curated **guardrails** for teaching critical concepts. It's a built-in tool (ToolSourceType.BuiltInKnowledge) that queries JSON-based knowledge entries and returns formatted content to guide the LLM's teaching.

## Purpose

- **Provide** critical guardrails for teaching security, privacy, and safety-critical concepts
- **Correct** common LLM misconceptions or knowledge gaps
- **Supply** application-specific context not in LLM training data
- **Establish** red lines and "must-knows" for teaching
- **Guide** the LLM toward correct teaching approaches

## Key Philosophy

**Guardrails, Not Encyclopedias**:
- Entries provide essential principles, not comprehensive documentation
- LLM uses guardrails + built-in knowledge to teach comprehensively
- Target ~200-400 tokens per entry (flexible based on complexity)
- Focus on "must-knows" and "never-dos"

See [Knowledge Library Concept](../../../03-concepts/knowledge-library.md) for detailed philosophy.

---

## Architecture

### Components

```
Infrastructure Layer:
├── Tools/BuiltInKnowledge/
│   ├── KnowledgeLibraryTool.cs          # ITool implementation
│   ├── KnowledgeLibraryToolExecutor.cs  # IToolExecutor implementation
│   └── BuiltInKnowledgeToolRegistry.cs  # IToolRegistry implementation
├── Knowledge/
│   ├── JsonKnowledgeLibrary.cs          # IKnowledgeLibrary implementation
│   └── JsonModels.cs                    # DTOs for JSON deserialization

Domain Layer:
└── Knowledge/
    ├── IKnowledgeLibrary.cs             # Domain service interface
    ├── KnowledgeEntry.cs                # Domain model
    ├── KnowledgeContent.cs              # Content structure
    ├── KnowledgeExample.cs              # Example structure
    ├── KnowledgeReference.cs            # Reference structure
    └── KnowledgeEntrySummary.cs         # Lightweight summary

Application Layer:
└── Teaching/
    └── TeachingModePromptBuilder.cs     # System prompt integration
```

---

## Tool Implementation

### KnowledgeLibraryTool

**File**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInKnowledge/KnowledgeLibraryTool.cs`

**Purpose**: Defines the tool schema and metadata for LLM consumption.

**Implementation**:
```csharp
public class KnowledgeLibraryTool : ITool
{
    public string Name => "knowledge_library_query";

    public string Description =>
        "Retrieve critical guardrails from the knowledge library for a specific topic. " +
        "Returns essential principles and red lines. Use when teaching security, privacy, or safety-critical concepts. " +
        "The guardrails guide your teaching; fill in details using your own knowledge.";

    public string ParametersSchema => """
    {
      "type": "object",
      "properties": {
        "topic": {
          "type": "string",
          "description": "The topic ID to query (e.g., 'api-key-security'). Available topics are listed in your system instructions."
        }
      },
      "required": ["topic"]
    }
    """;

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
```

**Key Design Decisions**:
- **Single parameter**: `topic` (topic ID) - keeps tool interface simple
- **No filtering**: LLM receives topic list in system prompt and decides what to query
- **Guardrails emphasis**: Description guides LLM to use entries as guardrails, not complete answers

---

### KnowledgeLibraryToolExecutor

**File**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInKnowledge/KnowledgeLibraryToolExecutor.cs`

**Purpose**: Executes the knowledge_library_query tool by retrieving and formatting entries.

**Execution Flow**:
1. **Validate** arguments (topic parameter required)
2. **Query** IKnowledgeLibrary.GetTopic(topicId)
3. **Format** entry as Markdown for LLM consumption
4. **Return** ToolExecutionResult.Success(formattedContent) or Error

**Markdown Formatting**:
```
# {Topic Name}

**Category:** {Category}
**Knowledge Gap Likelihood:** {low|medium|high}
**Last Updated:** {YYYY-MM-DD}

## Overview
{overview text}

## Key Points
- {keyPoint1}
- {keyPoint2}

## Examples
### {example.title}
```
{example.code}
```
{example.explanation}

## ⚠️ Warnings
- {warning1}
- {warning2}

## Best Practices
- {bestPractice1}

## Related Topics
For more information, you can also query: {relatedTopic1}, {relatedTopic2}

## References
- [{reference.title}]({reference.url})
```

**Error Handling**:
- Missing `topic` parameter → `ToolExecutionResult.Error("Missing required parameter 'topic'")`
- Topic not found → `ToolExecutionResult.Error("Topic '{topicId}' not found in knowledge library")`
- Exception during execution → Logged and returned as error

---

### BuiltInKnowledgeToolRegistry

**File**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInKnowledge/BuiltInKnowledgeToolRegistry.cs`

**Purpose**: Provides tool discovery for the knowledge library tool.

**Implementation**:
```csharp
public class BuiltInKnowledgeToolRegistry : IToolRegistry
{
    private readonly KnowledgeLibraryTool _tool;

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public async Task<IReadOnlyList<ITool>> GetToolsAsync()
    {
        return await Task.FromResult(new List<ITool> { _tool }.AsReadOnly());
    }

    public async Task<ITool?> GetToolAsync(string toolName)
    {
        if (toolName == _tool.Name)
            return await Task.FromResult(_tool);
        return null;
    }
}
```

**Features**:
- Single tool registry (currently only `knowledge_library_query`)
- Case-insensitive tool lookup (when implemented)
- Thread-safe

---

## Knowledge Library Implementation

### JsonKnowledgeLibrary

**File**: `TransparentAiAgentCore/Infrastructure/Knowledge/JsonKnowledgeLibrary.cs`

**Purpose**: Implements IKnowledgeLibrary by reading JSON files from disk.

**Initialization Flow**:
1. **Scan** `wwwroot/knowledge/entries/` directory at startup
2. **Parse** all `*.json` files
3. **Skip** corrupted files with error logging (graceful degradation)
4. **Build** in-memory index from successfully parsed entries
5. **Sort** index alphabetically by topic

**Key Features**:
- **Directory scanning**: No index.json needed - discovers entries automatically
- **Lazy loading**: Full entries loaded on-demand via GetTopic()
- **In-memory caching**: Entries cached after first load
- **Thread-safe**: Lock-based concurrency control
- **Graceful errors**: Corrupted files don't crash library

**API Methods**:
```csharp
public interface IKnowledgeLibrary
{
    KnowledgeEntry? GetTopic(string topicId);
    IReadOnlyList<KnowledgeEntrySummary> GetAllTopics();
    IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category);
    IReadOnlyList<string> GetCategories();
}
```

**Performance**:
- Index load: <10ms for 50 entries
- Entry retrieval (cached): <1ms
- Entry retrieval (from disk): <5ms

---

## JSON Entry Structure

### Complete Schema

```json
{
  "id": "topic-id",
  "topic": "Human-Readable Topic Name",
  "category": "Security|LLM Concepts|Application-Specific|Development|Advanced",
  "keywords": ["keyword1", "keyword2"],
  "summary": "Brief summary for index listing (max 200 chars)",
  "knowledgeGapLikelihood": "low|medium|high",
  "lastUpdated": "YYYY-MM-DD",
  "lastChecked": "YYYY-MM-DD",
  "content": {
    "overview": "High-level explanation (1-4 sentences)",
    "keyPoints": ["Critical principle 1", "Critical principle 2"],
    "examples": [
      {
        "title": "Example Title",
        "code": "Optional code snippet",
        "explanation": "What this demonstrates"
      }
    ],
    "warnings": ["Critical warning 1", "Critical warning 2"],
    "bestPractices": ["Recommended approach 1"],
    "relatedTopics": ["related-topic-id-1"],
    "references": [
      {
        "title": "External Resource",
        "url": "https://example.com"
      }
    ]
  }
}
```

### Field Descriptions

| Field | Required | Description |
|-------|----------|-------------|
| `id` | ✅ | Unique kebab-case identifier (must match filename) |
| `topic` | ✅ | Human-readable topic name |
| `category` | ✅ | Organization category |
| `keywords` | ❌ | Search keywords (3-7 recommended) |
| `summary` | ✅ | Brief summary (max 200 chars) |
| `knowledgeGapLikelihood` | ✅ | `low`, `medium`, or `high` |
| `lastUpdated` | ✅ | Date content was last modified |
| `lastChecked` | ✅ | Date accuracy was last verified |
| `content.overview` | ✅ | Main explanation (1-4 sentences) |
| `content.keyPoints` | ❌ | Critical bullet points (3-10 recommended) |
| `content.examples` | ❌ | Code/config examples (0-3 recommended) |
| `content.warnings` | ❌ | Critical warnings (2-5 recommended) |
| `content.bestPractices` | ❌ | Recommended approaches (often omitted) |
| `content.relatedTopics` | ❌ | Related entry IDs (0-5 recommended) |
| `content.references` | ❌ | External resources |

### Knowledge Gap Likelihood

| Value | Meaning | LLM Behavior |
|-------|---------|--------------|
| `low` | LLM's training data is current and accurate | Rely on inner knowledge confidently |
| `medium` | LLM's knowledge may be partially outdated | Cross-reference with library, consider web search |
| `high` | LLM's knowledge is very likely outdated | Prefer web search if available; warn user if not |

---

## System Prompt Integration

### TeachingModePromptBuilder

**File**: `TransparentAiAgentCore/Application/Teaching/TeachingModePromptBuilder.cs`

**Purpose**: Dynamically builds the knowledge library section of the teaching mode system prompt.

**Generated Prompt Section**:
```
# Knowledge Library - Guardrails for Teaching

You have access to a knowledge library containing GUARDRAILS for teaching critical concepts.
These entries provide essential principles, red lines, and corrections - NOT comprehensive documentation.

**How to use:**
1. Query the library to get critical guardrails on a topic
2. Use the guardrails to guide your teaching
3. Fill in details using your built-in knowledge
4. The library tells you what's CRITICAL; you provide the comprehensive teaching

**Available knowledge topics:**
- api-key-security: Critical security principles for API keys (gap likelihood: low)
- context-windows: Key facts about context limits (gap likelihood: low)
- mcp-overview: Essential MCP concepts (gap likelihood: high)
... (all discovered topics)

**When to query the library:**
1. Teaching security, privacy, or safety-critical topics (ALWAYS query for guardrails)
2. Application-specific features (context windows, MCP, teaching mode)
3. When you need to correct potential misconceptions
4. Before teaching best practices (get the "must-dos" and "must-not-dos")

**After querying:**
- Treat the entry as GUARDRAILS, not exhaustive content
- Teach comprehensively using the guardrails + your knowledge
- Always respect warnings and red lines from the library
- Use your judgment to expand on principles with relevant details

**Understanding Knowledge Gap Likelihood:**
- **Low**: Your training data is likely current - rely on inner knowledge confidently
- **Medium**: Your knowledge may be partially outdated - cross-reference with library
- **High**: Your knowledge is very likely outdated - prefer web search if available
```

**Integration Point**: `AppModeService.cs` appends this section when teaching mode is active.

---

## Usage Flow

### 1. Startup

```csharp
// DI Configuration (Program.cs)
services.AddSingleton<IKnowledgeLibrary>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<JsonKnowledgeLibrary>>();
    var knowledgeBasePath = Path.Combine(env.WebRootPath, "knowledge");
    return new JsonKnowledgeLibrary(knowledgeBasePath, logger);
});

services.AddSingleton<IToolRegistry, BuiltInKnowledgeToolRegistry>();
services.AddScoped<IToolExecutor, KnowledgeLibraryToolExecutor>();
services.AddSingleton<TeachingModePromptBuilder>();
```

**Result**: Knowledge library index loaded, tool registered, ready for queries.

### 2. Teaching Mode Activation

```csharp
// AppModeService builds system prompt
var promptBuilder = serviceProvider.GetRequiredService<TeachingModePromptBuilder>();
var knowledgeSection = promptBuilder.BuildKnowledgeLibrarySection();

systemPrompt += "\n\n" + knowledgeSection;
```

**Result**: LLM sees available topics and usage guidance.

### 3. LLM Queries Library

**LLM Decision**:
```
User asks about API key security
→ LLM sees "api-key-security" in available topics
→ LLM decides to query library for guardrails
```

**Tool Call**:
```json
{
  "id": "toolu_01ABC",
  "type": "tool_use",
  "name": "knowledge_library_query",
  "input": {
    "topic": "api-key-security"
  }
}
```

### 4. Tool Execution

**Flow**:
1. ToolManager routes to KnowledgeLibraryToolExecutor
2. Executor queries IKnowledgeLibrary.GetTopic("api-key-security")
3. JsonKnowledgeLibrary returns cached or loads from file
4. Executor formats entry as Markdown
5. ToolExecutionResult.Success(formattedMarkdown) returned

### 5. LLM Teaching

**LLM receives**:
```markdown
# API Key Security

**Category:** Security
**Knowledge Gap Likelihood:** low

## Overview
API keys are sensitive credentials...

## Key Points
- NEVER commit API keys to version control
- Store in environment variables or secure vaults only
- Rotate immediately if exposed

## ⚠️ Warnings
- Never log API keys (even partially masked)
...
```

**LLM response**:
```
Let me explain API key security. The most critical rule is to NEVER commit API keys
to version control - this is the #1 mistake developers make. When you hardcode a key
like "sk-ant-123..." in your code, it will be committed to git and potentially exposed
publicly.

Instead, you should store API keys in environment variables or secure vaults like
Azure Key Vault. Here's how you can do that in C#:

[LLM expands with comprehensive teaching based on guardrails]
```

**Result**: Teaching is grounded by guardrails but comprehensive using LLM's full knowledge.

---

## Error Scenarios

### Topic Not Found

**Tool Call**:
```json
{
  "name": "knowledge_library_query",
  "input": { "topic": "nonexistent-topic" }
}
```

**Tool Result**:
```json
{
  "is_error": true,
  "content": "Topic 'nonexistent-topic' not found in knowledge library"
}
```

**LLM Behavior**: Falls back to built-in knowledge with caveat about checking library.

### Corrupted Entry File

**Scenario**: `api-key-security.json` has invalid JSON

**Behavior**:
- Startup: Entry skipped, error logged, index built without it
- Runtime query: Returns null (topic not found)
- Other entries: Continue working normally

**Graceful Degradation**: One bad entry doesn't break entire library.

### Missing Required Parameter

**Tool Call**:
```json
{
  "name": "knowledge_library_query",
  "input": {}
}
```

**Tool Result**:
```json
{
  "is_error": true,
  "content": "Missing required parameter 'topic'"
}
```

---

## Testing

### Unit Tests

**Files**:
- `KnowledgeLibraryToolTests.cs` - Tool schema and metadata
- `KnowledgeLibraryToolExecutorTests.cs` - Execution and formatting
- `BuiltInKnowledgeToolRegistryTests.cs` - Registry operations
- `JsonKnowledgeLibraryTests.cs` - Library implementation

**Coverage**:
- Tool execution with valid/invalid topics
- Markdown formatting correctness
- Error handling (missing params, not found, corrupted files)
- Caching and lazy loading
- Thread-safety (concurrent queries)

### Integration Tests

**File**: `KnowledgeLibraryIntegrationTests.cs`

**Tests**:
- Complete flow: startup → query → formatted result
- System prompt includes all topics
- Tool available in teaching mode
- Concurrent queries (load test)

---

## Performance Considerations

### Startup Performance

- **Index loading**: 50 entries ~10ms
- **Directory scan**: Linear with file count
- **Parsing**: System.Text.Json (fast)

**Recommendation**: <100 entries for optimal startup time.

### Runtime Performance

- **GetTopic (cached)**: <1ms
- **GetTopic (from disk)**: <5ms
- **Formatting**: <1ms

**Caching Strategy**: Entries cached indefinitely (until app restart).

### Memory Usage

- **Index**: ~1KB per entry summary (50 entries = ~50KB)
- **Full entries**: ~2-5KB per entry (only if queried)
- **Cache**: Unbounded (grows with unique queries)

**Recommendation**: Acceptable for <200 entries. Monitor cache size if library grows significantly.

---

## Security Considerations

### File System Access

- **Read-only**: Library only reads from wwwroot/knowledge
- **No write operations**: Entries updated via deployment only
- **Path validation**: Prevents directory traversal (uses Path.Combine)

### Content Trust

- **Code review**: All entries go through PR review process
- **Version control**: Git provides audit trail
- **Validation**: JSON schema validation (when implemented)

### Denial of Service

- **Startup**: Index loading is bounded
- **Runtime**: Lazy loading prevents memory exhaustion
- **Caching**: Cache unbounded - could be issue with millions of unique queries (unlikely)

---

## Maintenance

### Adding New Entries

See [Adding Knowledge Entries Guide](../../../05-guides/development/adding-knowledge-entries.md).

**Summary**:
1. Create JSON file in `wwwroot/knowledge/entries/{topic-id}.json`
2. Validate against checklist
3. Test with manual query
4. Commit and push
5. Restart app to load new entry

### Updating Existing Entries

1. Edit JSON file
2. Update `lastUpdated` field (if content changed)
3. Update `lastChecked` field (if just verified)
4. Test changes
5. Commit with descriptive message

**Note**: Requires app restart to see changes.

### Deprecating Entries

1. Add deprecation notice to overview
2. Update related topics to remove references
3. After verification period, delete file
4. Commit deletion

---

## Future Enhancements

### Hot Reload (Post-MVP)

**Goal**: Update entries without restarting app

**Approach**: FileSystemWatcher on entries directory
- Detects file changes/additions/deletions
- Reloads index and clears cache
- Logs reload events

### Category-Based Browsing (If library grows >50 topics)

**New Tool**:
```json
{
  "name": "knowledge_library_list_topics",
  "input": {
    "category": "Security"
  }
}
```

**Reduces**: System prompt length when library is large.

### Search Functionality

**Goal**: Free-text search across entries

**Requires**: Keyword indexing, relevance scoring
**Benefit**: LLM can discover topics without exact ID

### Admin UI

**Goal**: Edit entries via web interface

**Benefit**: Non-developers can update content
**Cost**: Significant implementation effort

---

## Related Documentation

- [Knowledge Library Concept](../../../03-concepts/knowledge-library.md) - Philosophy and design
- [Adding Knowledge Entries](../../../05-guides/development/adding-knowledge-entries.md) - Step-by-step guide
- [Tool Manager](../tool-manager.md) - Tool orchestration
- [Teaching Mode](../../../03-concepts/teaching-mode/vision.md) - Teaching mode overview
- `KNOWLEDGE_LIBRARY_DESIGN.md` (project root) - Detailed design
- `KNOWLEDGE_LIBRARY_IMPLEMENTATION_PLAN.md` (project root) - Implementation sessions

---

**See Also**: [Component Overview](../../README.md)
