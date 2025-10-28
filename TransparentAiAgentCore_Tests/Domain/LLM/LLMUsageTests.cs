using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore_Tests.Domain.LLM;

[TestClass]
public class LLMUsageTests
{
    [TestMethod]
    public void LLMUsage_NegativePromptTokens_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMUsage(-1, 100));
    }

    [TestMethod]
    public void LLMUsage_NegativeCompletionTokens_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LLMUsage(100, -1));
    }

    [TestMethod]
    public void LLMUsage_ValidTokens_CalculatesTotalCorrectly()
    {
        // Arrange & Act
        var usage = new LLMUsage(150, 50);

        // Assert
        Assert.AreEqual(150, usage.PromptTokens);
        Assert.AreEqual(50, usage.CompletionTokens);
        Assert.AreEqual(200, usage.TotalTokens);
    }

    [TestMethod]
    public void LLMUsage_ZeroTokens_IsValid()
    {
        // Arrange & Act
        var usage = new LLMUsage(0, 0);

        // Assert
        Assert.AreEqual(0, usage.PromptTokens);
        Assert.AreEqual(0, usage.CompletionTokens);
        Assert.AreEqual(0, usage.TotalTokens);
    }
}
