using System;
using System.Collections.Generic;

namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a request to an LLM provider
/// </summary>
public class LLMRequest
{
    public List<LLMMessage> Messages { get; }
    public List<LLMTool>? Tools { get; }
    public double Temperature { get; }
    public double TopP { get; }
    public int MaxTokens { get; }
    public bool Stream { get; }

    public LLMRequest(
        List<LLMMessage> messages,
        double temperature = 0.7,
        double topP = 1.0,
        int maxTokens = 4096,
        bool stream = false,
        List<LLMTool>? tools = null)
    {
        if (messages == null || messages.Count == 0)
            throw new ArgumentException("Messages cannot be null or empty", nameof(messages));
        if (temperature < 0 || temperature > 2)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0 and 2");
        if (topP < 0 || topP > 1)
            throw new ArgumentOutOfRangeException(nameof(topP), "TopP must be between 0 and 1");
        if (maxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "MaxTokens must be greater than 0");

        Messages = messages;
        Temperature = temperature;
        TopP = topP;
        MaxTokens = maxTokens;
        Stream = stream;
        Tools = tools;
    }
}
