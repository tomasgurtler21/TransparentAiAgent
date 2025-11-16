# Long-Term Memory Tools

**Last Updated**: 2025-11-16
**Status**: Active
**Phase**: Phase 10
**Layer**: Infrastructure
**ToolSourceType**: `BuiltInLongTermMemory`

---

## 📋 Document Scope

**What belongs in this document**:
- Documentation for long-term memory built-in tools
- Tool definitions, schemas, and usage
- Tool executor implementation
- Security guardrails and constraints
- Testing strategy for memory tools

**What does NOT belong here**:
- ❌ Service implementation details (→ [Long-Term Memory Service](../../infrastructure/long-term-memory-service.md))
- ❌ Conceptual overview (→ [Long-Term Memory Concept](../../../03-concepts/long-term-memory.md))
- ❌ User guides (→ [Using Long-Term Memory](../../../05-guides/features/using-long-term-memory.md))

---

## Overview

The long-term memory tools provide the AI agent with capabilities to read and update persistent memory across conversation sessions. These are built-in tools (not MCP-based) that integrate directly with the `LongTermMemoryService`.

## Available Tools

### 1. long_term_memory_read

Reads the current long-term memory for the active mode.

#### Tool Definition

```csharp
public class LongTermMemoryReadTool : ITool
{
    public string Name => "long_term_memory_read";
    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;

    public string Description => @"
Reads your long-term memory for the current mode (Normal or Teaching).
This memory contains information you've stored about the user across previous conversations.

Use this tool when you need to recall:
- User's background, preferences, or context
- Previous conversations or decisions
- User's skill level or learning progress
- Project context from earlier sessions

Returns the current memory content as markdown, or empty string if no memory exists.
";

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {},
  ""required"": []
}";
}
```

#### Parameters

**None** - The tool automatically uses the current mode from `IAppModeService`.

#### Returns

- **Success**: Markdown-formatted memory content (or empty string if no memory exists)
- **Failure**: Error message describing what went wrong

#### Example Usage

```json
// Tool call (no parameters needed)
{
  "name": "long_term_memory_read",
  "input": {}
}

// Response
{
  "success": true,
  "output": "# User Context\n\n- Name: Alex\n- Role: Senior Developer\n- Learning: C# async programming\n- Prefers detailed explanations with code examples"
}
```

---

### 2. long_term_memory_update

Updates the long-term memory for the active mode with new content.

#### Tool Definition

```csharp
public class LongTermMemoryUpdateTool : ITool
{
    public string Name => "long_term_memory_update";
    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;

    public string Description => @"
Updates your long-term memory for the current mode (Normal or Teaching).
This OVERWRITES the existing memory with new content.

WHEN TO UPDATE:
- When you learn important new information about the user
- When user preferences or context changes
- When consolidating information from the current conversation
- At the end of a conversation (if auto-prompted)

WHAT TO STORE:
- User background, role, skill level
- Preferences (communication style, detail level, etc.)
- Project context and goals
- Learning progress and topics covered
- Relevant decisions or choices made

GUARDRAILS - NEVER STORE:
- Passwords, API keys, or secrets
- Personal identification numbers (SSN, etc.)
- Health information
- Confidential or proprietary data
- Credit card or financial information
- Temporary conversation details

IMPORTANT:
- Keep memory concise and focused (10,000 character limit)
- Use markdown formatting for readability
- Summarize if memory is getting large
- Always provide a reason explaining why you're updating

Parameters:
- content (required): The new memory content (markdown format)
- reason (required): Brief explanation of why you're updating memory
";

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""content"": {
      ""type"": ""string"",
      ""description"": ""The new memory content in markdown format. This will OVERWRITE existing memory.""
    },
    ""reason"": {
      ""type"": ""string"",
      ""description"": ""Brief explanation of why you're updating memory (for transparency and logging).""
    }
  },
  ""required"": [""content"", ""reason""]
}";
}
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `content` | string | Yes | New memory content (markdown). **Overwrites** existing memory. |
| `reason` | string | Yes | Explanation of why memory is being updated (for transparency). |

#### Returns

- **Success**: Confirmation with character count and timestamp
- **Failure**: Error message (e.g., size limit exceeded, I/O error)

#### Example Usage

```json
// Tool call
{
  "name": "long_term_memory_update",
  "input": {
    "content": "# User Context\n\n- Name: Alex\n- Role: Senior Developer\n- Currently Learning: C# async/await patterns\n- Prefers: Detailed explanations with code examples\n- Projects: Building Blazor real-time dashboard",
    "reason": "User introduced themselves and shared learning goals"
  }
}

// Success response
{
  "success": true,
  "output": "Memory updated successfully (247 characters). Last updated: 2025-11-16T10:30:00Z"
}

// Failure response (size limit)
{
  "success": false,
  "output": "Memory content exceeds maximum size of 10000 characters"
}
```

---

## Tool Executor

### LongTermMemoryToolExecutor

The executor handles both memory tools.

#### Implementation

```csharp
namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

