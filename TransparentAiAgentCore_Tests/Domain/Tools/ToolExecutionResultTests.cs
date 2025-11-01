using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore_Tests.Domain.Tools;

[TestClass]
public class ToolExecutionResultTests
{
    [TestMethod]
    public void Success_ValidContent_CreatesSuccessResult()
    {
        // Arrange
        var content = "Tool executed successfully";
        var executionTime = TimeSpan.FromMilliseconds(150);

        // Act
        var result = ToolExecutionResult.Success(content, executionTime);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(content, result.Content);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreEqual(executionTime, result.ExecutionTime);
    }

    [TestMethod]
    public void Failure_ValidErrorMessage_CreatesFailureResult()
    {
        // Arrange
        var errorMessage = "Tool execution failed";
        var executionTime = TimeSpan.FromMilliseconds(50);

        // Act
        var result = ToolExecutionResult.Failure(errorMessage, executionTime);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.Content); // Should have error content
        Assert.AreEqual(errorMessage, result.ErrorMessage);
        Assert.AreEqual(executionTime, result.ExecutionTime);
    }

    [TestMethod]
    public void Success_EmptyContent_CreatesSuccessResult()
    {
        // Arrange
        var content = string.Empty;
        var executionTime = TimeSpan.FromMilliseconds(100);

        // Act
        var result = ToolExecutionResult.Success(content, executionTime);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(string.Empty, result.Content);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Failure_EmptyErrorMessage_CreatesFailureResult()
    {
        // Arrange
        var errorMessage = string.Empty;
        var executionTime = TimeSpan.FromMilliseconds(25);

        // Act
        var result = ToolExecutionResult.Failure(errorMessage, executionTime);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(errorMessage, result.ErrorMessage);
    }

    [TestMethod]
    public void Success_ZeroExecutionTime_CreatesSuccessResult()
    {
        // Arrange
        var content = "Quick result";
        var executionTime = TimeSpan.Zero;

        // Act
        var result = ToolExecutionResult.Success(content, executionTime);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TimeSpan.Zero, result.ExecutionTime);
    }
}
