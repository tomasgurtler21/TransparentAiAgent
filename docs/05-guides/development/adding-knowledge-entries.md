# Adding Knowledge Entries

**Last Updated**: 2025-11-15
**Related**: [Knowledge Library Concept](../../03-concepts/knowledge-library.md), [Knowledge Library Tool](../../04-components/tools/knowledge-library-tool.md)

---

## 📋 Document Scope

### ✅ This document covers:
- Step-by-step process for adding knowledge entries
- Decision criteria for knowledgeGapLikelihood
- Content guidelines and quality standards
- Validation checklist and common mistakes
- Examples of good vs. bad entries

### ❌ This document does NOT cover:
- **Knowledge Library architecture** → See [04-components/tools/knowledge-library-tool.md](../../04-components/tools/knowledge-library-tool.md)
- **Knowledge Library concept** → See [03-concepts/knowledge-library.md](../../03-concepts/knowledge-library.md)
- **Testing knowledge entries** → See Session 7 in `KNOWLEDGE_LIBRARY_IMPLEMENTATION_PLAN.md`

---

## 🎯 Overview

The Knowledge Library is a curated collection of **guardrails** that guide the teaching agent when explaining critical concepts. Entries are **not** exhaustive documentation—they provide essential principles, red lines, and corrections that the LLM uses to teach accurately.

**Key Philosophy: Guardrails, Not Encyclopedias**

- ✅ Critical principles and "must-knows"
- ✅ Security red lines and "never-dos"
- ✅ Application-specific context
- ❌ Comprehensive tutorials or documentation
- ❌ Detailed step-by-step procedures
- ❌ General knowledge the LLM already has

---

## 📂 File Structure

Knowledge entries are stored as individual JSON files:

```
TransparentAiAgentGui/wwwroot/knowledge/
├── entries/
│   ├── api-key-security.json
│   ├── context-windows.json
│   ├── mcp-overview.json
│   └── ... (one file per entry)
├── _schema.json          # JSON schema for validation
└── README.md             # Maintenance guide
```

**Important**: No `index.json` file! The app scans the `entries/` directory at startup and builds an in-memory index automatically.

---

## 🚀 Step-by-Step: Adding a New Entry

### Step 1: Determine If Entry Is Needed

Ask yourself these questions:

**1. "What happens if the LLM teaches this wrong?"**
- Security breach / data loss / legal issue → **YES, add it**
- User gets suboptimal code → **NO, LLM can handle**

**2. "Does the LLM already know this well?"**
- No, it's app-specific → **YES, add it**
- Yes, it's general knowledge → **NO, don't add**

**3. "Can I express this in 3-10 critical principles?"**
- Yes, clear guardrails → **YES, add it**
- No, it's too complex/detailed → **NO, don't add** (or link to external docs)

**4. "Will this need frequent updates?"**
- No, principles are stable → **YES, add it**
- Yes, details change often → **NO, don't add** (maintenance burden)

### Step 2: Choose a Topic ID

**Format**: `kebab-case` (lowercase letters, numbers, hyphens only)

**Examples**:
- ✅ `api-key-security`
- ✅ `context-windows`
- ✅ `multi-agent-orchestration`
- ❌ `API_Key_Security` (wrong case)
- ❌ `apiKeySecurity` (camelCase not allowed)

**Rules**:
- Must be unique
- Should be descriptive and concise
- Must match the filename: `{id}.json`

### Step 3: Determine Knowledge Gap Likelihood

This field tells the LLM how reliable its built-in knowledge is for this topic.

| Value | Criteria | LLM Behavior | Examples |
|-------|----------|--------------|----------|
| **low** | • LLM's training data is current<br>• Topic is stable, rarely changes<br>• Well-established best practices | • Rely on inner knowledge confidently<br>• Use library for critical guardrails only | `api-key-security`<br>`context-windows`<br>`basic-authentication` |
| **medium** | • LLM's knowledge may be partially outdated<br>• Topic has evolved since training cutoff<br>• Conflicting information exists | • Cross-reference with knowledge library<br>• Consider web search for latest updates | `authentication-methods`<br>`prompt-engineering`<br>`conversation-management` |
| **high** | • LLM's knowledge is very likely outdated<br>• Rapidly evolving topic<br>• Recent developments post-training<br>• App-specific features | • Strongly prefer web search if available<br>• If no web search: warn user about potential outdated info<br>• Rely heavily on knowledge library | `mcp-overview`<br>`multi-agent-orchestration`<br>`teaching-mode`<br>`transparency-logging` |

