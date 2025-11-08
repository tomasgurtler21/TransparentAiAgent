# Tool Integration Points

**Purpose**: Guide for adding new tool sources to TransparentAiAgent
**Created**: 2025-11-01
**Status**: Ready for future built-in tools implementation

---

## Overview

This document explains how to add new tool sources to the TransparentAiAgent tool system. The tool architecture is designed to be **source-agnostic**, allowing easy integration of tools from multiple sources (MCP, built-in .NET methods, external APIs, custom protocols, etc.).

### Current Tool Sources

- **MCP Tools** (Phase 5) - Implemented
- **Built-In Tools** (Future) - Architecture ready, awaiting implementation

### Future Tool Sources (Examples)

- REST API tools
- GraphQL tools
- WebSocket-based tools
- Custom protocol tools

---

## Architecture Overview

### Tool Abstraction Layer

The tool system uses a **three-layer architecture**:

1. **Domain Layer**: Abstractions (interfaces) that all tool sources must implement
2. **Application Layer**: Orchestration and routing logic (ToolManager)
3. **Infrastructure Layer**: Concrete implementations for each tool source

```
┌──────────────────────────────────────────┐
│  Agent Orchestrator                      │
│  - Detects tool calls from LLM           │
│  - Delegates to ToolManager              │
└─────────────────┬────────────────────────┘
                  │
┌─────────────────▼────────────────────────┐
│  ToolManager (Application Layer)         │
│  - Routes by SourceType                  │
│  - Logs to Transparency System           │
│  - Handles errors/timeouts               │
└─────────────────┬────────────────────────┘
                  │
         ┌────────┴─────────┬──────────┐
         │                  │          │
         ▼                  ▼          ▼
    ┌────────┐        ┌────────┐  ┌────────┐
    │  MCP   │        │BuiltIn│  │ Future │
    │Executor│        │Executor│  │Protocol│
    └────────┘        └────────┘  └────────┘
       │                  │           │
       ▼                  ▼           ▼
  MCP Servers       .NET Methods   Other
```

---

## Step-by-Step Integration Guide

### Step 1: Define Tool Source Type

**Location**: `TransparentAiAgentCore/Domain/Tools/ToolSourceType.cs` (enum)

Add your new source type to the enum:

```csharp
public enum ToolSourceType
{
    MCP,        // Phase 5
    BuiltIn,    // Future - Add this when implementing built-in tools
    RestAPI,    // Future example
    // Add more as needed
}
```

---

### Step 2: Implement IToolExecutor

**Purpose**: Execute tools for your specific source type

**Interface**: `TransparentAiAgentCore/Domain/Tools/IToolExecutor.cs`

```csharp
namespace TransparentAiAgentCore.Domain.Tools;

public interface IToolExecutor
{
    /// <summary>
    /// Source type this executor handles
    /// </summary>
    ToolSourceType SourceType { get; }

    /// <summary>
    /// Execute a tool call
    /// </summary>
    Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,  // JSON string
        CancellationToken cancellationToken = default);
}
```

**Implementation Location**: `TransparentAiAgentCore/Infrastructure/Tools/[YourSource]/[YourSource]ToolExecutor.cs`

**Example - MCP Implementation** (Phase 5):
```csharp
// Location: Infrastructure/Tools/MCP/MCPToolExecutor.cs
public class MCPToolExecutor : IToolExecutor
{
    private readonly MCPClientWrapper _mcpClient;
    private readonly ITransparencyService _transparency;
    private readonly int _timeoutSeconds;

    public ToolSourceType SourceType => ToolSourceType.MCP;

    public async Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken)
    {
        // Validate tool is from MCP source
        if (tool.SourceType != ToolSourceType.MCP)
            return ToolExecutionResult.Failure("Tool is not from MCP source");

        // Get server name from metadata
        if (!tool.Metadata.TryGetValue("serverName", out var serverName))
            return ToolExecutionResult.Failure("MCP server name not found in tool metadata");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create timeout token
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            // Execute via MCP client
            var result = await _mcpClient.CallToolAsync(
                serverName,
                tool.Name,
                arguments,
                linkedCts.Token);

            stopwatch.Stop();
            return ToolExecutionResult.Success(result, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            return ToolExecutionResult.Failure($"Tool execution timeout ({_timeoutSeconds}s)", stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolExecutionResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }
}
```

