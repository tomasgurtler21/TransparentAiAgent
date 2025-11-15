using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class ProviderParametersTests
{
    [TestMethod]
    public void ProviderParameters_DefaultValues_ReturnsDefaultParameters()
    {
        // Arrange
        var defaults = new ProviderParameters(temperature: 0.7, topP: 1.0, maxTokens: 4096);
        var provider = new ProviderParameters();

        // Act
        var effective = provider.GetEffectiveParameters(defaults);

        // Assert
        Assert.AreEqual(0.7, effective.Temperature);
        Assert.AreEqual(1.0, effective.TopP);
        Assert.AreEqual(4096, effective.MaxTokens);
    }

    [TestMethod]
    public void ProviderParameters_PartialOverrides_MergesWithDefaults()
    {
        // Arrange
        var defaults = new ProviderParameters(temperature: 0.7, topP: 1.0, maxTokens: 4096);
        var provider = new ProviderParameters(temperature: 1.0); // Only override temp

        // Act
        var effective = provider.GetEffectiveParameters(defaults);

        // Assert
        Assert.AreEqual(1.0, effective.Temperature);  // Overridden
        Assert.AreEqual(1.0, effective.TopP);         // From defaults
        Assert.AreEqual(4096, effective.MaxTokens);   // From defaults
    }

    [TestMethod]
    public void ProviderParameters_AllOverrides_IgnoresDefaults()
    {
        // Arrange
        var defaults = new ProviderParameters(temperature: 0.7, topP: 1.0, maxTokens: 4096);
        var provider = new ProviderParameters(temperature: 1.0, topP: 0.5, maxTokens: 2048);

        // Act
        var effective = provider.GetEffectiveParameters(defaults);

        // Assert
        Assert.AreEqual(1.0, effective.Temperature);
        Assert.AreEqual(0.5, effective.TopP);
        Assert.AreEqual(2048, effective.MaxTokens);
    }

    [TestMethod]
    public void ProviderParameters_NullDefaults_UsesOnlyOverrides()
    {
        // Arrange
        var provider = new ProviderParameters(temperature: 1.0, topP: 0.8);
        var defaults = new ProviderParameters();

        // Act
        var effective = provider.GetEffectiveParameters(defaults);

        // Assert
        Assert.AreEqual(1.0, effective.Temperature);
        Assert.AreEqual(0.8, effective.TopP);
        Assert.IsNull(effective.MaxTokens);
    }

    [TestMethod]
    public void ProviderParameters_Constructor_SetsPropertiesCorrectly()
    {
        // Act
        var parameters = new ProviderParameters(temperature: 0.9, topP: 0.95, maxTokens: 8192);

        // Assert
        Assert.AreEqual(0.9, parameters.Temperature);
        Assert.AreEqual(0.95, parameters.TopP);
        Assert.AreEqual(8192, parameters.MaxTokens);
    }

    [TestMethod]
    public void ProviderParameters_DefaultConstructor_AllPropertiesNull()
    {
        // Act
        var parameters = new ProviderParameters();

        // Assert
        Assert.IsNull(parameters.Temperature);
        Assert.IsNull(parameters.TopP);
        Assert.IsNull(parameters.MaxTokens);
    }
}
