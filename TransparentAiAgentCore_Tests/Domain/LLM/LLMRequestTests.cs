using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore_Tests.Domain.LLM;

[TestClass]
public class LLMRequestTests
{
    [TestMethod]
    public void LLMRequest_NullMessages_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMRequest(null!));
    }

    [TestMethod]
    public void LLMRequest_EmptyMessages_ThrowsArgumentException()
    {
        // Arrange
        var messages = new List<LLMMessage>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMRequest(messages));
    }

    [TestMethod]
    public void LLMRequest_TemperatureBelowZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMRequest(messages, temperature: -0.1));
    }

    [TestMethod]
    public void LLMRequest_TemperatureAboveTwo_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMRequest(messages, temperature: 2.1));
    }

    [TestMethod]
    public void LLMRequest_TopPBelowZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMRequest(messages, topP: -0.1));
    }

    [TestMethod]
    public void LLMRequest_TopPAboveOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMRequest(messages, topP: 1.1));
    }

    [TestMethod]
    public void LLMRequest_MaxTokensZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMRequest(messages, maxTokens: 0));
    }

    [TestMethod]
    public void LLMRequest_MaxTokensNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMRequest(messages, maxTokens: -100));
    }

    [TestMethod]
    public void LLMRequest_ValidParametersWithDefaults_CreatesRequest()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act
        var request = new LLMRequest(messages);

        // Assert
        Assert.IsNotNull(request.Messages);
        Assert.AreEqual(1, request.Messages.Count);
        Assert.AreEqual(0.7, request.Temperature);
        Assert.AreEqual(1.0, request.TopP);
        Assert.AreEqual(4096, request.MaxTokens);
        Assert.IsFalse(request.Stream);
        Assert.IsNull(request.Tools);
    }

    [TestMethod]
    public void LLMRequest_ValidParametersWithCustomValues_CreatesRequest()
    {
        // Arrange
        var messages = new List<LLMMessage>
        {
            new LLMMessage("user", "Hello"),
            new LLMMessage("assistant", "Hi there!")
        };
        var tools = new List<LLMTool>
        {
            new LLMTool("get_weather", "Get weather", "{}")
        };

        // Act
        var request = new LLMRequest(
            messages: messages,
            temperature: 0.5,
            topP: 0.9,
            maxTokens: 2000,
            stream: true,
            tools: tools);

        // Assert
        Assert.IsNotNull(request.Messages);
        Assert.AreEqual(2, request.Messages.Count);
        Assert.AreEqual(0.5, request.Temperature);
        Assert.AreEqual(0.9, request.TopP);
        Assert.AreEqual(2000, request.MaxTokens);
        Assert.IsTrue(request.Stream);
        Assert.IsNotNull(request.Tools);
        Assert.AreEqual(1, request.Tools.Count);
    }

    [TestMethod]
    public void LLMRequest_TemperatureAtBoundaries_IsValid()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert - Temperature at 0
        var request1 = new LLMRequest(messages, temperature: 0.0);
        Assert.AreEqual(0.0, request1.Temperature);

        // Act & Assert - Temperature at 2
        var request2 = new LLMRequest(messages, temperature: 2.0);
        Assert.AreEqual(2.0, request2.Temperature);
    }

    [TestMethod]
    public void LLMRequest_TopPAtBoundaries_IsValid()
    {
        // Arrange
        var messages = new List<LLMMessage> { new LLMMessage("user", "Hello") };

        // Act & Assert - TopP at 0
        var request1 = new LLMRequest(messages, topP: 0.0);
        Assert.AreEqual(0.0, request1.TopP);

        // Act & Assert - TopP at 1
        var request2 = new LLMRequest(messages, topP: 1.0);
        Assert.AreEqual(1.0, request2.TopP);
    }
}
