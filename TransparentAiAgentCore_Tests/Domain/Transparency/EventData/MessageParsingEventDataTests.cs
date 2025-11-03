using TransparentAiAgentCore.Domain.Transparency.EventData;

namespace TransparentAiAgentCore_Tests.Domain.Transparency.EventData;

[TestClass]
public class MessageParsingEventDataTests
{
    [TestMethod]
    public void Constructor_SuccessScenario_InitializesCorrectly()
    {
        // Arrange
        var rawData = "{\"content\": \"test\"}";
        var parsedData = "Parsed: test";
        var success = true;

        // Act
        var data = new MessageParsingEventData(rawData, parsedData, success, null);

        // Assert
        Assert.AreEqual(rawData, data.RawData);
        Assert.AreEqual(parsedData, data.ParsedData);
        Assert.IsTrue(data.Success);
        Assert.IsNull(data.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_FailureScenario_InitializesWithError()
    {
        // Arrange
        var rawData = "{\"invalid\": json}";
        var parsedData = "";
        var success = false;
        var errorMessage = "JSON parsing failed";

        // Act
        var data = new MessageParsingEventData(rawData, parsedData, success, errorMessage);

        // Assert
        Assert.AreEqual(rawData, data.RawData);
        Assert.AreEqual(parsedData, data.ParsedData);
        Assert.IsFalse(data.Success);
        Assert.AreEqual(errorMessage, data.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_NullRawData_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new MessageParsingEventData(null!, "", true, null));
    }

    [TestMethod]
    public void Constructor_NullParsedData_AllowedForFailureCase()
    {
        // Arrange & Act
        var data = new MessageParsingEventData("{}", null!, false, "Error");

        // Assert
        Assert.IsNull(data.ParsedData);
        Assert.IsFalse(data.Success);
    }

    [TestMethod]
    public void Constructor_NullErrorMessageOnSuccess_Allowed()
    {
        // Arrange & Act
        var data = new MessageParsingEventData("{}", "parsed", true, null);

        // Assert
        Assert.IsNull(data.ErrorMessage);
        Assert.IsTrue(data.Success);
    }

    [TestMethod]
    public void Constructor_FailureWithoutErrorMessage_Allowed()
    {
        // Arrange & Act
        var data = new MessageParsingEventData("{}", "partial", false, null);

        // Assert
        Assert.IsFalse(data.Success);
        Assert.IsNull(data.ErrorMessage);
    }
}
