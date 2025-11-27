using System.Diagnostics;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Scenarios;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentCore.Infrastructure.Tools.ScenarioMock;

/// <summary>
/// Executes scenario mock tools by looking up predefined responses.
/// </summary>
public class ScenarioMockToolExecutor : IToolExecutor
{
    private readonly IScenarioToolRegistry _scenarioToolRegistry;
    private readonly ILogger<ScenarioMockToolExecutor> _logger;

    /// <summary>
    /// Gets the tool source type this executor handles.
    /// </summary>
    public ToolSourceType SourceType => ToolSourceType.ScenarioMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioMockToolExecutor"/> class.
    /// </summary>
    /// <param name="scenarioToolRegistry">The scenario tool registry.</param>
    /// <param name="logger">The logger instance.</param>
    public ScenarioMockToolExecutor(
        IScenarioToolRegistry scenarioToolRegistry,
        ILogger<ScenarioMockToolExecutor> logger)
    {
        _scenarioToolRegistry = scenarioToolRegistry ?? throw new ArgumentNullException(nameof(scenarioToolRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (tool.SourceType != ToolSourceType.ScenarioMock)
        {
            throw new ArgumentException(
                $"Tool '{tool.Name}' has source type '{tool.SourceType}' but executor handles '{ToolSourceType.ScenarioMock}'",
                nameof(tool));
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "Executing mock tool '{ToolName}' with arguments: {Arguments}",
            tool.Name,
            arguments);

        // Get predefined response
        var mockResponse = _scenarioToolRegistry.GetMockResponse(tool.Name, arguments);

        if (mockResponse == null)
        {
            stopwatch.Stop();
            var errorMessage = $"No mock response defined for tool '{tool.Name}' with arguments: {arguments}";
            _logger.LogWarning(errorMessage);
            return ToolExecutionResult.Failure(errorMessage, stopwatch.Elapsed);
        }

        // Simulate execution time if specified
        if (mockResponse.SimulatedExecutionTime.HasValue)
        {
            _logger.LogDebug(
                "Simulating execution time of {Duration}ms for tool '{ToolName}'",
                mockResponse.SimulatedExecutionTime.Value.TotalMilliseconds,
                tool.Name);

            await Task.Delay(mockResponse.SimulatedExecutionTime.Value, cancellationToken);
        }

        stopwatch.Stop();

        // Return predefined response
        if (mockResponse.IsSuccess)
        {
            _logger.LogDebug(
                "Mock tool '{ToolName}' succeeded with content length {ContentLength}",
                tool.Name,
                mockResponse.Content?.Length ?? 0);

            return ToolExecutionResult.Success(mockResponse.Content!, stopwatch.Elapsed);
        }
        else
        {
            _logger.LogDebug(
                "Mock tool '{ToolName}' failed with error: {ErrorMessage}",
                tool.Name,
                mockResponse.ErrorMessage);

            return ToolExecutionResult.Failure(
                mockResponse.ErrorMessage ?? "Unknown error",
                stopwatch.Elapsed);
        }
    }
}
