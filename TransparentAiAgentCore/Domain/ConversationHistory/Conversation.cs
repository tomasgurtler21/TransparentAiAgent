using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Domain.ConversationHistory;

/// <summary>
/// Represents a conversation with its messages and metadata.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Human-readable name for the conversation
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When the conversation was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification time
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Messages in the conversation
    /// </summary>
    public List<IMessage> Messages { get; set; } = new();

    /// <summary>
    /// Generates a conversation name from messages.
    /// Extracts first user message content, truncates to 50 chars, and sanitizes.
    /// Falls back to timestamp if no user message found.
    /// </summary>
    public static string GenerateName(IEnumerable<IMessage> messages)
    {
        // Find first user message
        var firstUserMessage = messages.FirstOrDefault(m => m.Role == Enums.MessageRole.User);

        // If no user message or empty content, use timestamp fallback
        if (firstUserMessage == null || string.IsNullOrWhiteSpace(firstUserMessage.Content))
        {
            return $"Conversation {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        }

        // Get the content and sanitize
        var name = firstUserMessage.Content.Trim();
        name = SanitizeFileName(name);

        // Truncate to 50 characters if needed
        if (name.Length > 50)
        {
            name = name.Substring(0, 50) + "...";
        }

        return name;
    }

    /// <summary>
    /// Sanitizes a string to be used as a filename by replacing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Invalid filename characters: < > : " / \ | ? *
        var invalidChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        var result = input;
        foreach (var invalidChar in invalidChars)
        {
            result = result.Replace(invalidChar, '_');
        }

        return result;
    }
}
