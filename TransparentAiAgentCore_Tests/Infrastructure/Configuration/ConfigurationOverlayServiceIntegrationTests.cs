using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Infrastructure.Configuration;

namespace TransparentAiAgentCore_Tests.Infrastructure.Configuration;

/// <summary>
/// Integration tests for ConfigurationOverlayService base configuration population.
/// Step 3 of CONFIG_RESTORATION_IMPLEMENTATION_PLAN.md (RED Phase)
/// </summary>
[TestClass]
public class ConfigurationOverlayServiceIntegrationTests
{
    // Test 1: Base config should contain messageLimit
    [TestMethod]
    public void BaseConfiguration_ShouldContainMessageLimit()
    {
        // Arrange
        var appConfig = new AppConfiguration
        {
            Agent = new AgentConfiguration { ContextWindowSize = 200 }
        };

        var baseConfig = new Dictionary<string, object>
        {
            { "messageLimit", appConfig.Agent.ContextWindowSize }
        };

        var service = new ConfigurationOverlayService(baseConfig);

        // Act
        var value = service.GetValue<int>("messageLimit", -1);

        // Assert
        Assert.AreEqual(200, value, "Base config should contain messageLimit from AppConfiguration");
        Assert.AreNotEqual(-1, value, "Should not fall back to default parameter");
    }

    // Test 2: Empty base config falls back to default (current buggy behavior)
    [TestMethod]
    public void EmptyBaseConfiguration_FallsBackToDefault()
    {
        // Arrange
        var service = new ConfigurationOverlayService(new Dictionary<string, object>());

        // Act
        var value = service.GetValue<int>("messageLimit", 100);

        // Assert
        Assert.AreEqual(100, value, "Empty base config should fall back to default parameter");
    }

    // Test 3: Base config should be overridden by overlay
    [TestMethod]
    public void BaseConfiguration_OverriddenByOverlay()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { { "messageLimit", 200 } };
        var service = new ConfigurationOverlayService(baseConfig);

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "messageLimit", 8 } });
        var value = service.GetValue<int>("messageLimit", -1);

        // Assert
        Assert.AreEqual(8, value, "Overlay should override base config");
    }

    // Test 4: After overlay pop, should return to base config value
    [TestMethod]
    public void PopOverlay_ReturnsToBaseConfiguration()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { { "messageLimit", 200 } };
        var service = new ConfigurationOverlayService(baseConfig);
        service.PushOverlay(new Dictionary<string, object> { { "messageLimit", 8 } });

        // Act
        service.PopOverlay();
        var value = service.GetValue<int>("messageLimit", -1);

        // Assert
        Assert.AreEqual(200, value, "Should return to base config value after overlay pop");
    }
}
