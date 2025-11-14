# Knowledge Library - Detailed Design

**Status**: Design & Brainstorming
**Created**: 2025-11-14
**Related**: docs/03-concepts/teaching-mode/future-enhancements.md (Enhancement 2)

---

## Purpose of This Document

This document serves as a detailed design and brainstorming space for the Knowledge Library feature. It's intentionally more detailed than the high-level concept in the future enhancements doc, to enable rapid iteration and crash-resistant progress.

**Why separate from other docs?**
- Living document for active design work
- Captures decisions and trade-offs as they happen
- Survives session crashes (commit often!)
- Focuses on implementation details, not just concepts

---

## Core Concept

The Knowledge Library is a curated collection of information that the teaching agent can access via built-in tools. It serves as one of 2-3 knowledge sources available to the model:

1. **Inner Knowledge** - The model's training data and built-in knowledge
2. **Knowledge Library** - Curated, application-specific information (this system)
3. **Web Search** - Live information from the internet

### Key Principle: LLM-First Design

**Critical insight**: The library is primarily accessed by the LLM via tools, not by users directly. This fundamentally changes the design:

- ❌ Not a traditional UI-first knowledge base with filters, categories, pagination
- ✅ Tool-first design: simple, focused retrieval for LLM consumption
- ✅ User never directly "browses" entries (at least not initially)
- ✅ LLM receives information and presents it to user in teaching context

---

## Design Decisions & Open Questions

### 1. Storage: One File vs. Many Files

**Trade-offs:**

| Aspect | Single File | Multiple Files |
|--------|-------------|----------------|
| **Organization** | ✅ Simple - one file to manage | ❌ Many files to track |
| **Failure Mode** | ❌ One corrupted entry breaks everything | ✅ Isolated failures |
| **Performance** | ⚠️ Load all entries at startup | ✅ Can lazy-load or index |
| **Version Control** | ❌ Every change modifies same file (merge conflicts) | ✅ Changes isolated to specific files |
| **Editing** | ✅ Easy to see all entries | ❌ Need to navigate between files |
| **Validation** | ❌ Parse entire file to validate | ✅ Validate entries individually |

**Initial Thoughts:**
- User leans toward separate files for robustness
- But acknowledged that "should not get through reviews and UTs though"
- This suggests: **separate files with strong validation**

**Proposal: Hybrid Approach**
```
wwwroot/knowledge/
├── index.json                 # Lightweight index: just IDs, topics, keywords
├── entries/
│   ├── api-key-security.json
│   ├── context-windows.json
│   ├── mcp-overview.json
│   └── ... (one file per entry)
└── _schema.json               # JSON schema for validation
```

