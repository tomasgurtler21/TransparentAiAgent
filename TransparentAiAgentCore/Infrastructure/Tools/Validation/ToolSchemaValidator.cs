using System.Text.Json;

namespace TransparentAiAgentCore.Infrastructure.Tools.Validation;

/// <summary>
/// Validates tool arguments against JSON Schema.
/// Ensures required parameters are present before tool execution.
/// </summary>
public class ToolSchemaValidator
{
    /// <summary>
    /// Validates tool arguments against the provided JSON schema.
    /// </summary>
    /// <param name="schema">JSON Schema string defining the tool's parameters</param>
    /// <param name="argumentsJson">JSON string containing the tool arguments</param>
    /// <returns>ValidationResult indicating success or failure with error details</returns>
    public ValidationResult ValidateArguments(string schema, string? argumentsJson)
    {
        // 1. Validate arguments is not null
        if (argumentsJson == null)
        {
            return ValidationResult.Failure("Arguments cannot be null");
        }

        // 2. Parse arguments JSON
        JsonDocument? argsDoc = null;
        try
        {
            argsDoc = JsonDocument.Parse(argumentsJson);
        }
        catch (JsonException ex)
        {
            return ValidationResult.Failure($"Invalid JSON in arguments: {ex.Message}");
        }

        // 3. Parse schema JSON
        JsonDocument? schemaDoc = null;
        try
        {
            schemaDoc = JsonDocument.Parse(schema);
        }
        catch (JsonException ex)
        {
            argsDoc?.Dispose();
            return ValidationResult.Failure($"Invalid JSON in schema: {ex.Message}");
        }

        try
        {
            // 4. Extract required fields from schema
            var requiredFields = ExtractRequiredFields(schemaDoc);

            // 5. Check each required field is present in arguments
            var argsRoot = argsDoc.RootElement;
            foreach (var requiredField in requiredFields)
            {
                if (!argsRoot.TryGetProperty(requiredField, out _))
                {
                    return ValidationResult.Failure(
                        $"Missing required parameter '{requiredField}'");
                }
            }

            // 6. Validate types and enums for all properties (Phase 3)
            if (schemaDoc.RootElement.TryGetProperty("properties", out var propertiesElement))
            {
                foreach (var property in propertiesElement.EnumerateObject())
                {
                    var fieldName = property.Name;
                    var fieldSchema = property.Value;

                    // Skip if field not present in arguments (only validate if present)
                    if (!argsRoot.TryGetProperty(fieldName, out var fieldValue))
                    {
                        continue;
                    }

                    // Validate type
                    if (fieldSchema.TryGetProperty("type", out var typeElement))
                    {
                        var expectedType = typeElement.GetString();
                        var validationError = ValidateType(fieldName, fieldValue, expectedType);
                        if (validationError != null)
                        {
                            return ValidationResult.Failure(validationError);
                        }
                    }

                    // Validate enum
                    if (fieldSchema.TryGetProperty("enum", out var enumElement))
                    {
                        var validationError = ValidateEnum(fieldName, fieldValue, enumElement);
                        if (validationError != null)
                        {
                            return ValidationResult.Failure(validationError);
                        }
                    }
                }
            }

            return ValidationResult.Success();
        }
        finally
        {
            argsDoc?.Dispose();
            schemaDoc?.Dispose();
        }
    }

    /// <summary>
    /// Extracts the list of required field names from a JSON Schema.
    /// </summary>
    private List<string> ExtractRequiredFields(JsonDocument schemaDoc)
    {
        var requiredFields = new List<string>();

        // Look for "required" array in the schema root
        if (schemaDoc.RootElement.TryGetProperty("required", out var requiredElement))
        {
            if (requiredElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in requiredElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var fieldName = item.GetString();
                        if (!string.IsNullOrEmpty(fieldName))
                        {
                            requiredFields.Add(fieldName);
                        }
                    }
                }
            }
        }

        return requiredFields;
    }

    /// <summary>
    /// Validates that a field value matches the expected JSON Schema type.
    /// </summary>
    /// <returns>Error message if validation fails, null if validation succeeds</returns>
    private string? ValidateType(string fieldName, JsonElement fieldValue, string? expectedType)
    {
        if (string.IsNullOrEmpty(expectedType))
        {
            return null; // No type constraint
        }

        var actualKind = fieldValue.ValueKind;

        var isValid = expectedType.ToLowerInvariant() switch
        {
            "string" => actualKind == JsonValueKind.String,
            "number" => actualKind == JsonValueKind.Number,
            "integer" => actualKind == JsonValueKind.Number,
            "boolean" => actualKind == JsonValueKind.True || actualKind == JsonValueKind.False,
            "object" => actualKind == JsonValueKind.Object,
            "array" => actualKind == JsonValueKind.Array,
            "null" => actualKind == JsonValueKind.Null,
            _ => true // Unknown type, skip validation
        };

        if (!isValid)
        {
            return $"Parameter '{fieldName}' has invalid type. Expected '{expectedType}' but got '{actualKind}'";
        }

        return null;
    }

    /// <summary>
    /// Validates that a field value is one of the allowed enum values.
    /// </summary>
    /// <returns>Error message if validation fails, null if validation succeeds</returns>
    private string? ValidateEnum(string fieldName, JsonElement fieldValue, JsonElement enumElement)
    {
        if (enumElement.ValueKind != JsonValueKind.Array)
        {
            return null; // Invalid enum definition, skip validation
        }

        var allowedValues = new List<string>();
        var actualValue = fieldValue.ValueKind == JsonValueKind.String
            ? fieldValue.GetString()
            : fieldValue.ToString();

        foreach (var enumValue in enumElement.EnumerateArray())
        {
            var enumString = enumValue.ValueKind == JsonValueKind.String
                ? enumValue.GetString()
                : enumValue.ToString();

            if (!string.IsNullOrEmpty(enumString))
            {
                allowedValues.Add(enumString);

                if (enumString == actualValue)
                {
                    return null; // Value found in enum
                }
            }
        }

        return $"Parameter '{fieldName}' has invalid value '{actualValue}'. " +
               $"Allowed values: [{string.Join(", ", allowedValues)}]";
    }
}
