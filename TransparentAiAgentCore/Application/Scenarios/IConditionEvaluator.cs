using TransparentAiAgentCore.Domain.Scenarios;

namespace TransparentAiAgentCore.Application.Scenarios;

/// <summary>
/// Evaluates conditions for WaitForCondition scenario steps.
/// </summary>
public interface IConditionEvaluator
{
    /// <summary>
    /// Evaluates a condition and returns true if the condition is met.
    /// </summary>
    /// <param name="condition">The condition type (e.g., "response_contains", "message_count").</param>
    /// <param name="parameters">Parameters for the condition evaluation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    Task<bool> EvaluateAsync(string condition, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken);
}
