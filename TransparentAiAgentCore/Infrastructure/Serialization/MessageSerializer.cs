using System;
using System.Collections.Generic;
using System.Text.Json;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore.Infrastructure.Serialization
{
    /// <summary>
    /// Provides serialization and deserialization for IMessage instances using MessageTypeDiscriminator.
    /// </summary>
    public class MessageSerializer
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, Type> _typeRegistry;

        public MessageSerializer()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNameCaseInsensitive = true, // Allow case-insensitive matching for deserialization
                IncludeFields = false
                // Use default PascalCase naming to match C# property names
            };

            // Initialize type registry with message type discriminators
            _typeRegistry = new Dictionary<string, Type>
            {
                { "User.Direct", typeof(DirectUserMessage) },
                { "Application.ScenarioUser", typeof(ScenarioUserMessage) },
                { "Application.ScenarioAssistant", typeof(ScenarioAssistantMessage) },
                { "Llm.Text", typeof(LlmTextMessage) },
                { "Llm.ToolCall", typeof(LlmToolCallMessage) },
                { "Tool.Result", typeof(ToolResultMessage) },
                { "Tool.Error", typeof(ToolErrorMessage) },
                { "System", typeof(SystemMessage) }
            };
        }

        /// <summary>
        /// Serializes a message to JSON string.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>JSON representation of the message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        public string Serialize(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Serialize using the concrete type to preserve all properties
            return JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
        }

        /// <summary>
        /// Deserializes a JSON string to an IMessage instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized message.</returns>
        /// <exception cref="ArgumentException">Thrown when json is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when discriminator is unknown.</exception>
        public IMessage Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            // Parse JSON to extract the discriminator
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("MessageTypeDiscriminator", out var discriminatorElement))
            {
                throw new InvalidOperationException("JSON does not contain a MessageTypeDiscriminator property.");
            }

            var discriminator = discriminatorElement.GetString();
            if (string.IsNullOrEmpty(discriminator))
            {
                throw new InvalidOperationException("MessageTypeDiscriminator is null or empty.");
            }

            // Look up the type in the registry
            if (!_typeRegistry.TryGetValue(discriminator, out var messageType))
            {
                throw new InvalidOperationException($"Unknown message type discriminator: {discriminator}");
            }

            // Deserialize to the concrete type
            var message = JsonSerializer.Deserialize(json, messageType, _jsonOptions) as IMessage;

            if (message == null)
            {
                throw new InvalidOperationException($"Failed to deserialize message of type {messageType.Name}");
            }

            return message;
        }
    }
}
