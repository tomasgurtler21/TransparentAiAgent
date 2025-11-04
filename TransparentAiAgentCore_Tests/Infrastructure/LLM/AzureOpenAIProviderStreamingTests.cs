using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TransparentAiAgentCore.Infrastructure.LLM;

namespace TransparentAiAgentCore_Tests.Infrastructure.LLM;

/// <summary>
/// Tests for AzureStreamingToolCallAccumulator - the helper class that accumulates
/// streaming tool call updates from Azure OpenAI by index.
///
/// These tests verify the fix for Bug #1: Index-based vs ID-based accumulation.
///
/// KEY BUG: Azure OpenAI sends ToolCallId only in FIRST chunk, subsequent chunks
/// have ToolCallId=null (only Index populated). Old code matched by ID → lost arguments.
/// New code matches by Index → fixed.
/// </summary>
[TestClass]
public class AzureStreamingToolCallAccumulatorTests
{
    [TestMethod]
    public void AddToolCallUpdate_ToolCallArgumentsInMultipleChunks_AccumulatesCorrectly()
    {
        // RED PHASE: Write test that simulates realistic Azure OpenAI streaming behavior
        // This test simulates the exact bug scenario from the production issue:
        // - Chunk 1: Index=0, ID="call_123", Name="todo_create", Arguments=""
        // - Chunk 2: Index=0, ID=null, Name=null, Arguments="{\"items\":"
        // - Chunk 3: Index=0, ID=null, Name=null, Arguments="[{\"title\":"
        // - Chunk 4: Index=0, ID=null, Name=null, Arguments="\"test\"}]}"
        //
        // Expected with CORRECT implementation: Arguments = "{\"items\":[{\"title\":\"test\"}]}"
        // Expected with BUGGY (ID-based) implementation: Arguments = "" (lost chunks 2-4)

        // Arrange
        var accumulator = new AzureStreamingToolCallAccumulator();

        // Act - Simulate Azure OpenAI streaming chunks
        // Chunk 1: First chunk with ID and Name
        accumulator.AddToolCallUpdate(
            index: 0,
            toolCallId: "call_123",
            functionName: "todo_create",
            argumentsUpdate: "");

        // Chunk 2: ID=null, Name=null, only arguments delta (THIS IS THE KEY!)
        accumulator.AddToolCallUpdate(
            index: 0,
            toolCallId: null,
            functionName: null,
            argumentsUpdate: "{\"items\":");

        // Chunk 3: ID=null, Name=null, more arguments
        accumulator.AddToolCallUpdate(
            index: 0,
            toolCallId: null,
            functionName: null,
            argumentsUpdate: "[{\"title\":");

        // Chunk 4: ID=null, Name=null, final arguments
        accumulator.AddToolCallUpdate(
            index: 0,
            toolCallId: null,
            functionName: null,
            argumentsUpdate: "\"test\"}]}");

        var result = accumulator.GetAccumulatedToolCalls();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("call_123", result[0].Id);
        Assert.AreEqual("todo_create", result[0].Name);
        Assert.AreEqual("{\"items\":[{\"title\":\"test\"}]}", result[0].Arguments);
    }

    [TestMethod]
    public void AddToolCallUpdate_MultipleToolCallsByIndex_AccumulatesEachSeparately()
    {
        // RED PHASE: Test two tool calls with different indices in same response

        // Arrange
        var accumulator = new AzureStreamingToolCallAccumulator();

        // Act - Simulate two tool calls streaming
        // Tool 1 (Index=0): First chunk
        accumulator.AddToolCallUpdate(0, "call_1", "tool1", "{\"a\"");

        // Tool 2 (Index=1): First chunk
        accumulator.AddToolCallUpdate(1, "call_2", "tool2", "{\"b\"");

        // Tool 1 (Index=0): Second chunk (ID=null, Name=null)
        accumulator.AddToolCallUpdate(0, null, null, ":1}");

        // Tool 2 (Index=1): Second chunk (ID=null, Name=null)
        accumulator.AddToolCallUpdate(1, null, null, ":2}");

        var result = accumulator.GetAccumulatedToolCalls();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);

        var tool1 = result.FirstOrDefault(tc => tc.Id == "call_1");
        var tool2 = result.FirstOrDefault(tc => tc.Id == "call_2");

        Assert.IsNotNull(tool1);
        Assert.AreEqual("tool1", tool1.Name);
        Assert.AreEqual("{\"a\":1}", tool1.Arguments);

        Assert.IsNotNull(tool2);
        Assert.AreEqual("tool2", tool2.Name);
        Assert.AreEqual("{\"b\":2}", tool2.Arguments);
    }

    [TestMethod]
    public void AddToolCallUpdate_IDBeforeNameInChunks_AccumulatesCorrectly()
    {
        // RED PHASE: Test edge case where ID arrives before Name in separate chunks

        // Arrange
        var accumulator = new AzureStreamingToolCallAccumulator();

        // Act - Simulate Azure sometimes sending ID before Name
        // Chunk 1: Index=0, ID="call_123", Name="", Arguments=""
        accumulator.AddToolCallUpdate(0, "call_123", "", "");

        // Chunk 2: Index=0, ID=null, Name="todo_create", Arguments=""
        accumulator.AddToolCallUpdate(0, null, "todo_create", "");

        // Chunk 3: Index=0, ID=null, Name=null, Arguments="{\"data\":\"test\"}"
        accumulator.AddToolCallUpdate(0, null, null, "{\"data\":\"test\"}");

        var result = accumulator.GetAccumulatedToolCalls();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("call_123", result[0].Id);
        Assert.AreEqual("todo_create", result[0].Name);
        Assert.AreEqual("{\"data\":\"test\"}", result[0].Arguments);
    }
}
