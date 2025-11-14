using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

/// <summary>
/// Tests for NEW ToolResultMessage (inheriting from ToolMessage base class).
/// This is part of the 4-tier message hierarchy refactor.
/// </summary>
[TestClass]
public class ToolResultMessageNewTests
{
    [TestMethod]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var toolCallId = "tool_call_123";

        // Act
        var msg = new ToolResultMessage(toolCallId, "get_weather", "{\"temp\":20}", false);

        // Assert
        Assert.AreEqual(toolCallId, msg.ToolCallId);
        Assert.AreEqual("get_weather", msg.ToolName);
        Assert.AreEqual("{\"temp\":20}", msg.Result);
        Assert.IsFalse(msg.IsError);
        Assert.AreEqual(MessageRole.Tool, msg.Role);
        Assert.AreEqual("Tool.Result", msg.MessageTypeDiscriminator);
        Assert.AreNotEqual(Guid.Empty, msg.Id);
    }

    [TestMethod]
    public void Constructor_NullToolName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new ToolResultMessage("tool_call_123", null!, "result", false));
    }

    [TestMethod]
    public void Constructor_NullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new ToolResultMessage("tool_call_123", "tool", null!, false));
    }

    [TestMethod]
    public void Constructor_EmptyToolName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => new ToolResultMessage("tool_call_123", "", "result", false));
    }

    [TestMethod]
    public void Constructor_WhitespaceToolName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => new ToolResultMessage("tool_call_123", "   ", "result", false));
    }

    [TestMethod]
    public void Result_SuccessfulToolExecution_ReturnsResult()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("tool_call_123", "get_weather", "{\"temp\":20}", false);

        // Assert
        Assert.AreEqual("{\"temp\":20}", msg.Result);
    }

    [TestMethod]
    public void IsError_False_WhenConstructedWithFalse()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("tool_call_123", "tool", "result", false);

        // Assert
        Assert.IsFalse(msg.IsError);
    }

    [TestMethod]
    public void IsError_True_WhenConstructedWithTrue()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("tool_call_123", "tool", "error details", true);

        // Assert
        Assert.IsTrue(msg.IsError);
    }

    [TestMethod]
    public void Content_ContainsResult()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("tool_call_123", "get_weather", "{\"temp\":20}", false);

        // Assert
        Assert.AreEqual("{\"temp\":20}", msg.Content);
    }

    [TestMethod]
    public void DefaultsToInContext()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("tool_call_123", "tool", "result", false);

        // Assert
        Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
    }

    [TestMethod]
    public void InheritsFromToolMessage()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("tool_call_123", "tool", "result", false);

        // Assert
        Assert.IsInstanceOfType(msg, typeof(ToolMessage));
    }

    [TestMethod]
    public void MessageTypeDiscriminator_ReturnsCorrectValue()
    {
        // Arrange & Act
        var msg = new ToolResultMessage("tool_call_123", "tool", "result", false);

        // Assert
        Assert.AreEqual("Tool.Result", msg.MessageTypeDiscriminator);
    }
}
