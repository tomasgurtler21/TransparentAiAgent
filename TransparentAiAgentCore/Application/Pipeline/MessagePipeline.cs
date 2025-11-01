using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Application.Pipeline;

/// <summary>
/// Transforms messages between domain models and LLM models
/// </summary>
public class MessagePipeline : IMessagePipeline
{
    public LLMMessage ConvertToLLMMessage(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return message switch
        {
            UserMessage userMsg => new LLMMessage("user", userMsg.Content),

            // CRITICAL: Check derived class BEFORE base class!
            AssistantToolCallMessage toolCallMsg => new LLMMessage(
                "assistant",
                toolCallMsg.Content,
                toolCallMsg.ToolCalls
                    .Select(tc => new LLMToolCall(tc.Id, tc.Name, tc.Arguments))
                    .ToList()),

            AssistantMessage assistantMsg => new LLMMessage("assistant", assistantMsg.Content),

            SystemMessage systemMsg => new LLMMessage("system", systemMsg.Content),

            ToolResultMessage toolResultMsg => new LLMMessage(
                "tool",
                toolResultMsg.Result,
                toolResultMsg.ToolCallId),

            _ => throw new AgentException($"Unknown message type: {message.GetType().Name}")
        };
    }

    public List<LLMMessage> ConvertToLLMMessages(IEnumerable<IMessage> messages)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        return messages.Select(ConvertToLLMMessage).ToList();
    }

    public IMessage ConvertToDomainMessage(LLMResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        // Note: Tool call messages are now handled directly in AgentOrchestrator
        // This method is only used for responses without tool calls
        return new AssistantMessage(response.Content);
    }
}
