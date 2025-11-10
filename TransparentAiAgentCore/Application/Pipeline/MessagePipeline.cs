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

        var messageList = messages.ToList();
        var llmMessages = new List<LLMMessage>();

        for (int i = 0; i < messageList.Count; i++)
        {
            var message = messageList[i];
            var isLastMessage = (i == messageList.Count - 1);

            // Filter out empty assistant messages that are NOT the final message
            // Per Anthropic API: "all messages must have non-empty content except for the optional final assistant message"
            if (message is AssistantMessage assistantMsg &&
                string.IsNullOrEmpty(assistantMsg.Content) &&
                !isLastMessage)
            {
                // Skip this empty assistant message as it's not the final message
                continue;
            }

            llmMessages.Add(ConvertToLLMMessage(message));
        }

        return llmMessages;
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
