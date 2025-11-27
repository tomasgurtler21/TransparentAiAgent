namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// DTO for deserializing mock tool response configurations from JSON scenario files.
/// This is serializable for JSON, while MockToolResponse is the runtime value object.
/// </summary>
public class MockToolResponseConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether this response represents a successful tool execution.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the content to return for successful executions.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the error message to return for failed executions.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the simulated execution time in milliseconds.
    /// If null or zero, no artificial delay will be added.
    /// </summary>
    public int? SimulatedExecutionTimeMs { get; set; }

    /// <summary>
    /// Converts this DTO to a runtime MockToolResponse value object.
    /// </summary>
    /// <returns>A MockToolResponse instance.</returns>
    public MockToolResponse ToMockToolResponse()
    {
        TimeSpan? executionTime = SimulatedExecutionTimeMs.HasValue && SimulatedExecutionTimeMs.Value > 0
            ? TimeSpan.FromMilliseconds(SimulatedExecutionTimeMs.Value)
            : null;

        if (IsSuccess)
        {
            if (string.IsNullOrEmpty(Content))
            {
                throw new InvalidOperationException(
                    "Content is required for success responses in MockToolResponseConfig");
            }
            return MockToolResponse.Success(Content, executionTime);
        }
        else
        {
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                throw new InvalidOperationException(
                    "ErrorMessage is required for error responses in MockToolResponseConfig");
            }
            return MockToolResponse.Error(ErrorMessage, executionTime);
        }
    }
}
