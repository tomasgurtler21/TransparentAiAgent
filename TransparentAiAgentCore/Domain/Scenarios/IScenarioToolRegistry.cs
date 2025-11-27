using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Domain.Scenarios;

/// <summary>
/// Registry for scenario-specific mock tools.
/// Tools registered here only exist during scenario execution and are automatically cleaned up.
/// Thread-safe for concurrent scenario execution.
/// </summary>
public interface IScenarioToolRegistry
{
    /// <summary>
    /// Registers a mock tool for the current scenario.
    /// The tool will be available to the agent until explicitly unregistered or the scenario completes.
    /// </summary>
    /// <param name="name">The tool name (must be unique across all tools).</param>
    /// <param name="description">Human-readable description of what the tool does.</param>
    /// <param name="parametersSchema">JSON schema describing the tool's parameters.</param>
    /// <param name="responseMap">Mapping from input arguments (as JSON string) to predefined responses.</param>
    void RegisterMockTool(
        string name,
        string description,
        string parametersSchema,
        Dictionary<string, MockToolResponse> responseMap);

    /// <summary>
    /// Unregisters a mock tool by name.
    /// If the tool doesn't exist, this operation is a no-op.
    /// </summary>
    /// <param name="toolName">The name of the tool to unregister.</param>
    void UnregisterMockTool(string toolName);

    /// <summary>
    /// Unregisters all mock tools.
    /// This is typically called during scenario cleanup.
    /// </summary>
    void UnregisterAllMockTools();

    /// <summary>
    /// Gets all currently registered mock tools.
    /// </summary>
    /// <returns>A read-only list of mock tools as ITool instances.</returns>
    IReadOnlyList<ITool> GetAllMockTools();

    /// <summary>
    /// Gets a specific mock tool by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to retrieve.</param>
    /// <returns>The mock tool, or null if not found.</returns>
    ITool? GetMockTool(string toolName);

    /// <summary>
    /// Gets the predefined response for a tool given specific input arguments.
    /// Used by the executor to look up how the mock tool should respond.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="arguments">The input arguments as a JSON string.</param>
    /// <returns>The predefined response, or null if no match is found.</returns>
    MockToolResponse? GetMockResponse(string toolName, string arguments);
}
