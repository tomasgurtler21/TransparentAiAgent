using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Application.Agent;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentCore.Application.Scenarios;

/// <summary>
/// Executes teaching scenarios step-by-step.
/// Supports both basic and advanced (Phase 10b) scenario features.
/// </summary>
public class ScenarioExecutor : IScenarioExecutor
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IConfigurationOverlay _configurationOverlay;
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IScenarioToolRegistry _scenarioToolRegistry;
    private readonly IUIControlService _uiControlService;
    private readonly ILogger<ScenarioExecutor> _logger;
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();
    private bool _userInputEnabled = true;
    private ScenarioExecutionState _state = ScenarioExecutionState.NotRunning;
    private readonly SemaphoreSlim _pauseSemaphore = new(0);
    private readonly object _pauseLock = new();
    private string? _pauseMessage = null;
    private int _initialOverlayCount = 0;
    private readonly HashSet<string> _registeredMockTools = new();
    private readonly SemaphoreSlim _toolCallWaitSemaphore = new(0);
    private readonly SemaphoreSlim _toolResponseWaitSemaphore = new(0);
    private readonly SemaphoreSlim _orchestratorContinueSemaphore = new(0);
    private string? _waitingForToolName = null;
    private bool _isWaitingForToolCall = false;
    private bool _isWaitingForToolResponse = false;
    private bool _orchestratorWaiting = false;

    public ScenarioDefinition? CurrentScenario { get; private set; }
    public bool IsExecuting { get; private set; }
    public int CurrentStepIndex { get; private set; } = -1;
    public bool UserInputEnabled => _userInputEnabled;
    public ScenarioExecutionState State => _state;
    public bool IsPaused => _state == ScenarioExecutionState.Paused;

    public ScenarioExecutor(
        IAgentOrchestrator orchestrator,
        IConfigurationOverlay configurationOverlay,
        IConditionEvaluator conditionEvaluator,
        IScenarioToolRegistry scenarioToolRegistry,
        IUIControlService uiControlService,
        ILogger<ScenarioExecutor> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _configurationOverlay = configurationOverlay ?? throw new ArgumentNullException(nameof(configurationOverlay));
        _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        _scenarioToolRegistry = scenarioToolRegistry ?? throw new ArgumentNullException(nameof(scenarioToolRegistry));
        _uiControlService = uiControlService ?? throw new ArgumentNullException(nameof(uiControlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<ScenarioExecutionEventArgs>? ScenarioStarted;
    public event EventHandler<ScenarioExecutionEventArgs>? ScenarioCompleted;
    public event EventHandler<ScenarioExecutionEventArgs>? ScenarioFailed;
    public event EventHandler<ScenarioStepEventArgs>? StepExecuted;
    public event EventHandler<AutoMessageSentEventArgs>? AutoMessageSent;
    public event EventHandler<ScenarioStreamingUpdateEventArgs>? StreamingUpdate;
    public event EventHandler<ScenarioPausedEventArgs>? ScenarioPaused;
    public event EventHandler<ScenarioExecutionEventArgs>? ScenarioResumed;

    public async Task ExecuteScenarioAsync(ScenarioDefinition scenario, CancellationToken cancellationToken = default)
    {
        if (scenario == null)
            throw new ArgumentNullException(nameof(scenario));

        lock (_lock)
        {
            if (IsExecuting)
                throw new InvalidOperationException("A scenario is already executing");

            IsExecuting = true;
            CurrentScenario = scenario;
            CurrentStepIndex = -1;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Store initial overlay count to ensure cleanup
            _initialOverlayCount = _configurationOverlay.OverlayCount;
        }

        try
        {
            // Set state to Running
            lock (_pauseLock)
            {
                _state = ScenarioExecutionState.Running;
            }

            // Reset UI filters to teaching mode defaults when scenario starts
            // This ensures tool calls/responses are hidden by default for clean learning experience
            _logger.LogInformation("Resetting UI filters to teaching mode defaults for scenario");
            var filterResult = _uiControlService.UpdateChatFilter(
                showUserMessages: true,
                showAssistantMessages: true,
                showSystemMessages: false,
                showToolCalls: false,
                showToolResults: false,
                showTruncatedMessages: true);

            if (filterResult != null && !filterResult.Success)
            {
                _logger.LogWarning("Failed to reset chat filters: {Error}", filterResult.Error);
            }

            // Also hide the filter controls themselves (teaching mode default)
            var visibilityResult = _uiControlService.UpdateFilterControlVisibility(false);
            if (visibilityResult != null && !visibilityResult.Success)
            {
                _logger.LogWarning("Failed to hide filter controls: {Error}", visibilityResult.Error);
            }

            // Reset context indicators to teaching mode defaults (invisible, not highlighted)
            var contextResult = _uiControlService.UpdateContextIndicators(visible: false, highlighted: false);
            if (contextResult != null && !contextResult.Success)
            {
                _logger.LogWarning("Failed to reset context indicators: {Error}", contextResult.Error);
            }

            _logger.LogInformation("Scenario STARTED: '{ScenarioName}' (ID: {ScenarioId}) with {StepCount} steps",
                scenario.Name, scenario.Id, scenario.Steps.Count);

            // Fire started event
            ScenarioStarted?.Invoke(this, new ScenarioExecutionEventArgs(scenario));

            // Execute each step
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var step = scenario.Steps[i];
                CurrentStepIndex = i;

                // Apply delay if specified
                if (step.DelayMs > 0)
                {
                    await Task.Delay(step.DelayMs, _cts.Token);
                }

                // Execute the step
                await ExecuteStepAsync(step, scenario, i, _cts.Token);

                // Fire step executed event
                StepExecuted?.Invoke(this, new ScenarioStepEventArgs(scenario, step, i));

                // Check if we should pause (either manually or via PauseForUser step)
                if (IsPaused)
                {
                    // Wait for resume signal
                    await _pauseSemaphore.WaitAsync(_cts.Token);
                }
            }

            // Set state to Completed
            lock (_pauseLock)
            {
                _state = ScenarioExecutionState.Completed;
            }

            _logger.LogInformation("Scenario COMPLETED: '{ScenarioName}' (ID: {ScenarioId}) - All {StepCount} steps executed successfully",
                scenario.Name, scenario.Id, scenario.Steps.Count);

            // Fire completed event
            ScenarioCompleted?.Invoke(this, new ScenarioExecutionEventArgs(scenario));
        }
        catch (OperationCanceledException)
        {
            // Scenario was cancelled/stopped
            lock (_pauseLock)
            {
                _state = ScenarioExecutionState.NotRunning;
            }

            _logger.LogWarning("Scenario CANCELLED: '{ScenarioName}' (ID: {ScenarioId}) at step {CurrentStep}/{TotalSteps}",
                scenario.Name, scenario.Id, CurrentStepIndex + 1, scenario.Steps.Count);
        }
        catch (Exception ex)
        {
            // Set state to Failed
            lock (_pauseLock)
            {
                _state = ScenarioExecutionState.Failed;
            }

            _logger.LogError(ex, "Scenario FAILED: '{ScenarioName}' (ID: {ScenarioId}) at step {CurrentStep}/{TotalSteps} - Error: {ErrorMessage}",
                scenario.Name, scenario.Id, CurrentStepIndex + 1, scenario.Steps.Count, ex.Message);

            // Fire failed event
            ScenarioFailed?.Invoke(this, new ScenarioExecutionEventArgs(scenario, ex.Message));
            throw;
        }
        finally
        {
            // CRITICAL: Release orchestrator if it's waiting (scenario ended while orchestrator blocked)
            if (_orchestratorWaiting)
            {
                _logger.LogInformation("Scenario ended while orchestrator was waiting - releasing orchestrator");
                _orchestratorContinueSemaphore.Release();
                _orchestratorWaiting = false;
            }

            // Cleanup mock tools
            foreach (var toolName in _registeredMockTools)
            {
                _scenarioToolRegistry.UnregisterMockTool(toolName);
            }
            _registeredMockTools.Clear();

            // Ensure configuration overlays are cleaned up
            // Pop any overlays that were pushed during the scenario
            while (_configurationOverlay.OverlayCount > _initialOverlayCount)
            {
                _configurationOverlay.PopOverlay();
            }

            lock (_lock)
            {
                IsExecuting = false;
                CurrentScenario = null;
                CurrentStepIndex = -1;
                _cts?.Dispose();
                _cts = null;
            }
        }
    }

    public void StopScenario()
    {
        lock (_lock)
        {
            // If paused, release semaphore so cancellation can propagate
            lock (_pauseLock)
            {
                if (IsPaused)
                {
                    _state = ScenarioExecutionState.NotRunning;
                    _pauseSemaphore.Release();
                }
            }

            _cts?.Cancel();
        }
    }

    public void PauseScenario(string? message = null)
    {
        lock (_pauseLock)
        {
            if (_state != ScenarioExecutionState.Running)
                return; // Can only pause if running

            _state = ScenarioExecutionState.Paused;
            _pauseMessage = message;
            ScenarioPaused?.Invoke(this, new ScenarioPausedEventArgs(CurrentScenario!, message));
        }
    }

    public void ResumeScenario()
    {
        lock (_pauseLock)
        {
            if (_state != ScenarioExecutionState.Paused)
                return; // Can only resume if paused

            _state = ScenarioExecutionState.Running;
            _pauseSemaphore.Release(); // Unblock the waiting scenario task

            // If orchestrator is waiting (due to WaitForToolCall/Response), release it too
            if (_orchestratorWaiting)
            {
                _orchestratorContinueSemaphore.Release();
                _logger.LogInformation("Resuming orchestrator - allowing tool execution to continue");
            }

            ScenarioResumed?.Invoke(this, new ScenarioExecutionEventArgs(CurrentScenario!));
        }
    }

    private async Task ExecuteStepAsync(ScenarioStep step, ScenarioDefinition scenario, int stepIndex, CancellationToken cancellationToken)
    {
        // For ApplyConfigOverlay and RestoreConfigOverlay steps, don't use the wrapper push/pop
        // They manage overlays explicitly and permanently until restore is called
        bool isOverlayManagementStep = step.Type == ScenarioStepType.ApplyConfigOverlay
                                        || step.Type == ScenarioStepType.RestoreConfigOverlay;

        // Apply config overlay if present (but not for overlay management steps)
        if (!isOverlayManagementStep && step.ConfigOverlay != null && step.ConfigOverlay.Count > 0)
        {
            _configurationOverlay.PushOverlay(step.ConfigOverlay);
        }

        try
        {
            switch (step.Type)
            {
                // Basic step types
                case ScenarioStepType.AutoMessage:
                    await ExecuteAutoMessageStepAsync(step, cancellationToken);
                    break;

                case ScenarioStepType.WaitForResponse:
                    await ExecuteWaitForResponseStepAsync(step, cancellationToken);
                    break;

                case ScenarioStepType.AgentPrompt:
                    await ExecuteAgentPromptStepAsync(step, cancellationToken);
                    break;

                // Advanced step types (Phase 10b)
                case ScenarioStepType.ScenarioUserMessage:
                    await ExecuteScenarioUserMessageStepAsync(step, cancellationToken);
                    break;

                case ScenarioStepType.WaitForCondition:
                    await ExecuteWaitForConditionStepAsync(step, cancellationToken);
                    break;

                case ScenarioStepType.ApplyConfigOverlay:
                    ExecuteApplyConfigOverlayStep(step);
                    break;

                case ScenarioStepType.RestoreConfigOverlay:
                    ExecuteRestoreConfigOverlayStep(step);
                    break;

                case ScenarioStepType.DisableUserInput:
                    ExecuteDisableUserInputStep();
                    break;

                case ScenarioStepType.EnableUserInput:
                    ExecuteEnableUserInputStep();
                    break;

                case ScenarioStepType.Delay:
                    await ExecuteDelayStepAsync(step, cancellationToken);
                    break;

                case ScenarioStepType.UIControl:
                    await ExecuteUIControlStepAsync(step, cancellationToken);
                    break;

                case ScenarioStepType.PauseForUser:
                    ExecutePauseForUserStep(step);
                    break;

                case ScenarioStepType.RegisterMockTool:
                    ExecuteRegisterMockToolStep(step);
                    break;

                case ScenarioStepType.UnregisterMockTool:
                    ExecuteUnregisterMockToolStep(step);
                    break;

                case ScenarioStepType.WaitForToolCall:
                    await ExecuteWaitForToolCallStepAsync(step, cancellationToken);
                    break;

                case ScenarioStepType.WaitForToolResponse:
                    await ExecuteWaitForToolResponseStepAsync(step, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown scenario step type: {step.Type}");
            }
        }
        finally
        {
            // Remove config overlay if we pushed one (but not for overlay management steps)
            if (!isOverlayManagementStep && step.ConfigOverlay != null && step.ConfigOverlay.Count > 0)
            {
                _configurationOverlay.PopOverlay();
            }
        }
    }

    private async Task ExecuteAutoMessageStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.Content))
            return;

        try
        {
            // Process the user input through the orchestrator (streaming)
            await foreach (var chunk in _orchestrator.ProcessUserInputStreamingAsync(new DirectUserMessage(step.Content), cancellationToken))
            {
                // ✅ FIX: Forward streaming chunks with full chunk data for UI to consume
                StreamingUpdate?.Invoke(this, new ScenarioStreamingUpdateEventArgs(
                    CurrentScenario!,
                    CurrentStepIndex,
                    chunk));

                if (chunk.IsComplete)
                    break;
            }

            // Fire event to notify that an auto-message was sent
            AutoMessageSent?.Invoke(this, new AutoMessageSentEventArgs(step.Content, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteAutoMessageStepAsync for scenario");

            // ✅ FIX: Send error chunk to UI so it can clean up streaming state
            StreamingUpdate?.Invoke(this, new ScenarioStreamingUpdateEventArgs(
                CurrentScenario!,
                CurrentStepIndex,
                new StreamingResponseChunk(null, true, StreamingStatus.Error)));

            throw; // Re-throw so scenario execution stops
        }
    }

    private async Task ExecuteWaitForResponseStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        // Wait for any pending responses to complete
        // We poll the conversation to check if the last message is a TEXT response from the assistant
        // (NOT a tool call request - only completes when LLM gives final text answer)

        var maxWaitMs = 30000; // 30 second timeout
        var pollIntervalMs = 100;
        var elapsed = 0;

        while (elapsed < maxWaitMs && !cancellationToken.IsCancellationRequested)
        {
            var messages = _orchestrator.ConversationManager.GetAllMessages();

            // Only complete when last message is a TEXT response (not tool call)
            if (messages.Count > 0)
            {
                var lastMessage = messages[messages.Count - 1];

                // Check if it's a text-only assistant message (LlmTextMessage)
                // NOT a tool call message (LlmToolCallMessage)
                if (lastMessage.Role == MessageRole.Assistant && lastMessage is LlmTextMessage)
                {
                    _logger.LogInformation("WaitForResponse: Completed - LLM sent text-only response");
                    break;
                }
            }

            await Task.Delay(pollIntervalMs, cancellationToken);
            elapsed += pollIntervalMs;
        }
    }

    private Task ExecuteAgentPromptStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.Content))
            return Task.CompletedTask;

        // Add a system message to the conversation
        var systemMessage = new SystemMessage(step.Content);
        _orchestrator.ConversationManager.AddMessage(systemMessage);

        return Task.CompletedTask;
    }

    // Advanced step type execution methods (Phase 10b)

    private async Task ExecuteScenarioUserMessageStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.Content))
            return;

        // Create a ScenarioUserMessage with annotation support
        // This uses the proper ApplicationMessage routing through ProcessApplicationMessageAsync
        var scenarioMessage = new ScenarioUserMessage(step.Content, step.Annotation);

        // IMPORTANT: scenario_user_message does NOT wait for orchestrator to complete
        // It just sends the message and returns immediately
        // Use wait_for_response if you need to wait for the LLM's response
        // Use wait_for_tool_call/wait_for_tool_response to intercept tool execution
        _logger.LogInformation("ScenarioUserMessage: Sending message and continuing without waiting");

        // ✅ FIX: Capture scenario context in local variables BEFORE starting background task
        // This prevents race condition where scenario completes and nulls CurrentScenario
        // while background task is still processing the LLM response
        var capturedScenario = CurrentScenario!;
        var capturedStepIndex = CurrentStepIndex;

        // Start orchestrator processing in background
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var chunk in _orchestrator.ProcessApplicationMessageAsync(scenarioMessage, cancellationToken))
                {
                    // ✅ FIX: Fire StreamingUpdate with full chunk so UI can handle all status changes
                    StreamingUpdate?.Invoke(this, new ScenarioStreamingUpdateEventArgs(
                        capturedScenario,
                        capturedStepIndex,
                        chunk));

                    if (chunk.IsComplete)
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background orchestrator processing for scenario user message");

                // ✅ FIX: Send error chunk to UI so it can clean up streaming state
                StreamingUpdate?.Invoke(this, new ScenarioStreamingUpdateEventArgs(
                    capturedScenario,
                    capturedStepIndex,
                    new StreamingResponseChunk(null, true, StreamingStatus.Error)));
            }
        }, cancellationToken);

        // Small delay to ensure orchestrator starts processing before we move to next step
        await Task.Delay(100, cancellationToken);

        // Fire event to notify that an auto-message was sent
        AutoMessageSent?.Invoke(this, new AutoMessageSentEventArgs(step.Content, DateTime.UtcNow));
    }

    private async Task ExecuteWaitForConditionStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.Condition))
            throw new InvalidOperationException("Condition is required for WaitForCondition step");

        var parameters = step.ConditionParameters ?? new Dictionary<string, object>();

        // Get timeout from parameters or use default
        var timeout = 30000;
        if (parameters.TryGetValue("timeout", out var timeoutObj))
        {
            timeout = Convert.ToInt32(timeoutObj);
        }

        var pollInterval = 200; // 200ms polling interval
        var elapsed = 0;

        while (elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            // Evaluate the condition
            var conditionMet = await _conditionEvaluator.EvaluateAsync(step.Condition, parameters, cancellationToken);

            if (conditionMet)
                return; // Condition met, continue to next step

            await Task.Delay(pollInterval, cancellationToken);
            elapsed += pollInterval;
        }

        // Timeout occurred - handle based on OnTimeout setting
        var onTimeout = step.OnTimeout ?? "continue";
        switch (onTimeout.ToLowerInvariant())
        {
            case "fail":
                throw new TimeoutException($"Condition '{step.Condition}' was not met within {timeout}ms");
            case "skip":
                // Skip - just return and continue to next step
                return;
            case "continue":
            default:
                // Continue - just return and continue to next step
                return;
        }
    }

    private void ExecuteApplyConfigOverlayStep(ScenarioStep step)
    {
        if (step.ConfigOverlay == null || step.ConfigOverlay.Count == 0)
            throw new InvalidOperationException("ConfigOverlay is required for ApplyConfigOverlay step");

        _configurationOverlay.PushOverlay(step.ConfigOverlay);
    }

    private void ExecuteRestoreConfigOverlayStep(ScenarioStep step)
    {
        if (_configurationOverlay.OverlayCount > 0)
        {
            _configurationOverlay.PopOverlay();
        }
    }

    private void ExecuteDisableUserInputStep()
    {
        _userInputEnabled = false;
    }

    private void ExecuteEnableUserInputStep()
    {
        _userInputEnabled = true;
    }

    private async Task ExecuteDelayStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        var duration = step.DelayMs > 0 ? step.DelayMs : 1000; // Default to 1 second if not specified
        await Task.Delay(duration, cancellationToken);
    }

    private Task ExecuteUIControlStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.UIControlTool))
            throw new InvalidOperationException("UIControlTool is required for UIControl step");

        _logger.LogInformation("Executing UI control step: {Tool}", step.UIControlTool);

        // Route to appropriate UI control method based on tool name
        Result<UIState> result = step.UIControlTool.ToLowerInvariant() switch
        {
            "ui_control_chat_filter" => ExecuteChatFilterControl(step),
            "ui_control_filter_visibility" => ExecuteFilterVisibilityControl(step),
            "ui_control_transparency_viewer" => ExecuteTransparencyViewerControl(step),
            "ui_control_tools_panel" => ExecuteToolsPanelControl(step),
            "ui_control_context_indicators" => ExecuteContextIndicatorsControl(step),
            "ui_control_configuration" => ExecuteConfigurationControl(step),
            "ui_control_scenario_selector" => ExecuteScenarioSelectorControl(step),
            _ => Result<UIState>.Fail($"Unknown UI control tool: {step.UIControlTool}")
        };

        if (!result.Success)
        {
            _logger.LogError("UI control step failed: {Error}", result.Error);
            throw new InvalidOperationException($"UI control step failed: {result.Error}");
        }

        _logger.LogInformation("UI control step executed successfully: {Tool}", step.UIControlTool);
        return Task.CompletedTask;
    }

    private Result<UIState> ExecuteChatFilterControl(ScenarioStep step)
    {
        var args = step.UIControlArguments ?? new Dictionary<string, object>();

        return _uiControlService.UpdateChatFilter(
            showUserMessages: GetBoolArgument(args, "show_user_messages"),
            showAssistantMessages: GetBoolArgument(args, "show_assistant_messages"),
            showSystemMessages: GetBoolArgument(args, "show_system_messages"),
            showToolCalls: GetBoolArgument(args, "show_tool_calls"),
            showToolResults: GetBoolArgument(args, "show_tool_results"),
            showTruncatedMessages: GetBoolArgument(args, "show_truncated_messages"));
    }

    private Result<UIState> ExecuteFilterVisibilityControl(ScenarioStep step)
    {
        var args = step.UIControlArguments ?? new Dictionary<string, object>();
        var visible = GetBoolArgument(args, "visible") ?? true;

        return _uiControlService.UpdateFilterControlVisibility(visible);
    }

    private Result<UIState> ExecuteTransparencyViewerControl(ScenarioStep step)
    {
        var args = step.UIControlArguments ?? new Dictionary<string, object>();

        return _uiControlService.UpdateTransparencyViewer(
            visible: GetBoolArgument(args, "visible"),
            eventTypeFilters: GetStringListArgument(args, "event_type_filters"),
            showTimestamps: GetBoolArgument(args, "show_timestamps"));
    }

    private Result<UIState> ExecuteToolsPanelControl(ScenarioStep step)
    {
        var args = step.UIControlArguments ?? new Dictionary<string, object>();

        return _uiControlService.UpdateToolsPanel(
            visible: GetBoolArgument(args, "visible"),
            expandedTools: GetStringListArgument(args, "expanded_tools"),
            highlightedTool: GetStringArgument(args, "highlighted_tool"));
    }

    private Result<UIState> ExecuteContextIndicatorsControl(ScenarioStep step)
    {
        var args = step.UIControlArguments ?? new Dictionary<string, object>();

        return _uiControlService.UpdateContextIndicators(
            visible: GetBoolArgument(args, "visible"),
            highlighted: GetBoolArgument(args, "highlighted"));
    }

    private Result<UIState> ExecuteConfigurationControl(ScenarioStep step)
    {
        var args = step.UIControlArguments ?? new Dictionary<string, object>();

        return _uiControlService.UpdateConfigurationPage(
            visible: GetBoolArgument(args, "visible"),
            highlightSection: GetStringArgument(args, "highlight_section"));
    }

    private Result<UIState> ExecuteScenarioSelectorControl(ScenarioStep step)
    {
        var args = step.UIControlArguments ?? new Dictionary<string, object>();
        var visible = GetBoolArgument(args, "visible");

        return _uiControlService.UpdateScenarioSelector(visible);
    }

    // Helper methods to extract arguments from dictionary
    private static bool? GetBoolArgument(IReadOnlyDictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            bool b => b,
            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.False => false,
            _ => null
        };
    }

    private static string? GetStringArgument(IReadOnlyDictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            string s => s,
            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.String => je.GetString(),
            _ => null
        };
    }

    private static List<string>? GetStringListArgument(IReadOnlyDictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value))
            return null;

        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var element in je.EnumerateArray())
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var str = element.GetString();
                    if (str != null)
                        list.Add(str);
                }
            }
            return list;
        }

        return null;
    }

    private void ExecutePauseForUserStep(ScenarioStep step)
    {
        // Pause the scenario with the provided message (from Content field)
        // The pause will occur after this step completes (in the main execution loop)
        var message = step.Content ?? "Scenario paused. Click Resume to continue.";
        PauseScenario(message);
    }

    private void ExecuteRegisterMockToolStep(ScenarioStep step)
    {
        if (string.IsNullOrWhiteSpace(step.MockToolName))
            throw new InvalidOperationException("MockToolName is required for RegisterMockTool step");

        if (step.MockToolResponseMap == null || step.MockToolResponseMap.Count == 0)
            throw new InvalidOperationException("MockToolResponseMap is required for RegisterMockTool step");

        // Convert MockToolResponseConfig DTOs to MockToolResponse value objects
        var responseMap = new Dictionary<string, MockToolResponse>();
        foreach (var kvp in step.MockToolResponseMap)
        {
            responseMap[kvp.Key] = kvp.Value.ToMockToolResponse();
        }

        // Register the mock tool
        _scenarioToolRegistry.RegisterMockTool(
            step.MockToolName,
            step.MockToolDescription ?? $"Mock tool: {step.MockToolName}",
            step.MockToolParametersSchema ?? "{}",
            responseMap);

        // Track for cleanup
        _registeredMockTools.Add(step.MockToolName);

        _logger.LogInformation(
            "Registered mock tool '{ToolName}' with {ResponseCount} response(s)",
            step.MockToolName,
            responseMap.Count);
    }

    private void ExecuteUnregisterMockToolStep(ScenarioStep step)
    {
        if (string.IsNullOrWhiteSpace(step.MockToolName))
            throw new InvalidOperationException("MockToolName is required for UnregisterMockTool step");

        _scenarioToolRegistry.UnregisterMockTool(step.MockToolName);
        _registeredMockTools.Remove(step.MockToolName);

        _logger.LogInformation("Unregistered mock tool '{ToolName}'", step.MockToolName);
    }

    private async Task ExecuteWaitForToolCallStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "WaitForToolCall: waiting for{ToolFilter}",
            step.ToolName != null ? $" tool '{step.ToolName}'" : " any tool call");

        _waitingForToolName = step.ToolName;
        _isWaitingForToolCall = true;

        try
        {
            await _toolCallWaitSemaphore.WaitAsync(cancellationToken);
            _logger.LogInformation(
                "WaitForToolCall: completed{ToolFilter}",
                step.ToolName != null ? $" for '{step.ToolName}'" : "");
        }
        finally
        {
            _isWaitingForToolCall = false;
            _waitingForToolName = null;
        }
    }

    private async Task ExecuteWaitForToolResponseStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "WaitForToolResponse: waiting for{ToolFilter}",
            step.ToolName != null ? $" tool '{step.ToolName}'" : " any tool response");

        _waitingForToolName = step.ToolName;
        _isWaitingForToolResponse = true;

        try
        {
            await _toolResponseWaitSemaphore.WaitAsync(cancellationToken);
            _logger.LogInformation(
                "WaitForToolResponse: completed{ToolFilter}",
                step.ToolName != null ? $" for '{step.ToolName}'" : "");
        }
        finally
        {
            _isWaitingForToolResponse = false;
            _waitingForToolName = null;
        }
    }

    /// <summary>
    /// Hook called by orchestrator BEFORE executing a tool.
    /// Orchestrator will WAIT here if scenario is on a WaitForToolCall step.
    /// </summary>
    public async Task WaitBeforeToolExecutionAsync(string toolName, CancellationToken cancellationToken = default)
    {
        // Only wait if scenario is currently on a WaitForToolCall step
        if (!_isWaitingForToolCall)
            return;

        // Only wait if this is the tool we're waiting for (or waiting for any tool)
        if (_waitingForToolName != null && _waitingForToolName != toolName)
            return;

        _logger.LogInformation(
            "Orchestrator paused BEFORE executing tool '{ToolName}' - waiting for scenario to allow continuation",
            toolName);

        // Release the scenario step's semaphore so it can complete
        _toolCallWaitSemaphore.Release();

        // Mark that orchestrator is waiting
        _orchestratorWaiting = true;

        try
        {
            // BLOCK orchestrator until scenario signals it can continue
            // This will be released when user resumes the scenario
            await _orchestratorContinueSemaphore.WaitAsync(cancellationToken);

            _logger.LogInformation(
                "Orchestrator resuming - will now execute tool '{ToolName}'",
                toolName);
        }
        finally
        {
            _orchestratorWaiting = false;
        }
    }

    /// <summary>
    /// Hook called by orchestrator AFTER executing a tool.
    /// Orchestrator will WAIT here if scenario is on a WaitForToolResponse step.
    /// </summary>
    public async Task WaitAfterToolExecutionAsync(string toolName, CancellationToken cancellationToken = default)
    {
        // Only wait if scenario is currently on a WaitForToolResponse step
        if (!_isWaitingForToolResponse)
            return;

        // Only wait if this is the tool we're waiting for (or waiting for any tool)
        if (_waitingForToolName != null && _waitingForToolName != toolName)
            return;

        _logger.LogInformation(
            "Orchestrator paused AFTER executing tool '{ToolName}' - waiting for scenario to allow continuation",
            toolName);

        // CRITICAL: Fire streaming event to trigger UI refresh BEFORE blocking
        // Tool result message was already added to conversation by orchestrator
        // Now we need to tell UI to refresh and show it
        StreamingUpdate?.Invoke(this, new ScenarioStreamingUpdateEventArgs(
            CurrentScenario!,
            CurrentStepIndex,
            new StreamingResponseChunk(null, IsComplete: false, Status: StreamingStatus.ToolResultsReady)));

        // Release the scenario step's semaphore so it can complete
        _toolResponseWaitSemaphore.Release();

        // Mark that orchestrator is waiting
        _orchestratorWaiting = true;

        try
        {
            // BLOCK orchestrator until scenario signals it can continue
            // This will be released when user resumes the scenario
            await _orchestratorContinueSemaphore.WaitAsync(cancellationToken);

            _logger.LogInformation(
                "Orchestrator resuming - tool '{ToolName}' execution completed",
                toolName);
        }
        finally
        {
            _orchestratorWaiting = false;
        }
    }
}
