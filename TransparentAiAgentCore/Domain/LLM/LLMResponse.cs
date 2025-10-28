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

    public LLMResponse(
        string? content,
        List<LLMToolCall>? toolCalls = null,
        string? finishReason = null,
        LLMUsage? usage = null)
    {
        // Content can be empty if there are tool calls
        if (string.IsNullOrEmpty(content) && (toolCalls == null || toolCalls.Count == 0))
            throw new ArgumentException("Response must have either content or tool calls");

        Content = content ?? string.Empty;
        ToolCalls = toolCalls;
        FinishReason = finishReason;
        Usage = usage;
    }
}
