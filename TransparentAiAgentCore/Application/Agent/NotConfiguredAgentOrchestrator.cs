using System.Runtime.CompilerServices;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Application.Agent;

/// <summary>
/// Stub implementation of IAgentOrchestrator used when LLM is not configured.
/// All operations throw ConfigurationException with helpful message.
/// </summary>
public class NotConfiguredAgentOrchestrator : IAgentOrchestrator
{
    private readonly IConversationManager _conversationManager;

    public IConversationManager ConversationManager => _conversationManager;

    public NotConfiguredAgentOrchestrator(IConversationManager conversationManager)
    {
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
    }

    public Task<IMessage> ProcessUserInputAsync(string userInput, CancellationToken cancellationToken = default)
    {
        throw new ConfigurationException(
            "LLM is not configured. Please configure Azure OpenAI or Anthropic settings in appsettings.json. " +
            "See docs/CONFIGURATION_SETUP.md for instructions.");
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators - intentional for stub implementation
    public async IAsyncEnumerable<StreamingResponseChunk> ProcessUserInputStreamingAsync(
        string userInput,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        throw new ConfigurationException(
            "LLM is not configured. Please configure Azure OpenAI or Anthropic settings in appsettings.json. " +
            "See docs/CONFIGURATION_SETUP.md for instructions.");
#pragma warning disable CS0162 // Unreachable code - needed for compiler
        yield break;
#pragma warning restore CS0162
    }
#pragma warning restore CS1998

    public void StartNewConversation()
    {
        // This operation is safe even without LLM config
        _conversationManager.ClearConversation();
    }
}