**Benefits:**
- ✅ Isolated failures (one bad entry doesn't break library)
- ✅ `index.json` is small, easy to parse, low risk
- ✅ Entries loaded on-demand (or cached)
- ✅ Version control friendly
- ✅ Easy to add/remove entries without touching other files
- ✅ Can still validate each entry against schema

**Implementation Note:** If parsing an entry fails, log error and skip it. Library remains functional with other entries.

---

### 2. KnowledgeEntry Structure: Simplification for LLM Tools

Original proposal (from future-enhancements.md):
```json
{
  "id": "api-key-security",
  "topic": "API Key Security",
  "category": "Security",
  "keywords": ["api", "key", "security", ...],
  "summary": "...",
  "content": {
    "overview": "...",
    "keyPoints": [...],
    "examples": [...],
    "warnings": [...],
    "relatedTopics": [...]
  }
}
```

**Concern:** "Knowledge entry is quite sophisticated, maybe too much."

**Analysis:**

The LLM tool interface for this would be:
```json
{
  "name": "knowledge_library_query",
  "description": "Query knowledge library for teaching information",
  "inputSchema": {
    "type": "object",
    "properties": {
      "topic": { "type": "string", "description": "Topic ID or search query" }
      // Optional filters here?
    }
  }
}
```

**Question:** Do we need sophisticated filtering in the tool?
- ❌ Probably not: "Having tons of input parameters for filtering etc is not best"
- ✅ LLM receives **list of all entries** (via index) and decides what to query
- ✅ Tool just retrieves one entry at a time by ID

**Simplified Tool Flow:**
1. **System/Teaching Prompt**: Includes available knowledge topics (from index.json)
   ```
   Available knowledge topics:
   - api-key-security: API Key Security
   - context-windows: Context Windows and Message Limits
   - mcp-overview: Model Context Protocol Overview
   ...
   ```
2. **LLM Decision**: Model sees topic list, decides what's relevant
3. **Tool Call**: `knowledge_library_query(topic="api-key-security")`
4. **Response**: Full entry content returned
5. **Teaching**: Model uses content to teach user

**Implication for "category" field:**
- ✅ **Keep it** - Low cost, enables future hierarchical access if needed
- ✅ Can be shown in index for context
- ❌ Don't build category-based retrieval yet
- ⚠️ Design note: Category is metadata, not a query parameter (for now)

**Revised Entry Structure (Slightly Simplified):**
```json
{
  "id": "api-key-security",
  "topic": "API Key Security",
  "category": "Security",                    // Keep for future use
  "keywords": ["api", "key", "security"],    // Keep for search/index
  "summary": "Best practices for API keys",  // Show in index
  "content": {
    "overview": "...",                       // Main explanation
    "keyPoints": [...],                      // Bullet points
    "examples": [...],                       // Code/config examples
    "warnings": [...],                       // Important cautions
    "relatedTopics": [...]                   // IDs of related entries
  }
}
```

**Rationale:**
- Structure is rich enough to be useful but not overcomplicated
- LLM can easily parse and present information
- Fits well with tool-based retrieval
- Future-proof: can add fields later without breaking existing entries

---

### 3. Tool Design: Minimal Parameters, Maximum Flexibility

**Proposed Tool Signature:**
```json
{
  "name": "knowledge_library_query",
  "description": "Retrieve detailed information from the knowledge library on a specific topic. Use this when teaching critical concepts (security, privacy, best practices) to ensure accuracy.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "topic": {
        "type": "string",
        "description": "The topic ID to query (e.g., 'api-key-security'). Available topics are listed in your system instructions."
      }
    },
    "required": ["topic"]
  }
}
```

**Why minimal?**
- ✅ Aligns with user's concern: "having tons of input parameters for filtering etc is not best"
- ✅ LLM already has the index (from system prompt) - can make smart decisions
- ✅ Simple tool = less room for error
- ✅ Can add optional parameters later if truly needed (e.g., `includeExamples: boolean`)

**Alternative Considered:**
```json
{
  "properties": {
    "query": { "type": "string" },  // Free-text search
    "category": { "type": "string" },
    "maxResults": { "type": "number" }
  }
}
```
**Rejected because:**
- Too complex for initial version
- Free-text search needs indexing/scoring logic
- Prefer explicit topic selection from known list

---

### 4. System Prompt Integration: How Does LLM Know What's Available?

**Challenge:** LLM needs to know what topics exist in the library to query them effectively.

**Solution: Dynamic System Prompt Section**

When teaching mode is active, inject a section into the system prompt:

```text
# Knowledge Library

You have access to a curated knowledge library for teaching. When teaching critical concepts,
use the `knowledge_library_query` tool to retrieve accurate, comprehensive information.

Available knowledge topics:
- api-key-security: API Key Security Best Practices
- context-windows: Context Windows and Message Limits
- mcp-overview: Model Context Protocol Overview
- tool-security: Secure Tool Usage
- transparency-logging: Understanding Transparency Logs
... (all topics from index.json)

Guidelines for using the knowledge library:
1. Query the library when teaching security, privacy, or best practices
2. Present information in a friendly, accessible way
3. Include examples when available
4. Always mention warnings/cautions from the library
5. Suggest related topics to deepen learning

Critical topics that REQUIRE knowledge library lookup:
- API key handling → query "api-key-security"
- Security best practices → query "tool-security"
- Context management → query "context-windows"
```

**Implementation:**
- `index.json` loaded at startup
- System prompt dynamically built from index
- If index changes, prompt updates on next conversation (or reload)

**Scaling Consideration:**
- What if we have 100+ topics? Won't fit in prompt.
- **Solution for later**: Category-based exposure
  ```text
  Available categories: Security, Tools, Transparency, Configuration
  Use knowledge_library_list_topics(category="Security") to see topics in a category.
  ```
- **For now**: Keep it simple, assume <50 topics (fits easily in prompt)

---

### 5. Entry Content Format: What Goes in "content"?

**Goal:** Provide LLM with structured information it can easily teach from.

**Proposed Content Structure:**
```json
{
  "content": {
    "overview": "String: High-level explanation of the topic (2-3 sentences)",
    "keyPoints": [
      "String: Important point 1",
      "String: Important point 2",
      "..."
    ],
    "examples": [
      {
        "title": "String: Example name",
        "code": "String: Code snippet or configuration",
        "explanation": "String: What this example demonstrates"
      }
    ],
    "warnings": [
      "String: Critical warning or caution",
      "..."
    ],
    "bestPractices": [
      "String: Recommended approach or pattern",
      "..."
    ],
    "relatedTopics": ["topic-id-1", "topic-id-2"],
    "references": [
      {
        "title": "String: External resource name",
        "url": "String: Optional URL"
      }
    ]
  }
}
```

**Optional Fields:**
- All fields except `overview` are optional
- LLM adapts based on what's present
- Allows minimal entries for simple topics, rich entries for complex ones

**Example Entry: api-key-security.json**
```json
{
  "id": "api-key-security",
  "topic": "API Key Security",
  "category": "Security",
  "keywords": ["api", "key", "secrets", "security", "authentication", "environment-variables"],
  "summary": "Best practices for handling API keys securely in applications",
  "content": {
    "overview": "API keys are sensitive credentials that authenticate your application to external services. Mishandling them can lead to security breaches, unauthorized access, and financial losses. Proper API key management is a fundamental security practice.",
    "keyPoints": [
      "Never commit API keys to version control (Git, SVN, etc.)",
      "Store keys in environment variables or secure vaults (e.g., Azure Key Vault)",
      "Rotate keys regularly and immediately if compromised",
      "Limit key permissions to the minimum required (principle of least privilege)",
      "Monitor key usage for anomalies or unexpected patterns",
      "Use different keys for development, staging, and production environments"
    ],
    "examples": [
      {
        "title": "Storing API Key in Environment Variable (C#)",
        "code": "var apiKey = Environment.GetEnvironmentVariable(\"ANTHROPIC_API_KEY\");\nif (string.IsNullOrEmpty(apiKey))\n{\n    throw new InvalidOperationException(\"API key not configured\");\n}",
        "explanation": "This approach keeps the key out of source code. The key is configured in the environment (e.g., via appsettings.json user secrets, Azure App Service settings, or .env files with .gitignore)."
      },
      {
        "title": "Using .NET User Secrets (Development)",
        "code": "# Set secret\ndotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\"\n\n# Access in code\nvar apiKey = configuration[\"Anthropic:ApiKey\"];",
        "explanation": "User Secrets store keys outside the project directory, preventing accidental commits. Ideal for local development."
      }
    ],
    "warnings": [
      "Never share API keys in screenshots, demos, or documentation",
      "Never log API keys (even partially) to console or log files",
      "Revoke and rotate keys immediately if compromised or exposed",
      "Be cautious with third-party libraries that might log requests including keys"
    ],
    "bestPractices": [
      "Use a secrets management service (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) for production",
      "Implement key rotation policies (e.g., rotate every 90 days)",
      "Use service accounts with scoped keys rather than personal keys in shared systems",
      "Audit key access and usage regularly",
      "Document where keys are stored and who has access"
    ],
    "relatedTopics": ["environment-variables", "authentication-methods", "security-best-practices"],
    "references": [
      {
        "title": "OWASP API Security Top 10",
        "url": "https://owasp.org/www-project-api-security/"
      },
      {
        "title": "Microsoft - Safe storage of app secrets (User Secrets)",
        "url": "https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets"
      }
    ]
  }
}
```

**Notes:**
- Rich content, but LLM can extract what's relevant for the conversation
- `references` field acknowledges external sources (transparency)
- Structure guides LLM to cover important points without being prescriptive

---

## Technical Design

### Component Architecture

```
Domain Layer (TransparentAiAgentCore):
├── Models/
│   ├── KnowledgeEntry.cs         // Domain model
│   ├── KnowledgeContent.cs       // Content structure
│   └── KnowledgeExample.cs       // Example structure
└── Services/
    └── IKnowledgeLibrary.cs      // Domain service interface

Infrastructure Layer (TransparentAiAgentCore):
├── Knowledge/
│   ├── JsonKnowledgeLibrary.cs   // Reads from JSON files
│   └── KnowledgeIndex.cs         // Loads and caches index

Tools Layer (TransparentAiAgentCore):
├── BuiltInTools/
│   └── KnowledgeLibraryTool.cs   // Implements the tool

UI Layer (TransparentAiAgentGui):
└── (Future: Knowledge browser UI, if needed)
```

### Interface: IKnowledgeLibrary

```csharp
namespace TransparentAiAgent.Domain.Services
{
    /// <summary>
    /// Domain service for accessing the knowledge library.
    /// </summary>
    public interface IKnowledgeLibrary
    {
        /// <summary>
        /// Gets a knowledge entry by ID.
        /// </summary>
        /// <param name="topicId">The unique identifier of the topic.</param>
        /// <returns>The knowledge entry, or null if not found.</returns>
        KnowledgeEntry? GetTopic(string topicId);

        /// <summary>
        /// Gets all available knowledge entry summaries (for index/listing).
        /// </summary>
        /// <returns>List of entry metadata (ID, topic, category, summary).</returns>
        IReadOnlyList<KnowledgeEntrySummary> GetAllTopics();

        /// <summary>
        /// Gets all topics in a specific category.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <returns>List of entry summaries in that category.</returns>
        IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category);

        /// <summary>
        /// Gets all unique categories in the library.
        /// </summary>
        /// <returns>List of category names.</returns>
        IReadOnlyList<string> GetCategories();
    }

    /// <summary>
    /// Lightweight summary of a knowledge entry (for indexing).
    /// </summary>
    public class KnowledgeEntrySummary
    {
        public required string Id { get; init; }
        public required string Topic { get; init; }
        public required string Category { get; init; }
        public required string Summary { get; init; }
        public List<string> Keywords { get; init; } = new();
    }
}
```

**Design Notes:**
- Minimal interface for initial version
- `GetTopic()` is primary method (used by tool)
- `GetAllTopics()` builds the system prompt section
- Category methods prepared but not used initially
- Returns `null` instead of throwing if topic not found (graceful degradation)

### Implementation: JsonKnowledgeLibrary

```csharp
namespace TransparentAiAgent.Infrastructure.Knowledge
{
    public class JsonKnowledgeLibrary : IKnowledgeLibrary
    {
        private readonly string _knowledgeBasePath;
        private readonly Dictionary<string, KnowledgeEntry> _entriesCache = new();
        private readonly List<KnowledgeEntrySummary> _index = new();
        private readonly ILogger<JsonKnowledgeLibrary> _logger;

        public JsonKnowledgeLibrary(
            IWebHostEnvironment env,
            ILogger<JsonKnowledgeLibrary> logger)
        {
            _logger = logger;
            _knowledgeBasePath = Path.Combine(env.WebRootPath, "knowledge");
            LoadIndex();
        }

        private void LoadIndex()
        {
            try
            {
                var indexPath = Path.Combine(_knowledgeBasePath, "index.json");
                if (!File.Exists(indexPath))
                {
                    _logger.LogWarning("Knowledge library index not found at {Path}", indexPath);
                    return;
                }

                var json = File.ReadAllText(indexPath);
                var indexData = JsonSerializer.Deserialize<KnowledgeIndexFile>(json);

                if (indexData?.Entries != null)
                {
                    _index.AddRange(indexData.Entries);
                    _logger.LogInformation("Loaded {Count} knowledge topics from index", _index.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load knowledge library index");
            }
        }

        public KnowledgeEntry? GetTopic(string topicId)
        {
            // Check cache first
            if (_entriesCache.TryGetValue(topicId, out var cached))
            {
                return cached;
            }

            // Load from file
            try
            {
                var entryPath = Path.Combine(_knowledgeBasePath, "entries", $"{topicId}.json");
                if (!File.Exists(entryPath))
                {
                    _logger.LogWarning("Knowledge entry not found: {TopicId}", topicId);
                    return null;
                }

                var json = File.ReadAllText(entryPath);
                var entry = JsonSerializer.Deserialize<KnowledgeEntry>(json);

                // Validate
                if (entry == null || entry.Id != topicId)
                {
                    _logger.LogError("Invalid knowledge entry file: {TopicId}", topicId);
                    return null;
                }

                // Cache and return
                _entriesCache[topicId] = entry;
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load knowledge entry: {TopicId}", topicId);
                return null;
            }
        }

        public IReadOnlyList<KnowledgeEntrySummary> GetAllTopics() => _index.AsReadOnly();

        public IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category)
        {
            return _index.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                         .ToList()
                         .AsReadOnly();
        }

        public IReadOnlyList<string> GetCategories()
        {
            return _index.Select(e => e.Category)
                         .Distinct()
                         .OrderBy(c => c)
                         .ToList()
                         .AsReadOnly();
        }
    }

    internal class KnowledgeIndexFile
    {
        public List<KnowledgeEntrySummary> Entries { get; set; } = new();
    }
}
```

**Implementation Notes:**
- Lazy-load entries (only load when queried)
- Cache entries in memory after first load
- Graceful error handling (bad entry doesn't crash library)
- Index loaded at startup for fast topic listing

### Tool Implementation: KnowledgeLibraryTool

```csharp
namespace TransparentAiAgent.Core.Tools.BuiltIn
{
    public class KnowledgeLibraryTool : IBuiltInTool
    {
        private readonly IKnowledgeLibrary _library;

        public KnowledgeLibraryTool(IKnowledgeLibrary library)
        {
            _library = library;
        }

        public string Name => "knowledge_library_query";

        public string Description =>
            "Retrieve detailed information from the knowledge library on a specific topic. " +
            "Use this when teaching critical concepts (security, privacy, best practices) to ensure accuracy.";

        public object InputSchema => new
        {
            type = "object",
            properties = new
            {
                topic = new
                {
                    type = "string",
                    description = "The topic ID to query (e.g., 'api-key-security'). Available topics are listed in your system instructions."
                }
            },
            required = new[] { "topic" }
        };

        public async Task<ToolResult> ExecuteAsync(
            Dictionary<string, object> arguments,
            CancellationToken cancellationToken)
        {
            if (!arguments.TryGetValue("topic", out var topicObj) || topicObj is not string topicId)
            {
                return ToolResult.Error("Missing or invalid 'topic' parameter");
            }

            var entry = _library.GetTopic(topicId);
            if (entry == null)
            {
                return ToolResult.Error($"Topic '{topicId}' not found in knowledge library");
            }

            // Format entry for LLM consumption
            var formattedContent = FormatKnowledgeEntry(entry);
            return ToolResult.Success(formattedContent);
        }

        private string FormatKnowledgeEntry(KnowledgeEntry entry)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {entry.Topic}");
            sb.AppendLine();
            sb.AppendLine($"**Category:** {entry.Category}");
            sb.AppendLine();

            // Overview
            sb.AppendLine("## Overview");
            sb.AppendLine(entry.Content.Overview);
            sb.AppendLine();

            // Key Points
            if (entry.Content.KeyPoints?.Any() == true)
            {
                sb.AppendLine("## Key Points");
                foreach (var point in entry.Content.KeyPoints)
                {
                    sb.AppendLine($"- {point}");
                }
                sb.AppendLine();
            }

            // Examples
            if (entry.Content.Examples?.Any() == true)
            {
                sb.AppendLine("## Examples");
                foreach (var example in entry.Content.Examples)
                {
                    sb.AppendLine($"### {example.Title}");
                    if (!string.IsNullOrEmpty(example.Code))
                    {
                        sb.AppendLine("```");
                        sb.AppendLine(example.Code);
                        sb.AppendLine("```");
                    }
                    sb.AppendLine(example.Explanation);
                    sb.AppendLine();
                }
            }

            // Warnings
            if (entry.Content.Warnings?.Any() == true)
            {
                sb.AppendLine("## ⚠️ Warnings");
                foreach (var warning in entry.Content.Warnings)
                {
                    sb.AppendLine($"- {warning}");
                }
                sb.AppendLine();
            }

            // Best Practices
            if (entry.Content.BestPractices?.Any() == true)
            {
                sb.AppendLine("## Best Practices");
                foreach (var practice in entry.Content.BestPractices)
                {
                    sb.AppendLine($"- {practice}");
                }
                sb.AppendLine();
            }

            // Related Topics
            if (entry.Content.RelatedTopics?.Any() == true)
            {
                sb.AppendLine("## Related Topics");
                sb.AppendLine($"For more information, you can also query: {string.Join(", ", entry.Content.RelatedTopics)}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
```

**Notes:**
- Formats entry as Markdown for easy LLM consumption
- Includes all sections that exist in the entry
- Related topics hint at potential follow-up queries

---

## System Prompt Integration

### Teaching Mode Prompt Addition

When teaching mode is active, dynamically inject this section:

```csharp
public class TeachingModePromptBuilder
{
    private readonly IKnowledgeLibrary _library;

    public string BuildKnowledgeLibrarySection()
    {
        var topics = _library.GetAllTopics();
        var sb = new StringBuilder();

        sb.AppendLine("# Knowledge Library");
        sb.AppendLine();
        sb.AppendLine("You have access to a curated knowledge library for teaching. When teaching critical concepts,");
        sb.AppendLine("use the `knowledge_library_query` tool to retrieve accurate, comprehensive information.");
        sb.AppendLine();
        sb.AppendLine("**Available knowledge topics:**");

        foreach (var topic in topics)
        {
            sb.AppendLine($"- `{topic.Id}`: {topic.Topic} - {topic.Summary}");
        }

        sb.AppendLine();
        sb.AppendLine("**Guidelines for using the knowledge library:**");
        sb.AppendLine("1. Query the library when teaching security, privacy, or best practices");
        sb.AppendLine("2. Present information in a friendly, accessible way");
        sb.AppendLine("3. Include examples when available");
        sb.AppendLine("4. Always mention warnings/cautions from the library");
        sb.AppendLine("5. Suggest related topics to deepen learning");
        sb.AppendLine();
        sb.AppendLine("**Critical topics that REQUIRE knowledge library lookup:**");
        sb.AppendLine("- API key handling → `api-key-security`");
        sb.AppendLine("- Security best practices → `tool-security`");
        sb.AppendLine("- Context management → `context-windows`");
        sb.AppendLine("- MCP tool usage → `mcp-overview`");

        return sb.ToString();
    }
}
```

This section is appended to the base teaching mode prompt when the conversation starts.

---

## File Structure on Disk

```
TransparentAiAgentGui/
└── wwwroot/
    └── knowledge/
        ├── index.json                     # Master index (lightweight)
        ├── entries/                       # Individual entry files
        │   ├── api-key-security.json
        │   ├── context-windows.json
        │   ├── mcp-overview.json
        │   ├── tool-security.json
        │   ├── transparency-logging.json
        │   ├── authentication-methods.json
        │   ├── environment-variables.json
        │   └── security-best-practices.json
        ├── _schema.json                   # JSON Schema for validation (dev tool)
        └── README.md                      # Explains the library structure
```

### index.json Example

```json
{
  "version": "1.0",
  "lastUpdated": "2025-11-14",
  "entries": [
    {
      "id": "api-key-security",
      "topic": "API Key Security",
      "category": "Security",
      "summary": "Best practices for handling API keys securely in applications",
      "keywords": ["api", "key", "secrets", "security", "authentication"]
    },
    {
      "id": "context-windows",
      "topic": "Context Windows and Message Limits",
      "category": "LLM Concepts",
      "summary": "Understanding how conversation context works and managing message history",
      "keywords": ["context", "window", "messages", "limits", "truncation"]
    },
    {
      "id": "mcp-overview",
      "topic": "Model Context Protocol Overview",
      "category": "Tools",
      "summary": "Introduction to MCP and how it enables tool integration",
      "keywords": ["mcp", "tools", "protocol", "integration"]
    }
  ]
}
```

**Design Notes:**
- Version field for future schema evolution
- Index is the source of truth for what topics exist
- Summaries shown in system prompt
- Keywords could be used for future search features

---

## Initial Knowledge Topics (Seed Content)

### Priority 1: Critical Security Topics
1. **api-key-security** - API key handling (MUST HAVE)
2. **tool-security** - Secure MCP tool usage
3. **environment-variables** - Configuration and secrets management
4. **authentication-methods** - Auth approaches (OAuth, API keys, etc.)

### Priority 2: Core LLM Concepts
5. **context-windows** - Context limits and message truncation
6. **token-limits** - Token counting and rate limits
7. **prompt-engineering** - Effective prompting techniques

### Priority 3: Application-Specific
8. **mcp-overview** - MCP introduction
9. **transparency-logging** - How transparency features work
10. **teaching-mode** - What is teaching mode and how to use it

### Priority 4: Development Topics
11. **configuration-management** - App configuration best practices
12. **error-handling** - Graceful error handling patterns
13. **testing-best-practices** - How to test agent interactions

### Priority 5: Advanced Topics
14. **streaming-responses** - How streaming works
15. **conversation-management** - Managing long conversations

**Initial Target:** 10-15 high-quality entries to prove the concept.

---

## Open Questions & Brainstorming

### Question 1: How to Handle Knowledge Updates?

**Scenario:** A knowledge entry needs to be updated (e.g., new security best practice, API change).

**Options:**
1. **Manual Update**: Developer edits JSON file, commits to repo
   - ✅ Simple, version-controlled
   - ❌ Requires code deployment to update

2. **Hot Reload**: Watch file system, reload index/entries when changed
   - ✅ No deployment needed for knowledge updates
   - ❌ More complex, cache invalidation issues

3. **Admin UI**: Build UI for editing entries
   - ✅ Non-developers can update
   - ❌ Significant effort, storage complexity

**Recommendation for Initial Version:** Option 1 (Manual Update)
- Keep it simple
- Knowledge updates are infrequent enough to tolerate deployments
- Benefit: All updates go through code review (quality control)

**Future:** Could add hot reload or admin UI if knowledge update frequency becomes a bottleneck.

---

### Question 2: Versioning of Knowledge Entries?

**Scenario:** Entry content changes over time. Should we track versions?

**Arguments For:**
- Could analyze which version was used in a specific conversation
- Could revert bad updates
- Historical record of changes

**Arguments Against:**
- Adds complexity (storage, retrieval, UI)
- Git already provides version history
- Unlikely to need this for MVP

**Recommendation:** No versioning in initial implementation. Git commit history is sufficient.

---

### Question 3: User-Facing Knowledge Browser?

**Scenario:** Should users be able to browse the knowledge library directly (not just via LLM)?

**Arguments For:**
- Users could learn without asking the agent
- Transparency: users see what information the agent has
- Could be a reference resource

**Arguments Against:**
- Extra UI work
- Duplicates teaching function (agent should teach)
- Knowledge is LLM-optimized, not necessarily user-optimized

**Recommendation:** NOT for initial version. Focus on LLM-mediated teaching. Could add browser later as a "knowledge base" feature.

---

### Question 4: Should LLM Be Able to List Topics by Category?

**Scenario:** Knowledge library grows to 50+ topics. System prompt too long to list all.

**Current Approach:** List all topics in system prompt
**Alternative:** Introduce category browsing

**Proposed Tool (Future):**
```json
{
  "name": "knowledge_library_list_topics",
  "inputSchema": {
    "properties": {
      "category": { "type": "string", "description": "Optional: filter by category" }
    }
  }
}
```

**Tool Flow:**
1. LLM: "Let me check what security topics are available"
2. Tool Call: `knowledge_library_list_topics(category="Security")`
3. Returns: List of security-related topics
4. LLM: Picks one and queries it

**Decision:** Implement this ONLY if we exceed ~40-50 topics (system prompt becomes too long). Otherwise, keep it simple.

---

### Question 5: Multi-Language Support?

**Scenario:** Support knowledge entries in multiple languages?

**Complexity:**
- Translation of entries
- Language detection
- Consistent terminology across languages

**Recommendation:** English only for initial version. Internationalization is a separate feature.

---

### Question 6: Entry Validation and Quality Control?

**Concern:** How to ensure knowledge entries are high-quality and accurate?

**Proposed Validation Layers:**

1. **JSON Schema Validation** (Automated):
   - Validate structure against `_schema.json`
   - Unit tests: all entries must parse and validate

2. **Content Guidelines** (Manual):
   - Document in `wwwroot/knowledge/README.md`
   - Standards for tone, length, examples
   - Review checklist for new entries

3. **Code Review** (Process):
   - All knowledge updates go through PR review
   - Reviewer checks for accuracy, completeness, clarity

4. **Testing with LLM** (Semi-Automated):
   - Test conversations where agent uses each entry
   - Verify agent teaches correctly based on entry content

**Recommendation:** Implement #1 (schema validation) and #3 (code review) immediately. #2 (guidelines doc) as first task. #4 (LLM testing) for later.

---

### Question 7: How to Handle "Related Topics" Links?

**Scenario:** Entry mentions related topics. How does LLM use them?

**Current Design:** Entry includes `relatedTopics: ["topic-id-1", "topic-id-2"]`

**LLM Behavior Options:**
1. **Mention Only**: LLM tells user "You might also be interested in X"
   - Simple, non-intrusive

2. **Automatic Pre-Load**: LLM queries related topics proactively
   - ❌ Could be overwhelming, unnecessary tokens

3. **User-Driven**: LLM asks "Would you like to learn about X?"
   - ✅ User controls depth

**Recommendation:** Option 3 (User-Driven). Related topics are suggestions, not automatic.

**Implementation:** In system prompt:
```text
When you finish teaching from a knowledge entry, if there are related topics,
suggest them to the user: "Would you also like to learn about [topic]?"
Only query related topics if the user expresses interest.
```

---

### Question 8: Entry Length - How Much is Too Much?

**Concern:** Long entries consume tokens and might overwhelm the LLM.

**Guidelines:**
- **Overview**: 2-4 sentences max
- **Key Points**: 5-10 bullets max
- **Examples**: 2-4 examples max, each <20 lines of code
- **Warnings**: 3-5 warnings max
- **Best Practices**: 5-8 practices max

**Total Target:** ~500-1000 tokens per entry when formatted

**Rationale:**
- LLM can easily process and teach from this
- Not overwhelming for user when presented
- Specific enough to be useful

**Validation:** Could add a token-counting test to warn if entry exceeds ~1500 tokens.

---

### Question 9: What if LLM Queries Non-Existent Topic?

**Scenario:** LLM calls `knowledge_library_query(topic="nonexistent")`

**Current Behavior:** Tool returns error: "Topic 'nonexistent' not found"

**LLM Response Options:**
1. Admit lack of knowledge: "I don't have that in my library"
2. Fallback to built-in knowledge: "Let me share what I know..."
3. Suggest closest match: "Did you mean 'context-windows'?"

**Recommendation:** Let LLM decide. Tool just returns clear error. LLM can fallback to built-in knowledge gracefully.

**Future Enhancement:** Fuzzy matching in tool to suggest close matches.

---

## Testing Strategy

### Unit Tests

1. **KnowledgeEntry Model**
   - Deserialization from JSON
   - Validation of required fields

2. **JsonKnowledgeLibrary**
   - Load index from file
   - Get topic by ID (success)
   - Get topic by ID (not found) → returns null
   - Get all topics
   - Get topics by category
   - Invalid JSON handling (graceful failure)

3. **KnowledgeLibraryTool**
   - Execute with valid topic → returns formatted content
   - Execute with invalid topic → returns error
   - Execute with missing parameter → returns error

### Integration Tests

1. **System Prompt Integration**
   - Verify knowledge topics appear in teaching mode prompt
   - Verify tool is registered in teaching mode

2. **End-to-End Tool Usage**
   - Simulate agent conversation where tool is called
   - Verify tool result contains expected content
   - Verify agent can teach from result

### Manual Testing

1. **Content Quality**
   - Read through each entry as a user
   - Verify clarity, accuracy, completeness

2. **LLM Teaching**
   - Have real conversations where agent uses library
   - Assess teaching quality
   - Verify agent follows warnings and best practices

---

## Implementation Phases

### Phase 1: Foundation (Days 1-2)
**Goal:** Basic infrastructure working

**Tasks:**
1. Define `KnowledgeEntry`, `KnowledgeContent` models
2. Implement `IKnowledgeLibrary` interface
3. Implement `JsonKnowledgeLibrary` with index loading
4. Create JSON schema (`_schema.json`)
5. Write unit tests for models and library service
6. Set up file structure (`wwwroot/knowledge/`)

**Deliverable:** Can load index and retrieve entries from JSON files

---

### Phase 2: Tool Integration (Day 3)
**Goal:** Tool accessible to LLM

**Tasks:**
1. Implement `KnowledgeLibraryTool`
2. Register tool in teaching mode configuration
3. Unit tests for tool execution
4. Manual test: call tool with mock LLM request

**Deliverable:** Tool works and returns formatted content

---

### Phase 3: System Prompt Integration (Day 4)
**Goal:** LLM knows about knowledge library

**Tasks:**
1. Build dynamic knowledge library section for system prompt
2. Integrate section into teaching mode prompt builder
3. Test that prompt includes all indexed topics
4. Verify prompt length is acceptable

**Deliverable:** Teaching mode prompt includes knowledge library section

---

### Phase 4: Initial Content (Days 5-7)
**Goal:** 10-15 high-quality knowledge entries

**Tasks:**
1. Write `api-key-security.json` (example above)
2. Write `context-windows.json`
3. Write `mcp-overview.json`
4. Write `tool-security.json`
5. Write `transparency-logging.json`
6. Write `environment-variables.json`
7. Write `authentication-methods.json`
8. Write `security-best-practices.json`
9. Write `token-limits.json`
10. Write `teaching-mode.json`
11. Update `index.json` with all entries
12. Validate all entries against schema

**Deliverable:** 10 validated, high-quality knowledge entries

---

### Phase 5: End-to-End Testing (Day 8)
**Goal:** Verify LLM uses library effectively

**Tasks:**
1. Start conversation in teaching mode
2. Ask about API key security
3. Verify agent queries library
4. Verify agent teaches using library content
5. Test multiple topics
6. Test related topics flow
7. Document any issues or improvements

**Deliverable:** Confident that knowledge library improves teaching quality

---

### Phase 6: Polish & Documentation (Day 9)
**Goal:** Production-ready

**Tasks:**
1. Write `wwwroot/knowledge/README.md` (content guidelines)
2. Add logging for library queries (analytics)
3. Error handling review
4. Performance review (caching, index size)
5. Update main docs with knowledge library feature
6. Create maintenance guide (how to add/update entries)

**Deliverable:** Feature complete, documented, ready to ship

---

## Success Criteria

### Functional Requirements
✅ LLM can query knowledge library via tool
✅ Tool returns accurate, formatted content
✅ System prompt lists all available topics
✅ Library gracefully handles missing entries
✅ Index loads at startup without errors

### Quality Requirements
✅ 10+ high-quality knowledge entries created
✅ All entries validate against JSON schema
✅ Entry content is clear, accurate, and comprehensive
✅ Code examples are tested and correct

### Teaching Quality
✅ Agent teaching measurably improved when using library
✅ Critical topics (security) taught consistently
✅ Agent includes warnings from library
✅ Agent suggests related topics appropriately

### Technical Quality
✅ Unit tests cover all components (>80% coverage)
✅ Integration tests verify end-to-end flow
✅ No performance regressions (index + entries load <100ms)
✅ Errors logged but don't crash application

---

## Future Enhancements (Post-MVP)

### Enhancement 1: Category-Based Browsing
- Add `knowledge_library_list_topics(category)` tool
- Use when library grows beyond ~50 topics
- Reduces system prompt length

### Enhancement 2: Knowledge Browser UI
- User-facing UI to browse knowledge entries
- Could be used as a reference without asking agent
- Requires additional UI work

### Enhancement 3: Advanced Search
- Free-text search across entries
- Keyword-based retrieval
- Requires indexing/search logic

### Enhancement 4: User-Contributed Entries
- Allow users to suggest or create entries
- Moderation workflow
- Community knowledge building

### Enhancement 5: Analytics & Optimization
- Track which topics are queried most
- Identify gaps in coverage
- A/B test entry formats for effectiveness

### Enhancement 6: Multi-Language Support
- Translate entries to other languages
- Language detection and selection
- Consistent terminology management

### Enhancement 7: RAG Integration
- Use retrieval-augmented generation for more dynamic content
- Semantic search over entries
- Could replace or augment current keyword-based system

---

## Risks & Mitigations

### Risk 1: Content Quality
**Risk:** Entries contain inaccurate or outdated information
**Impact:** Agent teaches incorrectly, users misled
**Mitigation:**
- Code review for all entries
- Content guidelines and checklist
- Regular audits and updates
- Source attribution in entries

### Risk 2: LLM Doesn't Use Library
**Risk:** Agent relies on built-in knowledge instead of querying library
**Impact:** Library investment wasted, teaching inconsistent
**Mitigation:**
- System prompt explicitly requires library use for critical topics
- Monitor tool usage analytics
- Iterate on prompt wording if needed

### Risk 3: Library Too Large
**Risk:** Too many entries → system prompt too long
**Impact:** Token budget exceeded, can't list all topics
**Mitigation:**
- Start with category-based listing if exceeds ~50 topics
- Lazy-load category → topics → content
- Consider RAG approach for very large libraries

### Risk 4: Maintenance Burden
**Risk:** Keeping entries up-to-date becomes time-consuming
**Impact:** Outdated information, declining quality
**Mitigation:**
- Keep initial scope small (10-15 entries)
- Only add entries for high-value, stable topics
- Schedule quarterly review of entries
- Use Git commit dates to flag old entries

### Risk 5: Parsing Failures
**Risk:** Corrupted JSON breaks library loading
**Impact:** Knowledge library unavailable
**Mitigation:**
- Graceful error handling (skip bad entries, log error)
- Schema validation in CI/CD
- Unit tests for all entries

---

## Comparison to Alternatives

### Alternative 1: Embedding Everything in System Prompt
**Pros:** Simple, no tool needed
**Cons:** Prompt becomes huge, inflexible, hard to maintain
**Decision:** Rejected - knowledge library is more scalable

### Alternative 2: External Knowledge API
**Pros:** Centralized, could serve multiple agents
**Cons:** Adds dependency, latency, complexity
**Decision:** Rejected for MVP - local files sufficient

### Alternative 3: RAG with Vector Database
**Pros:** Dynamic retrieval, scales to large corpora
**Cons:** Complex, requires vector DB, embeddings, semantic search
**Decision:** Deferred - overkill for initial set of <20 entries. Could revisit if library grows significantly.

### Alternative 4: Agent Memory/Learning
**Pros:** Agent learns from interactions, adaptive
**Cons:** Inconsistent, requires complex memory system
**Decision:** Deferred - knowledge library provides consistent baseline, memory could augment later

---

## Appendix: JSON Schema Definition

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "KnowledgeEntry",
  "type": "object",
  "required": ["id", "topic", "category", "summary", "content"],
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^[a-z0-9-]+$",
      "description": "Unique identifier (kebab-case)"
    },
    "topic": {
      "type": "string",
      "description": "Human-readable topic name"
    },
    "category": {
      "type": "string",
      "description": "Category for organization (e.g., Security, Tools, LLM Concepts)"
    },
    "keywords": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Keywords for search and indexing"
    },
    "summary": {
      "type": "string",
      "maxLength": 200,
      "description": "Brief summary (shown in index)"
    },
    "content": {
      "type": "object",
      "required": ["overview"],
      "properties": {
        "overview": {
          "type": "string",
          "description": "High-level explanation (2-4 sentences)"
        },
        "keyPoints": {
          "type": "array",
          "items": { "type": "string" },
          "maxItems": 10,
          "description": "Important points (bullet list)"
        },
        "examples": {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["title", "explanation"],
            "properties": {
              "title": { "type": "string" },
              "code": { "type": "string" },
              "explanation": { "type": "string" }
            }
          },
          "maxItems": 5,
          "description": "Code or configuration examples"
        },
        "warnings": {
          "type": "array",
          "items": { "type": "string" },
          "maxItems": 5,
          "description": "Critical warnings or cautions"
        },
        "bestPractices": {
          "type": "array",
          "items": { "type": "string" },
          "maxItems": 8,
          "description": "Recommended approaches"
        },
        "relatedTopics": {
          "type": "array",
          "items": { "type": "string", "pattern": "^[a-z0-9-]+$" },
          "description": "IDs of related knowledge entries"
        },
        "references": {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["title"],
            "properties": {
              "title": { "type": "string" },
              "url": { "type": "string", "format": "uri" }
            }
          },
          "description": "External resources"
        }
      }
    }
  }
}
```

---

## Conclusion

This design provides a solid foundation for the Knowledge Library feature:

- ✅ **LLM-first design**: Optimized for tool-based access, not UI browsing
- ✅ **Simple yet extensible**: Minimal tool interface, rich entry structure
- ✅ **Robust**: Separate files, graceful error handling, validation
- ✅ **Maintainable**: JSON-based, version-controlled, code-reviewed
- ✅ **Future-proof**: Category field ready, can add search/RAG later

**Next Steps:**
1. Review and refine this design with stakeholders
2. Begin implementation (Phase 1: Foundation)
3. Create first knowledge entry as proof of concept
4. Iterate based on testing and feedback

**This document will evolve** as implementation progresses and new insights emerge. Commit frequently to preserve decisions and context.
