# Agent UI Control Architecture

## 📐 Technical Specification for Interactive Teaching Mode Layer

This document provides the complete technical architecture for the Agent UI Control system, which enables agents to dynamically control UI components through built-in tools while maintaining full transparency.

---

## 🏗️ System Overview

### Architectural Layers

The UI Control system integrates with the existing Clean Architecture as a new **cross-cutting concern** that bridges the Application and Presentation layers:

```
┌─────────────────────────────────────────────────────────────────┐
│                     Presentation Layer                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Blazor Components (Pages & Components)                 │   │
│  │  - Home.razor                                           │   │
│  │  - MessageList.razor + MessageFilterControls.razor     │   │
│  │  - TransparencyViewer.razor                            │   │
│  │  - ToolsOverview.razor                                 │   │
│  │  - Configuration.razor                                 │   │
│  │  - NavMenu.razor (Mode Toggle)                         │   │
│  └────────────┬────────────────────────────────────────────┘   │
│               │ Subscribe to Events                             │
│               ├─► MessagesChanged                               │
│               ├─► ProcessingStateChanged                        │
│               ├─► UIStateChanged ← NEW                          │
│               └─► AppModeChanged ← NEW                          │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  UI Services (Scoped per SignalR connection)             │  │
│  │  - ConversationUIService                                 │  │
│  │  - UIControlService ← NEW                                │  │
│  │  - AppModeService ← NEW                                  │  │
│  └────────────┬─────────────────────────────────────────────┘  │
└───────────────┼─────────────────────────────────────────────────┘
                │
┌───────────────▼─────────────────────────────────────────────────┐
│                     Application Layer                           │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  AgentOrchestrator                                       │  │
│  │  - Coordinates tool execution                            │  │
│  │  - No awareness of UI control specifics                  │  │
│  └────────────┬─────────────────────────────────────────────┘  │
│               │                                                  │
│  ┌────────────▼─────────────────────────────────────────────┐  │
│  │  ToolManager                                             │  │
│  │  - Routes tool calls to appropriate executors            │  │
│  │  - Handles BuiltInUIControl source type ← NEW            │  │
│  └────────────┬─────────────────────────────────────────────┘  │
└───────────────┼─────────────────────────────────────────────────┘
                │
┌───────────────▼─────────────────────────────────────────────────┐
│                     Infrastructure Layer                        │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Tool Execution Infrastructure                           │  │
│  │                                                           │  │
│  │  ┌────────────────────────────────────────────────────┐ │  │
│  │  │ ToolRegistryComposite                              │ │  │
│  │  │  ├─ McpToolRegistry (MCP servers)                  │ │  │
│  │  │  └─ BuiltInUIControlToolRegistry ← NEW             │ │  │
│  │  └────────────────────────────────────────────────────┘ │  │
│  │                                                           │  │
│  │  ┌────────────────────────────────────────────────────┐ │  │
│  │  │ Tool Executors                                     │ │  │
│  │  │  ├─ McpToolExecutor                                │ │  │
│  │  │  └─ UIControlToolExecutor ← NEW                    │ │  │
│  │  └────────────────────────────────────────────────────┘ │  │
│  │                                                           │  │
│  │  ┌────────────────────────────────────────────────────┐ │  │
│  │  │ Built-In UI Control Tools                          │ │  │
│  │  │  ├─ UiControlChatFilterTool                        │ │  │
│  │  │  ├─ UiControlFilterVisibilityTool                  │ │  │
│  │  │  ├─ UiGetStateTool                                 │ │  │
│  │  │  ├─ UiControlTransparencyViewerTool                │ │  │
│  │  │  ├─ UiControlToolsPanelTool                        │ │  │
│  │  │  ├─ UiControlContextIndicatorsTool                 │ │  │
│  │  │  └─ UiControlConfigurationTool                     │ │  │
│  │  └────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘

Key Data Flow:
1. Agent calls UI control tool (e.g., "ui_control_chat_filter")
2. ToolManager routes to UIControlToolExecutor
3. Executor validates request and calls UIControlService
4. UIControlService updates UIState and fires UIStateChanged event
5. UI components receive event and call StateHasChanged()
6. SignalR pushes DOM updates to browser
7. Tool execution returns success/failure to agent
```

### Key Design Decisions

1. **Built-In Tools, Not MCP Servers**
   - UI control tools are part of the application, not external MCP servers
   - Eliminates network latency and external dependencies
   - Ensures reliability (>99.9% success rate)
   - Uses MCP protocol for consistency (agent perspective)

2. **Scoped Services for UI State**
   - `UIControlService` registered as Scoped (per SignalR connection)
   - Each user session has independent UI state
   - State isolated between concurrent users

3. **Event-Driven UI Updates**
   - Follows existing pattern from Phase 6 (streaming) and Phase 7 (config)
   - Components subscribe to `UIStateChanged` event
   - Decoupled architecture—UI components don't know about tools

4. **Clean Separation of Concerns**
   - Domain layer defines interfaces (`IUIControlService`)
   - Infrastructure implements tools
   - Presentation implements UI components
   - Application layer remains agnostic

---

## 🛠️ Component Specifications

### 1. UIState Model

**File:** `TransparentAiAgentCore/Domain/UIControl/UIState.cs`

