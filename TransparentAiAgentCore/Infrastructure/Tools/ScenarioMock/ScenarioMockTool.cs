using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.ScenarioMock;

/// <summary>
/// Represents a scenario-specific mock tool with predefined responses.
/// Mock tools are temporary, scenario-scoped, and used for teaching/demonstration purposes.
/// </summary>
public class ScenarioMockTool : ITool
{
    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the human-readable description of what the tool does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the JSON schema describing the tool's parameters.
    /// </summary>
    public string ParametersSchema { get; }

    /// <summary>
    /// Gets the tool source type (always ScenarioMock).
    /// </summary>
    public ToolSourceType SourceType => ToolSourceType.ScenarioMock;

    /// <summary>
    /// Gets the tool metadata (indicates it's a scenario mock tool).
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the response map for this mock tool.
    /// Maps input arguments (as JSON string) to predefined responses.
    /// Internal for use by ScenarioMockToolExecutor.
    /// </summary>
    internal IReadOnlyDictionary<string, MockToolResponse> ResponseMap { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioMockTool"/> class.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <param name="parametersSchema">The JSON schema for parameters.</param>
    /// <param name="responseMap">The mapping from arguments to responses.</param>
    public ScenarioMockTool(
        string name,
        string description,
        string parametersSchema,
        Dictionary<string, MockToolResponse> responseMap)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tool description cannot be null or whitespace", nameof(description));

        if (string.IsNullOrWhiteSpace(parametersSchema))
            throw new ArgumentException("Parameters schema cannot be null or whitespace", nameof(parametersSchema));

        if (responseMap == null || responseMap.Count == 0)
            throw new ArgumentException("Response map cannot be null or empty", nameof(responseMap));

        Name = name;
        Description = description;
        ParametersSchema = parametersSchema;
        ResponseMap = responseMap.AsReadOnly();

        Metadata = new Dictionary<string, string>
        {
            { "SourceType", SourceType.ToString() },
            { "IsScenarioMock", "true" },
            { "ResponseCount", responseMap.Count.ToString() }
        };
    }
}
