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

            AssistantMessage assistantMsg => new LLMMessage("assistant", assistantMsg.Content),

            SystemMessage systemMsg => new LLMMessage("system", systemMsg.Content),

            ToolCallMessage toolCallMsg => new LLMMessage(
                "assistant",
                toolCallMsg.Content,
                new List<LLMToolCall>
                {
                    new LLMToolCall(toolCallMsg.ToolCallId, toolCallMsg.ToolName, toolCallMsg.ToolParameters)
                }),

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

        // If response has tool calls, create ToolCallMessage
        if (response.ToolCalls != null && response.ToolCalls.Count > 0)
        {
            // For simplicity, handle single tool call
            // Multi-tool call support can be added later
            var toolCall = response.ToolCalls[0];
            return new ToolCallMessage(
                toolCall.Name,
                toolCall.Arguments,
                toolCall.Id);
        }

        // Otherwise, create AssistantMessage
        return new AssistantMessage(response.Content);
    }
}
