# Long-Term Memory - Implementation Plan

**Created**: 2025-11-16
**Status**: Ready for Implementation
**Related**: LONG_TERM_MEMORY_DESIGN.md

---

## Implementation Approach

Following the proven TDD workflow:
1. **Red**: Write failing test
2. **Green**: Implement minimum code to pass
3. **Refactor**: Clean up and improve
4. **Commit**: Small, focused commits after each green phase

**Phases**: Bottom-up implementation (Domain → Infrastructure → Application → Presentation)

---

## Phase 1: Domain Layer - Interfaces & Models

### 1.1 Create ToolSourceType Enum Entry

**File**: `TransparentAiAgentCore/Domain/Tools/ToolSourceType.cs`

**Task**: Add new enum value
```csharp
/// <summary>
/// Built-in long-term memory tool
/// Used by agents to read and update persistent user memory
/// </summary>
BuiltInLongTermMemory
```

**Test**: Not applicable (enum addition)

**Commit**: "Add BuiltInLongTermMemory to ToolSourceType enum"

---

### 1.2 Create Memory Domain Models

**File**: `TransparentAiAgentCore/Domain/Memory/MemoryUpdateResult.cs`

```csharp
namespace TransparentAiAgentCore.Domain.Memory;

/// <summary>
/// Result of a memory update operation.
/// </summary>
public record MemoryUpdateResult(
    bool Success,
    string? Error = null,
    int CharacterCount = 0,
    DateTime UpdatedAt = default);
```

**Test**: Not applicable (simple record)

**Commit**: "Add MemoryUpdateResult domain model"

---

### 1.3 Create ILongTermMemoryService Interface

**File**: `TransparentAiAgentCore/Domain/Memory/ILongTermMemoryService.cs`

```csharp
using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Domain.Memory;

/// <summary>
/// Service for managing long-term memory storage.
/// Provides mode-aware memory persistence using markdown files.
/// </summary>
public interface ILongTermMemoryService
{
    /// <summary>
    /// Reads the memory file for the specified mode.
    /// Returns empty string if file doesn't exist.
    /// </summary>
    Task<string> ReadMemoryAsync(AppMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the memory file for the specified mode.
    /// Creates file if it doesn't exist.
    /// </summary>
    Task<MemoryUpdateResult> UpdateMemoryAsync(
        AppMode mode,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if memory exists for the specified mode.
    /// </summary>
    Task<bool> HasMemoryAsync(AppMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last update timestamp for the specified mode.
    /// Returns null if file doesn't exist.
    /// </summary>
    Task<DateTime?> GetLastUpdateTimeAsync(AppMode mode, CancellationToken cancellationToken = default);
}
```

**Test**: Not applicable (interface only)

**Commit**: "Add ILongTermMemoryService interface"

---

### 1.4 Create Configuration Model

**File**: `TransparentAiAgentCore/Domain/Memory/LongTermMemoryConfiguration.cs`

```csharp
namespace TransparentAiAgentCore.Domain.Memory;

/// <summary>
/// Configuration for long-term memory feature.
/// </summary>
public class LongTermMemoryConfiguration
{
    /// <summary>
    /// Enable/disable long-term memory feature globally.
    /// User can still toggle per-session via UI checkbox.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Directory path for memory files (relative to app root).
    /// </summary>
    public string StorageDirectory { get; set; } = "data/memory";

    /// <summary>
    /// Maximum memory file size in characters.
    /// </summary>
    public int MaxCharacters { get; set; } = 10_000;

    /// <summary>
    /// Whether to auto-load memory at conversation start.
    /// </summary>
    public bool AutoLoadOnStart { get; set; } = true;

    /// <summary>
    /// Whether to prompt for memory update on conversation end.
    /// </summary>
    public bool PromptUpdateOnEnd { get; set; } = true;

    /// <summary>
    /// Timeout for memory update prompt (seconds).
    /// </summary>
    public int UpdatePromptTimeoutSeconds { get; set; } = 30;
}
```

**Test**: Not applicable (simple configuration class)

**Commit**: "Add LongTermMemoryConfiguration model"

---

## Phase 2: Infrastructure Layer - Service Implementation

### 2.1 Implement LongTermMemoryService (TDD)

**File**: `TransparentAiAgentCore/Infrastructure/Memory/LongTermMemoryService.cs`
**Test File**: `TransparentAiAgentCore_Tests/Infrastructure/Memory/LongTermMemoryServiceTests.cs`

#### Test 2.1.1: ReadMemoryAsync - File Doesn't Exist
```csharp
[TestMethod]
public async Task ReadMemoryAsync_FileDoesNotExist_ReturnsEmptyString()
{
    // Arrange
    var service = CreateService(tempDirectory);

    // Act
    var result = await service.ReadMemoryAsync(AppMode.Normal);

    // Assert
    Assert.AreEqual(string.Empty, result);
}
```

**Implementation**: Return empty string if file doesn't exist

**Commit**: "Implement ReadMemoryAsync for non-existent files"

---

#### Test 2.1.2: UpdateMemoryAsync - Creates New File
```csharp
[TestMethod]
public async Task UpdateMemoryAsync_NewFile_CreatesFileWithContent()
{
    // Arrange
    var service = CreateService(tempDirectory);
    var content = "# Test Memory\nContent here";

    // Act
    var result = await service.UpdateMemoryAsync(AppMode.Normal, content);

    // Assert
    Assert.IsTrue(result.Success);
    Assert.AreEqual(content.Length, result.CharacterCount);

    var readBack = await service.ReadMemoryAsync(AppMode.Normal);
    Assert.AreEqual(content, readBack);
}
```

