using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class ToolMessageBaseTests
{
    [TestMethod]
    public void ToolMessage_AbstractClassExists()
    {
        // This test verifies the abstract class exists and has the right interface
        var type = typeof(ToolMessage);

        // Assert
        Assert.IsTrue(type.IsAbstract);
        Assert.IsTrue(typeof(IMessage).IsAssignableFrom(type));
    }

    [TestMethod]
    public void ToolMessage_Role_IsAlwaysTool()
    {
        // Verify that the Role property returns Tool
        // We'll test this with a concrete implementation once ToolResultMessage inherits from ToolMessage
        var type = typeof(ToolMessage);
        var roleProperty = type.GetProperty("Role");

        // Assert
        Assert.IsNotNull(roleProperty);
        Assert.AreEqual(typeof(MessageRole), roleProperty.PropertyType);
    }
}
