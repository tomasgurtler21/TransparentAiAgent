using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class ToolCallMessageTests
{
    [TestMethod]
    public void ToolCallMessage_NullToolName_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new ToolCallMessage(null!, "{}", "call-123"));
    }

    [TestMethod]
    public void ToolCallMessage_NullToolCallId_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new ToolCallMessage("get_weather", "{}", null!));
    }

    [TestMethod]
    public void ToolCallMessage_NullParameters_DefaultsToEmptyJson()
    {
        var message = new ToolCallMessage("get_weather", null!, "call-123");
        Assert.AreEqual("{}", message.ToolParameters);
    }

    [TestMethod]
    public void ToolCallMessage_FormatsContentCorrectly()
    {
        var message = new ToolCallMessage("get_weather", "{\"city\":\"Prague\"}", "call-123");

        // Content should be human-readable format
        Assert.IsTrue(message.Content.Contains("Tool Call:"));
        Assert.IsTrue(message.Content.Contains("get_weather"));
        Assert.IsTrue(message.Content.Contains("{\"city\":\"Prague\"}"));
    }

    [TestMethod]
    public void ToolCallMessage_DefaultsToInContext()
    {
        var message = new ToolCallMessage("get_weather", "{}", "call-123");
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    [TestMethod]
    public void ToolCallMessage_GeneratesUniqueIds()
    {
        var msg1 = new ToolCallMessage("tool1", "{}", "call-1");
        var msg2 = new ToolCallMessage("tool2", "{}", "call-2");

        Assert.AreNotEqual(msg1.Id, msg2.Id);
        Assert.AreNotEqual(Guid.Empty, msg1.Id);
    }
}
