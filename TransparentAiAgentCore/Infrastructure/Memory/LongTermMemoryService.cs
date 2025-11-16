using Microsoft.Extensions.Logging;
using System.Text;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Infrastructure.Memory;

/// <summary>
/// Implementation of long-term memory service using file-based storage.
/// </summary>
public class LongTermMemoryService : ILongTermMemoryService
{
    private readonly LongTermMemoryConfiguration _config;
    private readonly ILogger<LongTermMemoryService> _logger;
    private readonly string _storageDirectory;

    public LongTermMemoryService(
        LongTermMemoryConfiguration config,
        ILogger<LongTermMemoryService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Resolve storage directory to absolute path
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Check if configured path is relative
        var isRelativePath = !Path.IsPathRooted(_config.StorageDirectory);

        _storageDirectory = isRelativePath
            ? Path.GetFullPath(Path.Combine(baseDir, _config.StorageDirectory))
            : Path.GetFullPath(_config.StorageDirectory);

        // Security: For relative paths, ensure no directory traversal outside app directory
        if (isRelativePath && !_storageDirectory.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Storage directory must be within application directory. " +
                $"Configured: {_config.StorageDirectory}, Resolved: {_storageDirectory}");
        }

        _logger.LogInformation("Memory storage directory: {Directory}", _storageDirectory);
    }

    private string GetMemoryFilePath(AppMode mode)
    {
        var fileName = mode switch
        {
            AppMode.Normal => "memory-normal.md",
            AppMode.Teaching => "memory-teaching.md",
            _ => throw new ArgumentException($"Unknown mode: {mode}")
        };
        return Path.Combine(_storageDirectory, fileName);
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
            Directory.CreateDirectory(_storageDirectory);

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
