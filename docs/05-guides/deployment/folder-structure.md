# Deployment Folder Structure

This guide explains the folder structure of the deployed TransparentAiAgent application and the purpose of each directory and file.

## Overview

The TransparentAiAgent deployment follows a clean, organized structure with all user-facing content consolidated into a `data/` folder for easy management and backup.

## Complete Folder Structure

```
TransparentAiAgent/
│
├── TransparentAiAgentGui.exe           # Main executable (Blazor Server app)
│
├── appsettings.json                     # Active configuration file
├── appsettings.Production.json          # Production config template
├── appsettings.Development.json         # (Optional) Development settings
│
├── wwwroot/                             # Web assets served by Blazor
│   ├── app.css                          # Application styles
│   ├── favicon.png                      # Browser icon
│   ├── bootstrap/                       # Bootstrap CSS framework
│   └── js/                              # JavaScript files
│
├── data/                                # All application data (user-modifiable)
│   ├── conversations/                   # Conversation history (runtime)
│   ├── memory/                          # Long-term memory (runtime)
│   ├── knowledge/                       # Knowledge library entries
│   └── scenarios/                       # Teaching mode scenarios
│
├── Anthropic.Client.dll                 # LLM provider dependencies
├── Azure.AI.OpenAI.dll
├── OpenAI.dll
├── Markdig.dll
├── ModelContextProtocol.dll
├── [Other .NET library DLLs]
│
└── TransparentAiAgentCore.dll           # Application core library
```

## Directory Descriptions

### Root Directory

| File/Folder | Type | Purpose | User-Editable |
|-------------|------|---------|---------------|
| `TransparentAiAgentGui.exe` | Executable | Main application entry point | No |
| `appsettings.json` | Config | Active configuration file | **Yes** |
| `appsettings.Production.json` | Template | Clean config template for reference | No |
| `*.dll` | Libraries | Application and dependency libraries | No |

### `wwwroot/` - Web Assets

Contains static files served by the Blazor web interface:

| Folder/File | Purpose | User-Editable |
|-------------|---------|---------------|
| `app.css` | Application styling | Yes (advanced) |
| `favicon.png` | Browser tab icon | Yes |
| `bootstrap/` | Bootstrap CSS framework files | No |
| `js/` | JavaScript for UI interactivity | No |

**Note:** Most users won't need to modify anything in `wwwroot/`. It contains the web UI assets.

### `data/` - Application Data (User Content)

This folder contains all user-modifiable and runtime-generated content. **This is the most important folder to backup.**

#### `data/conversations/` (Runtime - Created Automatically)

Stores conversation history as JSON files.

**Structure:**
```
data/conversations/
├── abc123_My_First_Chat.json
├── def456_Project_Discussion.json
└── ghi789_Code_Review.json
```

**File Format:**
- Naming: `{ConversationId}_{SanitizedName}.json`
- Content: Complete conversation history including all messages, tool calls, and metadata
- Created: When you start a new conversation
- Updated: After each message exchange

**Backup:** Important - contains your entire conversation history.

**Size:** Grows over time. Each conversation is typically 10-500 KB depending on length.

#### `data/memory/` (Runtime - Created on First Use)

Stores the agent's long-term memory.

**Structure:**
```
data/memory/
├── memory-normal.md        # Memory for normal chat mode
└── memory-teaching.md      # Memory for teaching mode
```

**File Format:**
- Markdown files containing structured memory
- Updated automatically at conversation end (if enabled)
- Max size controlled by `LongTermMemory.MaxCharacters` setting

**Backup:** Important if you use long-term memory features.

**Size:** Limited by configuration (default: 10,000 characters = ~10 KB).

#### `data/knowledge/` (Bundled - User Extensible)

Contains knowledge library entries that the agent can reference during conversations.

**Structure:**
```
data/knowledge/
├── index.json                          # Index of all knowledge entries
├── programming/
│   ├── csharp-best-practices.md
│   └── design-patterns.md
├── ai/
│   ├── llm-basics.md
│   └── prompting-techniques.md
└── custom/                             # Add your own here
    └── my-custom-knowledge.md
```

**File Format:**
- Markdown (.md) files with metadata header
- Organized by category (folder structure)
- Indexed in `index.json` for quick lookup

**User-Editable:** **Yes** - You can add, edit, or remove knowledge entries.

**Example Entry:**
```markdown
---
title: "C# Best Practices"
category: "programming"
tags: ["csharp", "coding-standards"]
---

# C# Best Practices

## Naming Conventions
- Use PascalCase for class names
- Use camelCase for local variables
...
```

**Backup:** Optional - bundled content can be restored, but custom additions should be backed up.

#### `data/scenarios/` (Bundled - User Extensible)

Contains teaching mode scenarios that guide users through educational exercises.

**Structure:**
```
data/scenarios/
├── scenario-index.json
├── 01-basic-programming/
│   ├── hello-world.json
│   └── variables-types.json
├── 02-algorithms/
│   ├── sorting.json
│   └── searching.json
└── custom/                             # Add custom scenarios here
    └── my-lesson.json
```

**File Format:**
- JSON files defining interactive teaching scenarios
- Each scenario has steps, hints, and validation criteria

