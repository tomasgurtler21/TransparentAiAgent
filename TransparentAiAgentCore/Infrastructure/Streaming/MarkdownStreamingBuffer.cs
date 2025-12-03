using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TransparentAiAgentCore.Infrastructure.Streaming;

/// <summary>
/// Buffers streaming markdown content to prevent rendering glitches from incomplete code blocks.
/// Only buffers code blocks - all other content flushes immediately for smooth streaming.
/// </summary>
public class MarkdownStreamingBuffer
{
    private readonly StringBuilder _buffer = new();
    private readonly ILogger<MarkdownStreamingBuffer>? _logger;

    public MarkdownStreamingBuffer(ILogger<MarkdownStreamingBuffer>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Appends new content and returns renderable content if not in incomplete code block
    /// </summary>
    /// <param name="token">New content to append</param>
    /// <returns>Renderable content if not in code block, null if buffering code block</returns>
    public string? AppendAndGetRenderable(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return token;
        }

        _buffer.Append(token);
        var content = _buffer.ToString();

        // ONLY hold content if we're in an incomplete code block
        var isIncomplete = IsInIncompleteCodeBlock(content);

        // 🔍 DIAGNOSTIC: Log buffering decisions
        if (isIncomplete)
        {
            var contentDisplay = content.Length > 50 ? content.Substring(0, 50).Replace("\n", "\\n") + "..." : content.Replace("\n", "\\n");
            _logger?.LogWarning("[BUFFER DEBUG] 🛑 BUFFERING (incomplete code block detected) - Buffer size: {BufferSize}, Content: '{Content}'",
                content.Length, contentDisplay);
        }

        if (isIncomplete)
        {
            return null;
        }

        // Otherwise, flush immediately for smooth streaming
        var result = content;
        _buffer.Clear();
        return result;
    }

    /// <summary>
    /// Forces flush of all buffered content
    /// </summary>
    public string Flush()
    {
        var result = _buffer.ToString();
        _buffer.Clear();
        return result;
    }

    private bool IsInIncompleteCodeBlock(string content)
    {
        // Count code fence markers (```)
        // Pattern matches: ```language (with any case, special chars like c#/c++, and optional trailing whitespace)
        // Old pattern @"^```[a-z]*$" was too strict - only matched lowercase letters
        // New pattern matches: ```json, ```JSON, ```c#, ```python , ``` (with spaces), etc.
        var fencePattern = @"^```[^\s]*\s*$";
        var lines = content.Split('\n');
        var fenceCount = 0;

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line.Trim(), fencePattern))
            {
                fenceCount++;
            }
        }

        // If odd number of fences, we're inside a code block
        // Also check if the last line is an opening fence
        var lastLine = lines.LastOrDefault()?.Trim() ?? "";
        var endsWithOpeningFence = Regex.IsMatch(lastLine, fencePattern);

        // 🔍 DIAGNOSTIC: Log fence detection details
        var isIncomplete = false;
        if (endsWithOpeningFence && fenceCount % 2 == 1)
        {
            _logger?.LogWarning("[BUFFER DEBUG] Code fence detected - FenceCount: {FenceCount}, LastLine: '{LastLine}', EndsWithFence: true → INCOMPLETE",
                fenceCount, lastLine);
            isIncomplete = true; // Still waiting for closing fence
        }
        else if (fenceCount % 2 == 1)
        {
            _logger?.LogWarning("[BUFFER DEBUG] Code fence detected - FenceCount: {FenceCount} (odd), LastLine: '{LastLine}' → INCOMPLETE",
                fenceCount, lastLine);
            isIncomplete = true;
        }

        // Check if we have an unclosed code block
        return isIncomplete;
    }
}
