# Installation Guide

This guide explains how to install and configure the TransparentAiAgent application on Windows systems.

## System Requirements

### Minimum Requirements
- **Operating System:** Windows 10 (64-bit) or later
- **RAM:** 4 GB minimum, 8 GB recommended
- **Disk Space:** 500 MB for application + space for conversations and memory
- **Network:** Internet connection required for LLM API calls

### No .NET Installation Required
This is a **self-contained** application - the .NET 8 runtime is included. You do NOT need to install .NET separately.

## Installation Steps

### 1. Download and Extract

1. Download the `TransparentAiAgent-win-x64.zip` file
2. Right-click the ZIP file and select **Extract All...**
3. Choose a destination folder (e.g., `C:\Program Files\TransparentAiAgent` or `C:\Users\YourName\Apps\TransparentAiAgent`)
4. Click **Extract**

After extraction, your folder should look like this:

```
TransparentAiAgent/
├── TransparentAiAgentGui.exe        # Main executable
├── appsettings.json                  # Configuration file (edit this!)
├── wwwroot/                          # Web assets
├── data/                             # Application data
└── [DLL files...]
```

### 2. Configure API Keys

Before running the application, you **must configure at least one LLM provider** with your API key.

#### Edit appsettings.json

1. Open `appsettings.json` in a text editor (Notepad, VS Code, or any text editor)
2. Find the LLM provider you want to use
3. Replace the placeholder API key with your actual key

**Example: Configuring Anthropic (Claude)**

```json
"claude-haiku": {
  "Type": "Anthropic",
  "DisplayName": "Claude Haiku",
  "Parameters": {
    "Model": "claude-haiku-4-5-20251001",
    "ApiKey": "sk-ant-api03-YOUR_ACTUAL_KEY_HERE",  ← Replace this
    "ExtendedThinking": {
      "Enabled": false
    }
  }
}
```

**Example: Configuring OpenAI**

```json
"openai-gpt5-mini": {
  "Type": "OpenAI",
  "DisplayName": "OpenAI GPT-5-mini",
  "Parameters": {
    "Model": "gpt-5-mini",
    "ApiKey": "sk-proj-YOUR_ACTUAL_KEY_HERE",  ← Replace this
    "IsReasoningModel": true
  }
}
```

#### Where to Get API Keys

- **Anthropic Claude:** https://console.anthropic.com/
  - Sign up for an account
  - Navigate to API Keys
  - Create a new key
  - Copy and paste into `appsettings.json`

- **OpenAI:** https://platform.openai.com/api-keys
  - Sign up for an account
  - Go to API Keys section
  - Create new secret key
  - Copy and paste into `appsettings.json`

#### Select Active Provider

Make sure `ActiveProvider` matches one of your configured providers:

```json
"LLM": {
  "ActiveProvider": "claude-haiku",  ← Must match a provider name below
  ...
}
```

### 3. First Run

1. Double-click `TransparentAiAgentGui.exe` to start the application
2. A command prompt window will open showing startup logs
3. Wait for the message showing which port the server is listening on (e.g., "Now listening on: http://localhost:54321")
4. Your default web browser will automatically open to the correct URL

**Note:** The application automatically selects an available port at startup. The port number will be shown in the console window and your browser will open to the correct URL automatically.

### 4. Verify Installation

Once the application loads in your browser:

1. You should see the TransparentAiAgent interface
2. The active LLM provider should be displayed in the header
3. Type a test message like "Hello, can you introduce yourself?"
4. If you get a response, the installation was successful

## Folder Structure

After first run, the `data/` folder will contain:

```
data/
├── knowledge/          # Knowledge library entries (bundled with app)
├── scenarios/          # Teaching mode scenarios (bundled with app)
├── conversations/      # Your conversation history (created at runtime)
└── memory/             # Long-term memory files (created when used)
```

### Important Folders

| Folder | Purpose | Created When |
|--------|---------|--------------|
| `conversations/` | Stores all your chat conversations as JSON files | First conversation started |
| `memory/` | Agent's long-term memory storage | Long-term memory feature first used |
| `knowledge/` | Built-in knowledge entries (can add your own) | Bundled with app |
| `scenarios/` | Teaching mode scenarios | Bundled with app |

**Backup Recommendation:** Periodically backup the `data/` folder to preserve your conversations and memory.

## Configuration Options

### Basic Settings

Edit `appsettings.json` to customize:

#### Agent Behavior

```json
"Agent": {
  "SystemPrompt": "You are a helpful AI assistant...",  ← Customize personality
  "ContextWindowSize": 20,                              ← Messages in context
  "EnableTools": true,                                  ← Enable tool use
  "ToolExecutionMode": "Sequential"                     ← How tools execute
}
```

#### LLM Parameters

```json
"DefaultParameters": {
  "Temperature": 0.7,      ← Creativity (0.0-2.0)
  "TopP": 1.0,             ← Nucleus sampling
  "MaxTokens": 4096        ← Max response length
}
```

