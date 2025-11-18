# Deployment & Troubleshooting Guide

Guide for running and troubleshooting TransparentAiAgent locally.

## Quick Start (Once Built)

```bash
# Navigate to solution directory
cd C:\programming\TransparentAiAgent\TransparentAiAgent

# Run the application
dotnet run --project TransparentAiAgentGui

# Or in Visual Studio: Press F5

# Open browser to: http://localhost:5000
```

**Expected output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shutdown.
```

---

## First-Time Setup Checklist

### 1. Prerequisites

- [ ] **.NET 8.0 SDK** installed
  ```bash
  dotnet --version
  # Should show: 8.0.x
  ```

- [ ] **Visual Studio 2022** (or VS Code with C# extension)

- [ ] **Browser** (Edge, Chrome, or Firefox)

- [ ] **Azure OpenAI credentials** (for testing on other station)
  - Endpoint URL
  - API key
  - Deployment name

### 2. Configuration Files

**Create**: `TransparentAiAgentGui/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Agent": {
    "SystemPrompt": "You are a helpful, transparent AI assistant.",
    "ContextWindowSize": 20
  },
  "LLM": {
    "Provider": "AzureOpenAI",
    "Temperature": 0.7,
    "TopP": 1.0,
    "AzureOpenAI": {
      "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
      "ApiKey": "YOUR-API-KEY-HERE",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview"
    }
  },
  "MCP": {
    "Servers": [
      {
        "Name": "todo-list",
        "Command": "C:\\programming\\MCP\\TODO list\\MCP-SimpleTodoList\\McpTodoList\\bin\\Release\\net8.0\\McpTodoList.exe",
        "Args": [],
        "Transport": "stdio"
      }
    ]
  }
}
```

**Security Note**: Never commit `appsettings.Development.json` with real API keys!

**Add to `.gitignore`**:
```
appsettings.Development.json
appsettings.*.json
```

### 3. Build Solution

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Expected: Build succeeded. 0 Error(s)
```

### 4. Run Tests

```bash
# Run all tests
dotnet test

# Expected: All tests passing
```

---

## Port Configuration

### Default Ports

- **HTTP**: 5000
- **HTTPS**: 5001

### Change Ports

**Option 1**: Command line
```bash
dotnet run --project TransparentAiAgentGui --urls "http://localhost:8080;https://localhost:8081"
```

**Option 2**: `launchSettings.json`

**File**: `TransparentAiAgentGui/Properties/launchSettings.json`

```json
{
  "profiles": {
    "TransparentAiAgentGui": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

## MCP Server Configuration

### Verify MCP Server Paths

**Test MCP server manually**:

```bash
# Navigate to MCP server directory
cd C:\programming\MCP\TODO list\MCP-SimpleTodoList\McpTodoList\bin\Release\net8.0

# Run server
McpTodoList.exe

# Should start and show MCP protocol messages
```

### Common MCP Server Issues

**Problem**: "MCP server not found"

**Solutions**:
1. Verify path in `appsettings.json`
2. Check if server executable exists
3. Ensure server is built (Release or Debug)

**Example MCP server configuration**:

```json
{
  "MCP": {
    "Servers": [
      {
        "Name": "todo-list",
        "Command": "C:\\path\\to\\McpTodoList.exe",
        "Args": [],
        "Transport": "stdio",
        "Enabled": true
      },
      {
        "Name": "context7",
        "Command": "npx",
        "Args": ["-y", "@upstash/context7-mcp"],
        "Transport": "stdio",
        "Enabled": true
      }
    ]
  }
}
```

**Note**: Paths must use double backslashes (`\\`) in JSON.

---

## Troubleshooting Common Issues

### Issue 1: Port Already in Use

**Error**:
```
Failed to bind to address http://127.0.0.1:5000: address already in use.
```

**Solutions**:

**A. Find and kill process using port**:
```bash
# Windows
netstat -ano | findstr :5000
# Note the PID (last column)

