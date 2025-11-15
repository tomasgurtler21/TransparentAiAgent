namespace TransparentAiAgentCore.Infrastructure.Configuration;

/// <summary>
/// Result of configuration validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed (no errors)
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Gets the list of validation errors
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    /// <param name="error">The error message to add</param>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
        }
    }
}
