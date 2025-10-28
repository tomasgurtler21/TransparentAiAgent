using System;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents token usage information
/// </summary>
public class LLMUsage
{
    public int PromptTokens { get; }
    public int CompletionTokens { get; }
    public int TotalTokens { get; }

    public LLMUsage(int promptTokens, int completionTokens)
    {
        if (promptTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(promptTokens), "Prompt tokens cannot be negative");
        if (completionTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(completionTokens), "Completion tokens cannot be negative");

        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        TotalTokens = promptTokens + completionTokens;
    }
}
