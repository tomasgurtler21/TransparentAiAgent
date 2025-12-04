# Long-Term Memory Service

**Last Updated**: 2025-11-16
**Status**: Active
**Phase**: Phase 10
**Layer**: Infrastructure

---

## 📋 Document Scope

**What belongs in this document**:
- Detailed documentation for the LongTermMemoryService component
- Purpose, responsibilities, and architecture
- Interface definitions and contracts
- Dependencies and relationships with other components
- Implementation notes and design patterns
- Testing strategies specific to this component
- Usage examples with code

**What does NOT belong here**:
- ❌ General architecture overviews (→ belongs in [02-architecture/](../../02-architecture/))
- ❌ Cross-cutting concepts (→ belongs in [Long-Term Memory Concept](../../03-concepts/long-term-memory.md))
- ❌ Step-by-step guides (→ belongs in [Using Long-Term Memory](../../05-guides/features/using-long-term-memory.md))
- ❌ Session notes or temporary fixes (→ belongs in [09-archive/](../../09-archive/))

---

## Overview

The `LongTermMemoryService` is the core infrastructure component responsible for persisting and retrieving agent memory across conversations. It provides mode-aware file-based storage using markdown format, with built-in validation, security features, and error handling.

## Purpose

The service enables the agent to remember user context, preferences, and background information across conversation sessions by:
- Storing memory as markdown files in a configurable directory
- Providing separate memory files for each mode (Normal, Teaching)
- Enforcing size limits to keep memory focused
- Ensuring file path security to prevent directory traversal attacks
- Using explicit UTF-8 encoding for proper Unicode support

## Responsibilities

### Core Responsibilities

- **Read Memory**: Retrieve memory content for a specific mode
- **Update Memory**: Overwrite memory content with validation
- **Check Existence**: Determine if memory exists for a mode
- **Get Metadata**: Retrieve last update timestamp

### Cross-Cutting Responsibilities

- **File Management**: Create directories, handle file I/O operations
- **Validation**: Enforce character limits and file path security
- **Error Handling**: Gracefully handle I/O errors and return failure results
- **Logging**: Record all operations for debugging and transparency

## Architecture

### Interfaces

```csharp
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

### Domain Models

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

### Dependencies

**Depends on**:
- `LongTermMemoryConfiguration` - Configuration settings
- `ILogger<LongTermMemoryService>` - Logging infrastructure
- `System.IO` - File operations
- `System.Text` - UTF-8 encoding

**Used by**:
- `LongTermMemoryToolExecutor` - Executes memory tool operations
- `ConversationUIService` - Coordinates memory with conversation lifecycle
- UI components - Memory viewer, management features

### Internal Structure

```
LongTermMemoryService
├── Constructor
│   ├── Validates configuration
│   ├── Resolves storage directory path
│   └── Ensures path security
├── GetMemoryFilePath(mode)
│   └── Maps mode to filename
├── ReadMemoryAsync(mode)
│   ├── Gets file path
│   ├── Checks existence
│   └── Reads with UTF-8
├── UpdateMemoryAsync(mode, content)
│   ├── Validates content length
│   ├── Creates directory if needed
│   └── Writes with UTF-8
├── HasMemoryAsync(mode)
│   └── Checks file existence
└── GetLastUpdateTimeAsync(mode)
    └── Gets file metadata