**Example - Built-In Implementation** (Future):
```csharp
// Location: Infrastructure/Tools/BuiltIn/BuiltInToolExecutor.cs
public class BuiltInToolExecutor : IToolExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Func<string, Task<string>>> _toolHandlers;

    public ToolSourceType SourceType => ToolSourceType.BuiltIn;

    public async Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken)
    {
        if (tool.SourceType != ToolSourceType.BuiltIn)
            return ToolExecutionResult.Failure("Tool is not built-in");

        if (!_toolHandlers.TryGetValue(tool.Name, out var handler))
            return ToolExecutionResult.Failure($"No handler registered for built-in tool: {tool.Name}");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await handler(arguments);
            stopwatch.Stop();
            return ToolExecutionResult.Success(result, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolExecutionResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    // Register handlers in constructor or via method
    private void RegisterHandlers()
    {
        _toolHandlers["reset_conversation"] = async (args) =>
        {
            var conversationManager = _serviceProvider.GetRequiredService<IConversationManager>();
            conversationManager.ClearHistory();
            return "{\"status\": \"success\", \"message\": \"Conversation reset\"}";
        };

        _toolHandlers["export_conversation"] = async (args) =>
        {
            // Implementation...
            return "{\"status\": \"success\", \"file\": \"conversation.json\"}";
        };

        // Add more built-in tools...
    }
}
```

---

### Step 3: Implement IToolRegistry

**Purpose**: Discover and manage tools from your source

**Interface**: `TransparentAiAgentCore/Domain/Tools/IToolRegistry.cs`

```csharp
namespace TransparentAiAgentCore.Domain.Tools;

public interface IToolRegistry
{
    /// <summary>
    /// Get all registered tools
    /// </summary>
    IReadOnlyList<ITool> GetAllTools();

    /// <summary>
    /// Get a tool by name (returns null if not found)
    /// </summary>
    ITool? GetTool(string toolName);

    /// <summary>
    /// Check if a tool is registered
    /// </summary>
    bool HasTool(string toolName);

    /// <summary>
    /// Refresh tool registry (re-discover from source)
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
```

**Implementation Location**: `TransparentAiAgentCore/Infrastructure/Tools/[YourSource]/[YourSource]ToolRegistry.cs`

**Example - MCP Implementation** (Phase 5):
```csharp
// Location: Infrastructure/Tools/MCP/MCPToolRegistry.cs
public class MCPToolRegistry : IToolRegistry
{
    private readonly MCPToolDiscovery _discovery;
    private readonly List<ITool> _tools = new();
    private readonly object _lock = new();

    public IReadOnlyList<ITool> GetAllTools()
    {
        lock (_lock)
        {
            return _tools.AsReadOnly();
        }
    }

    public ITool? GetTool(string toolName)
    {
        lock (_lock)
        {
            return _tools.FirstOrDefault(t => t.Name == toolName);
        }
    }

    public bool HasTool(string toolName)
    {
        lock (_lock)
        {
            return _tools.Any(t => t.Name == toolName);
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Discover tools from all MCP servers
        var discoveredTools = await _discovery.DiscoverToolsAsync(cancellationToken);

        lock (_lock)
        {
            _tools.Clear();
            _tools.AddRange(discoveredTools);
        }
    }
}
```

**Example - Built-In Implementation** (Future):
```csharp
// Location: Infrastructure/Tools/BuiltIn/BuiltInToolRegistry.cs
public class BuiltInToolRegistry : IToolRegistry
{
    private readonly List<ITool> _tools = new();

    public BuiltInToolRegistry()
    {
        // Register built-in tools
        RegisterBuiltInTools();
    }

    private void RegisterBuiltInTools()
    {
        _tools.Add(new BuiltInTool(
            name: "reset_conversation",
            description: "Clear the conversation history and start fresh",
            parametersSchema: "{}" // No parameters
        ));

        _tools.Add(new BuiltInTool(
            name: "export_conversation",
            description: "Export conversation history to JSON or Markdown format",
            parametersSchema: "{\"type\":\"object\",\"properties\":{\"format\":{\"type\":\"string\",\"enum\":[\"json\",\"markdown\"]}}}"
        ));

        _tools.Add(new BuiltInTool(
            name: "get_system_info",
            description: "Get transparency system statistics",
            parametersSchema: "{}"
        ));
    }

    public IReadOnlyList<ITool> GetAllTools() => _tools.AsReadOnly();

    public ITool? GetTool(string toolName) =>
        _tools.FirstOrDefault(t => t.Name == toolName);

    public bool HasTool(string toolName) =>
        _tools.Any(t => t.Name == toolName);

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Built-in tools are static, no refresh needed
        return Task.CompletedTask;
    }
}

// Helper class for built-in tools
internal class BuiltInTool : ITool
{
    public string Name { get; }
    public string Description { get; }
    public string ParametersSchema { get; }
    public ToolSourceType SourceType => ToolSourceType.BuiltIn;
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public BuiltInTool(string name, string description, string parametersSchema)
    {
        Name = name;
        Description = description;
        ParametersSchema = parametersSchema;
        Metadata = new Dictionary<string, string>
        {
            { "source", "built-in" }
        }.AsReadOnly();
    }
}
```

