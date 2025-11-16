using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Domain.Memory;

/// <summary>
/// Service for managing long-term memory storage.
/// Provides mode-aware memory persistence using markdown files.
/// </summary>
public interface ILongTermMemoryService
{
    /// <summary>
    /// Reads the memory file for the specified mode.
    /// Returns empty string if file doesn't exist.
    /// </summary>
    Task<string> ReadMemoryAsync(AppMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the memory file for the specified mode.
    /// Creates file if it doesn't exist.
    /// </summary>
    Task<MemoryUpdateResult> UpdateMemoryAsync(
        AppMode mode,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if memory exists for the specified mode.
    /// </summary>
    Task<bool> HasMemoryAsync(AppMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last update timestamp for the specified mode.
    /// Returns null if file doesn't exist.
    /// </summary>
    Task<DateTime?> GetLastUpdateTimeAsync(AppMode mode, CancellationToken cancellationToken = default);
}
