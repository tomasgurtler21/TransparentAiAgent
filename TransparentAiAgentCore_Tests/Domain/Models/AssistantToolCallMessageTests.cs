using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

/// <summary>
/// Tests for AssistantToolCallMessage - derived class that includes tool call requests
/// Following Lean TDD: Test meaningful behavior (validation, inheritance, immutability)
/// </summary>
[TestClass]
public class AssistantToolCallMessageTests
{
    #region Constructor Validation Tests

    [TestMethod]
    public void AssistantToolCallMessage_NullToolCalls_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new AssistantToolCallMessage("content", null!));

        Assert.IsTrue(exception.Message.Contains("at least one tool call"));
        Assert.AreEqual("toolCalls", exception.ParamName);
    }

    [TestMethod]
    public void AssistantToolCallMessage_EmptyToolCallsList_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new AssistantToolCallMessage("content", new List<ToolCall>()));

        Assert.IsTrue(exception.Message.Contains("at least one tool call"));
        Assert.AreEqual("toolCalls", exception.ParamName);
    }

    #endregion

    #region Inheritance Tests

    [TestMethod]
    public void AssistantToolCallMessage_InheritsFromAssistantMessage()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage("Test content", toolCalls);

        // Assert - verify it IS an AssistantMessage
        Assert.IsInstanceOfType(message, typeof(AssistantMessage));
    }

    [TestMethod]
    public void AssistantToolCallMessage_HasAssistantRole()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage("Test content", toolCalls);

        // Assert
        Assert.AreEqual(MessageRole.Assistant, message.Role);
    }

    [TestMethod]
    public void AssistantToolCallMessage_GeneratesUniqueId()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message1 = new AssistantToolCallMessage("Test", toolCalls);
        var message2 = new AssistantToolCallMessage("Test", toolCalls);

        // Assert
        Assert.AreNotEqual(Guid.Empty, message1.Id);
        Assert.AreNotEqual(message1.Id, message2.Id);
    }

    [TestMethod]
    public void AssistantToolCallMessage_SetsTimestamp()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };
        var beforeCreation = DateTime.UtcNow;

        // Act
        var message = new AssistantToolCallMessage("Test", toolCalls);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.IsTrue(message.Timestamp >= beforeCreation);
        Assert.IsTrue(message.Timestamp <= afterCreation);
    }

    [TestMethod]
    public void AssistantToolCallMessage_DefaultsToInContext()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage("Test", toolCalls);

        // Assert
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    #endregion

    #region Content Handling Tests

    [TestMethod]
    public void AssistantToolCallMessage_ValidContent_PreservesContent()
    {
        // Arrange
        var content = "I'll call the tool for you";
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage(content, toolCalls);

        // Assert
        Assert.AreEqual(content, message.Content);
    }

    [TestMethod]
    public void AssistantToolCallMessage_EmptyContent_AllowedWithToolCalls()
    {
        // Arrange - LLM can request tools without explanation text
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage(string.Empty, toolCalls);

        // Assert
        Assert.AreEqual(string.Empty, message.Content);
    }

    [TestMethod]
    public void AssistantToolCallMessage_NullContent_ConvertsToEmptyString()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage(null!, toolCalls);

        // Assert
        Assert.AreEqual(string.Empty, message.Content);
    }

    #endregion

    #region ToolCalls Property Tests

    [TestMethod]
    public void AssistantToolCallMessage_SingleToolCall_PreservesToolCall()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "add_todo", "{\"task\":\"Test\"}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage("Calling tool", toolCalls);

        // Assert
        Assert.AreEqual(1, message.ToolCalls.Count);
        Assert.AreEqual("call-123", message.ToolCalls[0].Id);
        Assert.AreEqual("add_todo", message.ToolCalls[0].Name);
        Assert.AreEqual("{\"task\":\"Test\"}", message.ToolCalls[0].Arguments);
    }

    [TestMethod]
    public void AssistantToolCallMessage_MultipleToolCalls_PreservesAllToolCalls()
    {
        // Arrange
        var toolCall1 = new ToolCall("call-1", "tool_a", "{\"arg\":\"1\"}");
        var toolCall2 = new ToolCall("call-2", "tool_b", "{\"arg\":\"2\"}");
        var toolCall3 = new ToolCall("call-3", "tool_c", "{\"arg\":\"3\"}");
        var toolCalls = new List<ToolCall> { toolCall1, toolCall2, toolCall3 };

        // Act
        var message = new AssistantToolCallMessage("Calling multiple tools", toolCalls);

        // Assert
        Assert.AreEqual(3, message.ToolCalls.Count);
        Assert.AreEqual("call-1", message.ToolCalls[0].Id);
        Assert.AreEqual("call-2", message.ToolCalls[1].Id);
        Assert.AreEqual("call-3", message.ToolCalls[2].Id);
    }

    [TestMethod]
    public void AssistantToolCallMessage_ToolCallsProperty_IsReadOnly()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };

        // Act
        var message = new AssistantToolCallMessage("Test", toolCalls);

        // Assert - ToolCalls should be IReadOnlyList
        Assert.IsInstanceOfType(message.ToolCalls, typeof(IReadOnlyList<ToolCall>));

        // Attempting to modify would cause compilation error:
        // message.ToolCalls.Add(...) - doesn't compile
        // message.ToolCalls = new List<ToolCall>() - doesn't compile
    }

    [TestMethod]
    public void AssistantToolCallMessage_ModifyingSourceList_DoesNotAffectMessage()
    {
        // Arrange
        var toolCall1 = new ToolCall("call-1", "tool_a", "{}");
        var sourceList = new List<ToolCall> { toolCall1 };

        // Act
        var message = new AssistantToolCallMessage("Test", sourceList);

        // Modify source list after message creation
        var toolCall2 = new ToolCall("call-2", "tool_b", "{}");
        sourceList.Add(toolCall2);

        // Assert - message should still have only 1 tool call (defensive copy)
        Assert.AreEqual(1, message.ToolCalls.Count);
        Assert.AreEqual("call-1", message.ToolCalls[0].Id);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void AssistantToolCallMessage_CanBeUsedAsAssistantMessage()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "tool_name", "{}");
        var toolCalls = new List<ToolCall> { toolCall };
        AssistantMessage message = new AssistantToolCallMessage("Test", toolCalls);

        // Act - use as base class
        var role = message.Role;
        var content = message.Content;
        var id = message.Id;

        // Assert - polymorphism works
        Assert.AreEqual(MessageRole.Assistant, role);
        Assert.AreEqual("Test", content);
        Assert.AreNotEqual(Guid.Empty, id);
    }

    [TestMethod]
    public void AssistantToolCallMessage_CanBeCastBackToDerivedType()
    {
        // Arrange
        var toolCall = new ToolCall("call-123", "add_todo", "{\"task\":\"Test\"}");
        var toolCalls = new List<ToolCall> { toolCall };
        AssistantMessage baseMessage = new AssistantToolCallMessage("Test", toolCalls);

        // Act - cast back to derived type
        var derivedMessage = baseMessage as AssistantToolCallMessage;

        // Assert
        Assert.IsNotNull(derivedMessage);
        Assert.AreEqual(1, derivedMessage.ToolCalls.Count);
        Assert.AreEqual("add_todo", derivedMessage.ToolCalls[0].Name);
    }

    #endregion
}