**Implementation**: Create directory if needed, write content to file

**Commit**: "Implement UpdateMemoryAsync - create new files"

---

#### Test 2.1.3: UpdateMemoryAsync - Overwrites Existing File
```csharp
[TestMethod]
public async Task UpdateMemoryAsync_ExistingFile_OverwritesContent()
{
    // Arrange
    var service = CreateService(tempDirectory);
    await service.UpdateMemoryAsync(AppMode.Normal, "Old content");

    // Act
    var newContent = "New content";
    var result = await service.UpdateMemoryAsync(AppMode.Normal, newContent);

    // Assert
    Assert.IsTrue(result.Success);
    var readBack = await service.ReadMemoryAsync(AppMode.Normal);
    Assert.AreEqual(newContent, readBack);
}
```

**Implementation**: Overwrite existing file

**Commit**: "Implement UpdateMemoryAsync - overwrite existing files"

---

#### Test 2.1.4: UpdateMemoryAsync - Size Limit Enforcement
```csharp
[TestMethod]
public async Task UpdateMemoryAsync_ContentExceedsMaxSize_ReturnsFailure()
{
    // Arrange
    var config = new LongTermMemoryConfiguration { MaxCharacters = 100 };
    var service = CreateService(tempDirectory, config);
    var oversizedContent = new string('x', 101);

    // Act
    var result = await service.UpdateMemoryAsync(AppMode.Normal, oversizedContent);

    // Assert
    Assert.IsFalse(result.Success);
    Assert.IsNotNull(result.Error);
    Assert.IsTrue(result.Error.Contains("exceeds maximum"));
}
```

**Implementation**: Validate content length before writing

**Commit**: "Add size limit validation to UpdateMemoryAsync"

---

#### Test 2.1.5: Mode-Aware File Selection
```csharp
[TestMethod]
public async Task MemoryService_DifferentModes_UsesSeparateFiles()
{
    // Arrange
    var service = CreateService(tempDirectory);
    var normalContent = "Normal mode memory";
    var teachingContent = "Teaching mode memory";

    // Act
    await service.UpdateMemoryAsync(AppMode.Normal, normalContent);
    await service.UpdateMemoryAsync(AppMode.Teaching, teachingContent);

    var normalRead = await service.ReadMemoryAsync(AppMode.Normal);
    var teachingRead = await service.ReadMemoryAsync(AppMode.Teaching);

    // Assert
    Assert.AreEqual(normalContent, normalRead);
    Assert.AreEqual(teachingContent, teachingRead);
    Assert.AreNotEqual(normalRead, teachingRead);
}
```

**Implementation**: Map AppMode to different file names

**Commit**: "Implement mode-aware file selection"

---

#### Test 2.1.6: HasMemoryAsync
```csharp
[TestMethod]
public async Task HasMemoryAsync_FileExists_ReturnsTrue()
{
    // Arrange
    var service = CreateService(tempDirectory);
    await service.UpdateMemoryAsync(AppMode.Normal, "Content");

    // Act
    var result = await service.HasMemoryAsync(AppMode.Normal);

    // Assert
    Assert.IsTrue(result);
}

[TestMethod]
public async Task HasMemoryAsync_FileDoesNotExist_ReturnsFalse()
{
    // Arrange
    var service = CreateService(tempDirectory);

    // Act
    var result = await service.HasMemoryAsync(AppMode.Normal);

    // Assert
    Assert.IsFalse(result);
}
```

**Implementation**: Check if file exists

**Commit**: "Implement HasMemoryAsync"

---

#### Test 2.1.7: GetLastUpdateTimeAsync
```csharp
[TestMethod]
public async Task GetLastUpdateTimeAsync_FileExists_ReturnsTimestamp()
{
    // Arrange
    var service = CreateService(tempDirectory);
    var before = DateTime.UtcNow;
    await service.UpdateMemoryAsync(AppMode.Normal, "Content");
    var after = DateTime.UtcNow;

    // Act
    var result = await service.GetLastUpdateTimeAsync(AppMode.Normal);

    // Assert
    Assert.IsNotNull(result);
    Assert.IsTrue(result >= before && result <= after);
}

[TestMethod]
public async Task GetLastUpdateTimeAsync_FileDoesNotExist_ReturnsNull()
{
    // Arrange
    var service = CreateService(tempDirectory);

    // Act
    var result = await service.GetLastUpdateTimeAsync(AppMode.Normal);

    // Assert
    Assert.IsNull(result);
}
```

**Implementation**: Get file's last write time

**Commit**: "Implement GetLastUpdateTimeAsync"

---

#### Test 2.1.8: Error Handling - Permission Denied
```csharp
[TestMethod]
public async Task UpdateMemoryAsync_PermissionDenied_ReturnsFailure()
{
    // Arrange - create read-only directory (platform-specific)
    // Act
    // Assert
    // Note: This test might be platform-specific, consider skipping on some platforms
}
```

**Implementation**: Catch IOException and return failure result

**Commit**: "Add error handling for file I/O failures"

---

