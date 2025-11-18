# Data Storage Guide

**Last Updated**: 2025-11-18
**Status**: ✅ Implemented

## 📋 Scope

### ✅ What's in this document
- User data storage locations (AppData)
- Data directory structure
- User settings management
- Finding and accessing user data
- Migrating data between installations

### ❌ What's NOT in this document
- Application configuration (appsettings.json) → See [LLM Provider Selector Guide](llm-provider-selector.md)
- Long-term memory usage → See [Using Long-Term Memory](../features/using-long-term-memory.md)
- Conversation history usage → See Component docs

---

## Overview

TransparentAiAgent stores user-specific data in the operating system's standard application data directory (AppData on Windows). This ensures:

- ✅ **Proper separation** between user data and application binaries
- ✅ **Persistence** across application updates
- ✅ **User-specific storage** in multi-user environments
- ✅ **OS-appropriate locations** (Windows, macOS, Linux)
- ✅ **Easy backup** of user data

---

## Data Storage Locations

### Root Data Directory

All user data is stored under the TransparentAiAgent folder in the OS-specific application data directory:

| Operating System | Location |
|-----------------|----------|
| **Windows** | `%AppData%\TransparentAiAgent` <br> Example: `C:\Users\YourName\AppData\Roaming\TransparentAiAgent` |
| **macOS** | `~/.config/TransparentAiAgent` <br> Example: `/Users/YourName/.config/TransparentAiAgent` |
| **Linux** | `~/.config/TransparentAiAgent` <br> Example: `/home/yourname/.config/TransparentAiAgent` |

### Directory Structure

```
%AppData%\TransparentAiAgent\
├── user-settings.json          # User preferences (context size, memory settings)
├── conversations/              # Conversation history files
│   ├── conversation-1.json
│   ├── conversation-2.json
│   └── ...
├── memory/                     # Long-term memory files
│   └── long-term-memory.json
└── logs/                       # Application logs (future)
```

---

## Data Types

### 1. User Settings (user-settings.json)

**Location**: `%AppData%\TransparentAiAgent\user-settings.json`

**Purpose**: Stores user-specific preferences that can be modified at runtime.

**Contents**:
```json
{
  "ContextWindowSize": 200,
  "EnableMemory": false
}
```

**Fields**:
- `ContextWindowSize` (int): Number of messages to include in LLM context window (default: 200)
- `EnableMemory` (bool): Whether long-term memory feature is enabled (default: false)

**Management**:
- Automatically created on first run with default values
- Modified via UI settings (future feature)
- Can be manually edited (application restart required)

---

### 2. Conversation History

**Location**: `%AppData%\TransparentAiAgent\conversations\`

**Purpose**: Stores all conversation messages across sessions.

**File Format**: JSON files named `conversation-{id}.json`

**Contents**:
- All messages (user and assistant)
- Timestamps
- Metadata

**Management**:
- Automatically created when starting a new conversation
- Automatically saved after each message
- Can be manually deleted to remove conversation history

---

### 3. Long-Term Memory

**Location**: `%AppData%\TransparentAiAgent\memory\long-term-memory.json`

**Purpose**: Stores persistent agent memory across sessions.

**Contents**:
- Facts learned about the user
- Preferences
- Previous interactions
- Contextual information

**Management**:
- Automatically created when memory feature is enabled
- Updated based on agent learning
- Can be manually deleted to reset memory

**Note**: ⚠️ Known issue - Long-term memory may not be available after recent configuration refactor. See [Known Issues](../../KnownIssues.md).

---

### 4. Logs (Future)

**Location**: `%AppData%\TransparentAiAgent\logs\`

**Purpose**: Application logs and diagnostic information.

**Status**: Directory created but logging not yet implemented.

---

## Finding Your Data

### Windows

**Option 1: Using Windows Explorer**
1. Press `Win + R` to open Run dialog
2. Type `%AppData%\TransparentAiAgent` and press Enter
3. Your data folder will open

**Option 2: Direct Path**
1. Open File Explorer
2. Navigate to: `C:\Users\YourUsername\AppData\Roaming\TransparentAiAgent`
3. Replace `YourUsername` with your Windows username

**Option 3: Copy Path to Clipboard**
- Run this in PowerShell:
  ```powershell
  echo $env:APPDATA\TransparentAiAgent | clip
  ```

### macOS

**Option 1: Using Terminal**
```bash
open ~/.config/TransparentAiAgent
```

**Option 2: Using Finder**
1. Open Finder
2. Press `Cmd + Shift + G` (Go to Folder)
3. Type `~/.config/TransparentAiAgent` and press Enter

### Linux

**Using Terminal**
```bash
cd ~/.config/TransparentAiAgent
ls -la
```

---

## Configuration vs User Data

TransparentAiAgent uses two separate configuration systems:

### Application Configuration (appsettings.json)

**Location**: Application installation directory (`TransparentAiAgentGui/appsettings.json`)

**Purpose**: Deployment-specific settings, LLM provider configuration

**Contents**:
- LLM provider settings (API keys, endpoints)
- Agent configuration (system prompt)
- MCP server settings
- Tool configuration

**Managed by**: Developers, administrators, deployment scripts

**See**: [LLM Provider Selector Guide](llm-provider-selector.md)

### User Settings (user-settings.json)

**Location**: User data directory (`%AppData%\TransparentAiAgent\user-settings.json`)

**Purpose**: User-specific runtime preferences

**Contents**:
- Context window size
- Memory enable/disable
- Future: UI preferences, language settings

**Managed by**: End users via UI (future) or manual editing

---

## Backing Up Your Data

### What to Back Up

To preserve your TransparentAiAgent data:

1. **User Settings**: `user-settings.json`
2. **Conversation History**: `conversations/` folder
3. **Long-Term Memory**: `memory/` folder

### Backup Steps (Windows)

```powershell
# Create backup folder
mkdir C:\Backups\TransparentAiAgent

