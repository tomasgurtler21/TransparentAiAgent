using TransparentAiAgentCore.Domain.Transparency;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore_Tests.Infrastructure.Transparency;

[TestClass]
public class TransparencyServiceTests
{
    private TransparencyService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new TransparencyService();
    }

    [TestMethod]
    public void LogEvent_AddsEventToStore()
    {
        // Arrange
        var evt = new TransparencyEvent(TransparencyEventType.UserInput, "test data");

        // Act
        _service.LogEvent(evt);

        // Assert
        var events = _service.GetEvents();
        Assert.AreEqual(1, events.Count());
        Assert.AreSame(evt, events.First());
    }

    [TestMethod]
    public void LogEvent_RaisesEventLoggedEvent()
    {
        // Arrange
        var evt = new TransparencyEvent(TransparencyEventType.UserInput, "test data");
        TransparencyEvent? raisedEvent = null;
        _service.EventLogged += (sender, e) => raisedEvent = e;

        // Act
        _service.LogEvent(evt);

        // Assert
        Assert.IsNotNull(raisedEvent);
        Assert.AreSame(evt, raisedEvent);
    }

    [TestMethod]
    public void LogEvent_NullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _service.LogEvent(null!));
    }

    [TestMethod]
    public void GetEvents_ReturnsAllLoggedEvents()
    {
        // Arrange
        var evt1 = new TransparencyEvent(TransparencyEventType.UserInput, "data1");
        var evt2 = new TransparencyEvent(TransparencyEventType.AssistantResponse, "data2");
        var evt3 = new TransparencyEvent(TransparencyEventType.ToolCall, "data3");

        // Act
        _service.LogEvent(evt1);
        _service.LogEvent(evt2);
        _service.LogEvent(evt3);

        // Assert
        var events = _service.GetEvents().ToList();
        Assert.AreEqual(3, events.Count);
        Assert.IsTrue(events.Contains(evt1));
        Assert.IsTrue(events.Contains(evt2));
        Assert.IsTrue(events.Contains(evt3));
    }

    [TestMethod]
    public void GetEvents_ReturnsCopy_NotOriginalList()
    {
        // Arrange
        var evt = new TransparencyEvent(TransparencyEventType.UserInput, "data");
        _service.LogEvent(evt);

        // Act
        var events1 = _service.GetEvents();
        var events2 = _service.GetEvents();

        // Assert - Different list instances but same content
        Assert.AreNotSame(events1, events2);
        Assert.AreEqual(events1.Count(), events2.Count());
    }

    [TestMethod]
    public void GetEventsByType_FiltersCorrectly()
    {
        // Arrange
        var evt1 = new TransparencyEvent(TransparencyEventType.UserInput, "data1");
        var evt2 = new TransparencyEvent(TransparencyEventType.AssistantResponse, "data2");
        var evt3 = new TransparencyEvent(TransparencyEventType.UserInput, "data3");
        _service.LogEvent(evt1);
        _service.LogEvent(evt2);
        _service.LogEvent(evt3);

        // Act
        var userInputEvents = _service.GetEventsByType(TransparencyEventType.UserInput).ToList();

        // Assert
        Assert.AreEqual(2, userInputEvents.Count);
        Assert.IsTrue(userInputEvents.Contains(evt1));
        Assert.IsTrue(userInputEvents.Contains(evt3));
        Assert.IsFalse(userInputEvents.Contains(evt2));
    }

    [TestMethod]
    public void GetEventsByTimeRange_FiltersCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var evt1 = new TransparencyEvent(Guid.NewGuid(), TransparencyEventType.UserInput,
            startTime.AddMinutes(-2), "old event");
        var evt2 = new TransparencyEvent(Guid.NewGuid(), TransparencyEventType.UserInput,
            startTime.AddMinutes(1), "in range event");
        var evt3 = new TransparencyEvent(Guid.NewGuid(), TransparencyEventType.UserInput,
            startTime.AddMinutes(5), "future event");

        _service.LogEvent(evt1);
        _service.LogEvent(evt2);
        _service.LogEvent(evt3);

        // Act
        var rangeEvents = _service.GetEventsByTimeRange(startTime, startTime.AddMinutes(3)).ToList();

        // Assert
        Assert.AreEqual(1, rangeEvents.Count);
        Assert.AreSame(evt2, rangeEvents[0]);
    }

    [TestMethod]
    public void ClearEvents_RemovesAllEvents()
    {
        // Arrange
        _service.LogEvent(new TransparencyEvent(TransparencyEventType.UserInput, "data1"));
        _service.LogEvent(new TransparencyEvent(TransparencyEventType.UserInput, "data2"));
        Assert.AreEqual(2, _service.GetEvents().Count());

        // Act
        _service.ClearEvents();

        // Assert
        Assert.AreEqual(0, _service.GetEvents().Count());
    }

    [TestMethod]
    public void LogEvent_ThreadSafe_MultipleThreads()
    {
        // Arrange
        const int threadCount = 10;
        const int eventsPerThread = 100;
        var tasks = new Task[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < eventsPerThread; j++)
                {
                    var evt = new TransparencyEvent(TransparencyEventType.UserInput, $"data-{j}");
                    _service.LogEvent(evt);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert
        var events = _service.GetEvents();
        Assert.AreEqual(threadCount * eventsPerThread, events.Count());
    }
}
