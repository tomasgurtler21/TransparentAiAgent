using Microsoft.Extensions.Logging;
using System.Text.Json;
using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Infrastructure.Serialization;

namespace TransparentAiAgentCore.Infrastructure.ConversationHistory;

/// <summary>
/// JSON file-based implementation of IConversationRepository.
/// Stores conversations as JSON files in a specified directory.
/// </summary>
public class JsonConversationRepository : IConversationRepository
{
    private readonly MessageSerializer _messageSerializer;
    private readonly ILogger<JsonConversationRepository> _logger;
    private readonly string _storageDirectory;

    public JsonConversationRepository(
        MessageSerializer messageSerializer,
        ILogger<JsonConversationRepository> logger,
        string storageDirectory)
    {
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
    }

    public async Task SaveAsync(Conversation conversation)
    {
        if (conversation == null)
            throw new ArgumentNullException(nameof(conversation));

        // Update LastModifiedAt timestamp
        conversation.LastModifiedAt = DateTime.UtcNow;

        // Ensure directory exists
        Directory.CreateDirectory(_storageDirectory);

        // Generate file path
        var filePath = GetFilePathForConversation(conversation.ConversationId, conversation.Name);

        // Serialize conversation to DTO
        var dto = new ConversationDto
        {
            ConversationId = conversation.ConversationId,
            Name = conversation.Name,
            CreatedAt = conversation.CreatedAt,
            LastModifiedAt = conversation.LastModifiedAt,
            Messages = conversation.Messages.Select(m => _messageSerializer.Serialize(m)).ToList()
        };

        // Write to file
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        _logger.LogInformation("Saved conversation {ConversationId} to {FilePath}", conversation.ConversationId, filePath);
    }

    private string GetFilePathForConversation(Guid conversationId, string name)
    {
        // Sanitize name for filename
        var sanitizedName = SanitizeFileName(name);
        var fileName = $"{conversationId}_{sanitizedName}.json";
        return Path.Combine(_storageDirectory, fileName);
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "unnamed";
        }

        // Invalid filename characters: < > : " / \ | ? *
        var invalidChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        var result = input;
        foreach (var invalidChar in invalidChars)
        {
            result = result.Replace(invalidChar, '_');
        }

        // Limit length to 50 characters
        if (result.Length > 50)
        {
            result = result.Substring(0, 50);
        }

        return result;
    }

    public async Task<Conversation> LoadAsync(Guid conversationId)
    {
        // Find file by conversationId pattern
        var filePath = FindConversationFile(conversationId);

        if (filePath == null)
        {
            throw new FileNotFoundException($"Conversation {conversationId} not found in {_storageDirectory}");
        }

        try
        {
            // Read JSON file
            var json = await File.ReadAllTextAsync(filePath);

            // Deserialize DTO
            var dto = JsonSerializer.Deserialize<ConversationDto>(json);

            if (dto == null)
            {
                throw new InvalidOperationException($"Failed to deserialize conversation from {filePath}");
            }

            // Convert DTO to domain model
            var conversation = new Conversation
            {
                ConversationId = dto.ConversationId,
                Name = dto.Name,
                CreatedAt = dto.CreatedAt,
                LastModifiedAt = dto.LastModifiedAt,
                Messages = dto.Messages.Select(m => _messageSerializer.Deserialize(m)).ToList()
            };

            _logger.LogInformation("Loaded conversation {ConversationId} from {FilePath}", conversationId, filePath);

            return conversation;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Corrupted JSON in file {filePath}", ex);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is not JsonException)
        {
            // Re-throw message deserialization errors
            throw new InvalidOperationException($"Failed to deserialize messages in conversation {conversationId}", ex);
        }
    }

    private string? FindConversationFile(Guid conversationId)
    {
        // Ensure directory exists
        if (!Directory.Exists(_storageDirectory))
        {
            return null;
        }

        // Find file starting with conversationId
        var files = Directory.GetFiles(_storageDirectory, $"{conversationId}_*.json");

        return files.FirstOrDefault();
    }

    public async Task<List<ConversationMetadata>> ListAllAsync()
    {
        // Check if directory exists
        if (!Directory.Exists(_storageDirectory))
        {
            return new List<ConversationMetadata>();
        }

        var metadataList = new List<ConversationMetadata>();

        // Get all JSON files
        var files = Directory.GetFiles(_storageDirectory, "*.json");

        foreach (var filePath in files)
        {
            try
            {
                // Read JSON file
                var json = await File.ReadAllTextAsync(filePath);

                // Deserialize DTO (but not messages for performance)
                var dto = JsonSerializer.Deserialize<ConversationDto>(json);

                if (dto != null)
                {
                    metadataList.Add(new ConversationMetadata
                    {
                        ConversationId = dto.ConversationId,
                        Name = dto.Name,
                        CreatedAt = dto.CreatedAt,
                        LastModifiedAt = dto.LastModifiedAt,
                        MessageCount = dto.Messages.Count
                    });
                }
            }
            catch (Exception ex)
            {
                // Skip corrupted files with warning
                _logger.LogWarning(ex, "Skipping corrupted conversation file: {FilePath}", filePath);
            }
        }

        // Sort by most recent first
        return metadataList.OrderByDescending(m => m.LastModifiedAt).ToList();
    }

    public async Task DeleteAsync(Guid conversationId)
    {
        var filePath = FindConversationFile(conversationId);

        if (filePath == null)
        {
            throw new FileNotFoundException($"Conversation {conversationId} not found in {_storageDirectory}");
        }

        File.Delete(filePath);

        _logger.LogInformation("Deleted conversation {ConversationId} from {FilePath}", conversationId, filePath);

        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid conversationId)
    {
        var filePath = FindConversationFile(conversationId);
        await Task.CompletedTask;
        return filePath != null;
    }
}

/// <summary>
/// DTO for JSON serialization of Conversation.
/// Messages are stored as JSON strings for proper serialization.
/// </summary>
internal class ConversationDto
{
    public Guid ConversationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public List<string> Messages { get; set; } = new();
}
