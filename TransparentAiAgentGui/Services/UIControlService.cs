using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Infrastructure.Transparency;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TransparentAiAgentGui.Services;

/// <summary>
/// Implementation of UI control service managing UI state and events.
/// Registered as Singleton service (shared across all render contexts in the app).
/// Note: This means UI state is shared app-wide. For multi-user scenarios, this would need to be per-user.
/// Thread-safe: Uses lock for state mutations and event invocation.
/// </summary>
public class UIControlService : IUIControlService
{
    private UIState _currentState;
    private readonly object _stateLock = new object();
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

        _logger.LogInformation("UIControlService created (Singleton)");
    }

    public UIState GetCurrentState()
    {
        lock (_stateLock)
        {
            return _currentState;
        }
    }

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
            lock (_stateLock)
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
            lock (_stateLock)
            {
                var newChatFilter = _currentState.ChatFilter with { FilterControlsVisible = visible };
                _currentState = _currentState with { ChatFilter = newChatFilter };

                LogUIControlEvent("FilterControlVisibilityChanged", new { visible });

                var subscriberCount = UIStateChanged?.GetInvocationList().Length ?? 0;
                _logger.LogInformation("UpdateFilterControlVisibility: Firing to {Count} subscribers", subscriberCount);

                UIStateChanged?.Invoke(this, _currentState);

                return Result<UIState>.Ok(_currentState);
            }
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
            lock (_stateLock)
            {
                // Close other overlays if opening this one
                if (visible == true)
                {
                    CloseAllOverlaysExcept("TransparencyViewer");
                }

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
            lock (_stateLock)
            {
                // Close other overlays if opening this one
                if (visible == true)
                {
                    CloseAllOverlaysExcept("ToolsPanel");
                }

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
            lock (_stateLock)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update context indicators");
            return Result<UIState>.Fail($"Failed to update context indicators: {ex.Message}");
        }
    }

    public Result<UIState> UpdateConfigurationPage(
        bool? visible = null,
        string? highlightSection = null)
    {
        try
        {
            lock (_stateLock)
            {
                // Close other overlays if opening this one
                if (visible == true)
                {
                    CloseAllOverlaysExcept("ConfigurationPage");
                }

                var newConfigPage = _currentState.ConfigurationPage with
                {
                    Visible = visible ?? _currentState.ConfigurationPage.Visible,
                    HighlightedSection = highlightSection ?? _currentState.ConfigurationPage.HighlightedSection
                };

                _currentState = _currentState with { ConfigurationPage = newConfigPage };

                LogUIControlEvent("ConfigurationPageUpdated", new
                {
                    newConfigPage.Visible,
                    newConfigPage.HighlightedSection
                });

                UIStateChanged?.Invoke(this, _currentState);

                return Result<UIState>.Ok(_currentState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration page");
            return Result<UIState>.Fail($"Failed to update configuration page: {ex.Message}");
        }
    }

    public Result<UIState> UpdateScenarioSelector(bool? visible = null)
    {
        try
        {
            lock (_stateLock)
            {
                // Close other overlays if opening this one
                if (visible == true)
                {
                    CloseAllOverlaysExcept("ScenarioSelector");
                }

                var newSelector = _currentState.ScenarioSelector with
                {
                    Visible = visible ?? _currentState.ScenarioSelector.Visible
                };

                _currentState = _currentState with { ScenarioSelector = newSelector };

                LogUIControlEvent("ScenarioSelectorUpdated", new
                {
                    newSelector.Visible
                });

                UIStateChanged?.Invoke(this, _currentState);

                return Result<UIState>.Ok(_currentState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scenario selector");
            return Result<UIState>.Fail($"Failed to update scenario selector: {ex.Message}");
        }
    }

    public Result<UIState> ResetToDefaults()
    {
        try
        {
            lock (_stateLock)
            {
                // Reset based on current mode
                _currentState = _currentState.CurrentMode == AppMode.Teaching
                    ? UIState.DefaultTeachingMode()
                    : UIState.DefaultNormalMode();

                LogUIControlEvent("UIStateReset", new { Mode = _currentState.CurrentMode.ToString() });
                UIStateChanged?.Invoke(this, _currentState);

                return Result<UIState>.Ok(_currentState);
            }
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
            lock (_stateLock)
            {
                _currentState = newMode == AppMode.Teaching
                    ? UIState.DefaultTeachingMode()
                    : UIState.DefaultNormalMode();

                LogUIControlEvent("AppModeChanged", new { Mode = newMode.ToString() });
                UIStateChanged?.Invoke(this, _currentState);

                return Result<UIState>.Ok(_currentState);
            }
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

    private void CloseAllOverlaysExcept(string overlayToKeepOpen)
    {
        // This runs within a lock, so thread-safe
        var updates = _currentState;

        if (overlayToKeepOpen != "TransparencyViewer" && _currentState.TransparencyViewer.Visible)
        {
            updates = updates with { TransparencyViewer = updates.TransparencyViewer with { Visible = false } };
        }

        if (overlayToKeepOpen != "ToolsPanel" && _currentState.ToolsPanel.Visible)
        {
            updates = updates with { ToolsPanel = updates.ToolsPanel with { Visible = false } };
        }

        if (overlayToKeepOpen != "ConfigurationPage" && _currentState.ConfigurationPage.Visible)
        {
            updates = updates with { ConfigurationPage = updates.ConfigurationPage with { Visible = false } };
        }

        if (overlayToKeepOpen != "ScenarioSelector" && _currentState.ScenarioSelector.Visible)
        {
            updates = updates with { ScenarioSelector = updates.ScenarioSelector with { Visible = false } };
        }

        _currentState = updates;
    }
}
