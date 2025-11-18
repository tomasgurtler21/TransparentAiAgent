using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Executes built-in UI control tools by routing to IUIControlService.
/// Parses JSON arguments and converts Result&lt;UIState&gt; to ToolExecutionResult.
/// Registered as Scoped to work with scoped IToolManager (accesses singleton IUIControlService).
/// </summary>
public class UIControlToolExecutor : IToolExecutor
{
    private readonly IUIControlService _uiControlService;
    private readonly ILogger<UIControlToolExecutor> _logger;

    public UIControlToolExecutor(
        IUIControlService uiControlService,
        ILogger<UIControlToolExecutor> logger)
    {
        _uiControlService = uiControlService ?? throw new ArgumentNullException(nameof(uiControlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the source type this executor handles (BuiltInUIControl).
    /// </summary>
    public ToolSourceType SourceType => ToolSourceType.BuiltInUIControl;

    /// <summary>
    /// Executes a UI control tool by routing to the appropriate IUIControlService method.
    /// Uses the injected singleton IUIControlService instance shared across all contexts.
    /// </summary>
    public Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing UI control tool: {ToolName} with arguments: {Arguments}", tool.Name, arguments);

            // Parse arguments - handle empty/null arguments for tools with optional parameters
            JsonDocument argsDoc;
            try
            {
                // If arguments is null, empty, or whitespace, treat as empty JSON object
                if (string.IsNullOrWhiteSpace(arguments))
                {
                    _logger.LogDebug("Tool {ToolName} called with no arguments, using empty object", tool.Name);
                    arguments = "{}";
                }

                argsDoc = JsonDocument.Parse(arguments);
                _logger.LogInformation("Successfully parsed JSON arguments for {ToolName}", tool.Name);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse arguments for tool {ToolName}. Arguments: '{Arguments}'", tool.Name, arguments);
                return Task.FromResult(ToolExecutionResult.Failure(
                    $"Invalid JSON arguments: {ex.Message}",
                    stopwatch.Elapsed));
            }

            // Route to appropriate handler
            Result<UIState> result = tool.Name.ToLowerInvariant() switch
            {
                "ui_control_chat_filter" => ExecuteChatFilterTool(_uiControlService, argsDoc),
                "ui_control_filter_visibility" => ExecuteFilterVisibilityTool(_uiControlService, argsDoc),
                "ui_get_state" => ExecuteGetStateTool(_uiControlService, argsDoc),
                "ui_control_transparency_viewer" => ExecuteTransparencyViewerTool(_uiControlService, argsDoc),
                "ui_control_tools_panel" => ExecuteToolsPanelTool(_uiControlService, argsDoc),
                "ui_control_context_indicators" => ExecuteContextIndicatorsTool(_uiControlService, argsDoc),
                "ui_control_configuration" => ExecuteConfigurationTool(_uiControlService, argsDoc),
                _ => Result<UIState>.Fail($"Unknown tool: {tool.Name}")
            };

            stopwatch.Stop();

            // Convert Result<UIState> to ToolExecutionResult
            if (result.Success && result.Value != null)
            {
                var serializedState = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                _logger.LogDebug("UI control tool {ToolName} executed successfully", tool.Name);
                return Task.FromResult(ToolExecutionResult.Success(serializedState, stopwatch.Elapsed));
            }
            else
            {
                _logger.LogWarning("UI control tool {ToolName} failed: {Error}", tool.Name, result.Error);
                return Task.FromResult(ToolExecutionResult.Failure(
                    result.Error ?? "Unknown error",
                    stopwatch.Elapsed));
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error executing UI control tool {ToolName}", tool.Name);
            return Task.FromResult(ToolExecutionResult.Failure(
                $"Unexpected error: {ex.Message}",
                stopwatch.Elapsed));
        }
    }

    private Result<UIState> ExecuteChatFilterTool(IUIControlService uiControlService, JsonDocument args)
    {
        var root = args.RootElement;

        var showUser = GetBoolProperty(root, "show_user_messages");
        var showAssistant = GetBoolProperty(root, "show_assistant_messages");
        var showSystem = GetBoolProperty(root, "show_system_messages");
        var showToolCalls = GetBoolProperty(root, "show_tool_calls");
        var showToolResults = GetBoolProperty(root, "show_tool_results");
        var showTruncated = GetBoolProperty(root, "show_truncated_messages");

        _logger.LogInformation(
            "ExecuteChatFilterTool: Parsed values - showUser={ShowUser}, showAssistant={ShowAssistant}, " +
            "showSystem={ShowSystem}, showToolCalls={ShowToolCalls}, showToolResults={ShowToolResults}, showTruncated={ShowTruncated}",
            showUser, showAssistant, showSystem, showToolCalls, showToolResults, showTruncated);

        return uiControlService.UpdateChatFilter(
            showUserMessages: showUser,
            showAssistantMessages: showAssistant,
            showSystemMessages: showSystem,
            showToolCalls: showToolCalls,
            showToolResults: showToolResults,
            showTruncatedMessages: showTruncated);
    }

    private Result<UIState> ExecuteFilterVisibilityTool(IUIControlService uiControlService, JsonDocument args)
    {
        var root = args.RootElement;
        var visible = GetBoolProperty(root, "visible") ?? true;

        return uiControlService.UpdateFilterControlVisibility(visible);
    }

    private Result<UIState> ExecuteGetStateTool(IUIControlService uiControlService, JsonDocument args)
    {
        // ui_get_state doesn't modify state, just returns current state
        var currentState = uiControlService.GetCurrentState();

        var root = args.RootElement;
        var component = GetStringProperty(root, "component");

        // Return full or partial state based on component parameter
        var stateToReturn = component?.ToLowerInvariant() switch
        {
            "chat_filter" => currentState with
            {
                TransparencyViewer = null!,
                ToolsPanel = null!,
                ContextIndicators = null!,
                ConfigurationPage = null!
            },
            "transparency_viewer" => currentState with
            {
                ChatFilter = null!,
                ToolsPanel = null!,
                ContextIndicators = null!,
                ConfigurationPage = null!
            },
            "tools_panel" => currentState with
            {
                ChatFilter = null!,
                TransparencyViewer = null!,
                ContextIndicators = null!,
                ConfigurationPage = null!
            },
            "context_indicators" => currentState with
            {
                ChatFilter = null!,
                TransparencyViewer = null!,
                ToolsPanel = null!,
                ConfigurationPage = null!
            },
            "configuration" => currentState with
            {
                ChatFilter = null!,
                TransparencyViewer = null!,
                ToolsPanel = null!,
                ContextIndicators = null!
            },
            _ => currentState // "all" or null returns full state
        };

        return Result<UIState>.Ok(stateToReturn);
    }

    private Result<UIState> ExecuteTransparencyViewerTool(IUIControlService uiControlService, JsonDocument args)
    {
        var root = args.RootElement;

        var eventTypeFilters = GetStringArrayProperty(root, "event_type_filters");

        return uiControlService.UpdateTransparencyViewer(
            visible: GetBoolProperty(root, "visible"),
            eventTypeFilters: eventTypeFilters,
            showTimestamps: GetBoolProperty(root, "show_timestamps"));
    }

    private Result<UIState> ExecuteToolsPanelTool(IUIControlService uiControlService, JsonDocument args)
    {
        var root = args.RootElement;

        // Handle expand/collapse by building the expanded tools list
        // This is simplified - a full implementation might track expand/collapse operations
        return uiControlService.UpdateToolsPanel(
            visible: GetBoolProperty(root, "visible"),
            expandedTools: null, // Could parse expand_tool/collapse_tool to build this
            highlightedTool: GetStringProperty(root, "highlight_tool"));
    }

    private Result<UIState> ExecuteContextIndicatorsTool(IUIControlService uiControlService, JsonDocument args)
    {
        var root = args.RootElement;

        return uiControlService.UpdateContextIndicators(
            visible: GetBoolProperty(root, "visible"),
            highlighted: GetBoolProperty(root, "highlighted"));
    }

    private Result<UIState> ExecuteConfigurationTool(IUIControlService uiControlService, JsonDocument args)
    {
        var root = args.RootElement;

        return uiControlService.UpdateConfigurationPage(
            visible: GetBoolProperty(root, "visible"),
            highlightSection: GetStringProperty(root, "highlight_section"));
    }

    // Helper methods for JSON parsing

    private static bool? GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
        {
            return property.GetBoolean();
        }
        return null;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }
        return null;
    }

    private static List<string>? GetStringArrayProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var item in property.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (value != null)
                    {
                        list.Add(value);
                    }
                }
            }
            return list;
        }
        return null;
    }
}
