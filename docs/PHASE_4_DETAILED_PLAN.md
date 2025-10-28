# Phase 4: Basic Web UI - Detailed Implementation Plan

**Status**: Implementation Ready
**Last Updated**: 2025-10-28

## Overview

This document provides a comprehensive implementation plan for Phase 4: Building the basic web UI using Blazor Server with Test-Driven Development (TDD).

## Goals

Build a functional web interface that enables end-to-end testing of the agent:
- Blazor Server application with real-time updates
- Chat interface for sending/receiving messages
- Visual context status indicators (what's in LLM context vs truncated)
- Message display with proper formatting
- State management for conversation UI
- Real-time streaming support via SignalR

## Why Phase 4 Before Phase 5 (MCP)?

**Key Decision** (DD-018): Implementing UI before MCP tools enables:
- ✅ End-to-end functional testing earlier
- ✅ Validation of agent architecture with real UI
- ✅ Proves basic agent works before adding tool complexity
- ✅ User can interact sooner
- ✅ Easier debugging (can see agent behavior visually)

## Architecture Layer: Presentation

Phase 4 focuses on:
- **Presentation Layer**: Blazor Server components, pages, services
- **Integration**: Connecting UI to Application Layer (Agent Orchestrator, Conversation Manager)

## Blazor Server Architecture Recap

```
Browser (localhost:5000)
    ↕ SignalR WebSocket (~1-5ms latency)
Server Process (localhost:5000)
    ├─ Blazor components render on server
    ├─ Direct access to C# services
    ├─ Direct LLM API calls
    ├─ Direct MCP server access (Phase 5)
    └─ Only UI updates sent to browser
```

**Benefits:**
- No REST API needed
- No CORS issues
- No separate frontend build
- Real-time streaming built-in
- Simple deployment
- API keys stay secure on server

## Lean TDD Approach

**IMPORTANT**: We follow **Lean TDD** for components with meaningful behavior.

### What We DO Test:
✅ **Business Logic & Behavior**
- Component state management (e.g., "sending message updates IsSending flag")
- Service interactions (e.g., "SendMessage calls AgentOrchestrator")
- Event handling (e.g., "OnSubmit validates input before sending")
- Conditional rendering logic (e.g., "truncated messages show warning icon")
- State changes (e.g., "new message added to conversation list")

### What We DON'T Test:
❌ **Blazor Framework Features**
- Razor syntax rendering (if it compiles, it works)
- Component lifecycle methods with no logic (OnInitialized with just assignment)
- Simple parameter passing (e.g., `[Parameter] public string Text { get; set; }`)
- CSS classes or styling
- Framework data binding (e.g., `@bind-Value`)

### Blazor Testing Note:
- We use **bUnit** for Blazor component testing (unit tests for components)
- Focus on component **behavior**, not rendering details
- Test state changes, event handlers, and service calls
- Mock dependencies (IAgentOrchestrator, IConversationManager)

## Components & Implementation Order

### 1. Project Setup

#### 1.1 Create Blazor Server Project

**Task**: Add new Blazor Server project to solution

```bash
# Create new Blazor Server project
dotnet new blazorserver -o TransparentAiAgentUI -f net8.0

# Add to solution
dotnet sln add TransparentAiAgentUI/TransparentAiAgentUI.csproj

# Add reference to Core project
dotnet add TransparentAiAgentUI/TransparentAiAgentUI.csproj reference TransparentAiAgentCore/TransparentAiAgentCore.csproj
```

**Project Structure:**
```
TransparentAiAgentUI/
├── Components/
│   ├── Chat/
│   │   ├── MessageDisplay.razor
│   │   ├── MessageList.razor
│   │   └── ChatInput.razor
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── Pages/
│       └── Home.razor
├── Services/
│   ├── IConversationUIService.cs
│   └── ConversationUIService.cs
├── Models/
│   └── UIMessage.cs (view model)
├── Program.cs
└── appsettings.json
```

**Tests**: ❌ NO TESTS (project setup)

---

#### 1.2 Configure Dependency Injection

**File**: `Program.cs`

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TransparentAiAgentCore.Application.Interfaces;
using TransparentAiAgentCore.Application.Services;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Infrastructure.Configuration;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Infrastructure.Serialization;
using TransparentAiAgentUI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Core services
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<ITransparencyService, TransparencyService>();
builder.Services.AddSingleton<ISerializationService, SerializationService>();

// Register Application services (from Phase 2 & 3)
builder.Services.AddSingleton<IConversationManager, ConversationManager>();
builder.Services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();

// Register UI services
builder.Services.AddScoped<IConversationUIService, ConversationUIService>();

// Load configuration
var configService = builder.Services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
var config = configService.LoadConfiguration();
builder.Services.AddSingleton(config);

var app = builder.Build();

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

**Tests**: ❌ NO TESTS (standard Blazor configuration)

---

### 2. View Models (UI Models)

#### 2.1 UIMessage Class

**Purpose**: View model for displaying messages with UI-specific properties

**File**: `TransparentAiAgentUI/Models/UIMessage.cs`

```csharp
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentUI.Models;

/// <summary>
/// View model for displaying messages in UI
/// </summary>
public class UIMessage
{
    public Guid Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MessageContextStatus ContextStatus { get; set; }

    /// <summary>
    /// CSS class for styling based on role
    /// </summary>
    public string CssClass => Role switch
    {
        MessageRole.User => "message-user",
        MessageRole.Assistant => "message-assistant",
        MessageRole.System => "message-system",
        MessageRole.Tool => "message-tool",
        _ => "message-default"
    };

    /// <summary>
    /// Icon to display for context status
    /// </summary>
    public string ContextStatusIcon => ContextStatus switch
    {
        MessageContextStatus.InContext => "✅",
        MessageContextStatus.TruncatedFromContext => "⚠️",
        _ => ""
    };

    /// <summary>
    /// Tooltip text for context status
    /// </summary>
    public string ContextStatusTooltip => ContextStatus switch
    {
        MessageContextStatus.InContext => "In LLM Context",
        MessageContextStatus.TruncatedFromContext => "Not in LLM Context (truncated)",
        _ => ""
    };

    /// <summary>
    /// Create UIMessage from domain message
    /// </summary>
    public static UIMessage FromDomainMessage(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return new UIMessage
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            ContextStatus = message.ContextStatus
        };
    }
}
```

**Tests** (Lean - Transformation Logic Only):
- ✅ FromDomainMessage() throws ArgumentNullException for null
- ✅ FromDomainMessage() maps all properties correctly
- ✅ CssClass returns correct class for each MessageRole
- ✅ ContextStatusIcon returns correct icon for each status
- ✅ ContextStatusTooltip returns correct tooltip for each status
- ❌ NO tests for: simple properties (trivial getters/setters)

**Test File**: `TransparentAiAgentUI_Tests/Models/UIMessageTests.cs`

---

### 3. UI Services (State Management)

#### 3.1 IConversationUIService Interface

**Purpose**: Manage UI state and coordinate with backend services

**File**: `TransparentAiAgentUI/Services/IConversationUIService.cs`

```csharp
using TransparentAiAgentUI.Models;

namespace TransparentAiAgentUI.Services;

public interface IConversationUIService
{
    /// <summary>
    /// Get all messages for display
    /// </summary>
    IReadOnlyList<UIMessage> Messages { get; }

    /// <summary>
    /// Is agent currently processing
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Send user message to agent
    /// </summary>
    Task SendMessageAsync(string content);

    /// <summary>
    /// Clear conversation
    /// </summary>
    Task ClearConversationAsync();

    /// <summary>
    /// Raised when messages change
    /// </summary>
    event EventHandler? MessagesChanged;

    /// <summary>
    /// Raised when processing state changes
    /// </summary>
    event EventHandler<bool>? ProcessingStateChanged;
}
```

**Tests**: ❌ NO TESTS (interface has no logic)

---

#### 3.2 ConversationUIService Class

**Purpose**: Implementation of UI state management

**File**: `TransparentAiAgentUI/Services/ConversationUIService.cs`

```csharp
using TransparentAiAgentCore.Application.Interfaces;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentUI.Models;

namespace TransparentAiAgentUI.Services;

public class ConversationUIService : IConversationUIService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IConversationManager _conversationManager;
    private readonly List<UIMessage> _messages = new();
    private bool _isProcessing;

    public IReadOnlyList<UIMessage> Messages => _messages.AsReadOnly();
    public bool IsProcessing => _isProcessing;

    public event EventHandler? MessagesChanged;
    public event EventHandler<bool>? ProcessingStateChanged;

    public ConversationUIService(
        IAgentOrchestrator orchestrator,
        IConversationManager conversationManager)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));

        // Subscribe to conversation changes
        _conversationManager.MessageAdded += OnMessageAdded;
        _conversationManager.MessageUpdated += OnMessageUpdated;
    }

    public async Task SendMessageAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty", nameof(content));

        SetProcessing(true);

        try
        {
            // Send to agent orchestrator
            await _orchestrator.SendUserMessageAsync(content);
        }
        catch
        {
            SetProcessing(false);
            throw;
        }

        // Processing state will be cleared when assistant responds
    }

    public async Task ClearConversationAsync()
    {
        _conversationManager.ClearConversation();
        _messages.Clear();
        OnMessagesChanged();
        await Task.CompletedTask;
    }

    private void OnMessageAdded(object? sender, IMessage message)
    {
        var uiMessage = UIMessage.FromDomainMessage(message);
        _messages.Add(uiMessage);
        OnMessagesChanged();

        // If this is an assistant message, we're done processing
        if (message.Role == MessageRole.Assistant)
        {
            SetProcessing(false);
        }
    }

    private void OnMessageUpdated(object? sender, IMessage message)
    {
        // Find and update existing message
        var existingIndex = _messages.FindIndex(m => m.Id == message.Id);
        if (existingIndex >= 0)
        {
            _messages[existingIndex] = UIMessage.FromDomainMessage(message);
            OnMessagesChanged();
        }
    }

    private void SetProcessing(bool isProcessing)
    {
        if (_isProcessing != isProcessing)
        {
            _isProcessing = isProcessing;
            ProcessingStateChanged?.Invoke(this, _isProcessing);
        }
    }

    private void OnMessagesChanged()
    {
        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

**Tests** (Lean - Service Behavior):
- ✅ Constructor throws ArgumentNullException for null orchestrator
- ✅ Constructor throws ArgumentNullException for null conversationManager
- ✅ SendMessageAsync() throws ArgumentException for null/empty content
- ✅ SendMessageAsync() sets IsProcessing to true
- ✅ SendMessageAsync() calls orchestrator.SendUserMessageAsync()
- ✅ OnMessageAdded() adds message to Messages list
- ✅ OnMessageAdded() raises MessagesChanged event
- ✅ OnMessageAdded() for assistant message sets IsProcessing to false
- ✅ OnMessageUpdated() updates existing message
- ✅ OnMessageUpdated() raises MessagesChanged event
- ✅ ClearConversationAsync() clears messages and raises MessagesChanged
- ✅ SetProcessing() raises ProcessingStateChanged event when state changes
- ❌ NO tests for: event subscriptions in constructor (integration concern)

**Test File**: `TransparentAiAgentUI_Tests/Services/ConversationUIServiceTests.cs`

---

### 4. Blazor Components

#### 4.1 MessageDisplay Component

**Purpose**: Display a single message with context status indicator

**File**: `TransparentAiAgentUI/Components/Chat/MessageDisplay.razor`

```razor
@using TransparentAiAgentUI.Models

<div class="message @Message.CssClass" title="@Message.ContextStatusTooltip">
    <div class="message-header">
        <span class="message-role">@Message.Role</span>
        <span class="message-context-status">@Message.ContextStatusIcon</span>
        <span class="message-timestamp">@Message.Timestamp.ToString("HH:mm:ss")</span>
    </div>
    <div class="message-content">
        @Message.Content
    </div>
</div>

@code {
    [Parameter]
    public UIMessage Message { get; set; } = null!;

    protected override void OnParametersSet()
    {
        if (Message == null)
            throw new ArgumentNullException(nameof(Message));
    }
}
```

**CSS File**: `TransparentAiAgentUI/Components/Chat/MessageDisplay.razor.css`

```css
.message {
    margin: 10px 0;
    padding: 12px;
    border-radius: 8px;
    border-left: 4px solid;
}

.message-user {
    background-color: #e3f2fd;
    border-left-color: #2196f3;
}

.message-assistant {
    background-color: #f3e5f5;
    border-left-color: #9c27b0;
}

.message-system {
    background-color: #fff3e0;
    border-left-color: #ff9800;
}

.message-tool {
    background-color: #e0f2f1;
    border-left-color: #009688;
}

.message-header {
    display: flex;
    gap: 10px;
    font-size: 0.85rem;
    color: #666;
    margin-bottom: 6px;
}

.message-role {
    font-weight: 600;
    text-transform: capitalize;
}

.message-content {
    white-space: pre-wrap;
    word-wrap: break-word;
}
```

**Tests** (Lean - Component Behavior):
- ✅ OnParametersSet() throws ArgumentNullException when Message is null
- ✅ Component renders message content
- ✅ Component renders correct CSS class based on role
- ✅ Component renders context status icon
- ❌ NO tests for: Razor rendering (if compiles, it works), CSS styling

**Test File**: `TransparentAiAgentUI_Tests/Components/Chat/MessageDisplayTests.cs`

---

#### 4.2 MessageList Component

**Purpose**: Display list of all messages with auto-scroll

**File**: `TransparentAiAgentUI/Components/Chat/MessageList.razor`

```razor
@using TransparentAiAgentUI.Models
@inject IJSRuntime JS

<div class="message-list" @ref="messageListRef">
    @if (Messages == null || !Messages.Any())
    {
        <div class="empty-state">
            <p>No messages yet. Start a conversation!</p>
        </div>
    }
    else
    {
        @foreach (var message in Messages)
        {
            <MessageDisplay Message="message" />
        }
    }

    @if (IsProcessing)
    {
        <div class="processing-indicator">
            <span class="spinner"></span>
            <span>Agent is thinking...</span>
        </div>
    }
</div>

@code {
    [Parameter]
    public IReadOnlyList<UIMessage>? Messages { get; set; }

    [Parameter]
    public bool IsProcessing { get; set; }

    private ElementReference messageListRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Auto-scroll to bottom when new messages arrive
        if (Messages?.Any() == true)
        {
            await JS.InvokeVoidAsync("scrollToBottom", messageListRef);
        }
    }
}
```

**CSS File**: `TransparentAiAgentUI/Components/Chat/MessageList.razor.css`

```css
.message-list {
    height: 60vh;
    overflow-y: auto;
    padding: 20px;
    background-color: #fafafa;
    border: 1px solid #ddd;
    border-radius: 8px;
}

