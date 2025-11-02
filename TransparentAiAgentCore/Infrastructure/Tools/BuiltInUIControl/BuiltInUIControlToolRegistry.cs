using System.Text.Json;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;

/// <summary>
/// Registry for built-in UI control tools.
/// These tools allow the agent to dynamically control the UI for teaching and demonstration purposes.
/// </summary>
public class BuiltInUIControlToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<ITool> _tools;

    public BuiltInUIControlToolRegistry()
    {
        _tools = RegisterAllTools();
    }

    /// <summary>
    /// Gets all registered UI control tools.
    /// </summary>
    public IReadOnlyList<ITool> GetAllTools()
    {
        return _tools;
    }

    /// <summary>
    /// Gets a tool by name (case-insensitive).
    /// </summary>
    public ITool? GetTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        return _tools.FirstOrDefault(t =>
            t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    public bool HasTool(string toolName)
    {
        return GetTool(toolName) != null;
    }

    /// <summary>
    /// Refreshes the tool registry.
    /// For static built-in tools, this is a no-op.
    /// </summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Built-in tools are static, no refresh needed
        return Task.CompletedTask;
    }

    private static IReadOnlyList<ITool> RegisterAllTools()
    {
        var tools = new List<ITool>
        {
            CreateChatFilterTool(),
            CreateFilterVisibilityTool(),
            CreateGetStateTool(),
            CreateTransparencyViewerTool(),
            CreateToolsPanelTool(),
            CreateContextIndicatorsTool(),
            CreateConfigurationTool()
        };

        return tools.AsReadOnly();
    }

    private static UIControlTool CreateChatFilterTool()
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                show_user_messages = new { type = "boolean", description = "Show user messages in chat history" },
                show_assistant_messages = new { type = "boolean", description = "Show assistant messages in chat history" },
                show_system_messages = new { type = "boolean", description = "Show system messages (system prompt) in chat history" },
                show_tool_calls = new { type = "boolean", description = "Show tool call messages in chat history" },
                show_tool_results = new { type = "boolean", description = "Show tool result messages in chat history" },
                show_truncated_messages = new { type = "boolean", description = "Show messages that have been truncated from context" }
            }
        });

        return new UIControlTool(
            "ui_control_chat_filter",
            "Control which message types are visible in the chat history. Use this to focus the user's attention on specific aspects of the conversation (e.g., show system messages to explain behavior, hide tool calls to reduce noise).",
            schema);
    }

    private static UIControlTool CreateFilterVisibilityTool()
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                visible = new { type = "boolean", description = "Whether filter controls should be visible to the user" }
            },
            required = new[] { "visible" }
        });

        return new UIControlTool(
            "ui_control_filter_visibility",
            "Show or hide the filter control checkboxes themselves. Useful in Teaching Mode to progressively reveal UI controls. Once visible, users can interact with controls directly.",
            schema);
    }

    private static UIControlTool CreateGetStateTool()
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                component = new
                {
                    type = "string",
                    @enum = new[] { "all", "chat_filter", "transparency_viewer", "tools_panel", "context_indicators", "configuration" },
                    description = "Which component's state to retrieve. Use 'all' for complete UI state."
                }
            }
        });

        return new UIControlTool(
            "ui_get_state",
            "Get the current state of UI components to understand what the user is currently seeing. Use this to check UI state before making changes or to provide context-aware responses.",
            schema);
    }

    private static UIControlTool CreateTransparencyViewerTool()
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                visible = new { type = "boolean", description = "Show or hide the transparency viewer panel" },
                event_type_filters = new
                {
                    type = "array",
                    items = new { type = "string" },
                    description = "List of event types to display (e.g., ['LLMRequest', 'ToolCallStarted']). Empty array shows all."
                },
                show_timestamps = new { type = "boolean", description = "Show timestamps for each event" }
            }
        });

        return new UIControlTool(
            "ui_control_transparency_viewer",
            "Control the transparency viewer panel that shows real-time event logging. Can show/hide viewer and filter specific event types.",
            schema);
    }

    private static UIControlTool CreateToolsPanelTool()
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                visible = new { type = "boolean", description = "Show or hide the tools panel" },
                expand_tool = new { type = "string", description = "Tool name to expand (shows full definition)" },
                collapse_tool = new { type = "string", description = "Tool name to collapse" },
                highlight_tool = new { type = "string", description = "Tool name to highlight (visual emphasis)" }
            }
        });

        return new UIControlTool(
            "ui_control_tools_panel",
            "Control the Tools Overview panel. Can expand/collapse specific tools, highlight tools, or navigate to the Tools page.",
            schema);
    }

    private static UIControlTool CreateContextIndicatorsTool()
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                visible = new { type = "boolean", description = "Show or hide context status indicators" },
                highlighted = new { type = "boolean", description = "Add visual emphasis to context indicators (e.g., glow, animation)" }
            }
        });

        return new UIControlTool(
            "ui_control_context_indicators",
            "Control the context status indicators (✅ In Context / ⚠️ Truncated) shown on messages. Useful for teaching about context windows and truncation.",
            schema);
    }

    private static UIControlTool CreateConfigurationTool()
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                navigate = new { type = "boolean", description = "Navigate to the Configuration page" },
                highlight_section = new
                {
                    type = "string",
                    @enum = new[] { "system-prompt", "llm-parameters", "tools-config" },
                    description = "Section ID to highlight on the Configuration page"
                }
            }
        });

        return new UIControlTool(
            "ui_control_configuration",
            "Control the Configuration page. Can navigate to the page and highlight specific sections (e.g., system prompt, LLM parameters).",
            schema);
    }
}