taskkill /PID <PID> /F
```

**B. Use different port**:
```bash
dotnet run --project TransparentAiAgentGui --urls "http://localhost:5050"
```

---

### Issue 2: SignalR Connection Failed

**Error in Browser Console**:
```
Failed to start the connection: Error: WebSocket failed to connect.
```

**Solutions**:

**A. Check if server is running**:
```bash
# Should see server logs in terminal
```

**B. Browser is connecting to wrong URL**:
- Verify URL: `http://localhost:5000`
- Check if HTTPS redirect is forcing https://localhost:5001

**C. Firewall blocking localhost**:
- Temporarily disable firewall
- Add exception for Kestrel/dotnet.exe

---

### Issue 3: Azure OpenAI Authentication Failed

**Error**:
```
401 Unauthorized
```

**Solutions**:

**A. Verify credentials**:
```bash
# Test with curl
curl -X POST https://YOUR-RESOURCE.openai.azure.com/openai/deployments/gpt-4/chat/completions?api-version=2024-02-15-preview \
  -H "api-key: YOUR-API-KEY" \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Test"}]}'
```

**B. Check configuration**:
- Endpoint URL correct?
- API key correct?
- Deployment name correct?
- API version supported?

**C. Azure portal check**:
- Is resource active?
- Are keys regenerated?
- Is quota available?

---

### Issue 4: MCP Server Not Starting

**Error**:
```
Failed to start MCP server: todo-list
```

**Solutions**:

**A. Verify server executable**:
```bash
# Try running manually
C:\path\to\McpTodoList.exe

# Should start without errors
```

**B. Check permissions**:
- Can your app spawn child processes?
- Antivirus blocking?

**C. Check server logs**:
- MCP servers should log to stderr
- Check TransparentAiAgent logs for MCP output

**D. Verify transport**:
```json
{
  "Transport": "stdio"  // Must be "stdio" for local servers
}
```

---

### Issue 5: Streaming Not Working

**Symptom**: Response appears all at once, not incrementally.

**Solutions**:

**A. Check LLM provider streaming support**:
```csharp
// In your LLM provider
public async IAsyncEnumerable<StreamChunk> StreamCompletionAsync(...)
{
    // Must actually stream, not batch
    await foreach (var chunk in actualStream)
    {
        yield return chunk; // ✅
    }
}
```

**B. Check SignalR buffering**:
```csharp
// In Blazor component
await foreach (var chunk in stream)
{
    message += chunk;
    StateHasChanged(); // Must call after each chunk
    await Task.Yield(); // Allow SignalR to send
}
```

**C. Browser dev tools**:
- Open Network tab
- Filter: WS (WebSocket)
- Check SignalR messages flowing

---

### Issue 6: High Memory Usage

**Symptom**: App uses excessive RAM.

**Solutions**:

**A. Check conversation history**:
```csharp
// Implement cleanup
if (conversationHistory.Count > 1000)
{
    // Remove old messages (keep context)
    conversationHistory.RemoveRange(0, 500);
}
```

**B. Check transparency event storage**:
```csharp
// Limit event storage
if (events.Count > 10000)
{
    events.RemoveRange(0, 5000);
}
```

**C. Check for memory leaks**:
- SignalR connections not disposed?
- HTTP clients not disposed?
- Event handlers not unsubscribed?

---

### Issue 7: Slow UI Updates

**Symptom**: UI feels sluggish.

**Solutions**:

**A. Reduce StateHasChanged() calls**:
```csharp
// Bad: Call for every character
foreach (var char in text)
{
    message += char;
    StateHasChanged(); // Too frequent!
}

// Good: Batch updates
foreach (var chunk in chunks)
{
    message += chunk;
    if (chunk.EndsWith(" ")) // Update per word
    {
        StateHasChanged();
    }
}
```

**B. Check browser performance**:
- Open browser dev tools
- Check Performance tab
- Look for long tasks

**C. Optimize rendering**:
```razor
@* Use @key for list items *@
@foreach (var message in messages)
{
    <MessageComponent @key="message.Id" Message="@message" />
}
```

---

### Issue 8: Context Not Updating

**Symptom**: Messages marked as "In Context" when they should be truncated.

