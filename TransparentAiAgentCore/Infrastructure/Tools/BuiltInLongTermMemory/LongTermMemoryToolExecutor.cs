using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

/// <summary>
/// Executes long-term memory tools by routing to ILongTermMemoryService.
/// Handles read and update operations for persistent user memory.
/// </summary>
public class LongTermMemoryToolExecutor : IToolExecutor
{
    private readonly ILongTermMemoryService _memoryService;
    private readonly IAppModeService _appModeService;
    private readonly ILogger<LongTermMemoryToolExecutor> _logger;

    public LongTermMemoryToolExecutor(
        ILongTermMemoryService memoryService,
        IAppModeService appModeService,
        ILogger<LongTermMemoryToolExecutor> logger)
    {
        _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        _appModeService = appModeService ?? throw new ArgumentNullException(nameof(appModeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ToolSourceType SourceType => ToolSourceType.BuiltInLongTermMemory;

    public async Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing long-term memory tool: {ToolName}", tool.Name);

            // Parse arguments
            JsonDocument argsDoc;
            try
            {
                if (string.IsNullOrWhiteSpace(arguments))
                {
                    arguments = "{}";
                }
                argsDoc = JsonDocument.Parse(arguments);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON arguments for {ToolName}", tool.Name);
                return ToolExecutionResult.Failure(
                    $"Invalid JSON arguments: {ex.Message}",
                    stopwatch.Elapsed);
            }

            // Route to appropriate handler
            var result = tool.Name.ToLowerInvariant() switch
            {
                "long_term_memory_read" => await ExecuteReadToolAsync(cancellationToken),
                "long_term_memory_update" => await ExecuteUpdateToolAsync(argsDoc, cancellationToken),
                _ => ToolExecutionResult.Failure($"Unknown tool: {tool.Name}", stopwatch.Elapsed)
            };

            stopwatch.Stop();
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error executing memory tool {ToolName}", tool.Name);
            return ToolExecutionResult.Failure(
                $"Unexpected error: {ex.Message}",
                stopwatch.Elapsed);
        }
    }

    private async Task<ToolExecutionResult> ExecuteReadToolAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var currentMode = _appModeService.CurrentMode;
        var memoryContent = await _memoryService.ReadMemoryAsync(currentMode, cancellationToken);

        // If memory doesn't exist yet, return a helpful message instead of empty string
        if (string.IsNullOrWhiteSpace(memoryContent))
        {
            _logger.LogInformation("No memory found for {Mode} mode", currentMode);
            return ToolExecutionResult.Success(
                "Memory does not exist yet. It will be created when you write to it for the first time using the long_term_memory_update tool.",
                stopwatch.Elapsed);
        }

        _logger.LogInformation("Read {CharCount} characters from {Mode} mode memory",
            memoryContent.Length, currentMode);

        return ToolExecutionResult.Success(memoryContent, stopwatch.Elapsed);
    }

    private async Task<ToolExecutionResult> ExecuteUpdateToolAsync(
        JsonDocument args,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var root = args.RootElement;

        // Extract required parameters
        var content = GetStringProperty(root, "content");
        var reason = GetStringProperty(root, "reason");

        // Validate required parameters
        if (content == null)
        {
            return ToolExecutionResult.Failure(
                "Missing required parameter: 'content'",
                stopwatch.Elapsed);
        }

        if (reason == null)
        {
            return ToolExecutionResult.Failure(
                "Missing required parameter: 'reason'",
                stopwatch.Elapsed);
        }

        var currentMode = _appModeService.CurrentMode;
        var updateResult = await _memoryService.UpdateMemoryAsync(
            currentMode,
            content,
            cancellationToken);

        if (updateResult.Success)
        {
            _logger.LogInformation("Updated {Mode} mode memory: {Reason} ({CharCount} chars)",
                currentMode, reason, updateResult.CharacterCount);

            var successMessage = $"Memory successfully updated for {currentMode} mode. " +
                                $"Reason: {reason}. " +
                                $"Size: {updateResult.CharacterCount} characters.";

            return ToolExecutionResult.Success(successMessage, stopwatch.Elapsed);
        }
        else
        {
            _logger.LogWarning("Failed to update {Mode} mode memory: {Error}",
                currentMode, updateResult.Error);

            return ToolExecutionResult.Failure(
                updateResult.Error ?? "Unknown error",
                stopwatch.Elapsed);
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }
        return null;
    }
}
