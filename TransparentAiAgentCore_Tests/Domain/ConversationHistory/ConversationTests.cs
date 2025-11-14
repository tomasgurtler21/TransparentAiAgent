using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.ConversationHistory;

/// <summary>
/// Tests for Conversation domain entity.
/// Following Lean TDD - only testing meaningful behavior (GenerateName logic).
/// </summary>
[TestClass]
public class ConversationTests
{
    [TestMethod]
    public void GenerateName_WithUserMessage_ExtractsContent()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new DirectUserMessage("What is clean architecture")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.AreEqual("What is clean architecture", name);
    }

    [TestMethod]
    public void GenerateName_WithLongMessage_TruncatesTo50Characters()
    {
        // Arrange
        var longMessage = new string('a', 100);
        var messages = new List<IMessage> { new DirectUserMessage(longMessage) };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.AreEqual(53, name.Length); // 50 + "..."
        Assert.IsTrue(name.EndsWith("..."));
        Assert.AreEqual(new string('a', 50) + "...", name);
    }

    [TestMethod]
    public void GenerateName_WithInvalidFileNameChars_Sanitizes()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new DirectUserMessage("File<>:|?*/name")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.IsFalse(name.Contains('<'));
        Assert.IsFalse(name.Contains('>'));
        Assert.IsFalse(name.Contains(':'));
        Assert.IsFalse(name.Contains('|'));
        Assert.IsFalse(name.Contains('?'));
        Assert.IsFalse(name.Contains('*'));
        Assert.IsFalse(name.Contains('/'));
        // Should replace invalid chars with underscores
        Assert.AreEqual("File_______name", name);
    }

    [TestMethod]
    public void GenerateName_NoUserMessage_FallsBackToTimestamp()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new LlmTextMessage("Hello from assistant")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.IsTrue(name.StartsWith("Conversation "));
        // Verify it contains a date/time component (basic check)
        Assert.IsTrue(name.Length > "Conversation ".Length);
    }

    [TestMethod]
    public void GenerateName_EmptyMessageList_FallsBackToTimestamp()
    {
        // Arrange
        var messages = new List<IMessage>();

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.IsTrue(name.StartsWith("Conversation "));
    }

    [TestMethod]
    public void GenerateName_WithMultipleUserMessages_UsesFirstOne()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new DirectUserMessage("First message"),
            new LlmTextMessage("Response"),
            new DirectUserMessage("Second message")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.AreEqual("First message", name);
    }

    [TestMethod]
    public void GenerateName_UserMessageAfterAssistantMessage_StillUsesFirstUserMessage()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new LlmTextMessage("Hello"),
            new DirectUserMessage("User message")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.AreEqual("User message", name);
    }

    [TestMethod]
    public void GenerateName_LongMessageWithInvalidChars_SanitizesAndTruncates()
    {
        // Arrange
        var longInvalidMessage = new string('a', 30) + "<>:|?*/" + new string('b', 30);
        var messages = new List<IMessage> { new DirectUserMessage(longInvalidMessage) };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        // Should sanitize first, then truncate to 50 + "..."
        Assert.AreEqual(53, name.Length);
        Assert.IsTrue(name.EndsWith("..."));
        Assert.IsFalse(name.Contains('<'));
        Assert.IsFalse(name.Contains('>'));
    }

    // Note: Removed GenerateName_WhitespaceOnlyMessage_FallsBackToTimestamp test
    // because DirectUserMessage constructor correctly validates against whitespace-only content.
    // This is proper domain validation and doesn't need to be tested here.
}