```csharp
namespace TransparentAiAgentCore.Domain.UIControl;

/// <summary>
/// Represents the complete state of all UI components controllable by the agent.
/// Immutable by design - updates create new instances.
/// </summary>
public record UIState
{
    /// <summary>
    /// Configuration for chat message filtering.
    /// </summary>
    public ChatFilterState ChatFilter { get; init; } = new();

    /// <summary>
    /// Configuration for transparency viewer.
    /// </summary>
    public TransparencyViewerState TransparencyViewer { get; init; } = new();

    /// <summary>
    /// Configuration for tools overview panel.
    /// </summary>
    public ToolsPanelState ToolsPanel { get; init; } = new();

    /// <summary>
    /// Configuration for context status indicators.
    /// </summary>
    public ContextIndicatorsState ContextIndicators { get; init; } = new();

    /// <summary>
    /// Configuration for the configuration page.
    /// </summary>
    public ConfigurationPageState ConfigurationPage { get; init; } = new();

    /// <summary>
    /// Current application mode (Normal or Teaching).
    /// </summary>
    public AppMode CurrentMode { get; init; } = AppMode.Normal;

    /// <summary>
    /// Creates a default UI state for Normal Mode (all controls visible).
    /// </summary>
    public static UIState DefaultNormalMode() => new()
    {
        ChatFilter = new ChatFilterState
        {
            ShowUserMessages = true,
            ShowAssistantMessages = true,
            ShowSystemMessages = true,
            ShowToolCalls = true,
            ShowToolResults = true,
            ShowTruncatedMessages = true,
            FilterControlsVisible = true
        },
        CurrentMode = AppMode.Normal
    };

    /// <summary>
    /// Creates a default UI state for Teaching Mode (controls hidden).
    /// </summary>
    public static UIState DefaultTeachingMode() => new()
    {
        ChatFilter = new ChatFilterState
        {
            ShowUserMessages = true,
            ShowAssistantMessages = true,
            ShowSystemMessages = false,
            ShowToolCalls = false,
            ShowToolResults = false,
            ShowTruncatedMessages = true,
            FilterControlsVisible = false
        },
        CurrentMode = AppMode.Teaching
    };
}

/// <summary>
/// Chat history filtering configuration.
/// </summary>
public record ChatFilterState
{
    public bool ShowUserMessages { get; init; } = true;
    public bool ShowAssistantMessages { get; init; } = true;
    public bool ShowSystemMessages { get; init; } = true;
    public bool ShowToolCalls { get; init; } = true;
    public bool ShowToolResults { get; init; } = true;
    public bool ShowTruncatedMessages { get; init; } = true;
    public bool FilterControlsVisible { get; init; } = true;
}

/// <summary>
/// Transparency viewer configuration.
/// </summary>
public record TransparencyViewerState
{
    public bool Visible { get; init; } = true;
    public List<string> EventTypeFilters { get; init; } = new();
    public bool ShowTimestamps { get; init; } = true;
}

/// <summary>
/// Tools overview panel configuration.
/// </summary>
public record ToolsPanelState
{
    public bool Visible { get; init; } = true;
    public List<string> ExpandedTools { get; init; } = new();
    public string? HighlightedTool { get; init; } = null;
}

/// <summary>
/// Context status indicators configuration.
/// </summary>
public record ContextIndicatorsState
{
    public bool Visible { get; init; } = true;
    public bool Highlighted { get; init; } = false;
}

/// <summary>
/// Configuration page state.
/// </summary>
public record ConfigurationPageState
{
    public bool NavigateRequested { get; init; } = false;
    public string? HighlightedSection { get; init; } = null;
}

/// <summary>
/// Application mode enumeration.
/// </summary>
public enum AppMode
{
    /// <summary>
    /// Normal transparent agent mode - all controls visible.
    /// </summary>
    Normal,

    /// <summary>
    /// Teaching mode - progressive reveal of features.
    /// </summary>
    Teaching
}
```

---

### 2. IUIControlService Interface

**File:** `TransparentAiAgentCore/Domain/UIControl/IUIControlService.cs`