---

### Step 4: Register with Dependency Injection

**Location**: `TransparentAiAgentCore/Program.cs` (or Startup.cs)

Register your executor and registry with the DI container:

```csharp
// Register MCP tool source (Phase 5)
services.AddSingleton<MCPClientWrapper>();
services.AddSingleton<MCPToolDiscovery>();
services.AddSingleton<MCPToolExecutor>();
services.AddSingleton<MCPToolRegistry>();
services.AddSingleton<IToolExecutor>(sp => sp.GetRequiredService<MCPToolExecutor>());

// Register Built-In tool source (Future)
services.AddSingleton<BuiltInToolExecutor>();
services.AddSingleton<BuiltInToolRegistry>();
services.AddSingleton<IToolExecutor>(sp => sp.GetRequiredService<BuiltInToolExecutor>());

// Register composite registry (aggregates all sources)
services.AddSingleton<IToolRegistry>(sp =>
{
    var registries = new List<IToolRegistry>
    {
        sp.GetRequiredService<MCPToolRegistry>(),
        sp.GetRequiredService<BuiltInToolRegistry>(), // Add when implemented
        // Add more registries here...
    };
    return new ToolRegistryComposite(registries);
});

// Register ToolManager (already done in Phase 5)
services.AddSingleton<IToolManager, ToolManager>();
```

---

### Step 5: Add Configuration (if needed)

If your tool source requires configuration, add it to the appropriate configuration class.

**Example - Built-In Tools Configuration** (Future):
```csharp
// Location: Domain/Configuration/BuiltInToolsConfiguration.cs
public class BuiltInToolsConfiguration
{
    /// <summary>
    /// Enable built-in tools (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Which built-in tools to enable
    /// </summary>
    public List<string> EnabledTools { get; set; } = new()
    {
        "reset_conversation",
        "export_conversation",
        "get_system_info"
    };

    public void Validate()
    {
        // Add validation if needed
    }
}

// Add to AppConfiguration.cs
public class AppConfiguration
{
    // ... existing properties ...

    public BuiltInToolsConfiguration BuiltInTools { get; set; } = new();
}
```

---

### Step 6: Test Your Implementation

**Unit Tests**: Test your executor and registry in isolation

```csharp
// Example: BuiltInToolExecutorTests.cs
[TestClass]
public class BuiltInToolExecutorTests
{
    [TestMethod]
    public async Task ExecuteAsync_ResetConversation_Success()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var executor = new BuiltInToolExecutor(serviceProvider);
        var tool = new BuiltInTool("reset_conversation", "Reset conversation", "{}");

        // Act
        var result = await executor.ExecuteAsync(tool, "{}", CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_ToolNotFound_ReturnsError()
    {
        // Arrange
        var executor = new BuiltInToolExecutor(CreateServiceProvider());
        var tool = new BuiltInTool("nonexistent_tool", "Does not exist", "{}");

        // Act
        var result = await executor.ExecuteAsync(tool, "{}", CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("No handler registered"));
    }
}
```

**Integration Tests**: Test with ToolManager

```csharp
[TestClass]
public class BuiltInToolsIntegrationTests
{
    [TestMethod]
    public async Task ToolManager_ExecuteBuiltInTool_Success()
    {
        // Arrange
        var toolManager = CreateToolManagerWithBuiltInTools();
        var toolCall = new LLMToolCall("id123", "reset_conversation", "{}");

        // Act
        var result = await toolManager.ExecuteToolCallAsync(toolCall, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
    }
}
```

---

## Integration Checklist

Use this checklist when adding a new tool source:

### Domain Layer
- [ ] Add new `ToolSourceType` enum value
- [ ] Ensure `ITool` interface covers your needs (usually no changes needed)

### Infrastructure Layer
- [ ] Implement `IToolExecutor` for your source
- [ ] Implement `IToolRegistry` for your source
- [ ] Add any source-specific helper classes (e.g., discovery, client wrapper)
- [ ] Handle errors and timeouts appropriately

### Configuration
- [ ] Add configuration class if needed
- [ ] Add validation logic
- [ ] Update `AppConfiguration` if needed
- [ ] Update `appsettings.json` schema

### Dependency Injection
- [ ] Register executor with DI container
- [ ] Register registry with DI container
- [ ] Add registry to `ToolRegistryComposite`

### Testing
- [ ] Unit tests for executor
- [ ] Unit tests for registry
- [ ] Integration tests with ToolManager
- [ ] End-to-end functional tests

