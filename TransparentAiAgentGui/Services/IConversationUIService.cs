using TransparentAiAgentCore.Domain.ConversationHistory;
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
    /// Gets the current conversation ID
    /// </summary>
    Guid CurrentConversationId { get; }

    /// <summary>
    /// Send user message to agent
    /// </summary>
    Task SendMessageAsync(string content);

    /// <summary>
    /// Clear conversation
    /// </summary>
    Task ClearConversationAsync();

    /// <summary>
    /// Load a conversation from history
    /// </summary>
    Task LoadConversationAsync(Conversation conversation);

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

    /// <summary>
    /// Raised when memory state changes (e.g., memory created/updated via tool)
    /// </summary>
    event EventHandler? MemoryStateChanged;

    /// <summary>
    /// Gets whether long-term memory is currently enabled
    /// </summary>
    bool IsMemoryEnabled { get; }

    /// <summary>
    /// Enable or disable long-term memory for the current conversation
    /// </summary>
    Task SetMemoryEnabledAsync(bool enabled);

    /// <summary>
    /// End the conversation and optionally trigger memory update prompt
    /// </summary>
    Task EndConversationAsync();
}