```csharp
namespace TransparentAiAgentCore.Domain.UIControl;

/// <summary>
/// Service for managing UI state and handling agent-driven UI control.
/// </summary>
public interface IUIControlService
{
    /// <summary>
    /// Raised when the UI state changes.
    /// </summary>
    event EventHandler<UIState>? UIStateChanged;

    /// <summary>
    /// Gets the current UI state.
    /// </summary>
    UIState GetCurrentState();

    /// <summary>
    /// Updates the chat filter configuration.
    /// </summary>
    /// <param name="showUserMessages">Whether to show user messages.</param>
    /// <param name="showAssistantMessages">Whether to show assistant messages.</param>
    /// <param name="showSystemMessages">Whether to show system messages.</param>
    /// <param name="showToolCalls">Whether to show tool calls.</param>
    /// <param name="showToolResults">Whether to show tool results.</param>
    /// <param name="showTruncatedMessages">Whether to show truncated messages.</param>
    /// <returns>Success result with updated state, or failure with error message.</returns>
    Result<UIState> UpdateChatFilter(
        bool? showUserMessages = null,
        bool? showAssistantMessages = null,
        bool? showSystemMessages = null,
        bool? showToolCalls = null,
        bool? showToolResults = null,
        bool? showTruncatedMessages = null);

    /// <summary>
    /// Controls visibility of filter controls themselves.
    /// </summary>
    Result<UIState> UpdateFilterControlVisibility(bool visible);

    /// <summary>
    /// Updates transparency viewer configuration.
    /// </summary>
    Result<UIState> UpdateTransparencyViewer(
        bool? visible = null,
        List<string>? eventTypeFilters = null,
        bool? showTimestamps = null);

    /// <summary>
    /// Updates tools panel configuration.
    /// </summary>
    Result<UIState> UpdateToolsPanel(
        bool? visible = null,
        string? expandTool = null,
        string? collapseTool = null,
        string? highlightTool = null);

    /// <summary>
    /// Updates context indicators configuration.
    /// </summary>
    Result<UIState> UpdateContextIndicators(
        bool? visible = null,
        bool? highlighted = null);

    /// <summary>
    /// Updates configuration page state.
    /// </summary>
    Result<UIState> UpdateConfigurationPage(
        bool? navigate = null,
        string? highlightSection = null);

    /// <summary>
    /// Resets UI state to defaults based on current mode.
    /// </summary>
    Result<UIState> ResetToDefaults();

    /// <summary>
    /// Switches application mode and updates UI state accordingly.
    /// </summary>
    Result<UIState> SwitchMode(AppMode newMode);
}

/// <summary>
/// Result type for UI control operations.
/// </summary>
public record Result<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }

    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
    public static Result<T> Fail(string error) => new() { Success = false, ErrorMessage = error };
}
```

---

### 3. UIControlService Implementation

**File:** `TransparentAiAgentGui/Services/UIControlService.cs`

```csharp
namespace TransparentAiAgentGui.Services;

/// <summary>
/// Implementation of UI control service managing UI state and events.
/// Registered as Scoped service (per SignalR connection).
/// </summary>
public class UIControlService : IUIControlService
{
    private UIState _currentState;
    private readonly ITransparencyService _transparencyService;
    private readonly ILogger<UIControlService> _logger;

    public event EventHandler<UIState>? UIStateChanged;

    public UIControlService(
        ITransparencyService transparencyService,
        ILogger<UIControlService> logger)
    {
        _transparencyService = transparencyService;
        _logger = logger;
        _currentState = UIState.DefaultNormalMode();
    }

    public UIState GetCurrentState() => _currentState;

    public Result<UIState> UpdateChatFilter(
        bool? showUserMessages = null,
        bool? showAssistantMessages = null,
        bool? showSystemMessages = null,
        bool? showToolCalls = null,
        bool? showToolResults = null,
        bool? showTruncatedMessages = null)
    {
        try
        {
            var newChatFilter = _currentState.ChatFilter with
            {
                ShowUserMessages = showUserMessages ?? _currentState.ChatFilter.ShowUserMessages,
                ShowAssistantMessages = showAssistantMessages ?? _currentState.ChatFilter.ShowAssistantMessages,
                ShowSystemMessages = showSystemMessages ?? _currentState.ChatFilter.ShowSystemMessages,
                ShowToolCalls = showToolCalls ?? _currentState.ChatFilter.ShowToolCalls,
                ShowToolResults = showToolResults ?? _currentState.ChatFilter.ShowToolResults,
                ShowTruncatedMessages = showTruncatedMessages ?? _currentState.ChatFilter.ShowTruncatedMessages
            };

            _currentState = _currentState with { ChatFilter = newChatFilter };

            LogUIControlEvent("ChatFilterUpdated", new
            {
                newChatFilter.ShowUserMessages,
                newChatFilter.ShowAssistantMessages,
                newChatFilter.ShowSystemMessages,
                newChatFilter.ShowToolCalls,
                newChatFilter.ShowToolResults,
                newChatFilter.ShowTruncatedMessages
            });

            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat filter");
            return Result<UIState>.Fail($"Failed to update chat filter: {ex.Message}");
        }
    }

    public Result<UIState> UpdateFilterControlVisibility(bool visible)
    {
        try
        {
            var newChatFilter = _currentState.ChatFilter with { FilterControlsVisible = visible };
            _currentState = _currentState with { ChatFilter = newChatFilter };

            LogUIControlEvent("FilterControlVisibilityChanged", new { visible });
            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update filter control visibility");
            return Result<UIState>.Fail($"Failed to update visibility: {ex.Message}");
        }
    }

    // Similar implementations for other Update methods...
    // (UpdateTransparencyViewer, UpdateToolsPanel, etc.)

    public Result<UIState> SwitchMode(AppMode newMode)
    {
        try
        {
            _currentState = newMode == AppMode.Teaching
                ? UIState.DefaultTeachingMode()
                : UIState.DefaultNormalMode();

            LogUIControlEvent("AppModeChanged", new { Mode = newMode.ToString() });
            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch mode");
            return Result<UIState>.Fail($"Failed to switch mode: {ex.Message}");
        }
    }

    private void LogUIControlEvent(string eventName, object data)
    {
        _transparencyService.LogEvent(
            TransparencyEventType.UIControlAction,
            data,
            $"UI Control: {eventName}");
    }
}
```

