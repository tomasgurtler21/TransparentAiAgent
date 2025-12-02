# Troubleshooting Guide

This guide helps you diagnose and fix common issues when running TransparentAiAgent.

**For installation issues**, see the [Installation Guide](installation-guide.md).

---

## Quick Reference

| Issue | Quick Fix | Section |
|-------|-----------|---------|
| Port already in use | Kill process or use different port | [Port Conflicts](#issue-1-port-already-in-use) |
| Can't connect in browser | Check server running, firewall | [Connection Issues](#issue-2-signalr-connection-failed) |
| API authentication failed | Verify API key in appsettings.json | [API Authentication](#issue-3-api-authentication-failed) |
| MCP server not starting | Check executable path and permissions | [MCP Servers](#issue-4-mcp-server-not-starting) |
| Streaming not working | Check provider support | [Streaming](#issue-5-streaming-not-working) |
| High memory usage | Start new conversation or restart | [Memory Issues](#issue-6-high-memory-usage) |
| Slow UI | Reduce update frequency | [Performance](#issue-7-slow-ui-updates) |
| Context not updating | Verify context management | [Context Issues](#issue-8-context-not-updating) |
| Can't find data | Check AppData folder | [Data Location](#issue-9-cant-find-user-data) |
| Settings not persisting | Check folder permissions | [Data Persistence](#issue-10-data-not-persisting-after-restart) |

---

## Common Runtime Issues

### Issue 1: Port Already in Use

**Good News**: As of the latest version, the application **automatically finds an available port** at startup. You should no longer encounter port conflict issues!

**How it works**:
- The application requests an available port from the operating system
- The OS assigns an available port automatically
- The console will show the actual port being used (e.g., "Server is listening on http://localhost:54321")
- Your browser will open automatically to the correct URL

**If you still see a port conflict error**:

This might happen if you're running an older version or using custom launch settings.

**Solution A**: Update to the latest version

Ensure you're running the latest version of TransparentAiAgent which includes automatic port selection.

**Solution B**: Find and kill the conflicting process (for debugging)

```bash
# Find process using a specific port
netstat -ano | findstr :5000

# Note the PID (last column) and kill it
taskkill /PID <PID> /F
```

**Solution C**: Force a specific port (advanced users only)

If you need to use a specific port for testing or development:

**Windows Command Prompt**:
```cmd
set ASPNETCORE_URLS=http://localhost:5050
TransparentAiAgentGui.exe
```

**Windows PowerShell**:
```powershell
$env:ASPNETCORE_URLS="http://localhost:5050"
.\TransparentAiAgentGui.exe
```

Then navigate to `http://localhost:5050` in your browser.

**Note**: With automatic port selection, these manual workarounds are rarely needed.

---

### Issue 2: SignalR Connection Failed

**Symptom**: Browser shows "Disconnected" or connection errors in the console.

**Error in Browser Console** (Press F12 to open):
```
Failed to start the connection: Error: WebSocket failed to connect.
```

**Solution A**: Verify the application is running

1. Check the console/terminal window where you started the application
2. You should see messages like "Now listening on: http://localhost:5000"
3. If not running, start it by double-clicking the `.exe` file

**Solution B**: Check the URL

1. Ensure you're navigating to `http://localhost:5000` (not HTTPS)
2. If using HTTPS (`https://localhost:5001`), make sure the certificate is trusted

**Solution C**: Check Windows Firewall

1. Open **Windows Defender Firewall**
2. Click **Allow an app through firewall**
3. Find `TransparentAiAgentGui.exe` and ensure it's allowed for "Private" networks
4. If not listed, click **Allow another app** and browse to the executable

**Solution D**: Try a different browser

Some browsers may have stricter WebSocket policies. Try:
- Edge
- Chrome
- Firefox

---

### Issue 3: API Authentication Failed

**Symptom**: Application starts, but messages fail to send with authentication errors.

**Common error messages**:
```
401 Unauthorized
403 Forbidden
Invalid API key
```

**Solution A**: Verify your API key in appsettings.json

1. Open `appsettings.json` in your application folder
2. Find your active LLM provider (check `"ActiveProvider"` setting)
3. Verify the API key:
   - No extra spaces before/after the key
   - No quotes around the key (unless part of the actual key)
   - Complete key copied correctly

**Solution B**: Check your LLM provider account

- **Anthropic**: Visit https://console.anthropic.com/
  - Check if you have available credits
  - Verify the API key is active
  - Regenerate key if necessary

- **OpenAI**: Visit https://platform.openai.com/
  - Check if you have available credits
  - Verify the API key hasn't been revoked
  - Check usage limits

**Solution C**: Test your API key manually

**For Anthropic**:
```bash
curl https://api.anthropic.com/v1/messages \
  -H "x-api-key: YOUR_API_KEY" \
  -H "anthropic-version: 2023-06-01" \
  -H "content-type: application/json" \
  -d '{"model":"claude-haiku-4-5-20251001","max_tokens":10,"messages":[{"role":"user","content":"Hi"}]}'
```

**For OpenAI**:
```bash
curl https://api.openai.com/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"model":"gpt-4","messages":[{"role":"user","content":"Hi"}],"max_tokens":10}'
```

If these commands fail, the issue is with your API key or account.

---

### Issue 4: MCP Server Not Starting

**Symptom**: MCP tools (like todo-list, context7, etc.) are not available in the application.

**Error in application console**:
```
Failed to start MCP server: todo-list
```

**Solution A**: Verify the MCP server configuration

1. Open `appsettings.json`
2. Find the `"MCP"` section
3. Check the server configuration:
   - **Command**: Path to executable (use `\\` for Windows paths in JSON)
   - **Args**: Command arguments as array
   - **Enabled**: Must be `true`

**Example configuration**:
```json
"MCP": {
  "Servers": [
    {
      "Name": "todo-list",
      "Command": "npx",
      "Args": ["-y", "@anthropic/mcp-server-todo-list"],
      "Enabled": true
    }
  ]
}
```

**Solution B**: Test the MCP server manually

For NPX-based servers:
```bash
npx -y @anthropic/mcp-server-todo-list
```

For executable-based servers:
```bash
C:\path\to\McpServer.exe
```

The server should start and show protocol messages. If it crashes or shows errors, the issue is with the MCP server itself, not TransparentAiAgent.

**Solution C**: Check antivirus/security software

Some security software blocks applications from spawning child processes. Temporarily disable antivirus or add an exception for `TransparentAiAgentGui.exe`.

**Solution D**: Verify Node.js/npx is installed (for NPX-based servers)

Many MCP servers use NPX. Ensure Node.js is installed:
```bash
node --version
npx --version
```

If not installed, download from https://nodejs.org/

---

### Issue 5: Streaming Not Working

**Symptom**: AI responses appear all at once instead of word-by-word.

**Solution A**: Check if your LLM provider supports streaming

Not all LLM providers or models support streaming. Check the provider documentation:
- **Anthropic Claude**: ✅ Supports streaming
- **OpenAI**: ✅ Supports streaming
- **Azure OpenAI**: ✅ Supports streaming (if enabled in deployment)

**Solution B**: Check browser console for errors

1. Press **F12** to open browser developer tools
2. Go to **Console** tab
3. Look for WebSocket or SignalR errors
4. Try refreshing the page

**Solution C**: Network issues

If on a slow or unstable network connection:
- Streaming chunks may arrive slowly
- May appear as "all at once" if delays are short
- Try testing on a faster connection

---

### Issue 6: High Memory Usage

**Symptom**: Application uses excessive RAM (over 1 GB or causing system slowdown).

**Understanding memory usage**: The application loads conversations on-demand from JSON files. Only the current conversation is kept in memory, not all conversations. High memory usage is typically caused by:
- A very large current conversation (hundreds of messages with long content)
- Browser memory accumulation (especially in long-running sessions)
- Memory leaks in browser or application

**Solution A**: Start a new conversation

If your current conversation has become very large:
1. Start a new conversation from the conversation selector
2. The previous conversation will be automatically saved to disk
3. Memory should drop back to baseline

**Solution B**: Restart the application

Simply closing and reopening the application will clear memory:
1. Close the browser tab
2. Close the console window (or press Ctrl+C)
3. Restart the application

**Solution C**: Clear browser cache and restart browser

Browser memory can accumulate over time:
1. Clear browser cache (Ctrl+Shift+Delete)
2. Close all browser windows
3. Reopen browser and navigate to the application

**Solution D**: Check for multiple instances

Ensure you don't have multiple instances of the application running:
1. Open Task Manager (Ctrl+Shift+Esc)
2. Look for multiple `TransparentAiAgentGui.exe` processes
3. End extra processes

**Expected memory usage**:
- Fresh start: ~100-200 MB
- After 50 messages in current conversation: ~200-400 MB
- After 500 messages in current conversation: ~500-800 MB

If significantly higher, something may be wrong.

**Note**: Deleting old conversations will NOT reduce memory usage since only the current conversation is loaded into memory.

---

### Issue 7: Slow UI Updates

**Symptom**: User interface feels sluggish or unresponsive.

**Solution A**: Check browser performance

1. Try a different browser (Chrome, Edge, Firefox)
2. Close other tabs to free up memory
3. Disable browser extensions temporarily
4. Clear browser cache

**Solution B**: Check system resources

1. Open Task Manager
2. Check CPU and Memory usage
3. Close other applications if system is under load

**Solution C**: Reduce conversation size

Large conversations with hundreds of messages can slow the UI:
1. Start a new conversation for better performance
2. Archive old conversations
3. Consider adjusting the context window size in settings (if available in UI)

---

### Issue 8: Context Status Not Updating

**Symptom**: Messages show "In Context" when they should be marked as "Truncated" or "Excluded".

**Solution A**: Adjust context window size

1. Open the application settings (if available in UI)
2. Reduce the context window size
3. Send a new message to trigger context recalculation

**Solution B**: Restart the conversation

Context calculations happen when messages are sent:
1. Start a new conversation
2. The context management should work correctly in the new conversation

**Solution C**: Check appsettings.json

Verify the context window configuration:
```json
"Agent": {
  "ContextWindowSize": 20  ← Adjust this number
}
```

Lower numbers = fewer messages in context = more aggressive truncation.

---

### Issue 9: Can't Find User Data

**Symptom**: Need to locate conversation history, user settings, or long-term memory files.

**Solutions**:

**A. Find user data directory**:

```bash
# Open Run dialog (Win + R) and enter:
%AppData%\TransparentAiAgent

# Or in PowerShell:
explorer "$env:APPDATA\TransparentAiAgent"
```

**B. Data directory structure**:
```
%AppData%\TransparentAiAgent\
├── user-settings.json          # User preferences
├── conversations/              # All conversation history
├── memory/                     # Long-term memory
└── logs/                       # Application logs (future)
```

**C. View/edit user settings**:
```json
{
  "ContextWindowSize": 200,
  "EnableMemory": false
}
```

**See**: [Data Storage Guide](data-storage.md) for complete documentation.

---

### Issue 10: Data Not Persisting After Restart

**Symptom**: Settings or conversations lost after closing application.

**Solutions**:

**A. Check data directory permissions**:
```bash
# Windows PowerShell - Check if directory is writable:
Test-Path -Path "$env:APPDATA\TransparentAiAgent" -PathType Container

# If false, check Windows user permissions
```

**B. Check application logs for write errors**:
- Look for exceptions mentioning "access denied"
- Check if antivirus is blocking file writes

**C. Verify data directory exists**:
```bash
# Windows - Create manually if missing:
mkdir "$env:APPDATA\TransparentAiAgent"
mkdir "$env:APPDATA\TransparentAiAgent\conversations"
mkdir "$env:APPDATA\TransparentAiAgent\memory"
```

**D. Check if running as different user**:
- Each Windows user has separate AppData directory
- Ensure running application as same user account

---

### Issue 11: Configuration vs User Settings Confusion

**Symptom**: Not sure whether to edit appsettings.json or user-settings.json.

**Quick Reference**:

| Setting | Location | File | Restart Required? |
|---------|----------|------|-------------------|
| **LLM Provider** | App directory | `appsettings.json` | ✅ Yes |
| **API Keys** | App directory | `appsettings.json` | ✅ Yes |
| **System Prompt** | App directory | `appsettings.json` | ✅ Yes |
| **Context Window Size** | User data | `user-settings.json` | ❌ No (future) |
| **Enable Memory** | User data | `user-settings.json` | ❌ No (future) |

**Application Configuration** (`appsettings.json`):
- Located in: `TransparentAiAgentGui/appsettings.json`
- Contains: LLM providers, API keys, deployment settings
- See: [LLM Provider Selector Guide](llm-provider-selector.md)

**User Settings** (`user-settings.json`):
- Located in: `%AppData%\TransparentAiAgent\user-settings.json`
- Contains: User preferences, runtime settings
- See: [Data Storage Guide](data-storage.md)

---

## General Troubleshooting Tips

### Using Browser Developer Tools

Press **F12** to open developer tools in your browser:

1. **Console tab**: Check for JavaScript errors or warnings
2. **Network tab**:
   - Filter by "WS" to see WebSocket/SignalR messages
   - Check connection status
3. **Application tab**: Check local storage if issues with persistence

### Check Application Logs

The application outputs logs to the console window:
1. Look for error messages in red
2. Check for warnings in yellow
3. Note any stack traces for reporting issues

Common log messages:
- "Now listening on..." = Application started successfully
- "Failed to start MCP server..." = MCP configuration issue
- "401 Unauthorized" = API key problem

### Performance Monitoring

**Check memory usage**:
1. Open Task Manager (Ctrl+Shift+Esc)
2. Find `TransparentAiAgentGui.exe`
3. Check memory column

**Expected performance**:
- Memory: 100-500 MB (depending on conversation size)
- CPU: <1% idle, 5-20% during AI responses
- Response time: 500-3000ms (depends on LLM provider)

If significantly different, see [Issue 6: High Memory Usage](#issue-6-high-memory-usage) or [Issue 7: Slow UI Updates](#issue-7-slow-ui-updates).

---

## Security Best Practices

### Protecting Your API Keys

1. **Never share appsettings.json** - It contains your API keys
2. **Rotate keys regularly** - Change them every few months
3. **Monitor usage** - Check your LLM provider dashboard for unexpected usage
4. **Use HTTPS** - Navigate to `https://localhost:5001` instead of HTTP

### Local Network Security

The application runs on localhost only, meaning:
- ✅ Not accessible from other devices on your network
- ✅ Not accessible from the internet
- ⚠️ Other applications on your computer can access it
- ⚠️ Browser extensions can access it

For maximum security, use HTTPS: `https://localhost:5001`

---

## Getting Additional Help

### Before Reporting Issues

1. Check this troubleshooting guide
2. Review the [Installation Guide](installation-guide.md)
3. Check the [Configuration Guide](llm-provider-selector.md)
4. Try restarting the application
5. Collect error messages from console and browser

### Where to Get Help

- **Installation issues**: See [Installation Guide](installation-guide.md)
- **Configuration issues**: See [LLM Provider Selector Guide](llm-provider-selector.md)
- **Data management**: See [Data Storage Guide](data-storage.md)
- **Report bugs**: [GitHub Issues](https://github.com/tomasgurtler21/TransparentAiAgent/issues)

### What to Include When Reporting

1. **Operating System**: Windows version
2. **Error message**: Exact text from console/browser
3. **Steps to reproduce**: What you did before the error
4. **LLM provider**: Which provider you're using (don't include API key!)
5. **Application version**: If available

---

**Last Updated**: 2025-12-02
