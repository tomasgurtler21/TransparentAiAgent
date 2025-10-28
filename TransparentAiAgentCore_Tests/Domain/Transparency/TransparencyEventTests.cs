using TransparentAiAgentCore.Domain.Transparency;

namespace TransparentAiAgentCore_Tests.Domain.Transparency;

[TestClass]
public class TransparencyEventTests
{
    [TestMethod]
    public void Constructor_GeneratesUniqueIds()
    {
        // Arrange & Act
        var event1 = new TransparencyEvent(TransparencyEventType.UserInput, "data1");
        var event2 = new TransparencyEvent(TransparencyEventType.UserInput, "data2");

        // Assert
        Assert.AreNotEqual(event1.Id, event2.Id);
        Assert.AreNotEqual(Guid.Empty, event1.Id);
        Assert.AreNotEqual(Guid.Empty, event2.Id);
    }

    [TestMethod]
    public void Constructor_SetsTimestampToUtcNow()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var evt = new TransparencyEvent(TransparencyEventType.UserInput, "data");

        // Assert
        var afterCreate = DateTime.UtcNow;
        Assert.IsTrue(evt.Timestamp >= beforeCreate);
        Assert.IsTrue(evt.Timestamp <= afterCreate);
    }

    [TestMethod]
    public void Constructor_NullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new TransparencyEvent(TransparencyEventType.UserInput, null!));
    }

    [TestMethod]
    public void Constructor_ValidData_StoresEventType()
    {
        // Arrange & Act
        var evt = new TransparencyEvent(TransparencyEventType.ToolCall, "test data");

        // Assert
        Assert.AreEqual(TransparencyEventType.ToolCall, evt.EventType);
    }

    [TestMethod]
    public void Constructor_ValidData_StoresData()
    {
        // Arrange & Act
        var evt = new TransparencyEvent(TransparencyEventType.UserInput, "test data");

        // Assert
        Assert.AreEqual("test data", evt.Data);
    }

    [TestMethod]
    public void Constructor_WithAdditionalInfo_StoresAdditionalInfo()
    {
        // Arrange & Act
        var evt = new TransparencyEvent(TransparencyEventType.Error, "error data", "additional context");

        // Assert
        Assert.AreEqual("additional context", evt.AdditionalInfo);
    }

    [TestMethod]
    public void Constructor_WithoutAdditionalInfo_AdditionalInfoIsNull()
    {
        // Arrange & Act
        var evt = new TransparencyEvent(TransparencyEventType.UserInput, "data");

        // Assert
        Assert.IsNull(evt.AdditionalInfo);
    }
}
