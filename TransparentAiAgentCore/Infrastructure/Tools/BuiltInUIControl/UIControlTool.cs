using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Represents a built-in UI control tool.
/// Implements ITool abstraction for tools that control the UI dynamically.
/// </summary>
public class UIControlTool : ITool
{
    private readonly IReadOnlyDictionary<string, string> _metadata;

    /// <summary>
    /// Initializes a new instance of the UIControlTool class.
    /// </summary>
    /// <param name="name">The unique tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <param name="parametersSchema">The JSON Schema for parameters.</param>
    /// <exception cref="ArgumentException">Thrown when name or description is null or whitespace.</exception>
    public UIControlTool(string name, string description, string parametersSchema)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tool description cannot be null or whitespace", nameof(description));

        Name = name;
        Description = description;
        ParametersSchema = parametersSchema ?? "{}";

        // Build metadata
        var metadata = new Dictionary<string, string>
        {
            ["SourceType"] = "BuiltInUIControl",
            ["Category"] = "UI Control"
        };

        _metadata = metadata;
    }

    /// <summary>
    /// Gets the unique tool name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the tool description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the JSON Schema for parameters.
    /// </summary>
    public string ParametersSchema { get; }

    /// <summary>
    /// Gets the tool source type (always BuiltInUIControl).
    /// </summary>
    public ToolSourceType SourceType => ToolSourceType.BuiltInUIControl;

    /// <summary>
    /// Gets the tool metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata => _metadata;
}
