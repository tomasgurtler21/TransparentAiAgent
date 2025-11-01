# Phase 6: Enhanced UI & Transparency - Detailed Implementation Plan

**Version:** 1.0
**Date:** 2025-11-01
**Status:** Planning
**Estimated Duration:** 20-30 hours

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Architecture Decisions](#architecture-decisions)
3. [Component 1: Streaming LLM Responses](#component-1-streaming-llm-responses)
4. [Component 2: Transparency Viewer](#component-2-transparency-viewer)
5. [Component 3: Tools Overview](#component-3-tools-overview)
6. [Component 4: Enhanced Message Formatting](#component-4-enhanced-message-formatting)
7. [Testing Strategy](#testing-strategy)
8. [Implementation Timeline](#implementation-timeline)
9. [Success Criteria](#success-criteria)
10. [Future Enhancements (Phase 7+)](#future-enhancements-phase-7)
11. [Risk Assessment](#risk-assessment)

---

## Executive Summary

### Phase 6 Goals

Phase 6 focuses on **Enhanced UI & Transparency** to provide users with real-time visibility into agent operations, tool usage, and system state. Building upon Phase 5's successful tool integration (332 passing tests), Phase 6 will deliver a rich, interactive user experience.

### Key Objectives

1. **Real-time Streaming**: Display LLM responses token-by-token as they arrive
2. **Transparency Viewer**: Provide comprehensive visibility into all system events
3. **Tools Overview**: Show available tools, their schemas, and usage statistics
4. **Enhanced Formatting**: Improve visual presentation of tool calls, results, and context

### Scope

**In Scope:**
- Streaming LLM responses via existing SignalR infrastructure
- New TransparencyViewer component for event monitoring
- New ToolsOverview component for tool inspection
- Enhanced JSON formatting for tool calls/results
- Real-time UI updates for all agent operations
- Component-level unit tests (bUnit)

**Out of Scope (Deferred to Phase 7+):**
- UI element toggles (checkboxes to show/hide message types)
- LLM-controlled UI elements
- Server-Sent Events (SSE) endpoints (using SignalR instead)
- Advanced filtering/search in chat history
- User preference persistence

### Deliverables

1. ✅ Streaming LLM responses functional in UI
2. ✅ TransparencyViewer component operational
3. ✅ ToolsOverview component operational
4. ✅ Enhanced tool call/result formatting
5. ✅ bUnit tests for new components
6. ✅ Updated documentation
7. ✅ Manual testing checklist completed

---

## Architecture Decisions

### Decision 1: SignalR vs SSE

**Decision:** Use existing SignalR infrastructure for all real-time updates (LLM streaming, transparency events, tool updates)

**Rationale:**
- Blazor Server uses SignalR by default for all server-to-client communication
- SignalR connection is already established and maintained
- No additional infrastructure or complexity required
- Blazor's reactive model (`StateHasChanged()`) integrates seamlessly with SignalR
- SSE would require JavaScript interop and separate endpoint management

**Original Roadmap Adjustment:**
The original roadmap mentioned "SSE endpoint for Transparency events" and "SSE client in Blazor." This is revised to leverage SignalR for the following reasons:
- Blazor Server's built-in SignalR provides equivalent functionality
- Simpler implementation with less code
- Better integration with Blazor's component lifecycle
- Maintains consistency with existing UI update patterns

**Implementation Impact:**
- No new API endpoints required
- Existing `ConversationUIService` event model continues to work
- Components subscribe to service events and call `StateHasChanged()`

### Decision 2: Event-Driven UI Architecture

**Decision:** Continue using event-driven architecture with `ConversationUIService` as central hub

**Current Pattern:**
```csharp
// Service raises events
public event EventHandler? MessagesChanged;
public event EventHandler<bool>? ProcessingStateChanged;

// Components subscribe
@implements IDisposable

protected override void OnInitialized()
{
    ConversationUIService.MessagesChanged += OnMessagesChanged;
}

private async void OnMessagesChanged(object? sender, EventArgs e)
{
    await InvokeAsync(StateHasChanged);
}
```

**Extension for Phase 6:**
Add new events for transparency and tool updates:
```csharp
public event EventHandler<TransparencyEvent>? TransparencyEventOccurred;
public event EventHandler? ToolsChanged;
```

### Decision 3: Component Structure

**Decision:** Create independent, reusable components following Blazor best practices

**Component Hierarchy:**
```
Home.razor (Main Page)
├── MessageList.razor (Chat History)
│   └── MessageDisplay.razor (Individual Messages)
├── ChatInput.razor (User Input)
├── TransparencyViewer.razor (NEW - Event Monitor)
│   └── TransparencyEventDisplay.razor (NEW - Event Details)
└── ToolsOverview.razor (NEW - Tool Inspector)
    └── ToolCard.razor (NEW - Tool Details)
```

### Decision 4: Streaming Strategy

**Decision:** Use incremental message updates with throttled UI refreshes

**Approach:**
1. **Backend**: Stream tokens from Azure OpenAI SDK via `IAsyncEnumerable<StreamingChatCompletionUpdate>`
2. **Service Layer**: Accumulate tokens and update message incrementally
3. **UI Throttling**: Limit `StateHasChanged()` calls to ~50ms intervals to prevent UI thrashing
4. **Final Update**: Ensure complete message is displayed after streaming completes

**Pseudo-code:**
```csharp
// In AgentOrchestrator.cs
var lastUpdate = DateTime.UtcNow;
await foreach (var update in streamingResponse)
{
    messageBuilder.Append(update.ContentUpdate);

    // Throttle UI updates (max 20 updates/second)
    if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds > 50)
    {
        _conversationUIService.UpdateStreamingMessage(messageBuilder.ToString());
        lastUpdate = DateTime.UtcNow;
    }
}
// Final update
_conversationUIService.UpdateStreamingMessage(messageBuilder.ToString(), isComplete: true);
```

### Decision 5: Markdown Buffering Strategy

**Decision:** Implement semantic-boundary-aware buffering for markdown content and tool calls during streaming

**Problem:**
Simple time-based throttling can cause visual glitches when streaming partial markdown constructs:
- Partial tables (e.g., `| Name | Ag`) render incorrectly
- Incomplete code blocks (e.g., ` ``` py`) show malformed syntax
- Half-formed headings (e.g., `### Hea`) flicker between text and heading
- Tool calls appear as broken XML/JSON before complete

**Solution:**
Implement a `MarkdownStreamingBuffer` that:
1. Detects markdown construct boundaries (tables, code blocks, headings, lists, formatting)
2. Buffers incomplete constructs until they're complete
3. Detects tool call boundaries and buffers until complete
4. Only emits complete, renderable markdown units
5. Provides fallback time-based flush (1 second max) to prevent indefinite buffering

**Buffering Rules:**

| Markdown Construct | Buffer Until | Example |
|-------------------|--------------|---------|
| Code blocks | Closing ` ``` ` fence | ` ```python\ncode\n``` ` |
| Tables | Complete row (newline after `\|`) | `\| Col1 \| Col2 \|\n` |
| Headings | End of line | `### Heading\n` |
| Lists | End of line | `- Item\n` |
| Tool calls | Closing tag/brace | `<tool_call>...</tool_call>` |
| Bold/Italic | Closing markers | `**bold**` or `*italic*` |

**Implementation Approach:**

```csharp
public class MarkdownStreamingBuffer
{
    private readonly StringBuilder _buffer = new();
    private readonly StringBuilder _renderableBuffer = new();
    private MarkdownState _state = MarkdownState.Normal;
    private DateTime _lastFlushTime = DateTime.UtcNow;

    public string? AppendAndGetRenderable(string token)
    {
        _buffer.Append(token);

        // Force flush if buffering too long (prevent hanging)
        if ((DateTime.UtcNow - _lastFlushTime).TotalMilliseconds > 1000)
        {
            return ForceFlush();
        }

        // State machine for markdown construct detection
        UpdateState();

        // Don't emit if in middle of construct
        if (_state != MarkdownState.Normal)
        {
            return null;
        }

        // Extract and emit complete units
        var renderable = ExtractCompleteUnits();
        if (!string.IsNullOrEmpty(renderable))
        {
            _lastFlushTime = DateTime.UtcNow;
        }

        return renderable;
    }

    private void UpdateState()
    {
        var content = _buffer.ToString();

        // Detect code blocks
        if (IsInCodeBlock(content))
        {
            _state = MarkdownState.InCodeBlock;
            return;
        }

        // Detect tables
        if (IsInTableRow(content))
        {
            _state = MarkdownState.InTableRow;
            return;
        }

        // Detect tool calls
        if (IsInToolCall(content))
        {
            _state = MarkdownState.InToolCall;
            return;
        }

        // Detect incomplete heading
        if (IsInHeading(content))
        {
            _state = MarkdownState.InHeading;
            return;
        }

        _state = MarkdownState.Normal;
    }

    private string? ExtractCompleteUnits()
    {
        // Logic to extract complete markdown units from buffer
        // Move extracted content to renderable buffer
        // Return renderable content
    }
}

public enum MarkdownState
{
    Normal,
    InCodeBlock,
    InTableRow,
    InHeading,
    InToolCall
}
```

**Rationale:**
- **User Experience**: Prevents jarring visual glitches during streaming
- **Progressive Enhancement**: Content appears smoothly without formatting jumps
- **Safety**: Timeout prevents indefinite buffering on malformed markdown
- **Tool Call UX**: Tool calls only appear when complete, avoiding confusion
- **Performance**: Minimal overhead (regex + state machine) compared to time-based approach

**Trade-offs:**
- **Complexity**: More complex than simple time-based throttling
- **Latency**: Slight delay showing incomplete constructs (max 1 second)
- **Edge Cases**: May not detect all markdown variations (rare formats, nested constructs)
- **Mitigation**: Fallback flush ensures content always appears eventually

**Alternative Considered (Rejected):**
- *Post-completion markdown rendering*: Stream raw text, render markdown only after complete
  - **Rejected because**: User wants progressive markdown rendering during streaming
- *Client-side buffering*: Move buffering logic to Blazor component
  - **Rejected because**: Server-side buffering provides better control and testability

---

## Component 1: Streaming LLM Responses

### Overview

Enable real-time token-by-token display of LLM responses as they arrive from Azure OpenAI, providing immediate feedback to users and improving perceived responsiveness.

**Note**: This component focuses on the infrastructure for streaming. For markdown-aware buffering logic that prevents rendering glitches, see **Component 1.5: Markdown-Aware Streaming Buffer**.

### Current State Analysis

**Current Behavior:**
From code inspection of `AgentOrchestrator.cs` and related files, the system currently:
1. Calls Azure OpenAI SDK with streaming enabled
2. Receives `IAsyncEnumerable<StreamingChatCompletionUpdate>`
3. Likely buffers the entire response before displaying (needs verification)
4. Updates UI only once response is complete

**File Locations:**
- `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs` - Agent logic
- `TransparentAiAgentCore/Infrastructure/LLM/AzureOpenAiChatService.cs` - OpenAI integration
- `TransparentAiAgentGui/Services/ConversationUIService.cs` - UI service layer
- `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor` - Message rendering

### Detailed Implementation Steps

#### Step 1: Modify AzureOpenAiChatService (2-3 hours)

**File:** `TransparentAiAgentCore/Infrastructure/LLM/AzureOpenAiChatService.cs`

**Changes Required:**

1. Add streaming callback interface:
```csharp
public interface IStreamingCallback
{
    Task OnTokenReceived(string token);
    Task OnStreamComplete();
    Task OnStreamError(Exception ex);
}
```

2. Modify `GetChatCompletionAsync` to accept optional streaming callback:
```csharp
public async Task<ChatCompletion> GetChatCompletionAsync(
    List<ChatMessage> messages,
    IStreamingCallback? streamingCallback = null,
    CancellationToken cancellationToken = default)
{
    var options = new ChatCompletionOptions
    {
        // ... existing options
    };

    var response = _client.GetChatCompletionAsync(messages, options, cancellationToken);

    if (streamingCallback != null)
    {
        return await StreamResponseAsync(response, streamingCallback, cancellationToken);
    }
    else
    {
        return await response; // Non-streaming mode
    }
}

private async Task<ChatCompletion> StreamResponseAsync(
    Task<ClientResult<ChatCompletion>> responseTask,
    IStreamingCallback callback,
    CancellationToken cancellationToken)
{
    // Check if response supports streaming
    var clientResult = await responseTask;

    // If streaming available
    if (clientResult is StreamingChatCompletion streamingResponse)
    {
        var contentBuilder = new StringBuilder();

        try
        {
            await foreach (var update in streamingResponse.WithCancellation(cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.ContentUpdate))
                {
                    contentBuilder.Append(update.ContentUpdate);
                    await callback.OnTokenReceived(update.ContentUpdate);
                }
            }

            await callback.OnStreamComplete();
        }
        catch (Exception ex)
        {
            await callback.OnStreamError(ex);
            throw;
        }

        // Return complete response
        return new ChatCompletion(contentBuilder.ToString());
    }
    else
    {
        // Fallback to non-streaming
        return clientResult.Value;
    }
}
```

**Testing:**
- Unit test for streaming callback invocation
- Unit test for error handling during streaming
- Unit test for fallback to non-streaming mode

#### Step 2: Extend ConversationUIService (1-2 hours)

**File:** `TransparentAiAgentGui/Services/ConversationUIService.cs`

**Changes Required:**

1. Add streaming message support:
```csharp
public interface IConversationUIService
{
    // Existing methods...
    event EventHandler? MessagesChanged;
    event EventHandler<bool>? ProcessingStateChanged;

    // NEW: Streaming support
    void StartStreamingMessage(string messageId);
    void UpdateStreamingMessage(string messageId, string content);
    void CompleteStreamingMessage(string messageId);
    event EventHandler<StreamingMessageUpdate>? StreamingMessageUpdated;
}

public class StreamingMessageUpdate
{
    public string MessageId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}

public class ConversationUIService : IConversationUIService
{
    // Existing fields...
    private string? _currentStreamingMessageId;
    private readonly object _streamingLock = new object();

    public event EventHandler<StreamingMessageUpdate>? StreamingMessageUpdated;

    public void StartStreamingMessage(string messageId)
    {
        lock (_streamingLock)
        {
            _currentStreamingMessageId = messageId;
        }
        OnMessagesChanged(); // Create empty message
    }

    public void UpdateStreamingMessage(string messageId, string content)
    {
        lock (_streamingLock)
        {
            if (_currentStreamingMessageId != messageId) return;
        }

        StreamingMessageUpdated?.Invoke(this, new StreamingMessageUpdate
        {
            MessageId = messageId,
            Content = content,
            IsComplete = false
        });
    }

    public void CompleteStreamingMessage(string messageId)
    {
        lock (_streamingLock)
        {
            if (_currentStreamingMessageId != messageId) return;
            _currentStreamingMessageId = null;
        }

        StreamingMessageUpdated?.Invoke(this, new StreamingMessageUpdate
        {
            MessageId = messageId,
            Content = string.Empty,
            IsComplete = true
        });

        OnMessagesChanged(); // Final update
    }
}
```

**Testing:**
- Unit test for concurrent streaming message updates
- Unit test for streaming state management
- Unit test for event invocation

#### Step 3: Implement Streaming Callback in AgentOrchestrator (2-3 hours)

**File:** `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs`

**Important**: The implementation below shows basic throttling. For the enhanced version with **semantic-boundary-aware markdown buffering**, see Component 1.5. The final implementation should use `MarkdownStreamingBuffer` instead of raw `StringBuilder` to prevent markdown rendering glitches.

**Changes Required:**

1. Create streaming callback implementation (basic version with throttling only):
```csharp
private class AgentStreamingCallback : IStreamingCallback
{
    private readonly IConversationUIService _uiService;
    private readonly string _messageId;
    private readonly StringBuilder _contentBuilder;
    private DateTime _lastUpdateTime;
    private const int ThrottleMilliseconds = 50; // Max 20 updates/sec

    public AgentStreamingCallback(IConversationUIService uiService, string messageId)
    {
        _uiService = uiService;
        _messageId = messageId;
        _contentBuilder = new StringBuilder();
        _lastUpdateTime = DateTime.UtcNow;
    }

    public Task OnTokenReceived(string token)
    {
        _contentBuilder.Append(token);

        // Throttle UI updates
        var now = DateTime.UtcNow;
        if ((now - _lastUpdateTime).TotalMilliseconds >= ThrottleMilliseconds)
        {
            _uiService.UpdateStreamingMessage(_messageId, _contentBuilder.ToString());
            _lastUpdateTime = now;
        }

        return Task.CompletedTask;
    }

    public Task OnStreamComplete()
    {
        // Send final update (no throttling)
        _uiService.UpdateStreamingMessage(_messageId, _contentBuilder.ToString());
        _uiService.CompleteStreamingMessage(_messageId);
        return Task.CompletedTask;
    }

    public Task OnStreamError(Exception ex)
    {
        _uiService.CompleteStreamingMessage(_messageId);
        // Log error via transparency service
        return Task.CompletedTask;
    }
}
```

2. Modify `ProcessUserInputAsync` to use streaming:
```csharp
public async Task ProcessUserInputAsync(string userInput, CancellationToken cancellationToken = default)
{
    // ... existing code to add user message

    // Start streaming message
    var assistantMessageId = Guid.NewGuid().ToString();
    _conversationUIService.StartStreamingMessage(assistantMessageId);

    try
    {
        // Create streaming callback
        var streamingCallback = new AgentStreamingCallback(_conversationUIService, assistantMessageId);

        // Get LLM response with streaming
        var llmResponse = await _chatService.GetChatCompletionAsync(
            messages,
            streamingCallback,
            cancellationToken);

        // ... rest of processing (tool calls, etc.)
    }
    catch (Exception ex)
    {
        _conversationUIService.CompleteStreamingMessage(assistantMessageId);
        throw;
    }
}
```

**Testing:**
- Integration test for end-to-end streaming
- Unit test for throttling logic
- Unit test for error handling during streaming

#### Step 4: Update MessageDisplay Component (1-2 hours)

**File:** `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor`

**Changes Required:**

1. Add streaming state management:
```razor
@implements IDisposable
@inject IConversationUIService ConversationUIService

<div class="message @GetMessageClass()">
    <div class="message-header">
        <!-- Existing header -->
        @if (IsStreaming)
        {
            <span class="streaming-indicator">●</span>
        }
    </div>
    <div class="message-content">
        @if (IsStreaming)
        {
            @StreamingContent
            <span class="cursor">▌</span>
        }
        else
        {
            @Message.Content
        }
    </div>
</div>

@code {
    [Parameter]
    public UIMessage Message { get; set; } = null!;

    private bool IsStreaming { get; set; }
    private string StreamingContent { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        ConversationUIService.StreamingMessageUpdated += OnStreamingUpdate;
    }

    private async void OnStreamingUpdate(object? sender, StreamingMessageUpdate update)
    {
        if (update.MessageId == Message.Id)
        {
            IsStreaming = !update.IsComplete;
            StreamingContent = update.Content;
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        ConversationUIService.StreamingMessageUpdated -= OnStreamingUpdate;
    }
}
```

2. Add CSS for streaming indicator:
```css
/* MessageDisplay.razor.css */
.streaming-indicator {
    display: inline-block;
    width: 8px;
    height: 8px;
    background-color: #28a745;
    border-radius: 50%;
    margin-left: 8px;
    animation: pulse 1.5s ease-in-out infinite;
}

@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.3; }
}

.cursor {
    display: inline-block;
    animation: blink 1s step-end infinite;
}

@keyframes blink {
    0%, 100% { opacity: 1; }
    50% { opacity: 0; }
}
```

**Testing:**
- bUnit test for streaming indicator appearance
- bUnit test for cursor animation
- Manual testing for smooth streaming display

#### Step 5: Add Transparency Events for Streaming (1 hour)

**File:** `TransparentAiAgentCore/Domain/Enums/TransparencyEventType.cs`

**Changes Required:**

Add new event types:
```csharp
public enum TransparencyEventType
{
    // Existing types...

    // NEW: Streaming events
    StreamingStarted,
    StreamingTokenReceived,
    StreamingCompleted,
    StreamingError
}
```

**File:** `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs`

Log transparency events:
```csharp
private class AgentStreamingCallback : IStreamingCallback
{
    private readonly ITransparencyService _transparencyService;

    public Task OnTokenReceived(string token)
    {
        _transparencyService.LogEvent(
            TransparencyEventType.StreamingTokenReceived,
            new { Token = token, Length = token.Length });
        // ... existing code
    }

    // Similar for OnStreamComplete, OnStreamError
}
```

### Expected Outcomes

After implementing streaming:
- ✅ Users see LLM responses appear token-by-token in real-time
- ✅ Streaming indicator shows active generation
- ✅ Blinking cursor appears during streaming
- ✅ UI updates smoothly without flickering (throttled)
- ✅ Streaming events logged to transparency service
- ✅ Error handling prevents UI from freezing on stream failures

### Files Modified Summary

| File | Type | Changes |
|------|------|---------|
| `AzureOpenAiChatService.cs` | Modified | Add streaming callback support |
| `IConversationUIService.cs` | Modified | Add streaming methods/events |
| `ConversationUIService.cs` | Modified | Implement streaming message management |
| `AgentOrchestrator.cs` | Modified | Implement streaming callback |
| `MessageDisplay.razor` | Modified | Add streaming UI with cursor |
| `MessageDisplay.razor.css` | Modified | Add streaming animations |
| `TransparencyEventType.cs` | Modified | Add streaming event types |

---

## Component 1.5: Markdown-Aware Streaming Buffer

### Overview

Implement semantic-boundary-aware buffering to prevent visual glitches when streaming markdown content. This component ensures that partial markdown constructs (tables, code blocks, headings, tool calls) are buffered until complete, providing smooth progressive rendering without formatting jumps.

### Requirements

**Functional Requirements:**
- Detect markdown construct boundaries (code blocks, tables, headings, lists, bold/italic)
- Buffer incomplete constructs until they can be safely rendered
- Detect and buffer tool calls until complete
- Provide fallback time-based flush (1 second max) to prevent indefinite buffering
- Support all common markdown syntax variations

**Non-Functional Requirements:**
- Minimal latency impact (<50ms per token)
- Handle edge cases (malformed markdown, nested constructs)
- Thread-safe for concurrent streaming operations
- Memory-efficient buffering

### Detailed Implementation Steps

#### Step 1: Create MarkdownStreamingBuffer Core (2-3 hours)

**File:** `TransparentAiAgentCore/Infrastructure/Markdown/MarkdownStreamingBuffer.cs` (new)

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace TransparentAiAgentCore.Infrastructure.Markdown;

/// <summary>
/// Buffers streaming markdown content and only emits complete, renderable units
/// to prevent visual glitches from partial markdown constructs.
/// </summary>
public class MarkdownStreamingBuffer
{
    private readonly StringBuilder _buffer = new();
    private readonly StringBuilder _emittedContent = new();
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private const int MaxBufferTimeMs = 1000;

    // Regex patterns for markdown construct detection
    private static readonly Regex CodeFencePattern = new(@"```", RegexOptions.Compiled);
    private static readonly Regex TableRowPattern = new(@"\|.*\|$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex HeadingPattern = new(@"^#{1,6}\s+[^\n]*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ToolCallOpenPattern = new(@"<tool_call[^>]*>(?![\s\S]*</tool_call>)", RegexOptions.Compiled);

    /// <summary>
    /// Appends new token to buffer and returns renderable content if available.
    /// </summary>
    /// <param name="token">Token received from LLM stream</param>
    /// <returns>Content safe to render, or null if still buffering</returns>
    public string? AppendAndGetRenderable(string token)
    {
        _buffer.Append(token);

        // Force flush if buffering too long (prevent hanging on malformed markdown)
        if ((DateTime.UtcNow - _lastFlushTime).TotalMilliseconds > MaxBufferTimeMs)
        {
            return ForceFlush();
        }

        // Check if we're in middle of markdown construct
        if (IsInIncompleteConstruct())
        {
            return null; // Keep buffering
        }

        // Extract and return complete units
        return ExtractCompleteUnits();
    }

    /// <summary>
    /// Forces flush of all buffered content (called at stream end or timeout).
    /// </summary>
    public string ForceFlush()
    {
        var content = _buffer.ToString();
        _buffer.Clear();
        _emittedContent.Append(content);
        _lastFlushTime = DateTime.UtcNow;
        return content;
    }

    /// <summary>
    /// Resets buffer state for new message.
    /// </summary>
    public void Reset()
    {
        _buffer.Clear();
        _emittedContent.Clear();
        _lastFlushTime = DateTime.UtcNow;
    }

    private bool IsInIncompleteConstruct()
    {
        var content = _buffer.ToString();

        // Check for incomplete code block
        if (IsInCodeBlock(content))
            return true;

        // Check for incomplete table row
        if (IsInIncompleteTableRow(content))
            return true;

        // Check for incomplete heading
        if (IsInIncompleteHeading(content))
            return true;

        // Check for incomplete tool call
        if (IsInToolCall(content))
            return true;

        // Check for incomplete bold/italic
        if (HasUnmatchedFormatting(content))
            return true;

        return false;
    }

    private bool IsInCodeBlock(string content)
    {
        // Count code fence markers (```)
        var matches = CodeFencePattern.Matches(content);
        // Odd number means we're inside a code block
        return matches.Count % 2 != 0;
    }

    private bool IsInIncompleteTableRow(string content)
    {
        // Check if content ends with pipe but no newline
        // This indicates we're in middle of a table row
        var lines = content.Split('\n');
        var lastLine = lines[^1];

        // If last line has pipes but doesn't end with newline, we're in a row
        return lastLine.Contains('|') && !content.EndsWith('\n');
    }

    private bool IsInIncompleteHeading(string content)
    {
        // Check if content starts with # but doesn't have newline yet
        var lines = content.Split('\n');
        var lastLine = lines[^1];

        return lastLine.StartsWith('#') && !content.EndsWith('\n');
    }

    private bool IsInToolCall(string content)
    {
        // Check for unclosed tool call tags
        return ToolCallOpenPattern.IsMatch(content);
    }

    private bool HasUnmatchedFormatting(string content)
    {
        // Check for unmatched bold markers (**)
        var boldMatches = Regex.Matches(content, @"\*\*");
        if (boldMatches.Count % 2 != 0)
            return true;

        // Check for unmatched italic markers (*)
        // More complex: need to distinguish from bold
        var allAsterisks = content.Count(c => c == '*');
        var boldAsterisks = boldMatches.Count * 2;
        var italicAsterisks = allAsterisks - boldAsterisks;

        return italicAsterisks % 2 != 0;
    }

    private string? ExtractCompleteUnits()
    {
        var buffered = _buffer.ToString();

        // Find the last complete unit boundary
        var lastSafeBoundary = FindLastSafeBoundary(buffered);

        if (lastSafeBoundary <= 0)
            return null; // No complete units yet

        // Extract content up to safe boundary
        var renderable = buffered.Substring(0, lastSafeBoundary);

        // Keep remaining in buffer
        _buffer.Clear();
        _buffer.Append(buffered.Substring(lastSafeBoundary));

        _emittedContent.Append(renderable);
        _lastFlushTime = DateTime.UtcNow;

        return renderable;
    }

    private int FindLastSafeBoundary(string content)
    {
        // Safe boundaries are:
        // 1. End of complete line (after newline)
        // 2. After complete sentence (. or ! or ?)
        // 3. After whitespace

        // Prefer newline boundaries
        var lastNewline = content.LastIndexOf('\n');
        if (lastNewline > 0)
        {
            // Check if content after newline is safe to keep buffered
            var afterNewline = content.Substring(lastNewline + 1);
            if (string.IsNullOrWhiteSpace(afterNewline) || !afterNewline.TrimStart().StartsWith('#'))
            {
                return lastNewline + 1;
            }
        }

        // Fall back to space boundary
        var lastSpace = content.LastIndexOf(' ');
        if (lastSpace > content.Length / 2) // Only if we have enough content
        {
            return lastSpace + 1;
        }

        return 0; // Keep buffering
    }

    /// <summary>
    /// Gets the complete emitted content so far.
    /// </summary>
    public string GetEmittedContent() => _emittedContent.ToString();
}
```

**Testing:**
- Unit test for code block detection (odd/even fence count)
- Unit test for table row buffering
- Unit test for heading buffering
- Unit test for tool call buffering
- Unit test for timeout flush
- Unit test for nested constructs

#### Step 2: Create Markdown Renderer Interface (30 min)

**File:** `TransparentAiAgentCore/Infrastructure/Markdown/IMarkdownRenderer.cs` (new)

```csharp
namespace TransparentAiAgentCore.Infrastructure.Markdown;

/// <summary>
/// Abstraction for markdown rendering to HTML.
/// </summary>
public interface IMarkdownRenderer
{
    /// <summary>
    /// Converts markdown text to HTML.
    /// </summary>
    /// <param name="markdown">Markdown content</param>
    /// <returns>HTML content</returns>
    string ToHtml(string markdown);

    /// <summary>
    /// Converts markdown text to HTML with syntax highlighting for code blocks.
    /// </summary>
    /// <param name="markdown">Markdown content</param>
    /// <param name="enableSyntaxHighlighting">Enable code syntax highlighting</param>
    /// <returns>HTML content</returns>
    string ToHtml(string markdown, bool enableSyntaxHighlighting);
}
```

**File:** `TransparentAiAgentCore/Infrastructure/Markdown/MarkdigRenderer.cs` (new)

```csharp
using Markdig;

namespace TransparentAiAgentCore.Infrastructure.Markdown;

/// <summary>
/// Markdown renderer using Markdig library.
/// </summary>
public class MarkdigRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;
    private readonly MarkdownPipeline _pipelineWithHighlighting;

    public MarkdigRenderer()
    {
        // Basic pipeline
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        // Pipeline with syntax highlighting
        _pipelineWithHighlighting = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseSyntaxHighlighting() // If using Markdig.SyntaxHighlighting extension
            .Build();
    }

    public string ToHtml(string markdown)
    {
        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }

    public string ToHtml(string markdown, bool enableSyntaxHighlighting)
    {
        var pipeline = enableSyntaxHighlighting ? _pipelineWithHighlighting : _pipeline;
        return Markdig.Markdown.ToHtml(markdown, pipeline);
    }
}
```

#### Step 3: Update AgentOrchestrator to Use Semantic Buffer (1-2 hours)

**File:** `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs`

Modify the streaming callback from Component 1:

```csharp
private class AgentStreamingCallback : IStreamingCallback
{
    private readonly IConversationUIService _uiService;
    private readonly string _messageId;
    private readonly MarkdownStreamingBuffer _markdownBuffer; // NEW: Use semantic buffer
    private DateTime _lastUpdateTime;
    private const int ThrottleMilliseconds = 50;

    public AgentStreamingCallback(IConversationUIService uiService, string messageId)
    {
        _uiService = uiService;
        _messageId = messageId;
        _markdownBuffer = new MarkdownStreamingBuffer(); // NEW
        _lastUpdateTime = DateTime.UtcNow;
    }

    public Task OnTokenReceived(string token)
    {
        // NEW: Use semantic buffer instead of raw StringBuilder
        var renderable = _markdownBuffer.AppendAndGetRenderable(token);

        // Only update UI if we have renderable content AND throttle interval passed
        if (renderable != null)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUpdateTime).TotalMilliseconds >= ThrottleMilliseconds)
            {
                _uiService.UpdateStreamingMessage(_messageId, _markdownBuffer.GetEmittedContent());
                _lastUpdateTime = now;
            }
        }

        return Task.CompletedTask;
    }

    public Task OnStreamComplete()
    {
        // Force flush any remaining buffered content
        _markdownBuffer.ForceFlush();

        // Send final update (no throttling)
        _uiService.UpdateStreamingMessage(_messageId, _markdownBuffer.GetEmittedContent());
        _uiService.CompleteStreamingMessage(_messageId);
        return Task.CompletedTask;
    }

    public Task OnStreamError(Exception ex)
    {
        _markdownBuffer.ForceFlush();
        _uiService.CompleteStreamingMessage(_messageId);
        // Log error via transparency service
        return Task.CompletedTask;
    }
}
```

#### Step 4: Add Configuration Support (30 min)

**File:** `TransparentAiAgentCore/Infrastructure/Markdown/MarkdownStreamingConfig.cs` (new)

```csharp
namespace TransparentAiAgentCore.Infrastructure.Markdown;

public class MarkdownStreamingConfig
{
    /// <summary>
    /// Enable semantic buffering for markdown constructs.
    /// If false, uses simple time-based throttling.
    /// </summary>
    public bool EnableSemanticBuffering { get; set; } = true;

    /// <summary>
    /// Maximum time to buffer incomplete constructs before forcing flush (milliseconds).
    /// </summary>
    public int MaxBufferTimeMs { get; set; } = 1000;

    /// <summary>
    /// Enable syntax highlighting for code blocks.
    /// </summary>
    public bool EnableSyntaxHighlighting { get; set; } = true;

    /// <summary>
    /// Buffer tool calls until complete.
    /// </summary>
    public bool BufferToolCalls { get; set; } = true;
}
```

### Expected Outcomes

After implementing Markdown-Aware Streaming Buffer:
- ✅ Code blocks don't render until closing fence received
- ✅ Table rows render completely without partial pipe characters
- ✅ Headings don't flicker between text and heading format
- ✅ Tool calls are hidden until complete
- ✅ Bold/italic markers don't show temporarily
- ✅ Fallback flush prevents hanging (max 1 second buffer time)
- ✅ Smooth progressive rendering without formatting jumps

### Files Modified Summary

| File | Type | Changes |
|------|------|---------|
| `MarkdownStreamingBuffer.cs` | New | Core semantic buffering logic |
| `IMarkdownRenderer.cs` | New | Markdown rendering interface |
| `MarkdigRenderer.cs` | New | Markdig implementation |
| `MarkdownStreamingConfig.cs` | New | Configuration model |
| `AgentOrchestrator.cs` | Modified | Use MarkdownStreamingBuffer in callback |

### Testing Strategy

**Unit Tests** (`MarkdownStreamingBufferTests.cs`):
```csharp
[TestMethod]
public void CodeBlock_BuffersUntilClosingFence()
{
    var buffer = new MarkdownStreamingBuffer();

    Assert.IsNull(buffer.AppendAndGetRenderable("```py"));
    Assert.IsNull(buffer.AppendAndGetRenderable("thon\n"));
    Assert.IsNull(buffer.AppendAndGetRenderable("print('"));
    Assert.IsNull(buffer.AppendAndGetRenderable("test')\n"));

    var result = buffer.AppendAndGetRenderable("```\n");
    Assert.IsNotNull(result);
    Assert.IsTrue(result.Contains("```python"));
}

[TestMethod]
public void Table_BuffersUntilCompleteRow()
{
    var buffer = new MarkdownStreamingBuffer();

    Assert.IsNull(buffer.AppendAndGetRenderable("| Name "));
    Assert.IsNull(buffer.AppendAndGetRenderable("| Age |"));

    var result = buffer.AppendAndGetRenderable("\n");
    Assert.IsNotNull(result);
    Assert.IsTrue(result.Contains("| Name | Age |"));
}

[TestMethod]
public void Timeout_ForcesFlusAfterMaxTime()
{
    var buffer = new MarkdownStreamingBuffer();

    buffer.AppendAndGetRenderable("```python\ncode");

    // Simulate timeout by waiting
    System.Threading.Thread.Sleep(1100);

    var result = buffer.AppendAndGetRenderable("more");
    Assert.IsNotNull(result); // Should force flush
}
```

---

## Component 2: Transparency Viewer

### Overview

Create a dedicated component for viewing all transparency events in real-time, providing full visibility into agent operations, tool calls, LLM interactions, and system state changes.

### Requirements

**Functional Requirements:**
- Display all 16+ transparency event types in real-time
- Show event timestamp, type, and details
- Support filtering by event type
- Support search by content
- Collapsible event details (JSON payload)
- Auto-scroll to latest events
- Clear/reset event list
- Export events to JSON (future enhancement)

**Non-Functional Requirements:**
- Handle 1000+ events without performance degradation
- Update UI within 100ms of event occurrence
- Responsive design (works on mobile)

### Component Architecture

**New Files to Create:**
1. `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`
2. `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor.css`
3. `TransparentAiAgentGui/Components/Transparency/TransparencyEventDisplay.razor`
4. `TransparentAiAgentGui/Components/Transparency/TransparencyEventDisplay.razor.css`

**Service Integration:**
- Subscribe to `ITransparencyService.OnEventLogged`
- Query `ITransparencyService.GetRecentEvents()` on component init

### Detailed Implementation Steps

#### Step 1: Extend TransparencyService Interface (30 min)

**File:** `TransparentAiAgentCore/Infrastructure/Transparency/ITransparencyService.cs`

**Changes Required:**

Add real-time event notification:
```csharp
public interface ITransparencyService
{
    // Existing methods...
    void LogEvent(TransparencyEventType eventType, object? data = null, string? context = null);
    IReadOnlyList<TransparencyEvent> GetRecentEvents(int count = 100);

    // NEW: Real-time event notification
    event EventHandler<TransparencyEvent>? EventLogged;
}

public class TransparencyEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TransparencyEventType EventType { get; set; }
    public string? Context { get; set; }
    public object? Data { get; set; }
    public string? Serialized { get; set; } // JSON representation
}
```

**File:** `TransparentAiAgentCore/Infrastructure/Transparency/TransparencyService.cs`

**Changes Required:**

```csharp
public class TransparencyService : ITransparencyService
{
    private readonly List<TransparencyEvent> _events = new();
    private readonly object _lock = new();

    public event EventHandler<TransparencyEvent>? EventLogged;

    public void LogEvent(TransparencyEventType eventType, object? data = null, string? context = null)
    {
        var transparencyEvent = new TransparencyEvent
        {
            EventType = eventType,
            Context = context,
            Data = data,
            Serialized = data != null ? JsonSerializer.Serialize(data) : null
        };

        lock (_lock)
        {
            _events.Add(transparencyEvent);
        }

        // Raise event (outside lock to prevent deadlocks)
        EventLogged?.Invoke(this, transparencyEvent);
    }

    public IReadOnlyList<TransparencyEvent> GetRecentEvents(int count = 100)
    {
        lock (_lock)
        {
            return _events.TakeLast(count).ToList();
        }
    }
}
```

#### Step 2: Create TransparencyEventDisplay Component (1-2 hours)

**File:** `TransparentAiAgentGui/Components/Transparency/TransparencyEventDisplay.razor`

```razor
@using TransparentAiAgentCore.Infrastructure.Transparency

<div class="transparency-event @GetEventClass()">
    <div class="event-header" @onclick="ToggleExpanded">
        <span class="event-icon">@GetEventIcon()</span>
        <span class="event-type">@Event.EventType</span>
        <span class="event-timestamp">@Event.Timestamp.ToString("HH:mm:ss.fff")</span>
        @if (!string.IsNullOrEmpty(Event.Context))
        {
            <span class="event-context">@Event.Context</span>
        }
        <span class="expand-icon">@(IsExpanded ? "▼" : "▶")</span>
    </div>

    @if (IsExpanded && Event.Serialized != null)
    {
        <div class="event-details">
            <pre><code>@Event.Serialized</code></pre>
        </div>
    }
</div>

@code {
    [Parameter]
    public TransparencyEvent Event { get; set; } = null!;

    private bool IsExpanded { get; set; }

    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    private string GetEventClass()
    {
        return Event.EventType switch
        {
            TransparencyEventType.Error => "event-error",
            TransparencyEventType.ToolCall => "event-tool",
            TransparencyEventType.ToolResult => "event-tool",
            TransparencyEventType.AssistantResponse => "event-assistant",
            TransparencyEventType.UserInput => "event-user",
            _ => "event-system"
        };
    }

    private string GetEventIcon()
    {
        return Event.EventType switch
        {
            TransparencyEventType.Error => "❌",
            TransparencyEventType.ToolCall => "🔧",
            TransparencyEventType.ToolResult => "✅",
            TransparencyEventType.AssistantResponse => "🤖",
            TransparencyEventType.UserInput => "👤",
            TransparencyEventType.StreamingStarted => "▶",
            TransparencyEventType.StreamingCompleted => "⏹",
            TransparencyEventType.MCPServerConnected => "🔌",
            _ => "ℹ️"
        };
    }
}
```

**File:** `TransparentAiAgentGui/Components/Transparency/TransparencyEventDisplay.razor.css`

```css
.transparency-event {
    border-left: 4px solid #6c757d;
    padding: 8px 12px;
    margin-bottom: 8px;
    background-color: #f8f9fa;
    border-radius: 4px;
    transition: background-color 0.2s;
}

.transparency-event:hover {
    background-color: #e9ecef;
}

.event-header {
    display: flex;
    align-items: center;
    gap: 12px;
    cursor: pointer;
    user-select: none;
}

.event-icon {
    font-size: 18px;
    width: 24px;
    text-align: center;
}

.event-type {
    font-weight: 600;
    flex: 1;
}

.event-timestamp {
    font-family: 'Courier New', monospace;
    font-size: 12px;
    color: #6c757d;
}

.event-context {
    font-size: 12px;
    color: #495057;
    font-style: italic;
}

.expand-icon {
    color: #6c757d;
    font-size: 12px;
}

.event-details {
    margin-top: 12px;
    padding: 12px;
    background-color: #ffffff;
    border-radius: 4px;
    border: 1px solid #dee2e6;
}

.event-details pre {
    margin: 0;
    font-size: 12px;
    white-space: pre-wrap;
    word-wrap: break-word;
}

/* Event type colors */
.event-error {
    border-left-color: #dc3545;
}

.event-tool {
    border-left-color: #007bff;
}

.event-assistant {
    border-left-color: #28a745;
}

.event-user {
    border-left-color: #17a2b8;
}

.event-system {
    border-left-color: #6c757d;
}
```

#### Step 3: Create TransparencyViewer Component (2-3 hours)

**File:** `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor`

```razor
@using TransparentAiAgentCore.Infrastructure.Transparency
@using TransparentAiAgentCore.Domain.Enums
@implements IDisposable
@inject ITransparencyService TransparencyService

<div class="transparency-viewer">
    <div class="viewer-header">
        <h3>Transparency Events</h3>
        <div class="viewer-controls">
            <input type="text"
                   class="search-input"
                   placeholder="Search events..."
                   @bind="SearchQuery"
                   @bind:event="oninput" />

            <select class="filter-select" @bind="SelectedEventType">
                <option value="">All Events</option>
                @foreach (var eventType in Enum.GetValues<TransparencyEventType>())
                {
                    <option value="@eventType">@eventType</option>
                }
            </select>

            <label class="auto-scroll-label">
                <input type="checkbox" @bind="AutoScroll" />
                Auto-scroll
            </label>

            <button class="btn btn-sm btn-outline-secondary" @onclick="ClearEvents">
                Clear
            </button>
        </div>
    </div>

    <div class="viewer-stats">
        <span>Total: @Events.Count</span>
        <span>Filtered: @FilteredEvents.Count</span>
    </div>

    <div class="viewer-content" @ref="ViewerContentRef">
        @if (!FilteredEvents.Any())
        {
            <div class="no-events">
                <p>No events to display</p>
            </div>
        }
        else
        {
            @foreach (var ev in FilteredEvents)
            {
                <TransparencyEventDisplay Event="ev" />
            }
        }
    </div>
</div>

@code {
    private List<TransparencyEvent> Events { get; set; } = new();
    private string SearchQuery { get; set; } = string.Empty;
    private string SelectedEventType { get; set; } = string.Empty;
    private bool AutoScroll { get; set; } = true;
    private ElementReference ViewerContentRef;

    private List<TransparencyEvent> FilteredEvents =>
        Events
            .Where(e =>
                (string.IsNullOrEmpty(SelectedEventType) || e.EventType.ToString() == SelectedEventType) &&
                (string.IsNullOrEmpty(SearchQuery) ||
                 e.EventType.ToString().Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                 e.Context?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                 e.Serialized?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();

    protected override void OnInitialized()
    {
        // Load existing events
        Events = TransparencyService.GetRecentEvents(1000).ToList();

        // Subscribe to new events
        TransparencyService.EventLogged += OnEventLogged;
    }

    private async void OnEventLogged(object? sender, TransparencyEvent e)
    {
        Events.Add(e);

        // Limit to 1000 events to prevent memory issues
        if (Events.Count > 1000)
        {
            Events.RemoveAt(0);
        }

        await InvokeAsync(async () =>
        {
            StateHasChanged();

            if (AutoScroll)
            {
                await ScrollToBottom();
            }
        });
    }

    private async Task ScrollToBottom()
    {
        // Use JS interop to scroll to bottom
        await JSRuntime.InvokeVoidAsync("scrollToBottom", ViewerContentRef);
    }

    private void ClearEvents()
    {
        Events.Clear();
        StateHasChanged();
    }

    public void Dispose()
    {
        TransparencyService.EventLogged -= OnEventLogged;
    }
}
```

**File:** `TransparentAiAgentGui/Components/Transparency/TransparencyViewer.razor.css`

```css
.transparency-viewer {
    display: flex;
    flex-direction: column;
    height: 100%;
    border: 1px solid #dee2e6;
    border-radius: 8px;
    background-color: #ffffff;
}

.viewer-header {
    padding: 16px;
    border-bottom: 1px solid #dee2e6;
    background-color: #f8f9fa;
}

.viewer-header h3 {
    margin: 0 0 12px 0;
    font-size: 18px;
}

.viewer-controls {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
    align-items: center;
}

.search-input {
    flex: 1;
    min-width: 200px;
    padding: 6px 12px;
    border: 1px solid #ced4da;
    border-radius: 4px;
}

.filter-select {
    padding: 6px 12px;
    border: 1px solid #ced4da;
    border-radius: 4px;
}

.auto-scroll-label {
    display: flex;
    align-items: center;
    gap: 6px;
    margin: 0;
    cursor: pointer;
}

.viewer-stats {
    padding: 8px 16px;
    background-color: #e9ecef;
    border-bottom: 1px solid #dee2e6;
    font-size: 12px;
    display: flex;
    gap: 16px;
}

.viewer-content {
    flex: 1;
    overflow-y: auto;
    padding: 16px;
}

.no-events {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 200px;
    color: #6c757d;
}
```

#### Step 4: Add JavaScript for Auto-Scroll (30 min)

**File:** `TransparentAiAgentGui/wwwroot/js/site.js` (create if doesn't exist)

```javascript
window.scrollToBottom = function(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};
```

**File:** `TransparentAiAgentGui/Components/Layout/MainLayout.razor`

Add script reference:
```razor
<script src="js/site.js"></script>
```

#### Step 5: Add TransparencyViewer to Navigation (30 min)

**File:** `TransparentAiAgentGui/Components/Layout/NavMenu.razor`

Add navigation link:
```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="transparency">
        <span class="bi bi-eye" aria-hidden="true"></span> Transparency
    </NavLink>
</div>
```

**File:** `TransparentAiAgentGui/Components/Pages/Transparency.razor` (new file)

```razor
@page "/transparency"
@using TransparentAiAgentGui.Components.Transparency

<PageTitle>Transparency</PageTitle>

<h1>Transparency Viewer</h1>

<TransparencyViewer />
```

#### Step 6: Testing (2 hours)

**Unit Tests to Create:**

1. `TransparencyServiceTests.cs`:
```csharp
[TestMethod]
public void EventLogged_RaisesEvent()
{
    // Arrange
    var service = new TransparencyService();
    TransparencyEvent? raisedEvent = null;
    service.EventLogged += (sender, e) => raisedEvent = e;

    // Act
    service.LogEvent(TransparencyEventType.ToolCall, new { tool = "test" });

    // Assert
    Assert.IsNotNull(raisedEvent);
    Assert.AreEqual(TransparencyEventType.ToolCall, raisedEvent.EventType);
}
```

2. `TransparencyViewerTests.cs` (bUnit):
```csharp
[TestMethod]
public void TransparencyViewer_DisplaysEvents()
{
    // Arrange
    using var ctx = new TestContext();
    var mockService = new Mock<ITransparencyService>();
    var events = new List<TransparencyEvent>
    {
        new() { EventType = TransparencyEventType.ToolCall, Context = "Test" }
    };
    mockService.Setup(s => s.GetRecentEvents(It.IsAny<int>())).Returns(events);
    ctx.Services.AddSingleton(mockService.Object);

    // Act
    var component = ctx.RenderComponent<TransparencyViewer>();

    // Assert
    component.Find(".event-type").TextContent.Should().Contain("ToolCall");
}
```

**Manual Testing Checklist:**
- [ ] Events appear in real-time as agent operates
- [ ] Search filters events correctly
- [ ] Event type filter works
- [ ] Auto-scroll scrolls to latest events
- [ ] Clear button clears all events
- [ ] Event details expand/collapse
- [ ] Icons and colors match event types
- [ ] Component handles 1000+ events without lag

### Expected Outcomes

After implementing Transparency Viewer:
- ✅ Users can view all agent operations in real-time
- ✅ Events are searchable and filterable
- ✅ Event details are collapsible for readability
- ✅ Auto-scroll keeps latest events visible
- ✅ Performance remains smooth with large event counts
- ✅ Clear visual distinction between event types

---

## Component 3: Tools Overview

### Overview

Create a component that displays all available tools, their schemas, and usage statistics, providing developers and users with complete visibility into the agent's capabilities.

### Requirements

**Functional Requirements:**
- List all available tools from all sources (MCP, built-in, future)
- Display tool name, description, and source
- Show tool input schema (JSON Schema)
- Display usage statistics (call count, success rate, avg duration)
- Search/filter tools by name or description
- Group tools by source (MCP server, built-in, etc.)
- Click tool to see detailed information

**Non-Functional Requirements:**
- Load tool list quickly (<500ms)
- Support 50+ tools without performance issues
- Responsive design

### Component Architecture

**New Files to Create:**
1. `TransparentAiAgentGui/Components/Tools/ToolsOverview.razor`
2. `TransparentAiAgentGui/Components/Tools/ToolsOverview.razor.css`
3. `TransparentAiAgentGui/Components/Tools/ToolCard.razor`
4. `TransparentAiAgentGui/Components/Tools/ToolCard.razor.css`
5. `TransparentAiAgentGui/Components/Tools/ToolDetailsModal.razor`

### Detailed Implementation Steps

#### Step 1: Create Tool Usage Statistics Service (1-2 hours)

**File:** `TransparentAiAgentCore/Infrastructure/Tools/IToolUsageStatistics.cs` (new)

```csharp
public interface IToolUsageStatistics
{
    void RecordToolCall(string toolName, bool success, TimeSpan duration);
    ToolStats GetToolStats(string toolName);
    Dictionary<string, ToolStats> GetAllStats();
}

public class ToolStats
{
    public string ToolName { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate => CallCount > 0 ? (double)SuccessCount / CallCount : 0;
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration => CallCount > 0 ? TotalDuration / CallCount : TimeSpan.Zero;
    public DateTime? LastUsed { get; set; }
}
```

**File:** `TransparentAiAgentCore/Infrastructure/Tools/ToolUsageStatistics.cs` (new)

```csharp
public class ToolUsageStatistics : IToolUsageStatistics
{
    private readonly Dictionary<string, ToolStats> _stats = new();
    private readonly object _lock = new();

    public void RecordToolCall(string toolName, bool success, TimeSpan duration)
    {
        lock (_lock)
        {
            if (!_stats.ContainsKey(toolName))
            {
                _stats[toolName] = new ToolStats { ToolName = toolName };
            }

            var stats = _stats[toolName];
            stats.CallCount++;
            if (success) stats.SuccessCount++;
            else stats.FailureCount++;
            stats.TotalDuration += duration;
            stats.LastUsed = DateTime.UtcNow;
        }
    }

    public ToolStats GetToolStats(string toolName)
    {
        lock (_lock)
        {
            return _stats.TryGetValue(toolName, out var stats)
                ? stats
                : new ToolStats { ToolName = toolName };
        }
    }

    public Dictionary<string, ToolStats> GetAllStats()
    {
        lock (_lock)
        {
            return new Dictionary<string, ToolStats>(_stats);
        }
    }
}
```

#### Step 2: Integrate Statistics into ToolManager (1 hour)

**File:** `TransparentAiAgentCore/Application/Tools/ToolManager.cs`

Modify to record statistics:
```csharp
public class ToolManager : IToolManager
{
    private readonly IToolUsageStatistics _statistics;

    public ToolManager(
        IToolRegistry toolRegistry,
        IToolExecutor toolExecutor,
        IToolUsageStatistics statistics)
    {
        _toolRegistry = toolRegistry;
        _toolExecutor = toolExecutor;
        _statistics = statistics;
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await _toolExecutor.ExecuteAsync(toolName, arguments, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _statistics.RecordToolCall(toolName, result.IsSuccess, duration);

            return result;
        }
        catch (Exception)
        {
            var duration = DateTime.UtcNow - startTime;
            _statistics.RecordToolCall(toolName, false, duration);
            throw;
        }
    }
}
```

#### Step 3: Create ToolCard Component (1-2 hours)

**File:** `TransparentAiAgentGui/Components/Tools/ToolCard.razor`

```razor
@using TransparentAiAgentCore.Domain.Tools
@using TransparentAiAgentCore.Infrastructure.Tools

<div class="tool-card" @onclick="OnClick">
    <div class="tool-header">
        <h4 class="tool-name">@Tool.Name</h4>
        <span class="tool-source">@GetSourceBadge()</span>
    </div>

    @if (!string.IsNullOrEmpty(Tool.Description))
    {
        <p class="tool-description">@Tool.Description</p>
    }

    @if (Stats != null)
    {
        <div class="tool-stats">
            <span class="stat-item">
                <strong>Calls:</strong> @Stats.CallCount
            </span>
            <span class="stat-item">
                <strong>Success:</strong> @Stats.SuccessRate.ToString("P0")
            </span>
            @if (Stats.LastUsed.HasValue)
            {
                <span class="stat-item">
                    <strong>Last used:</strong> @FormatTimeAgo(Stats.LastUsed.Value)
                </span>
            }
        </div>
    }
</div>

@code {
    [Parameter]
    public ITool Tool { get; set; } = null!;

    [Parameter]
    public ToolStats? Stats { get; set; }

    [Parameter]
    public EventCallback<ITool> OnToolClick { get; set; }

    private async Task OnClick()
    {
        await OnToolClick.InvokeAsync(Tool);
    }

    private string GetSourceBadge()
    {
        // Determine source from tool metadata
        return Tool.GetType().Name.Contains("MCP") ? "MCP" : "Built-in";
    }

    private string FormatTimeAgo(DateTime timestamp)
    {
        var diff = DateTime.UtcNow - timestamp;
        if (diff.TotalSeconds < 60) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }
}
```

**File:** `TransparentAiAgentGui/Components/Tools/ToolCard.razor.css`

```css
.tool-card {
    border: 1px solid #dee2e6;
    border-radius: 8px;
    padding: 16px;
    background-color: #ffffff;
    cursor: pointer;
    transition: all 0.2s;
}

.tool-card:hover {
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
}

.tool-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
}

.tool-name {
    margin: 0;
    font-size: 16px;
    font-weight: 600;
}

.tool-source {
    font-size: 12px;
    padding: 4px 8px;
    border-radius: 4px;
    background-color: #007bff;
    color: white;
}

.tool-description {
    font-size: 14px;
    color: #6c757d;
    margin-bottom: 12px;
}

.tool-stats {
    display: flex;
    gap: 16px;
    font-size: 12px;
    color: #495057;
}

.stat-item strong {
    color: #212529;
}
```

#### Step 4: Create ToolsOverview Component (2-3 hours)

**File:** `TransparentAiAgentGui/Components/Tools/ToolsOverview.razor`

```razor
@using TransparentAiAgentCore.Domain.Tools
@using TransparentAiAgentCore.Infrastructure.Tools
@inject IToolRegistry ToolRegistry
@inject IToolUsageStatistics ToolUsageStatistics

<div class="tools-overview">
    <div class="overview-header">
        <h3>Available Tools</h3>
        <div class="overview-controls">
            <input type="text"
                   class="search-input"
                   placeholder="Search tools..."
                   @bind="SearchQuery"
                   @bind:event="oninput" />

            <select class="source-filter" @bind="SourceFilter">
                <option value="">All Sources</option>
                <option value="MCP">MCP Tools</option>
                <option value="Built-in">Built-in Tools</option>
            </select>
        </div>
    </div>

    <div class="overview-stats">
        <span>Total Tools: @AllTools.Count</span>
        <span>Filtered: @FilteredTools.Count</span>
    </div>

    <div class="tools-grid">
        @if (!FilteredTools.Any())
        {
            <div class="no-tools">
                <p>No tools found</p>
            </div>
        }
        else
        {
            @foreach (var tool in FilteredTools)
            {
                var stats = ToolUsageStatistics.GetToolStats(tool.Name);
                <ToolCard Tool="tool"
                         Stats="stats"
                         OnToolClick="OnToolCardClick" />
            }
        }
    </div>
</div>

@if (SelectedTool != null)
{
    <ToolDetailsModal Tool="SelectedTool"
                     Stats="ToolUsageStatistics.GetToolStats(SelectedTool.Name)"
                     OnClose="CloseDetailsModal" />
}

@code {
    private List<ITool> AllTools { get; set; } = new();
    private string SearchQuery { get; set; } = string.Empty;
    private string SourceFilter { get; set; } = string.Empty;
    private ITool? SelectedTool { get; set; }

    private List<ITool> FilteredTools =>
        AllTools
            .Where(t =>
                (string.IsNullOrEmpty(SearchQuery) ||
                 t.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                 t.Description?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true) &&
                (string.IsNullOrEmpty(SourceFilter) || GetToolSource(t) == SourceFilter))
            .OrderBy(t => t.Name)
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        AllTools = (await ToolRegistry.GetAllToolsAsync()).ToList();
    }

    private void OnToolCardClick(ITool tool)
    {
        SelectedTool = tool;
    }

    private void CloseDetailsModal()
    {
        SelectedTool = null;
    }

    private string GetToolSource(ITool tool)
    {
        return tool.GetType().Name.Contains("MCP") ? "MCP" : "Built-in";
    }
}
```

**File:** `TransparentAiAgentGui/Components/Tools/ToolsOverview.razor.css`

```css
.tools-overview {
    padding: 16px;
}

.overview-header {
    margin-bottom: 16px;
}

.overview-header h3 {
    margin: 0 0 12px 0;
}

.overview-controls {
    display: flex;
    gap: 12px;
}

.search-input {
    flex: 1;
    padding: 8px 12px;
    border: 1px solid #ced4da;
    border-radius: 4px;
}

.source-filter {
    padding: 8px 12px;
    border: 1px solid #ced4da;
    border-radius: 4px;
}

.overview-stats {
    padding: 8px 0;
    margin-bottom: 16px;
    display: flex;
    gap: 16px;
    font-size: 14px;
}

.tools-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 16px;
}

.no-tools {
    grid-column: 1 / -1;
    text-align: center;
    padding: 48px;
    color: #6c757d;
}
```

#### Step 5: Create ToolDetailsModal Component (1-2 hours)

**File:** `TransparentAiAgentGui/Components/Tools/ToolDetailsModal.razor`

```razor
@using TransparentAiAgentCore.Domain.Tools
@using TransparentAiAgentCore.Infrastructure.Tools
@using System.Text.Json

<div class="modal-backdrop" @onclick="OnClose">
    <div class="modal-content" @onclick:stopPropagation>
        <div class="modal-header">
            <h3>@Tool.Name</h3>
            <button class="close-button" @onclick="OnClose">×</button>
        </div>

        <div class="modal-body">
            @if (!string.IsNullOrEmpty(Tool.Description))
            {
                <section>
                    <h4>Description</h4>
                    <p>@Tool.Description</p>
                </section>
            }

            <section>
                <h4>Input Schema</h4>
                <pre><code>@FormatJson(Tool.InputSchema)</code></pre>
            </section>

            @if (Stats != null && Stats.CallCount > 0)
            {
                <section>
                    <h4>Usage Statistics</h4>
                    <div class="stats-grid">
                        <div class="stat-box">
                            <strong>Total Calls</strong>
                            <span class="stat-value">@Stats.CallCount</span>
                        </div>
                        <div class="stat-box">
                            <strong>Success Rate</strong>
                            <span class="stat-value">@Stats.SuccessRate.ToString("P1")</span>
                        </div>
                        <div class="stat-box">
                            <strong>Avg Duration</strong>
                            <span class="stat-value">@Stats.AverageDuration.TotalMilliseconds.ToString("F0")ms</span>
                        </div>
                        <div class="stat-box">
                            <strong>Last Used</strong>
                            <span class="stat-value">@(Stats.LastUsed?.ToString("yyyy-MM-dd HH:mm") ?? "Never")</span>
                        </div>
                    </div>
                </section>
            }
        </div>
    </div>
</div>

@code {
    [Parameter]
    public ITool Tool { get; set; } = null!;

    [Parameter]
    public ToolStats? Stats { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private string FormatJson(object? obj)
    {
        if (obj == null) return "{}";
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
```

**File:** `TransparentAiAgentGui/Components/Tools/ToolDetailsModal.razor.css`

```css
.modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
}

.modal-content {
    background-color: white;
    border-radius: 8px;
    max-width: 800px;
    max-height: 90vh;
    overflow-y: auto;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px;
    border-bottom: 1px solid #dee2e6;
}

.modal-header h3 {
    margin: 0;
}

.close-button {
    background: none;
    border: none;
    font-size: 32px;
    cursor: pointer;
    color: #6c757d;
    line-height: 1;
}

.close-button:hover {
    color: #212529;
}

.modal-body {
    padding: 20px;
}

.modal-body section {
    margin-bottom: 24px;
}

.modal-body h4 {
    margin-bottom: 12px;
    font-size: 16px;
    font-weight: 600;
}

.modal-body pre {
    background-color: #f8f9fa;
    padding: 12px;
    border-radius: 4px;
    overflow-x: auto;
}

.stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: 16px;
}

.stat-box {
    border: 1px solid #dee2e6;
    border-radius: 4px;
    padding: 12px;
    text-align: center;
}

.stat-box strong {
    display: block;
    font-size: 12px;
    color: #6c757d;
    margin-bottom: 8px;
}

.stat-value {
    display: block;
    font-size: 24px;
    font-weight: 600;
    color: #212529;
}
```

#### Step 6: Add Tools Page to Navigation (30 min)

**File:** `TransparentAiAgentGui/Components/Layout/NavMenu.razor`

```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="tools">
        <span class="bi bi-tools" aria-hidden="true"></span> Tools
    </NavLink>
</div>
```

**File:** `TransparentAiAgentGui/Components/Pages/Tools.razor` (new)

```razor
@page "/tools"
@using TransparentAiAgentGui.Components.Tools

<PageTitle>Tools</PageTitle>

<h1>Tools Overview</h1>

<ToolsOverview />
```

#### Step 7: Register Services (15 min)

**File:** `TransparentAiAgentGui/Program.cs`

Add service registration:
```csharp
builder.Services.AddSingleton<IToolUsageStatistics, ToolUsageStatistics>();
```

### Expected Outcomes

After implementing Tools Overview:
- ✅ Users can browse all available tools
- ✅ Tool descriptions and schemas are visible
- ✅ Usage statistics show which tools are used most
- ✅ Search and filtering make tools easy to find
- ✅ Modal provides detailed tool information
- ✅ Component is performant with 50+ tools

---

## Component 4: Enhanced Message Formatting & Markdown Rendering

### Overview

Improve the visual presentation of messages with markdown rendering, JSON syntax highlighting, and enhanced formatting for tool calls and results. This component enables rich formatting in assistant responses including headings, tables, code blocks with syntax highlighting, lists, and inline formatting.

### Requirements

**Markdown Rendering:**
- Install and configure Markdig 0.43 for markdown-to-HTML conversion
- Render assistant messages as rich markdown (headings, tables, lists, bold/italic, links)
- Syntax highlighting for code blocks
- Support for advanced markdown extensions (tables, task lists, etc.)

**JSON Formatting:**
- JSON syntax highlighting for tool arguments/results
- Collapsible sections for large payloads
- Better error message formatting
- Copy button for JSON content
- Improved context status display

### Implementation Steps

#### Step 0: Install Markdig NuGet Package (15 min)

**File:** `TransparentAiAgentGui/TransparentAiAgentGui.csproj`

Add Markdig package reference:

```xml
<ItemGroup>
  <PackageReference Include="Markdig" Version="0.43.0" />
</ItemGroup>
```

Run package restore:
```bash
dotnet restore TransparentAiAgentGui/TransparentAiAgentGui.csproj
```

#### Step 1: Create MarkdownDisplay Component (1-2 hours)

**File:** `TransparentAiAgentGui/Components/Chat/MarkdownDisplay.razor` (new)

```razor
@using Markdig
@using Microsoft.AspNetCore.Components
@inject IMarkdownRenderer MarkdownRenderer

<div class="markdown-content" @attributes="AdditionalAttributes">
    @((MarkupString)RenderedHtml)
</div>

@code {
    [Parameter]
    public string MarkdownContent { get; set; } = string.Empty;

    [Parameter]
    public bool EnableSyntaxHighlighting { get; set; } = true;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string RenderedHtml => string.IsNullOrEmpty(MarkdownContent)
        ? string.Empty
        : MarkdownRenderer.ToHtml(MarkdownContent, EnableSyntaxHighlighting);
}
```

**File:** `TransparentAiAgentGui/Components/Chat/MarkdownDisplay.razor.css` (new)

```css
.markdown-content {
    line-height: 1.6;
    color: #333;
}

/* Headings */
.markdown-content h1 {
    font-size: 2em;
    border-bottom: 2px solid #e1e4e8;
    padding-bottom: 0.3em;
    margin-top: 24px;
    margin-bottom: 16px;
}

.markdown-content h2 {
    font-size: 1.5em;
    border-bottom: 1px solid #e1e4e8;
    padding-bottom: 0.3em;
    margin-top: 24px;
    margin-bottom: 16px;
}

.markdown-content h3 {
    font-size: 1.25em;
    margin-top: 24px;
    margin-bottom: 16px;
}

.markdown-content h4, .markdown-content h5, .markdown-content h6 {
    margin-top: 24px;
    margin-bottom: 16px;
}

/* Code blocks */
.markdown-content pre {
    background-color: #f6f8fa;
    border-radius: 6px;
    padding: 16px;
    overflow-x: auto;
    margin: 16px 0;
}

.markdown-content code {
    background-color: #f6f8fa;
    border-radius: 3px;
    padding: 0.2em 0.4em;
    font-family: 'Courier New', Courier, monospace;
    font-size: 85%;
}

.markdown-content pre code {
    background-color: transparent;
    padding: 0;
}

/* Tables */
.markdown-content table {
    border-collapse: collapse;
    width: 100%;
    margin: 16px 0;
}

.markdown-content table th,
.markdown-content table td {
    border: 1px solid #d0d7de;
    padding: 6px 13px;
}

.markdown-content table th {
    background-color: #f6f8fa;
    font-weight: 600;
}

.markdown-content table tr:nth-child(2n) {
    background-color: #f6f8fa;
}

/* Lists */
.markdown-content ul,
.markdown-content ol {
    margin: 16px 0;
    padding-left: 2em;
}

.markdown-content li {
    margin: 0.25em 0;
}

/* Links */
.markdown-content a {
    color: #0969da;
    text-decoration: none;
}

.markdown-content a:hover {
    text-decoration: underline;
}

/* Block quotes */
.markdown-content blockquote {
    border-left: 4px solid #d0d7de;
    padding-left: 16px;
    margin: 16px 0;
    color: #57606a;
}

/* Horizontal rules */
.markdown-content hr {
    border: none;
    border-top: 1px solid #d0d7de;
    margin: 24px 0;
}

/* Inline code distinction */
.markdown-content p code,
.markdown-content li code {
    color: #cf222e;
}

/* Task lists */
.markdown-content input[type="checkbox"] {
    margin-right: 0.5em;
}
```

#### Step 2: Register Markdown Renderer Service (15 min)

**File:** `TransparentAiAgentGui/Program.cs`

Add service registration (assuming MarkdigRenderer was created in Component 1.5):

```csharp
using TransparentAiAgentCore.Infrastructure.Markdown;

// ... existing code ...

builder.Services.AddSingleton<IMarkdownRenderer, MarkdigRenderer>();
```

#### Step 3: Update MessageDisplay to Use MarkdownDisplay (1 hour)

**File:** `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor`

Update to render assistant messages as markdown:

```razor
@using TransparentAiAgentGui.Models

<div class="message @GetMessageClass()">
    <div class="message-header">
        <span class="message-role">@GetMessageRole()</span>
        <span class="message-timestamp">@Message.Timestamp.ToString("HH:mm:ss")</span>
    </div>

    <div class="message-content">
        @if (Message.IsToolCall)
        {
            <div class="tool-call-section">
                <span class="tool-icon">🔧</span>
                <strong>Tool Call: @Message.ToolName</strong>
                <JsonDisplay JsonContent="@Message.ToolArguments"
                            Label="Arguments"
                            IsCollapsible="true" />
            </div>
        }
        else if (Message.IsToolResult)
        {
            <div class="tool-result-section @GetResultClass()">
                <span class="result-icon">@(Message.IsSuccess ? "✅" : "❌")</span>
                <strong>Tool Result</strong>
                <JsonDisplay JsonContent="@Message.Content"
                            Label="Result"
                            IsCollapsible="true" />
            </div>
        }
        else if (Message.Role == MessageRole.Assistant)
        {
            <!-- NEW: Render assistant messages as markdown -->
            <MarkdownDisplay MarkdownContent="@Message.Content" />
        }
        else
        {
            <!-- User and system messages as plain text -->
            <div class="plain-text">@Message.Content</div>
        }
    </div>
</div>

@code {
    [Parameter]
    public UIMessage Message { get; set; } = null!;

    private string GetMessageClass() => Message.Role switch
    {
        MessageRole.User => "message-user",
        MessageRole.Assistant => "message-assistant",
        MessageRole.System => "message-system",
        _ => "message-unknown"
    };

    private string GetMessageRole() => Message.Role switch
    {
        MessageRole.User => "You",
        MessageRole.Assistant => "Assistant",
        MessageRole.System => "System",
        _ => "Unknown"
    };

    private string GetResultClass() => Message.IsSuccess ? "result-success" : "result-error";
}
```

**File:** `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor.css`

Update CSS to accommodate markdown:

```css
.message {
    margin-bottom: 16px;
    padding: 12px;
    border-radius: 8px;
    background-color: #f8f9fa;
}

.message-user {
    background-color: #e3f2fd;
    margin-left: 20%;
}

.message-assistant {
    background-color: #f1f8e9;
    margin-right: 20%;
}

.message-system {
    background-color: #fff3e0;
    font-style: italic;
}

.message-header {
    display: flex;
    justify-content: space-between;
    margin-bottom: 8px;
    font-size: 12px;
    color: #6c757d;
}

.message-role {
    font-weight: 600;
}

.message-content {
    /* Remove previous pre-wrap to let markdown component handle formatting */
}

.plain-text {
    white-space: pre-wrap;
    word-wrap: break-word;
}

.tool-call-section,
.tool-result-section {
    padding: 12px;
    border-radius: 4px;
    border-left: 4px solid #007bff;
    background-color: #ffffff;
}

.result-success {
    border-left-color: #28a745;
}

.result-error {
    border-left-color: #dc3545;
}

.tool-icon,
.result-icon {
    margin-right: 8px;
    font-size: 18px;
}
```

#### Step 4: Add JSON Syntax Highlighting (1-2 hours)

**File:** `TransparentAiAgentGui/Components/Chat/JsonDisplay.razor` (new)

```razor
<div class="json-display">
    @if (ShowCopyButton)
    {
        <button class="copy-button" @onclick="CopyToClipboard">
            @(CopiedRecently ? "Copied!" : "Copy")
        </button>
    }

    @if (IsCollapsible)
    {
        <div class="collapse-header" @onclick="ToggleCollapsed">
            <span class="collapse-icon">@(IsCollapsed ? "▶" : "▼")</span>
            <span>@Label</span>
        </div>
    }

    @if (!IsCollapsed)
    {
        <pre class="json-content"><code>@FormattedJson</code></pre>
    }
</div>

@code {
    [Parameter]
    public string JsonContent { get; set; } = string.Empty;

    [Parameter]
    public string Label { get; set; } = "JSON";

    [Parameter]
    public bool IsCollapsible { get; set; } = true;

    [Parameter]
    public bool ShowCopyButton { get; set; } = true;

    private bool IsCollapsed { get; set; } = true;
    private bool CopiedRecently { get; set; }

    private string FormattedJson =>
        string.IsNullOrEmpty(JsonContent)
            ? "{}"
            : JsonSerializer.Serialize(
                JsonSerializer.Deserialize<object>(JsonContent),
                new JsonSerializerOptions { WriteIndented = true });

    private void ToggleCollapsed()
    {
        IsCollapsed = !IsCollapsed;
    }

    private async Task CopyToClipboard()
    {
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", FormattedJson);
        CopiedRecently = true;
        StateHasChanged();

        await Task.Delay(2000);
        CopiedRecently = false;
        StateHasChanged();
    }
}
```

#### Step 2: Update MessageDisplay to Use JsonDisplay (1 hour)

**File:** `TransparentAiAgentGui/Components/Chat/MessageDisplay.razor`

Integrate JsonDisplay for tool calls and results:
```razor
@if (Message.MessageType == MessageType.ToolCall)
{
    <div class="tool-call-section">
        <span class="tool-icon">🔧</span>
        <strong>@Message.ToolName</strong>
        <JsonDisplay JsonContent="@Message.ToolArguments"
                    Label="Arguments"
                    IsCollapsible="true" />
    </div>
}

@if (Message.MessageType == MessageType.ToolResult)
{
    <div class="tool-result-section @GetResultClass()">
        <span class="result-icon">@(Message.IsSuccess ? "✅" : "❌")</span>
        <strong>Result</strong>
        <JsonDisplay JsonContent="@Message.Content"
                    Label="Result"
                    IsCollapsible="true" />
    </div>
}
```

### Expected Outcomes

After enhancing message formatting:
- ✅ JSON content is properly formatted with indentation
- ✅ Large payloads are collapsible to reduce clutter
- ✅ Copy button allows easy copying of JSON
- ✅ Tool calls and results are visually distinct
- ✅ Error messages are highlighted

---

## Testing Strategy

### Unit Testing

**Test Coverage Goals:**
- Core logic: 80%+
- Service layer: 70%+
- Infrastructure: 60%+

**New Test Files to Create:**
1. `StreamingCallbackTests.cs` - Test streaming logic
2. `TransparencyServiceTests.cs` - Test event logging and querying
3. `ToolUsageStatisticsTests.cs` - Test statistics recording
4. `ConversationUIServiceStreamingTests.cs` - Test UI service streaming
5. `MarkdownStreamingBufferTests.cs` - Test semantic buffering for markdown constructs
6. `MarkdigRendererTests.cs` - Test markdown rendering

**Key Test Scenarios:**
- Streaming message updates are throttled correctly
- Transparency events are logged and retrievable
- Tool statistics are recorded accurately
- Error handling works during streaming failures
- Event subscriptions are cleaned up properly (no memory leaks)
- **Markdown buffering prevents partial constructs from rendering** (NEW)
- **Code blocks buffer until closing fence received** (NEW)
- **Table rows buffer until complete** (NEW)
- **Headings buffer until end of line** (NEW)
- **Tool calls buffer until complete** (NEW)
- **Timeout forces flush after 1 second max** (NEW)

### Component Testing (bUnit)

**New Test Files to Create:**
1. `TransparencyViewerTests.cs` - Test event display and filtering
2. `ToolsOverviewTests.cs` - Test tool listing and search
3. `JsonDisplayTests.cs` - Test JSON formatting and collapsing
4. `MessageDisplayStreamingTests.cs` - Test streaming UI updates
5. `MarkdownDisplayTests.cs` - Test markdown rendering in UI (NEW)
6. `MessageDisplayMarkdownTests.cs` - Test assistant messages render as markdown (NEW)

**Example bUnit Test:**
```csharp
[TestMethod]
public void TransparencyViewer_FiltersEventsByType()
{
    // Arrange
    using var ctx = new TestContext();
    var mockService = new Mock<ITransparencyService>();
    var events = new List<TransparencyEvent>
    {
        new() { EventType = TransparencyEventType.ToolCall },
        new() { EventType = TransparencyEventType.Error },
        new() { EventType = TransparencyEventType.ToolCall }
    };
    mockService.Setup(s => s.GetRecentEvents(It.IsAny<int>())).Returns(events);
    ctx.Services.AddSingleton(mockService.Object);

    // Act
    var component = ctx.RenderComponent<TransparencyViewer>();
    var select = component.Find(".filter-select");
    select.Change("ToolCall");

    // Assert
    var displayedEvents = component.FindAll(".transparency-event");
    Assert.AreEqual(2, displayedEvents.Count);
}
```

### Integration Testing

**Test Scenarios:**
1. **End-to-End Streaming Test**
   - User sends message
   - LLM streams response token-by-token
   - UI updates in real-time
   - Transparency events logged
   - Tool statistics updated

2. **Tool Execution with Transparency**
   - User triggers tool call
   - Tool executes and returns result
   - Transparency viewer shows all events
   - Tools overview shows updated statistics

3. **Multi-Tool Workflow**
   - Agent calls multiple tools in sequence
   - Each tool call appears in UI
   - All events logged to transparency
   - Statistics accurate for all tools

**Manual Test Execution:**
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Integration"
```

### Manual Testing Checklist

**Streaming Tests:**
- [ ] Tokens appear incrementally (not all at once)
- [ ] Streaming indicator appears during generation
- [ ] Cursor blinks during streaming
- [ ] Throttling prevents UI flickering
- [ ] Final message is complete and accurate
- [ ] Errors during streaming are handled gracefully

**Transparency Viewer Tests:**
- [ ] All event types display with correct icons
- [ ] Real-time events appear immediately
- [ ] Search finds events by content
- [ ] Filter by event type works
- [ ] Auto-scroll keeps latest events visible
- [ ] Clear button removes all events
- [ ] Expand/collapse works for event details
- [ ] Handles 1000+ events without lag

**Tools Overview Tests:**
- [ ] All tools from all sources display
- [ ] Search finds tools by name/description
- [ ] Source filter works (MCP vs Built-in)
- [ ] Tool cards show usage statistics
- [ ] Clicking tool opens detail modal
- [ ] Modal shows schema and statistics
- [ ] Modal closes properly
- [ ] Component loads quickly (<500ms)

**Enhanced Formatting Tests:**
- [ ] JSON is properly indented
- [ ] Copy button copies JSON to clipboard
- [ ] Collapsible sections expand/collapse
- [ ] Tool calls have correct icon (🔧)
- [ ] Tool results have correct icon (✅/❌)
- [ ] Error messages are highlighted in red

**Markdown Rendering Tests:** (NEW)
- [ ] Headings render with correct size and styling
- [ ] Tables render with borders and proper alignment
- [ ] Code blocks have background color and proper formatting
- [ ] Inline code has distinct styling from regular text
- [ ] Lists (ordered and unordered) render correctly
- [ ] Bold and italic formatting works
- [ ] Links are clickable and styled
- [ ] Blockquotes have left border and indentation
- [ ] Task lists render with checkboxes

**Markdown Streaming Tests:** (NEW)
- [ ] Partial table rows don't render until complete
- [ ] Incomplete code blocks buffer until closing fence
- [ ] Headings don't flicker during streaming
- [ ] Tool calls stay hidden until complete
- [ ] Bold/italic markers don't show temporarily
- [ ] Buffered content appears within 1 second (timeout flush)
- [ ] Nested markdown constructs handle correctly

### Performance Testing

**Metrics to Measure:**
- Time to first token in streaming (should be <500ms)
- UI update frequency during streaming (should be ~20 updates/sec)
- Transparency viewer render time with 1000 events (should be <1s)
- Tools overview load time with 50 tools (should be <500ms)
- Memory usage after 10 minutes of operation (should be stable)

**Performance Test Scripts:**
```csharp
[TestMethod]
public async Task Streaming_HandlesHighTokenRate()
{
    // Simulate 100 tokens/second
    var tokens = Enumerable.Range(0, 1000).Select(i => "token" + i);
    var startTime = DateTime.UtcNow;

    foreach (var token in tokens)
    {
        await streamingCallback.OnTokenReceived(token);
    }

    var duration = DateTime.UtcNow - startTime;
    Assert.IsTrue(duration.TotalSeconds < 2, "Streaming should handle 100 tokens/sec");
}
```

---

## Implementation Timeline

### Phase 6a: Streaming LLM Responses (8-10 hours)

**Week 1:**
- Day 1-2: Modify `AzureOpenAiChatService` for streaming (3 hours)
- Day 2-3: Extend `ConversationUIService` (2 hours)
- Day 3-4: Implement streaming in `AgentOrchestrator` (3 hours)
- Day 4-5: Update `MessageDisplay` component (2 hours)
- Day 5: Add transparency events and testing (2 hours)

**Deliverable:** Token-by-token streaming functional in UI

### Phase 6a-1: Markdown-Aware Streaming Buffer (4-5 hours) **(NEW)**

**Week 1-2:**
- Day 1: Create `MarkdownStreamingBuffer` core (3 hours)
- Day 1-2: Create markdown renderer interface and Markdig implementation (1 hour)
- Day 2: Update `AgentOrchestrator` to use semantic buffer (1 hour)
- Day 2: Add configuration support (30 min)
- Day 2: Testing markdown buffering logic (2 hours)

**Deliverable:** Semantic buffering prevents markdown rendering glitches

### Phase 6b: Transparency Viewer (6-8 hours)

**Week 2:**
- Day 1: Extend `TransparencyService` with events (1 hour)
- Day 1-2: Create `TransparencyEventDisplay` component (2 hours)
- Day 2-3: Create `TransparencyViewer` component (3 hours)
- Day 3: Add JavaScript and navigation (1 hour)
- Day 4: Testing and polish (2 hours)

**Deliverable:** Transparency Viewer operational

### Phase 6c: Tools Overview (6-8 hours)

**Week 2-3:**
- Day 1: Create `ToolUsageStatistics` service (2 hours)
- Day 1-2: Integrate statistics into `ToolManager` (1 hour)
- Day 2: Create `ToolCard` component (2 hours)
- Day 3: Create `ToolsOverview` component (3 hours)
- Day 3-4: Create `ToolDetailsModal` component (2 hours)
- Day 4: Add navigation and services (1 hour)

**Deliverable:** Tools Overview operational

### Phase 6d: Enhanced Formatting & Markdown Rendering (5-7 hours) **(UPDATED)**

**Week 3:**
- Day 1: Install Markdig 0.43 and create `MarkdownDisplay` component (2 hours) **(NEW)**
- Day 1: Register markdown renderer service (15 min) **(NEW)**
- Day 1-2: Update `MessageDisplay` to use `MarkdownDisplay` for assistant messages (1 hour) **(NEW)**
- Day 2: Create `JsonDisplay` component (2 hours)
- Day 2-3: Update `MessageDisplay` to use `JsonDisplay` for tool calls/results (1 hour)
- Day 3: Testing markdown rendering and polish (2 hours) **(UPDATED)**

**Deliverable:** Enhanced message formatting with full markdown support

### Phase 6e: Testing & Documentation (4-6 hours)

**Week 3-4:**
- Day 1-2: Write unit tests (3 hours)
- Day 2: Write bUnit tests (2 hours)
- Day 3: Manual testing checklist (2 hours)
- Day 3: Update documentation (1 hour)

**Deliverable:** Phase 6 complete and tested

### Total Estimated Duration

**Original Estimate:**
- Minimum: 20 hours
- Maximum: 30 hours
- Realistic: 25 hours

**Updated Estimate (with Markdown Buffering & Rendering):**
- **Minimum:** 27 hours (4 weeks at ~7 hours/week)
- **Maximum:** 40 hours (5 weeks at ~8 hours/week)
- **Realistic:** 32 hours (4-5 weeks)

**Additional Time Breakdown:**
- Markdown-Aware Streaming Buffer (Component 1.5): +4-5 hours
- Markdown Rendering in UI (Component 4 updates): +3-4 hours
- Additional testing for markdown: +1-2 hours
- **Total Added:** ~7-10 hours

---

## Success Criteria

### Phase 6 is considered complete when:

**Functional Requirements:**
- ✅ LLM responses stream token-by-token in real-time
- ✅ **Markdown content renders correctly during streaming (no partial construct glitches)** (NEW)
- ✅ **Assistant messages display as rich markdown (headings, tables, code blocks, etc.)** (NEW)
- ✅ Transparency Viewer displays all events with filtering/search
- ✅ Tools Overview shows all tools with usage statistics
- ✅ JSON content is formatted with syntax highlighting
- ✅ All components are responsive and performant

**Technical Requirements:**
- ✅ 80%+ unit test coverage for new code
- ✅ All bUnit tests passing for new components
- ✅ Manual testing checklist 100% complete
- ✅ No performance regressions (existing tests pass)
- ✅ No memory leaks (event handlers properly disposed)

**Documentation Requirements:**
- ✅ Phase 6 implementation documented
- ✅ New components documented in code comments
- ✅ README updated with new features
- ✅ User guide updated (if applicable)

**Quality Gates:**
- ✅ Code review completed
- ✅ No critical bugs in issue tracker
- ✅ All Azure Pipelines green (if applicable)
- ✅ Performance benchmarks met

---

## Future Enhancements (Phase 7+)

### Phase 7: UI Element Toggles (8-12 hours)

**Objective:** Add ability to toggle visibility of different UI elements (message types, events, etc.)

**Components:**

1. **UIFilterService** (2 hours)
   - Centralized state management for UI filters
   - Support for multiple filter types (message type, event type, etc.)
   - Event-driven updates
   - LocalStorage persistence

2. **FilterPanel Component** (3 hours)
   - Checkboxes for each message type
   - Checkboxes for each event type
   - "Show All" / "Hide All" buttons
   - Collapsible panel design

3. **Integration with Existing Components** (3 hours)
   - Modify `MessageList` to filter based on service
   - Modify `TransparencyViewer` to filter based on service
   - Ensure reactive updates when filters change

4. **Testing** (2 hours)
   - Unit tests for `UIFilterService`
   - bUnit tests for `FilterPanel`
   - Manual testing checklist

**Example Implementation:**

```csharp
// UIFilterService.cs
public class UIFilterService
{
    public bool ShowUserMessages { get; set; } = true;
    public bool ShowAssistantMessages { get; set; } = true;
    public bool ShowSystemMessages { get; set; } = true;
    public bool ShowToolCalls { get; set; } = true;
    public bool ShowToolResults { get; set; } = true;

    public event EventHandler? FiltersChanged;

    public void UpdateFilter(string filterName, bool value)
    {
        // Update filter
        // Persist to LocalStorage
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool ShouldDisplay(UIMessage message)
    {
        return message.MessageType switch
        {
            MessageType.User => ShowUserMessages,
            MessageType.Assistant => ShowAssistantMessages,
            MessageType.System => ShowSystemMessages,
            MessageType.ToolCall => ShowToolCalls,
            MessageType.ToolResult => ShowToolResults,
            _ => true
        };
    }
}
```

**UI Design:**

```
┌─────────────────────────────────┐
│ Filters [Show/Hide]             │
├─────────────────────────────────┤
│ Message Types:                  │
│ ☑ User Messages                 │
│ ☑ Assistant Messages            │
│ ☐ System Messages               │
│ ☑ Tool Calls                    │
│ ☑ Tool Results                  │
│                                 │
│ Transparency Events:            │
│ ☑ Tool Events                   │
│ ☑ MCP Events                    │
│ ☐ Streaming Events              │
│ ☑ Error Events                  │
│                                 │
│ [Show All] [Hide All]           │
└─────────────────────────────────┘
```

### Phase 8: LLM-Controlled UI Elements (10-15 hours)

**Objective:** Allow the LLM to programmatically control UI element visibility

**Components:**

1. **UI Control Tool Definitions** (2 hours)
   - Define tools: `SetUIFilter`, `ToggleFilter`, `ResetFilters`
   - Add input schemas (which filter, what value)
   - Add to built-in tool registry

2. **UI Control Tool Executor** (2 hours)
   - Implement executor that updates `UIFilterService`
   - Add confirmation prompts for safety
   - Log transparency events

3. **Safety & Confirmation** (3 hours)
   - Add user confirmation for UI changes
   - Allow user to disable LLM UI control
   - Add audit log for UI changes

4. **Agent Integration** (2 hours)
   - Update agent to recognize UI control tools
   - Add logic for when to use UI controls
   - System prompt updates

5. **Testing** (3 hours)
   - Unit tests for UI control tools
   - Integration tests for agent → UI control
   - Manual testing with various scenarios

**Example Tool Definition:**

```csharp
public class SetUIFilterTool : ITool
{
    public string Name => "SetUIFilter";
    public string Description => "Control UI element visibility";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            filterType = new { type = "string", enum = new[] { "UserMessages", "ToolCalls", etc. } },
            visible = new { type = "boolean" }
        },
        required = new[] { "filterType", "visible" }
    };
}
```

**Safety Considerations:**
- User confirmation required for first-time use
- User can disable LLM UI control globally
- All UI changes logged to transparency
- User can revert any UI change manually

### Phase 9: Advanced Filtering & Search (6-8 hours)

**Objective:** Add advanced filtering and search capabilities to chat history and transparency viewer

**Features:**
- Date/time range filtering
- Full-text search with highlighting
- Saved filter presets
- Export filtered results to JSON/CSV
- Regex search support
- Filter by tool name, LLM model, etc.

### Phase 10: Visualization & Analytics (12-16 hours)

**Objective:** Add charts and graphs for tool usage, performance metrics, and agent behavior

**Components:**
- Tool usage pie chart (which tools used most)
- Performance timeline (response times over time)
- Success rate graphs (tool success/failure trends)
- Token usage tracking (cost estimation)
- Export data for external analysis

**Libraries to Consider:**
- Chart.js for Blazor
- ApexCharts for Blazor
- Plotly.NET

---

## Risk Assessment

### Technical Risks

**Risk 1: Streaming Performance**
- **Description:** Frequent UI updates may cause performance issues or UI flickering
- **Mitigation:**
  - Implement throttling (max 20 updates/sec)
  - Use `InvokeAsync` for thread-safe updates
  - Test with long responses (10k+ tokens)
- **Contingency:** If throttling insufficient, implement batching (update every N tokens)

**Risk 2: SignalR Connection Stability**
- **Description:** SignalR connection may drop during long operations
- **Mitigation:**
  - Blazor Server handles reconnection automatically
  - Add reconnection UI indicator
  - Test with slow network conditions
- **Contingency:** Implement connection health monitoring and user notifications

**Risk 3: Memory Leaks from Event Subscriptions**
- **Description:** Components may not properly dispose event subscriptions
- **Mitigation:**
  - Implement `IDisposable` on all subscribing components
  - Use weak event patterns where appropriate
  - Memory profiling during testing
- **Contingency:** Add automated memory leak detection tests

**Risk 4: Large Payload Rendering**
- **Description:** Very large tool results or events may slow UI
- **Mitigation:**
  - Implement collapsible sections by default
  - Limit initial event display to 100 most recent
  - Virtualize long lists (consider Blazor virtualization)
- **Contingency:** Add pagination for event lists if needed

### Integration Risks

**Risk 5: Azure OpenAI SDK Changes**
- **Description:** SDK updates may break streaming implementation
- **Mitigation:**
  - Pin SDK version initially
  - Comprehensive integration tests
  - Monitor SDK release notes
- **Contingency:** Abstract streaming behind interface for easy swapping

**Risk 6: Tool Registry Breaking Changes**
- **Description:** Changes to tool abstractions may affect Tools Overview
- **Mitigation:**
  - Tools Overview uses interface contracts
  - Version tool abstraction interfaces
  - Comprehensive unit tests
- **Contingency:** Adapter pattern to bridge version differences

### Project Risks

**Risk 7: Scope Creep**
- **Description:** Additional features may be requested during Phase 6
- **Mitigation:**
  - Clear scope definition in this document
  - Defer non-essential features to Phase 7+
  - Regular scope reviews
- **Contingency:** Time-box Phase 6 to 30 hours max

**Risk 8: Testing Time Underestimation**
- **Description:** Testing may take longer than estimated
- **Mitigation:**
  - Allocate 25% of timeline to testing
  - Automate where possible (unit/bUnit tests)
  - Parallel manual testing with development
- **Contingency:** Prioritize critical path testing, defer edge cases

---

## Appendix A: File Structure After Phase 6

```
TransparentAiAgent/
├── TransparentAiAgentCore/
│   ├── Domain/
│   │   ├── Enums/
│   │   │   └── TransparencyEventType.cs (modified - new streaming events)
│   │   └── Tools/
│   │       └── ITool.cs (existing)
│   ├── Application/
│   │   ├── Agent/
│   │   │   └── AgentOrchestrator.cs (modified - streaming support)
│   │   └── Tools/
│   │       └── ToolManager.cs (modified - statistics integration)
│   └── Infrastructure/
│       ├── LLM/
│       │   └── AzureOpenAiChatService.cs (modified - streaming callback)
│       ├── Markdown/                                    **NEW**
│       │   ├── MarkdownStreamingBuffer.cs (new)        **NEW**
│       │   ├── IMarkdownRenderer.cs (new)              **NEW**
│       │   ├── MarkdigRenderer.cs (new)                **NEW**
│       │   └── MarkdownStreamingConfig.cs (new)        **NEW**
│       ├── Transparency/
│       │   ├── ITransparencyService.cs (modified - events)
│       │   └── TransparencyService.cs (modified - event notification)
│       └── Tools/
│           ├── IToolUsageStatistics.cs (new)
│           └── ToolUsageStatistics.cs (new)
├── TransparentAiAgentGui/
│   ├── Components/
│   │   ├── Chat/
│   │   │   ├── MessageDisplay.razor (modified - streaming UI + markdown)
│   │   │   ├── MessageDisplay.razor.css (modified - animations + markdown support)
│   │   │   ├── MessageList.razor (existing)
│   │   │   ├── ChatInput.razor (existing)
│   │   │   ├── MarkdownDisplay.razor (new)                   **NEW**
│   │   │   ├── MarkdownDisplay.razor.css (new)               **NEW**
│   │   │   ├── JsonDisplay.razor (new)
│   │   │   └── JsonDisplay.razor.css (new)
│   │   ├── Transparency/
│   │   │   ├── TransparencyViewer.razor (new)
│   │   │   ├── TransparencyViewer.razor.css (new)
│   │   │   ├── TransparencyEventDisplay.razor (new)
│   │   │   └── TransparencyEventDisplay.razor.css (new)
│   │   ├── Tools/
│   │   │   ├── ToolsOverview.razor (new)
│   │   │   ├── ToolsOverview.razor.css (new)
│   │   │   ├── ToolCard.razor (new)
│   │   │   ├── ToolCard.razor.css (new)
│   │   │   ├── ToolDetailsModal.razor (new)
│   │   │   └── ToolDetailsModal.razor.css (new)
│   │   ├── Pages/
│   │   │   ├── Home.razor (existing)
│   │   │   ├── Transparency.razor (new)
│   │   │   └── Tools.razor (new)
│   │   └── Layout/
│   │       ├── MainLayout.razor (modified - script reference)
│   │       └── NavMenu.razor (modified - new links)
│   ├── Services/
│   │   ├── IConversationUIService.cs (modified - streaming methods)
│   │   └── ConversationUIService.cs (modified - streaming implementation)
│   ├── wwwroot/
│   │   └── js/
│   │       └── site.js (new - auto-scroll utility)
│   ├── TransparentAiAgentGui.csproj (modified - Markdig 0.43 package) **NEW**
│   └── Program.cs (modified - service registrations + markdown renderer) **UPDATED**
└── TransparentAiAgentTests/
    ├── Unit/
    │   ├── StreamingCallbackTests.cs (new)
    │   ├── TransparencyServiceTests.cs (new)
    │   ├── ToolUsageStatisticsTests.cs (new)
    │   ├── ConversationUIServiceStreamingTests.cs (new)
    │   ├── MarkdownStreamingBufferTests.cs (new)              **NEW**
    │   └── MarkdigRendererTests.cs (new)                      **NEW**
    └── Component/
        ├── TransparencyViewerTests.cs (new)
        ├── ToolsOverviewTests.cs (new)
        ├── JsonDisplayTests.cs (new)
        ├── MessageDisplayStreamingTests.cs (new)
        ├── MarkdownDisplayTests.cs (new)                      **NEW**
        └── MessageDisplayMarkdownTests.cs (new)               **NEW**
```

**Summary:**
- **Modified Files:** 12 (+2 for markdown: AgentOrchestrator.cs, MessageDisplay.razor, Program.cs, TransparentAiAgentGui.csproj)
- **New Files:** 35 (+10 for markdown: 4 core files + 2 UI components + 4 test files)
- **Total Files Changed:** 47 (+12 from original estimate)

---

## Appendix B: Configuration Changes

### appsettings.json Updates

Add configuration for streaming, transparency, and markdown:

```json
{
  "Transparency": {
    "MaxEventsInMemory": 1000,
    "EnableRealTimeEvents": true,
    "LogStreamingTokens": false
  },
  "Streaming": {
    "ThrottleIntervalMs": 50,
    "MaxTokensPerUpdate": 10,
    "EnableCursor": true
  },
  "Markdown": {                                      // **NEW**
    "EnableRendering": true,                          // **NEW**
    "EnableSemanticBuffering": true,                  // **NEW**
    "MaxBufferTimeMs": 1000,                          // **NEW**
    "EnableSyntaxHighlighting": true,                 // **NEW**
    "BufferToolCalls": true                           // **NEW**
  },                                                  // **NEW**
  "Tools": {
    "EnableUsageStatistics": true,
    "StatisticsRetentionDays": 30
  }
}
```

### Configuration Descriptions

**Markdown Section** (NEW):
- `EnableRendering`: Enable markdown-to-HTML conversion for assistant messages
- `EnableSemanticBuffering`: Use semantic-boundary-aware buffering during streaming
  - `true`: Buffer incomplete markdown constructs (prevents glitches)
  - `false`: Use simple time-based throttling (may show partial markdown)
- `MaxBufferTimeMs`: Maximum time to buffer incomplete constructs (default: 1000ms)
  - Prevents indefinite buffering on malformed markdown
  - Content will force-flush after this timeout
- `EnableSyntaxHighlighting`: Enable code syntax highlighting in markdown code blocks
- `BufferToolCalls`: Buffer tool calls until complete during streaming
  - `true`: Tool calls hidden until fully received
  - `false`: Tool calls stream incrementally (may show partial XML/JSON)

---

## Appendix C: Code Style Guidelines

**Blazor Component Naming:**
- Components: PascalCase (e.g., `TransparencyViewer.razor`)
- Parameters: PascalCase with `[Parameter]` attribute
- Private fields: camelCase with underscore prefix (e.g., `_events`)
- Events: PascalCase with EventHandler suffix (e.g., `FiltersChanged`)

**CSS Naming:**
- Classes: kebab-case (e.g., `.transparency-viewer`)
- BEM notation for complex components (e.g., `.viewer__header--active`)
- Scoped CSS preferred (`.razor.css` files)

**C# Conventions:**
- Interfaces: PascalCase with `I` prefix (e.g., `ITransparencyService`)
- Async methods: Suffix with `Async` (e.g., `GetEventsAsync`)
- Events: PascalCase (e.g., `EventLogged`)
- Event args: Custom class with EventArgs suffix

**Testing Conventions:**
- Test methods: `MethodName_Scenario_ExpectedOutcome`
- Test classes: `ClassNameTests`
- Arrange-Act-Assert pattern for all tests
- One logical assertion per test

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-01 | AI Assistant | Initial comprehensive plan created |

---

**End of Phase 6 Detailed Implementation Plan**
