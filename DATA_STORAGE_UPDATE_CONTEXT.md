# Data Storage Update Context

**Issue**: System I/O errors on Windows when writing long-term memory and conversation history files
**Date Started**: 2025-11-18
**Branch**: `claude/update-data-storage-013YqRoBWv8AEH2s21Bosdpt`

## Problem Analysis

### Current Situation
The application currently writes data to subdirectories within the application directory:
- `data/memory/` - Long-term memory files
- `data/conversations/` - Conversation history JSON files
- `logs/` - Crash logs
- `appsettings.json` - Configuration file

### Why This Causes Issues on Windows

1. **Permission Problems**: If the app is installed in `Program Files` or similar protected locations, write operations may fail due to UAC (User Account Control)
2. **Security Restrictions**: Windows Defender and antivirus software may flag or block file writes in executable directories
3. **Multi-User Environment**: Writing to app directory doesn't respect user separation - all users would share the same data
4. **Updates/Reinstalls**: Data could be lost when the application is updated or reinstalled

### Root Cause
The I/O errors started after implementing:
- Long-term memory (writes `memory-normal.md`, `memory-teaching.md`)
- Conversation history (writes JSON files per conversation)

These features write frequently to disk, triggering permission/security issues.

## Solution: Use OS-Appropriate User Data Directories

### Correct Approach
Write user data to system-appropriate locations:

**Windows**: `%APPDATA%\TransparentAiAgent\`
- Roaming: `C:\Users\{username}\AppData\Roaming\TransparentAiAgent\`
- Local: `C:\Users\{username}\AppData\Local\TransparentAiAgent\`

**Linux**: `~/.local/share/TransparentAiAgent/` or `~/.config/TransparentAiAgent/`

**macOS**: `~/Library/Application Support/TransparentAiAgent/`

### .NET Implementation
Use: `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)`
- Returns `%APPDATA%` on Windows
- Returns `~/.config/` on Linux
- Returns `~/Library/Application Support/` on macOS

For local (non-roaming) data:
Use: `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`

## Files Requiring Updates

### 1. Long-Term Memory (HIGH PRIORITY)
**File**: `TransparentAiAgentCore/Infrastructure/Memory/LongTermMemoryService.cs`
- **Line 24-42**: Path resolution logic
- **Current**: Uses `AppDomain.CurrentDomain.BaseDirectory + "data/memory"`
- **New**: Use `{ApplicationData}/TransparentAiAgent/memory/`

**File**: `TransparentAiAgentCore/Domain/Memory/LongTermMemoryConfiguration.cs`
- **Line 17**: Default path `"data/memory"`
- **New**: Should default to null and be computed at runtime

### 2. Conversation History (HIGH PRIORITY)
**File**: `TransparentAiAgentGui/Program.cs`
- **Line 85**: `Path.Combine(builder.Environment.ContentRootPath, "data", "conversations")`
- **New**: Use `{ApplicationData}/TransparentAiAgent/conversations/`

### 3. Configuration Files (MEDIUM PRIORITY)
**File**: `TransparentAiAgentCore/Infrastructure/Configuration/ConfigurationService.cs`
- **Line 11**: Default path `"appsettings.json"`
- **Current**: Writes to current directory
- **Consideration**: appsettings.json is typically read-only after deployment. User settings should go to AppData, but base config may stay in app directory.
- **Decision**: TBD - may need to split into base config (read-only in app dir) and user settings (writable in AppData)

### 4. Crash Logs (LOW PRIORITY)
**File**: `TransparentAiAgentGui/Program.cs`
- **Line 671-672**: `Path.Combine(AppContext.BaseDirectory, "logs")`
- **Current**: Writes to `{AppDir}/logs/`
- **New**: Could use `{LocalApplicationData}/TransparentAiAgent/logs/` for better permissions
- **Note**: Less critical since crashes may occur before we can reliably write elsewhere

### 5. Read-Only Data (NO CHANGE NEEDED)
These directories are read-only and can stay in app directory:
- `data/knowledge/entries/` - Knowledge library JSON files
- `data/scenarios/` - Scenario definition files
- `data/translations/` - Translation files

## Implementation Plan

### Phase 1: Core Changes (HIGH PRIORITY)
1. Create helper service for path resolution (`IDataPathService`)
   - Encapsulates logic for getting OS-appropriate paths
   - Centralizes path configuration
   - Makes testing easier

2. Update LongTermMemoryService
   - Use ApplicationData path
   - Maintain backward compatibility (migrate existing files if found)

3. Update ConversationHistoryRepository
   - Use ApplicationData path
   - Migrate existing conversations if found

### Phase 2: Configuration Strategy (MEDIUM PRIORITY)
4. Decide on configuration file strategy:
   - **Option A**: Keep appsettings.json in app dir (read-only), create user-settings.json in AppData (writable)
   - **Option B**: Move entire config to AppData
   - **Recommendation**: Option A - follows standard practices

### Phase 3: Polish (LOW PRIORITY)
5. Update crash logs location
6. Add migration tool/logic for existing installations
7. Document new storage locations in user guide

## Implementation Details

### Proposed DataPathService Interface
```csharp
public interface IDataPathService
{
    /// <summary>
    /// Gets the root directory for user data (uses ApplicationData)
    /// </summary>
    string GetUserDataRoot();

