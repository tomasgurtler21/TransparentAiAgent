using System;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a tool definition to send to the LLM
/// </summary>
public class LLMTool
{
    public string Name { get; }
    public string Description { get; }
    public string ParametersSchema { get; } // JSON schema

    public LLMTool(string name, string description, string parametersSchema)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tool description cannot be null or whitespace", nameof(description));

        Name = name;
        Description = description;
        ParametersSchema = parametersSchema ?? "{}";
    }
}
