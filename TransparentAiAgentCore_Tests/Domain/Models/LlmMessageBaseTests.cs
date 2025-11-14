using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class LlmMessageBaseTests
{
    [TestMethod]
    public void LlmTextMessage_InheritsFromLlmMessage()
    {
        // Arrange & Act
        var msg = new LlmTextMessage("Test");

        // Assert
        Assert.IsInstanceOfType(msg, typeof(LlmMessage));
        Assert.AreEqual(MessageRole.Assistant, msg.Role);
    }

    [TestMethod]
    public void LlmToolCallMessage_InheritsFromLlmMessage()
    {
        // Arrange & Act
        var toolCalls = new List<ToolCall> { new ToolCall("id1", "tool1", "{}") };
        var msg = new LlmToolCallMessage("", toolCalls);

        // Assert
        Assert.IsInstanceOfType(msg, typeof(LlmMessage));
        Assert.AreEqual(MessageRole.Assistant, msg.Role);
    }
}
