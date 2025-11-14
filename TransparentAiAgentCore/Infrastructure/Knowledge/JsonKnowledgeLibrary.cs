namespace TransparentAiAgentCore.Infrastructure.Knowledge;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Knowledge;

public class JsonKnowledgeLibrary : IKnowledgeLibrary
{
    private readonly string _entriesPath;
    private readonly ILogger<JsonKnowledgeLibrary> _logger;
    private readonly Dictionary<string, KnowledgeEntry> _entriesCache = new();
    private readonly List<KnowledgeEntrySummary> _index = new();
    private readonly object _cacheLock = new object();

    public JsonKnowledgeLibrary(string knowledgeBasePath, ILogger<JsonKnowledgeLibrary> logger)
    {
        if (string.IsNullOrWhiteSpace(knowledgeBasePath))
            throw new ArgumentException("Knowledge base path cannot be null or whitespace", nameof(knowledgeBasePath));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _entriesPath = Path.Combine(knowledgeBasePath, "entries");

        LoadIndex();
    }

    private void LoadIndex()
    {
        try
        {
            if (!Directory.Exists(_entriesPath))
            {
                _logger.LogWarning("Knowledge library entries directory not found at {Path}", _entriesPath);
                return;
            }

            var entryFiles = Directory.GetFiles(_entriesPath, "*.json");
            var loadedCount = 0;
            var failedCount = 0;

            foreach (var filePath in entryFiles)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, options);

                    if (dto != null)
                    {
                        var entry = dto.ToDomain();
                        if (entry != null && !string.IsNullOrEmpty(entry.Id))
                        {
                            // Add to index
                            _index.Add(KnowledgeEntrySummary.FromEntry(entry));
                            loadedCount++;
                        }
                        else
                        {
                            _logger.LogWarning("Invalid entry in file {Path}: missing required fields", filePath);
                            failedCount++;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize entry in file {Path}", filePath);
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse knowledge entry file: {Path}", filePath);
                    failedCount++;
                }
            }

            // Sort index by topic name for consistent ordering
            _index.Sort((a, b) => string.Compare(a.Topic, b.Topic, StringComparison.OrdinalIgnoreCase));

            _logger.LogInformation("Knowledge library initialized: {Loaded} entries loaded, {Failed} failed",
                loadedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan knowledge library directory");
        }
    }

    public KnowledgeEntry? GetTopic(string topicId)
    {
        if (string.IsNullOrWhiteSpace(topicId))
            throw new ArgumentException("Topic ID cannot be null or whitespace", nameof(topicId));

        // Check cache first
        lock (_cacheLock)
        {
            if (_entriesCache.TryGetValue(topicId, out var cached))
            {
                return cached;
            }
        }

        // Load from file
        try
        {
            var entryPath = Path.Combine(_entriesPath, $"{topicId}.json");
            if (!File.Exists(entryPath))
            {
                _logger.LogWarning("Knowledge entry not found: {TopicId}", topicId);
                return null;
            }

            var json = File.ReadAllText(entryPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var dto = JsonSerializer.Deserialize<KnowledgeEntryDto>(json, options);

            // Validate
            if (dto == null)
            {
                _logger.LogError("Failed to deserialize knowledge entry: {TopicId}", topicId);
                return null;
            }

            var entry = dto.ToDomain();
            if (entry == null || entry.Id != topicId)
            {
                _logger.LogError("Invalid knowledge entry file: {TopicId}", topicId);
                return null;
            }

            // Cache and return
            lock (_cacheLock)
            {
                _entriesCache[topicId] = entry;
            }

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load knowledge entry: {TopicId}", topicId);
            return null;
        }
    }

    public IReadOnlyList<KnowledgeEntrySummary> GetAllTopics()
    {
        return _index.AsReadOnly();
    }

    public IReadOnlyList<KnowledgeEntrySummary> GetTopicsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or whitespace", nameof(category));

        return _index
            .Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<string> GetCategories()
    {
        return _index
            .Select(e => e.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}