public class LongTermMemoryToolExecutor : IToolExecutor
{
    private readonly ILongTermMemoryService _memoryService;
    private readonly IAppModeService _appModeService;
    private readonly ILogger<LongTermMemoryToolExecutor> _logger;

    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;

    public async Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string argsJson,
        CancellationToken cancellationToken = default)
    {
        var currentMode = _appModeService.CurrentMode;

        return tool.Name switch
        {
            "long_term_memory_read" => await ExecuteReadAsync(currentMode, cancellationToken),
            "long_term_memory_update" => await ExecuteUpdateAsync(currentMode, argsJson, cancellationToken),
            _ => new ToolExecutionResult(
                Success: false,
                Output: $"Unknown memory tool: {tool.Name}")
        };
    }

    private async Task<ToolExecutionResult> ExecuteReadAsync(
        AppMode mode,
        CancellationToken cancellationToken)
    {
        var memory = await _memoryService.ReadMemoryAsync(mode, cancellationToken);

        return new ToolExecutionResult(
            Success: true,
            Output: string.IsNullOrEmpty(memory)
                ? "(No memory exists for this mode)"
                : memory);
    }

    private async Task<ToolExecutionResult> ExecuteUpdateAsync(
        AppMode mode,
        string argsJson,
        CancellationToken cancellationToken)
    {
        // Parse arguments
        var args = JsonSerializer.Deserialize<UpdateArgs>(argsJson);
        if (args?.Content == null || args.Reason == null)
        {
            return new ToolExecutionResult(
                Success: false,
                Output: "Missing required parameters: content and reason");
        }

        // Update memory
        var result = await _memoryService.UpdateMemoryAsync(
            mode,
            args.Content,
            cancellationToken);

        if (!result.Success)
        {
            return new ToolExecutionResult(
                Success: false,
                Output: result.Error ?? "Unknown error");
        }

        return new ToolExecutionResult(
            Success: true,
            Output: $"Memory updated successfully ({result.CharacterCount} characters). " +
                   $"Reason: {args.Reason}");
    }
}
```

---

## Tool Registry

### BuiltInLongTermMemoryToolRegistry

Registers both memory tools for discovery.

```csharp
public class BuiltInLongTermMemoryToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<ITool> _tools;

    public BuiltInLongTermMemoryToolRegistry()
    {
        _tools = new List<ITool>
        {
            new LongTermMemoryReadTool(),
            new LongTermMemoryUpdateTool()
        }.AsReadOnly();
    }

    public IReadOnlyList<ITool> GetAllTools() => _tools;

    public ITool? GetTool(string name) =>
        _tools.FirstOrDefault(t => t.Name == name);
}
```

---

## Security Guardrails

### Built-in Protections

1. **Size Limits**: 10,000 character maximum enforced by service
2. **Mode Isolation**: Each mode has separate memory (prevents context mixing)
3. **File Path Security**: Directory traversal prevention in service layer
4. **Explicit Guardrails**: Tool description includes "NEVER STORE" list
5. **Reason Requirement**: All updates must include justification

### Agent Instructions

The tool descriptions explicitly instruct the agent to:

**NEVER STORE**:
- Passwords, API keys, secrets
- Personal identification numbers
- Health information
- Confidential/proprietary data
- Credit card/financial information
- Temporary conversation details

### User Safety Nets

Even with guardrails, users have multiple safety features:

1. **Transparency Logging**: All updates logged with reasons
2. **View Memory**: UI allows viewing memory anytime
3. **Edit Memory**: Manual editing via UI
4. **Clear Memory**: One-click deletion
5. **Local Storage**: Files never leave user's machine

---

## Testing Strategy

### Unit Tests

Located in: `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInLongTermMemory/`

#### Tool Metadata Tests

```csharp
[TestMethod]
public void LongTermMemoryReadTool_HasCorrectMetadata()
{
    var tool = new LongTermMemoryReadTool();

    Assert.AreEqual("long_term_memory_read", tool.Name);
    Assert.AreEqual(ToolSourceType.BuiltInLongTermMemory, tool.SourceType);
    Assert.IsNotNull(tool.Description);
    Assert.IsNotNull(tool.ParametersSchema);
}

