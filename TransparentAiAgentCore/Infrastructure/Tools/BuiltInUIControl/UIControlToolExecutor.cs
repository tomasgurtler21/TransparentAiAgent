using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Executes built-in UI control tools by routing to IUIControlService.
/// Parses JSON arguments and converts Result&lt;UIState&gt; to ToolExecutionResult.
/// Registered as Scoped to share the same IUIControlService instance with UI components.
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
    /// Uses the injected scoped IUIControlService instance to support per-connection UI state.
    /// </summary>
    public Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing UI control tool: {ToolName}", tool.Name);

            // Parse arguments
            JsonDocument argsDoc;
            try
            {
                argsDoc = JsonDocument.Parse(arguments);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse arguments for tool {ToolName}", tool.Name);
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

        return uiControlService.UpdateChatFilter(
            showUserMessages: GetBoolProperty(root, "show_user_messages"),
            showAssistantMessages: GetBoolProperty(root, "show_assistant_messages"),
            showSystemMessages: GetBoolProperty(root, "show_system_messages"),
            showToolCalls: GetBoolProperty(root, "show_tool_calls"),
            showToolResults: GetBoolProperty(root, "show_tool_results"),
            showTruncatedMessages: GetBoolProperty(root, "show_truncated_messages"));
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
            navigate: GetBoolProperty(root, "navigate"),
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
