using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.ConversationHistory;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Streaming;
using TransparentAiAgentGui.Models;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentGui.Services;

public class ConversationUIService : IConversationUIService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IConversationManager _conversationManager;
    private readonly IScenarioExecutor _scenarioExecutor;
    private readonly IConversationHistoryManager _historyManager;
    private readonly ILongTermMemoryService? _memoryService;
    private readonly IAppModeService? _appModeService;
    private readonly LongTermMemoryConfiguration? _memoryConfig;
    private readonly ILogger<ConversationUIService>? _logger;
    private readonly List<UIMessage> _messages = new();
    private bool _isProcessing;
    private UIMessage? _currentStreamingMessage;
    private readonly object _streamingLock = new();
    private readonly HashSet<string> _pendingAutoMessages = new();
    private readonly object _autoMessageLock = new();
    private bool _isScenarioStreaming = false;
    private MarkdownStreamingBuffer? _scenarioStreamingBuffer; // Buffer for scenario streaming (separate from normal streaming)
    private readonly object _messagesLock = new();
    private string? _baseSystemPrompt; // Original system prompt without memory

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

    public Guid CurrentConversationId => _conversationManager.ConversationId;

    public bool IsMemoryEnabled { get; private set; }

    public event EventHandler? MessagesChanged;
    public event EventHandler<bool>? ProcessingStateChanged;
    public event EventHandler<StreamingMessageUpdate>? StreamingMessageUpdated;
    public event EventHandler? MemoryStateChanged;

    public ConversationUIService(
        IAgentOrchestrator orchestrator,
        IConversationManager conversationManager,
        IScenarioExecutor scenarioExecutor,
        IConversationHistoryManager historyManager,
        ILongTermMemoryService? memoryService = null,
        IAppModeService? appModeService = null,
        LongTermMemoryConfiguration? memoryConfig = null,
        ILogger<ConversationUIService>? logger = null)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _scenarioExecutor = scenarioExecutor ?? throw new ArgumentNullException(nameof(scenarioExecutor));
        _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        _memoryService = memoryService;
        _appModeService = appModeService;
        _memoryConfig = memoryConfig;
        _logger = logger;

        // Subscribe to context status changes
        _conversationManager.ContextStatusChanged += OnContextStatusChanged;

        // Subscribe to scenario auto-message events
        _scenarioExecutor.AutoMessageSent += OnAutoMessageSent;

        // Subscribe to scenario streaming events for real-time UI updates
        _scenarioExecutor.StreamingUpdate += OnScenarioStreamingUpdate;

        // Subscribe to mode changes for memory loading
        if (_appModeService != null)
        {
            _appModeService.ModeChanged += OnModeChanged;
        }

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
            // Ensure memory is loaded before processing (in case conversation was just initialized)
            await RefreshMemoryStateAsync();

            // Process user input through orchestrator
            await _orchestrator.ProcessUserInputAsync(new DirectUserMessage(content));

            // Refresh UI messages from conversation manager
            RefreshMessages();

            // Refresh memory state (in case LLM updated memory via tool)
            await RefreshMemoryStateAsync();

            // Auto-save conversation after message processing
            _ = Task.Run(async () => await AutoSaveConversationAsync());
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
            // Ensure memory is loaded before processing (in case conversation was just initialized)
            await RefreshMemoryStateAsync();

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

            // ✅ FIX: Create buffer for the initial streaming placeholder
            // Note: Buffer logging is optional - main diagnostics are in this service
            MarkdownStreamingBuffer? buffer = new MarkdownStreamingBuffer(null);
            var lastUpdate = DateTime.UtcNow;
            const int ThrottleMilliseconds = 30; // ~33 updates/second for smooth streaming

            // 🔍 DIAGNOSTIC: Track chunk processing
            int chunkNumber = 0;
            int nullRenderableCount = 0;
            int nonNullRenderableCount = 0;

            // Process streaming response
            await foreach (var chunk in _orchestrator.ProcessUserInputStreamingAsync(new DirectUserMessage(content)))
            {
                chunkNumber++;

                // Process ALL chunks, even with empty ContentDelta (signal chunks)
                // Use buffer to get renderable content (buffer created when placeholder created)
                var renderableContent = buffer?.AppendAndGetRenderable(chunk.ContentDelta ?? string.Empty);

                // 🔍 DIAGNOSTIC: Log chunk processing details
                var contentDeltaDisplay = chunk.ContentDelta == null ? "NULL" :
                                         string.IsNullOrEmpty(chunk.ContentDelta) ? "EMPTY" :
                                         chunk.ContentDelta.Length > 20 ? $"'{chunk.ContentDelta.Substring(0, 20)}...'" :
                                         $"'{chunk.ContentDelta}'";
                var renderableDisplay = renderableContent == null ? "NULL" :
                                       renderableContent.Length > 30 ? $"'{renderableContent.Substring(0, 30)}...'" :
                                       $"'{renderableContent}'";

                if (renderableContent == null)
                    nullRenderableCount++;
                else
                    nonNullRenderableCount++;

                _logger?.LogInformation("[STREAM DEBUG #{ChunkNumber}] ContentDelta={ContentDelta}, Renderable={Renderable}, NullCount={NullCount}, NonNullCount={NonNullCount}",
                    chunkNumber, contentDeltaDisplay, renderableDisplay, nullRenderableCount, nonNullRenderableCount);

                // Throttle UI updates (only if we have actual renderable content)
                var now = DateTime.UtcNow;
                if (renderableContent != null && (now - lastUpdate).TotalMilliseconds >= ThrottleMilliseconds)
                {
                    // 🔍 DIAGNOSTIC: Log UI update trigger
                    _logger?.LogInformation("[STREAM DEBUG #{ChunkNumber}] ✅ UI UPDATE TRIGGERED - Elapsed: {Elapsed:F1}ms",
                        chunkNumber, (now - lastUpdate).TotalMilliseconds);

                    lock (_streamingLock)
                    {
                        // If we have content but no streaming message (e.g., after tool execution),
                        // create a new streaming placeholder for the next LLM response
                        if (_currentStreamingMessage == null)
                        {
                            // ✅ FIX: Create a FRESH buffer for this new placeholder
                            buffer = new MarkdownStreamingBuffer(null);

                            var newStreamingMessage = new UIMessage
                            {
                                Id = Guid.NewGuid(),
                                Role = TransparentAiAgentCore.Domain.Enums.MessageRole.Assistant,
                                Content = string.Empty,
                                Timestamp = DateTime.UtcNow,
                                ContextStatus = TransparentAiAgentCore.Domain.Enums.MessageContextStatus.InContext
                            };
                            _currentStreamingMessage = newStreamingMessage;

                            lock (_messagesLock)
                            {
                                _messages.Add(newStreamingMessage);
                            }
                            OnMessagesChanged();
                        }

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

                // Handle status changes - refresh messages when tools are being executed
                if (chunk.Status == StreamingStatus.ExecutingTools)
                {
                    // CRITICAL: Stop updating the streaming placeholder since we're switching to tool execution
                    // The streaming placeholder was for the LLM's text response, but now we have tool calls
                    lock (_streamingLock)
                    {
                        _currentStreamingMessage = null;
                        // ✅ FIX: Reset buffer so next streaming placeholder gets a fresh one
                        buffer = null;
                    }

                    // Refresh messages to show tool calls that were just added to conversation
                    // This will clear the streaming placeholder and show the real tool call messages
                    RefreshMessages();
                }

                // Handle ToolResultsReady status - refresh messages to show tool results
                if (chunk.Status == StreamingStatus.ToolResultsReady)
                {
                    // Refresh messages to show tool results that were just added to conversation
                    RefreshMessages();
                }

                // ✅ FIX: Handle Error status - clean up on API errors
                if (chunk.Status == StreamingStatus.Error)
                {
                    lock (_streamingLock)
                    {
                        _currentStreamingMessage = null;
                        buffer = null;
                    }

                    SetProcessing(false);
                    RefreshMessages();
                    break; // Exit the streaming loop
                }

                if (chunk.IsComplete)
                {
                    // Flush any remaining buffered content
                    var remainingContent = buffer?.Flush();
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

            // 🔍 DIAGNOSTIC: Log streaming completion summary
            var nullRatio = chunkNumber > 0 ? (double)nullRenderableCount / chunkNumber * 100 : 0;
            _logger?.LogInformation("[STREAM DEBUG SUMMARY] Total chunks: {TotalChunks}, Null renderable: {NullCount}, Non-null renderable: {NonNullCount}, Ratio: {Ratio:F1}% null",
                chunkNumber, nullRenderableCount, nonNullRenderableCount, nullRatio);

            // Refresh messages from conversation manager to sync state
            RefreshMessages();

            // Refresh memory state (in case LLM updated memory via tool)
            await RefreshMemoryStateAsync();

            // Auto-save conversation after message processing
            _ = Task.Run(async () => await AutoSaveConversationAsync());
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
        _conversationManager.ResetConversation();
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

    public async Task LoadConversationAsync(Conversation conversation)
    {
        if (conversation == null)
            throw new ArgumentNullException(nameof(conversation));

        // Clear current conversation
        _conversationManager.ClearConversation();

        // Set the conversation ID to match the loaded conversation
        _conversationManager.SetConversationId(conversation.ConversationId);

        // Load messages from the conversation
        foreach (var message in conversation.Messages)
        {
            _conversationManager.AddMessage(message);
        }

        // Refresh UI messages
        RefreshMessages();

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
        var chunk = e.Chunk;

        // If this is the first chunk of a scenario streaming session, set up the messages
        if (!_isScenarioStreaming)
        {
            lock (_streamingLock)
            {
                _isScenarioStreaming = true;
                SetProcessing(true);

                // Refresh to get the user message that was added by orchestrator
                RefreshMessages();

                // ✅ Create fresh buffer for this scenario streaming session
                _scenarioStreamingBuffer = new MarkdownStreamingBuffer();

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
        }

        // Handle content deltas - update streaming placeholder (process ALL chunks)
        var renderableContent = _scenarioStreamingBuffer?.AppendAndGetRenderable(chunk.ContentDelta ?? string.Empty);

        if (renderableContent != null)
        {
            lock (_streamingLock)
            {
                // If no streaming message exists (e.g., after tool execution), create a new one
                if (_currentStreamingMessage == null)
                {
                    // ✅ Create fresh buffer for new placeholder (Bug 2 fix for scenarios)
                    _scenarioStreamingBuffer = new MarkdownStreamingBuffer();

                    var newStreamingMessage = new UIMessage
                    {
                        Id = Guid.NewGuid(),
                        Role = TransparentAiAgentCore.Domain.Enums.MessageRole.Assistant,
                        Content = string.Empty,
                        Timestamp = DateTime.UtcNow,
                        ContextStatus = TransparentAiAgentCore.Domain.Enums.MessageContextStatus.InContext
                    };
                    _currentStreamingMessage = newStreamingMessage;

                    lock (_messagesLock)
                    {
                        _messages.Add(newStreamingMessage);
                    }
                    OnMessagesChanged();
                }

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
        }

        // ✅ FIX: Handle ExecutingTools status - show tool calls immediately!
        if (chunk.Status == StreamingStatus.ExecutingTools)
        {
            lock (_streamingLock)
            {
                _currentStreamingMessage = null;
                // Reset buffer for next response
                _scenarioStreamingBuffer = null;
            }

            // Refresh messages to show tool calls that were just added to conversation
            RefreshMessages();
        }

        // Handle ToolResultsReady status - refresh messages to show tool results
        if (chunk.Status == StreamingStatus.ToolResultsReady)
        {
            // Refresh messages to show tool results that were just added to conversation
            RefreshMessages();
        }

        // ✅ FIX: Handle Error status - clean up UI state on API errors
        if (chunk.Status == StreamingStatus.Error)
        {
            lock (_streamingLock)
            {
                _isScenarioStreaming = false;
                _currentStreamingMessage = null;
                _scenarioStreamingBuffer = null;
            }

            SetProcessing(false);
            RefreshMessages();
            return; // Don't process completion logic
        }

        // If streaming is complete, finalize
        if (chunk.IsComplete)
        {
            // Flush any remaining buffered content
            var remainingContent = _scenarioStreamingBuffer?.Flush();
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

                _isScenarioStreaming = false;
                _currentStreamingMessage = null;
                _scenarioStreamingBuffer = null;
            }

            SetProcessing(false);
            RefreshMessages();
        }
    }

    /// <summary>
    /// Auto-saves the current conversation to persistent storage.
    /// Runs asynchronously without blocking UI and handles failures gracefully.
    /// </summary>
    private async Task AutoSaveConversationAsync()
    {
        try
        {
            var messages = _conversationManager.GetAllMessages();

            // Skip saving if conversation is empty
            if (messages.Count == 0)
            {
                return;
            }

            var conversationId = _conversationManager.ConversationId;

            // Fire-and-forget: save in background without blocking
            await _historyManager.SaveCurrentConversationAsync(conversationId, messages);
        }
        catch (Exception)
        {
            // Log error but don't throw - auto-save failures shouldn't crash the app
            // In production, this would log to ILogger
        }
    }

    public async Task SetMemoryEnabledAsync(bool enabled)
    {
        if (_memoryService == null || _appModeService == null || _memoryConfig == null)
        {
            IsMemoryEnabled = false;
            return;
        }

        if (enabled && !IsMemoryEnabled)
        {
            // Enabling memory - store base prompt and load memory
            _baseSystemPrompt = GetCurrentSystemPrompt();
            IsMemoryEnabled = true;

            if (_memoryConfig.AutoLoadOnStart)
            {
                await LoadMemoryIntoConversationAsync();
            }
        }
        else if (!enabled && IsMemoryEnabled)
        {
            // Disabling memory - restore original prompt
            IsMemoryEnabled = false;
            if (_baseSystemPrompt != null)
            {
                _conversationManager.UpdateSystemPrompt(_baseSystemPrompt);
            }
        }
    }

    public async Task EndConversationAsync()
    {
        if (!IsMemoryEnabled || _memoryConfig == null || !_memoryConfig.PromptUpdateOnEnd)
        {
            return;
        }

        var prompt =
            "CONVERSATION ENDING: Please review our conversation. " +
            "If you learned anything important about the user (preferences, background, context), " +
            "update long-term memory using the long_term_memory_update tool. " +
            "If nothing significant changed, no action needed.";

        var message = new ScenarioUserMessage(prompt);

        using var cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(_memoryConfig.UpdatePromptTimeoutSeconds));

        try
        {
            // Consume the streaming response to completion
            await foreach (var _ in _orchestrator.ProcessApplicationMessageAsync(message, cts.Token))
            {
                // We don't need to process the chunks, just consume them
            }

            _logger?.LogInformation("Memory update prompt completed");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Memory update prompt timed out after {Timeout}s",
                _memoryConfig.UpdatePromptTimeoutSeconds);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during memory update prompt");
        }
    }

    private async void OnModeChanged(object? sender, AppMode newMode)
    {
        if (IsMemoryEnabled)
        {
            await LoadMemoryIntoConversationAsync();
        }
    }

    private async Task LoadMemoryIntoConversationAsync()
    {
        if (_memoryService == null || _appModeService == null)
        {
            return;
        }

        var memoryContent = await _memoryService.ReadMemoryAsync(_appModeService.CurrentMode);

        if (string.IsNullOrWhiteSpace(memoryContent))
        {
            _logger?.LogDebug("Memory is empty, skipping injection");
            return;
        }

        // Get base prompt if not already stored
        if (_baseSystemPrompt == null)
        {
            _baseSystemPrompt = GetCurrentSystemPrompt();
        }

        // Combine base prompt with memory
        var combinedPrompt = $"{_baseSystemPrompt}\n\n# Long-Term Memory\n{memoryContent}";
        _conversationManager.UpdateSystemPrompt(combinedPrompt);

        _logger?.LogInformation("Loaded {CharCount} chars of memory into conversation",
            memoryContent.Length);
    }

    private string GetCurrentSystemPrompt()
    {
        var messages = _conversationManager.GetInContextMessages();
        var systemMessage = messages.FirstOrDefault(m => m.Role == TransparentAiAgentCore.Domain.Enums.MessageRole.System);
        return systemMessage?.Content ?? "You are a helpful assistant.";
    }

    /// <summary>
    /// Refreshes memory state by reloading memory into system prompt if enabled.
    /// Should be called after LLM responses that may have updated memory.
    /// </summary>
    private async Task RefreshMemoryStateAsync()
    {
        if (!IsMemoryEnabled || _memoryService == null || _appModeService == null)
        {
            return;
        }

        // Reload memory into system prompt
        await LoadMemoryIntoConversationAsync();

        // Notify UI that memory state may have changed
        MemoryStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
