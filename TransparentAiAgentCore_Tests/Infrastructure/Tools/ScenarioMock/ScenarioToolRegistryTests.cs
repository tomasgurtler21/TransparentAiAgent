using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Infrastructure.Tools.ScenarioMock;
using Microsoft.Extensions.Logging;
using Moq;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.ScenarioMock;

/// <summary>
/// Tests for ScenarioToolRegistry following Lean TDD principles.
/// Testing meaningful behavior: registration, retrieval, mock response matching, and JSON semantic comparison.
/// </summary>
[TestClass]
public class ScenarioToolRegistryTests
{
    private Mock<ILogger<ScenarioToolRegistry>> _mockLogger = null!;
    private ScenarioToolRegistry _registry = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ScenarioToolRegistry>>();
        _registry = new ScenarioToolRegistry(_mockLogger.Object);
    }

    #region Helper Methods

    private ScenarioMockTool CreateTestTool(
        string name = "test_tool",
        Dictionary<string, MockToolResponse>? responseMap = null)
    {
        responseMap ??= new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("default response") }
        };

        return new ScenarioMockTool(
            name: name,
            description: "Test tool",
            parametersSchema: "{}",
            responseMap: responseMap);
    }

    #endregion

    #region JSON Matching Tests (Phase 1 - Critical Bug Fix Tests)

    [TestMethod]
    public void GetMockResponse_ExactStringMatch_ReturnsResponse()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"test.txt\"}", MockToolResponse.Success("File contents") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("read_file", "{\"fileName\":\"test.txt\"}");

        // Assert
        Assert.IsNotNull(result, "Exact string match should return response");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("File contents", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_DifferentWhitespace_ReturnsResponse()
    {
        // Arrange - Response map has no spaces
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"test.txt\"}", MockToolResponse.Success("File contents") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Act - Arguments have space after colon
        var result = _registry.GetMockResponse("read_file", "{\"fileName\": \"test.txt\"}");

        // Assert
        Assert.IsNotNull(result, "Different whitespace should still match via semantic JSON comparison");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("File contents", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_DifferentPropertyOrder_ReturnsResponse()
    {
        // Arrange - Response map has properties in one order
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"a\":1,\"b\":2}", MockToolResponse.Success("Success") }
        };
        _registry.RegisterMockTool("test_tool", "Test", "{}", responseMap);

        // Act - Arguments have properties in different order
        var result = _registry.GetMockResponse("test_tool", "{\"b\":2,\"a\":1}");

        // Assert
        Assert.IsNotNull(result, "Different property order should match via semantic JSON comparison");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Success", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_NestedObjectsMatch_ReturnsResponse()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"user\":{\"name\":\"John\",\"age\":30}}", MockToolResponse.Success("User found") }
        };
        _registry.RegisterMockTool("get_user", "Gets user", "{}", responseMap);

        // Act - Different whitespace in nested object
        var result = _registry.GetMockResponse("get_user", "{\"user\": {\"name\": \"John\", \"age\": 30}}");

        // Assert
        Assert.IsNotNull(result, "Nested objects should match");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("User found", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_ArraysMatch_ReturnsResponse()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"items\":[1,2,3]}", MockToolResponse.Success("Array processed") }
        };
        _registry.RegisterMockTool("process_array", "Processes array", "{}", responseMap);

        // Act - Different whitespace in array
        var result = _registry.GetMockResponse("process_array", "{\"items\": [1, 2, 3]}");

        // Assert
        Assert.IsNotNull(result, "Arrays should match");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Array processed", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_ArraysDifferentOrder_NoMatch()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"items\":[1,2,3]}", MockToolResponse.Success("Array processed") }
        };
        _registry.RegisterMockTool("process_array", "Processes array", "{}", responseMap);

        // Act - Array with different order (order matters in arrays)
        var result = _registry.GetMockResponse("process_array", "{\"items\":[3,2,1]}");

        // Assert
        Assert.IsNull(result, "Arrays with different order should NOT match (order matters)");
    }

    [TestMethod]
    public void GetMockResponse_NumbersMatch_ReturnsResponse()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"value\":42}", MockToolResponse.Success("Number matched") }
        };
        _registry.RegisterMockTool("check_number", "Checks number", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("check_number", "{\"value\": 42}");

        // Assert
        Assert.IsNotNull(result, "Numbers should match");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Number matched", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_BooleanTrueMatches_ReturnsResponse()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"enabled\":true}", MockToolResponse.Success("Enabled") }
        };
        _registry.RegisterMockTool("check_flag", "Checks flag", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("check_flag", "{\"enabled\": true}");

        // Assert
        Assert.IsNotNull(result, "Boolean true should match");
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void GetMockResponse_BooleanFalseMatches_ReturnsResponse()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"enabled\":false}", MockToolResponse.Success("Disabled") }
        };
        _registry.RegisterMockTool("check_flag", "Checks flag", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("check_flag", "{\"enabled\": false}");

        // Assert
        Assert.IsNotNull(result, "Boolean false should match");
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void GetMockResponse_NullValueMatches_ReturnsResponse()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"data\":null}", MockToolResponse.Success("Null data") }
        };
        _registry.RegisterMockTool("check_null", "Checks null", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("check_null", "{\"data\": null}");

        // Assert
        Assert.IsNotNull(result, "Null values should match");
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void GetMockResponse_InvalidJsonArguments_ReturnsNull()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"valid\":\"json\"}", MockToolResponse.Success("Success") }
        };
        _registry.RegisterMockTool("test_tool", "Test", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("test_tool", "{invalid json}");

        // Assert
        Assert.IsNull(result, "Invalid JSON arguments should return null");
    }

    [TestMethod]
    public void GetMockResponse_InvalidJsonInResponseMapKey_SkipsInvalidKey()
    {
        // Arrange - One valid key, one invalid key
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{invalid json}", MockToolResponse.Success("Should be skipped") },
            { "{\"valid\":\"json\"}", MockToolResponse.Success("Valid response") }
        };
        _registry.RegisterMockTool("test_tool", "Test", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("test_tool", "{\"valid\":\"json\"}");

        // Assert
        Assert.IsNotNull(result, "Should find valid key despite invalid key in map");
        Assert.AreEqual("Valid response", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_NoMatch_ReturnsNull()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"test.txt\"}", MockToolResponse.Success("File contents") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("read_file", "{\"fileName\":\"different.txt\"}");

        // Assert
        Assert.IsNull(result, "No matching response should return null");
    }

    [TestMethod]
    public void GetMockResponse_NullToolName_ReturnsNull()
    {
        // Act
        var result = _registry.GetMockResponse(null!, "{}");

        // Assert
        Assert.IsNull(result, "Null tool name should return null");
    }

    [TestMethod]
    public void GetMockResponse_EmptyToolName_ReturnsNull()
    {
        // Act
        var result = _registry.GetMockResponse(string.Empty, "{}");

        // Assert
        Assert.IsNull(result, "Empty tool name should return null");
    }

    [TestMethod]
    public void GetMockResponse_NullArguments_ReturnsNull()
    {
        // Arrange
        _registry.RegisterMockTool("test_tool", "Test", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        });

        // Act
        var result = _registry.GetMockResponse("test_tool", null!);

        // Assert
        Assert.IsNull(result, "Null arguments should return null");
    }

    [TestMethod]
    public void GetMockResponse_EmptyArguments_ReturnsNull()
    {
        // Arrange
        _registry.RegisterMockTool("test_tool", "Test", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        });

        // Act
        var result = _registry.GetMockResponse("test_tool", string.Empty);

        // Assert
        Assert.IsNull(result, "Empty arguments should return null");
    }

    [TestMethod]
    public void GetMockResponse_NonExistentTool_ReturnsNull()
    {
        // Act
        var result = _registry.GetMockResponse("non_existent_tool", "{}");

        // Assert
        Assert.IsNull(result, "Non-existent tool should return null");
    }

    #endregion

    #region Wildcard/Default Response Tests

    [TestMethod]
    public void GetMockResponse_NoMatchWithWildcard_ReturnsWildcardResponse()
    {
        // Arrange - Include wildcard default response
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"specific.txt\"}", MockToolResponse.Success("Specific file") },
            { "*", MockToolResponse.Error("File not found") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Act - Request a file that doesn't have a specific response
        var result = _registry.GetMockResponse("read_file", "{\"fileName\":\"unknown.txt\"}");

        // Assert
        Assert.IsNotNull(result, "Should return wildcard response when no specific match found");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("File not found", result.ErrorMessage);
    }

    [TestMethod]
    public void GetMockResponse_SpecificMatchWithWildcard_ReturnsSpecificResponse()
    {
        // Arrange - Include both specific and wildcard responses
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"specific.txt\"}", MockToolResponse.Success("Specific file contents") },
            { "*", MockToolResponse.Error("File not found") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Act - Request the specific file
        var result = _registry.GetMockResponse("read_file", "{\"fileName\":\"specific.txt\"}");

        // Assert - Should get specific response, not wildcard
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess, "Should return specific response, not wildcard");
        Assert.AreEqual("Specific file contents", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_SemanticMatchWithWildcard_ReturnsSemanticMatch()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"specific.txt\"}", MockToolResponse.Success("Specific file") },
            { "*", MockToolResponse.Error("File not found") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Act - Request with different whitespace (semantic match)
        var result = _registry.GetMockResponse("read_file", "{\"fileName\": \"specific.txt\"}");

        // Assert - Should match semantically, not fall back to wildcard
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess, "Should return semantic match, not wildcard");
        Assert.AreEqual("Specific file", result.Content);
    }

    [TestMethod]
    public void GetMockResponse_NoMatchNoWildcard_ReturnsNull()
    {
        // Arrange - No wildcard response
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"fileName\":\"specific.txt\"}", MockToolResponse.Success("Specific file") }
        };
        _registry.RegisterMockTool("read_file", "Reads a file", "{}", responseMap);

        // Act
        var result = _registry.GetMockResponse("read_file", "{\"fileName\":\"unknown.txt\"}");

        // Assert
        Assert.IsNull(result, "Should return null when no match and no wildcard");
    }

    [TestMethod]
    public void GetMockResponse_OnlyWildcard_ReturnsWildcardForAnyInput()
    {
        // Arrange - Only wildcard response (useful for simple mock tools)
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "*", MockToolResponse.Success("Default response for all inputs") }
        };
        _registry.RegisterMockTool("simple_tool", "Simple tool", "{}", responseMap);

        // Act & Assert - Any input returns wildcard
        var result1 = _registry.GetMockResponse("simple_tool", "{\"any\":\"input1\"}");
        Assert.IsNotNull(result1);
        Assert.AreEqual("Default response for all inputs", result1.Content);

        var result2 = _registry.GetMockResponse("simple_tool", "{\"different\":\"input2\"}");
        Assert.IsNotNull(result2);
        Assert.AreEqual("Default response for all inputs", result2.Content);

        var result3 = _registry.GetMockResponse("simple_tool", "{}");
        Assert.IsNotNull(result3);
        Assert.AreEqual("Default response for all inputs", result3.Content);
    }

    [TestMethod]
    public void GetMockResponse_WildcardWithPropertyOrder_ReturnsWildcard()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"a\":1,\"b\":2}", MockToolResponse.Success("Matched") },
            { "*", MockToolResponse.Error("No match") }
        };
        _registry.RegisterMockTool("test_tool", "Test", "{}", responseMap);

        // Act - Different property order should match semantically, not wildcard
        var result1 = _registry.GetMockResponse("test_tool", "{\"b\":2,\"a\":1}");
        Assert.IsNotNull(result1);
        Assert.IsTrue(result1.IsSuccess, "Should match semantically");

        // Act - Completely different should use wildcard
        var result2 = _registry.GetMockResponse("test_tool", "{\"c\":3}");
        Assert.IsNotNull(result2);
        Assert.IsFalse(result2.IsSuccess, "Should use wildcard for non-matching input");
        Assert.AreEqual("No match", result2.ErrorMessage);
    }

    #endregion

    #region Registration Tests (Phase 2)

    [TestMethod]
    public void RegisterMockTool_ValidTool_SuccessfullyRegisters()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act
        _registry.RegisterMockTool("test_tool", "Test tool", "{}", responseMap);

        // Assert
        var tool = _registry.GetMockTool("test_tool");
        Assert.IsNotNull(tool, "Tool should be registered");
        Assert.AreEqual("test_tool", tool.Name);
        Assert.AreEqual(ToolSourceType.ScenarioMock, tool.SourceType);
    }

    [TestMethod]
    public void RegisterMockTool_NullName_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            _registry.RegisterMockTool(null!, "Description", "{}", responseMap));
    }

    [TestMethod]
    public void RegisterMockTool_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            _registry.RegisterMockTool(string.Empty, "Description", "{}", responseMap));
    }

    [TestMethod]
    public void RegisterMockTool_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            _registry.RegisterMockTool("   ", "Description", "{}", responseMap));
    }

    [TestMethod]
    public void RegisterMockTool_DuplicateName_ReplacesExistingTool()
    {
        // Arrange
        var responseMap1 = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("First response") }
        };
        var responseMap2 = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Second response") }
        };

        // Act
        _registry.RegisterMockTool("test_tool", "First", "{}", responseMap1);
        _registry.RegisterMockTool("test_tool", "Second", "{}", responseMap2);

        // Assert
        var response = _registry.GetMockResponse("test_tool", "{}");
        Assert.IsNotNull(response);
        Assert.AreEqual("Second response", response.Content, "Tool should be replaced with new registration");
    }

    [TestMethod]
    public void RegisterMockTool_MultipleTools_AllRegistered()
    {
        // Arrange & Act
        _registry.RegisterMockTool("tool1", "Tool 1", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Tool 1") }
        });
        _registry.RegisterMockTool("tool2", "Tool 2", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Tool 2") }
        });
        _registry.RegisterMockTool("tool3", "Tool 3", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Tool 3") }
        });

        // Assert
        var allTools = _registry.GetAllMockTools();
        Assert.AreEqual(3, allTools.Count, "All three tools should be registered");
        Assert.IsTrue(allTools.Any(t => t.Name == "tool1"));
        Assert.IsTrue(allTools.Any(t => t.Name == "tool2"));
        Assert.IsTrue(allTools.Any(t => t.Name == "tool3"));
    }

    #endregion

    #region Retrieval Tests (Phase 2)

    [TestMethod]
    public void GetMockTool_ExistingTool_ReturnsTool()
    {
        // Arrange
        _registry.RegisterMockTool("test_tool", "Test", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        });

        // Act
        var tool = _registry.GetMockTool("test_tool");

        // Assert
        Assert.IsNotNull(tool);
        Assert.AreEqual("test_tool", tool.Name);
        Assert.AreEqual("Test", tool.Description);
    }

    [TestMethod]
    public void GetMockTool_NonExistentTool_ReturnsNull()
    {
        // Act
        var tool = _registry.GetMockTool("non_existent_tool");

        // Assert
        Assert.IsNull(tool, "Non-existent tool should return null");
    }

    [TestMethod]
    public void GetMockTool_NullName_ReturnsNull()
    {
        // Act
        var tool = _registry.GetMockTool(null!);

        // Assert
        Assert.IsNull(tool, "Null tool name should return null");
    }

    [TestMethod]
    public void GetMockTool_EmptyName_ReturnsNull()
    {
        // Act
        var tool = _registry.GetMockTool(string.Empty);

        // Assert
        Assert.IsNull(tool, "Empty tool name should return null");
    }

    [TestMethod]
    public void GetAllMockTools_MultipleTools_ReturnsAllTools()
    {
        // Arrange
        _registry.RegisterMockTool("tool1", "Tool 1", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("1") }
        });
        _registry.RegisterMockTool("tool2", "Tool 2", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("2") }
        });

        // Act
        var tools = _registry.GetAllMockTools();

        // Assert
        Assert.AreEqual(2, tools.Count);
        CollectionAssert.AllItemsAreNotNull(tools.ToList());
    }

    [TestMethod]
    public void GetAllMockTools_EmptyRegistry_ReturnsEmptyList()
    {
        // Act
        var tools = _registry.GetAllMockTools();

        // Assert
        Assert.AreEqual(0, tools.Count, "Empty registry should return empty list");
    }

    [TestMethod]
    public void GetAllMockTools_ReturnsReadOnlyList()
    {
        // Arrange
        _registry.RegisterMockTool("tool1", "Tool 1", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("1") }
        });

        // Act
        var tools = _registry.GetAllMockTools();

        // Assert
        Assert.IsInstanceOfType(tools, typeof(IReadOnlyList<ITool>));
    }

    #endregion

    #region Unregistration Tests (Phase 2)

    [TestMethod]
    public void UnregisterMockTool_ExistingTool_RemovesTool()
    {
        // Arrange
        _registry.RegisterMockTool("test_tool", "Test", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        });

        // Act
        _registry.UnregisterMockTool("test_tool");

        // Assert
        var tool = _registry.GetMockTool("test_tool");
        Assert.IsNull(tool, "Tool should be removed");
    }

    [TestMethod]
    public void UnregisterMockTool_NonExistentTool_NoError()
    {
        // Act & Assert - Should not throw
        _registry.UnregisterMockTool("non_existent_tool");
    }

    [TestMethod]
    public void UnregisterMockTool_NullName_NoError()
    {
        // Act & Assert - Should not throw
        _registry.UnregisterMockTool(null!);
    }

    [TestMethod]
    public void UnregisterMockTool_EmptyName_NoError()
    {
        // Act & Assert - Should not throw
        _registry.UnregisterMockTool(string.Empty);
    }

    [TestMethod]
    public void UnregisterAllMockTools_ClearsAllTools()
    {
        // Arrange
        _registry.RegisterMockTool("tool1", "Tool 1", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("1") }
        });
        _registry.RegisterMockTool("tool2", "Tool 2", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("2") }
        });

        // Act
        _registry.UnregisterAllMockTools();

        // Assert
        var tools = _registry.GetAllMockTools();
        Assert.AreEqual(0, tools.Count, "All tools should be removed");
    }

    [TestMethod]
    public void UnregisterAllMockTools_EmptyRegistry_NoError()
    {
        // Act & Assert - Should not throw
        _registry.UnregisterAllMockTools();
    }

    #endregion

    #region IToolRegistry Interface Tests (Phase 2)

    [TestMethod]
    public void GetAllTools_ReturnsAllMockTools()
    {
        // Arrange
        _registry.RegisterMockTool("tool1", "Tool 1", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("1") }
        });

        // Act
        var tools = _registry.GetAllTools();

        // Assert
        Assert.AreEqual(1, tools.Count);
        Assert.AreEqual("tool1", tools[0].Name);
    }

    [TestMethod]
    public void GetTool_ReturnsCorrectTool()
    {
        // Arrange
        _registry.RegisterMockTool("test_tool", "Test", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        });

        // Act
        var tool = _registry.GetTool("test_tool");

        // Assert
        Assert.IsNotNull(tool);
        Assert.AreEqual("test_tool", tool.Name);
    }

    [TestMethod]
    public void HasTool_ExistingTool_ReturnsTrue()
    {
        // Arrange
        _registry.RegisterMockTool("test_tool", "Test", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        });

        // Act & Assert
        Assert.IsTrue(_registry.HasTool("test_tool"));
    }

    [TestMethod]
    public void HasTool_NonExistentTool_ReturnsFalse()
    {
        // Act & Assert
        Assert.IsFalse(_registry.HasTool("non_existent_tool"));
    }

    [TestMethod]
    public void HasTool_NullName_ReturnsFalse()
    {
        // Act & Assert
        Assert.IsFalse(_registry.HasTool(null!));
    }

    [TestMethod]
    public void HasTool_EmptyName_ReturnsFalse()
    {
        // Act & Assert
        Assert.IsFalse(_registry.HasTool(string.Empty));
    }

    [TestMethod]
    public async Task RefreshAsync_CompletesSuccessfully()
    {
        // Act & Assert - Should complete without error
        await _registry.RefreshAsync();
    }

    [TestMethod]
    public async Task RefreshAsync_DoesNotAffectTools()
    {
        // Arrange
        _registry.RegisterMockTool("test_tool", "Test", "{}", new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        });
        var toolsBeforeRefresh = _registry.GetAllTools();

        // Act
        await _registry.RefreshAsync();
        var toolsAfterRefresh = _registry.GetAllTools();

        // Assert
        Assert.AreEqual(toolsBeforeRefresh.Count, toolsAfterRefresh.Count,
            "RefreshAsync should not affect tools for mock registry");
    }

    #endregion
}

