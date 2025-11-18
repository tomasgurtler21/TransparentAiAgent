using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Infrastructure.Configuration;

namespace TransparentAiAgentCore_Tests.Infrastructure.Configuration;

[TestClass]
public class ConfigurationOverlayServiceTests
{
    private IConfigurationOverlay CreateService(Dictionary<string, object>? baseConfig = null)
    {
        return new ConfigurationOverlayService(baseConfig ?? new Dictionary<string, object>());
    }

    [TestMethod]
    public void GetValue_WithNoOverlays_ReturnsBaseConfigValue()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        var service = CreateService(baseConfig);

        // Act
        var result1 = service.GetValue<string>("key1");
        var result2 = service.GetValue<int>("key2");

        // Assert
        Assert.AreEqual("value1", result1);
        Assert.AreEqual(42, result2);
    }

    [TestMethod]
    public void GetValue_WithOverlay_ReturnsOverlayValue()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { ["key1"] = "base" };
        var service = CreateService(baseConfig);
        var overlay = new Dictionary<string, object> { ["key1"] = "overlay" };

        // Act
        service.PushOverlay(overlay);
        var result = service.GetValue<string>("key1");

        // Assert
        Assert.AreEqual("overlay", result);
    }

    [TestMethod]
    public void GetValue_WithMultipleOverlays_ReturnsMostRecentOverlayValue()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { ["key1"] = "base" };
        var service = CreateService(baseConfig);
        var overlay1 = new Dictionary<string, object> { ["key1"] = "overlay1" };
        var overlay2 = new Dictionary<string, object> { ["key1"] = "overlay2" };

        // Act
        service.PushOverlay(overlay1);
        service.PushOverlay(overlay2);
        var result = service.GetValue<string>("key1");

        // Assert
        Assert.AreEqual("overlay2", result);
    }

    [TestMethod]
    public void GetValue_AfterPopOverlay_ReturnsNextOverlayOrBaseValue()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { ["key1"] = "base" };
        var service = CreateService(baseConfig);
        var overlay1 = new Dictionary<string, object> { ["key1"] = "overlay1" };
        var overlay2 = new Dictionary<string, object> { ["key1"] = "overlay2" };

        service.PushOverlay(overlay1);
        service.PushOverlay(overlay2);

        // Act
        service.PopOverlay();
        var result1 = service.GetValue<string>("key1");

        service.PopOverlay();
        var result2 = service.GetValue<string>("key1");

        // Assert
        Assert.AreEqual("overlay1", result1);
        Assert.AreEqual("base", result2);
    }

    [TestMethod]
    public void GetValue_WithDefault_ReturnsDefaultWhenKeyNotFound()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetValue("nonexistent", "default");

        // Assert
        Assert.AreEqual("default", result);
    }

    [TestMethod]
    public void GetValue_WithDefault_ReturnsValueWhenKeyFound()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { ["key1"] = "found" };
        var service = CreateService(baseConfig);

        // Act
        var result = service.GetValue("key1", "default");

        // Assert
        Assert.AreEqual("found", result);
    }

    [TestMethod]
    public void PopOverlay_WhenNoOverlays_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => service.PopOverlay());
    }

    [TestMethod]
    public void OverlayCount_TracksNumberOfActiveOverlays()
    {
        // Arrange
        var service = CreateService();
        var overlay = new Dictionary<string, object> { ["key"] = "value" };

        // Act & Assert
        Assert.AreEqual(0, service.OverlayCount);

        service.PushOverlay(overlay);
        Assert.AreEqual(1, service.OverlayCount);

        service.PushOverlay(overlay);
        Assert.AreEqual(2, service.OverlayCount);

        service.PopOverlay();
        Assert.AreEqual(1, service.OverlayCount);

        service.PopOverlay();
        Assert.AreEqual(0, service.OverlayCount);
    }

    [TestMethod]
    public void ClearAllOverlays_RemovesAllOverlaysAndRestoresBaseConfig()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { ["key1"] = "base" };
        var service = CreateService(baseConfig);
        var overlay1 = new Dictionary<string, object> { ["key1"] = "overlay1" };
        var overlay2 = new Dictionary<string, object> { ["key1"] = "overlay2" };

        service.PushOverlay(overlay1);
        service.PushOverlay(overlay2);

        // Act
        service.ClearAllOverlays();

        // Assert
        Assert.AreEqual(0, service.OverlayCount);
        Assert.AreEqual("base", service.GetValue<string>("key1"));
    }

    [TestMethod]
    public void HasKey_ReturnsTrueWhenKeyExistsInBaseConfig()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { ["key1"] = "value" };
        var service = CreateService(baseConfig);

        // Act
        var result = service.HasKey("key1");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasKey_ReturnsTrueWhenKeyExistsInOverlay()
    {
        // Arrange
        var service = CreateService();
        var overlay = new Dictionary<string, object> { ["key1"] = "value" };

        service.PushOverlay(overlay);

        // Act
        var result = service.HasKey("key1");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasKey_ReturnsFalseWhenKeyDoesNotExist()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.HasKey("nonexistent");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void PushOverlay_WithDifferentKeys_BothKeysAccessible()
    {
        // Arrange
        var baseConfig = new Dictionary<string, object> { ["baseKey"] = "baseValue" };
        var service = CreateService(baseConfig);
        var overlay = new Dictionary<string, object> { ["overlayKey"] = "overlayValue" };

        // Act
        service.PushOverlay(overlay);

        // Assert
        Assert.AreEqual("baseValue", service.GetValue<string>("baseKey"));
        Assert.AreEqual("overlayValue", service.GetValue<string>("overlayKey"));
    }

    [TestMethod]
    public void GetValue_WithNullableType_ReturnsNullWhenKeyNotFound()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetValue<string>("nonexistent");

        // Assert
        Assert.IsNull(result);
    }

    // ===== Event System Tests (Step 1: RED Phase) =====

    [TestMethod]
    public void PushOverlay_WithValidOverlay_FiresOverlayChangedEvent()
    {
        // Arrange
        var service = CreateService();
        bool eventFired = false;
        service.OverlayChanged += (sender, args) => eventFired = true;

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });

        // Assert
        Assert.IsTrue(eventFired, "OverlayChanged event should fire when overlay is pushed");
    }

    [TestMethod]
    public void PopOverlay_WithExistingOverlay_FiresOverlayChangedEvent()
    {
        // Arrange
        var service = CreateService();
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });
        bool eventFired = false;
        service.OverlayChanged += (sender, args) => eventFired = true;

        // Act
        service.PopOverlay();

        // Assert
        Assert.IsTrue(eventFired, "OverlayChanged event should fire when overlay is popped");
    }

    [TestMethod]
    public void PushOverlay_EventArgs_ContainsPushChangeType()
    {
        // Arrange
        var service = CreateService();
        ConfigurationChangedEventArgs? capturedArgs = null;
        service.OverlayChanged += (sender, args) => capturedArgs = args;

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });

        // Assert
        Assert.IsNotNull(capturedArgs, "Event args should not be null");
        Assert.AreEqual(ChangeType.Push, capturedArgs.Type, "Change type should be Push");
    }

    [TestMethod]
    public void PushOverlay_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var service = CreateService();
        int callCount = 0;
        service.OverlayChanged += (sender, args) => callCount++;
        service.OverlayChanged += (sender, args) => callCount++;

        // Act
        service.PushOverlay(new Dictionary<string, object> { { "test", "value" } });

        // Assert
        Assert.AreEqual(2, callCount, "Both subscribers should receive event");
    }
}
