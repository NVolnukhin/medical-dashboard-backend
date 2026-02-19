using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Services.Queue;
using Xunit;

namespace Tests.MedicalDashboard.NSTests.Services
{
    public class PriorityNotificationQueueTests
    {
        private readonly Mock<ILogger<PriorityNotificationQueue>> _loggerMock = new();
        private readonly PriorityNotificationQueue _queue;

        public PriorityNotificationQueueTests()
        {
            _queue = new PriorityNotificationQueue(_loggerMock.Object);
        }

        [Fact]
        public async Task EnqueueAsync_AddsToQueue()
        {
            var req = new NotificationRequest { Recipient = "test", Priority = NotificationPriority.High };
            await _queue.EnqueueAsync(req);
            var count = await _queue.GetCountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task TryDequeueAsync_ReturnsItem()
        {
            var req = new NotificationRequest { Recipient = "test", Priority = NotificationPriority.High };
            await _queue.EnqueueAsync(req);
            var (success, notification) = await _queue.TryDequeueAsync();
            Assert.True(success);
            Assert.NotNull(notification);
        }

        [Fact]
        public async Task GetCountAsync_ReturnsCorrectCount()
        {
            var req = new NotificationRequest { Recipient = "test", Priority = NotificationPriority.High };
            await _queue.EnqueueAsync(req);
            var count = await _queue.GetCountAsync();
            Assert.Equal(1, count);
        }
    }
} 