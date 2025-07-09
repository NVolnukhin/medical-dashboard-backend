using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Interfaces;
using NotificationService.Repositories.Template;

namespace Tests.MedicalDashboard.NSTests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationSender> _senderMock;
        private readonly Mock<INotificationTemplateRepository> _templateRepoMock;
        private readonly Mock<ILogger<NotificationService.Services.Notification.NotificationService>> _loggerMock;
        private readonly NotificationService.Services.Notification.NotificationService _service;

        public NotificationServiceTests()
        {
            _senderMock = new Mock<INotificationSender>();
            _templateRepoMock = new Mock<INotificationTemplateRepository>();
            _loggerMock = new Mock<ILogger<NotificationService.Services.Notification.NotificationService>>();
            _service = new NotificationService.Services.Notification.NotificationService(new[] { _senderMock.Object }, _templateRepoMock.Object, _loggerMock.Object);
            _senderMock.SetupGet(s => s.Type).Returns(NotificationType.Email);
        }

        [Fact]
        public async Task SendNotificationAsync_ReturnsSuccess_WhenSenderExists()
        {
            _senderMock.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),  It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var request = new NotificationRequest { Type = NotificationType.Email, Recipient = "test@example.com", Subject = "subj", Body = "body" };
            var result = await _service.SendNotificationAsync(request);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task SendNotificationAsync_ReturnsError_WhenSenderNotFound()
        {
            var service = new NotificationService.Services.Notification.NotificationService(new List<INotificationSender>(), _templateRepoMock.Object, _loggerMock.Object);
            var request = new NotificationRequest { Type = NotificationType.Sms, Recipient = "test@example.com", Subject = "subj", Body = "body" };
            var result = await service.SendNotificationAsync(request);
            Assert.False(result.Success);
            Assert.Contains("не найден", result.ErrorMessage);
        }

        [Fact]
        public async Task SendNotificationAsync_ReturnsError_WhenTemplateNotFound()
        {
            var request = new NotificationRequest { Type = NotificationType.Email, Recipient = "test@example.com", Subject = "subj", Body = "body", TemplateName = "tpl" };
            _templateRepoMock.Setup(r => r.GetBySubjectAndTypeAsync("tpl", NotificationType.Email, It.IsAny<CancellationToken>())).ReturnsAsync((NotificationTemplate)null);
            var result = await _service.SendNotificationAsync(request);
            Assert.False(result.Success);
            Assert.Contains("not found", result.ErrorMessage);
        }

        [Fact]
        public async Task SendNotificationAsync_UsesTemplate_WhenTemplateExists()
        {
            var template = new NotificationTemplate { Subject = "subj {name}", Body = "body {val}", Type = NotificationType.Email };
            _templateRepoMock.Setup(r => r.GetBySubjectAndTypeAsync("tpl", NotificationType.Email, It.IsAny<CancellationToken>())).ReturnsAsync(template);
            _senderMock.Setup(s => s.SendAsync("test@example.com", "subj test", "body 123",  It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
            var request = new NotificationRequest { Type = NotificationType.Email, Recipient = "test@example.com", TemplateName = "tpl", TemplateParameters = new Dictionary<string, string> { { "name", "test" }, { "val", "123" } } };
            var result = await _service.SendNotificationAsync(request);
            Assert.True(result.Success);
            _senderMock.Verify();
        }

        [Fact]
        public async Task SendNotificationAsync_ReturnsError_WhenSenderThrows()
        {
            _senderMock.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),  It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fail"));
            var request = new NotificationRequest { Type = NotificationType.Email, Recipient = "test@example.com", Subject = "subj", Body = "body" };
            var result = await _service.SendNotificationAsync(request);
            Assert.False(result.Success);
            Assert.Equal("fail", result.ErrorMessage);
        }

        [Fact]
        public void ReplaceTemplateParameters_ReplacesAll()
        {
            var privateMethod = typeof(NotificationService.Services.Notification.NotificationService).GetMethod("ReplaceTemplateParameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parameters = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };
            var result = (string)privateMethod.Invoke(_service, new object[] { "{a} {b} {a}", parameters });
            Assert.Equal("1 2 1", result);
        }
    }
} 