using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Scenarios;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentCore.Infrastructure.Tools.ScenarioMock;

/// <summary>
/// Registry for scenario-specific mock tools.
/// Implements both IScenarioToolRegistry (for scenario management) and IToolRegistry (for tool system integration).
/// Thread-safe for concurrent scenario execution.
/// </summary>
public class ScenarioToolRegistry : IScenarioToolRegistry, IToolRegistry
{
    private readonly Dictionary<string, ScenarioMockTool> _mockTools = new();
    private readonly object _lock = new();
    private readonly ILogger<ScenarioToolRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioToolRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ScenarioToolRegistry(ILogger<ScenarioToolRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IScenarioToolRegistry Implementation

    /// <inheritdoc/>
    public void RegisterMockTool(
        string name,
        string description,
        string parametersSchema,
        Dictionary<string, MockToolResponse> responseMap)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(name));

        var mockTool = new ScenarioMockTool(name, description, parametersSchema, responseMap);

        lock (_lock)
        {
            if (_mockTools.ContainsKey(name))
            {
                _logger.LogWarning(
                    "Mock tool '{ToolName}' is already registered. Replacing with new registration.",
                    name);
            }

            _mockTools[name] = mockTool;
            _logger.LogInformation(
                "Registered mock tool '{ToolName}' with {ResponseCount} response(s)",
                name,
                responseMap.Count);
        }
    }

    /// <inheritdoc/>
    public void UnregisterMockTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return;

        lock (_lock)
        {
            if (_mockTools.Remove(toolName))
            {
                _logger.LogInformation("Unregistered mock tool '{ToolName}'", toolName);
            }
        }
    }

    /// <inheritdoc/>
    public void UnregisterAllMockTools()
    {
        lock (_lock)
        {
            var count = _mockTools.Count;
            if (count > 0)
            {
                _mockTools.Clear();
                _logger.LogInformation("Unregistered all {Count} mock tool(s)", count);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITool> GetAllMockTools()
    {
        lock (_lock)
        {
            return _mockTools.Values.Cast<ITool>().ToList().AsReadOnly();
        }
    }

    /// <inheritdoc/>
    public ITool? GetMockTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        lock (_lock)
        {
            return _mockTools.TryGetValue(toolName, out var tool) ? tool : null;
        }
    }

    /// <inheritdoc/>
    public MockToolResponse? GetMockResponse(string toolName, string arguments)
    {
        if (string.IsNullOrWhiteSpace(toolName) || string.IsNullOrWhiteSpace(arguments))
            return null;

        lock (_lock)
        {
            if (!_mockTools.TryGetValue(toolName, out var mockTool))
                return null;

            // Try exact match first
            if (mockTool.ResponseMap.TryGetValue(arguments, out var response))
                return response;

            // No match found
            return null;
        }
    }

    #endregion

    #region IToolRegistry Implementation

    /// <inheritdoc/>
    public IReadOnlyList<ITool> GetAllTools()
    {
        return GetAllMockTools();
    }

    /// <inheritdoc/>
    public ITool? GetTool(string toolName)
    {
        return GetMockTool(toolName);
    }

    /// <inheritdoc/>
    public bool HasTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return false;

        lock (_lock)
        {
            return _mockTools.ContainsKey(toolName);
        }
    }

    /// <inheritdoc/>
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Mock tools are managed explicitly via Register/Unregister methods
        // No refresh needed
        return Task.CompletedTask;
    }

    #endregion
}