**Decision Framework**:

1. **Is this topic app-specific?** → Likely **high** (LLM wasn't trained on it)
2. **Has this topic changed significantly in the last 12 months?** → Likely **medium** or **high**
3. **Is this a fundamental, stable concept?** → Likely **low**
4. **When was the topic last standardized?** → Before 2023: **low**, 2023-2024: **medium**, 2025+: **high**

### Step 4: Create the JSON File

Create a new file in `TransparentAiAgentGui/wwwroot/knowledge/entries/{topic-id}.json`

**Template**:

```json
{
  "id": "topic-id",
  "topic": "Human-Readable Topic Name",
  "category": "Category Name",
  "keywords": ["keyword1", "keyword2", "keyword3"],
  "summary": "Brief one-sentence summary for index listing",
  "knowledgeGapLikelihood": "low|medium|high",
  "lastUpdated": "YYYY-MM-DD",
  "lastChecked": "YYYY-MM-DD",
  "content": {
    "overview": "High-level explanation (1-4 sentences depending on complexity)",
    "keyPoints": [
      "Critical principle 1",
      "Critical principle 2",
      "Critical principle 3"
    ],
    "examples": [
      {
        "title": "Example Title",
        "code": "Optional code snippet",
        "explanation": "What this example demonstrates"
      }
    ],
    "warnings": [
      "Critical warning 1",
      "Critical warning 2"
    ],
    "bestPractices": [
      "Recommended approach 1",
      "Recommended approach 2"
    ],
    "relatedTopics": ["related-topic-id-1", "related-topic-id-2"],
    "references": [
      {
        "title": "External Resource Name",
        "url": "https://example.com/resource"
      }
    ]
  }
}
```

**Field Descriptions**:

| Field | Required | Description | Guidelines |
|-------|----------|-------------|------------|
| `id` | ✅ Yes | Unique kebab-case identifier | Must match filename |
| `topic` | ✅ Yes | Human-readable topic name | Clear and concise |
| `category` | ✅ Yes | Category for organization | See categories below |
| `keywords` | ❌ No | Search keywords | 3-7 keywords |
| `summary` | ✅ Yes | Brief summary for index | Max 200 characters |
| `knowledgeGapLikelihood` | ✅ Yes | How outdated LLM's knowledge likely is | `low`, `medium`, or `high` |
| `lastUpdated` | ✅ Yes | Date content was last modified | YYYY-MM-DD format |
| `lastChecked` | ✅ Yes | Date accuracy was last verified | YYYY-MM-DD format |
| `content.overview` | ✅ Yes | Main explanation | 1-4 sentences |
| `content.keyPoints` | ❌ No | Critical bullet points | 3-10 items |
| `content.examples` | ❌ No | Code/config examples | 0-3 examples |
| `content.warnings` | ❌ No | Critical warnings | 2-5 items |
| `content.bestPractices` | ❌ No | Recommended approaches | Often omit—LLM knows these |
| `content.relatedTopics` | ❌ No | IDs of related entries | 0-5 topics |
| `content.references` | ❌ No | External resources | Link to official docs |

**Categories**:
- `Security` - Security, privacy, authentication
- `LLM Concepts` - Context windows, tokens, prompting
- `Application-Specific` - MCP, transparency, teaching mode
- `Development` - Testing, configuration, error handling
- `Advanced` - Multi-agent, streaming, orchestration

### Step 5: Write High-Quality Content

#### Content Length Guidelines

Target token counts based on topic complexity:

| Topic Type | Token Range | Rationale |
|------------|-------------|-----------|
| **Simple** | ~100-200 | Fundamental concepts, unlikely to change |
| **Moderate** | ~200-400 | Most security topics, standard best practices |
| **Complex** | ~400-800 | Rapidly evolving areas where LLM has limited knowledge |

**Don't artificially limit content if a topic genuinely needs detailed guardrails.**

#### Overview Guidelines

**Length**: 1-4 sentences depending on complexity

**Content**:
- Start with "what" and "why"
- State the key problem or concept
- Set context for the keyPoints that follow

**Example (Good)**:
```json
"overview": "API keys are sensitive credentials that grant access to services. Mishandling leads to security breaches, unauthorized access, and financial loss. The most common mistake is committing keys to version control."
```

**Example (Bad - Too Long)**:
```json
"overview": "API keys are authentication tokens that have been used in software development for many years. They serve as a simple authentication mechanism that allows applications to access third-party services. Throughout the history of software development, many different authentication methods have been used..."
```

#### Key Points Guidelines

**Purpose**: Critical "must-knows" and "never-dos"

**Length**: 3-10 points (more for complex topics)

**Format**:
- Start with action verbs or imperatives
- Be specific and actionable
- Prioritize security and correctness

**Example (Good)**:
```json
"keyPoints": [
  "NEVER commit API keys to version control - this is the #1 mistake",
  "Store in environment variables or secure vaults only",
  "Rotate immediately if exposed (assume compromise)",
  "Use different keys for dev/staging/production environments"
]
```

**Example (Bad - Too Generic)**:
```json
"keyPoints": [
  "API keys are important",
  "You should be careful with security",
  "Make sure to follow best practices"
]
```

#### Examples Guidelines

**When to include**:
- Complex concepts that benefit from code
- Security patterns that need demonstration
- Common mistakes to avoid

**When to omit**:
- Simple concepts (LLM can generate examples)
- General programming constructs

**Format**:
```json
"examples": [
  {
    "title": "Correct: Environment Variable",
    "code": "var key = Environment.GetEnvironmentVariable(\"ANTHROPIC_API_KEY\");",
    "explanation": "Keeps key out of source code and allows different values per environment"
  },
  {
    "title": "WRONG: Hardcoded Key",
    "code": "var key = \"sk-ant-1234567890\"; // NEVER DO THIS!",
    "explanation": "This will be committed to git and exposed publicly"
  }
]
```

**Guidelines**:
- Keep code snippets short (1-5 lines)
- Show both correct and incorrect patterns
- Explain *why*, not just *how*
- Use realistic code (not pseudo-code)

#### Warnings Guidelines

**Purpose**: Critical cautions and red lines

**When to include**:
- Security vulnerabilities
- Data loss risks
- Legal/compliance issues
- Common dangerous mistakes

**Format**:
- Start with "Never" or "Always" when appropriate
- Be specific about consequences
- Focus on critical issues only

**Example (Good)**:
```json
"warnings": [
  "Never log API keys (even partially masked) - log aggregation services could expose them",
  "Never share in screenshots or documentation - they'll be indexed by search engines",
  "Revoke immediately if accidentally committed - assume the key is compromised"
]
```

**Example (Bad - Too Vague)**:
```json
"warnings": [
  "Be careful with API keys",
  "Don't make security mistakes"
]
```

#### Best Practices Guidelines

**When to include**:
- Application-specific patterns
- Non-obvious optimizations
- Corrections to common misconceptions

**When to omit**:
- General programming best practices (LLM knows these)
- Standard industry practices (LLM knows these)

**Note**: This section is often omitted in the guardrails approach. Include only if you're correcting specific misconceptions.

#### Related Topics Guidelines

**Purpose**: Help users discover related knowledge

**Format**: List of topic IDs (not full topic names)

**Guidelines**:
- Only include directly related topics
- Limit to 3-5 topics
- Ensure IDs actually exist

**Example**:
```json
"relatedTopics": ["environment-variables", "authentication-methods", "secure-configuration"]
```

#### References Guidelines

**When to include**:
- Official documentation
- Authoritative sources
- Detailed specifications

**When to omit**:
- Blog posts or opinion pieces
- Outdated resources
- Paywalled content

**Example**:
```json
"references": [
  {
    "title": "OWASP API Security Project",
    "url": "https://owasp.org/www-project-api-security/"
  },
  {
    "title": "NIST Digital Identity Guidelines",
    "url": "https://pages.nist.gov/800-63-3/"
  }
]
```

### Step 6: Validate Your Entry

#### Automated Validation

**JSON Schema Validation** (future enhancement):
```bash
# Validate against schema (when schema validator is implemented)
# validate-knowledge-entry.sh entries/your-entry.json
```

**Manual Validation Checklist**:

- [ ] File is valid JSON (use a JSON validator)
- [ ] Filename matches `id` field
- [ ] `id` is kebab-case with no uppercase
- [ ] All required fields are present
- [ ] `knowledgeGapLikelihood` is exactly "low", "medium", or "high"
- [ ] Dates are in YYYY-MM-DD format
- [ ] `relatedTopics` IDs exist in the knowledge library
- [ ] No spelling or grammar errors
- [ ] Code examples are tested and correct
- [ ] References have valid URLs

#### Content Quality Checklist

**Guardrails Philosophy**:
- [ ] Entry provides critical principles, not comprehensive docs
- [ ] Length is appropriate for topic complexity (~200-400 tokens typical)
- [ ] Content corrects or guides, doesn't duplicate LLM knowledge
- [ ] Focus is on "must-knows" and "never-dos"

**Clarity**:
- [ ] Overview clearly states what and why
- [ ] Key points are specific and actionable
- [ ] Examples demonstrate (not just state) concepts
- [ ] Warnings identify real risks with consequences

**Accuracy**:
- [ ] All facts are current and correct
- [ ] Code examples have been tested
- [ ] References are authoritative
- [ ] No outdated or deprecated information

**Completeness**:
- [ ] All critical principles are covered
- [ ] Common mistakes are warned against
- [ ] Related topics are linked
- [ ] Nothing critical is missing

### Step 7: Test Your Entry

**Manual Testing**:

1. **Place file in correct location**:
   ```bash
   TransparentAiAgentGui/wwwroot/knowledge/entries/{topic-id}.json
   ```

2. **Restart the application**:
   ```bash
   dotnet run --project TransparentAiAgentGui
   ```

3. **Check startup logs**:
   - Look for "Knowledge library initialized: X entries loaded, 0 failed"
   - If your entry failed to load, check error logs

4. **Test in teaching mode**:
   - Start a conversation in teaching mode
   - Verify your topic appears in the system prompt
   - Ask the agent about your topic
   - Verify the agent queries the knowledge library
   - Verify the agent uses your content correctly

**Integration Testing** (once implemented in Session 7):
- Run end-to-end tests to verify tool execution
- Verify formatted output matches expectations

### Step 8: Commit Your Entry

```bash
git add TransparentAiAgentGui/wwwroot/knowledge/entries/{topic-id}.json
git commit -m "content: add {topic-id} knowledge entry

- Category: {category}
- Gap likelihood: {low|medium|high}
- Content: {brief description of what this entry covers}
"
git push
```

---

## 📝 Complete Example: API Key Security

This is a **good example** of a knowledge entry following the guardrails approach:

```json
{
  "id": "api-key-security",
  "topic": "API Key Security",
  "category": "Security",
  "keywords": ["api", "key", "secrets", "security", "authentication"],
  "summary": "Critical security guardrails for API key handling",
  "knowledgeGapLikelihood": "low",
  "lastUpdated": "2025-11-15",
  "lastChecked": "2025-11-15",
  "content": {
    "overview": "API keys are sensitive credentials that grant access to services. Mishandling leads to security breaches, unauthorized access, and financial loss. The most common mistake is committing keys to version control.",
    "keyPoints": [
      "NEVER commit API keys to version control - this is the #1 mistake",
      "Store in environment variables or secure vaults only (Azure Key Vault, AWS Secrets Manager, etc.)",
      "Rotate immediately if exposed (assume compromise, not just potential compromise)",
      "Use different keys for dev/staging/production environments",
      "Set up key expiration and rotation policies",
      "Monitor key usage for anomalies"
    ],
    "examples": [
      {
        "title": "Correct: Environment Variable",
        "code": "var apiKey = Environment.GetEnvironmentVariable(\"ANTHROPIC_API_KEY\")\n    ?? throw new InvalidOperationException(\"API key not configured\");",
        "explanation": "Keeps key out of source code, allows different values per environment, and fails fast if missing"
      },
      {
        "title": "WRONG: Hardcoded Key",
        "code": "var apiKey = \"sk-ant-1234567890\"; // NEVER DO THIS!",
        "explanation": "This will be committed to git, exposed in logs, and potentially leaked publicly"
      }
    ],
    "warnings": [
      "Never log API keys (even partially masked) - log aggregation exposes them",
      "Never share in screenshots or documentation - search engines index them",
      "Revoke immediately if accidentally committed - git history preserves them forever",
      "Never hardcode keys in configuration files committed to source control"
    ],
    "relatedTopics": ["environment-variables", "secure-configuration"],
    "references": [
      {
        "title": "OWASP API Security Project",
        "url": "https://owasp.org/www-project-api-security/"
      },
      {
        "title": "Anthropic API Key Best Practices",
        "url": "https://docs.anthropic.com/claude/reference/api-keys"
      }
    ]
  }
}
```

**Why this is a good entry**:
- ✅ Concise (~250 tokens) - Provides guardrails, not exhaustive guide
- ✅ Actionable key points with clear "never-dos"
- ✅ Minimal examples showing correct and incorrect patterns
- ✅ Specific warnings with consequences
- ✅ Links to authoritative sources for details
- ✅ LLM can expand on these principles using built-in knowledge

**What this entry does NOT include** (intentionally):
- ❌ Complete guide to all key management services
- ❌ Detailed rotation procedures for every platform
- ❌ Comprehensive threat modeling
- ❌ Language-specific syntax for all programming languages

**Why the omissions are correct**:
- LLM already knows about Azure Key Vault, AWS Secrets Manager, etc.
- LLM can provide language-specific examples
- Entry provides the critical principles; LLM fills in the details

---

## 🚫 Common Mistakes to Avoid

### Mistake 1: Encyclopedia Creep

**Bad** (~800 tokens):
```json
{
  "topic": "API Key Security",
  "content": {
    "overview": "API keys have a long history in software development dating back to... [5 paragraphs]",
    "keyPoints": [
      "Use API keys for authentication",
      "There are many types of keys including symmetric and asymmetric...",
      "OAuth 2.0 is an alternative that involves...",
      "HMAC signatures can be used...",
      "Rate limiting is important...",
      "Encryption at rest...",
      "Encryption in transit...",
      "...10 more generic points..."
    ],
    "examples": [
      "...3 examples in Java...",
      "...3 examples in Python...",
      "...3 examples in C#...",
      "...detailed OAuth flow diagrams..."
    ],
    "bestPractices": [
      "...15 best practices covering everything..."
    ]
  }
}
```

**Good** (~200-300 tokens):
```json
{
  "content": {
    "overview": "API keys are sensitive credentials. Mishandling leads to security breaches and unauthorized access.",
    "keyPoints": [
      "NEVER commit API keys to version control",
      "Store in environment variables or secure vaults only",
      "Rotate immediately if exposed"
    ],
    "examples": [
      {
        "title": "Correct: Environment Variable",
        "code": "var key = Environment.GetEnvironmentVariable(\"API_KEY\");",
        "explanation": "Keeps key out of source code"
      }
    ],
    "warnings": [
      "Never log API keys (even masked)",
      "Revoke immediately if accidentally committed"
    ]
  }
}
```

**Why the second is better**:
- Focuses on critical mistakes and corrections
- LLM can provide multi-language examples
- LLM can explain OAuth if user asks
- Maintenance is much easier

### Mistake 2: Vague Guidance

**Bad**:
```json
"keyPoints": [
  "Be careful with API keys",
  "Follow security best practices",
  "Use proper authentication methods"
]
```

**Good**:
```json
"keyPoints": [
  "NEVER commit API keys to version control - this is the #1 mistake",
  "Store in environment variables or Azure Key Vault only",
  "Rotate immediately if exposed (assume compromise)"
]
```

### Mistake 3: Outdated Information

**Bad** (knowledgeGapLikelihood: low, but content is from 2019):
```json
{
  "knowledgeGapLikelihood": "low",
  "lastChecked": "2019-01-01",
  "content": {
    "overview": "Use .NET Framework 4.7 for best results..."
  }
}
```

**Good**:
```json
{
  "knowledgeGapLikelihood": "low",
  "lastChecked": "2025-11-15",
  "content": {
    "overview": "Use .NET 8.0 or later for modern development..."
  }
}
```

### Mistake 4: Wrong Gap Likelihood

**Bad** (MCP is app-specific and rapidly evolving, should be "high"):
```json
{
  "id": "mcp-overview",
  "knowledgeGapLikelihood": "low"
}
```

**Good**:
```json
{
  "id": "mcp-overview",
  "knowledgeGapLikelihood": "high",
  "content": {
    "overview": "MCP (Model Context Protocol) is a protocol for tool integration introduced in 2024..."
  }
}
```

### Mistake 5: Missing Critical Warnings

**Bad**:
```json
{
  "topic": "Database Connection Strings",
  "content": {
    "overview": "Connection strings specify how to connect to databases.",
    "keyPoints": [
      "Include server, database, and credentials"
    ]
  }
}
```

**Good**:
```json
{
  "topic": "Database Connection Strings",
  "content": {
    "overview": "Connection strings contain sensitive credentials and must be protected like API keys.",
    "keyPoints": [
      "NEVER commit connection strings to source control",
      "Store in environment variables or Azure Key Vault",
      "Use different connection strings per environment"
    ],
    "warnings": [
      "Connection strings often contain passwords in plain text",
      "Exposing connection strings can lead to complete database compromise"
    ]
  }
}
```

### Mistake 6: Duplicating General Knowledge

**Bad** (LLM already knows C# async/await):
```json
{
  "id": "async-await-basics",
  "topic": "Async/Await in C#",
  "knowledgeGapLikelihood": "low",
  "content": {
    "overview": "Async and await are keywords in C# for asynchronous programming...",
    "keyPoints": [
      "Use async for methods that perform I/O",
      "Await pauses execution without blocking",
      "Returns Task or Task<T>"
    ]
  }
}
```

**Better** (if you must have an entry, focus on app-specific patterns):
```json
{
  "id": "async-patterns-blazor-server",
  "topic": "Async Patterns in Blazor Server",
  "knowledgeGapLikelihood": "medium",
  "content": {
    "overview": "Blazor Server requires special async patterns due to SignalR connections and UI thread constraints.",
    "keyPoints": [
      "NEVER use async void in Blazor components - use async Task",
      "Use InvokeAsync() when updating UI from background threads",
      "Dispose async resources properly in IAsyncDisposable"
    ],
    "warnings": [
      "async void swallows exceptions in Blazor components",
      "Direct UI updates from background threads cause crashes"
    ]
  }
}
```

---

## 🎯 Decision Matrix: Should I Add This Entry?

| Topic | LLM Knows Well? | Critical If Wrong? | App-Specific? | **Decision** | **Gap Likelihood** |
|-------|-----------------|-------------------|---------------|-------------|-------------------|
| C# Async/Await Basics | ✅ Yes | ❌ No | ❌ No | ❌ **Don't Add** | N/A |
| API Key Security | ✅ Yes | ✅ Yes | ❌ No | ✅ **Add** | low |
| Blazor Server Async Patterns | ⚠️ Partially | ✅ Yes | ⚠️ Framework | ✅ **Add** | medium |
| MCP Protocol | ❌ No | ✅ Yes | ✅ Yes | ✅ **Add** | high |
| Teaching Mode Features | ❌ No | ⚠️ Medium | ✅ Yes | ✅ **Add** | high |
| General SOLID Principles | ✅ Yes | ❌ No | ❌ No | ❌ **Don't Add** | N/A |
| App-Specific Security Patterns | ⚠️ Partially | ✅ Yes | ✅ Yes | ✅ **Add** | medium |
| How to Use JSON in C# | ✅ Yes | ❌ No | ❌ No | ❌ **Don't Add** | N/A |
| Context Window Management | ✅ Yes | ⚠️ Medium | ⚠️ App-variant | ✅ **Add** | low |
| Multi-Agent Orchestration | ❌ No | ✅ Yes | ⚠️ Cutting-edge | ✅ **Add** | high |

**Legend**:
- ✅ Yes / ❌ No / ⚠️ Partially or Medium

---

## 🔄 Maintenance Guidelines

### When to Update an Entry

**Update immediately if**:
- Security vulnerability is discovered
- Best practices change significantly
- External references become outdated
- Code examples no longer work

**Update during quarterly review if**:
- `lastChecked` is older than 6 months
- Topic has evolved but not critically
- Minor improvements identified

### How to Update an Entry

1. Edit the JSON file
2. Update `lastUpdated` field to current date
3. If content didn't change but you verified accuracy, update only `lastChecked`
4. Test the changes
5. Commit with descriptive message:
   ```bash
   git commit -m "content: update {topic-id} - {reason for update}"
   ```

### Deprecating an Entry

If an entry becomes obsolete:

1. **Don't delete immediately** - dependent topics may reference it
2. **Add deprecation notice** to overview:
   ```json
   "overview": "[DEPRECATED: This topic is no longer relevant as of {date}. See {replacement-topic-id} instead.] ..."
   ```
3. **Update related topics** to remove references
4. **After verification period**, delete the file and commit

---

## 📊 Quality Metrics

Good knowledge entries should meet these criteria:

**Content Quality**:
- ✅ Critical principles clearly stated
- ✅ Warnings identify real, specific risks
- ✅ Examples demonstrate (not just state)
- ✅ Length appropriate for complexity (~200-400 tokens typical)

**Technical Quality**:
- ✅ All facts are current and accurate
- ✅ Code examples have been tested
- ✅ References are authoritative
- ✅ No spelling or grammar errors

**Usability**:
- ✅ LLM can teach effectively from this entry
- ✅ Entry corrects specific misconceptions
- ✅ Related topics aid discovery
- ✅ Doesn't duplicate general knowledge

**Maintainability**:
- ✅ Principles stable (unlikely to change often)
- ✅ Content is concise (easy to review)
- ✅ Gap likelihood is accurate
- ✅ Timestamps are current

---

## 🎓 Learning from Examples

See the actual entries in `TransparentAiAgentGui/wwwroot/knowledge/entries/` for real-world examples.

**Recommended study order**:
1. `api-key-security.json` - Classic guardrails example
2. `context-windows.json` - Stable LLM concept (low gap)
3. `mcp-overview.json` - App-specific, rapidly evolving (high gap)
4. `teaching-mode.json` - App-specific feature (high gap)

---

## 🆘 Troubleshooting

### Entry Not Appearing in System Prompt

**Possible causes**:
- File not in `wwwroot/knowledge/entries/` directory
- JSON syntax error (validate with JSON linter)
- Filename doesn't match `id` field
- App not restarted after adding entry

**Solution**:
1. Check startup logs for parsing errors
2. Validate JSON syntax
3. Verify filename matches `id`
4. Restart application

### Entry Loads But Agent Doesn't Use It

**Possible causes**:
- Topic not mentioned in system prompt
- Agent relying on built-in knowledge instead
- Query prompt not clear enough

**Solution**:
1. Verify entry appears in teaching mode system prompt
2. Ask agent explicitly: "What does the knowledge library say about {topic}?"
3. Review tool call logs to see if library was queried

### Validation Errors

**Common issues**:
- `knowledgeGapLikelihood` not exactly "low", "medium", or "high" (case-sensitive)
- Date not in YYYY-MM-DD format
- `id` contains uppercase or special characters
- Missing required fields

**Solution**:
- Use the validation checklist in Step 6
- Compare to example entries
- Use JSON schema validator (when available)

---

## 📚 Additional Resources

- [Knowledge Library Concept](../../03-concepts/knowledge-library.md) - Philosophy and design
- [Knowledge Library Tool](../../04-components/tools/knowledge-library-tool.md) - Technical architecture
- `KNOWLEDGE_LIBRARY_DESIGN.md` (project root) - Detailed design decisions
- `KNOWLEDGE_LIBRARY_IMPLEMENTATION_PLAN.md` (project root) - Implementation sessions
- `wwwroot/knowledge/README.md` - Quick reference for maintainers

---

**Remember**: You're creating **guardrails**, not encyclopedias. Focus on critical principles that guide the LLM's teaching, and let the LLM fill in the comprehensive details using its vast built-in knowledge.

**Happy knowledge building! 🚀**