### Transparency
- [ ] Log tool discovery events (if applicable)
- [ ] Tool execution already logged by ToolManager (automatic)
- [ ] Add source-specific event types if needed

### Documentation
- [ ] Update this document with your implementation example
- [ ] Document any configuration settings
- [ ] Document any special requirements or limitations

---

## Common Patterns

### Pattern 1: Synchronous Tools (Built-In)

For tools that execute synchronously (e.g., built-in .NET methods):
- Wrap synchronous calls in `Task.FromResult()`
- Consider using `Task.Run()` for CPU-intensive operations
- Add cancellation token support where possible

### Pattern 2: Asynchronous Tools (MCP, REST API)

For tools that execute asynchronously (e.g., network calls):
- Use `async/await` throughout
- Pass cancellation tokens
- Implement timeout logic
- Handle network errors gracefully

### Pattern 3: Tool Discovery (Dynamic vs Static)

**Dynamic Discovery** (e.g., MCP):
- Connect to source at startup
- Fetch tool list
- Cache tools in registry
- Support refresh on demand

**Static Discovery** (e.g., Built-In):
- Hardcode tool list
- Register in constructor
- No refresh needed (or no-op refresh)

### Pattern 4: Tool Metadata

Use metadata dictionary to store source-specific information:

```csharp
// MCP tools
Metadata = new Dictionary<string, string>
{
    { "serverName", "todo-list" },
    { "serverVersion", "1.0.0" },
    { "transport", "stdio" }
};

// Built-in tools
Metadata = new Dictionary<string, string>
{
    { "source", "built-in" },
    { "assembly", "TransparentAiAgentCore" }
};

// REST API tools (future example)
Metadata = new Dictionary<string, string>
{
    { "endpoint", "https://api.example.com/tool" },
    { "authType", "bearer" }
};
```

---

## MCP Implementation Reference

The MCP implementation (Phase 5) serves as a reference for all future tool sources. Key files:

**Domain Abstractions**:
- `Domain/Tools/ITool.cs`
- `Domain/Tools/IToolExecutor.cs`
- `Domain/Tools/IToolRegistry.cs`
- `Domain/Tools/IToolManager.cs`
- `Domain/Tools/ToolExecutionResult.cs`
- `Domain/Tools/ToolSourceType.cs`

**Application Layer**:
- `Application/Tools/ToolManager.cs`

**MCP Infrastructure**:
- `Infrastructure/Tools/MCP/MCPClientWrapper.cs`
- `Infrastructure/Tools/MCP/MCPToolDiscovery.cs`
- `Infrastructure/Tools/MCP/MCPToolExecutor.cs`
- `Infrastructure/Tools/MCP/MCPToolRegistry.cs`
- `Infrastructure/Tools/ToolRegistryComposite.cs`

**Tests**:
- `TransparentAiAgentCore_Tests/Tools/ToolManagerTests.cs`
- `TransparentAiAgentCore_Tests/Tools/MCP/MCPToolExecutorTests.cs`
- `TransparentAiAgentCore_Tests/Tools/MCP/MCPToolRegistryTests.cs`

---

## FAQ

### Q: Do I need to modify ToolManager when adding a new tool source?

**A**: No! ToolManager routes by `SourceType` automatically. As long as your executor implements `IToolExecutor` and is registered with DI, ToolManager will find it.

### Q: Can I have multiple tool sources active at once?

**A**: Yes! That's the purpose of `ToolRegistryComposite`. It aggregates all registered `IToolRegistry` implementations. The LLM will see all tools from all sources.

### Q: What if two tool sources have tools with the same name?

**A**: First match wins (based on registry order in `ToolRegistryComposite`). Consider namespacing tool names (e.g., `mcp:add_todo` vs `builtin:reset_conversation`) if conflicts are likely.

### Q: How do I handle authentication for my tool source?

**A**: Add authentication logic in your executor's `ExecuteAsync` method. Store credentials in your source-specific configuration. See `MCPConfiguration` for example.

### Q: Can I add tool sources dynamically at runtime?

**A**: The current architecture supports startup-time registration. Dynamic registration would require refactoring `ToolManager` and `ToolRegistryComposite` to support adding executors/registries at runtime.

### Q: How do I test my tool source in isolation?

**A**: Mock the dependencies (e.g., HTTP client, MCP client) and test your executor and registry independently. See MCP tests for examples.

---

## Next Steps

When you're ready to implement built-in tools or other tool sources:

1. Review this document
2. Study the MCP implementation (Phase 5)
3. Follow the step-by-step guide
4. Use the checklist to track progress
5. Write tests first (TDD)
6. Update this document with your implementation notes

---

**Last Updated**: 2025-11-01
**Status**: Ready for future tool source implementations
