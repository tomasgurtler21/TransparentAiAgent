using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Accumulates streaming chunks into a complete response
/// </summary>
public class StreamingResponseAccumulator
{
    private readonly StringBuilder _contentBuilder = new();
    private readonly Dictionary<string, StringBuilder> _toolCallArguments = new();
    private readonly List<LLMToolCall> _toolCalls = new();
    private string? _finishReason;

    public void AddChunk(StreamingLLMChunk chunk)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));

        // Accumulate content
        if (!string.IsNullOrEmpty(chunk.ContentDelta))
        {
            _contentBuilder.Append(chunk.ContentDelta);
        }

        // Accumulate tool calls
        if (chunk.ToolCallDelta != null)
        {
            var toolCall = chunk.ToolCallDelta;

            if (!_toolCallArguments.ContainsKey(toolCall.Id))
            {
                _toolCallArguments[toolCall.Id] = new StringBuilder();
            }

            _toolCallArguments[toolCall.Id].Append(toolCall.Arguments);
        }

        // Track finish reason
        if (chunk.IsComplete && !string.IsNullOrEmpty(chunk.FinishReason))
        {
            _finishReason = chunk.FinishReason;
        }
    }

    public LLMResponse ToResponse()
    {
        // Build complete tool calls
        List<LLMToolCall>? toolCalls = null;
        if (_toolCallArguments.Count > 0)
        {
            toolCalls = _toolCallArguments
                .Select(kvp => new LLMToolCall(kvp.Key, "extracted_name", kvp.Value.ToString()))
                .ToList();
        }

        return new LLMResponse(
            _contentBuilder.ToString(),
            toolCalls,
            _finishReason,
            null); // Usage not available during streaming
    }

    public void Reset()
    {
        _contentBuilder.Clear();
        _toolCallArguments.Clear();
        _toolCalls.Clear();
        _finishReason = null;
    }
}
