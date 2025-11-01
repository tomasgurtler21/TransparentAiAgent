using TransparentAiAgentGui.Models;

namespace TransparentAiAgentGui.Services;

public interface IConversationUIService
{
    /// <summary>
    /// Get all messages for display
    /// </summary>
    IReadOnlyList<UIMessage> Messages { get; }

    /// <summary>
    /// Is agent currently processing
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Send user message to agent
    /// </summary>
    Task SendMessageAsync(string content);

    /// <summary>
    /// Clear conversation
    /// </summary>
    Task ClearConversationAsync();

    /// <summary>
    /// Send user message to agent with streaming response
    /// </summary>
    Task SendMessageStreamingAsync(string content);

    /// <summary>
    /// Raised when messages change
    /// </summary>
    event EventHandler? MessagesChanged;

    /// <summary>
    /// Raised when processing state changes
    /// </summary>
    event EventHandler<bool>? ProcessingStateChanged;

    /// <summary>
    /// Raised when a streaming message is updated
    /// </summary>
    event EventHandler<StreamingMessageUpdate>? StreamingMessageUpdated;
}
