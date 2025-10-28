using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentGui.Models;

namespace TransparentAiAgentGui.Services;

public class ConversationUIService : IConversationUIService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IConversationManager _conversationManager;
    private readonly List<UIMessage> _messages = new();
    private bool _isProcessing;

    public IReadOnlyList<UIMessage> Messages => _messages.AsReadOnly();
    public bool IsProcessing => _isProcessing;

    public event EventHandler? MessagesChanged;
    public event EventHandler<bool>? ProcessingStateChanged;

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
