using System.Text;
using System.Text.RegularExpressions;

namespace TransparentAiAgentCore.Infrastructure.Streaming;

/// <summary>
/// Buffers streaming markdown content to prevent rendering glitches from incomplete constructs.
/// Only emits complete, renderable markdown units (code blocks, table rows, headings, etc.)
/// </summary>
public class MarkdownStreamingBuffer
{
    private readonly StringBuilder _buffer = new();
    private readonly StringBuilder _renderableBuffer = new();
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private const int ForceFlushMilliseconds = 1000; // Force flush after 1 second

    /// <summary>
    /// Appends new content and returns renderable content if markdown construct is complete
    /// </summary>
    /// <param name="token">New content to append</param>
    /// <returns>Renderable content if construct is complete, null otherwise</returns>
    public string? AppendAndGetRenderable(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return token;
        }

        _buffer.Append(token);

        // Force flush if buffering too long (prevent hanging)
        if ((DateTime.UtcNow - _lastFlushTime).TotalMilliseconds > ForceFlushMilliseconds)
        {
            return ForceFlush();
        }

        var content = _buffer.ToString();

        // Check if we're in an incomplete markdown construct
        if (IsInIncompleteCodeBlock(content))
        {
            return null;
        }

        if (IsInIncompleteTableRow(content))
        {
            return null;
        }

        if (IsInIncompleteHeading(content))
        {
            return null;
        }

        // Content is complete, flush it
        var result = _buffer.ToString();
        _buffer.Clear();
        _lastFlushTime = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Forces flush of all buffered content
    /// </summary>
    public string Flush()
    {
        var result = _buffer.ToString();
        _buffer.Clear();
        _lastFlushTime = DateTime.UtcNow;
        return result;
    }

    private string ForceFlush()
    {
        var result = _buffer.ToString();
        _buffer.Clear();
        _lastFlushTime = DateTime.UtcNow;
        return result;
    }

    private bool IsInIncompleteCodeBlock(string content)
    {
        // Count code fence markers (```)
        var fencePattern = @"^```[a-z]*$";
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
        var endsWithOpeningFence = Regex.IsMatch(lastLine, @"^```[a-z]*$");

        if (endsWithOpeningFence && fenceCount % 2 == 1)
        {
            return true; // Still waiting for closing fence
        }

        // Check if we have an unclosed code block
        return fenceCount % 2 == 1;
    }

    private bool IsInIncompleteTableRow(string content)
    {
        // Check if content starts with | but doesn't end with |\n
        if (content.TrimStart().StartsWith("|"))
        {
            // Check if the last line is a complete table row
            var lastNewlineIndex = content.LastIndexOf('\n');
            var lastLine = lastNewlineIndex >= 0
                ? content.Substring(lastNewlineIndex + 1)
                : content;

            // If last line starts with | but doesn't end with |, it's incomplete
            if (lastLine.TrimStart().StartsWith("|") && !lastLine.TrimEnd().EndsWith("|"))
            {
                return true;
            }

            // If last line has | but no newline after, it's incomplete
            if (lastLine.Contains("|") && !content.EndsWith("\n"))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInIncompleteHeading(string content)
    {
        // Check if content starts with # but doesn't end with newline
        var lines = content.Split('\n');
        var lastLine = lines.LastOrDefault() ?? "";

        // If last line starts with # and has no newline, it's incomplete
        if (lastLine.TrimStart().StartsWith("#") && !content.EndsWith("\n"))
        {
            return true;
        }

        return false;
    }
}
