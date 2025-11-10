using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Application.Agent;

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
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();
    private bool _userInputEnabled = true;

    public ScenarioDefinition? CurrentScenario { get; private set; }
    public bool IsExecuting { get; private set; }
    public int CurrentStepIndex { get; private set; } = -1;
    public bool UserInputEnabled => _userInputEnabled;

    public ScenarioExecutor(
        IAgentOrchestrator orchestrator,
        IConfigurationOverlay configurationOverlay,
        IConditionEvaluator conditionEvaluator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _configurationOverlay = configurationOverlay ?? throw new ArgumentNullException(nameof(configurationOverlay));
        _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
    }

    public event EventHandler<ScenarioExecutionEventArgs>? ScenarioStarted;
    public event EventHandler<ScenarioExecutionEventArgs>? ScenarioCompleted;
    public event EventHandler<ScenarioExecutionEventArgs>? ScenarioFailed;
    public event EventHandler<ScenarioStepEventArgs>? StepExecuted;
    public event EventHandler<AutoMessageSentEventArgs>? AutoMessageSent;
    public event EventHandler<ScenarioStreamingUpdateEventArgs>? StreamingUpdate;

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
        }

        try
        {
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

                // Execute the step (for now, just mark it as executed)
                await ExecuteStepAsync(step, scenario, i, _cts.Token);

                // Fire step executed event
                StepExecuted?.Invoke(this, new ScenarioStepEventArgs(scenario, step, i));
            }

            // Fire completed event
            ScenarioCompleted?.Invoke(this, new ScenarioExecutionEventArgs(scenario));
        }
        catch (OperationCanceledException)
        {
            // Scenario was cancelled/stopped - this is expected
        }
        catch (Exception ex)
        {
            // Fire failed event
            ScenarioFailed?.Invoke(this, new ScenarioExecutionEventArgs(scenario, ex.Message));
            throw;
        }
        finally
        {
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
            _cts?.Cancel();
        }
    }

    private async Task ExecuteStepAsync(ScenarioStep step, ScenarioDefinition scenario, int stepIndex, CancellationToken cancellationToken)
    {
        // Apply config overlay if present
        if (step.ConfigOverlay != null && step.ConfigOverlay.Count > 0)
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

                default:
                    throw new InvalidOperationException($"Unknown scenario step type: {step.Type}");
            }
        }
        finally
        {
            // Remove config overlay if we pushed one
            if (step.ConfigOverlay != null && step.ConfigOverlay.Count > 0)
            {
                _configurationOverlay.PopOverlay();
            }
        }
    }

    private async Task ExecuteAutoMessageStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.Content))
            return;

        // Process the user input through the orchestrator (streaming)
        await foreach (var chunk in _orchestrator.ProcessUserInputStreamingAsync(step.Content, cancellationToken))
        {
            // Forward streaming chunks as events for UI to consume
            StreamingUpdate?.Invoke(this, new ScenarioStreamingUpdateEventArgs(
                chunk.ContentDeltaSafe,
                chunk.IsComplete));

            if (chunk.IsComplete)
                break;
        }

        // Fire event to notify that an auto-message was sent
        AutoMessageSent?.Invoke(this, new AutoMessageSentEventArgs(step.Content, DateTime.UtcNow));
    }

    private async Task ExecuteWaitForResponseStepAsync(ScenarioStep step, CancellationToken cancellationToken)
    {
        // Wait for any pending responses to complete
        // We poll the conversation to check if the last message is from the assistant
        // This ensures the agent has finished responding before continuing

        var maxWaitMs = 30000; // 30 second timeout
        var pollIntervalMs = 100;
        var elapsed = 0;

        while (elapsed < maxWaitMs && !cancellationToken.IsCancellationRequested)
        {
            var messages = _orchestrator.ConversationManager.GetAllMessages();

            // If we have messages and the last one is from the assistant, we're done waiting
            if (messages.Count > 0 && messages[messages.Count - 1].Role == MessageRole.Assistant)
            {
                break;
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

        // ScenarioUserMessage behaves like AutoMessage but with annotation support
        // The annotation is handled by the UI layer
        await ExecuteAutoMessageStepAsync(step, cancellationToken);
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

        // This would need to integrate with the UIControlService
        // For now, we'll throw NotImplementedException
        // In a full implementation, this would call the UIControlService to execute the tool
        throw new NotImplementedException(
            "UIControl step execution requires integration with UIControlService. " +
            $"Tool: {step.UIControlTool}");
    }
}
