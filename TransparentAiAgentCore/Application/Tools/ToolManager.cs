using System.Diagnostics;
using System.Text.Json;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Infrastructure.Transparency;

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

    public ToolManager(
        IToolRegistry registry,
        IEnumerable<IToolExecutor> executors,
        ITransparencyService transparencyService)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _executors = executors ?? throw new ArgumentNullException(nameof(executors));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
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

            // 4. Execute tool
            var result = await executor.ExecuteAsync(tool, toolCall.Arguments, cancellationToken);

            stopwatch.Stop();

            // 5. Log result to Transparency System
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
    /// </summary>
    public List<LLMTool> GetLLMToolDefinitions()
    {
        var tools = _registry.GetAllTools();

        return tools
            .Select(t => new LLMTool(t.Name, t.Description, t.ParametersSchema))
            .ToList();
    }
}