---

### 4. Built-In UI Control Tool Registry

**File:** `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/BuiltInUIControlToolRegistry.cs`

```csharp
namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Registry for built-in UI control tools.
/// These tools appear as MCP tools to the agent but are executed locally.
/// </summary>
public class BuiltInUIControlToolRegistry : IToolRegistry
{
    private readonly List<Tool> _tools = new();

    public BuiltInUIControlToolRegistry()
    {
        RegisterAllTools();
    }

    public Task<IEnumerable<Tool>> GetToolsAsync()
    {
        return Task.FromResult<IEnumerable<Tool>>(_tools);
    }

    public Task<Tool?> GetToolByNameAsync(string toolName)
    {
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        return Task.FromResult(tool);
    }

    private void RegisterAllTools()
    {
        _tools.Add(CreateChatFilterTool());
        _tools.Add(CreateFilterVisibilityTool());
        _tools.Add(CreateGetStateTool());
        _tools.Add(CreateTransparencyViewerTool());
        _tools.Add(CreateToolsPanelTool());
        _tools.Add(CreateContextIndicatorsTool());
        _tools.Add(CreateConfigurationTool());
    }

    private Tool CreateChatFilterTool() => new()
    {
        Name = "ui_control_chat_filter",
        Description = "Control which message types are visible in the chat history. Use this to focus the user's attention on specific aspects of the conversation (e.g., show system messages to explain behavior, hide tool calls to reduce noise).",
        InputSchema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["show_user_messages"] = new { type = "boolean", description = "Show user messages in chat history" },
                ["show_assistant_messages"] = new { type = "boolean", description = "Show assistant messages in chat history" },
                ["show_system_messages"] = new { type = "boolean", description = "Show system messages (system prompt) in chat history" },
                ["show_tool_calls"] = new { type = "boolean", description = "Show tool call messages in chat history" },
                ["show_tool_results"] = new { type = "boolean", description = "Show tool result messages in chat history" },
                ["show_truncated_messages"] = new { type = "boolean", description = "Show messages that have been truncated from context" }
            }
        },
        SourceType = ToolSourceType.BuiltInUIControl
    };

    private Tool CreateFilterVisibilityTool() => new()
    {
        Name = "ui_control_filter_visibility",
        Description = "Show or hide the filter control checkboxes themselves. Useful in Teaching Mode to progressively reveal UI controls. Once visible, users can interact with controls directly.",
        InputSchema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["visible"] = new { type = "boolean", description = "Whether filter controls should be visible to the user" }
            },
            Required = new List<string> { "visible" }
        },
        SourceType = ToolSourceType.BuiltInUIControl
    };

    private Tool CreateGetStateTool() => new()
    {
        Name = "ui_get_state",
        Description = "Get the current state of UI components to understand what the user is currently seeing. Use this to check UI state before making changes or to provide context-aware responses.",
        InputSchema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["component"] = new
                {
                    type = "string",
                    @enum = new[] { "all", "chat_filter", "transparency_viewer", "tools_panel", "context_indicators", "configuration" },
                    description = "Which component's state to retrieve. Use 'all' for complete UI state."
                }
            }
        },
        SourceType = ToolSourceType.BuiltInUIControl
    };

    private Tool CreateTransparencyViewerTool() => new()
    {
        Name = "ui_control_transparency_viewer",
        Description = "Control the transparency viewer panel that shows real-time event logging. Can show/hide viewer and filter specific event types.",
        InputSchema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["visible"] = new { type = "boolean", description = "Show or hide the transparency viewer panel" },
                ["event_type_filters"] = new
                {
                    type = "array",
                    items = new { type = "string" },
                    description = "List of event types to display (e.g., ['LLMRequest', 'ToolCallStarted']). Empty array shows all."
                },
                ["show_timestamps"] = new { type = "boolean", description = "Show timestamps for each event" }
            }
        },
        SourceType = ToolSourceType.BuiltInUIControl
    };

    private Tool CreateToolsPanelTool() => new()
    {
        Name = "ui_control_tools_panel",
        Description = "Control the Tools Overview panel. Can expand/collapse specific tools, highlight tools, or navigate to the Tools page.",
        InputSchema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["visible"] = new { type = "boolean", description = "Show or hide the tools panel" },
                ["expand_tool"] = new { type = "string", description = "Tool name to expand (shows full definition)" },
                ["collapse_tool"] = new { type = "string", description = "Tool name to collapse" },
                ["highlight_tool"] = new { type = "string", description = "Tool name to highlight (visual emphasis)" }
            }
        },
        SourceType = ToolSourceType.BuiltInUIControl
    };

    private Tool CreateContextIndicatorsTool() => new()
    {
        Name = "ui_control_context_indicators",
        Description = "Control the context status indicators (✅ In Context / ⚠️ Truncated) shown on messages. Useful for teaching about context windows and truncation.",
        InputSchema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["visible"] = new { type = "boolean", description = "Show or hide context status indicators" },
                ["highlighted"] = new { type = "boolean", description = "Add visual emphasis to context indicators (e.g., glow, animation)" }
            }
        },
        SourceType = ToolSourceType.BuiltInUIControl
    };

    private Tool CreateConfigurationTool() => new()
    {
        Name = "ui_control_configuration",
        Description = "Control the Configuration page. Can navigate to the page and highlight specific sections (e.g., system prompt, LLM parameters).",
        InputSchema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["navigate"] = new { type = "boolean", description = "Navigate to the Configuration page" },
                ["highlight_section"] = new
                {
                    type = "string",
                    @enum = new[] { "system-prompt", "llm-parameters", "tools-config" },
                    description = "Section ID to highlight on the Configuration page"
                }
            }
        },
        SourceType = ToolSourceType.BuiltInUIControl
    };
}
```

