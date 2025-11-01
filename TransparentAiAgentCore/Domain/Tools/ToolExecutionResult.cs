namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// Represents the result of a tool execution.
/// Contains success status, content, error information, and execution metrics.
/// </summary>
public class ToolExecutionResult
{
    /// <summary>
    /// Gets a value indicating whether the tool execution was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the result content (JSON or text).
    /// For successful executions, contains the tool output.
    /// For failures, contains error details.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the error message if the execution failed, otherwise null.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the time taken to execute the tool.
    /// </summary>
    public TimeSpan ExecutionTime { get; }

    private ToolExecutionResult(bool success, string content, string? errorMessage, TimeSpan executionTime)
    {
        IsSuccess = success;
        Content = content;
        ErrorMessage = errorMessage;
        ExecutionTime = executionTime;
    }

    /// <summary>
    /// Creates a successful tool execution result.
    /// </summary>
    /// <param name="content">The result content from the tool.</param>
    /// <param name="executionTime">The time taken to execute the tool.</param>
    /// <returns>A successful <see cref="ToolExecutionResult"/>.</returns>
    public static ToolExecutionResult Success(string content, TimeSpan executionTime)
    {
        return new ToolExecutionResult(
            success: true,
            content: content,
            errorMessage: null,
            executionTime: executionTime);
    }

    /// <summary>
    /// Creates a failed tool execution result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="executionTime">The time taken before the failure occurred.</param>
    /// <returns>A failed <see cref="ToolExecutionResult"/>.</returns>
    public static ToolExecutionResult Failure(string errorMessage, TimeSpan executionTime)
    {
        return new ToolExecutionResult(
            success: false,
            content: $"Error: {errorMessage}",
            errorMessage: errorMessage,
            executionTime: executionTime);
    }
}
