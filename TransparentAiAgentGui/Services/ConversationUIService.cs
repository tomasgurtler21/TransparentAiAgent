using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.Streaming;
using TransparentAiAgentGui.Models;

namespace TransparentAiAgentGui.Services;

public class ConversationUIService : IConversationUIService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IConversationManager _conversationManager;
    private readonly List<UIMessage> _messages = new();
    private bool _isProcessing;
    private UIMessage? _currentStreamingMessage;
    private readonly object _streamingLock = new();

    public IReadOnlyList<UIMessage> Messages => _messages.AsReadOnly();
    public bool IsProcessing => _isProcessing;

    public event EventHandler? MessagesChanged;
    public event EventHandler<bool>? ProcessingStateChanged;
    public event EventHandler<StreamingMessageUpdate>? StreamingMessageUpdated;

    public ConversationUIService(
        IAgentOrchestrator orchestrator,
        IConversationManager conversationManager)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));

        // Subscribe to context status changes
        _conversationManager.ContextStatusChanged += OnContextStatusChanged;

        // Load existing messages if any
        RefreshMessages();
    }

    public async Task SendMessageAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty", nameof(content));

        SetProcessing(true);

        try
        {
            // Process user input through orchestrator
            await _orchestrator.ProcessUserInputAsync(content);

            // Refresh UI messages from conversation manager
            RefreshMessages();
        }
        catch
        {
            SetProcessing(false);
            throw;
        }
        finally
        {
            SetProcessing(false);
        }
    }

    public async Task SendMessageStreamingAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty", nameof(content));

        SetProcessing(true);

        try
        {
            // Create streaming assistant message placeholder
            var streamingMessage = new UIMessage
            {
                Id = Guid.NewGuid(),
                Role = TransparentAiAgentCore.Domain.Enums.MessageRole.Assistant,
                Content = string.Empty,
                Timestamp = DateTime.UtcNow,
                ContextStatus = TransparentAiAgentCore.Domain.Enums.MessageContextStatus.InContext
            };

            lock (_streamingLock)
            {
                _currentStreamingMessage = streamingMessage;
                _messages.Add(streamingMessage);
            }

            OnMessagesChanged();

            // Use MarkdownStreamingBuffer for intelligent buffering
            var buffer = new MarkdownStreamingBuffer();
            var contentBuilder = new System.Text.StringBuilder();
            var lastUpdate = DateTime.UtcNow;
            const int ThrottleMilliseconds = 50; // Max 20 updates/second

            // Process streaming response
            await foreach (var chunk in _orchestrator.ProcessUserInputStreamingAsync(content))
            {
                if (!string.IsNullOrEmpty(chunk.ContentDelta))
                {
                    contentBuilder.Append(chunk.ContentDelta);

                    // Use buffer to get renderable content
                    var renderableContent = buffer.AppendAndGetRenderable(chunk.ContentDelta);

                    // Throttle UI updates
                    var now = DateTime.UtcNow;
                    if (renderableContent != null && (now - lastUpdate).TotalMilliseconds >= ThrottleMilliseconds)
                    {
                        lock (_streamingLock)
                        {
                            if (_currentStreamingMessage != null)
                            {
                                _currentStreamingMessage.Content += renderableContent;

                                StreamingMessageUpdated?.Invoke(this, new StreamingMessageUpdate
                                {
                                    MessageId = _currentStreamingMessage.Id,
                                    Content = _currentStreamingMessage.Content,
                                    IsComplete = false
                                });
                            }
                        }
                        lastUpdate = now;
                    }
                }

                if (chunk.IsComplete)
                {
                    // Flush any remaining buffered content
                    var remainingContent = buffer.Flush();
                    if (!string.IsNullOrEmpty(remainingContent))
                    {
                        lock (_streamingLock)
                        {
                            if (_currentStreamingMessage != null)
                            {
                                _currentStreamingMessage.Content += remainingContent;
                            }
                        }
                    }

                    // Final update
                    lock (_streamingLock)
                    {
                        if (_currentStreamingMessage != null)
                        {
                            StreamingMessageUpdated?.Invoke(this, new StreamingMessageUpdate
                            {
                                MessageId = _currentStreamingMessage.Id,
                                Content = _currentStreamingMessage.Content,
                                IsComplete = true
                            });
                        }
                    }
                }
            }

            // Refresh messages from conversation manager to sync state
            RefreshMessages();
        }
        catch
        {
            SetProcessing(false);
            lock (_streamingLock)
            {
                _currentStreamingMessage = null;
            }
            throw;
        }
        finally
        {
            SetProcessing(false);
            lock (_streamingLock)
            {
                _currentStreamingMessage = null;
            }
        }
    }

    public async Task ClearConversationAsync()
    {
        _conversationManager.ClearConversation();
        _messages.Clear();
        OnMessagesChanged();
        await Task.CompletedTask;
    }

    private void RefreshMessages()
    {
        _messages.Clear();
        var domainMessages = _conversationManager.GetAllMessages();
        foreach (var domainMessage in domainMessages)
        {
            _messages.Add(UIMessage.FromDomainMessage(domainMessage));
        }
        OnMessagesChanged();
    }

    private void OnContextStatusChanged(object? sender, ContextStatusChangedEventArgs e)
    {
        // Refresh messages when context status changes
        RefreshMessages();
    }

    private void SetProcessing(bool isProcessing)
    {
        if (_isProcessing != isProcessing)
        {
            _isProcessing = isProcessing;
            ProcessingStateChanged?.Invoke(this, _isProcessing);
        }
    }

    private void OnMessagesChanged()
    {
        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }
}
