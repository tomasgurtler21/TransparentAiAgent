using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore_Tests.Domain.LLM;

[TestClass]
public class LLMMessageTests
{
    #region Regular Message Constructor Tests

    [TestMethod]
    public void LLMMessage_NullRole_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage(null!, "content"));
    }

    [TestMethod]
    public void LLMMessage_EmptyRole_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("", "content"));
    }

    [TestMethod]
    public void LLMMessage_WhitespaceRole_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("   ", "content"));
    }

    [TestMethod]
    public void LLMMessage_NullContent_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("user", null!));
    }

    [TestMethod]
    public void LLMMessage_UserRole_EmptyContent_ThrowsArgumentException()
    {
        // Arrange, Act & Assert - User messages require content
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("user", ""));
    }

    [TestMethod]
    public void LLMMessage_UserRole_WhitespaceContent_ThrowsArgumentException()
    {
        // Arrange, Act & Assert - User messages require content
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("user", "   "));
    }

    [TestMethod]
    public void LLMMessage_SystemRole_EmptyContent_ThrowsArgumentException()
    {
        // Arrange, Act & Assert - System messages require content
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("system", ""));
    }

    [TestMethod]
    public void LLMMessage_SystemRole_WhitespaceContent_ThrowsArgumentException()
    {
        // Arrange, Act & Assert - System messages require content
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("system", "   "));
    }

    [TestMethod]
    public void LLMMessage_AssistantRole_EmptyContent_AllowsEmptyString()
    {
        // Arrange, Act - Assistant messages can have empty content (tool-call-only responses)
        var message = new LLMMessage("assistant", "");

        // Assert
        Assert.AreEqual("assistant", message.Role);
        Assert.AreEqual("", message.Content);
    }

    [TestMethod]
    public void LLMMessage_AssistantRole_WhitespaceContent_AllowsWhitespace()
    {
        // Arrange, Act - Assistant messages can have whitespace content
        var message = new LLMMessage("assistant", "   ");

        // Assert
        Assert.AreEqual("assistant", message.Role);
        Assert.AreEqual("   ", message.Content);
    }

    [TestMethod]
    public void LLMMessage_ValidRoleAndContent_CreatesMessage()
    {
        // Arrange & Act
        var message = new LLMMessage("user", "Hello");

        // Assert
        Assert.AreEqual("user", message.Role);
        Assert.AreEqual("Hello", message.Content);
        Assert.IsNull(message.ToolCalls);
        Assert.IsNull(message.ToolCallId);
    }

    #endregion

    #region Tool Call Constructor Tests

    [TestMethod]
    public void LLMMessage_WithToolCalls_NullRole_ThrowsArgumentException()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage(null!, "content", toolCalls));
    }

    [TestMethod]
    public void LLMMessage_WithToolCalls_EmptyRole_ThrowsArgumentException()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("", "content", toolCalls));
    }

    [TestMethod]
    public void LLMMessage_WithToolCalls_NullToolCalls_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new LLMMessage("assistant", "content", (List<LLMToolCall>)null!));
    }

    [TestMethod]
    public void LLMMessage_WithToolCalls_EmptyContent_AllowsEmptyString()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>();

        // Act
        var message = new LLMMessage("assistant", "", toolCalls);

        // Assert
        Assert.AreEqual("assistant", message.Role);
        Assert.AreEqual("", message.Content);
        Assert.IsNotNull(message.ToolCalls);
        Assert.AreEqual(0, message.ToolCalls.Count);
    }

    [TestMethod]
    public void LLMMessage_WithToolCalls_ValidParameters_CreatesMessage()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>
        {
            new LLMToolCall("call-1", "get_weather", "{\"city\":\"Prague\"}")
        };

        // Act
        var message = new LLMMessage("assistant", "Let me check the weather", toolCalls);

        // Assert
        Assert.AreEqual("assistant", message.Role);
        Assert.AreEqual("Let me check the weather", message.Content);
        Assert.IsNotNull(message.ToolCalls);
        Assert.AreEqual(1, message.ToolCalls.Count);
        Assert.IsNull(message.ToolCallId);
    }

    #endregion

    #region Tool Result Constructor Tests

    [TestMethod]
    public void LLMMessage_WithToolCallId_NullRole_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage(null!, "result", "call-1"));
    }

    [TestMethod]
    public void LLMMessage_WithToolCallId_EmptyRole_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("", "result", "call-1"));
    }

    [TestMethod]
    public void LLMMessage_WithToolCallId_NullContent_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("tool", null!, "call-1"));
    }

    [TestMethod]
    public void LLMMessage_WithToolCallId_EmptyContent_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("tool", "", "call-1"));
    }

    [TestMethod]
    public void LLMMessage_WithToolCallId_NullToolCallId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("tool", "result", (string)null!));
    }

    [TestMethod]
    public void LLMMessage_WithToolCallId_EmptyToolCallId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("tool", "result", ""));
    }

    [TestMethod]
    public void LLMMessage_WithToolCallId_WhitespaceToolCallId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMMessage("tool", "result", "   "));
    }

    [TestMethod]
    public void LLMMessage_WithToolCallId_ValidParameters_CreatesMessage()
    {
        // Arrange & Act
        var message = new LLMMessage("tool", "{\"temperature\":20}", "call-1");

        // Assert
        Assert.AreEqual("tool", message.Role);
        Assert.AreEqual("{\"temperature\":20}", message.Content);
        Assert.AreEqual("call-1", message.ToolCallId);
        Assert.IsNull(message.ToolCalls);
    }

    #endregion
}
