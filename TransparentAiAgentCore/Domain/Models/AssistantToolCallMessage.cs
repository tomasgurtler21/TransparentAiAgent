using System;
using System.Collections.Generic;
using System.Linq;

namespace TransparentAiAgentCore.Domain.Models;

/// <summary>
/// Represents an assistant message that requests one or more tool calls.
/// Derived from AssistantMessage to support polymorphism - can be used anywhere AssistantMessage is expected.
/// </summary>
public class AssistantToolCallMessage : AssistantMessage
{
    /// <summary>
    /// Tool calls requested by the assistant.
    /// Always contains at least one tool call.
    /// Immutable after creation.
    /// </summary>
    public IReadOnlyList<ToolCall> ToolCalls { get; }

    /// <summary>
    /// Creates a new assistant tool call message
    /// </summary>
    /// <param name="content">Message content (can be empty when LLM only requests tools)</param>
    /// <param name="toolCalls">List of tool calls (must contain at least one)</param>
    /// <exception cref="ArgumentException">Thrown when toolCalls is null or empty</exception>
    public AssistantToolCallMessage(string content, List<ToolCall> toolCalls)
        : base(content ?? string.Empty)
    {
        if (toolCalls == null || toolCalls.Count == 0)
            throw new ArgumentException("Must have at least one tool call", nameof(toolCalls));

        // Create defensive copy to ensure immutability
        ToolCalls = toolCalls.ToList().AsReadOnly();
    }
}
