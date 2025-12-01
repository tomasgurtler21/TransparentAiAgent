using System.Diagnostics;
using System.Text.Json;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Configuration;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Infrastructure.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.Validation;

namespace TransparentAiAgentCore.Application.Tools;

/// <summary>
/// High-level tool management and orchestration.
/// Routes tool calls to appropriate executors and logs to transparency system.
/// </summary>
public class ToolManager : IToolManager
{
    private readonly IToolRegistry _registry;
    private readonly IEnumerable<IToolExecutor> _executors;
    private readonly ITransparencyService _transparencyService;
    private readonly IToolUsageStatistics _statistics;
    private readonly ToolSchemaValidator _validator;
    private readonly IAppModeService? _appModeService;
    private readonly IUserSettingsService? _userSettingsService;

    public ToolManager(
        IToolRegistry registry,
        IEnumerable<IToolExecutor> executors,
        ITransparencyService transparencyService,
        IToolUsageStatistics statistics,
        ToolSchemaValidator validator,
        IAppModeService? appModeService = null,
        IUserSettingsService? userSettingsService = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _executors = executors ?? throw new ArgumentNullException(nameof(executors));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _appModeService = appModeService; // Optional - may be null if mode service not available
        _userSettingsService = userSettingsService; // Optional - may be null if settings service not available
    }

    /// <summary>
    /// Gets the tool registry.
    /// </summary>
    public IToolRegistry Registry => _registry;

