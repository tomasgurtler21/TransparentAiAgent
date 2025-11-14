using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

/// <summary>
/// Tests for ToolErrorMessage (part of 4-tier message hierarchy).
/// </summary>
[TestClass]
public class ToolErrorMessageTests
{
    [TestMethod]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var toolCallId = "tool_call_456";
        var exception = new InvalidOperationException("Test error");

        // Act
        var msg = new ToolErrorMessage(toolCallId, "tool", "Error message", exception);

        // Assert
        Assert.AreEqual(toolCallId, msg.ToolCallId);
        Assert.AreEqual("tool", msg.ToolName);
        Assert.AreEqual("Error message", msg.ErrorMessage);
        Assert.AreEqual(exception, msg.Exception);
        Assert.AreEqual("Tool.Error", msg.MessageTypeDiscriminator);
        Assert.AreNotEqual(Guid.Empty, msg.Id);
        Assert.AreEqual(MessageRole.Tool, msg.Role);
    }

    [TestMethod]
    public void Constructor_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new ToolErrorMessage("tool_call_456", "tool", null!));
    }

    [TestMethod]
    public void Constructor_NullToolName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new ToolErrorMessage("tool_call_456", null!, "error"));
    }

    [TestMethod]
    public void Constructor_EmptyToolName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => new ToolErrorMessage("tool_call_456", "", "error"));
    }

    [TestMethod]
    public void Constructor_EmptyErrorMessage_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => new ToolErrorMessage("tool_call_456", "tool", ""));
    }

    [TestMethod]
    public void Constructor_WithoutException_CreatesMessage()
    {
        // Arrange & Act
        var msg = new ToolErrorMessage("tool_call_456", "tool", "Error");

        // Assert
        Assert.IsNull(msg.Exception);
        Assert.AreEqual("Error", msg.ErrorMessage);
    }

    [TestMethod]
    public void Content_ContainsToolNameAndError()
    {
        // Arrange & Act
        var msg = new ToolErrorMessage("tool_call_456", "get_weather", "Connection timeout");

        // Assert
        Assert.IsTrue(msg.Content.Contains("get_weather"));
        Assert.IsTrue(msg.Content.Contains("Connection timeout"));
    }

    [TestMethod]
    public void DefaultsToInContext()
    {
        // Arrange & Act
        var msg = new ToolErrorMessage("tool_call_456", "tool", "error");

        // Assert
        Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
    }

    [TestMethod]
    public void InheritsFromToolMessage()
    {
        // Arrange & Act
        var msg = new ToolErrorMessage("tool_call_456", "tool", "error");

        // Assert
        Assert.IsInstanceOfType(msg, typeof(ToolMessage));
    }

    [TestMethod]
    public void MessageTypeDiscriminator_ReturnsCorrectValue()
    {
        // Arrange & Act
        var msg = new ToolErrorMessage("tool_call_456", "tool", "error");

        // Assert
        Assert.AreEqual("Tool.Error", msg.MessageTypeDiscriminator);
    }

    [TestMethod]
    public void Content_FormatsErrorMessageCorrectly()
    {
        // Arrange & Act
        var msg = new ToolErrorMessage("tool_call_456", "my_tool", "Something went wrong");

        // Assert
        Assert.IsTrue(msg.Content.Contains("Error executing"));
        Assert.IsTrue(msg.Content.Contains("my_tool"));
        Assert.IsTrue(msg.Content.Contains("Something went wrong"));
    }
}
