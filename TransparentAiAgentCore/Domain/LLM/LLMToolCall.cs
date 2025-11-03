using System;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a tool call request from the LLM.
/// Can also represent a streaming delta where name/arguments may be incomplete.
/// </summary>
public class LLMToolCall
{
    public string Id { get; }
    public string Name { get; }
    public string Arguments { get; } // JSON string

    public LLMToolCall(string id, string name, string arguments)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(id));

        // Allow empty name for streaming deltas (name may arrive in later chunks)
        if (name == null)
            throw new ArgumentException("Tool name cannot be null", nameof(name));

        Id = id;
        Name = name;
        Arguments = arguments ?? "{}"; // Default to empty object
    }
}
