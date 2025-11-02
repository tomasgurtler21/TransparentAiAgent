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
    private readonly Dictionary<string, ToolCallData> _toolCalls = new();
    private string? _finishReason;

    private class ToolCallData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public StringBuilder Arguments { get; } = new();
    }

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

            if (!_toolCalls.ContainsKey(toolCall.Id))
            {
                _toolCalls[toolCall.Id] = new ToolCallData
                {
                    Id = toolCall.Id,
                    Name = toolCall.Name
                };
            }

            // Update name if provided (it might come in later chunks)
            if (!string.IsNullOrEmpty(toolCall.Name))
            {
                _toolCalls[toolCall.Id].Name = toolCall.Name;
            }

            // Accumulate arguments
            if (!string.IsNullOrEmpty(toolCall.Arguments))
            {
                _toolCalls[toolCall.Id].Arguments.Append(toolCall.Arguments);
            }
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
        if (_toolCalls.Count > 0)
        {
            toolCalls = _toolCalls.Values
                .Select(tc => new LLMToolCall(tc.Id, tc.Name, tc.Arguments.ToString()))
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
        _toolCalls.Clear();
        _finishReason = null;
    }
}