**Solutions**:

**A. Verify context management**:
```csharp
public void TruncateContext(int maxMessages)
{
    var messagesInContext = messages.Where(m => m.ContextStatus == InContext).ToList();

    if (messagesInContext.Count > maxMessages)
    {
        var toTruncate = messagesInContext.Count - maxMessages;
        for (int i = 0; i < toTruncate; i++)
        {
            messagesInContext[i].ContextStatus = TruncatedFromContext; // ✅
        }
    }
}
```

**B. Check UI binding**:
```razor
@* Ensure UI reflects status *@
<div class="message @GetContextClass(message)">
    @message.Content
</div>

@code {
    string GetContextClass(Message msg)
    {
        return msg.ContextStatus == InContext ? "in-context" : "truncated";
    }
}
```

---

### Issue 9: Can't Find User Data or Settings

**Symptom**: Need to locate conversation history, user settings, or long-term memory files.

**Solutions**:

**A. Find user data directory**:

**Windows**:
```bash
# Open Run dialog (Win + R) and enter:
%AppData%\TransparentAiAgent

# Or in PowerShell:
explorer "$env:APPDATA\TransparentAiAgent"
```

**macOS/Linux**:
```bash
# Open in terminal:
open ~/.config/TransparentAiAgent

# Or navigate manually:
cd ~/.config/TransparentAiAgent
ls -la
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

## Debugging Tips

### Enable Verbose Logging

**In `appsettings.Development.json`**:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.AspNetCore.SignalR": "Debug",
      "TransparentAiAgent": "Trace"
    }
  }
}
```

### Browser Developer Tools

**F12** to open dev tools:

1. **Console**: Check for JavaScript errors
2. **Network**:
   - WS tab: SignalR WebSocket messages
   - Check connection status
3. **Application**: Check local storage, session storage

### Visual Studio Debugging

**Breakpoints**:
```csharp
private async Task SendMessage()
{
    // Set breakpoint here ← F9
    var response = await orchestrator.ProcessAsync(message);
}
```

**Watch variables**:
- `conversationManager.Messages`
- `message.ContextStatus`
- `llmProvider.CurrentState`

**Output window**:
- Shows all logs
- Filter by "TransparentAiAgent"

---

## Performance Monitoring

### Check Localhost Latency

**In browser console**:

```javascript
// Measure SignalR round-trip
let start = performance.now();
// Trigger button click
// When response arrives:
let latency = performance.now() - start;
console.log(`Latency: ${latency}ms`);

// Expected: 1-10ms for localhost
```

### Monitor Memory

**Task Manager** (Windows):
- Find `TransparentAiAgentGui.exe`
- Check memory usage
- Should be < 500MB typically

### Monitor SignalR

**In `Startup.cs` / `Program.cs`**:

```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Development only
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});
```

---

## Development Workflow

### Typical Development Session

```bash
# 1. Start the app
dotnet run --project TransparentAiAgentGui

# 2. Make code changes
# (Edit files in Visual Studio)

# 3. Hot reload (if supported)
# Or Ctrl+C and restart

# 4. Run tests
dotnet test

# 5. Commit changes
git add .
git commit -m "Add feature X"
```

### Watch Mode (Auto-Rebuild)

```bash
dotnet watch run --project TransparentAiAgentGui

# Automatically rebuilds and restarts on file changes
```

---

## Security Considerations

### Local Deployment Security

**Good**:
- ✅ API keys in local config files (not committed)
- ✅ Running on localhost (not exposed to network)
- ✅ HTTPS available (localhost:5001)

**Be aware**:
- ⚠️ Other processes on your machine can access localhost
- ⚠️ Browser extensions can access localhost
- ⚠️ Malware could intercept localhost traffic

**Best practices**:
1. Don't commit API keys
2. Use HTTPS even for localhost
3. Rotate API keys regularly
4. Use environment variables for sensitive data

### Environment Variables

**Instead of `appsettings.json`**:

```bash
# Set environment variables
set LLM__AzureOpenAI__ApiKey=your-key-here
set LLM__AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/

# Run app
dotnet run --project TransparentAiAgentGui
```

