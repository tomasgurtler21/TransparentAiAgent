using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.ScenarioMock;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.ScenarioMock;

/// <summary>
/// Tests for ScenarioMockTool following Lean TDD principles.
/// Testing meaningful behavior: construction validation, tool metadata, and source type.
/// </summary>
[TestClass]
public class ScenarioMockToolTests
{
    #region Construction Tests

    [TestMethod]
    public void Constructor_ValidParameters_CreatesToolSuccessfully()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act
        var tool = new ScenarioMockTool(
            name: "test_tool",
            description: "Test description",
            parametersSchema: "{}",
            responseMap: responseMap);

        // Assert
        Assert.IsNotNull(tool);
        Assert.AreEqual("test_tool", tool.Name);
        Assert.AreEqual("Test description", tool.Description);
        Assert.AreEqual("{}", tool.ParametersSchema);
        Assert.AreEqual(ToolSourceType.ScenarioMock, tool.SourceType);
    }

    [TestMethod]
    public void Constructor_NullName_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool(null!, "Description", "{}", responseMap));
    }

    [TestMethod]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool(string.Empty, "Description", "{}", responseMap));
    }

    [TestMethod]
    public void Constructor_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("   ", "Description", "{}", responseMap));
    }

    [TestMethod]
    public void Constructor_NullDescription_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", null!, "{}", responseMap));
    }

    [TestMethod]
    public void Constructor_EmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", string.Empty, "{}", responseMap));
    }

    [TestMethod]
    public void Constructor_WhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", "   ", "{}", responseMap));
    }

    [TestMethod]
    public void Constructor_NullParametersSchema_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", "Description", null!, responseMap));
    }

    [TestMethod]
    public void Constructor_EmptyParametersSchema_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", "Description", string.Empty, responseMap));
    }

    [TestMethod]
    public void Constructor_WhitespaceParametersSchema_ThrowsArgumentException()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", "Description", "   ", responseMap));
    }

    [TestMethod]
    public void Constructor_NullResponseMap_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", "Description", "{}", null!));
    }

    [TestMethod]
    public void Constructor_EmptyResponseMap_ThrowsArgumentException()
    {
        // Arrange
        var emptyResponseMap = new Dictionary<string, MockToolResponse>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioMockTool("test_tool", "Description", "{}", emptyResponseMap));
    }

    #endregion

    #region Property Tests

    [TestMethod]
    public void SourceType_ReturnsScenarioMock()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };
        var tool = new ScenarioMockTool("test_tool", "Description", "{}", responseMap);

        // Act
        var sourceType = tool.SourceType;

        // Assert
        Assert.AreEqual(ToolSourceType.ScenarioMock, sourceType);
    }

    [TestMethod]
    public void Metadata_ContainsSourceType()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };
        var tool = new ScenarioMockTool("test_tool", "Description", "{}", responseMap);

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsTrue(metadata.ContainsKey("SourceType"), "Metadata should contain SourceType");
        Assert.AreEqual("ScenarioMock", metadata["SourceType"]);
    }

    [TestMethod]
    public void Metadata_ContainsIsScenarioMock()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };
        var tool = new ScenarioMockTool("test_tool", "Description", "{}", responseMap);

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsTrue(metadata.ContainsKey("IsScenarioMock"), "Metadata should contain IsScenarioMock");
        Assert.AreEqual("true", metadata["IsScenarioMock"]);
    }

    [TestMethod]
    public void Metadata_ContainsResponseCount()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{\"arg1\":\"value1\"}", MockToolResponse.Success("Response 1") },
            { "{\"arg2\":\"value2\"}", MockToolResponse.Success("Response 2") },
            { "{\"arg3\":\"value3\"}", MockToolResponse.Success("Response 3") }
        };
        var tool = new ScenarioMockTool("test_tool", "Description", "{}", responseMap);

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsTrue(metadata.ContainsKey("ResponseCount"), "Metadata should contain ResponseCount");
        Assert.AreEqual("3", metadata["ResponseCount"]);
    }

    [TestMethod]
    public void Metadata_IsReadOnly()
    {
        // Arrange
        var responseMap = new Dictionary<string, MockToolResponse>
        {
            { "{}", MockToolResponse.Success("Success") }
        };
        var tool = new ScenarioMockTool("test_tool", "Description", "{}", responseMap);

        // Act
        var metadata = tool.Metadata;

        // Assert
        Assert.IsInstanceOfType(metadata, typeof(IReadOnlyDictionary<string, string>));
    }

    #endregion
}