.empty-state {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100%;
    color: #999;
}

.processing-indicator {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px;
    color: #666;
    font-style: italic;
}

.spinner {
    width: 16px;
    height: 16px;
    border: 2px solid #ddd;
    border-top-color: #2196f3;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes spin {
    to { transform: rotate(360deg); }
}
```

**JavaScript File**: `TransparentAiAgentUI/wwwroot/js/site.js`

```javascript
window.scrollToBottom = (element) => {
    element.scrollTop = element.scrollHeight;
};
```

**Tests** (Lean - Component Behavior):
- ✅ Component renders empty state when no messages
- ✅ Component renders MessageDisplay for each message
- ✅ Component renders processing indicator when IsProcessing is true
- ❌ NO tests for: auto-scroll JavaScript (browser behavior), CSS rendering

**Test File**: `TransparentAiAgentUI_Tests/Components/Chat/MessageListTests.cs`

---

#### 4.3 ChatInput Component

**Purpose**: Input field for sending messages

**File**: `TransparentAiAgentUI/Components/Chat/ChatInput.razor`

```razor
<div class="chat-input">
    <textarea
        @bind="inputText"
        @onkeydown="HandleKeyDown"
        placeholder="Type your message..."
        rows="3"
        disabled="@IsDisabled"
        class="input-field">
    </textarea>
    <button
        @onclick="HandleSendClick"
        disabled="@(IsDisabled || string.IsNullOrWhiteSpace(inputText))"
        class="send-button">
        Send
    </button>
