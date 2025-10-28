using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class ToolResultMessageTests
{
    [TestMethod]
    public void ToolResultMessage_NullToolCallId_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new ToolResultMessage(null!, "get_weather", "{}", true));
    }

    [TestMethod]
    public void ToolResultMessage_NullToolName_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new ToolResultMessage("call-123", null!, "{}", true));
    }

    [TestMethod]
    public void ToolResultMessage_NullResult_DefaultsToEmptyJson()
    {
        var message = new ToolResultMessage("call-123", "get_weather", null!, true);
        Assert.AreEqual("{}", message.Result);
    }

    [TestMethod]
    public void ToolResultMessage_Success_FormatsContentCorrectly()
    {
        var message = new ToolResultMessage("call-123", "get_weather",
            "{\"temp\":20}", true);

        Assert.IsTrue(message.Content.Contains("Tool Result:"));
        Assert.IsTrue(message.Content.Contains("{\"temp\":20}"));
        Assert.IsFalse(message.Content.Contains("Error"));
    }

    [TestMethod]
    public void ToolResultMessage_Error_FormatsContentCorrectly()
    {
        var message = new ToolResultMessage("call-123", "get_weather",
            null!, false, "Connection timeout");

        Assert.IsTrue(message.Content.Contains("Tool Error:"));
        Assert.IsTrue(message.Content.Contains("Connection timeout"));
        Assert.IsFalse(message.Content.Contains("Tool Result:"));
    }

    [TestMethod]
    public void ToolResultMessage_DefaultsToInContext()
    {
        var message = new ToolResultMessage("call-123", "get_weather", "{}", true);
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    [TestMethod]
    public void ToolResultMessage_GeneratesUniqueIds()
    {
        var msg1 = new ToolResultMessage("call-1", "tool1", "{}", true);
        var msg2 = new ToolResultMessage("call-2", "tool2", "{}", true);

        Assert.AreNotEqual(msg1.Id, msg2.Id);
        Assert.AreNotEqual(Guid.Empty, msg1.Id);
    }
}