**In code**:
```csharp
var apiKey = configuration["LLM:AzureOpenAI:ApiKey"];
// Reads from environment variable if set
```

---

## Network Configuration

### Firewall Rules

**If Windows Firewall blocks app**:

1. Open Windows Defender Firewall
2. Advanced Settings → Inbound Rules
3. New Rule → Program
4. Select: `C:\Program Files\dotnet\dotnet.exe`
5. Allow connection
6. Apply to all profiles

### CORS (If Needed Later)

**If you add external API calls**:

```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Use CORS
app.UseCors("AllowLocalhost");
```

---

## Testing on Other Station (Azure OpenAI)

### Preparing for Other Station

**1. Export configuration** (without secrets):

**File**: `config-template.json`
```json
{
  "LLM": {
    "AzureOpenAI": {
      "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
      "ApiKey": "*** SET THIS ON OTHER STATION ***",
      "DeploymentName": "gpt-4"
    }
  }
}
```

**2. Document credentials needed**:
- Azure OpenAI resource name
- Deployment name
- API version

**3. Copy code to other station**:
```bash
# Commit and push to Git
git push origin main

# On other station, clone
git clone <repository-url>
```

**4. Set up on other station**:
```bash
cd TransparentAiAgent
dotnet restore
dotnet build

# Add API key to appsettings.Development.json
# Run
dotnet run --project TransparentAiAgentGui
```

---

## Useful Commands Reference

### Build & Run

```bash
# Clean build
dotnet clean
dotnet build

# Run (Development)
dotnet run --project TransparentAiAgentGui --environment Development

# Run (Production)
dotnet run --project TransparentAiAgentGui --environment Production

# Watch mode (auto-reload)
dotnet watch run --project TransparentAiAgentGui
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~MessageTests"

# Run with coverage
dotnet test /p:CollectCoverage=true

# Verbose output
dotnet test -v detailed
```

### Package Management

```bash
# Add package
dotnet add package Microsoft.Extensions.Http

# Update package
dotnet add package Microsoft.Extensions.Http --version 8.0.0

# List packages
dotnet list package
```

### Solution Management

```bash
# Add project to solution
dotnet sln add TransparentAiAgentCore/TransparentAiAgentCore.csproj

# List projects in solution
dotnet sln list
```

---

## Health Check Endpoint

### Add Health Check (Future)

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck("mcp-servers", <custom-health-check>)
    .AddCheck("llm-provider", <custom-health-check>);

app.MapHealthChecks("/health");
```

**Access**: http://localhost:5000/health

**Response**:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "self": { "status": "Healthy" },
    "mcp-servers": { "status": "Healthy" },
    "llm-provider": { "status": "Healthy" }
  }
}
```

---

## Performance Benchmarks (Expected)

### Localhost Performance

- **SignalR round-trip**: 1-10ms
- **Message processing** (no LLM): 1-5ms
- **LLM call** (Azure OpenAI): 500-3000ms (varies)
- **MCP tool call**: 10-100ms (depends on tool)
- **UI update (SignalR)**: 1-5ms

### Memory Usage (Expected)

- **Initial**: ~100MB
- **With 100 messages**: ~150MB
- **With 1000 messages**: ~300MB
- **Peak (streaming)**: ~500MB

### CPU Usage (Expected)

- **Idle**: <1%
- **During LLM call**: 5-15%
- **During streaming**: 10-20%

**If you see significantly different numbers, something might be wrong.**

---

## Quick Checklist Before Starting Development Tomorrow

- [ ] .NET 8.0 SDK installed
- [ ] Visual Studio 2022 ready
- [ ] Solution builds without errors
- [ ] Tests run and pass
- [ ] Azure OpenAI credentials ready (for other station)
- [ ] MCP server paths verified
- [ ] Git repository set up
- [ ] `appsettings.Development.json` created (not committed)
- [ ] Documentation read and understood

**Ready to code Phase 1!** 🚀

---

**Last Updated**: 2025-11-18