</div>

@code {
    [Parameter]
    public EventCallback<string> OnSend { get; set; }

    [Parameter]
    public bool IsDisabled { get; set; }

    private string inputText = string.Empty;

    private async Task HandleSendClick()
    {
        if (!string.IsNullOrWhiteSpace(inputText))
        {
            var message = inputText;
            inputText = string.Empty; // Clear input
            await OnSend.InvokeAsync(message);
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // Send on Ctrl+Enter or Cmd+Enter
        if ((e.CtrlKey || e.MetaKey) && e.Key == "Enter")
        {
            await HandleSendClick();
        }
    }
}
```

**CSS File**: `TransparentAiAgentUI/Components/Chat/ChatInput.razor.css`

```css
.chat-input {
    display: flex;
    gap: 10px;
    padding: 20px;
    background-color: #fff;
    border: 1px solid #ddd;
    border-radius: 8px;
    margin-top: 20px;
}

.input-field {
    flex: 1;
    padding: 12px;
    border: 1px solid #ccc;
    border-radius: 4px;
    font-family: inherit;
    font-size: 1rem;
    resize: vertical;
}

.input-field:disabled {
    background-color: #f5f5f5;
    cursor: not-allowed;
}

.send-button {
    padding: 12px 24px;
    background-color: #2196f3;
    color: white;
    border: none;
    border-radius: 4px;
    font-weight: 600;
    cursor: pointer;
    transition: background-color 0.2s;
}

.send-button:hover:not(:disabled) {
    background-color: #1976d2;
}

.send-button:disabled {
    background-color: #ccc;
    cursor: not-allowed;
}
```

**Tests** (Lean - Component Behavior):
- ✅ HandleSendClick() invokes OnSend with current input text
- ✅ HandleSendClick() clears input after sending
- ✅ HandleSendClick() does nothing if input is empty/whitespace
- ✅ HandleKeyDown() sends on Ctrl+Enter
- ✅ HandleKeyDown() sends on Cmd+Enter (Mac)
- ✅ HandleKeyDown() does nothing for other keys
- ❌ NO tests for: disabled state (Blazor binding), CSS styling

**Test File**: `TransparentAiAgentUI_Tests/Components/Chat/ChatInputTests.cs`

---

#### 4.4 Home Page Component

**Purpose**: Main chat page that combines all chat components

**File**: `TransparentAiAgentUI/Components/Pages/Home.razor`

```razor
@page "/"
@using TransparentAiAgentUI.Services
@inject IConversationUIService ConversationService
@implements IDisposable

<PageTitle>Transparent AI Agent</PageTitle>

<div class="chat-container">
    <div class="chat-header">
        <h1>Transparent AI Agent</h1>
        <button @onclick="HandleClearClick" class="clear-button">Clear Conversation</button>
    </div>

    <MessageList Messages="ConversationService.Messages" IsProcessing="ConversationService.IsProcessing" />

    <ChatInput OnSend="HandleSendMessage" IsDisabled="ConversationService.IsProcessing" />

    @if (!string.IsNullOrEmpty(errorMessage))
    {
        <div class="error-message">
            @errorMessage
        </div>
    }
</div>

@code {
    private string? errorMessage;

    protected override void OnInitialized()
    {
        // Subscribe to UI service events
        ConversationService.MessagesChanged += OnMessagesChanged;
        ConversationService.ProcessingStateChanged += OnProcessingStateChanged;
    }

    private async Task HandleSendMessage(string message)
    {
        errorMessage = null;

        try
        {
            await ConversationService.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error: {ex.Message}";
        }
    }

    private async Task HandleClearClick()
    {
        await ConversationService.ClearConversationAsync();
        errorMessage = null;
    }

    private void OnMessagesChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnProcessingStateChanged(object? sender, bool isProcessing)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ConversationService.MessagesChanged -= OnMessagesChanged;
        ConversationService.ProcessingStateChanged -= OnProcessingStateChanged;
    }
}
```

**CSS File**: `TransparentAiAgentUI/Components/Pages/Home.razor.css`

```css
.chat-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
}