```

## Implementation Notes

### Key Algorithms

#### Mode-to-Filename Mapping

```csharp
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
```

**Design Decision**: Hard-coded filenames ensure consistency and prevent dynamic path construction vulnerabilities.

#### File Path Security

```csharp
public LongTermMemoryService(
    LongTermMemoryConfiguration config,
    ILogger<LongTermMemoryService> logger)
{
    // Resolve and validate storage directory
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
    _storageDirectory = Path.GetFullPath(Path.Combine(baseDir, config.StorageDirectory));

    // Security: Ensure storage is within app directory
    if (!_storageDirectory.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Storage directory must be within application directory. " +
            $"Configured: {config.StorageDirectory}, Resolved: {_storageDirectory}");
    }
}
```

**Security Feature**: Prevents directory traversal attacks by validating that the resolved path stays within the application directory.

#### Size Limit Validation

```csharp
public async Task<MemoryUpdateResult> UpdateMemoryAsync(
    AppMode mode,
    string content,
    CancellationToken cancellationToken = default)
{
    // Validate size
    if (content.Length > _config.MaxCharacters)
    {
        return new MemoryUpdateResult(
            Success: false,
            Error: $"Memory content exceeds maximum size of {_config.MaxCharacters} characters");
    }
    // ... rest of implementation
}
```

**Design Decision**: Validation happens before I/O to fail fast and avoid partial writes.

### Design Patterns Used

**Repository Pattern**: The service acts as a repository for memory data, abstracting file system details from consumers.

**Result Object Pattern**: `MemoryUpdateResult` encapsulates success/failure state, error messages, and metadata, avoiding exceptions for expected failures.

**Dependency Injection**: Service is registered in DI container and injected where needed.

**Async/Await**: All I/O operations are async to avoid blocking.

### Important Considerations

#### UTF-8 Encoding

All file operations use **explicit UTF-8 encoding**:

```csharp
await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
```

**Reason**: Ensures proper handling of international characters, emojis, and special symbols in memory content.

#### Directory Auto-Creation

The service automatically creates the storage directory if it doesn't exist:

```csharp
Directory.CreateDirectory(_storageDirectory);
```

**Reason**: Simplifies deployment - no manual directory creation required.

#### Error Handling Strategy

File I/O errors are caught and returned as failure results, not thrown:

```csharp
try
{
    // ... file operations
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to update memory for {Mode} mode", mode);
    return new MemoryUpdateResult(
        Success: false,
        Error: $"Failed to write memory: {ex.Message}");
}
```

**Reason**: Allows callers to handle failures gracefully without try-catch blocks.

## Configuration

### Configuration Model

```csharp
namespace TransparentAiAgentCore.Domain.Memory;