# Copy user data
xcopy "%AppData%\TransparentAiAgent" "C:\Backups\TransparentAiAgent" /E /I /Y
```

### Backup Steps (macOS/Linux)

```bash
# Create backup folder
mkdir -p ~/Backups/TransparentAiAgent

# Copy user data
cp -r ~/.config/TransparentAiAgent ~/Backups/TransparentAiAgent
```

---

## Restoring Data

### Restore from Backup (Windows)

```powershell
# Close TransparentAiAgent if running

# Restore data
xcopy "C:\Backups\TransparentAiAgent" "%AppData%\TransparentAiAgent" /E /I /Y
```

### Restore from Backup (macOS/Linux)

```bash
# Close TransparentAiAgent if running

# Restore data
cp -r ~/Backups/TransparentAiAgent ~/.config/TransparentAiAgent
```

---

## Migrating Between Installations

### Export Data from Old Installation

1. **Locate data folder** (see "Finding Your Data" above)
2. **Copy entire folder** to external drive or cloud storage
3. **Verify backup** by checking copied files

### Import Data to New Installation

1. **Install TransparentAiAgent** on new machine
2. **Run application once** to create data directories
3. **Close application**
4. **Copy backed up files** to new data directory
5. **Restart application**

Your conversations, settings, and memory will be available on the new installation.

---

## Resetting User Data

### Reset Everything

**Warning**: This deletes all conversations, memory, and settings!

**Windows**:
```powershell
# Close TransparentAiAgent first!
Remove-Item -Recurse -Force "$env:APPDATA\TransparentAiAgent"
```

**macOS/Linux**:
```bash
# Close TransparentAiAgent first!
rm -rf ~/.config/TransparentAiAgent
```

On next launch, application will recreate default directories and settings.

### Reset Specific Data

**Reset User Settings Only**:
```powershell
# Windows
Remove-Item "$env:APPDATA\TransparentAiAgent\user-settings.json"
```

**Reset Conversations Only**:
```powershell
# Windows
Remove-Item -Recurse "$env:APPDATA\TransparentAiAgent\conversations"
```

**Reset Long-Term Memory Only**:
```powershell
# Windows
Remove-Item -Recurse "$env:APPDATA\TransparentAiAgent\memory"
```

---

## Data Privacy & Security

### What Data is Stored

TransparentAiAgent stores:
- ✅ **Conversation messages** (your questions and AI responses)
- ✅ **User preferences** (settings, memory toggle)
- ✅ **Long-term memory** (facts learned about you)

TransparentAiAgent does **NOT** store:
- ❌ API keys (stored in appsettings.json in app directory)
- ❌ LLM provider credentials
- ❌ Authentication tokens

### Security Considerations

1. **Conversation data contains your messages** - Be mindful when sharing backups
2. **Data is stored unencrypted** - Use OS-level encryption (BitLocker, FileVault) if needed
3. **API keys are in appsettings.json** - Not in user data directory
4. **Multi-user systems** - Each user has separate data directory

### Exporting Logs (Future Feature)

**Note**: Export functionality is planned but not yet implemented. See [Known Issues](../../KnownIssues.md).

When implemented, exported logs will include:
- Transparency events
- Message content
- Tool usage

**Warning**: Exported logs will contain your conversation content. Handle with care regarding privacy/security.

---

## Troubleshooting

### Data Directory Not Created

**Symptom**: `%AppData%\TransparentAiAgent` doesn't exist

**Solution**:
1. Run TransparentAiAgent at least once
2. Send at least one message to trigger data initialization
3. Check permissions on AppData folder

### Can't Find AppData Folder (Windows)

**Symptom**: Can't navigate to AppData

**Solution**:
1. AppData is a hidden folder by default
2. In File Explorer: View → Show → Hidden items (checkbox)
3. Or use Run dialog: `Win + R`, type `%AppData%`

### Permission Denied Errors

**Symptom**: Application can't write to data directory

**Solution**:
1. Check folder permissions
2. Run application with appropriate user permissions
3. Check antivirus isn't blocking file access

### Data Not Persisting

**Symptom**: Settings or conversations lost after restart

**Possible Causes**:
1. Application can't write to AppData (check permissions)
2. Data directory was deleted
3. Running application from different user account

**Solution**:
1. Check application logs for write errors
2. Verify data directory exists
3. Ensure running as same user

---

## Implementation Details

For developers interested in how data storage works:

### DataPathService

**Interface**: `IDataPathService`
**Implementation**: `DataPathService`
**Location**: `TransparentAiAgentCore/Infrastructure/DataPath/`

**Responsibilities**:
- Resolves OS-appropriate data paths
- Creates directory structure
- Provides path APIs to other services

**Methods**:
```csharp
string GetUserDataRoot();           // %AppData%\TransparentAiAgent
string GetMemoryDirectory();        // %AppData%\TransparentAiAgent\memory
string GetConversationsDirectory(); // %AppData%\TransparentAiAgent\conversations
string GetLogsDirectory();          // %AppData%\TransparentAiAgent\logs
void EnsureDirectoriesExist();      // Creates all directories
```

### UserSettingsService

**Interface**: `IUserSettingsService`
**Implementation**: `UserSettingsService`
**Location**: `TransparentAiAgentCore/Infrastructure/Configuration/`

**Responsibilities**:
- Loads and saves user settings
- Manages in-memory settings cache
- Provides settings update APIs

**Methods**:
```csharp
UserSettings LoadSettings();
void SaveSettings(UserSettings settings);
void UpdateContextWindowSize(int contextWindowSize);
void UpdateEnableMemory(bool enableMemory);
UserSettings GetCurrentSettings();
```

---

## Related Documentation

- **Configuration Guide**: [LLM Provider Selector Guide](llm-provider-selector.md)
- **Long-Term Memory**: [Using Long-Term Memory](../features/using-long-term-memory.md)
- **Troubleshooting**: [Troubleshooting Guide](troubleshooting.md)
- **Known Issues**: [Known Issues](../../KnownIssues.md)
- **Component Docs**: [DataPath Service](../../04-components/infrastructure/data-path-service.md) (if exists)

---

## FAQ

### Where is my conversation data stored?

On Windows: `C:\Users\YourName\AppData\Roaming\TransparentAiAgent\conversations\`

### Can I move the data directory?

Not currently supported. Data directory location is determined by the OS.

### How much disk space does TransparentAiAgent use?

Depends on usage:
- User settings: < 1 KB
- Each conversation: 1-10 MB (depending on length)
- Long-term memory: 1-5 MB
- Typical usage: 10-100 MB total

### Can I delete old conversations?

Yes! Simply delete conversation files from the `conversations/` directory. The application will not be affected.

### Will updates delete my data?

No! User data is stored separately from application binaries. Application updates won't affect your data.

### Can I sync data across devices?

Not natively supported, but you can:
1. Back up data directory
2. Restore to another device
3. Or use cloud storage folder (at your own risk - manual sync required)

---

**Last Updated**: 2025-11-18
**Current Phase**: Configuration Refactor (AppData Migration Complete)
**Status**: ✅ Implemented

---