---

### 5. UI Control Tool Executor

**File:** `TransparentAiAgentCore/Infrastructure/Tools/BuiltInUIControl/UIControlToolExecutor.cs`

```csharp
namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Executes built-in UI control tools by routing to UIControlService.
/// </summary>
public class UIControlToolExecutor : IToolExecutor
{
    private readonly IUIControlService _uiControlService;
    private readonly ILogger<UIControlToolExecutor> _logger;

    public UIControlToolExecutor(
        IUIControlService uiControlService,
        ILogger<UIControlToolExecutor> logger)
    {
        _uiControlService = uiControlService;
        _logger = logger;
    }

    public Task<ToolResult> ExecuteToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken)
    {
        try
        {
            var result = toolName switch
            {
                "ui_control_chat_filter" => ExecuteChatFilterTool(arguments),
                "ui_control_filter_visibility" => ExecuteFilterVisibilityTool(arguments),
                "ui_get_state" => ExecuteGetStateTool(arguments),
                "ui_control_transparency_viewer" => ExecuteTransparencyViewerTool(arguments),
                "ui_control_tools_panel" => ExecuteToolsPanelTool(arguments),
                "ui_control_context_indicators" => ExecuteContextIndicatorsTool(arguments),
                "ui_control_configuration" => ExecuteConfigurationTool(arguments),
                _ => throw new ArgumentException($"Unknown UI control tool: {toolName}")
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute UI control tool: {ToolName}", toolName);
            return Task.FromResult(ToolResult.Failure($"Tool execution failed: {ex.Message}"));
        }
    }

    private ToolResult ExecuteChatFilterTool(JsonElement arguments)
    {
        var showUser = GetOptionalBool(arguments, "show_user_messages");
        var showAssistant = GetOptionalBool(arguments, "show_assistant_messages");
        var showSystem = GetOptionalBool(arguments, "show_system_messages");
        var showToolCalls = GetOptionalBool(arguments, "show_tool_calls");
        var showToolResults = GetOptionalBool(arguments, "show_tool_results");
        var showTruncated = GetOptionalBool(arguments, "show_truncated_messages");

        var result = _uiControlService.UpdateChatFilter(
            showUser, showAssistant, showSystem,
            showToolCalls, showToolResults, showTruncated);

        return result.Success
            ? ToolResult.Success("Chat filter updated successfully", SerializeState(result.Value!))
            : ToolResult.Failure(result.ErrorMessage!);
    }

    private ToolResult ExecuteGetStateTool(JsonElement arguments)
    {
        var component = GetOptionalString(arguments, "component") ?? "all";
        var currentState = _uiControlService.GetCurrentState();

        var stateData = component switch
        {
            "all" => SerializeState(currentState),
            "chat_filter" => JsonSerializer.SerializeToDocument(currentState.ChatFilter),
            "transparency_viewer" => JsonSerializer.SerializeToDocument(currentState.TransparencyViewer),
            "tools_panel" => JsonSerializer.SerializeToDocument(currentState.ToolsPanel),
            "context_indicators" => JsonSerializer.SerializeToDocument(currentState.ContextIndicators),
            "configuration" => JsonSerializer.SerializeToDocument(currentState.ConfigurationPage),
            _ => throw new ArgumentException($"Unknown component: {component}")
        };

        return ToolResult.Success("Current UI state retrieved", stateData);
    }

    // Helper methods for parsing JSON arguments
    private bool? GetOptionalBool(JsonElement args, string propertyName)
    {
        return args.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False
            ? prop.GetBoolean()
            : null;
    }

    private string? GetOptionalString(JsonElement args, string propertyName)
    {
        return args.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private JsonDocument SerializeState(UIState state)
    {
        return JsonSerializer.SerializeToDocument(state, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
```

---

### 6. ToolSourceType Extension

**File:** `TransparentAiAgentCore/Domain/Tools/ToolSourceType.cs` (modify existing)

```csharp
namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Identifies the source/type of a tool.
/// </summary>
public enum ToolSourceType
{
    /// <summary>
    /// Tool from an external MCP server.
    /// </summary>
    MCP,

    /// <summary>
    /// Built-in UI control tool (local execution).
    /// </summary>
    BuiltInUIControl
}
```

---

### 7. AppModeService

**File:** `TransparentAiAgentGui/Services/AppModeService.cs`