    /// <summary>
    /// Executes a tool call from the LLM.
    /// </summary>
    public async Task<ToolExecutionResult> ExecuteToolCallAsync(
        LLMToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        if (toolCall == null)
            throw new ArgumentNullException(nameof(toolCall));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Lookup tool in registry
            var tool = _registry.GetTool(toolCall.Name);
            if (tool == null)
            {
                stopwatch.Stop();
                return ToolExecutionResult.Failure(
                    $"Tool '{toolCall.Name}' not found in registry",
                    stopwatch.Elapsed);
            }

            // 2. Find executor for this tool's source type
            var executor = _executors.FirstOrDefault(e => e.SourceType == tool.SourceType);
            if (executor == null)
            {
                stopwatch.Stop();
                return ToolExecutionResult.Failure(
                    $"No executor found for source type {tool.SourceType}",
                    stopwatch.Elapsed);
            }

            // 3. Log to Transparency System (tool call started)
            _transparencyService.LogEvent(new TransparencyEvent(
                TransparencyEventType.ToolCall,
                JsonSerializer.Serialize(new
                {
                    ToolName = tool.Name,
                    SourceType = tool.SourceType.ToString(),
                    Arguments = toolCall.Arguments,
                    CallId = toolCall.Id
                })
            ));

            // 3a. Validate arguments against schema BEFORE execution
            var validationResult = _validator.ValidateArguments(tool.ParametersSchema, toolCall.Arguments);
            if (!validationResult.IsValid)
            {
                stopwatch.Stop();

                // Log validation failure to Transparency System
                _transparencyService.LogEvent(new TransparencyEvent(
                    TransparencyEventType.ToolArgumentValidationFailed,
                    JsonSerializer.Serialize(new
                    {
                        ToolName = tool.Name,
                        Arguments = toolCall.Arguments,
                        Schema = tool.ParametersSchema,
                        ValidationError = validationResult.ErrorMessage,
                        CallId = toolCall.Id
                    }),
                    $"Schema validation failed for tool '{tool.Name}'"));

                // Record statistics for failed validation
                _statistics.RecordToolCall(tool.Name, false, stopwatch.Elapsed);

                return ToolExecutionResult.Failure(
                    $"Schema validation failed: {validationResult.ErrorMessage}",
                    stopwatch.Elapsed);
            }

            // 4. Execute tool
            var result = await executor.ExecuteAsync(tool, toolCall.Arguments, cancellationToken);

            stopwatch.Stop();

            // 5. Record statistics
            _statistics.RecordToolCall(tool.Name, result.IsSuccess, stopwatch.Elapsed);

            // 6. Log result to Transparency System
            var eventType = result.IsSuccess
                ? TransparencyEventType.ToolResult
                : TransparencyEventType.Error;

            _transparencyService.LogEvent(new TransparencyEvent(
                eventType,
                JsonSerializer.Serialize(new
                {
                    ToolName = tool.Name,
                    Success = result.IsSuccess,
                    ExecutionTimeMs = result.ExecutionTime.TotalMilliseconds,
                    Result = result.IsSuccess ? result.Content : result.ErrorMessage,
                    CallId = toolCall.Id
                })
            ));

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record statistics for failed call
            _statistics.RecordToolCall(toolCall.Name, false, stopwatch.Elapsed);

            // Log error to Transparency System
            _transparencyService.LogEvent(new TransparencyEvent(
                TransparencyEventType.Error,
                JsonSerializer.Serialize(new
                {
                    ToolName = toolCall.Name,
                    Error = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    CallId = toolCall.Id
                })
            ));

            return ToolExecutionResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Converts registered tools to LLM tool format.
    /// Filters tools based on current mode: built-in UI control and knowledge library tools
    /// are only available in Teaching mode.
    /// </summary>
    public List<LLMTool> GetLLMToolDefinitions()
    {
        try
        {
            // Get current mode (default to Normal if mode service not available)
            var currentMode = _appModeService?.CurrentMode ?? AppMode.Normal;

            // Log registry type and tool count for transparency
            var registryType = _registry.GetType().Name;
            var tools = _registry.GetAllTools();
            var toolCount = tools?.Count ?? 0;

            _transparencyService.LogEvent(new TransparencyEvent(
                TransparencyEventType.SystemState,
                JsonSerializer.Serialize(new
                {
                    RegistryType = registryType,
                    ToolCount = toolCount,
                    CurrentMode = currentMode.ToString(),
                    Tools = tools?.Select(t => new { t.Name, t.SourceType }).ToList()
                }),
                $"[ToolManager] GetLLMToolDefinitions - Registry: {registryType}, Count: {toolCount}, Mode: {currentMode}"));

            if (tools == null || tools.Count == 0)
            {
                _transparencyService.LogEvent(new TransparencyEvent(
                    TransparencyEventType.SystemState,
                    "Registry returned null or empty tools list",
                    "[ToolManager] WARNING: No tools available from registry"));
                return new List<LLMTool>();
            }

            // Get user settings for memory preference
            var enableMemory = _userSettingsService?.GetCurrentSettings()?.EnableMemory ?? false;

            // Filter tools based on mode and user preferences
            // In Normal mode: exclude UI control and knowledge library tools (teaching-specific)
            // In Teaching mode: include all tools
            // Long-term memory tools: only if EnableMemory setting is true
            var filteredTools = tools.Where(t =>
            {
                // Filter by mode (existing logic)
                if (currentMode == AppMode.Normal &&
                    (t.SourceType == ToolSourceType.BuiltInUIControl ||
                     t.SourceType == ToolSourceType.BuiltInKnowledge))
                    return false;

                // Filter by user preference: memory tools
                if (t.SourceType == ToolSourceType.BuiltInLongTermMemory && !enableMemory)
                    return false;

                return true;
            }).ToList();

            var filteredCount = filteredTools.Count();
            if (filteredCount < toolCount)
            {
                var excludedCount = toolCount - filteredCount;
                var excludedTypes = new List<string>();

                if (currentMode == AppMode.Normal)
                {
                    excludedTypes.Add("BuiltInUIControl");
                    excludedTypes.Add("BuiltInKnowledge");
                }

                if (!enableMemory)
                {
                    excludedTypes.Add("BuiltInLongTermMemory");
                }

                _transparencyService.LogEvent(new TransparencyEvent(
                    TransparencyEventType.SystemState,
                    JsonSerializer.Serialize(new
                    {
                        CurrentMode = currentMode.ToString(),
                        EnableMemory = enableMemory,
                        TotalTools = toolCount,
                        FilteredTools = filteredCount,
                        ExcludedTools = excludedCount,
                        ExcludedTypes = excludedTypes
                    }),
                    $"[ToolManager] Filtered {excludedCount} tools (mode: {currentMode}, memory: {enableMemory})"));
            }

            var llmTools = filteredTools
                .Select(t => new LLMTool(t.Name, t.Description, t.ParametersSchema))
                .ToList();

            _transparencyService.LogEvent(new TransparencyEvent(
                TransparencyEventType.SystemState,
                JsonSerializer.Serialize(new
                {
                    ConvertedCount = llmTools.Count,
                    ToolNames = llmTools.Select(t => t.Name).ToList(),
                    Mode = currentMode.ToString(),
                    EnableMemory = enableMemory
                }),
                $"[ToolManager] Converted {llmTools.Count} tools to LLM format (mode: {currentMode}, memory: {enableMemory})"));

            return llmTools;
        }
        catch (Exception ex)
        {
            _transparencyService.LogEvent(new TransparencyEvent(
                TransparencyEventType.Error,
                JsonSerializer.Serialize(new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                }),
                $"[ToolManager] Error in GetLLMToolDefinitions: {ex.Message}"));
            throw;
        }
    }
}