#### Long-Term Memory

```json
"LongTermMemory": {
  "Enabled": true,                              ← Enable/disable memory
  "StorageDirectory": "data/memory",            ← Where to store
  "MaxCharacters": 10000,                       ← Max memory size
  "AutoLoadOnStart": true,                      ← Load on startup
  "PromptUpdateOnEnd": true                     ← Update when conversation ends
}
```

### Advanced: MCP Servers

To enable Model Context Protocol (MCP) servers for additional tools:

```json
"MCP": {
  "Servers": [
    {
      "Name": "todo-list",
      "Command": "npx",
      "Args": ["-y", "@anthropic/mcp-server-todo-list"],
      "Env": {}
    }
  ],
  "AutoDiscoverTools": true,
  "ToolExecutionTimeoutSeconds": 180
}
```

See [Configuration Guide](configuration-guide.md) for more details.

## Troubleshooting

### Application won't start

**Issue:** Double-clicking the .exe does nothing or shows an error.

**Solutions:**
1. Check Windows Event Viewer for error details
2. Ensure the folder has write permissions (for data/ folder creation)
3. Try running as Administrator (right-click → Run as administrator)
4. Check antivirus software isn't blocking the executable

### "Connection refused" or "Cannot connect"

**Issue:** Browser shows connection error

**Solutions:**
1. Check the console window - look for error messages and note the actual port being used
2. Verify the application started successfully (look for "Now listening on..." message)
3. Manually navigate to the URL shown in the console window
4. Check Windows Firewall settings

**Note:** The application automatically selects an available port, so port conflicts should be rare. If the browser doesn't open automatically, check the console for the actual URL.

### "API key invalid" or LLM errors

**Issue:** App starts but fails when sending messages.

**Solutions:**
1. Verify your API key is correctly copied in `appsettings.json`
2. Ensure there are no extra spaces or quotes around the key
3. Check your LLM provider account has available credits
4. Verify the model name matches what's available in your account

### Configuration file errors

**Issue:** "Failed to load configuration" or JSON parse errors.

**Solutions:**
1. Validate your `appsettings.json` using a JSON validator (https://jsonlint.com)
2. Common mistakes:
   - Missing commas between entries
   - Extra commas at the end of lists
   - Mismatched quotes or braces
3. If corrupted, restore from a backup or re-download the application

### Data folder permissions

**Issue:** "Access denied" when saving conversations or memory.

**Solutions:**
1. Ensure the application has write permissions to the `data/` folder
2. Don't install in protected folders like `C:\Program Files\` unless running as admin
3. Recommended: Install in your user folder (`C:\Users\YourName\Apps\`)

## Uninstallation

To remove TransparentAiAgent:

1. **Backup data** (if you want to keep conversations):
   - Copy the entire `data/` folder to a safe location

2. **Close the application**:
   - Close the browser tab
   - Close the console window (or Ctrl+C to terminate)

3. **Delete the installation folder**:
   - Simply delete the folder where you extracted the application

4. **Optional: Clean up browser data**:
   - Clear browser cache/cookies for `localhost` (if desired)

No registry entries or system files are created outside the application folder.

## Upgrading

To upgrade to a new version:

1. **Backup your data folder**:
   ```
   Copy data/ → data_backup/
   ```

2. **Download new version** and extract to a temporary location

3. **Copy your data folder** from old installation:
   ```
   Copy old_installation/data/ → new_installation/data/
   ```

4. **Copy your configuration**:
   ```
   Copy old_installation/appsettings.json → new_installation/appsettings.json
   ```
   (Or reconfigure API keys in the new appsettings.json)

5. **Test the new version**

6. **Delete old installation** once confirmed working

## Security Best Practices

1. **Protect your API keys**:
   - Never share your `appsettings.json` file
   - Don't commit it to version control if you're developing

2. **Network security**:
   - The application runs on `localhost` only (not accessible from network)
   - All LLM API calls use HTTPS

3. **Data privacy**:
   - Conversations are stored locally in `data/conversations/`
   - No data is sent anywhere except to your configured LLM provider
   - Your data never leaves your machine except via LLM API calls

## Getting Help

If you encounter issues:

1. Check the console window for error messages
2. Review the [Troubleshooting Guide](troubleshooting.md)
3. Consult the [Configuration Guide](configuration-guide.md)
4. Check the [FAQ](../faq.md) (if available)
5. Report issues on GitHub: [Project Issues](https://github.com/tomasgurtler21/TransparentAiAgent/issues)

## Next Steps

- **Explore features**: Try different conversation modes (Chat, Teaching, Knowledge Library)
- **Customize**: Adjust the system prompt and LLM parameters to your needs
- **Add knowledge**: Create custom knowledge entries in `data/knowledge/`
- **Learn more**: Read the [Feature Guides](../README.md) to understand all capabilities

Welcome to TransparentAiAgent!
