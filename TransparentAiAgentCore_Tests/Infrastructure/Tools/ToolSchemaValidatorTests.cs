using TransparentAiAgentCore.Infrastructure.Tools.Validation;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools;

[TestClass]
public class ToolSchemaValidatorTests
{
    [TestMethod]
    public void ValidateArguments_MissingRequiredField_ReturnsFailure()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                },
                "required": ["name"]
            }
            """;
        var arguments = "{}"; // Missing required field 'name'

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("name"));
        Assert.IsTrue(result.ErrorMessage.Contains("required"));
    }

    [TestMethod]
    public void ValidateArguments_AllRequiredFieldsPresent_ReturnsSuccess()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                },
                "required": ["name"]
            }
            """;
        var arguments = """{"name": "test"}""";

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void ValidateArguments_NoRequiredFields_ReturnsSuccess()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                }
            }
            """;
        var arguments = "{}"; // No required fields, empty is ok

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateArguments_MultipleRequiredFields_OneMissing_ReturnsFailure()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    },
                    "age": {
                        "type": "number"
                    }
                },
                "required": ["name", "age"]
            }
            """;
        var arguments = """{"name": "test"}"""; // Missing 'age'

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("age"));
    }

    [TestMethod]
    public void ValidateArguments_NullArguments_ReturnsFailure()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                },
                "required": ["name"]
            }
            """;

        // Act
        var result = validator.ValidateArguments(schema, null);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void ValidateArguments_InvalidJsonArguments_ReturnsFailure()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                }
            }
            """;
        var arguments = "{invalid json}";

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("JSON") || result.ErrorMessage.Contains("parse"));
    }

    // Phase 3: Type Validation Tests
    [TestMethod]
    public void ValidateArguments_WrongTypeString_ReturnsFailure()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "age": {
                        "type": "number"
                    }
                },
                "required": ["age"]
            }
            """;
        var arguments = """{"age": "not a number"}"""; // Wrong type

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("age") && result.ErrorMessage.Contains("type"));
    }

    [TestMethod]
    public void ValidateArguments_CorrectTypeNumber_ReturnsSuccess()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "age": {
                        "type": "number"
                    }
                },
                "required": ["age"]
            }
            """;
        var arguments = """{"age": 25}""";

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateArguments_WrongTypeBoolean_ReturnsFailure()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "isActive": {
                        "type": "boolean"
                    }
                },
                "required": ["isActive"]
            }
            """;
        var arguments = """{"isActive": "yes"}"""; // Wrong type

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("isActive") && result.ErrorMessage.Contains("type"));
    }

    // Phase 3: Enum Validation Tests
    [TestMethod]
    public void ValidateArguments_InvalidEnumValue_ReturnsFailure()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "status": {
                        "type": "string",
                        "enum": ["active", "inactive", "pending"]
                    }
                },
                "required": ["status"]
            }
            """;
        var arguments = """{"status": "deleted"}"""; // Not in enum

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("status") && result.ErrorMessage.Contains("enum"));
    }

    [TestMethod]
    public void ValidateArguments_ValidEnumValue_ReturnsSuccess()
    {
        // Arrange
        var validator = new ToolSchemaValidator();
        var schema = """
            {
                "type": "object",
                "properties": {
                    "status": {
                        "type": "string",
                        "enum": ["active", "inactive", "pending"]
                    }
                },
                "required": ["status"]
            }
            """;
        var arguments = """{"status": "active"}"""; // Valid enum value

        // Act
        var result = validator.ValidateArguments(schema, arguments);

        // Assert
        Assert.IsTrue(result.IsValid);
    }
}
