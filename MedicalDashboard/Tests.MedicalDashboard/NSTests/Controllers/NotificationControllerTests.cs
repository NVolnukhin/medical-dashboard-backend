using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Controllers;
using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Services.Queue;
using Xunit;

namespace Tests.MedicalDashboard.NSTests.Controllers
{
    public class NotificationControllerTests
    {
        private readonly Mock<IPriorityNotificationQueue> _queueMock;
        private readonly Mock<ILogger<NotificationController>> _loggerMock;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _queueMock = new Mock<IPriorityNotificationQueue>();
            _loggerMock = new Mock<ILogger<NotificationController>>();
            _controller = new NotificationController(_queueMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Notify_ReturnsOk_WhenRequestIsValid()
        {
            // Arrange
            var request = new NotificationRequest
            {
                Type = NotificationType.Email,
                Recipient = "test@example.com",
                Subject = "Test",
                Body = "Body",
                Priority = NotificationPriority.Normal
            };

            // Act
            var result = await _controller.Notify(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var notificationResult = Assert.IsType<NotificationResult>(okResult.Value);
            Assert.True(notificationResult.Success);
            _queueMock.Verify(q => q.EnqueueAsync(It.IsAny<NotificationRequest>()), Times.Once);
        }

        [Fact]
        public async Task Notify_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var request = new NotificationRequest();
            _controller.ModelState.AddModelError("Recipient", "Required");

            // Act
            var result = await _controller.Notify(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var notificationResult = Assert.IsType<NotificationResult>(badRequest.Value);
            Assert.False(notificationResult.Success);
            Assert.Equal("Некорректные данные запроса", notificationResult.ErrorMessage);
            _queueMock.Verify(q => q.EnqueueAsync(It.IsAny<NotificationRequest>()), Times.Never);
        }

        [Fact]
        public async Task Notify_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var request = new NotificationRequest
            {
                Type = NotificationType.Email,
                Recipient = "test@example.com",
                Subject = "Test",
                Body = "Body",
                Priority = NotificationPriority.Normal
            };
            _queueMock.Setup(q => q.EnqueueAsync(It.IsAny<NotificationRequest>())).ThrowsAsync(new System.Exception("fail"));

            // Act
            var result = await _controller.Notify(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var notificationResult = Assert.IsType<NotificationResult>(objectResult.Value);
            Assert.False(notificationResult.Success);
            Assert.Equal("fail", notificationResult.ErrorMessage);
        }

        [Fact]
        public void GetNotificationTypes_ReturnsOk()
        {
            // Act
            var result = _controller.GetNotificationTypes();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var types = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.NotEmpty(types);
        }

        [Fact]
        public void GetNotificationPriorities_ReturnsOk()
        {
            // Act
            var result = _controller.GetNotificationPriorities();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var priorities = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.NotEmpty(priorities);
        }
    }
} 