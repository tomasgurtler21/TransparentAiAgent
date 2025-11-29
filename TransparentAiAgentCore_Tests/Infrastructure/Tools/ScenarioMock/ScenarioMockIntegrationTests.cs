using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.ScenarioMock;
using Microsoft.Extensions.Logging;
using Moq;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.ScenarioMock;

/// <summary>
/// Integration tests for the complete mock tools workflow: register → execute → unregister.
/// Testing the interaction between ScenarioToolRegistry, ScenarioMockTool, and ScenarioMockToolExecutor.
/// </summary>
[TestClass]
public class ScenarioMockIntegrationTests
{
    private ScenarioToolRegistry _registry = null!;
    private ScenarioMockToolExecutor _executor = null!;
    private Mock<ILogger<ScenarioToolRegistry>> _mockRegistryLogger = null!;
    private Mock<ILogger<ScenarioMockToolExecutor>> _mockExecutorLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockRegistryLogger = new Mock<ILogger<ScenarioToolRegistry>>();
        _mockExecutorLogger = new Mock<ILogger<ScenarioMockToolExecutor>>();
        _registry = new ScenarioToolRegistry(_mockRegistryLogger.Object);
        _executor = new ScenarioMockToolExecutor(_registry, _mockExecutorLogger.Object);
    }

    #region Integration Workflow Tests

    [TestMethod]
    public async Task FullWorkflow_RegisterExecuteUnregister_WorksCorrectly()
    {
        // Phase 1: Register
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"test.txt\"}", MockToolResponse.Success("File contents: Hello World!") },
            { "{\"fileName\":\"error.txt\"}", MockToolResponse.Error("File not found") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Verify registration
        var tool = _registry.GetMockTool("read_file");
        Assert.IsNotNull(tool, "Tool should be registered");

        // Phase 2: Execute with success response
        var result1 = await _executor.ExecuteAsync(tool, "{\"fileName\":\"test.txt\"}", CancellationToken.None);
        Assert.IsTrue(result1.IsSuccess, "First execution should succeed");
        Assert.AreEqual("File contents: Hello World!", result1.Content);

        // Phase 3: Execute with error response
        var result2 = await _executor.ExecuteAsync(tool, "{\"fileName\":\"error.txt\"}", CancellationToken.None);
        Assert.IsFalse(result2.IsSuccess, "Second execution should fail");
        Assert.AreEqual("File not found", result2.ErrorMessage);

        // Phase 4: Execute with whitespace difference (semantic JSON matching)
        var result3 = await _executor.ExecuteAsync(tool, "{\"fileName\": \"test.txt\"}", CancellationToken.None);
        Assert.IsTrue(result3.IsSuccess, "Should match despite whitespace difference");
        Assert.AreEqual("File contents: Hello World!", result3.Content);

        // Phase 5: Unregister
        _registry.UnregisterMockTool("read_file");
        var toolAfterUnregister = _registry.GetMockTool("read_file");
        Assert.IsNull(toolAfterUnregister, "Tool should be unregistered");
    }

    [TestMethod]
    public async Task MultipleTools_RegisteredConcurrently_AllExecuteCorrectly()
    {
        // Register multiple tools
        _registry.RegisterMockTool("tool1", "Tool 1", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{\"id\":1}", MockToolResponse.Success("Response from tool 1") }
        });

        _registry.RegisterMockTool("tool2", "Tool 2", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{\"id\":2}", MockToolResponse.Success("Response from tool 2") }
        });

        _registry.RegisterMockTool("tool3", "Tool 3", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{\"id\":3}", MockToolResponse.Success("Response from tool 3") }
        });

        // Get all tools
        var tool1 = _registry.GetMockTool("tool1");
        var tool2 = _registry.GetMockTool("tool2");
        var tool3 = _registry.GetMockTool("tool3");

        Assert.IsNotNull(tool1);
        Assert.IsNotNull(tool2);
        Assert.IsNotNull(tool3);

        // Execute all tools concurrently
        var task1 = _executor.ExecuteAsync(tool1!, "{\"id\":1}", CancellationToken.None);
        var task2 = _executor.ExecuteAsync(tool2!, "{\"id\":2}", CancellationToken.None);
        var task3 = _executor.ExecuteAsync(tool3!, "{\"id\":3}", CancellationToken.None);

        var results = await Task.WhenAll(task1, task2, task3);

        // Verify all executions succeeded
        Assert.IsTrue(results[0].IsSuccess);
        Assert.AreEqual("Response from tool 1", results[0].Content);

        Assert.IsTrue(results[1].IsSuccess);
        Assert.AreEqual("Response from tool 2", results[1].Content);

        Assert.IsTrue(results[2].IsSuccess);
        Assert.AreEqual("Response from tool 3", results[2].Content);
    }

    [TestMethod]
    public async Task JsonMatching_RealWorldScenario_MatchesCorrectly()
    {
        // This test simulates the real-world scenario from why-tool-visibility-matters.json
        // where the LLM might serialize JSON with different formatting than the scenario keys

        // Arrange - Register tool with keys having no whitespace (like in scenario file)
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"FileA\"}", MockToolResponse.Error("{\"code\":-32603,\"message\":\"Internal error\"}") },
            { "{\"fileName\":\"ImprovedFile1\"}", MockToolResponse.Error("File not found: ImprovedFile1.") },
            { "{\"fileName\":\"AbsolutelyCorrectFile\"}", MockToolResponse.Success("File contents: Hello from the correct file!") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        var tool = _registry.GetMockTool("read_file");
        Assert.IsNotNull(tool);

        // Act & Assert - Test with various JSON formatting variations that LLM might produce

        // Variation 1: Space after colon
        var result1 = await _executor.ExecuteAsync(tool, "{\"fileName\": \"FileA\"}", CancellationToken.None);
        Assert.IsFalse(result1.IsSuccess);
        Assert.IsTrue(result1.ErrorMessage!.Contains("Internal error"));

        // Variation 2: Space after colon and comma
        var result2 = await _executor.ExecuteAsync(tool, "{\"fileName\": \"ImprovedFile1\"}", CancellationToken.None);
        Assert.IsFalse(result2.IsSuccess);
        Assert.AreEqual("File not found: ImprovedFile1.", result2.ErrorMessage);

        // Variation 3: Exact match (no spaces)
        var result3 = await _executor.ExecuteAsync(tool, "{\"fileName\":\"AbsolutelyCorrectFile\"}", CancellationToken.None);
        Assert.IsTrue(result3.IsSuccess);
        Assert.AreEqual("File contents: Hello from the correct file!", result3.Content);

        // Variation 4: Space after colon (success case)
        var result4 = await _executor.ExecuteAsync(tool, "{\"fileName\": \"AbsolutelyCorrectFile\"}", CancellationToken.None);
        Assert.IsTrue(result4.IsSuccess);
        Assert.AreEqual("File contents: Hello from the correct file!", result4.Content);

        // Variation 5: Multiple spaces (edge case)
        var result5 = await _executor.ExecuteAsync(tool, "{\"fileName\":  \"FileA\"}", CancellationToken.None);
        Assert.IsFalse(result5.IsSuccess);
        Assert.IsTrue(result5.ErrorMessage!.Contains("Internal error"));
    }

    [TestMethod]
    public async Task ComplexJsonMatching_NestedObjects_MatchesCorrectly()
    {
        // Arrange - Complex nested JSON structures
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            {
                "{\"user\":{\"name\":\"John\",\"age\":30},\"action\":\"login\"}",
                MockToolResponse.Success("User John logged in")
            },
            {
                "{\"data\":{\"items\":[1,2,3],\"total\":3}}",
                MockToolResponse.Success("Data processed")
            }
        };
        _registry.RegisterMockTool("complex_tool", "Complex tool", "{}", responseMap);

        var tool = _registry.GetMockTool("complex_tool");
        Assert.IsNotNull(tool);

        // Act & Assert - Test with different property orders and whitespace

        // Test 1: Different property order in root
        var result1 = await _executor.ExecuteAsync(tool,
            "{\"action\": \"login\", \"user\": {\"name\": \"John\", \"age\": 30}}",
            CancellationToken.None);
        Assert.IsTrue(result1.IsSuccess);
        Assert.AreEqual("User John logged in", result1.Content);

        // Test 2: Different property order in nested object
        var result2 = await _executor.ExecuteAsync(tool,
            "{\"user\": {\"age\": 30, \"name\": \"John\"}, \"action\": \"login\"}",
            CancellationToken.None);
        Assert.IsTrue(result2.IsSuccess);
        Assert.AreEqual("User John logged in", result2.Content);

        // Test 3: Nested array with whitespace
        var result3 = await _executor.ExecuteAsync(tool,
            "{\"data\": {\"items\": [1, 2, 3], \"total\": 3}}",
            CancellationToken.None);
        Assert.IsTrue(result3.IsSuccess);
        Assert.AreEqual("Data processed", result3.Content);
    }

    #endregion
}
