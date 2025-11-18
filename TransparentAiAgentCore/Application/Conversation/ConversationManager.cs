using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Infrastructure.Configuration;

namespace TransparentAiAgentCore.Application.Conversation;

/// <summary>
/// Manages conversation history and context window with simple truncation strategy
/// </summary>
public class ConversationManager : IConversationManager
{
    private const string ConfigKey_MessageLimit = "messageLimit";

    private readonly List<IMessage> _messages = new();
    private readonly object _lock = new();
    private readonly ITransparencyService _transparencyService;
    private readonly IConfigurationOverlay? _configurationOverlay;

    public Guid ConversationId { get; private set; }
    public int ContextWindowSize { get; private set; }
    public int InContextMessageCount => GetInContextMessages().Count;

    public event EventHandler<ContextStatusChangedEventArgs>? ContextStatusChanged;

    public ConversationManager(
        int contextWindowSize,
        ITransparencyService transparencyService,
        IConfigurationOverlay? configurationOverlay = null)
    {
        if (contextWindowSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(contextWindowSize),
                "Context window size must be greater than 0");

        _transparencyService = transparencyService
            ?? throw new ArgumentNullException(nameof(transparencyService));

        _configurationOverlay = configurationOverlay;

        ConversationId = Guid.NewGuid();
        ContextWindowSize = contextWindowSize;

        LogEvent("ConversationStarted", $"Conversation {ConversationId} started with context window size {contextWindowSize}");

        // Subscribe to overlay changes
        if (_configurationOverlay != null)
        {
            _configurationOverlay.OverlayChanged += OnConfigurationOverlayChanged;
            LogEvent("OverlaySubscribed", "Subscribed to configuration overlay changes");
        }
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

    public void ResetConversation()
    {
        lock (_lock)
        {
            var oldId = ConversationId;
            ConversationId = Guid.NewGuid();
            _messages.Clear();
            LogEvent("ConversationReset", $"Conversation reset from {oldId} to {ConversationId}");
        }
    }

    public void SetConversationId(Guid conversationId)
    {
        lock (_lock)
        {
            var oldId = ConversationId;
            ConversationId = conversationId;
            LogEvent("ConversationIdChanged", $"Conversation ID changed from {oldId} to {conversationId}");
        }
    }

    /// <summary>
    /// Updates the system prompt by replacing the first SystemMessage in conversation.
    /// If no SystemMessage exists, creates a new one.
    /// </summary>
    public void UpdateSystemPrompt(string newPrompt)
    {
        if (string.IsNullOrWhiteSpace(newPrompt))
            throw new ArgumentException("System prompt cannot be null or whitespace", nameof(newPrompt));

        lock (_lock)
        {
            // Find first system message
            var systemMessage = _messages.FirstOrDefault(m => m.Role == MessageRole.System) as SystemMessage;

            if (systemMessage != null)
            {
                // Replace existing system message (maintain immutability)
                var oldContent = systemMessage.Content;
                var index = _messages.IndexOf(systemMessage);
                var newSystemMessage = new SystemMessage(newPrompt);

                // Preserve context status from old message
                newSystemMessage.ContextStatus = systemMessage.ContextStatus;

                _messages[index] = newSystemMessage;
                LogEvent("SystemPromptUpdated", $"System prompt updated from '{oldContent}' to '{newPrompt}'");
            }
            else
            {
                // Create new system message at the beginning
                var newSystemMessage = new SystemMessage(newPrompt);
                _messages.Insert(0, newSystemMessage);
                LogEvent("SystemPromptCreated", $"New system prompt created: '{newPrompt}'");
            }
        }
    }

    /// <summary>
    /// Updates the context window size for the conversation.
    /// </summary>
    public void UpdateContextWindowSize(int newSize)
    {
        if (newSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(newSize), "Context window size must be greater than 0");

        lock (_lock)
        {
            var oldSize = ContextWindowSize;
            ContextWindowSize = newSize;
            LogEvent("ContextWindowSizeUpdated", $"Context window size updated from {oldSize} to {newSize}");

            // Re-apply truncation with new size
            TruncateIfNeeded();
        }
    }

    /// <summary>
    /// Re-evaluates context window truncation based on current effective limit.
    /// Truncates messages when count exceeds limit, restores when count is below limit.
    /// </summary>
    private void TruncateIfNeeded()
    {
        // Get effective context window size from overlay or use default
        var effectiveWindowSize = ContextWindowSize;
        if (_configurationOverlay != null)
        {
            effectiveWindowSize = _configurationOverlay.GetValue(ConfigKey_MessageLimit, ContextWindowSize);
            if (effectiveWindowSize != ContextWindowSize)
            {
                LogEvent("EffectiveContextWindowSize", $"Using overlay {ConfigKey_MessageLimit}: {effectiveWindowSize} (base: {ContextWindowSize})");
            }
        }

        var inContextMessages = _messages
            .Where(m => m.ContextStatus == MessageContextStatus.InContext)
            .ToList();

        var currentCount = inContextMessages.Count;

        // CASE 1: Too many messages - need to truncate
        if (currentCount > effectiveWindowSize)
        {
            int toTruncate = currentCount - effectiveWindowSize;

            var messagesToTruncate = inContextMessages
                .OrderBy(m => m.Role == MessageRole.System ? 1 : 0) // System messages last
                .ThenBy(m => m.Timestamp) // Oldest first
                .Take(toTruncate)
                .ToList();

            foreach (var message in messagesToTruncate)
            {
                var oldStatus = message.ContextStatus;
                message.ContextStatus = MessageContextStatus.TruncatedFromContext;

                ContextStatusChanged?.Invoke(this,
                    new ContextStatusChangedEventArgs(message.Id, oldStatus, message.ContextStatus));

                LogEvent("MessageTruncated", $"Message {message.Id} truncated from context");
            }

            LogEvent("ContextTruncated",
                $"Truncated {toTruncate} message(s). Current: {InContextMessageCount}/{effectiveWindowSize} in context");
        }
        // CASE 2: Room for more messages - restore truncated ones
        else if (currentCount < effectiveWindowSize)
        {
            int toRestore = effectiveWindowSize - currentCount;

            var truncatedMessages = _messages
                .Where(m => m.ContextStatus == MessageContextStatus.TruncatedFromContext)
                .OrderBy(m => m.Timestamp) // Restore oldest first (FIFO)
                .Take(toRestore)
                .ToList();

            if (truncatedMessages.Count > 0)
            {
                foreach (var message in truncatedMessages)
                {
                    var oldStatus = message.ContextStatus;
                    message.ContextStatus = MessageContextStatus.InContext;

                    ContextStatusChanged?.Invoke(this,
                        new ContextStatusChangedEventArgs(message.Id, oldStatus, message.ContextStatus));

                    LogEvent("MessageRestored", $"Message {message.Id} restored to context");
                }

                LogEvent("ContextRestored",
                    $"Restored {truncatedMessages.Count} message(s). Current: {InContextMessageCount}/{effectiveWindowSize} in context");
            }
        }
        // CASE 3: Perfect fit - no action needed
    }

    private void OnConfigurationOverlayChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        lock (_lock)
        {
            LogEvent("ConfigurationOverlayChanged",
                $"Change type: {e.Type}, Re-evaluating truncation");

            // Re-evaluate truncation with new overlay values
            TruncateIfNeeded();
        }
    }

    private void LogEvent(string eventType, string details)
    {
        _transparencyService?.LogEvent(
            new TransparencyEvent(
                Domain.Transparency.TransparencyEventType.ContextChange,
                System.Text.Json.JsonSerializer.Serialize(new { ConversationId, EventType = eventType }),
                details));
    }

    public void Dispose()
    {
        if (_configurationOverlay != null)
        {
            _configurationOverlay.OverlayChanged -= OnConfigurationOverlayChanged;
        }
    }
}
