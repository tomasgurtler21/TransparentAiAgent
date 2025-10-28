using TransparentAiAgentCore.Domain.Transparency;

namespace TransparentAiAgentCore.Infrastructure.Transparency;

public class TransparencyService : ITransparencyService
{
    private readonly List<TransparencyEvent> _events = new();
    private readonly object _lock = new();

    public event EventHandler<TransparencyEvent>? EventLogged;

    public void LogEvent(TransparencyEvent evt)
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));

        lock (_lock)
        {
            _events.Add(evt);
        }

        // Raise event
        EventLogged?.Invoke(this, evt);
    }

    public IEnumerable<TransparencyEvent> GetEvents()
    {
        lock (_lock)
        {
            return _events.ToList(); // Return copy
        }
    }

    public IEnumerable<TransparencyEvent> GetEventsByType(TransparencyEventType eventType)
    {
        lock (_lock)
        {
            return _events.Where(e => e.EventType == eventType).ToList();
        }
    }

    public IEnumerable<TransparencyEvent> GetEventsByTimeRange(DateTime start, DateTime end)
    {
        lock (_lock)
        {
            return _events.Where(e => e.Timestamp >= start && e.Timestamp <= end).ToList();
        }
    }

    public void ClearEvents()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
