namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Defines the visibility of a scenario system message.
/// </summary>
public enum MessageVisibility
{
    /// <summary>
    /// Message is visible only to the model (in context).
    /// </summary>
    ModelOnly,

    /// <summary>
    /// Message is visible only to the user (in UI).
    /// </summary>
    UserOnly,

    /// <summary>
    /// Message is visible to both model and user.
    /// </summary>
    Both
}
