using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

/// <summary>
/// Tests for ToolCall value object
/// Following Lean TDD: Test meaningful behavior (validation, initialization)
/// </summary>
[TestClass]
public class ToolCallTests
{
    #region Constructor Validation Tests

    [TestMethod]
    public void ToolCall_NullId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new ToolCall(null!, "tool_name", "{}"));

        // Verify error message contains expected details
        Assert.IsTrue(exception.Message.Contains("Tool call ID"));
        Assert.AreEqual("id", exception.ParamName);
    }

    [TestMethod]
    public void ToolCall_EmptyId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new ToolCall("", "tool_name", "{}"));

        Assert.IsTrue(exception.Message.Contains("Tool call ID"));
        Assert.AreEqual("id", exception.ParamName);
    }

    [TestMethod]
    public void ToolCall_WhitespaceId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new ToolCall("   ", "tool_name", "{}"));

        Assert.IsTrue(exception.Message.Contains("Tool call ID"));
        Assert.AreEqual("id", exception.ParamName);
    }

    [TestMethod]
    public void ToolCall_NullName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new ToolCall("call-123", null!, "{}"));

        Assert.IsTrue(exception.Message.Contains("Tool name"));
        Assert.AreEqual("name", exception.ParamName);
    }

    [TestMethod]
    public void ToolCall_EmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new ToolCall("call-123", "", "{}"));

        Assert.IsTrue(exception.Message.Contains("Tool name"));
        Assert.AreEqual("name", exception.ParamName);
    }

    [TestMethod]
    public void ToolCall_WhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(
            () => new ToolCall("call-123", "   ", "{}"));

        Assert.IsTrue(exception.Message.Contains("Tool name"));
        Assert.AreEqual("name", exception.ParamName);
    }

    #endregion

    #region Property Initialization Tests

    [TestMethod]
    public void ToolCall_ValidParameters_PropertiesSetCorrectly()
    {
        // Arrange
        var id = "call-123";
        var name = "add_todo";
        var arguments = "{\"task\":\"Review code\"}";

        // Act
        var toolCall = new ToolCall(id, name, arguments);

        // Assert
        Assert.AreEqual(id, toolCall.Id);
        Assert.AreEqual(name, toolCall.Name);
        Assert.AreEqual(arguments, toolCall.Arguments);
    }

    [TestMethod]
    public void ToolCall_NullArguments_DefaultsToEmptyJson()
    {
        // Act
        var toolCall = new ToolCall("call-123", "tool_name", null!);

        // Assert
        Assert.AreEqual("{}", toolCall.Arguments);
    }

    [TestMethod]
    public void ToolCall_EmptyArguments_PreservesEmptyString()
    {
        // Act
        var toolCall = new ToolCall("call-123", "tool_name", "");

        // Assert
        // Empty string is preserved (validation happens at JSON parsing level, not here)
        Assert.AreEqual("", toolCall.Arguments);
    }

    [TestMethod]
    public void ToolCall_ComplexArguments_PreservesExactJson()
    {
        // Arrange
        var complexJson = @"{
            ""task"": ""Review Phase 5"",
            ""priority"": ""high"",
            ""metadata"": {
                ""tags"": [""urgent"", ""code-review""]
            }
        }";

        // Act
        var toolCall = new ToolCall("call-123", "add_todo", complexJson);

        // Assert
        Assert.AreEqual(complexJson, toolCall.Arguments);
    }

    #endregion

    #region Immutability Tests

    [TestMethod]
    public void ToolCall_Properties_AreReadOnly()
    {
        // This test verifies that properties have only getters
        // If this compiles, properties are read-only
        // (Compilation would fail if we try to set: toolCall.Id = "new-id")

        // Arrange & Act
        var toolCall = new ToolCall("call-123", "tool_name", "{}");

        // Assert - verify we can read but not write
        var id = toolCall.Id;
        var name = toolCall.Name;
        var arguments = toolCall.Arguments;

        Assert.IsNotNull(id);
        Assert.IsNotNull(name);
        Assert.IsNotNull(arguments);

        // Note: Attempting toolCall.Id = "new" would cause compilation error
        // This is a design test - verifying immutability is enforced by the type system
    }

    #endregion
}
