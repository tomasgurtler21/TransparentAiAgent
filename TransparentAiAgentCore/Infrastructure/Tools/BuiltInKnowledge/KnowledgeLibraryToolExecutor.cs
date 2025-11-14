using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;

/// <summary>
/// Executes knowledge library tool calls by querying IKnowledgeLibrary.
/// Formats knowledge entries as markdown for LLM consumption.
/// </summary>
public class KnowledgeLibraryToolExecutor : IToolExecutor
{
    private readonly IKnowledgeLibrary _library;
    private readonly ILogger<KnowledgeLibraryToolExecutor> _logger;

    public KnowledgeLibraryToolExecutor(
        IKnowledgeLibrary library,
        ILogger<KnowledgeLibraryToolExecutor> logger)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ToolSourceType SourceType => ToolSourceType.BuiltInKnowledge;

    public Task<ToolExecutionResult> ExecuteAsync(
        ITool tool,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing knowledge library tool: {ToolName} with arguments: {Arguments}",
                tool.Name, arguments);

            // Validate tool name
            if (tool.Name != "knowledge_library_query")
            {
                return Task.FromResult(ToolExecutionResult.Failure(
                    $"Unknown tool: {tool.Name}",
                    stopwatch.Elapsed));
            }

            // Parse arguments
            JsonDocument argsDoc;
            try
            {
                argsDoc = JsonDocument.Parse(arguments);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse arguments");
                return Task.FromResult(ToolExecutionResult.Failure(
                    $"Invalid JSON arguments: {ex.Message}",
                    stopwatch.Elapsed));
            }

            // Extract topic parameter
            var root = argsDoc.RootElement;
            if (!root.TryGetProperty("topic", out var topicProperty))
            {
                return Task.FromResult(ToolExecutionResult.Failure(
                    "Missing required parameter 'topic'",
                    stopwatch.Elapsed));
            }

            var topicId = topicProperty.GetString();
            if (string.IsNullOrWhiteSpace(topicId))
            {
                return Task.FromResult(ToolExecutionResult.Failure(
                    "Parameter 'topic' cannot be empty",
                    stopwatch.Elapsed));
            }

            // Query library
            var entry = _library.GetTopic(topicId);
            if (entry == null)
            {
                _logger.LogWarning("Topic not found: {TopicId}", topicId);
                return Task.FromResult(ToolExecutionResult.Failure(
                    $"Topic '{topicId}' not found in knowledge library",
                    stopwatch.Elapsed));
            }

            // Format entry for LLM consumption
            var formattedContent = FormatKnowledgeEntry(entry);

            stopwatch.Stop();
            _logger.LogInformation("Successfully retrieved knowledge entry: {TopicId}", topicId);

            return Task.FromResult(ToolExecutionResult.Success(formattedContent, stopwatch.Elapsed));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error executing knowledge library tool");
            return Task.FromResult(ToolExecutionResult.Failure(
                $"Unexpected error: {ex.Message}",
                stopwatch.Elapsed));
        }
    }

    private static string FormatKnowledgeEntry(KnowledgeEntry entry)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {entry.Topic}");
        sb.AppendLine();
        sb.AppendLine($"**Category:** {entry.Category}");
        sb.AppendLine($"**Knowledge Gap Likelihood:** {entry.KnowledgeGapLikelihood}");
        sb.AppendLine($"**Last Updated:** {entry.LastUpdated}");
        sb.AppendLine();

        // Overview
        sb.AppendLine("## Overview");
        sb.AppendLine(entry.Content.Overview);
        sb.AppendLine();

        // Key Points
        if (entry.Content.KeyPoints.Any())
        {
            sb.AppendLine("## Key Points");
            foreach (var point in entry.Content.KeyPoints)
            {
                sb.AppendLine($"- {point}");
            }
            sb.AppendLine();
        }

        // Examples
        if (entry.Content.Examples.Any())
        {
            sb.AppendLine("## Examples");
            foreach (var example in entry.Content.Examples)
            {
                sb.AppendLine($"### {example.Title}");
                if (!string.IsNullOrEmpty(example.Code))
                {
                    sb.AppendLine("```");
                    sb.AppendLine(example.Code);
                    sb.AppendLine("```");
                }
                sb.AppendLine(example.Explanation);
                sb.AppendLine();
            }
        }

        // Warnings
        if (entry.Content.Warnings.Any())
        {
            sb.AppendLine("## ⚠️ Warnings");
            foreach (var warning in entry.Content.Warnings)
            {
                sb.AppendLine($"- {warning}");
            }
            sb.AppendLine();
        }

        // Best Practices
        if (entry.Content.BestPractices.Any())
        {
            sb.AppendLine("## Best Practices");
            foreach (var practice in entry.Content.BestPractices)
            {
                sb.AppendLine($"- {practice}");
            }
            sb.AppendLine();
        }

        // Related Topics
        if (entry.Content.RelatedTopics.Any())
        {
            sb.AppendLine("## Related Topics");
            sb.AppendLine($"For more information, you can also query: {string.Join(", ", entry.Content.RelatedTopics)}");
            sb.AppendLine();
        }

        // References
        if (entry.Content.References.Any())
        {
            sb.AppendLine("## References");
            foreach (var reference in entry.Content.References)
            {
                if (!string.IsNullOrEmpty(reference.Url))
                {
                    sb.AppendLine($"- [{reference.Title}]({reference.Url})");
                }
                else
                {
                    sb.AppendLine($"- {reference.Title}");
                }
            }
        }

        return sb.ToString();
    }
}
