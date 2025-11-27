namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Represents a predefined response for a scenario mock tool.
/// Encapsulates the response behavior (success/failure), content, and optional execution timing.
/// Used in teaching scenarios to demonstrate tool behavior with controlled, deterministic responses.
/// </summary>
public class MockToolResponse
{
    /// <summary>
    /// Gets a value indicating whether this response represents a successful tool execution.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the content to return for successful executions.
    /// For error responses, this will be null.
    /// </summary>
    public string? Content { get; }

    /// <summary>
    /// Gets the error message to return for failed executions.
    /// For successful responses, this will be null.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the simulated execution time for this response.
    /// If null, the executor will return immediately without artificial delay.
    /// </summary>
    public TimeSpan? SimulatedExecutionTime { get; }

    private MockToolResponse(bool isSuccess, string? content, string? errorMessage, TimeSpan? simulatedExecutionTime)
    {
        IsSuccess = isSuccess;
        Content = content;
        ErrorMessage = errorMessage;
        SimulatedExecutionTime = simulatedExecutionTime;
    }

    /// <summary>
    /// Creates a successful mock tool response.
    /// </summary>
    /// <param name="content">The content to return.</param>
    /// <param name="executionTime">Optional simulated execution time.</param>
    /// <returns>A successful MockToolResponse.</returns>
    public static MockToolResponse Success(string content, TimeSpan? executionTime = null)
    {
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Content cannot be null or empty for success responses", nameof(content));

        return new MockToolResponse(
            isSuccess: true,
            content: content,
            errorMessage: null,
            simulatedExecutionTime: executionTime);
    }

    /// <summary>
    /// Creates a failed mock tool response with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message to return.</param>
    /// <param name="executionTime">Optional simulated execution time.</param>
    /// <returns>A failed MockToolResponse.</returns>
    public static MockToolResponse Error(string errorMessage, TimeSpan? executionTime = null)
    {
        if (string.IsNullOrEmpty(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty for error responses", nameof(errorMessage));

        return new MockToolResponse(
            isSuccess: false,
            content: null,
            errorMessage: errorMessage,
            simulatedExecutionTime: executionTime);
    }
}