.chat-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
}

.chat-header h1 {
    margin: 0;
    color: #333;
}

.clear-button {
    padding: 8px 16px;
    background-color: #f44336;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 600;
}

.clear-button:hover {
    background-color: #d32f2f;
}

.error-message {
    margin-top: 20px;
    padding: 12px;
    background-color: #ffebee;
    color: #c62828;
    border-left: 4px solid #f44336;
    border-radius: 4px;
}
```

**Tests** (Lean - Component Behavior):
- ✅ OnInitialized() subscribes to ConversationService events
- ✅ HandleSendMessage() calls ConversationService.SendMessageAsync()
- ✅ HandleSendMessage() clears error message before sending
- ✅ HandleSendMessage() sets error message on exception
- ✅ HandleClearClick() calls ConversationService.ClearConversationAsync()
- ✅ HandleClearClick() clears error message
- ✅ Dispose() unsubscribes from events
- ✅ OnMessagesChanged() triggers StateHasChanged
- ✅ OnProcessingStateChanged() triggers StateHasChanged
- ❌ NO tests for: Razor rendering, CSS styling, component composition

**Test File**: `TransparentAiAgentUI_Tests/Components/Pages/HomeTests.cs`

---

### 5. Layout Components

#### 5.1 MainLayout Component

**File**: `TransparentAiAgentUI/Components/Layout/MainLayout.razor`

```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <span class="app-title">Transparent AI Agent</span>
        </div>

        <article class="content px-4">
            @Body
        </article>
    </main>
