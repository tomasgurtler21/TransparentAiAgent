using System;
using System.Collections.Generic;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a message in the format expected by LLM providers
/// </summary>
public class LLMMessage
{
    public string Role { get; }
    public string Content { get; }
    public List<LLMToolCall>? ToolCalls { get; }
    public string? ToolCallId { get; }

    // For regular messages (user, assistant, system)
    public LLMMessage(string role, string content)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Role = role;
        Content = content;
    }

    // For assistant messages with tool calls
    public LLMMessage(string role, string content, List<LLMToolCall> toolCalls)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));

        Role = role;
        Content = content ?? string.Empty; // Content can be empty when there are tool calls
        ToolCalls = toolCalls ?? throw new ArgumentNullException(nameof(toolCalls));
    }

    // For tool result messages
    public LLMMessage(string role, string content, string toolCallId)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));

        Role = role;
        Content = content;
        ToolCallId = toolCallId;
    }
}
