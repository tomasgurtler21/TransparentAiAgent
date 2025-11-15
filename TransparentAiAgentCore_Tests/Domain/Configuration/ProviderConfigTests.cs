using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class ProviderConfigTests
{
    [TestMethod]
    public void ProviderConfig_NullType_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ProviderConfig(type: null!, displayName: "Test", parameters: new()));
    }

    [TestMethod]
    public void ProviderConfig_EmptyType_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ProviderConfig(type: "", displayName: "Test", parameters: new()));
    }

    [TestMethod]
    public void ProviderConfig_WhitespaceType_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ProviderConfig(type: "   ", displayName: "Test", parameters: new()));
    }

    [TestMethod]
    public void ProviderConfig_NullDisplayName_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ProviderConfig(type: "Anthropic", displayName: null!, parameters: new()));
    }

    [TestMethod]
    public void ProviderConfig_EmptyDisplayName_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ProviderConfig(type: "Anthropic", displayName: "", parameters: new()));
    }

    [TestMethod]
    public void ProviderConfig_ValidParameters_SetsProperties()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["Model"] = "claude-haiku" };

        // Act
        var config = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude Fast",
            parameters: parameters
        );

        // Assert
        Assert.AreEqual("Anthropic", config.Type);
        Assert.AreEqual("Claude Fast", config.DisplayName);
        Assert.AreEqual("claude-haiku", config.Parameters["Model"]);
    }

    [TestMethod]
    public void ProviderConfig_NullParameters_InitializesEmptyDictionary()
    {
        // Act
        var config = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude Fast",
            parameters: null!
        );

        // Assert
        Assert.IsNotNull(config.Parameters);
        Assert.AreEqual(0, config.Parameters.Count);
    }

    [TestMethod]
    public void ProviderConfig_WithParameterOverrides_SetsOverrides()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["Model"] = "claude-haiku" };
        var overrides = new ProviderParameters(temperature: 1.0);

        // Act
        var config = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude Fast",
            parameters: parameters,
            parameterOverrides: overrides
        );

        // Assert
        Assert.IsNotNull(config.ParameterOverrides);
        Assert.AreEqual(1.0, config.ParameterOverrides.Temperature);
    }
}
