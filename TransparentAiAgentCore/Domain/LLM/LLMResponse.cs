using System;
using System.Collections.Generic;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a complete response from an LLM provider
/// </summary>
public class LLMResponse
{
    public string Content { get; }
    public List<LLMToolCall>? ToolCalls { get; }
    public string? FinishReason { get; }
    public LLMUsage? Usage { get; }

    /// <summary>
    /// Extended thinking content from the model (e.g., Anthropic's thinking blocks).
    /// This represents the model's internal reasoning process.
    /// </summary>
    public string? Thinking { get; }

    public LLMResponse(
        string? content,
        List<LLMToolCall>? toolCalls = null,
        string? finishReason = null,
        LLMUsage? usage = null,
        string? thinking = null)
    {
        // Content can be empty if there are tool calls or thinking
        if (string.IsNullOrEmpty(content) &&
            (toolCalls == null || toolCalls.Count == 0) &&
            string.IsNullOrEmpty(thinking))
            throw new ArgumentException("Response must have either content, tool calls, or thinking");

        Content = content ?? string.Empty;
        ToolCalls = toolCalls;
        FinishReason = finishReason;
        Usage = usage;
        Thinking = thinking;
    }
}