public class LongTermMemoryConfiguration
{
    /// <summary>
    /// Enable/disable long-term memory feature globally.
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

### appsettings.json

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

### Dependency Injection Registration

```csharp
// In Program.cs or startup configuration

// Memory configuration
builder.Services.Configure<LongTermMemoryConfiguration>(
    builder.Configuration.GetSection("LongTermMemory"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<LongTermMemoryConfiguration>>().Value);

// Memory service
builder.Services.AddScoped<ILongTermMemoryService, LongTermMemoryService>();
```

## Testing Strategy

### Unit Tests

The service has comprehensive unit test coverage in `TransparentAiAgentCore_Tests/Infrastructure/Memory/LongTermMemoryServiceTests.cs`.

**Test Categories**:

#### Basic Operations
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

#### Create and Update
```csharp
[TestMethod]
public async Task UpdateMemoryAsync_NewFile_CreatesFileWithContent()
{
    // Test that new files are created correctly
}

[TestMethod]
public async Task UpdateMemoryAsync_ExistingFile_OverwritesContent()
{
    // Test that existing files are overwritten
}
```

#### Validation
```csharp
[TestMethod]
public async Task UpdateMemoryAsync_ContentExceedsMaxSize_ReturnsFailure()
{
    // Test size limit enforcement
}

[TestMethod]
public void Constructor_InvalidStorageDirectory_ThrowsException()
{
    // Test directory traversal protection
}
```

#### Mode Isolation
```csharp
[TestMethod]
public async Task MemoryService_DifferentModes_UsesSeparateFiles()
{
    // Test that Normal and Teaching modes use separate files
}
```

#### Character Encoding
```csharp
[TestMethod]
public async Task UpdateMemoryAsync_UnicodeContent_PreservesEncoding()
{
    // Test UTF-8 encoding with emojis and special characters
}
```

### Integration Tests

Integration tests in `TransparentAiAgentCore_Tests/Integration/LongTermMemoryIntegrationTests.cs` verify end-to-end functionality.

### Mocking Dependencies

For testing components that use `ILongTermMemoryService`:

```csharp
var mockMemoryService = new Mock<ILongTermMemoryService>();

// Setup read behavior
mockMemoryService
    .Setup(x => x.ReadMemoryAsync(AppMode.Normal, It.IsAny<CancellationToken>()))
    .ReturnsAsync("Test memory content");

// Setup update behavior
mockMemoryService
    .Setup(x => x.UpdateMemoryAsync(
        AppMode.Normal,
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(new MemoryUpdateResult(true, null, 100, DateTime.UtcNow));

// Use mock in tests
var service = new MyService(mockMemoryService.Object);
```

## Usage Examples

### Reading Memory

```csharp
public class MyService
{
    private readonly ILongTermMemoryService _memoryService;
    private readonly IAppModeService _appModeService;

    public async Task LoadUserContextAsync()
    {
        // Read memory for current mode
        var memory = await _memoryService.ReadMemoryAsync(_appModeService.CurrentMode);

        if (string.IsNullOrEmpty(memory))
        {
            _logger.LogInformation("No memory found for {Mode} mode", _appModeService.CurrentMode);
            return;
        }

        _logger.LogInformation("Loaded {CharCount} characters of memory", memory.Length);

        // Use memory content...
    }
}
```

### Updating Memory

```csharp
public class MyService
{
    private readonly ILongTermMemoryService _memoryService;
    private readonly IAppModeService _appModeService;

    public async Task<bool> UpdateUserPreferencesAsync(string preferences)
    {
        // Prepare memory content
        var memoryContent = $"# User Preferences\n\n{preferences}";

        // Update memory
        var result = await _memoryService.UpdateMemoryAsync(
            _appModeService.CurrentMode,
            memoryContent);

        if (!result.Success)
        {
            _logger.LogError("Failed to update memory: {Error}", result.Error);
            return false;
        }

        _logger.LogInformation(
            "Memory updated successfully ({CharCount} chars)",
            result.CharacterCount);
        return true;
    }
}
```

### Checking Memory Existence

```csharp
public class MyService
{
    private readonly ILongTermMemoryService _memoryService;

    public async Task<bool> ShouldPromptForIntroductionAsync()
    {
        // Check if we have memory (user has introduced themselves before)
        var hasMemory = await _memoryService.HasMemoryAsync(AppMode.Normal);

        // Prompt for introduction only if no memory exists
        return !hasMemory;
    }
}
```

### Getting Last Update Time

```csharp
public class MyService
{
    private readonly ILongTermMemoryService _memoryService;

    public async Task<string> GetMemoryAgeAsync()
    {
        var lastUpdate = await _memoryService.GetLastUpdateTimeAsync(AppMode.Normal);

        if (!lastUpdate.HasValue)
        {
            return "No memory exists";
        }

        var age = DateTime.UtcNow - lastUpdate.Value;
        return $"Memory last updated {age.Days} days ago";
    }
}
```

## Security Considerations

### Directory Traversal Protection

The service validates that the resolved storage directory is within the application base directory:

```csharp
// ❌ Attack attempt
config.StorageDirectory = "../../etc/passwd";
var service = new LongTermMemoryService(config, logger);
// Throws: InvalidOperationException

// ✅ Valid configuration
config.StorageDirectory = "data/memory";
var service = new LongTermMemoryService(config, logger);
// Works correctly
```

### File Path Hardening

- Mode names are mapped to hardcoded filenames (no user input in paths)
- No dynamic path construction based on user input
- All paths are normalized using `Path.GetFullPath()`

### Size Limits

The 10,000 character limit prevents:
- Memory files from growing unbounded
- Excessive disk usage
- Performance degradation from large file reads

## Performance Characteristics

### File I/O Performance

- **Read**: ~1-5ms for typical memory files (1-5KB)
- **Write**: ~5-10ms with directory creation
- **Check Existence**: <1ms (OS file stat)

### Memory Usage

- Minimal memory footprint - files loaded on demand
- UTF-8 encoding ensures efficient character storage
- No caching (current implementation) - files read fresh each time

### Scalability

Current implementation is suitable for:
- ✅ Single-user applications
- ✅ Small to medium memory files (up to 10KB)
- ✅ Infrequent updates (few times per conversation)

Not optimized for:
- ❌ High-frequency updates (100+ per second)
- ❌ Very large memory files (>100KB)
- ❌ Concurrent multi-user access

## Known Limitations

1. **No Hot Reload**: Manual edits to memory files require app restart
2. **No File Locking**: Concurrent writes from multiple processes not prevented
3. **No Versioning**: No automatic history or rollback capability
4. **No Compression**: Large markdown files stored as-is
5. **No Caching**: Files re-read on every access

## Future Enhancements

### Potential Improvements

1. **Caching Layer**: In-memory cache with change detection
2. **File Locking**: Prevent concurrent modification
3. **Version History**: Track changes over time with git-like versioning
4. **Compression**: Compress large memory files
5. **Hot Reload**: FileSystemWatcher to detect external changes
6. **Encryption**: Optional encryption at rest for sensitive deployments

## Related Documentation

- **Concept**: [Long-Term Memory](../../03-concepts/long-term-memory.md) - High-level overview
- **Tools**: [Long-Term Memory Tools](../tools/builtin/long-term-memory-tools.md) - Built-in tools
- **Guide**: [Using Long-Term Memory](../../05-guides/features/using-long-term-memory.md) - Usage guide
- **Configuration**: [Configuration Guide](../../05-guides/installation/llm-provider-selector.md#long-term-memory) - Setup details
- **Design**: `LONG_TERM_MEMORY_DESIGN.md` (project root) - Design decisions
- **Tests**: `TransparentAiAgentCore_Tests/Infrastructure/Memory/` - Test suite

---

**See Also**: [Infrastructure Components](../infrastructure/README.md)
