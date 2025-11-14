using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Application.ConversationHistory;

/// <summary>
/// Manages conversation history operations including saving, loading, and listing conversations.
/// </summary>
public class ConversationHistoryManager : IConversationHistoryManager
{
    private readonly IConversationRepository _repository;
    private readonly ILogger<ConversationHistoryManager> _logger;

    public ConversationHistoryManager(
        IConversationRepository repository,
        ILogger<ConversationHistoryManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SaveCurrentConversationAsync(Guid conversationId, IReadOnlyList<IMessage> messages)
    {
        // Skip save if no messages
        if (messages == null || messages.Count == 0)
        {
            _logger.LogDebug("Skipping save for conversation {ConversationId} - no messages", conversationId);
            return;
        }

        try
        {
            // Check if conversation already exists
            bool exists = await _repository.ExistsAsync(conversationId);

            Domain.ConversationHistory.Conversation conversation;

            if (exists)
            {
                // Load existing conversation to preserve metadata
                conversation = await _repository.LoadAsync(conversationId);
                conversation.Messages = messages.ToList();
            }
            else
            {
                // Create new conversation
                conversation = new Domain.ConversationHistory.Conversation
                {
                    ConversationId = conversationId,
                    Name = Domain.ConversationHistory.Conversation.GenerateName(messages),
                    CreatedAt = DateTime.UtcNow,
                    Messages = messages.ToList()
                };
            }

            // Update last modified time
            conversation.LastModifiedAt = DateTime.UtcNow;

            // Save to repository
            await _repository.SaveAsync(conversation);

            _logger.LogInformation("Saved conversation {ConversationId} with {MessageCount} messages",
                conversationId, messages.Count);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - save failures should not crash the app
            _logger.LogError(ex, "Failed to save conversation {ConversationId}", conversationId);
        }
    }

    public async Task<Domain.ConversationHistory.Conversation> LoadConversationAsync(Guid conversationId)
    {
        _logger.LogInformation("Loading conversation {ConversationId}", conversationId);
        return await _repository.LoadAsync(conversationId);
    }

    public async Task<IReadOnlyList<ConversationMetadata>> GetConversationListAsync()
    {
        _logger.LogDebug("Getting conversation list");
        return await _repository.ListAllAsync();
    }

    public Task<Guid> CreateNewConversationAsync()
    {
        var newId = Guid.NewGuid();
        _logger.LogInformation("Created new conversation ID {ConversationId}", newId);
        return Task.FromResult(newId);
    }

    public async Task DeleteConversationAsync(Guid conversationId)
    {
        _logger.LogInformation("Deleting conversation {ConversationId}", conversationId);
        await _repository.DeleteAsync(conversationId);
    }
}
