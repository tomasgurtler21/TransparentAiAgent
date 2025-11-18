using Microsoft.Extensions.Logging;
using System.Text;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.DataPath;

namespace TransparentAiAgentCore.Infrastructure.Memory;

/// <summary>
/// Implementation of long-term memory service using file-based storage.
/// </summary>
public class LongTermMemoryService : ILongTermMemoryService
{
    private readonly LongTermMemoryConfiguration _config;
    private readonly IDataPathService _dataPathService;
    private readonly ILogger<LongTermMemoryService> _logger;

    public LongTermMemoryService(
        LongTermMemoryConfiguration config,
        IDataPathService dataPathService,
        ILogger<LongTermMemoryService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _dataPathService = dataPathService ?? throw new ArgumentNullException(nameof(dataPathService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("Memory storage directory: {Directory}", _dataPathService.GetMemoryDirectory());
    }

    private string GetMemoryFilePath(AppMode mode)
    {
        var fileName = mode switch
        {
            AppMode.Normal => "memory-normal.md",
            AppMode.Teaching => "memory-teaching.md",
            _ => throw new ArgumentException($"Unknown mode: {mode}")
        };
        return Path.Combine(_dataPathService.GetMemoryDirectory(), fileName);
    }

    public async Task<string> ReadMemoryAsync(AppMode mode, CancellationToken cancellationToken = default)
    {
        var filePath = GetMemoryFilePath(mode);

        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
    }

    public async Task<MemoryUpdateResult> UpdateMemoryAsync(AppMode mode, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate size
            if (content.Length > _config.MaxCharacters)
            {
                return new MemoryUpdateResult(
                    Success: false,
                    Error: $"Memory content exceeds maximum size of {_config.MaxCharacters} characters");
            }

            var filePath = GetMemoryFilePath(mode);

            // Ensure directory exists
            Directory.CreateDirectory(_dataPathService.GetMemoryDirectory());

            // Write with explicit UTF-8 encoding
            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);

            _logger.LogInformation("Updated {Mode} mode memory ({CharCount} chars)", mode, content.Length);

            return new MemoryUpdateResult(
                Success: true,
                CharacterCount: content.Length,
                UpdatedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update memory for {Mode} mode", mode);
            return new MemoryUpdateResult(
                Success: false,
                Error: $"Failed to write memory: {ex.Message}");
        }
    }

    public Task<bool> HasMemoryAsync(AppMode mode, CancellationToken cancellationToken = default)
    {
        var filePath = GetMemoryFilePath(mode);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<DateTime?> GetLastUpdateTimeAsync(AppMode mode, CancellationToken cancellationToken = default)
    {
        var filePath = GetMemoryFilePath(mode);

        if (!File.Exists(filePath))
        {
            return Task.FromResult<DateTime?>(null);
        }

        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
        return Task.FromResult<DateTime?>(lastWriteTime);
    }
}