    /// <summary>
    /// Gets the directory for long-term memory files
    /// </summary>
    string GetMemoryDirectory();

    /// <summary>
    /// Gets the directory for conversation history
    /// </summary>
    string GetConversationsDirectory();

    /// <summary>
    /// Gets the directory for logs
    /// </summary>
    string GetLogsDirectory();

    /// <summary>
    /// Ensures all necessary directories exist
    /// </summary>
    void EnsureDirectoriesExist();
}
```

### Implementation
```csharp
public class DataPathService : IDataPathService
{
    private readonly string _userDataRoot;

    public DataPathService()
    {
        // Use ApplicationData for roaming user data
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _userDataRoot = Path.Combine(appData, "TransparentAiAgent");
    }

    public string GetUserDataRoot() => _userDataRoot;

    public string GetMemoryDirectory() =>
        Path.Combine(_userDataRoot, "memory");

    public string GetConversationsDirectory() =>
        Path.Combine(_userDataRoot, "conversations");

    public string GetLogsDirectory() =>
        Path.Combine(_userDataRoot, "logs");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(GetMemoryDirectory());
        Directory.CreateDirectory(GetConversationsDirectory());
        Directory.CreateDirectory(GetLogsDirectory());
    }
}
```

## Migration Strategy

For existing installations, we should:
1. Check for old data in `{AppDir}/data/*`
2. If found, copy to new AppData location
3. Log the migration
4. Optionally delete old files (or leave them as backup)

## Testing Considerations

1. Test on Windows without admin privileges
2. Test in Program Files installation scenario
3. Test with Windows Defender enabled
4. Test multi-user scenario (different Windows users)
5. Verify paths on Linux/macOS as well

## Questions for User

1. **Configuration Strategy**: Should we split config into read-only (app dir) and user-writable (AppData) parts, or move everything to AppData?

2. **Migration**: Should we automatically migrate existing data, or require manual migration?

3. **Backward Compatibility**: Do we need to maintain support for old paths, or can we do a clean break?

## Decisions Made

*(To be filled as we progress)*

## Progress Log

- **2025-11-18 Initial**: Created context document, analyzed codebase
- *(More entries to be added)*

## References

- [.NET Environment.SpecialFolder Enum](https://docs.microsoft.com/en-us/dotnet/api/system.environment.specialfolder)
- [Windows Application Data Guidelines](https://docs.microsoft.com/en-us/windows/apps/design/app-settings/store-and-retrieve-app-data)
- [XDG Base Directory Specification (Linux)](https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html)
