using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Domain.Transparency;

namespace TransparentAiAgentCore.Application.Conversation;

/// <summary>
/// Manages conversation history and context window with simple truncation strategy
/// </summary>
public class ConversationManager : IConversationManager
{
    private readonly List<IMessage> _messages = new();
    private readonly object _lock = new();
    private readonly ITransparencyService _transparencyService;

    public Guid ConversationId { get; }
    public int ContextWindowSize { get; }
    public int InContextMessageCount => GetInContextMessages().Count;

    public event EventHandler<ContextStatusChangedEventArgs>? ContextStatusChanged;

    public ConversationManager(
        int contextWindowSize,
        ITransparencyService transparencyService)
    {
        if (contextWindowSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(contextWindowSize),
                "Context window size must be greater than 0");

        _transparencyService = transparencyService
            ?? throw new ArgumentNullException(nameof(transparencyService));

        ConversationId = Guid.NewGuid();
        ContextWindowSize = contextWindowSize;

        LogEvent("ConversationStarted", $"Conversation {ConversationId} started with context window size {contextWindowSize}");
    }

    public void AddMessage(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        lock (_lock)
        {
            _messages.Add(message);

            // After adding, check if we need to truncate
            TruncateIfNeeded();

            LogEvent("MessageAdded", $"Message {message.Id} added to conversation. Total: {_messages.Count}, In context: {InContextMessageCount}");
        }
    }

    public IReadOnlyList<IMessage> GetAllMessages()
    {
        lock (_lock)
        {
            return _messages.ToList(); // Return defensive copy
        }
    }

    public IReadOnlyList<IMessage> GetInContextMessages()
    {
        lock (_lock)
        {
            return _messages
                .Where(m => m.ContextStatus == MessageContextStatus.InContext)
                .ToList();
        }
    }

    public void ClearConversation()
    {
        lock (_lock)
        {
            _messages.Clear();
            LogEvent("ConversationCleared", $"Conversation {ConversationId} cleared");
        }
    }

    /// <summary>
    /// Truncate oldest messages if we exceed context window size.
    /// Strategy: Remove oldest InContext messages first, keeping system messages if possible.
    /// </summary>
    private void TruncateIfNeeded()
    {
        var inContextMessages = _messages
            .Where(m => m.ContextStatus == MessageContextStatus.InContext)
            .ToList();

        if (inContextMessages.Count <= ContextWindowSize)
            return; // No truncation needed

        // How many messages to truncate
        int toTruncate = inContextMessages.Count - ContextWindowSize;

        // Get messages to truncate (oldest first, but prefer non-system messages)
        var messagesToTruncate = inContextMessages
            .OrderBy(m => m.Role == MessageRole.System ? 1 : 0) // System messages last priority for truncation
            .ThenBy(m => m.Timestamp) // Oldest first
            .Take(toTruncate)
            .ToList();

        foreach (var message in messagesToTruncate)
        {
            var oldStatus = message.ContextStatus;
            message.ContextStatus = MessageContextStatus.TruncatedFromContext;

            // Raise event
            ContextStatusChanged?.Invoke(
                this,
                new ContextStatusChangedEventArgs(message.Id, oldStatus, message.ContextStatus));

            LogEvent("MessageTruncated", $"Message {message.Id} truncated from context");
        }

        LogEvent("ContextTruncated", $"{toTruncate} messages truncated. In context: {InContextMessageCount}/{ContextWindowSize}");
    }

    private void LogEvent(string eventType, string details)
    {
        _transparencyService?.LogEvent(
            new TransparencyEvent(
                Domain.Transparency.TransparencyEventType.ContextChange,
                System.Text.Json.JsonSerializer.Serialize(new { ConversationId, EventType = eventType }),
                details));
    }
}
