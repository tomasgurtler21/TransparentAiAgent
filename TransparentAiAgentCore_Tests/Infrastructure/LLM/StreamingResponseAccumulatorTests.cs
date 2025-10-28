using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Infrastructure.LLM;

namespace TransparentAiAgentCore_Tests.Infrastructure.LLM;

[TestClass]
public class StreamingResponseAccumulatorTests
{
    [TestMethod]
    public void AddChunk_NullChunk_ThrowsArgumentNullException()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => accumulator.AddChunk(null!));
    }

    [TestMethod]
    public void AddChunk_ContentDelta_AccumulatesCorrectly()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        var chunk1 = new StreamingLLMChunk("Hello");
        var chunk2 = new StreamingLLMChunk(" world");
        var chunk3 = new StreamingLLMChunk("!");

        // Act
        accumulator.AddChunk(chunk1);
        accumulator.AddChunk(chunk2);
        accumulator.AddChunk(chunk3);
        var response = accumulator.ToResponse();

        // Assert
        Assert.AreEqual("Hello world!", response.Content);
    }

    [TestMethod]
    public void AddChunk_EmptyContentDelta_HandlesCorrectly()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        var chunk1 = new StreamingLLMChunk("Hello");
        var chunk2 = new StreamingLLMChunk("");
        var chunk3 = new StreamingLLMChunk("world");

        // Act
        accumulator.AddChunk(chunk1);
        accumulator.AddChunk(chunk2);
        accumulator.AddChunk(chunk3);
        var response = accumulator.ToResponse();

        // Assert
        Assert.AreEqual("Helloworld", response.Content);
    }

    [TestMethod]
    public void AddChunk_ToolCallDeltas_AccumulatesArguments()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        var chunk1 = new StreamingLLMChunk("", new LLMToolCall("call-1", "get_weather", "{\"ci"));
        var chunk2 = new StreamingLLMChunk("", new LLMToolCall("call-1", "get_weather", "ty\":\""));
        var chunk3 = new StreamingLLMChunk("", new LLMToolCall("call-1", "get_weather", "Prague\"}"));

        // Act
        accumulator.AddChunk(chunk1);
        accumulator.AddChunk(chunk2);
        accumulator.AddChunk(chunk3);
        var response = accumulator.ToResponse();

        // Assert
        Assert.IsNotNull(response.ToolCalls);
        Assert.AreEqual(1, response.ToolCalls.Count);
        Assert.AreEqual("{\"city\":\"Prague\"}", response.ToolCalls[0].Arguments);
    }

    [TestMethod]
    public void AddChunk_MultipleToolCalls_AccumulatesSeparately()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        var chunk1 = new StreamingLLMChunk("", new LLMToolCall("call-1", "tool1", "arg1"));
        var chunk2 = new StreamingLLMChunk("", new LLMToolCall("call-2", "tool2", "arg2"));
        var chunk3 = new StreamingLLMChunk("", new LLMToolCall("call-1", "tool1", "-more"));

        // Act
        accumulator.AddChunk(chunk1);
        accumulator.AddChunk(chunk2);
        accumulator.AddChunk(chunk3);
        var response = accumulator.ToResponse();

        // Assert
        Assert.IsNotNull(response.ToolCalls);
        Assert.AreEqual(2, response.ToolCalls.Count);

        var tool1 = response.ToolCalls.Find(tc => tc.Id == "call-1");
        var tool2 = response.ToolCalls.Find(tc => tc.Id == "call-2");

        Assert.IsNotNull(tool1);
        Assert.IsNotNull(tool2);
        Assert.AreEqual("arg1-more", tool1.Arguments);
        Assert.AreEqual("arg2", tool2.Arguments);
    }

    [TestMethod]
    public void AddChunk_FinishReason_TracksCorrectly()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        var chunk1 = new StreamingLLMChunk("Hello");
        var chunk2 = new StreamingLLMChunk("", isComplete: true, finishReason: "stop");

        // Act
        accumulator.AddChunk(chunk1);
        accumulator.AddChunk(chunk2);
        var response = accumulator.ToResponse();

        // Assert
        Assert.AreEqual("stop", response.FinishReason);
    }

    [TestMethod]
    public void ToResponse_NoContent_ThrowsArgumentException()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => accumulator.ToResponse());
    }

    [TestMethod]
    public void ToResponse_NoToolCalls_ReturnsNull()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        accumulator.AddChunk(new StreamingLLMChunk("Hello"));

        // Act
        var response = accumulator.ToResponse();

        // Assert
        Assert.IsNull(response.ToolCalls);
    }

    [TestMethod]
    public void ToResponse_UsageIsNull()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        accumulator.AddChunk(new StreamingLLMChunk("Hello"));

        // Act
        var response = accumulator.ToResponse();

        // Assert
        Assert.IsNull(response.Usage);
    }

    [TestMethod]
    public void Reset_ClearsAllAccumulatedData()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        accumulator.AddChunk(new StreamingLLMChunk("Hello"));
        accumulator.AddChunk(new StreamingLLMChunk("", new LLMToolCall("call-1", "tool", "args")));
        accumulator.AddChunk(new StreamingLLMChunk("", isComplete: true, finishReason: "stop"));

        // Act
        accumulator.Reset();

        // Assert - After reset, trying to get response with no data should throw
        Assert.ThrowsException<ArgumentException>(() => accumulator.ToResponse());
    }

    [TestMethod]
    public void Reset_AllowsReuse()
    {
        // Arrange
        var accumulator = new StreamingResponseAccumulator();
        accumulator.AddChunk(new StreamingLLMChunk("First"));
        accumulator.Reset();

        // Act
        accumulator.AddChunk(new StreamingLLMChunk("Second"));
        var response = accumulator.ToResponse();

        // Assert
        Assert.AreEqual("Second", response.Content);
    }
}
