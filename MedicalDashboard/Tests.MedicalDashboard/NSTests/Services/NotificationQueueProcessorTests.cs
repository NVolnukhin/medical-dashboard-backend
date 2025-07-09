using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Config;
using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Services.DeadLetter;
using NotificationService.Services.Notification;
using NotificationService.Services.Queue;

namespace Tests.MedicalDashboard.NSTests.Services
{
    public class NotificationQueueProcessorTests
    {
        private readonly Mock<IPriorityNotificationQueue> _queueMock = new();
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
        private readonly Mock<ILogger<NotificationQueueProcessor>> _loggerMock = new();
        private readonly Mock<IOptions<QueueSettings>> _settingsMock = new();
        private readonly NotificationQueueProcessor _processor;

        public NotificationQueueProcessorTests()
        {
            _settingsMock.Setup(s => s.Value).Returns(new QueueSettings { ProcessingIntervalMs = 1 });
            _processor = new NotificationQueueProcessor(_queueMock.Object, _scopeFactoryMock.Object, _loggerMock.Object, _settingsMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_HandlesQueueAndLogs()
        {
            var notification = new NotificationRequest { Recipient = "test", Priority = NotificationPriority.Normal };
            _queueMock.Setup(q => q.TryDequeueAsync()).ReturnsAsync((true, notification));
            var scopeMock = new Mock<IServiceScope>();
            var providerMock = new Mock<IServiceProvider>();
            var notifServiceMock = new Mock<INotificationService>();
            var deadLetterMock = new Mock<IDeadLetterService>();
            notifServiceMock.Setup(n => n.SendNotificationAsync(notification, It.IsAny<CancellationToken>())).ReturnsAsync(new NotificationResult { Success = true });
            providerMock.Setup(p => p.GetService(typeof(INotificationService))).Returns(notifServiceMock.Object);
            providerMock.Setup(p => p.GetService(typeof(IDeadLetterService))).Returns(deadLetterMock.Object);
            scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);
            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
            var token = new CancellationTokenSource(10).Token;
            await _processor.StartAsync(token);
            await Task.Delay(20);
            await _processor.StopAsync(token);
            _loggerMock.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }
    }
} 