</div>

<div id="blazor-error-ui">
    An unhandled error has occurred.
    <a href="" class="reload">Reload</a>
    <a class="dismiss">🗙</a>
</div>
```

**Tests**: ❌ NO TESTS (standard Blazor layout, no logic)

---

#### 5.2 NavMenu Component

**File**: `TransparentAiAgentUI/Components/Layout/NavMenu.razor`

```razor
<div class="nav-menu">
    <nav class="flex-column">
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="bi bi-house-door-fill" aria-hidden="true"></span> Chat
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="config">
                <span class="bi bi-gear-fill" aria-hidden="true"></span> Configuration
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="transparency">
                <span class="bi bi-eye-fill" aria-hidden="true"></span> Transparency
            </NavLink>
        </div>
    </nav>
</div>
```

**Note**: Configuration and Transparency pages will be implemented in Phase 6 & 7.

**Tests**: ❌ NO TESTS (standard navigation, no logic)

---

### 6. Testing Setup

#### 6.1 Create Test Project

```bash
# Create test project with bUnit
dotnet new mstest -o TransparentAiAgentUI_Tests -f net8.0

# Add bUnit package for Blazor component testing
dotnet add TransparentAiAgentUI_Tests/TransparentAiAgentUI_Tests.csproj package bunit
dotnet add TransparentAiAgentUI_Tests/TransparentAiAgentUI_Tests.csproj package Moq

