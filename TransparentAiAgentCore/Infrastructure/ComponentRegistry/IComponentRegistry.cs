namespace TransparentAiAgentCore.Infrastructure.ComponentRegistry;

/// <summary>
/// Registry for tracking highlightable UI components.
/// Components register themselves with unique IDs to enable agent-driven highlighting.
/// Thread-safe for concurrent registration from multiple components.
/// </summary>
public interface IComponentRegistry
{
    /// <summary>
    /// Registers a component as highlightable.
    /// </summary>
    /// <param name="componentId">Unique component ID (e.g., "chat.context-indicators", "chat.message-123").</param>
    /// <param name="description">Human-readable description for the agent.</param>
    /// <param name="category">Component category for grouping (e.g., "chat", "tools", "config").</param>
    void Register(string componentId, string description, string category);

    /// <summary>
    /// Unregisters a component (typically called in Dispose).
    /// </summary>
    /// <param name="componentId">The component ID to unregister.</param>
    void Unregister(string componentId);

    /// <summary>
    /// Gets all registered components.
    /// </summary>
    /// <returns>Collection of all registered components.</returns>
    IEnumerable<ComponentRegistration> GetAll();

    /// <summary>
    /// Gets components filtered by category.
    /// </summary>
    /// <param name="category">Category to filter by (case-insensitive).</param>
    /// <returns>Components in the specified category.</returns>
    IEnumerable<ComponentRegistration> GetByCategory(string category);

    /// <summary>
    /// Checks if a component ID is registered.
    /// Used for validation before highlighting.
    /// </summary>
    /// <param name="componentId">The component ID to check.</param>
    /// <returns>True if registered, false otherwise.</returns>
    bool IsRegistered(string componentId);
}

/// <summary>
/// Record representing a registered component.
/// </summary>
public record ComponentRegistration
{
    /// <summary>
    /// Unique component identifier.
    /// Pattern: {category}.{component-type}[.{instance-id}]
    /// </summary>
    public required string ComponentId { get; init; }

    /// <summary>
    /// Human-readable description shown to the agent.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Category for grouping (chat, tools, config, etc.).
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// When the component was registered (UTC).
    /// </summary>
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
}