**Service Implementation Skeleton**:
```csharp
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Infrastructure.Memory;

public class LongTermMemoryService : ILongTermMemoryService
{
    private readonly LongTermMemoryConfiguration _config;
    private readonly ILogger<LongTermMemoryService> _logger;
    private readonly string _storageDirectory;

    public LongTermMemoryService(
        LongTermMemoryConfiguration config,
        ILogger<LongTermMemoryService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.StorageDirectory);
    }

    private string GetMemoryFilePath(AppMode mode)
    {
        var fileName = mode switch
        {
            AppMode.Normal => "memory-normal.md",
            AppMode.Teaching => "memory-teaching.md",
            _ => throw new ArgumentException($"Unknown mode: {mode}")
        };
        return Path.Combine(_storageDirectory, fileName);
    }

    // ... implement interface methods
}
```

---

## Phase 3: Infrastructure Layer - Tools

### 3.1 Create Long-Term Memory Tools (TDD)

**Files**:
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInLongTermMemory/LongTermMemoryReadTool.cs`
- `TransparentAiAgentCore/Infrastructure/Tools/BuiltInLongTermMemory/LongTermMemoryUpdateTool.cs`

**Test File**: `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInLongTermMemory/LongTermMemoryToolsTests.cs`

#### Test 3.1.1: Tool Metadata
```csharp
[TestMethod]
public void LongTermMemoryReadTool_HasCorrectMetadata()
{
    // Arrange
    var tool = new LongTermMemoryReadTool();

    // Assert
    Assert.AreEqual("long_term_memory_read", tool.Name);
    Assert.AreEqual(ToolSourceType.BuiltInLongTermMemory, tool.SourceType);
    Assert.IsNotNull(tool.Description);
    Assert.IsNotNull(tool.ParametersSchema);
}

[TestMethod]
public void LongTermMemoryUpdateTool_HasCorrectMetadata()
{
    // Arrange
    var tool = new LongTermMemoryUpdateTool();

    // Assert
    Assert.AreEqual("long_term_memory_update", tool.Name);
    Assert.AreEqual(ToolSourceType.BuiltInLongTermMemory, tool.SourceType);
    Assert.IsTrue(tool.Description.Contains("GUARDRAILS")); // Ensure guardrails are in description
}
```

**Implementation**: Create tool classes implementing ITool

**Commit**: "Add LongTermMemoryReadTool and LongTermMemoryUpdateTool"

---

### 3.2 Create Tool Executor (TDD)

**File**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInLongTermMemory/LongTermMemoryToolExecutor.cs`
**Test File**: `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInLongTermMemory/LongTermMemoryToolExecutorTests.cs`

