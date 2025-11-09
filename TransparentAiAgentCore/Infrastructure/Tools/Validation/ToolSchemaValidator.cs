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

            // 5. If no required fields, validation passes
            if (requiredFields.Count == 0)
            {
                return ValidationResult.Success();
            }

            // 6. Check each required field is present in arguments
            var argsRoot = argsDoc.RootElement;
            foreach (var requiredField in requiredFields)
            {
                if (!argsRoot.TryGetProperty(requiredField, out _))
                {
                    return ValidationResult.Failure(
                        $"Missing required parameter '{requiredField}'");
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
}
