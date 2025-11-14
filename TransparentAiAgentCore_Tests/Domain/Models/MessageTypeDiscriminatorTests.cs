using System;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class MessageTypeDiscriminatorTests
{
    [TestMethod]
    public void UserMessage_HasDiscriminator()
    {
        // Arrange & Act
        var msg = new DirectUserMessage("Test");

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(msg.MessageTypeDiscriminator));
    }

    [TestMethod]
    public void LlmTextMessage_HasDiscriminator()
    {
        // Arrange & Act
        var msg = new LlmTextMessage("Test");

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(msg.MessageTypeDiscriminator));
        Assert.AreEqual("Llm.Text", msg.MessageTypeDiscriminator);
    }

    [TestMethod]
    public void ToolResultMessage_HasDiscriminator()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("call-123", "test_tool", "result", false);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(msg.MessageTypeDiscriminator));
        Assert.AreEqual("Tool.Result", msg.MessageTypeDiscriminator);
    }
}
