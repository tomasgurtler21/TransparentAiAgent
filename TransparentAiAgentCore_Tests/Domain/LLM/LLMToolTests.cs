using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore_Tests.Domain.LLM;

[TestClass]
public class LLMToolTests
{
    [TestMethod]
    public void LLMTool_NullName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMTool(null!, "description", "{}"));
    }

    [TestMethod]
    public void LLMTool_EmptyName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMTool("", "description", "{}"));
    }

    [TestMethod]
    public void LLMTool_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMTool("   ", "description", "{}"));
    }

    [TestMethod]
    public void LLMTool_NullDescription_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMTool("tool_name", null!, "{}"));
    }

    [TestMethod]
    public void LLMTool_EmptyDescription_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMTool("tool_name", "", "{}"));
    }

    [TestMethod]
    public void LLMTool_WhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMTool("tool_name", "   ", "{}"));
    }

    [TestMethod]
    public void LLMTool_NullParametersSchema_DefaultsToEmptyJson()
    {
        // Arrange & Act
        var tool = new LLMTool("get_weather", "Get weather for a city", null!);

        // Assert
        Assert.AreEqual("get_weather", tool.Name);
        Assert.AreEqual("Get weather for a city", tool.Description);
        Assert.AreEqual("{}", tool.ParametersSchema);
    }

    [TestMethod]
    public void LLMTool_ValidParameters_CreatesTool()
    {
        // Arrange
        var schema = "{\"type\":\"object\",\"properties\":{\"city\":{\"type\":\"string\"}}}";

        // Act
        var tool = new LLMTool("get_weather", "Get weather for a city", schema);

        // Assert
        Assert.AreEqual("get_weather", tool.Name);
        Assert.AreEqual("Get weather for a city", tool.Description);
        Assert.AreEqual(schema, tool.ParametersSchema);
    }

    [TestMethod]
    public void LLMTool_EmptySchema_PreservesEmptyString()
    {
        // Arrange & Act
        var tool = new LLMTool("simple_tool", "A simple tool", "");

        // Assert
        Assert.AreEqual("simple_tool", tool.Name);
        Assert.AreEqual("A simple tool", tool.Description);
        Assert.AreEqual("", tool.ParametersSchema);
    }
}
