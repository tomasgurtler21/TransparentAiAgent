namespace TransparentAiAgentCore.Domain.Memory;

/// <summary>
/// Result of a memory update operation.
/// </summary>
public record MemoryUpdateResult(
    bool Success,
    string? Error = null,
    int CharacterCount = 0,
    DateTime UpdatedAt = default);
