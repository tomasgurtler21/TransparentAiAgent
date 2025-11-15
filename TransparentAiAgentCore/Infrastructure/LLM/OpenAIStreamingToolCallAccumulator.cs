using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore.Infrastructure.LLM;

/// <summary>
/// Accumulates streaming tool call updates from OpenAI by index.
///
/// KEY INSIGHT: OpenAI sends ToolCallId and FunctionName only in the FIRST chunk.
/// Subsequent chunks have ToolCallId=null and only use Index to identify which tool call to update.
///
/// This class matches tool call updates by Index, not by ID.
/// </summary>
public class OpenAIStreamingToolCallAccumulator
{
    // Use Dictionary with Index as key to properly accumulate tool calls
    private readonly Dictionary<int, (string Id, string Name, StringBuilder Arguments)> _accumulatedToolCallsByIndex = new();

    /// <summary>
    /// Adds a tool call update to the accumulator.
    /// OpenAI streams tool calls by INDEX, not ID:
    /// - First chunk: Contains ID + Name + empty/partial arguments
    /// - Subsequent chunks: Only Index + arguments delta (ID and Name are null)
    /// </summary>
    /// <param name="index">The tool call index (stable across all chunks)</param>
    /// <param name="toolCallId">Tool call ID (only in first chunk, null in subsequent)</param>
    /// <param name="functionName">Function name (only in first chunk, null in subsequent)</param>
    /// <param name="argumentsUpdate">Arguments delta to append</param>
    public void AddToolCallUpdate(int index, string? toolCallId, string? functionName, string? argumentsUpdate)
    {
        if (_accumulatedToolCallsByIndex.TryGetValue(index, out var existingToolCall))
        {
            // Update existing tool call entry
            var id = !string.IsNullOrEmpty(toolCallId)
                ? toolCallId
                : existingToolCall.Id;

            var name = !string.IsNullOrEmpty(functionName)
                ? functionName
                : existingToolCall.Name;

            // Append arguments delta
            if (argumentsUpdate != null)
            {
                existingToolCall.Arguments.Append(argumentsUpdate);
            }

            // Update the dictionary entry (ID or Name might have been updated)
            _accumulatedToolCallsByIndex[index] = (id, name, existingToolCall.Arguments);
        }
        else
        {
            // Create new tool call entry
            var id = toolCallId ?? string.Empty;
            var name = functionName ?? string.Empty;
            var arguments = new StringBuilder();

            if (argumentsUpdate != null)
            {
                arguments.Append(argumentsUpdate);
            }

            _accumulatedToolCallsByIndex[index] = (id, name, arguments);
        }
    }

    /// <summary>
    /// Gets the accumulated tool calls as a list.
    /// </summary>
    /// <returns>List of accumulated LLMToolCall objects</returns>
    public List<LLMToolCall> GetAccumulatedToolCalls()
    {
        return _accumulatedToolCallsByIndex.Values
            .Select(tc => new LLMToolCall(tc.Id, tc.Name, tc.Arguments.ToString()))
            .ToList();
    }
}
