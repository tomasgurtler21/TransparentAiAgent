using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Application.Scenarios;

/// <summary>
/// Evaluates conditions for WaitForCondition scenario steps.
/// </summary>
public class ConditionEvaluator : IConditionEvaluator
{
    private readonly IAgentOrchestrator _orchestrator;

    public ConditionEvaluator(IAgentOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    /// <inheritdoc />
    public async Task<bool> EvaluateAsync(
        string condition,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(condition))
            throw new ArgumentException("Condition cannot be null or whitespace", nameof(condition));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        return condition.ToLowerInvariant() switch
        {
            "response_contains" => await EvaluateResponseContainsAsync(parameters, cancellationToken),
            "message_count" => await EvaluateMessageCountAsync(parameters, cancellationToken),
            "user_interaction" => await EvaluateUserInteractionAsync(parameters, cancellationToken),
            "ui_state_changed" => await EvaluateUIStateChangedAsync(parameters, cancellationToken),
            _ => throw new NotSupportedException($"Condition type '{condition}' is not supported")
        };
    }

    private async Task<bool> EvaluateResponseContainsAsync(
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // Get keywords parameter
        if (!parameters.TryGetValue("keywords", out var keywordsObj))
            throw new ArgumentException("Parameter 'keywords' is required for response_contains condition");

        var keywords = ConvertToStringList(keywordsObj);
        if (keywords.Count == 0)
            throw new ArgumentException("Keywords list cannot be empty");

        // Get timeout parameter (optional, default 30000ms)
        var timeout = 30000;
        if (parameters.TryGetValue("timeout", out var timeoutObj))
        {
            timeout = Convert.ToInt32(timeoutObj);
        }

        // Check if the last assistant message contains any of the keywords
        var messages = _orchestrator.ConversationManager.GetAllMessages();
        if (messages.Count == 0)
            return false;

        var lastMessage = messages[messages.Count - 1];
        if (lastMessage.Role != MessageRole.Assistant)
            return false;

        var content = lastMessage.GetContentAsString()?.ToLowerInvariant() ?? string.Empty;

        // Check if any keyword is present in the content
        foreach (var keyword in keywords)
        {
            if (content.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private Task<bool> EvaluateMessageCountAsync(
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // Get count parameter
        if (!parameters.TryGetValue("count", out var countObj))
            throw new ArgumentException("Parameter 'count' is required for message_count condition");

        var expectedCount = Convert.ToInt32(countObj);

        var messages = _orchestrator.ConversationManager.GetAllMessages();
        return Task.FromResult(messages.Count >= expectedCount);
    }

    private Task<bool> EvaluateUserInteractionAsync(
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // This condition would need to be implemented with UI integration
        // For now, we'll throw NotImplementedException
        throw new NotImplementedException("user_interaction condition is not yet implemented. This requires UI integration.");
    }

    private Task<bool> EvaluateUIStateChangedAsync(
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // This condition would need to be implemented with UIControlService integration
        // For now, we'll throw NotImplementedException
        throw new NotImplementedException("ui_state_changed condition is not yet implemented. This requires UIControlService integration.");
    }

    private List<string> ConvertToStringList(object obj)
    {
        if (obj is IEnumerable<string> stringList)
            return stringList.ToList();

        if (obj is IEnumerable<object> objList)
            return objList.Select(o => o?.ToString() ?? string.Empty).ToList();

        if (obj is string singleString)
            return new List<string> { singleString };

        throw new ArgumentException($"Cannot convert {obj.GetType()} to string list");
    }
}
