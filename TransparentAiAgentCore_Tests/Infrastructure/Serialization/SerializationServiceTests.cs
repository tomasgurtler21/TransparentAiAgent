using TransparentAiAgentCore.Infrastructure.Serialization;

namespace TransparentAiAgentCore_Tests.Infrastructure.Serialization;

[TestClass]
public class SerializationServiceTests
{
    private SerializationService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new SerializationService();
    }

    [TestMethod]
    public void SerializeToJson_ValidObject_ProducesValidJson()
    {
        // Arrange
        var testObject = new TestClass
        {
            Name = "Test",
            Value = 42,
            IsActive = true
        };

        // Act
        var json = _service.SerializeToJson(testObject);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("\"name\":\"Test\""), "Should use camelCase");
        Assert.IsTrue(json.Contains("\"value\":42"));
        Assert.IsTrue(json.Contains("\"isActive\":true"));
        Assert.IsFalse(json.Contains("\n"), "Should not be indented");
    }

    [TestMethod]
    public void SerializeToPrettyJson_ValidObject_ProducesIndentedJson()
    {
        // Arrange
        var testObject = new TestClass
        {
            Name = "Test",
            Value = 42
        };

        // Act
        var json = _service.SerializeToPrettyJson(testObject);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("\"name\"") && json.Contains("\"Test\""), "Should use camelCase");
        Assert.IsTrue(json.Contains("\n"), "Should be indented");
    }

    [TestMethod]
    public void DeserializeFromJson_ValidJson_ReconstructsObject()
    {
        // Arrange
        var json = "{\"name\":\"Test\",\"value\":42,\"isActive\":true}";

        // Act
        var result = _service.DeserializeFromJson<TestClass>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Name);
        Assert.AreEqual(42, result.Value);
        Assert.AreEqual(true, result.IsActive);
    }

    [TestMethod]
    public void RoundTripSerialization_MaintainsDataIntegrity()
    {
        // Arrange
        var original = new TestClass
        {
            Name = "Test Object",
            Value = 123,
            IsActive = false
        };

        // Act
        var json = _service.SerializeToJson(original);
        var deserialized = _service.DeserializeFromJson<TestClass>(json);

        // Assert
        Assert.AreEqual(original.Name, deserialized.Name);
        Assert.AreEqual(original.Value, deserialized.Value);
        Assert.AreEqual(original.IsActive, deserialized.IsActive);
    }

    [TestMethod]
    public void SerializeToJson_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _service.SerializeToJson<TestClass>(null!));
    }

    [TestMethod]
    public void SerializeToPrettyJson_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _service.SerializeToPrettyJson<TestClass>(null!));
    }

    [TestMethod]
    public void DeserializeFromJson_NullJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _service.DeserializeFromJson<TestClass>(null!));
    }

    [TestMethod]
    public void DeserializeFromJson_EmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _service.DeserializeFromJson<TestClass>(""));
    }

    [TestMethod]
    public void DeserializeFromJson_WhitespaceJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _service.DeserializeFromJson<TestClass>("   "));
    }

    // Test helper class
    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }
}
