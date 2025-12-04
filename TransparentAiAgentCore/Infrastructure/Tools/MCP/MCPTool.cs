using System.Text.Json;
using ModelContextProtocol.Client;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.MCP;

/// <summary>
/// Represents a tool from an MCP server, implementing the ITool abstraction.
/// </summary>
public class MCPTool : ITool
{
    private readonly McpClientTool _mcpClientTool;
    private readonly string _serverName;
    private readonly IReadOnlyDictionary<string, string> _metadata;

    public MCPTool(McpClientTool mcpClientTool, string serverName)
    {
        _mcpClientTool = mcpClientTool ?? throw new ArgumentNullException(nameof(mcpClientTool));

        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name cannot be null or whitespace", nameof(serverName));

        _serverName = serverName;

        // Build metadata
        var metadata = new Dictionary<string, string>
        {
            ["ServerName"] = serverName,
            ["SourceType"] = "MCP"
        };

        _metadata = metadata;
    }

    /// <summary>
    /// Gets the unique tool name.
    /// </summary>
    public string Name => _mcpClientTool.Name;

    /// <summary>
    /// Gets the tool description.
    /// </summary>
    public string Description => _mcpClientTool.Description ?? string.Empty;

    /// <summary>
    /// Gets the JSON Schema for parameters.
    /// </summary>
    public string ParametersSchema
    {
        get
        {
            // Extract the input schema from McpClientTool.JsonSchema property
            // JsonSchema is a JsonElement containing the tool's parameter schema
            try
            {
                var jsonSchema = _mcpClientTool.JsonSchema;
                if (jsonSchema.ValueKind == System.Text.Json.JsonValueKind.Undefined ||
                    jsonSchema.ValueKind == System.Text.Json.JsonValueKind.Null)
                {
                    return "{}";
                }

                // Serialize the JsonElement to a string
                return JsonSerializer.Serialize(jsonSchema);
            }
            catch
            {
                // If there's any issue accessing the schema, return empty schema
                return "{}";
            }
        }
    }

    /// <summary>
    /// Gets the tool source type (always MCP).
    /// </summary>
    public ToolSourceType SourceType => ToolSourceType.MCP;

    /// <summary>
    /// Gets the tool metadata (includes server name).
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    /// <summary>
    /// Gets the underlying MCP client tool.
    /// </summary>
    internal McpClientTool McpClientTool => _mcpClientTool;
}
