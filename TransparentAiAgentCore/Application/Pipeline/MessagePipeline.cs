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
            // NEW MESSAGE TYPES (Session 2.1)
            // CRITICAL: Check specific concrete types BEFORE base classes!

            // User-originated messages
            DirectUserMessage directUserMsg => new LLMMessage("user", directUserMsg.Content),
            ScenarioUserMessage scenarioUserMsg => new LLMMessage("user", scenarioUserMsg.Content), // Annotation is UI-only

            // LLM-originated messages
            LlmToolCallMessage llmToolCallMsg => new LLMMessage(
                "assistant",
                llmToolCallMsg.Content,
                llmToolCallMsg.ToolCalls
                    .Select(tc => new LLMToolCall(tc.Id, tc.Name, tc.Arguments))
                    .ToList()),
            LlmTextMessage llmTextMsg => new LLMMessage("assistant", llmTextMsg.Content),

            // Application-originated messages
            ScenarioAssistantMessage scenarioAssistantMsg => new LLMMessage("assistant", scenarioAssistantMsg.Content), // Annotation is UI-only

            // Tool-originated messages (new hierarchy)
            ToolResultMessage toolResultMsg => new LLMMessage(
                "tool",
                toolResultMsg.Result,
                toolResultMsg.ToolCallId),
            ToolErrorMessage toolErrorMsg => new LLMMessage(
                "tool",
                toolErrorMsg.Content, // Content already formatted with error message
                toolErrorMsg.ToolCallId),

            // System messages
            SystemMessage systemMsg => new LLMMessage("system", systemMsg.Content),

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
            // BUT: Keep tool call messages even with empty content (tool calls are valid content)

            bool isEmptyLlmText = message is LlmTextMessage llmTextMsg &&
                                  string.IsNullOrEmpty(llmTextMsg.Content) &&
                                  !isLastMessage;

            if (isEmptyLlmText)
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
        return new LlmTextMessage(response.Content);
    }
}