[TestMethod]
public void LongTermMemoryUpdateTool_HasCorrectMetadata()
{
    var tool = new LongTermMemoryUpdateTool();

    Assert.AreEqual("long_term_memory_update", tool.Name);
    Assert.IsTrue(tool.Description.Contains("GUARDRAILS"));
    Assert.IsTrue(tool.Description.Contains("NEVER STORE"));
}
```

#### Executor Tests - Read

```csharp
[TestMethod]
public async Task ExecuteAsync_ReadTool_ReturnsMemoryContent()
{
    // Arrange
    var mockMemoryService = new Mock<ILongTermMemoryService>();
    var mockAppModeService = new Mock<IAppModeService>();
    mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
    mockMemoryService
        .Setup(x => x.ReadMemoryAsync(AppMode.Normal, It.IsAny<CancellationToken>()))
        .ReturnsAsync("Test memory content");

    var executor = new LongTermMemoryToolExecutor(
        mockMemoryService.Object,
        mockAppModeService.Object,
        Mock.Of<ILogger<LongTermMemoryToolExecutor>>());

    var tool = new LongTermMemoryReadTool();

    // Act
    var result = await executor.ExecuteAsync(tool, "{}", CancellationToken.None);

    // Assert
    Assert.IsTrue(result.Success);
    Assert.AreEqual("Test memory content", result.Output);
}
```

#### Executor Tests - Update

```csharp
[TestMethod]
public async Task ExecuteAsync_UpdateTool_ValidContent_UpdatesMemory()
{
    // Arrange
    var mockMemoryService = new Mock<ILongTermMemoryService>();
    var mockAppModeService = new Mock<IAppModeService>();
    mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
    mockMemoryService
        .Setup(x => x.UpdateMemoryAsync(
            AppMode.Normal,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new MemoryUpdateResult(true, null, 100, DateTime.UtcNow));

    var executor = new LongTermMemoryToolExecutor(
        mockMemoryService.Object,
        mockAppModeService.Object,
        Mock.Of<ILogger<LongTermMemoryToolExecutor>>());

    var tool = new LongTermMemoryUpdateTool();
    var args = @"{""content"": ""New memory"", ""reason"": ""Test update""}";

    // Act
    var result = await executor.ExecuteAsync(tool, args, CancellationToken.None);

    // Assert
    Assert.IsTrue(result.Success);
    mockMemoryService.Verify(
        x => x.UpdateMemoryAsync(
            AppMode.Normal,
            "New memory",
            It.IsAny<CancellationToken>()),
        Times.Once);
}

[TestMethod]
public async Task ExecuteAsync_UpdateTool_MissingContent_ReturnsFailure()
{
    // Test validation of required parameters
}
```

#### Registry Tests

```csharp
[TestMethod]
public void GetAllTools_ReturnsReadAndUpdateTools()
{
    var registry = new BuiltInLongTermMemoryToolRegistry();

    var tools = registry.GetAllTools();

    Assert.AreEqual(2, tools.Count);
    Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_read"));
    Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_update"));
}
```

### Integration Tests

End-to-end tests verify the complete flow from tool call to memory persistence.

---

## Usage Examples

### Agent Reading Memory

**Scenario**: Agent needs to recall user context at conversation start.

```
Agent: Let me check what I remember about you...
[Calls: long_term_memory_read]

System: (Returns memory content)

Agent: Hi Alex! I see you're working on that Blazor dashboard project.
Since you prefer detailed explanations, let me walk through the WebSocket
integration step by step...
```

### Agent Updating Memory

**Scenario**: User shares new information during conversation.

```
User: By the way, I prefer TypeScript over JavaScript

Agent: Got it! Let me update my memory so I remember your preference.
[Calls: long_term_memory_update with:
  content: "... - Prefers TypeScript over JavaScript ..."
  reason: "User stated preference for TypeScript"]

System: Memory updated successfully

Agent: I've noted your preference for TypeScript. I'll keep that in mind
for future recommendations!
```

### End of Conversation Update

**Scenario**: Conversation ends, agent reviews what was learned.

```
[User clicks "End Conversation"]

System: (Sends prompt to agent: "Review conversation and update memory if needed")

Agent: [Reviews conversation, identifies new information]
[Calls: long_term_memory_update with:
  content: "Updated memory with new project details and learning progress"
  reason: "End of conversation - consolidating new information"]

System: Memory updated successfully
```

---

## Configuration

Tools are automatically registered when long-term memory is enabled:

```csharp
// In Program.cs

// Memory tools (registered when feature is enabled)
builder.Services.AddSingleton<IToolRegistry, BuiltInLongTermMemoryToolRegistry>();
builder.Services.AddSingleton<IToolExecutor, LongTermMemoryToolExecutor>();
```

The `ToolManager` composite pattern automatically discovers and includes these tools.

---

## Performance Characteristics

- **Read Tool**: ~1-5ms (file I/O)
- **Update Tool**: ~5-10ms (validation + file I/O)
- **Memory Usage**: Minimal (no caching, files loaded on demand)

---

## Related Documentation

- **Service**: [Long-Term Memory Service](../../infrastructure/long-term-memory-service.md) - Backend implementation
- **Concept**: [Long-Term Memory](../../../03-concepts/long-term-memory.md) - High-level overview
- **Guide**: [Using Long-Term Memory](../../../05-guides/features/using-long-term-memory.md) - User guide
- **Tool System**: [Tool System Concept](../../../03-concepts/tool-system.md) - General tool architecture

---

**See Also**: [Built-in Tools Overview](README.md)
