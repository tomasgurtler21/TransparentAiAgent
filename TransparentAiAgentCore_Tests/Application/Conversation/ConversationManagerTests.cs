using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore_Tests.Application.Conversation
{
    [TestClass]
    public class ConversationManagerTests
    {
        private ITransparencyService _transparencyService;

        [TestInitialize]
        public void Setup()
        {
            _transparencyService = new TransparencyService();
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_NegativeContextWindowSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new ConversationManager(-1, _transparencyService));
        }

        [TestMethod]
        public void Constructor_ZeroContextWindowSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new ConversationManager(0, _transparencyService));
        }

        [TestMethod]
        public void Constructor_NullTransparencyService_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new ConversationManager(10, null!));
        }

        [TestMethod]
        public void Constructor_ValidParameters_GeneratesUniqueConversationId()
        {
            var manager1 = new ConversationManager(10, _transparencyService);
            var manager2 = new ConversationManager(10, _transparencyService);

            Assert.AreNotEqual(Guid.Empty, manager1.ConversationId);
            Assert.AreNotEqual(manager1.ConversationId, manager2.ConversationId);
        }

        [TestMethod]
        public void Constructor_ValidParameters_LogsConversationStartedEvent()
        {
            var manager = new ConversationManager(10, _transparencyService);

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("Conversation") && e.AdditionalInfo.Contains("started")));
        }

        #endregion

        #region AddMessage Tests

        [TestMethod]
        public void AddMessage_NullMessage_ThrowsArgumentNullException()
        {
            var manager = new ConversationManager(10, _transparencyService);

            Assert.ThrowsException<ArgumentNullException>(() =>
                manager.AddMessage(null!));
        }

        [TestMethod]
        public void AddMessage_ValidMessage_AddsToHistory()
        {
            var manager = new ConversationManager(10, _transparencyService);
            var message = new UserMessage("Hello");

            manager.AddMessage(message);

            var allMessages = manager.GetAllMessages();
            Assert.AreEqual(1, allMessages.Count);
            Assert.AreEqual(message.Id, allMessages[0].Id);
        }

        [TestMethod]
        public void AddMessage_ValidMessage_LogsMessageAddedEvent()
        {
            var manager = new ConversationManager(10, _transparencyService);
            _transparencyService.ClearEvents(); // Clear constructor event

            var message = new UserMessage("Hello");
            manager.AddMessage(message);

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("Message") && e.AdditionalInfo.Contains("added")));
        }

        #endregion

        #region GetAllMessages Tests

        [TestMethod]
        public void GetAllMessages_ReturnsAllMessagesIncludingTruncated()
        {
            var manager = new ConversationManager(2, _transparencyService);

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3); // This should trigger truncation

            var allMessages = manager.GetAllMessages();
            Assert.AreEqual(3, allMessages.Count);
        }

        [TestMethod]
        public void GetAllMessages_ReturnsDefensiveCopy()
        {
            var manager = new ConversationManager(10, _transparencyService);
            var message = new UserMessage("Hello");
            manager.AddMessage(message);

            var messages1 = manager.GetAllMessages();
            var messages2 = manager.GetAllMessages();

            // Should be different list instances
            Assert.AreNotSame(messages1, messages2);
        }

        #endregion

        #region GetInContextMessages Tests

        [TestMethod]
        public void GetInContextMessages_ReturnsOnlyInContextMessages()
        {
            var manager = new ConversationManager(2, _transparencyService);

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3); // This should trigger truncation of msg1

            var inContextMessages = manager.GetInContextMessages();
            Assert.AreEqual(2, inContextMessages.Count);
            Assert.IsTrue(inContextMessages.All(m => m.ContextStatus == MessageContextStatus.InContext));
        }

        [TestMethod]
        public void GetInContextMessages_ExcludesTruncatedMessages()
        {
            var manager = new ConversationManager(2, _transparencyService);

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3); // This should trigger truncation of msg1

            var inContextMessages = manager.GetInContextMessages();
            Assert.IsFalse(inContextMessages.Any(m => m.Id == msg1.Id));
        }

        #endregion

        #region InContextMessageCount Tests

        [TestMethod]
        public void InContextMessageCount_ReturnsCorrectCount()
        {
            var manager = new ConversationManager(3, _transparencyService);

            Assert.AreEqual(0, manager.InContextMessageCount);

            manager.AddMessage(new UserMessage("One"));
            Assert.AreEqual(1, manager.InContextMessageCount);

            manager.AddMessage(new UserMessage("Two"));
            Assert.AreEqual(2, manager.InContextMessageCount);

            manager.AddMessage(new UserMessage("Three"));
            Assert.AreEqual(3, manager.InContextMessageCount);
        }

        #endregion

        #region Truncation Tests

        [TestMethod]
        public void AddMessage_UnderContextLimit_DoesNotTruncate()
        {
            var manager = new ConversationManager(3, _transparencyService);

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);

            Assert.AreEqual(MessageContextStatus.InContext, msg1.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg2.ContextStatus);
        }

        [TestMethod]
        public void AddMessage_ExceedsContextLimit_TruncatesOldestMessage()
        {
            var manager = new ConversationManager(2, _transparencyService);

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3);

            Assert.AreEqual(MessageContextStatus.TruncatedFromContext, msg1.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg2.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg3.ContextStatus);
        }

        [TestMethod]
        public void AddMessage_TruncatesMultipleMessages_WhenNeeded()
        {
            var manager = new ConversationManager(2, _transparencyService);

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");
            var msg4 = new UserMessage("Four");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3);
            manager.AddMessage(msg4);

            Assert.AreEqual(MessageContextStatus.TruncatedFromContext, msg1.ContextStatus);
            Assert.AreEqual(MessageContextStatus.TruncatedFromContext, msg2.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg3.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg4.ContextStatus);
        }

        [TestMethod]
        public void AddMessage_Truncation_RaisesContextStatusChangedEvent()
        {
            var manager = new ConversationManager(2, _transparencyService);
            var eventRaised = false;
            Guid? changedMessageId = null;

            manager.ContextStatusChanged += (sender, args) =>
            {
                eventRaised = true;
                changedMessageId = args.MessageId;
            };

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3); // Should trigger truncation

            Assert.IsTrue(eventRaised);
            Assert.AreEqual(msg1.Id, changedMessageId);
        }

        [TestMethod]
        public void AddMessage_Truncation_EventHasCorrectStatusTransition()
        {
            var manager = new ConversationManager(2, _transparencyService);
            MessageContextStatus? oldStatus = null;
            MessageContextStatus? newStatus = null;

            manager.ContextStatusChanged += (sender, args) =>
            {
                oldStatus = args.OldStatus;
                newStatus = args.NewStatus;
            };

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3);

            Assert.AreEqual(MessageContextStatus.InContext, oldStatus);
            Assert.AreEqual(MessageContextStatus.TruncatedFromContext, newStatus);
        }

        [TestMethod]
        public void AddMessage_Truncation_LogsTruncationEvents()
        {
            var manager = new ConversationManager(2, _transparencyService);
            _transparencyService.ClearEvents();

            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3);

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("truncated")));
        }

        [TestMethod]
        public void AddMessage_PreservesSystemMessages_TruncatesOthersFirst()
        {
            var manager = new ConversationManager(3, _transparencyService);

            var systemMsg = new SystemMessage("System prompt");
            var msg1 = new UserMessage("One");
            var msg2 = new UserMessage("Two");
            var msg3 = new UserMessage("Three");

            manager.AddMessage(systemMsg);
            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3); // Should truncate msg1, not systemMsg

            Assert.AreEqual(MessageContextStatus.InContext, systemMsg.ContextStatus);
            Assert.AreEqual(MessageContextStatus.TruncatedFromContext, msg1.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg2.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg3.ContextStatus);
        }

        [TestMethod]
        public void AddMessage_TruncatesInTimestampOrder_ForSamePriority()
        {
            var manager = new ConversationManager(2, _transparencyService);

            var msg1 = new UserMessage("One");
            Thread.Sleep(10); // Ensure different timestamps
            var msg2 = new UserMessage("Two");
            Thread.Sleep(10);
            var msg3 = new UserMessage("Three");

            manager.AddMessage(msg1);
            manager.AddMessage(msg2);
            manager.AddMessage(msg3);

            // msg1 is oldest, should be truncated first
            Assert.AreEqual(MessageContextStatus.TruncatedFromContext, msg1.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg2.ContextStatus);
            Assert.AreEqual(MessageContextStatus.InContext, msg3.ContextStatus);
        }

        #endregion

        #region ClearConversation Tests

        [TestMethod]
        public void ClearConversation_RemovesAllMessages()
        {
            var manager = new ConversationManager(10, _transparencyService);

            manager.AddMessage(new UserMessage("One"));
            manager.AddMessage(new UserMessage("Two"));

            manager.ClearConversation();

            Assert.AreEqual(0, manager.GetAllMessages().Count);
        }

        [TestMethod]
        public void ClearConversation_LogsClearedEvent()
        {
            var manager = new ConversationManager(10, _transparencyService);
            manager.AddMessage(new UserMessage("One"));
            _transparencyService.ClearEvents();

            manager.ClearConversation();

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("cleared")));
        }

        #endregion

        #region Thread Safety Tests

        [TestMethod]
        public void AddMessage_ConcurrentCalls_DoNotCorruptState()
        {
            var manager = new ConversationManager(100, _transparencyService);
            var tasks = new List<Task>();

            for (int i = 0; i < 50; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    manager.AddMessage(new UserMessage($"Message {index}"));
                }));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.AreEqual(50, manager.GetAllMessages().Count);
        }

        #endregion
    }
}