```csharp
namespace TransparentAiAgentGui.Services;

/// <summary>
/// Manages application mode switching (Normal vs Teaching).
/// Coordinates mode changes across services (UI, Conversation, etc.).
/// </summary>
public class AppModeService
{
    private AppMode _currentMode = AppMode.Normal;
    private readonly IUIControlService _uiControlService;
    private readonly IConversationUIService _conversationService;
    private readonly IConfigurationService _configService;
    private readonly ILogger<AppModeService> _logger;

    public event EventHandler<AppMode>? ModeChanged;

    public AppMode CurrentMode => _currentMode;

    public AppModeService(
        IUIControlService uiControlService,
        IConversationUIService conversationService,
        IConfigurationService configService,
        ILogger<AppModeService> logger)
    {
        _uiControlService = uiControlService;
        _conversationService = conversationService;
        _configService = configService;
        _logger = logger;
    }

    public async Task<bool> SwitchModeAsync(AppMode newMode, bool clearConversation = false)
    {
        try
        {
            if (_currentMode == newMode)
                return true;

            _logger.LogInformation("Switching mode from {OldMode} to {NewMode}", _currentMode, newMode);

            // Update UI state
            var uiResult = _uiControlService.SwitchMode(newMode);
            if (!uiResult.Success)
            {
                _logger.LogError("Failed to switch UI mode: {Error}", uiResult.ErrorMessage);
                return false;
            }

            // Update system prompt if Teaching Mode
            if (newMode == AppMode.Teaching)
            {
                await _conversationService.UpdateSystemPromptAsync(GetTeachingModeSystemPrompt());
            }
            else
            {
                // Restore original system prompt from config
                var originalPrompt = _configService.GetSystemPrompt();
                await _conversationService.UpdateSystemPromptAsync(originalPrompt);
            }

            // Clear conversation if requested
            if (clearConversation)
            {
                await _conversationService.ClearConversationAsync();
            }

            _currentMode = newMode;
            ModeChanged?.Invoke(this, newMode);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch mode");
            return false;
        }
    }

    private string GetTeachingModeSystemPrompt()
    {
        return @"You are a transparent AI assistant in Teaching Mode. Your primary goal is to teach users about transparent AI concepts through interactive exploration.

Teaching Guidelines:
1. Use UI control tools to progressively reveal features as they become relevant
2. Always explain WHY you're showing something before revealing it
3. Ask permission before revealing new UI elements: ""Would you like me to show you...?""
4. After revealing a feature, encourage the user to interact with it
5. Build on previous reveals - check current UI state before introducing new features
6. Keep explanations concise and conversational

Available UI Control Tools:
- ui_control_chat_filter: Show/hide message types (system, tools, etc.)
- ui_control_filter_visibility: Reveal the filter controls themselves
- ui_get_state: Check what the user currently sees
- ui_control_transparency_viewer: Control event logging panel
- ui_control_tools_panel: Highlight and explain tools
- ui_control_context_indicators: Teach about context windows
- ui_control_configuration: Guide through configuration options

Teaching Topics:
- System messages and their role
- Tool calls and how agents take actions
- Context windows and message truncation
- Transparency logging and debugging
- Configuration and customization
- Token usage and costs

Remember: You're teaching, not lecturing. Be friendly, interactive, and responsive to the user's curiosity.";
    }
}
```

---

## 📊 Data Flow Diagrams

### Flow 1: Agent Controls Chat Filter

```
┌─────────────┐
│  User asks  │
│  question   │
└──────┬──────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  Agent processes question and decides to show        │
│  system message to explain behavior                  │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  Agent generates tool call:                          │
│  {                                                   │
│    "tool": "ui_control_chat_filter",                │
│    "arguments": {                                   │
│      "show_system_messages": true                   │
│    }                                                 │
│  }                                                   │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  ToolManager routes to UIControlToolExecutor         │
│  based on ToolSourceType.BuiltInUIControl            │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  UIControlToolExecutor parses arguments and calls:   │
│  _uiControlService.UpdateChatFilter(                 │
│    showSystemMessages: true                          │
│  )                                                   │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  UIControlService:                                   │
│  1. Updates _currentState.ChatFilter                 │
│  2. Logs transparency event                          │
│  3. Fires UIStateChanged event                       │
└──────┬───────────────────────────────────────────────┘
       │
       ├──────────────────┬──────────────────┐
       ▼                  ▼                  ▼
┌────────────┐  ┌──────────────────┐  ┌────────────────┐
│MessageList │  │  Home.razor      │  │ Other          │
│ receives   │  │  receives event  │  │ subscribers    │
│ UIState    │  │                  │  │                │
│ Changed    │  │                  │  │                │
└─────┬──────┘  └────────┬─────────┘  └────────┬───────┘
      │                  │                     │
      ▼                  ▼                     ▼
┌────────────────────────────────────────────────────┐
│  All components call StateHasChanged()             │
└──────┬─────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  Blazor calculates DOM diff and sends via SignalR   │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  Browser updates UI:                                 │
│  - System message becomes visible                    │
│  - Filter checkbox updated to checked                │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  Tool execution returns success to agent:            │
│  {                                                   │
│    "success": true,                                  │
│    "content": "Chat filter updated successfully",    │
│    "data": { "chatFilter": {...} }                   │
│  }                                                   │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  Agent continues response:                           │
│  "See that message at the top? That's my system      │
│  prompt—it's the instructions that guide me."        │
└──────────────────────────────────────────────────────┘
```