#### Test 3.2.1: Execute Read Tool
```csharp
[TestMethod]
public async Task ExecuteAsync_ReadTool_ReturnsMemoryContent()
{
    // Arrange
    var mockMemoryService = new Mock<ILongTermMemoryService>();
    var mockAppModeService = new Mock<IAppModeService>();
    mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
    mockMemoryService.Setup(x => x.ReadMemoryAsync(AppMode.Normal, It.IsAny<CancellationToken>()))
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

**Implementation**: Execute read tool by calling service

**Commit**: "Implement read tool execution"

---

#### Test 3.2.2: Execute Update Tool - Success
```csharp
[TestMethod]
public async Task ExecuteAsync_UpdateTool_ValidContent_UpdatesMemory()
{
    // Arrange
    var mockMemoryService = new Mock<ILongTermMemoryService>();
    var mockAppModeService = new Mock<IAppModeService>();
    mockAppModeService.Setup(x => x.CurrentMode).Returns(AppMode.Normal);
    mockMemoryService.Setup(x => x.UpdateMemoryAsync(
            AppMode.Normal,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new MemoryUpdateResult(true, null, 100, DateTime.UtcNow));

    var executor = CreateExecutor(mockMemoryService, mockAppModeService);
    var tool = new LongTermMemoryUpdateTool();
    var args = @"{""content"": ""New memory"", ""reason"": ""Test update""}";

    // Act
    var result = await executor.ExecuteAsync(tool, args, CancellationToken.None);

    // Assert
    Assert.IsTrue(result.Success);
    mockMemoryService.Verify(x => x.UpdateMemoryAsync(
        AppMode.Normal,
        "New memory",
        It.IsAny<CancellationToken>()), Times.Once);
}
```

**Implementation**: Parse arguments and call service

**Commit**: "Implement update tool execution"

---

#### Test 3.2.3: Execute Update Tool - Missing Arguments
```csharp
[TestMethod]
public async Task ExecuteAsync_UpdateTool_MissingContent_ReturnsFailure()
{
    // Arrange
    var executor = CreateExecutor();
    var tool = new LongTermMemoryUpdateTool();
    var args = @"{""reason"": ""Test""}"; // Missing content

    // Act
    var result = await executor.ExecuteAsync(tool, args, CancellationToken.None);

    // Assert
    Assert.IsFalse(result.Success);
    Assert.IsTrue(result.Output.Contains("content"));
}
```

**Implementation**: Validate required arguments

**Commit**: "Add argument validation to update tool executor"

---

#### Test 3.2.4: Unknown Tool
```csharp
[TestMethod]
public async Task ExecuteAsync_UnknownTool_ReturnsFailure()
{
    // Arrange
    var executor = CreateExecutor();
    var unknownTool = new Mock<ITool>();
    unknownTool.Setup(x => x.Name).Returns("unknown_tool");
    unknownTool.Setup(x => x.SourceType).Returns(ToolSourceType.BuiltInLongTermMemory);

    // Act
    var result = await executor.ExecuteAsync(unknownTool.Object, "{}", CancellationToken.None);

    // Assert
    Assert.IsFalse(result.Success);
}
```

**Implementation**: Check tool name and return error for unknown tools

**Commit**: "Add unknown tool handling"

---

**Executor Implementation Skeleton**:
```csharp
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

public class LongTermMemoryToolExecutor : IToolExecutor
{
    private readonly ILongTermMemoryService _memoryService;
    private readonly IAppModeService _appModeService;
    private readonly ILogger<LongTermMemoryToolExecutor> _logger;

    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;

    // ... implement ExecuteAsync
}
```

---

### 3.3 Create Tool Registry (TDD)

**File**: `TransparentAiAgentCore/Infrastructure/Tools/BuiltInLongTermMemory/BuiltInLongTermMemoryToolRegistry.cs`
**Test File**: `TransparentAiAgentCore_Tests/Infrastructure/Tools/BuiltInLongTermMemory/BuiltInLongTermMemoryToolRegistryTests.cs`

#### Test 3.3.1: GetAllTools Returns Two Tools
```csharp
[TestMethod]
public void GetAllTools_ReturnsReadAndUpdateTools()
{
    // Arrange
    var registry = new BuiltInLongTermMemoryToolRegistry();

    // Act
    var tools = registry.GetAllTools();

    // Assert
    Assert.AreEqual(2, tools.Count);
    Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_read"));
    Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_update"));
}
```

**Commit**: "Add BuiltInLongTermMemoryToolRegistry with two tools"

---

#### Test 3.3.2: GetTool By Name
```csharp
[TestMethod]
public void GetTool_ValidName_ReturnsTool()
{
    // Arrange
    var registry = new BuiltInLongTermMemoryToolRegistry();

    // Act
    var tool = registry.GetTool("long_term_memory_read");

    // Assert
    Assert.IsNotNull(tool);
    Assert.AreEqual("long_term_memory_read", tool.Name);
}

[TestMethod]
public void GetTool_InvalidName_ReturnsNull()
{
    // Arrange
    var registry = new BuiltInLongTermMemoryToolRegistry();

    // Act
    var tool = registry.GetTool("nonexistent_tool");

    // Assert
    Assert.IsNull(tool);
}
```

**Commit**: "Implement GetTool by name"

---

**Registry Implementation**:
```csharp
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

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

    // ... implement interface
}
```

---

## Phase 4: Application Layer - Integration

### 4.1 Update ConversationUIService (TDD)

**File**: `TransparentAiAgentGui/Services/ConversationUIService.cs`
**Test File**: `TransparentAiAgentGui_Tests/Services/ConversationUIServiceMemoryTests.cs` (new file)

#### Test 4.1.1: Memory Enable/Disable
```csharp
[TestMethod]
public async Task SetMemoryEnabledAsync_EnablesMemory()
{
    // Arrange
    var service = CreateService();

    // Act
    await service.SetMemoryEnabledAsync(true);

    // Assert
    Assert.IsTrue(service.IsMemoryEnabled);
}
```

**Implementation**: Add `IsMemoryEnabled` property and `SetMemoryEnabledAsync` method

**Commit**: "Add memory enable/disable to ConversationUIService"

---

#### Test 4.1.2: Auto-Load Memory on Enable
```csharp
[TestMethod]
public async Task SetMemoryEnabledAsync_AutoLoadEnabled_LoadsMemory()
{
    // Arrange
    var mockMemoryService = new Mock<ILongTermMemoryService>();
    mockMemoryService.Setup(x => x.ReadMemoryAsync(It.IsAny<AppMode>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync("Test memory");

    var config = new LongTermMemoryConfiguration { AutoLoadOnStart = true };
    var service = CreateService(memoryService: mockMemoryService.Object, config: config);

    // Act
    await service.SetMemoryEnabledAsync(true);

    // Assert
    // Verify memory was loaded (check orchestrator received system message)
}
```

**Implementation**: Auto-load memory when enabled (if config allows)

**Commit**: "Implement auto-load memory on enable"

---

#### Test 4.1.3: End Conversation - Prompt for Memory Update
```csharp
[TestMethod]
public async Task EndConversationAsync_MemoryEnabled_SendsUpdatePrompt()
{
    // Arrange
    var mockOrchestrator = new Mock<IAgentOrchestrator>();
    var config = new LongTermMemoryConfiguration { PromptUpdateOnEnd = true };
    var service = CreateService(orchestrator: mockOrchestrator.Object, config: config);
    await service.SetMemoryEnabledAsync(true);

    // Act
    await service.EndConversationAsync();

    // Assert
    mockOrchestrator.Verify(x => x.ProcessSystemMessageAsync(
        It.Is<IMessage>(m => m.Content.Contains("review our conversation")),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

**Implementation**: Add `EndConversationAsync` method

**Commit**: "Implement end conversation memory update prompt"

---

### 4.2 Update IConversationUIService Interface

**File**: `TransparentAiAgentGui/Services/IConversationUIService.cs`

Add new members:
```csharp
bool IsMemoryEnabled { get; }
Task SetMemoryEnabledAsync(bool enabled);
Task EndConversationAsync();
```

**Commit**: "Add memory methods to IConversationUIService"

---

### 4.3 Dependency Injection Registration

**File**: `TransparentAiAgentGui/Program.cs`

Add registrations:
```csharp
// Memory configuration
builder.Services.Configure<LongTermMemoryConfiguration>(
    builder.Configuration.GetSection("LongTermMemory"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<LongTermMemoryConfiguration>>().Value);

// Memory service
builder.Services.AddScoped<ILongTermMemoryService, LongTermMemoryService>();

// Memory tools
builder.Services.AddSingleton<IToolRegistry, BuiltInLongTermMemoryToolRegistry>();
builder.Services.AddSingleton<IToolExecutor, LongTermMemoryToolExecutor>();
```

**Note**: Registry and executor will be picked up by ToolManager's composite pattern

**Commit**: "Register memory services in DI container"

---

### 4.4 Update appsettings.json

**File**: `TransparentAiAgentGui/appsettings.json`

Add configuration section:
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

**Commit**: "Add LongTermMemory configuration to appsettings"

---

## Phase 5: UI Layer - Components

### 5.1 Add Memory Checkbox to Home.razor

**File**: `TransparentAiAgentGui/Components/Pages/Home.razor`

Add checkbox below conversation selector:
```razor
@* Long-term memory control *@
<div class="memory-control">
    <label class="memory-checkbox">
        <input type="checkbox"
               @bind="isMemoryEnabled"
               @bind:after="OnMemoryToggled"
               disabled="@(!isMemoryFeatureEnabled)" />
        <span>Use long-term memory</span>
        @if (isMemoryFeatureEnabled)
        {
            <span class="info-icon"
                  title="Allow agent to remember your preferences and context across conversations">
                ℹ️
            </span>
        }
    </label>

    @if (isMemoryEnabled && hasMemory)
    {
        <div class="memory-actions">
            <button class="btn-link btn-sm" @onclick="ViewMemory">View</button>
            <button class="btn-link btn-sm" @onclick="ClearMemory">Clear</button>
        </div>
    }
</div>
```

**Code-behind additions**:
```csharp
@code {
    [Inject] private ILongTermMemoryService MemoryService { get; set; } = default!;
    [Inject] private LongTermMemoryConfiguration MemoryConfig { get; set; } = default!;

    private bool isMemoryFeatureEnabled = false;
    private bool isMemoryEnabled = false;
    private bool hasMemory = false;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        isMemoryFeatureEnabled = MemoryConfig.Enabled;

        // Check if memory exists for current mode
        if (isMemoryFeatureEnabled)
        {
            hasMemory = await MemoryService.HasMemoryAsync(AppModeService.CurrentMode);
        }
    }

    private async Task OnMemoryToggled()
    {
        await ConversationUIService.SetMemoryEnabledAsync(isMemoryEnabled);

        if (isMemoryEnabled)
        {
            hasMemory = await MemoryService.HasMemoryAsync(AppModeService.CurrentMode);
        }
    }

    private async Task ViewMemory()
    {
        // Show memory viewer overlay (Phase 5.3)
    }

    private async Task ClearMemory()
    {
        // Confirm and clear memory (Phase 5.4)
    }
}
```

**Commit**: "Add memory checkbox to Home page"

---

### 5.2 Style Memory Controls

**File**: `TransparentAiAgentGui/Components/Pages/Home.razor.css`

Add styles:
```css
.memory-control {
    margin-bottom: 1rem;
    padding: 0.5rem;
    background-color: var(--surface-color);
    border-radius: 4px;
}

.memory-checkbox {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;
}

.memory-checkbox input[type="checkbox"] {
    cursor: pointer;
}

.info-icon {
    cursor: help;
    opacity: 0.7;
    font-size: 0.9em;
}

.memory-actions {
    display: flex;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

.btn-link {
    background: none;
    border: none;
    color: var(--primary-color);
    cursor: pointer;
    text-decoration: underline;
    padding: 0;
}

.btn-link:hover {
    color: var(--primary-hover-color);
}

.btn-sm {
    font-size: 0.875rem;
}
```

**Commit**: "Add styling for memory controls"

---

### 5.3 Create Memory Viewer Overlay

**File**: `TransparentAiAgentGui/Components/Shared/MemoryViewerOverlay.razor`

Create new overlay component:
```razor
@if (isVisible)
{
    <div class="overlay" @onclick="Close">
        <div class="overlay-content memory-viewer" @onclick:stopPropagation>
            <div class="overlay-header">
                <h3>Long-Term Memory (@CurrentMode Mode)</h3>
                <button class="btn-close" @onclick="Close">✕</button>
            </div>

            <div class="overlay-body">
                @if (isEditing)
                {
                    <textarea class="memory-editor"
                              @bind="editedContent"
                              rows="20"></textarea>
                }
                else
                {
                    <div class="memory-display markdown-content">
                        @((MarkupString)renderedMarkdown)
                    </div>
                }
            </div>

            <div class="overlay-footer">
                @if (isEditing)
                {
                    <button class="btn btn-primary" @onclick="SaveChanges">Save</button>
                    <button class="btn btn-secondary" @onclick="CancelEditing">Cancel</button>
                }
                else
                {
                    <button class="btn btn-secondary" @onclick="StartEditing">Edit</button>
                    <button class="btn btn-secondary" @onclick="Close">Close</button>
                }
            </div>
        </div>
    </div>
}

@code {
    [Inject] private ILongTermMemoryService MemoryService { get; set; } = default!;
    [Inject] private IAppModeService AppModeService { get; set; } = default!;

    private bool isVisible = false;
    private bool isEditing = false;
    private string memoryContent = string.Empty;
    private string editedContent = string.Empty;
    private string renderedMarkdown = string.Empty;
    private AppMode CurrentMode => AppModeService.CurrentMode;

    public async Task ShowAsync()
    {
        memoryContent = await MemoryService.ReadMemoryAsync(CurrentMode);
        editedContent = memoryContent;
        renderedMarkdown = RenderMarkdown(memoryContent);
        isVisible = true;
        StateHasChanged();
    }

    private void Close()
    {
        isVisible = false;
        isEditing = false;
    }

    private void StartEditing()
    {
        editedContent = memoryContent;
        isEditing = true;
    }

    private void CancelEditing()
    {
        isEditing = false;
    }

    private async Task SaveChanges()
    {
        var result = await MemoryService.UpdateMemoryAsync(CurrentMode, editedContent);

        if (result.Success)
        {
            memoryContent = editedContent;
            renderedMarkdown = RenderMarkdown(memoryContent);
            isEditing = false;
        }
        else
        {
            // Show error message (toast notification?)
        }
    }

    private string RenderMarkdown(string markdown)
    {
        // Use Markdig or similar library to render markdown to HTML
        // For now, simple pre-formatted display
        return $"<pre>{System.Web.HttpUtility.HtmlEncode(markdown)}</pre>";
    }
}
```

**Commit**: "Add MemoryViewerOverlay component"

---

### 5.4 Implement View/Clear Memory Actions

**File**: `TransparentAiAgentGui/Components/Pages/Home.razor`

Add overlay reference and implement actions:
```razor
<MemoryViewerOverlay @ref="memoryViewer" />

@code {
    private MemoryViewerOverlay memoryViewer = default!;

    private async Task ViewMemory()
    {
        await memoryViewer.ShowAsync();
    }

    private async Task ClearMemory()
    {
        // TODO: Add confirmation dialog
        var confirmed = true; // await ConfirmationDialog.ShowAsync("Clear long-term memory?");

        if (confirmed)
        {
            var result = await MemoryService.UpdateMemoryAsync(
                AppModeService.CurrentMode,
                string.Empty);

            if (result.Success)
            {
                hasMemory = false;
                StateHasChanged();
            }
        }
    }
}
```

**Commit**: "Implement view and clear memory actions"

---

### 5.5 Rename "Clear Conversation" to "End Conversation"

**File**: `TransparentAiAgentGui/Components/Pages/Home.razor`

Find and replace button text/method name:
```razor
<button class="btn btn-secondary" @onclick="EndConversation">
    End Conversation
</button>

@code {
    private async Task EndConversation()
    {
        // Trigger memory update prompt if enabled
        await ConversationUIService.EndConversationAsync();

        // Then clear conversation
        await ConversationUIService.ClearConversationAsync();
    }
}
```

**Commit**: "Rename Clear Conversation to End Conversation"

---

## Phase 6: Testing & Validation

### 6.1 Integration Tests

**File**: `TransparentAiAgentCore_Tests/Integration/LongTermMemoryIntegrationTests.cs`

#### Test 6.1.1: End-to-End Memory Flow
```csharp
[TestMethod]
public async Task EndToEnd_EnableMemory_UpdateAndReload_Success()
{
    // Arrange - set up full service stack
    var services = new ServiceCollection();
    // ... register all services

    var provider = services.BuildServiceProvider();
    var uiService = provider.GetRequiredService<IConversationUIService>();
    var memoryService = provider.GetRequiredService<ILongTermMemoryService>();

    // Act
    await uiService.SetMemoryEnabledAsync(true);
    await memoryService.UpdateMemoryAsync(AppMode.Normal, "Test memory");
    var retrieved = await memoryService.ReadMemoryAsync(AppMode.Normal);

    // Assert
    Assert.AreEqual("Test memory", retrieved);
}
```

**Commit**: "Add end-to-end integration tests"

---

### 6.2 Manual Testing Checklist

Create manual test scenarios document:

**File**: `docs/testing/long-term-memory-manual-tests.md`

```markdown
# Long-Term Memory - Manual Testing Checklist

## Test Scenario 1: First-Time User
1. Enable feature in appsettings.json
2. Start app
3. Check memory checkbox
4. Have a conversation introducing yourself
5. Click "End Conversation"
6. Verify memory file created in data/memory/
7. Start new conversation
8. Verify agent remembers you

## Test Scenario 2: Memory Viewing
1. Enable memory and have conversation
2. Update memory
3. Click "View" button
4. Verify markdown is rendered
5. Click "Edit"
6. Modify content
7. Click "Save"
8. Verify changes persisted

## Test Scenario 3: Mode Isolation
1. Enable memory in Normal mode
2. Store some info
3. Switch to Teaching mode
4. Verify separate memory
5. Update Teaching memory
6. Switch back to Normal
7. Verify Normal memory unchanged

## Test Scenario 4: Size Limit
1. Enable memory
2. Try to store content > 10,000 chars
3. Verify error message
4. Check transparency logs

## Test Scenario 5: Privacy
1. Have conversation with sensitive info
2. Check if agent self-censors
3. View memory
4. Verify no sensitive data stored
```

**Commit**: "Add manual testing checklist"

---

## Phase 7: Documentation

### 7.1 Component Documentation

**File**: `docs/04-components/infrastructure/long-term-memory-service.md`

Create comprehensive component doc following template

**Commit**: "Add LongTermMemoryService component documentation"

---

### 7.2 User Guide

**File**: `docs/05-guides/features/using-long-term-memory.md`

Create user-facing guide:
- What is long-term memory?
- How to enable it
- What gets stored (and what doesn't)
- How to view/edit/clear memory
- Privacy considerations

**Commit**: "Add user guide for long-term memory"

---

### 7.3 Update README

**File**: `docs/README.md`

Add reference to long-term memory in feature list and guides section

**Commit**: "Update README with long-term memory feature"

---

## Phase 8: Cleanup & Polish

### 8.1 Code Review Checklist

- [ ] All tests passing (unit + integration)
- [ ] No compiler warnings
- [ ] Consistent coding style
- [ ] XML documentation comments on public APIs
- [ ] Proper error handling and logging
- [ ] Configuration validated
- [ ] No hardcoded paths or magic strings
- [ ] DI registrations correct
- [ ] Memory properly disposed (IDisposable if needed)

### 8.2 Performance Check

- [ ] File I/O is async
- [ ] No unnecessary file reads
- [ ] Memory service doesn't block UI
- [ ] Tool execution < 50ms for local operations

### 8.3 Security Check

- [ ] Guardrails in tool descriptions are clear
- [ ] Size limits enforced
- [ ] File paths validated (no directory traversal)
- [ ] No secrets in memory files (user responsibility + guidance)

---

## Commit Strategy

Follow atomic commits:
- Each test + implementation = 1 commit
- Commits should be small and focused
- Commit message format: "{action} {what}" (e.g., "Add ILongTermMemoryService interface")
- Group related commits before push

**Push frequency**: After each completed phase or every ~5-10 commits

---

## Risk Mitigation

### Risk 1: File I/O Errors
**Mitigation**: Comprehensive error handling, return failure results, log all errors

### Risk 2: Memory Size Growth
**Mitigation**: 10K character limit, LLM must summarize if exceeded

### Risk 3: Sensitive Data Storage
**Mitigation**: Clear guardrails, user education, transparency (user can always view memory)

### Risk 4: Mode Switching Edge Cases
**Mitigation**: Tests for mode isolation, mode-aware file selection

### Risk 5: Concurrent Access
**Mitigation**: File locking if needed (future), scoped services for isolation

---

## Success Criteria

Implementation is complete when:

1. ✅ All unit tests passing (>90% coverage on core logic)
2. ✅ Integration tests passing
3. ✅ Manual tests completed successfully
4. ✅ Documentation complete
5. ✅ Feature enabled in appsettings with default = false
6. ✅ UI works correctly (checkbox, view, clear)
7. ✅ Memory persists across app restarts
8. ✅ Mode isolation works correctly
9. ✅ End conversation triggers memory update
10. ✅ No regressions in existing features

---

## Estimated Effort

- **Phase 1**: 1-2 hours (domain models, interfaces)
- **Phase 2**: 3-4 hours (service implementation + tests)
- **Phase 3**: 2-3 hours (tools, executor, registry + tests)
- **Phase 4**: 2-3 hours (application integration + tests)
- **Phase 5**: 3-4 hours (UI components)
- **Phase 6**: 2-3 hours (integration tests, manual testing)
- **Phase 7**: 2-3 hours (documentation)
- **Phase 8**: 1-2 hours (cleanup, polish)

**Total**: ~16-24 hours (2-3 full development days)

---

## Next Steps

1. Review this plan with user
2. Start Phase 1 (domain layer)
3. Follow TDD approach strictly
4. Commit frequently
5. Push after each phase
6. Update this document if any changes needed during implementation

---

## Critical Design Review - Implementation Impact

**⚠️ BLOCKING ISSUES**: Implementation cannot proceed until critical design issues are resolved.

**See**: [LONG_TERM_MEMORY_DESIGN_REVIEW.md](./LONG_TERM_MEMORY_DESIGN_REVIEW.md) for complete analysis.

### Issues Blocking Implementation

#### 1. AppModeService Layering (Blocks Phase 3)

**Problem**: `LongTermMemoryToolExecutor` needs `IAppModeService`, but interface is in GUI layer.

**Solution Required**: Move `IAppModeService` to Domain layer before Phase 1.

**New Phase 0** (if Option A chosen):
- Move `TransparentAiAgentGui/Services/IAppModeService.cs` → `TransparentAiAgentCore/Domain/UIControl/IAppModeService.cs`
- Update all references
- Verify existing code still compiles
- Run existing tests
- **Commit**: "Move IAppModeService to Domain layer for clean architecture"

**Alternative**: If Option B or C chosen, update Phase 3 accordingly.

#### 2. System Message Injection (Affects Phase 4)

**Problem**: Design doesn't specify HOW to inject memory into conversation.

**Solution Required**: Add explicit implementation to Phase 4.1:

```csharp
// In ConversationUIService.LoadMemoryIntoConversation()
private async Task LoadMemoryIntoConversation()
{
    if (!IsMemoryEnabled) return;

    var memoryContent = await _memoryService.ReadMemoryAsync(_appModeService.CurrentMode);

    if (string.IsNullOrWhiteSpace(memoryContent))
    {
        _logger.LogDebug("Memory empty, skipping injection");
        return;
    }

    // Get current system prompt
    var currentPrompt = _appConfiguration.Agent.SystemPrompt;

    // Merge memory into system prompt
    var mergedPrompt = $@"{currentPrompt}

---

## LONG-TERM MEMORY

{memoryContent}

---

Use this memory to personalize your responses. You can update it anytime using the long_term_memory_update tool.";

    // Update conversation manager
    _conversationManager.UpdateSystemPrompt(mergedPrompt);

    _logger.LogInformation("Loaded {CharCount} chars of memory into conversation",
        memoryContent.Length);
}
```

**Also needed**: Restore original prompt when disabling memory:
```csharp
public async Task SetMemoryEnabledAsync(bool enabled)
{
    IsMemoryEnabled = enabled;

    if (enabled)
    {
        await LoadMemoryIntoConversation();
    }
    else
    {
        // Restore original system prompt
        var originalPrompt = _appConfiguration.Agent.SystemPrompt;
        _conversationManager.UpdateSystemPrompt(originalPrompt);
    }
}
```

#### 3. Mode Switch Event Handling (Affects Phase 4)

**Problem**: No specification for hooking into mode switch to trigger memory operations.

**Solution Required**: Update AppModeService and ConversationUIService in Phase 4.

**Option A**: Add event subscription in ConversationUIService constructor:
```csharp
public ConversationUIService(...)
{
    // ... existing code

    // Subscribe to mode change to load new mode's memory
    _appModeService.ModeChanged += OnModeChanged;
}

private async void OnModeChanged(object? sender, AppMode newMode)
{
    if (IsMemoryEnabled)
    {
        await LoadMemoryIntoConversation(); // Load new mode's memory
    }
}
```

**Option B**: Add ModeSwitching event to AppModeService:
```csharp
// In IAppModeService (Domain)
event EventHandler<AppMode>? ModeSwitching; // BEFORE switch
event EventHandler<AppMode>? ModeChanged;   // AFTER switch

// In AppModeService implementation
public async Task SwitchModeAsync(AppMode newMode, bool clearConversation = false)
{
    // Fire BEFORE switching
    ModeSwitching?.Invoke(this, newMode);

    // Prompt for memory update (via ConversationUIService subscriber)
    // ... wait for completion

    // Do mode switch
    // ...

    // Fire AFTER switching
    ModeChanged?.Invoke(this, newMode);
}
```

### Updated Phase Breakdown

**Phase 0: Prerequisites** (NEW - 1-2 hours)
- 0.1: Resolve AppModeService layering (move interface or choose alternative)
- 0.2: Add Markdig NuGet package to TransparentAiAgentGui project
- 0.3: Verify all critical design decisions documented

**Phase 2 Additions**:
- Add to 2.1: File path validation (security)
- Add to 2.1: UTF-8 encoding explicit
- Add to 2.1: Directory auto-creation
- Add Test 2.1.9: Path validation test
- Add Test 2.1.10: UTF-8 encoding test

**Phase 4 Additions**:
- Add to 4.1: Explicit system message injection implementation
- Add to 4.1: Prompt restoration when disabling memory
- Add to 4.1: Mode switch event subscription
- Add Test 4.1.4: System message injection test
- Add Test 4.1.5: Mode switch triggers memory load test

**Phase 5 Additions**:
- Add to 5.1: localStorage persistence for checkbox state
- Add to 5.3: Character counter in memory editor
- Add to 5.3: Last updated timestamp display
- Add to 5.3: Error toast notifications
- Add Test 5.1.1: localStorage persistence test
- Add Test 5.3.1: Character counter shows correctly
- Add Test 5.3.2: Error feedback displays

### Revised Estimated Effort

- **Phase 0**: 1-2 hours (prerequisites)
- **Phase 1**: 1-2 hours (domain models, interfaces)
- **Phase 2**: 4-5 hours (service + additional validations + tests)
- **Phase 3**: 2-3 hours (tools, executor, registry + tests)
- **Phase 4**: 3-4 hours (application integration + event handling + tests)
- **Phase 5**: 4-5 hours (UI + localStorage + error feedback + tests)
- **Phase 6**: 2-3 hours (integration tests, manual testing)
- **Phase 7**: 2-3 hours (documentation)
- **Phase 8**: 1-2 hours (cleanup, polish)

**Total**: ~20-29 hours (3-4 full development days)

### Decision Checklist Before Implementation

- [ ] **CRITICAL**: AppModeService layering solution chosen (A, B, or C?)
- [ ] **CRITICAL**: System message injection approach approved
- [ ] **CRITICAL**: Mode switch event handling approach approved
- [ ] **HIGH**: Error feedback strategy defined (toast? inline? both?)
- [ ] **HIGH**: Markdown library chosen (Markdig?)
- [ ] **MEDIUM**: Checkbox persistence via localStorage approved
- [ ] **MEDIUM**: Known limitations documented and accepted

**Status**: ⛔ BLOCKED - Awaiting user decisions on critical issues
