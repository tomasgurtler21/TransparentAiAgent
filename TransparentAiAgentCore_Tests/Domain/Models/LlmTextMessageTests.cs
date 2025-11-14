using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class LlmTextMessageTests
{
    [TestMethod]
    public void Constructor_ValidContent_SetsDiscriminator()
    {
        // Arrange & Act
        var msg = new LlmTextMessage("Response");

        // Assert
        Assert.AreEqual("Llm.Text", msg.MessageTypeDiscriminator);
    }

    [TestMethod]
    public void Role_IsAssistant()
    {
        // Arrange & Act
        var msg = new LlmTextMessage("Response");

        // Assert
        Assert.AreEqual(MessageRole.Assistant, msg.Role);
    }

    [TestMethod]
    public void Constructor_EmptyContent_IsValid()
    {
        // Arrange & Act - Empty content is valid for tool-call-only responses
        var msg = new LlmTextMessage("");

        // Assert
        Assert.AreEqual("", msg.Content);
    }

    [TestMethod]
    public void Constructor_NullContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LlmTextMessage(null!));
    }

    [TestMethod]
    public void Constructor_ValidContent_InitializesProperties()
    {
        // Arrange & Act
        var msg = new LlmTextMessage("Test content");

        // Assert
        Assert.AreEqual("Test content", msg.Content);
        Assert.AreNotEqual(Guid.Empty, msg.Id);
        Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
        Assert.IsTrue((DateTime.UtcNow - msg.Timestamp).TotalSeconds < 1);
    }
}