# Add reference to UI project
dotnet add TransparentAiAgentUI_Tests/TransparentAiAgentUI_Tests.csproj reference TransparentAiAgentUI/TransparentAiAgentUI.csproj

# Add reference to Core project (for domain models)
dotnet add TransparentAiAgentUI_Tests/TransparentAiAgentUI_Tests.csproj reference TransparentAiAgentCore/TransparentAiAgentCore.csproj

# Add to solution
dotnet sln add TransparentAiAgentUI_Tests/TransparentAiAgentUI_Tests.csproj
```

**Test Project Structure:**
```
TransparentAiAgentUI_Tests/
├── Models/
│   └── UIMessageTests.cs
├── Services/
│   └── ConversationUIServiceTests.cs
├── Components/
│   ├── Chat/
│   │   ├── MessageDisplayTests.cs
│   │   ├── MessageListTests.cs
│   │   └── ChatInputTests.cs
│   └── Pages/
│       └── HomeTests.cs
└── TestHelpers/
    └── MockServices.cs
```

---

#### 6.2 Example Test with bUnit

**File**: `TransparentAiAgentUI_Tests/Components/Chat/MessageDisplayTests.cs`

```csharp
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentUI.Components.Chat;
using TransparentAiAgentUI.Models;

namespace TransparentAiAgentUI_Tests.Components.Chat;