### Flow 2: User Toggles Filter Manually

```
┌─────────────────────────────────────┐
│  User clicks "Show Tool Calls"      │
│  checkbox in MessageFilterControls  │
└──────┬──────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  MessageFilterControls.OnCheckboxChanged()           │
│  calls ConversationService.UpdateChatFilter()        │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  ConversationService delegates to UIControlService   │
│  (UI changes go through same service as agent)       │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  UIControlService updates state and fires event      │
│  (same as agent-driven flow)                         │
└──────┬───────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│  MessageList re-renders with tool calls visible      │
└──────────────────────────────────────────────────────┘

NOTE: User changes and agent changes flow through the same service,
ensuring consistency and proper event propagation.
```

---

## 🔧 Component Integration Patterns

### Pattern 1: UI Component Subscribing to State Changes

**Example: MessageList.razor**

```razor
@inject IUIControlService UIControlService
@implements IDisposable

<div class="message-list">
    @if (UIControlService.GetCurrentState().ChatFilter.FilterControlsVisible)
    {
        <MessageFilterControls />
    }

    @foreach (var message in FilteredMessages)
    {
        <MessageDisplay Message="message" />
    }
</div>

@code {
    [Parameter]
    public List<UIMessage>? Messages { get; set; }

    private UIState _uiState = UIState.DefaultNormalMode();

    private IEnumerable<UIMessage> FilteredMessages =>
        Messages?.Where(ShouldDisplayMessage) ?? Enumerable.Empty<UIMessage>();

    protected override void OnInitialized()
    {
        UIControlService.UIStateChanged += OnUIStateChanged;
        _uiState = UIControlService.GetCurrentState();
    }

    private void OnUIStateChanged(object? sender, UIState newState)
    {
        InvokeAsync(() =>
        {
            _uiState = newState;
            StateHasChanged();
        });
    }

    private bool ShouldDisplayMessage(UIMessage message)
    {
        var filter = _uiState.ChatFilter;

        return message.Role switch
        {
            MessageRole.User => filter.ShowUserMessages,
            MessageRole.Assistant => filter.ShowAssistantMessages,
            MessageRole.System => filter.ShowSystemMessages,
            _ when message.IsToolCall => filter.ShowToolCalls,
            _ when message.IsToolResult => filter.ShowToolResults,
            _ => true
        } && (filter.ShowTruncatedMessages || message.ContextStatus != MessageContextStatus.TruncatedFromContext);
    }

    public void Dispose()
    {
        UIControlService.UIStateChanged -= OnUIStateChanged;
    }
}
```

### Pattern 2: Filter Controls Component

**Example: MessageFilterControls.razor**

```razor
@inject IUIControlService UIControlService

<div class="filter-controls">
    <h4>Message Filters</h4>

    <label>
        <input type="checkbox"
               checked="@_currentFilter.ShowSystemMessages"
               @onchange="@(e => UpdateFilter(showSystemMessages: (bool)e.Value!))" />
        Show System Messages
    </label>

    <label>
        <input type="checkbox"
               checked="@_currentFilter.ShowToolCalls"
               @onchange="@(e => UpdateFilter(showToolCalls: (bool)e.Value!))" />
        Show Tool Calls
    </label>

    <label>
        <input type="checkbox"
               checked="@_currentFilter.ShowToolResults"
               @onchange="@(e => UpdateFilter(showToolResults: (bool)e.Value!))" />
        Show Tool Results
    </label>

    <!-- More checkboxes... -->
</div>

@code {
    private ChatFilterState _currentFilter = new();

    protected override void OnInitialized()
    {
        UIControlService.UIStateChanged += OnUIStateChanged;
        _currentFilter = UIControlService.GetCurrentState().ChatFilter;
    }

    private void OnUIStateChanged(object? sender, UIState newState)
    {
        InvokeAsync(() =>
        {
            _currentFilter = newState.ChatFilter;
            StateHasChanged();
        });
    }

    private void UpdateFilter(
        bool? showSystemMessages = null,
        bool? showToolCalls = null,
        bool? showToolResults = null)
    {
        UIControlService.UpdateChatFilter(
            showSystemMessages: showSystemMessages,
            showToolCalls: showToolCalls,
            showToolResults: showToolResults);
    }

    public void Dispose()
    {
        UIControlService.UIStateChanged -= OnUIStateChanged;
    }
}
```

---

## 🧪 Testing Strategy

### Unit Tests

**UIControlServiceTests.cs**

```csharp
[TestClass]
public class UIControlServiceTests
{
    [TestMethod]
    public void UpdateChatFilter_UpdatesStateAndFiresEvent()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var mockLogger = new Mock<ILogger<UIControlService>>();
        var service = new UIControlService(mockTransparency.Object, mockLogger.Object);

        UIState? capturedState = null;
        service.UIStateChanged += (sender, state) => capturedState = state;

        // Act
        var result = service.UpdateChatFilter(showSystemMessages: true, showToolCalls: false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(capturedState);
        Assert.IsTrue(capturedState.ChatFilter.ShowSystemMessages);
        Assert.IsFalse(capturedState.ChatFilter.ShowToolCalls);
    }

    [TestMethod]
    public void SwitchMode_ToTeachingMode_HidesControls()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.SwitchMode(AppMode.Teaching);

        // Assert
        Assert.IsTrue(result.Success);
        var state = service.GetCurrentState();
        Assert.AreEqual(AppMode.Teaching, state.CurrentMode);
        Assert.IsFalse(state.ChatFilter.FilterControlsVisible);
        Assert.IsFalse(state.ChatFilter.ShowSystemMessages);
    }
}
```

