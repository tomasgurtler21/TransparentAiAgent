namespace TransparentAiAgentCore.Infrastructure.DataPath;

/// <summary>
/// Service for resolving OS-appropriate data storage paths.
/// </summary>
public interface IDataPathService
{
    /// <summary>
    /// Gets the root directory for user data (uses ApplicationData).
    /// Example: C:\Users\{username}\AppData\Roaming\TransparentAiAgent
    /// </summary>
    string GetUserDataRoot();

    /// <summary>
    /// Gets the directory for long-term memory files.
    /// </summary>
    string GetMemoryDirectory();

    /// <summary>
    /// Gets the directory for conversation history.
    /// </summary>
    string GetConversationsDirectory();

    /// <summary>
    /// Gets the directory for logs.
    /// </summary>
    string GetLogsDirectory();

    /// <summary>
    /// Ensures all necessary directories exist.
    /// Creates them if they don't exist.
    /// </summary>
    void EnsureDirectoriesExist();
}
