using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class AssistantMessageTests
{
    [TestMethod]
    public void AssistantMessage_NullContent_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => new AssistantMessage(null!));
    }

    [TestMethod]
    public void AssistantMessage_ValidContent_DefaultsToInContext()
    {
        var message = new AssistantMessage("Hello");
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    [TestMethod]
    public void AssistantMessage_GeneratesUniqueIds()
    {
        var msg1 = new AssistantMessage("Hello");
        var msg2 = new AssistantMessage("World");

        Assert.AreNotEqual(msg1.Id, msg2.Id);
        Assert.AreNotEqual(Guid.Empty, msg1.Id);
    }
}