### Component Tests (bUnit)

**MessageListFilteringTests.cs**

```csharp
[TestClass]
public class MessageListFilteringTests : TestContext
{
    [TestMethod]
    public void MessageList_FiltersSystemMessages_WhenFilterDisabled()
    {
        // Arrange
        var mockUIControl = new Mock<IUIControlService>();
        var initialState = UIState.DefaultNormalMode() with
        {
            ChatFilter = new ChatFilterState { ShowSystemMessages = false }
        };
        mockUIControl.Setup(s => s.GetCurrentState()).Returns(initialState);
        Services.AddSingleton(mockUIControl.Object);

        var messages = new List<UIMessage>
        {
            new() { Role = MessageRole.System, Content = "System message" },
            new() { Role = MessageRole.User, Content = "User message" }
        };

        // Act
        var cut = RenderComponent<MessageList>(parameters => parameters
            .Add(p => p.Messages, messages));

        // Assert
        var renderedMessages = cut.FindAll(".message");
        Assert.AreEqual(1, renderedMessages.Count); // Only user message visible
    }
}
```

---

## 🔒 Security & Validation

### Validation Rules

1. **Tool Argument Validation**
   - All tool arguments validated before execution
   - Unknown properties ignored (defensive)
   - Required properties checked

2. **State Immutability**
   - UIState is immutable (record type)
   - Updates create new instances
   - Thread-safe by design

3. **Error Handling**
   - All exceptions caught and logged
   - Failures return descriptive error messages
   - System remains stable even if UI control fails

4. **User Override Protection**
   - User changes never blocked by agent
   - Clear indicators when agent vs user made change
   - No silent manipulation

### Audit Trail

All UI control actions logged to TransparencyService:

```csharp
_transparencyService.LogEvent(
    TransparencyEventType.UIControlAction,
    new
    {
        ToolName = "ui_control_chat_filter",
        Arguments = new { showSystemMessages = true },
        Result = "Success"
    },
    "Agent revealed system messages");
```

---

## 📈 Performance Considerations

### Latency Targets

- Tool execution: <5ms (local, no network)
- State update + event: <10ms
- UI re-render: <50ms (SignalR + Blazor)
- Total user-perceivable latency: <100ms

### Optimization Strategies

1. **Scoped Services**
   - UIControlService per connection (no contention)
   - State isolated between users

2. **Immutable State**
   - Structural sharing for efficient updates
   - No defensive copying needed

3. **Selective Re-renders**
   - Components only re-render on relevant state changes
   - Use `ShouldRender()` for optimization if needed

4. **Event Throttling**
   - If multiple rapid updates, throttle events (like Phase 6 streaming)

---

## 🔗 Integration Points

### With Existing Systems

**1. ToolManager**
- Add routing for `ToolSourceType.BuiltInUIControl`
- No changes to core execution logic

**2. ToolRegistryComposite**
- Add `BuiltInUIControlToolRegistry` to composite
- Tools appear alongside MCP tools

**3. TransparencyService**
- Log all UI control actions
- Use existing `TransparencyEventType` enum (add `UIControlAction`)

**4. ConversationManager**
- No direct changes needed
- Mode switching updates system prompt via existing API

**5. Tools Overview Page**
- Display built-in tools with special badge
- Group by ToolSourceType

---

## 📝 Configuration

### appsettings.json Additions

```json
{
  "TeachingMode": {
    "SystemPrompt": "[Teaching mode system prompt from AppModeService]",
    "DefaultUIState": {
      "chatFilter": {
        "showSystemMessages": false,
        "showToolCalls": false,
        "filterControlsVisible": false
      }
    }
  }
}
```

---

## 🎯 Success Criteria

### Functional Requirements
✅ All 7 UI control tools implemented and functional
✅ Agent can query current UI state with `ui_get_state`
✅ User manual changes work alongside agent changes
✅ Mode switching works without errors
✅ Filter controls appear/disappear correctly

### Non-Functional Requirements
✅ Tool execution <5ms (P95)
✅ >99.9% tool execution success rate
✅ No memory leaks from event subscriptions
✅ State synchronized across all components
✅ Proper disposal of event handlers

### Testing Requirements
✅ >80% code coverage for new code
✅ All unit tests pass
✅ All component tests pass (bUnit)
✅ Integration tests for end-to-end flows

---

## 🔗 Related Documentation

- **[Interactive Teaching Mode Vision](./INTERACTIVE_TEACHING_MODE_VISION.md)** - Philosophy and use cases
- **[Teaching Mode Implementation Roadmap](./TEACHING_MODE_IMPLEMENTATION_ROADMAP.md)** - Development plan
- **[Architecture Overview](./ARCHITECTURE.md)** - Base system architecture

---

**Document Version:** 1.0
**Last Updated:** 2025-11-02
**Status:** Technical Specification (Pre-Implementation)
