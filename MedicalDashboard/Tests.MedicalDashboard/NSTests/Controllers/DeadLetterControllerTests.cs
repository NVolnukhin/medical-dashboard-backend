using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Controllers;
using NotificationService.Data.Models;
using NotificationService.Services.DeadLetter;
using Xunit;

namespace Tests.MedicalDashboard.NSTests.Controllers
{
    public class DeadLetterControllerTests
    {
        private readonly Mock<IDeadLetterService> _serviceMock;
        private readonly Mock<ILogger<DeadLetterController>> _loggerMock;
        private readonly DeadLetterController _controller;

        public DeadLetterControllerTests()
        {
            _serviceMock = new Mock<IDeadLetterService>();
            _loggerMock = new Mock<ILogger<DeadLetterController>>();
            _controller = new DeadLetterController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAllDeadLetters_ReturnsOk()
        {
            _serviceMock.Setup(s => s.GetAllDeadLettersAsync(default)).ReturnsAsync(new List<DeadLetterMessage> { new DeadLetterMessage() });
            var result = await _controller.GetAllDeadLetters();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<DeadLetterMessage>>(ok.Value);
            Assert.NotNull(list);
        }

        [Fact]
        public async Task GetAllDeadLetters_ReturnsInternalServerError_WhenExceptionThrown()
        {
            _serviceMock.Setup(s => s.GetAllDeadLettersAsync(default)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.GetAllDeadLetters();
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, obj.StatusCode);
            var res = Assert.IsType<NotificationResult>(obj.Value);
            Assert.False(res.Success);
            Assert.Equal("fail", res.ErrorMessage);
        }

        [Fact]
        public async Task GetUnprocessedDeadLetters_ReturnsOk()
        {
            _serviceMock.Setup(s => s.GetUnprocessedDeadLettersAsync(default)).ReturnsAsync(new List<DeadLetterMessage> { new DeadLetterMessage() });
            var result = await _controller.GetUnprocessedDeadLetters();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<DeadLetterMessage>>(ok.Value);
            Assert.NotNull(list);
        }

        [Fact]
        public async Task GetUnprocessedDeadLetters_ReturnsInternalServerError_WhenExceptionThrown()
        {
            _serviceMock.Setup(s => s.GetUnprocessedDeadLettersAsync(default)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.GetUnprocessedDeadLetters();
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, obj.StatusCode);
            var res = Assert.IsType<NotificationResult>(obj.Value);
            Assert.False(res.Success);
            Assert.Equal("fail", res.ErrorMessage);
        }

        [Fact]
        public async Task ProcessDeadLetter_ReturnsOk_WhenMessageProcessed()
        {
            var id = Guid.NewGuid();
            var msg = new DeadLetterMessage { Id = id };
            _serviceMock.Setup(s => s.ProcessDeadLetterAsync(id, default)).ReturnsAsync(msg);
            var result = await _controller.ProcessDeadLetter(id);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var res = Assert.IsType<DeadLetterMessage>(ok.Value);
            Assert.Equal(id, res.Id);
        }

        [Fact]
        public async Task ProcessDeadLetter_ReturnsNotFound_WhenKeyNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.ProcessDeadLetterAsync(id, default)).ThrowsAsync(new KeyNotFoundException());
            var result = await _controller.ProcessDeadLetter(id);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            var res = Assert.IsType<NotificationResult>(notFound.Value);
            Assert.False(res.Success);
        }

        [Fact]
        public async Task ProcessDeadLetter_ReturnsInternalServerError_WhenExceptionThrown()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.ProcessDeadLetterAsync(id, default)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.ProcessDeadLetter(id);
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, obj.StatusCode);
            var res = Assert.IsType<NotificationResult>(obj.Value);
            Assert.False(res.Success);
            Assert.Equal("fail", res.ErrorMessage);
        }
    }
} 