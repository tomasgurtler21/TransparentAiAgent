using System;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Represents a single tool call request from the LLM.
/// This is a value object, NOT an IMessage.
/// It encapsulates the data for one tool invocation.
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Unique identifier for this tool call (used to match with tool results)
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Name of the tool to invoke
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// JSON-formatted arguments for the tool call
    /// </summary>
    public string Arguments { get; }

    /// <summary>
    /// Creates a new tool call instance
    /// </summary>
    /// <param name="id">Unique tool call identifier</param>
    /// <param name="name">Tool name</param>
    /// <param name="arguments">JSON arguments (null defaults to "{}")</param>
    /// <exception cref="ArgumentException">Thrown when id or name is null/empty/whitespace</exception>
    public ToolCall(string id, string name, string arguments)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(name));

        Id = id;
        Name = name;
        Arguments = arguments ?? "{}";
    }
}