[TestClass]
public class MessageDisplayTests : TestContext
{
    [TestMethod]
    public void MessageDisplay_OnParametersSet_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            var cut = RenderComponent<MessageDisplay>();
        });
    }

    [TestMethod]
    public void MessageDisplay_RendersMessageContent()
    {
        // Arrange
        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.User,
            Content = "Hello, world!",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters =>
            parameters.Add(p => p.Message, message));

        // Assert
        var contentDiv = cut.Find(".message-content");
        Assert.AreEqual("Hello, world!", contentDiv.TextContent.Trim());
    }

    [TestMethod]
    public void MessageDisplay_AppliesCorrectCssClass_ForUserMessage()
    {
        // Arrange
        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.User,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters =>
            parameters.Add(p => p.Message, message));

        // Assert
        var messageDiv = cut.Find(".message");
        Assert.IsTrue(messageDiv.ClassList.Contains("message-user"));
    }

    [TestMethod]
    public void MessageDisplay_RendersContextStatusIcon()
    {
        // Arrange
        var message = new UIMessage
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.User,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var cut = RenderComponent<MessageDisplay>(parameters =>
            parameters.Add(p => p.Message, message));

        // Assert
        var statusSpan = cut.Find(".message-context-status");
        Assert.AreEqual("✅", statusSpan.TextContent);
    }
}
```

---

## Implementation Order (TDD)

### Order of Implementation

1. **Project Setup** (No tests)
   - Create Blazor Server project
   - Configure dependencies
   - Set up folder structure

2. **View Models** (Unit tests)
   - UIMessage class with transformation methods

3. **UI Services** (Unit tests with mocks)
   - IConversationUIService interface
   - ConversationUIService implementation

4. **Blazor Components** (bUnit tests)
   - MessageDisplay component
   - ChatInput component
   - MessageList component
   - Home page component

5. **Layout Components** (No tests - standard Blazor)
   - MainLayout
   - NavMenu

6. **Integration Testing** (Manual for Phase 4)
   - Run application
   - Test end-to-end flow
   - Verify real-time updates

---

## TDD Workflow for Blazor Components

### Using bUnit for Component Testing

**Step 1: RED (Write Failing Test)**

```csharp
[TestMethod]
public void ChatInput_HandleSendClick_InvokesOnSendCallback()
{
    // Arrange
    var wasCalled = false;
    string? receivedMessage = null;

    var cut = RenderComponent<ChatInput>(parameters =>
        parameters.Add(p => p.OnSend, EventCallback.Factory.Create<string>(
            this, (msg) => { wasCalled = true; receivedMessage = msg; })));

    // Set input text
    var textarea = cut.Find("textarea");
    textarea.Change("Hello");

    // Act
    var button = cut.Find(".send-button");
    button.Click();

    // Assert
    Assert.IsTrue(wasCalled);
    Assert.AreEqual("Hello", receivedMessage);
}
```

**Step 2: GREEN (Implement Component)**

Implement `ChatInput.razor` with `HandleSendClick` method.

**Step 3: REFACTOR**

Improve code, ensure tests still pass.

---

## Project Structure

```
TransparentAiAgent/
├── TransparentAiAgentCore/          # Phase 1-3
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── TransparentAiAgentCore_Tests/    # Phase 1-3 tests
├── TransparentAiAgentUI/            # ⭐ NEW - Phase 4
│   ├── Components/
│   │   ├── Chat/
│   │   │   ├── MessageDisplay.razor
│   │   │   ├── MessageList.razor
│   │   │   └── ChatInput.razor
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   └── NavMenu.razor
│   │   └── Pages/
│   │       └── Home.razor
│   ├── Services/
│   │   ├── IConversationUIService.cs
│   │   └── ConversationUIService.cs
│   ├── Models/
│   │   └── UIMessage.cs
│   ├── wwwroot/
│   │   ├── js/site.js
│   │   └── css/site.css
│   ├── Program.cs
│   ├── App.razor
│   └── appsettings.json
└── TransparentAiAgentUI_Tests/      # ⭐ NEW - Phase 4 tests
    ├── Models/
    ├── Services/
    ├── Components/
    └── TestHelpers/
