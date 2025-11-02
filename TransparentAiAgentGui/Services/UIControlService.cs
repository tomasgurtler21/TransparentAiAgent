using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Infrastructure.Transparency;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

    public Result<UIState> UpdateTransparencyViewer(
        bool? visible = null,
        List<string>? eventTypeFilters = null,
        bool? showTimestamps = null)
    {
        try
        {
            var newViewer = _currentState.TransparencyViewer with
            {
                Visible = visible ?? _currentState.TransparencyViewer.Visible,
                EventTypeFilters = eventTypeFilters ?? _currentState.TransparencyViewer.EventTypeFilters,
                ShowTimestamps = showTimestamps ?? _currentState.TransparencyViewer.ShowTimestamps
            };

            _currentState = _currentState with { TransparencyViewer = newViewer };

            LogUIControlEvent("TransparencyViewerUpdated", new
            {
                newViewer.Visible,
                EventTypeFilterCount = newViewer.EventTypeFilters.Count,
                newViewer.ShowTimestamps
            });

            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update transparency viewer");
            return Result<UIState>.Fail($"Failed to update transparency viewer: {ex.Message}");
        }
    }

    public Result<UIState> UpdateToolsPanel(
        bool? visible = null,
        List<string>? expandedTools = null,
        string? highlightedTool = null)
    {
        try
        {
            var newPanel = _currentState.ToolsPanel with
            {
                Visible = visible ?? _currentState.ToolsPanel.Visible,
                ExpandedTools = expandedTools ?? _currentState.ToolsPanel.ExpandedTools,
                HighlightedTool = highlightedTool ?? _currentState.ToolsPanel.HighlightedTool
            };

            _currentState = _currentState with { ToolsPanel = newPanel };

            LogUIControlEvent("ToolsPanelUpdated", new
            {
                newPanel.Visible,
                ExpandedToolsCount = newPanel.ExpandedTools.Count,
                newPanel.HighlightedTool
            });

            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tools panel");
            return Result<UIState>.Fail($"Failed to update tools panel: {ex.Message}");
        }
    }

    public Result<UIState> UpdateContextIndicators(
        bool? visible = null,
        bool? highlighted = null)
    {
        try
        {
            var newIndicators = _currentState.ContextIndicators with
            {
                Visible = visible ?? _currentState.ContextIndicators.Visible,
                Highlighted = highlighted ?? _currentState.ContextIndicators.Highlighted
            };

            _currentState = _currentState with { ContextIndicators = newIndicators };

            LogUIControlEvent("ContextIndicatorsUpdated", new
            {
                newIndicators.Visible,
                newIndicators.Highlighted
            });

            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update context indicators");
            return Result<UIState>.Fail($"Failed to update context indicators: {ex.Message}");
        }
    }

    public Result<UIState> UpdateConfigurationPage(
        bool? navigate = null,
        string? highlightSection = null)
    {
        try
        {
            var newConfigPage = _currentState.ConfigurationPage with
            {
                NavigateRequested = navigate ?? _currentState.ConfigurationPage.NavigateRequested,
                HighlightedSection = highlightSection ?? _currentState.ConfigurationPage.HighlightedSection
            };

            _currentState = _currentState with { ConfigurationPage = newConfigPage };

            LogUIControlEvent("ConfigurationPageUpdated", new
            {
                newConfigPage.NavigateRequested,
                newConfigPage.HighlightedSection
            });

            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration page");
            return Result<UIState>.Fail($"Failed to update configuration page: {ex.Message}");
        }
    }

    public Result<UIState> ResetToDefaults()
    {
        try
        {
            // Reset based on current mode
            _currentState = _currentState.CurrentMode == AppMode.Teaching
                ? UIState.DefaultTeachingMode()
                : UIState.DefaultNormalMode();

            LogUIControlEvent("UIStateReset", new { Mode = _currentState.CurrentMode.ToString() });
            UIStateChanged?.Invoke(this, _currentState);

            return Result<UIState>.Ok(_currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset to defaults");
            return Result<UIState>.Fail($"Failed to reset to defaults: {ex.Message}");
        }
    }

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
        var jsonData = JsonSerializer.Serialize(data);
        var evt = new TransparencyEvent(
            TransparencyEventType.UIControlAction,
            jsonData,
            $"UI Control: {eventName}");
        _transparencyService.LogEvent(evt);
    }
}
