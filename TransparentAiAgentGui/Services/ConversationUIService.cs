using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.Streaming;
using TransparentAiAgentGui.Models;

namespace TransparentAiAgentGui.Services;

public class ConversationUIService : IConversationUIService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IConversationManager _conversationManager;
    private readonly IScenarioExecutor _scenarioExecutor;
    private readonly List<UIMessage> _messages = new();
    private bool _isProcessing;
    private UIMessage? _currentStreamingMessage;
    private readonly object _streamingLock = new();
    private readonly HashSet<string> _pendingAutoMessages = new();
    private readonly object _autoMessageLock = new();
    private bool _isScenarioStreaming = false;
    private readonly object _messagesLock = new();

    // Return a snapshot copy to prevent collection modification exceptions during enumeration
    public IReadOnlyList<UIMessage> Messages
    {
        get
        {
            lock (_messagesLock)
            {
                return _messages.ToList();
            }
        }
    }

    public bool IsProcessing => _isProcessing;

    public event EventHandler? MessagesChanged;
    public event EventHandler<bool>? ProcessingStateChanged;
    public event EventHandler<StreamingMessageUpdate>? StreamingMessageUpdated;

    public ConversationUIService(
        IAgentOrchestrator orchestrator,
        IConversationManager conversationManager,
        IScenarioExecutor scenarioExecutor)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _scenarioExecutor = scenarioExecutor ?? throw new ArgumentNullException(nameof(scenarioExecutor));

        // Subscribe to context status changes
        _conversationManager.ContextStatusChanged += OnContextStatusChanged;

        // Subscribe to scenario auto-message events
        _scenarioExecutor.AutoMessageSent += OnAutoMessageSent;

        // Subscribe to scenario streaming events for real-time UI updates
        _scenarioExecutor.StreamingUpdate += OnScenarioStreamingUpdate;

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
            await _orchestrator.ProcessUserInputAsync(new DirectUserMessage(content));

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
            // Check if this is an auto-message
            bool isAutoMessage = false;
            lock (_autoMessageLock)
            {
                if (_pendingAutoMessages.Contains(content))
                {
                    isAutoMessage = true;
                    _pendingAutoMessages.Remove(content);
                }
            }

            // Add user message to UI immediately for instant feedback
            var userMessage = new UIMessage
            {
                Id = Guid.NewGuid(),
                Role = TransparentAiAgentCore.Domain.Enums.MessageRole.User,
                Content = content,
                Timestamp = DateTime.UtcNow,
                ContextStatus = TransparentAiAgentCore.Domain.Enums.MessageContextStatus.InContext,
                IsAutoMessage = isAutoMessage
            };

            lock (_messagesLock)
            {
                _messages.Add(userMessage);
            }
            OnMessagesChanged();

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
            }

            lock (_messagesLock)
            {
                _messages.Add(streamingMessage);
            }

            OnMessagesChanged();

            // Use MarkdownStreamingBuffer for intelligent buffering
            var buffer = new MarkdownStreamingBuffer();
            var contentBuilder = new System.Text.StringBuilder();
            var lastUpdate = DateTime.UtcNow;
            const int ThrottleMilliseconds = 50; // Max 20 updates/second

            // Process streaming response
            await foreach (var chunk in _orchestrator.ProcessUserInputStreamingAsync(new DirectUserMessage(content)))
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
        lock (_messagesLock)
        {
            _messages.Clear();
        }
        lock (_autoMessageLock)
        {
            _pendingAutoMessages.Clear();
        }
        OnMessagesChanged();
        await Task.CompletedTask;
    }

    private void RefreshMessages()
    {
        var domainMessages = _conversationManager.GetAllMessages();
        var newMessages = new List<UIMessage>();

        foreach (var domainMessage in domainMessages)
        {
            var uiMessage = UIMessage.FromDomainMessage(domainMessage);

            // Check if this user message is an auto-message
            if (uiMessage.Role == TransparentAiAgentCore.Domain.Enums.MessageRole.User)
            {
                lock (_autoMessageLock)
                {
                    if (_pendingAutoMessages.Contains(uiMessage.Content))
                    {
                        uiMessage.IsAutoMessage = true;
                        _pendingAutoMessages.Remove(uiMessage.Content);
                    }
                }
            }

            newMessages.Add(uiMessage);
        }

        lock (_messagesLock)
        {
            _messages.Clear();
            _messages.AddRange(newMessages);
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

    private void OnAutoMessageSent(object? sender, AutoMessageSentEventArgs e)
    {
        // Track this message content as an auto-message
        lock (_autoMessageLock)
        {
            _pendingAutoMessages.Add(e.MessageContent);
        }
    }

    private void OnScenarioStreamingUpdate(object? sender, ScenarioStreamingUpdateEventArgs e)
    {
        lock (_streamingLock)
        {
            // If this is the first chunk of a scenario streaming session, set up the messages
            if (!_isScenarioStreaming)
            {
                _isScenarioStreaming = true;
                SetProcessing(true);

                // Refresh to get the user message that was added by orchestrator
                RefreshMessages();

                // Create streaming assistant message placeholder
                var streamingMessage = new UIMessage
                {
                    Id = Guid.NewGuid(),
                    Role = TransparentAiAgentCore.Domain.Enums.MessageRole.Assistant,
                    Content = string.Empty,
                    Timestamp = DateTime.UtcNow,
                    ContextStatus = TransparentAiAgentCore.Domain.Enums.MessageContextStatus.InContext
                };

                _currentStreamingMessage = streamingMessage;

                lock (_messagesLock)
                {
                    _messages.Add(streamingMessage);
                }
                OnMessagesChanged();
            }

            // Update current streaming message with content delta
            if (_currentStreamingMessage != null && !string.IsNullOrEmpty(e.ContentDelta))
            {
                _currentStreamingMessage.Content += e.ContentDelta;

                // Fire event to notify UI of streaming update
                StreamingMessageUpdated?.Invoke(this, new StreamingMessageUpdate
                {
                    MessageId = _currentStreamingMessage.Id,
                    Content = _currentStreamingMessage.Content,
                    IsComplete = e.IsComplete
                });
            }

            // If streaming is complete, finalize the message
            if (e.IsComplete)
            {
                _isScenarioStreaming = false;
                _currentStreamingMessage = null;
                SetProcessing(false);
                RefreshMessages();
            }
        }
    }
}
