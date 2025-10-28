using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore.Application.Pipeline;

/// <summary>
/// Transforms messages between domain models and LLM models
/// </summary>
public interface IMessagePipeline
{
    /// <summary>
    /// Convert domain message to LLM message
    /// </summary>
    LLMMessage ConvertToLLMMessage(IMessage message);

    /// <summary>
    /// Convert multiple domain messages to LLM messages
    /// </summary>
    List<LLMMessage> ConvertToLLMMessages(IEnumerable<IMessage> messages);

    /// <summary>
    /// Convert LLM response to domain message
    /// </summary>
    IMessage ConvertToDomainMessage(LLMResponse response);
}
