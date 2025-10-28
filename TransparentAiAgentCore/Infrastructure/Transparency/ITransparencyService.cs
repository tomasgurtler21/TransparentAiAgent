using TransparentAiAgentCore.Domain.Transparency;

namespace TransparentAiAgentCore.Infrastructure.Transparency;

public interface ITransparencyService
{
    /// <summary>
    /// Log a transparency event
    /// </summary>
    void LogEvent(TransparencyEvent evt);

    /// <summary>
    /// Get all transparency events
    /// </summary>
    IEnumerable<TransparencyEvent> GetEvents();

    /// <summary>
    /// Get transparency events by type
    /// </summary>
    IEnumerable<TransparencyEvent> GetEventsByType(TransparencyEventType eventType);

    /// <summary>
    /// Get transparency events within time range
    /// </summary>
    IEnumerable<TransparencyEvent> GetEventsByTimeRange(DateTime start, DateTime end);

    /// <summary>
    /// Clear all events (for testing)
    /// </summary>
    void ClearEvents();

    /// <summary>
    /// Event raised when new transparency event is logged
    /// </summary>
    event EventHandler<TransparencyEvent>? EventLogged;
}
