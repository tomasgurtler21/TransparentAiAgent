using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Scenarios;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

            // Try exact string match first (fast path for exact matches)
            if (mockTool.ResponseMap.TryGetValue(arguments, out var response))
                return response;

            // Try semantic JSON matching
            try
            {
                using var argDoc = JsonDocument.Parse(arguments);

                foreach (var kvp in mockTool.ResponseMap)
                {
                    // Skip wildcard key during semantic matching
                    if (kvp.Key == "*")
                        continue;

                    try
                    {
                        using var expectedDoc = JsonDocument.Parse(kvp.Key);

                        if (JsonEquals(argDoc.RootElement, expectedDoc.RootElement))
                        {
                            _logger.LogDebug(
                                "Found mock response for tool '{ToolName}' via semantic JSON match",
                                toolName);
                            return kvp.Value;
                        }
                    }
                    catch (JsonException ex)
                    {
                        // If response map key is malformed, log and skip
                        _logger.LogWarning(ex,
                            "Invalid JSON in response map key for tool '{ToolName}': {Key}",
                            toolName, kvp.Key);
                    }
                }
            }
            catch (JsonException ex)
            {
                // Arguments are not valid JSON
                _logger.LogWarning(ex,
                    "Invalid JSON arguments for tool '{ToolName}': {Arguments}",
                    toolName, arguments);
            }

            // Check for wildcard/default response
            if (mockTool.ResponseMap.TryGetValue("*", out var defaultResponse))
            {
                _logger.LogDebug(
                    "Using wildcard default response for tool '{ToolName}' with arguments: {Arguments}",
                    toolName, arguments);
                return defaultResponse;
            }

            // No match found
            return null;
        }
    }

    /// <summary>
    /// Compares two JsonElement objects for semantic equality.
    /// Handles objects, arrays, primitives, null.
    /// </summary>
    private static bool JsonEquals(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
            return false;

        switch (a.ValueKind)
        {
            case JsonValueKind.Object:
                var aProps = a.EnumerateObject().OrderBy(p => p.Name).ToList();
                var bProps = b.EnumerateObject().OrderBy(p => p.Name).ToList();

                if (aProps.Count != bProps.Count)
                    return false;

                for (int i = 0; i < aProps.Count; i++)
                {
                    if (aProps[i].Name != bProps[i].Name)
                        return false;
                    if (!JsonEquals(aProps[i].Value, bProps[i].Value))
                        return false;
                }
                return true;

            case JsonValueKind.Array:
                var aArray = a.EnumerateArray().ToList();
                var bArray = b.EnumerateArray().ToList();

                if (aArray.Count != bArray.Count)
                    return false;

                for (int i = 0; i < aArray.Count; i++)
                {
                    if (!JsonEquals(aArray[i], bArray[i]))
                        return false;
                }
                return true;

            case JsonValueKind.String:
                return a.GetString() == b.GetString();

            case JsonValueKind.Number:
                return a.GetRawText() == b.GetRawText();

            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return true; // ValueKind already matched

            default:
                return false;
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
