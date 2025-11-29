namespace TransparentAiAgentCore.Domain.UIControl;

/// <summary>
/// Represents the highlighting state for UI components in teaching mode.
/// Immutable by design - updates create new instances.
/// </summary>
public record HighlightState
{
    /// <summary>
    /// Set of component IDs that are currently highlighted.
    /// Component IDs follow the pattern: {category}.{component-type}[.{instance-id}]
    /// Examples: "chat.context-indicators", "chat.message-123", "tools.panel"
    /// </summary>
    public IReadOnlySet<string> ActiveHighlights { get; init; } = new HashSet<string>();

    /// <summary>
    /// Timestamp of the last user-initiated highlight dismissal.
    /// Used for tracking user interactions with highlighting system.
    /// </summary>
    public DateTime? LastDismissalTime { get; init; }

    /// <summary>
    /// Creates a new state with the specified component highlighted.
    /// </summary>
    /// <param name="componentId">The component ID to highlight.</param>
    /// <returns>New HighlightState with the component added to ActiveHighlights.</returns>
    public HighlightState WithHighlight(string componentId)
    {
        var newSet = new HashSet<string>(ActiveHighlights) { componentId };
        return this with { ActiveHighlights = newSet };
    }

    /// <summary>
    /// Creates a new state with the specified component un-highlighted.
    /// </summary>
    /// <param name="componentId">The component ID to remove highlight from.</param>
    /// <returns>New HighlightState with the component removed from ActiveHighlights.</returns>
    public HighlightState WithoutHighlight(string componentId)
    {
        var newSet = new HashSet<string>(ActiveHighlights);
        newSet.Remove(componentId);
        return this with { ActiveHighlights = newSet };
    }

    /// <summary>
    /// Creates a new state with multiple components highlighted or un-highlighted.
    /// Efficient batch operation for updating multiple highlights at once.
    /// </summary>
    /// <param name="ids">Component IDs to update.</param>
    /// <param name="enabled">True to highlight, false to remove highlight.</param>
    /// <returns>New HighlightState with updated highlights.</returns>
    public HighlightState WithHighlights(IEnumerable<string> ids, bool enabled)
    {
        var newSet = new HashSet<string>(ActiveHighlights);
        foreach (var id in ids)
        {
            if (enabled)
                newSet.Add(id);
            else
                newSet.Remove(id);
        }
        return this with { ActiveHighlights = newSet };
    }

    /// <summary>
    /// Creates a new state with all highlights cleared.
    /// </summary>
    /// <returns>New HighlightState with empty ActiveHighlights.</returns>
    public HighlightState ClearAll() =>
        this with { ActiveHighlights = new HashSet<string>() };

    /// <summary>
    /// Checks if a specific component is currently highlighted.
    /// </summary>
    /// <param name="componentId">The component ID to check.</param>
    /// <returns>True if the component is highlighted, false otherwise.</returns>
    public bool IsHighlighted(string componentId) =>
        ActiveHighlights.Contains(componentId);
}