**User-Editable:** **Yes** - You can create custom teaching scenarios.

**Example Scenario Structure:**
```json
{
  "id": "hello-world",
  "title": "Your First C# Program",
  "description": "Learn to write a Hello World program in C#",
  "difficulty": "beginner",
  "steps": [
    {
      "title": "Create Main Method",
      "hint": "All C# programs start with a Main method...",
      "validation": "..."
    }
  ]
}
```

**Backup:** Optional - bundled content can be restored, but custom scenarios should be backed up.

## File Sizes (Typical)

| Component | Typical Size |
|-----------|--------------|
| Executable + DLLs | 80-100 MB (self-contained with .NET runtime) |
| `wwwroot/` | 2-5 MB |
| `data/knowledge/` | 1-5 MB (bundled content) |
| `data/scenarios/` | 0.5-2 MB (bundled content) |
| `data/conversations/` | Grows with use (10-500 KB per conversation) |
| `data/memory/` | < 100 KB |
| **Total Initial** | ~90-110 MB |

## What to Backup

### Critical (Must Backup)
- `data/conversations/` - Your entire conversation history
- `data/memory/` - Agent's long-term memory
- `appsettings.json` - Your configuration and API keys

### Optional (Can Be Restored)
- `data/knowledge/custom/` - Only your custom knowledge entries
- `data/scenarios/custom/` - Only your custom scenarios

### Not Needed (Bundled with Application)
- `wwwroot/`
- `data/knowledge/` (except custom additions)
- `data/scenarios/` (except custom additions)
- DLL files and executable

## Folder Permissions

### Required Permissions

The application needs **Read/Write** access to:
- `data/` and all subdirectories
- `appsettings.json` (Read-only is acceptable if you don't modify it at runtime)

### Recommended Installation Locations

**Good (User has full control):**
- `C:\Users\{YourName}\Apps\TransparentAiAgent\`
- `D:\Applications\TransparentAiAgent\`
- `C:\MyApps\TransparentAiAgent\`

**Avoid (May require admin rights):**
- `C:\Program Files\TransparentAiAgent\` - Requires admin for writes
- `C:\Windows\` - Not appropriate
- Network drives - May have permission issues

## Portable Installation

The application is **fully portable**:
- All data is stored relative to the executable location
- No registry entries are created
- No system-wide installation required
- You can copy the entire folder to another machine or USB drive

**To make portable:**
1. Copy the entire installation folder to a USB drive
2. Run from the USB drive - all data stays on the drive
3. Conversations and memory move with you

**Note:** API keys in `appsettings.json` will also move with the portable installation.

## Disk Space Management

### Monitoring Size

Check the size of your data folder periodically:

```powershell
# PowerShell: Get data folder size
Get-ChildItem -Path "data" -Recurse | Measure-Object -Property Length -Sum
```

### Cleaning Up

**Conversations:**
- Old conversations can be deleted from `data/conversations/`
- The application will continue to work normally
- Consider archiving old conversations instead of deleting

**Memory:**
- Memory files are limited by configuration (default: 10,000 characters)
- Manually edit `data/memory/*.md` if needed (be careful with format)
- Or delete to reset (will be recreated on next use)

## Advanced: Customizing Folder Locations

To change where `data/` folders are created, edit `appsettings.json`:

```json
{
  "LongTermMemory": {
    "StorageDirectory": "data/memory"      ← Change path here
  }
}
```

**Note:** Currently, conversations path is hardcoded in the application but could be made configurable in a future update.

## Folder Structure Changes from wwwroot

In previous versions, `knowledge/` and `scenarios/` were in `wwwroot/`. They have been moved to `data/` for clarity:

**Old Structure:**
```
wwwroot/
├── knowledge/     ← Was here
└── scenarios/     ← Was here
```

**New Structure (Current):**
```
data/
├── knowledge/     ← Now here (more intuitive)
├── scenarios/     ← Now here
├── conversations/
└── memory/
```

This change makes it clearer that these are content folders, not web assets.

## Troubleshooting Folder Issues

### "Access Denied" when saving conversations

**Cause:** Application doesn't have write permissions to `data/` folder.

**Solution:**
1. Check folder permissions (right-click → Properties → Security)
2. Ensure your user account has "Modify" permission
3. Move installation to a user-owned folder

### Folders not being created

**Cause:** Write permissions missing or path configuration wrong.

**Solution:**
1. Run application as Administrator once to create folders
2. Or manually create the folder structure and set permissions

### Conversations not appearing

**Cause:** Looking in wrong location or file format corrupted.

**Solution:**
1. Check `data/conversations/` exists
2. Verify JSON files are valid (use JSON validator)
3. Check console for error messages about file I/O

## Summary

The deployment folder structure is designed to be:
- **Clear:** Obvious purpose for each folder
- **Organized:** User content in `data/`, web assets in `wwwroot/`
- **Portable:** Everything relative to executable location
- **Backup-friendly:** Important data clearly separated in `data/`

For daily use, you only need to:
1. Edit `appsettings.json` to configure API keys
2. Backup `data/` folder periodically
3. Run `TransparentAiAgentGui.exe`

Everything else is managed automatically.
