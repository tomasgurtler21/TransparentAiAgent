using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore.Domain.Models
{
    /// <summary>
    /// Represents an assistant message originated by the application (e.g., from scenario playback).
    /// These messages provide scripted assistant responses for teaching or demonstration purposes.
    /// </summary>
    public class ScenarioAssistantMessage : ApplicationMessage
    {
        /// <summary>
        /// Gets the annotation text for teaching mode display.
        /// This optional text provides context about the scenario step for educational purposes.
        /// </summary>
        public string? Annotation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScenarioAssistantMessage"/> class.
        /// </summary>
        /// <param name="content">The message content. Cannot be null, empty, or whitespace.</param>
        /// <param name="annotation">Optional annotation text for teaching mode display.</param>
        /// <exception cref="ArgumentException">Thrown when content is null, empty, or whitespace.</exception>
        public ScenarioAssistantMessage(string content, string? annotation = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Content cannot be null, empty, or whitespace.", nameof(content));
            }

            Id = Guid.NewGuid();
            Content = content;
            Timestamp = DateTime.UtcNow;
            Annotation = annotation;
            MessageType = "scenario_assistant_message";
            ContextStatus = MessageContextStatus.InContext;
        }

        public override MessageRole Role => MessageRole.Assistant;

        public override string MessageTypeDiscriminator => "Application.ScenarioAssistant";
    }
}
