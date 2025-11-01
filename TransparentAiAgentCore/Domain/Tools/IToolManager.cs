using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore.Domain.Tools;

/// <summary>
/// High-level tool management and orchestration.
/// Coordinates between tool registry, executors, and LLM integration.
/// </summary>
public interface IToolManager
{
    /// <summary>
    /// Gets the tool registry.
    /// </summary>
    IToolRegistry Registry { get; }

    /// <summary>
    /// Executes a tool call from the LLM.
    /// </summary>
    /// <param name="toolCall">The tool call from the LLM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tool execution result.</returns>
    Task<ToolExecutionResult> ExecuteToolCallAsync(
        LLMToolCall toolCall,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts registered tools to LLM tool format for inclusion in LLM requests.
    /// </summary>
    /// <returns>A list of tools in LLM format.</returns>
    List<LLMTool> GetLLMToolDefinitions();
}