```

---

## Deliverables

At the end of Phase 4, we will have:

✅ **Functional Web UI**:
- Blazor Server application running on localhost:5000
- Chat interface with message input and display
- Visual context status indicators (✅ in context, ⚠️ truncated)
- Real-time message streaming via SignalR
- Processing state indicators

✅ **UI Components**:
- MessageDisplay (individual message with context indicator)
- MessageList (scrollable message history)
- ChatInput (message input with Ctrl+Enter support)
- Home page (main chat interface)
- Layout components

✅ **State Management**:
- ConversationUIService (UI state and backend coordination)
- Event-driven updates (MessagesChanged, ProcessingStateChanged)
- Proper error handling and display

✅ **Test Coverage**:
- Unit tests for UIMessage transformations
- Unit tests for ConversationUIService behavior
- bUnit tests for component behavior
- ~80%+ code coverage for meaningful behavior

✅ **End-to-End Testing**:
- Can send user messages
- Can receive assistant responses
- Can see context status changes
- Can clear conversation
- Real-time UI updates work

✅ **Foundation for Phase 5**:
- UI ready for tool call display (Phase 5)
- UI ready for enhanced transparency (Phase 6)
- UI ready for configuration interface (Phase 7)

---

## Dependencies

### Required from Previous Phases:

**Phase 1** (Foundation):
- ✅ Domain models (IMessage, UserMessage, AssistantMessage, etc.)
- ✅ Configuration models
- ✅ Transparency system

**Phase 2** (LLM Integration):
- ✅ ILLMProvider interface
- ✅ LLM provider implementation (Azure OpenAI or mock)

**Phase 3** (Agent Core):
- ✅ IConversationManager interface and implementation
- ✅ IAgentOrchestrator interface and implementation
- ✅ Message pipeline

**Note**: If Phases 2 & 3 are not complete, you can mock these interfaces for Phase 4 development and testing.

---

## Testing Strategy

### Unit Tests (Services & Models)
- Standard MSTest with Moq for mocking
- Test service behavior in isolation
- Mock IAgentOrchestrator and IConversationManager

### Component Tests (Blazor Components)
- Use bUnit for component testing
- Test component behavior (event handlers, state changes)
- Test parameter validation
- Mock EventCallbacks and services
- **Do NOT test**: Razor rendering details, CSS styling

### Integration Tests (Manual for Phase 4)
- Run full application
- Test end-to-end user flows:
  - Send message → Agent responds → Message appears
  - Context truncation → Warning icon appears
  - Clear conversation → Messages disappear
- Verify SignalR real-time updates

### Future (Phase 6+)
- Automated integration tests with Playwright or Selenium
- End-to-end tests for complete workflows

---

## Next Phase

**Phase 5: MCP Integration** will build on this UI to:
- Display tool calls and results in message list
- Show tool execution in transparency view
- Update UI components to handle ToolCallMessage and ToolResultMessage
- Add MCP server management UI (Phase 7)

---

**Status**: Ready for Implementation
**Estimated Time**: 2-3 days
**Approach**: Lean TDD - Tests for Meaningful Behavior!

---

## Quick Start Checklist

Before starting Phase 4:

### Prerequisites
- [ ] Phase 1 complete (Domain models, Configuration, Transparency)
- [ ] Phase 2 complete (LLM Integration) OR mock ILLMProvider
- [ ] Phase 3 complete (Agent Core) OR mock IAgentOrchestrator and IConversationManager
- [ ] .NET 8.0 SDK installed
- [ ] Solution builds successfully

### Phase 4 First Steps
1. [ ] Create TransparentAiAgentUI Blazor Server project
2. [ ] Create TransparentAiAgentUI_Tests test project with bUnit
3. [ ] Implement UIMessage with tests (TDD)
4. [ ] Implement ConversationUIService with tests (TDD)
5. [ ] Implement MessageDisplay component with tests (TDD)
6. [ ] Implement ChatInput component with tests (TDD)
7. [ ] Implement MessageList component with tests (TDD)
8. [ ] Implement Home page with tests (TDD)
9. [ ] Add layout components (MainLayout, NavMenu)
10. [ ] Configure dependency injection in Program.cs
11. [ ] Run application and test end-to-end
12. [ ] Fix any issues, refactor, improve

**Let's build a beautiful, transparent UI! 🚀**
