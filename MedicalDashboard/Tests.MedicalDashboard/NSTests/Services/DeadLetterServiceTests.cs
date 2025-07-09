using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Data.Models;
using NotificationService.Repositories.DeadLetter;
using NotificationService.Services.DeadLetter;

namespace Tests.MedicalDashboard.NSTests.Services
{
    public class DeadLetterServiceTests
    {
        private readonly Mock<IDeadLetterRepository> _repoMock;
        private readonly Mock<ILogger<DeadLetterService>> _loggerMock;
        private readonly DeadLetterService _service;

        public DeadLetterServiceTests()
        {
            _repoMock = new Mock<IDeadLetterRepository>();
            _loggerMock = new Mock<ILogger<DeadLetterService>>();
            _service = new DeadLetterService(_repoMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task PublishToDeadLetterQueueAsync_AddsMessage()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<DeadLetterMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync((DeadLetterMessage msg, CancellationToken _) => msg).Verifiable();
            await _service.PublishToDeadLetterQueueAsync("topic", "{}", "err", "recv");
            _repoMock.Verify();
        }

        [Fact]
        public async Task PublishToDeadLetterQueueAsync_Throws_WhenRepoThrows()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<DeadLetterMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fail"));
            await Assert.ThrowsAsync<Exception>(() => _service.PublishToDeadLetterQueueAsync("topic", "{}", "err", "recv"));
        }

        [Fact]
        public async Task GetAllDeadLettersAsync_ReturnsList()
        {
            _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<DeadLetterMessage> { new DeadLetterMessage() });
            var result = await _service.GetAllDeadLettersAsync();
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetAllDeadLettersAsync_Throws_WhenRepoThrows()
        {
            _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fail"));
            await Assert.ThrowsAsync<Exception>(() => _service.GetAllDeadLettersAsync());
        }

        [Fact]
        public async Task GetUnprocessedDeadLettersAsync_ReturnsList()
        {
            _repoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<DeadLetterMessage> { new DeadLetterMessage() });
            var result = await _service.GetUnprocessedDeadLettersAsync();
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetUnprocessedDeadLettersAsync_Throws_WhenRepoThrows()
        {
            _repoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fail"));
            await Assert.ThrowsAsync<Exception>(() => _service.GetUnprocessedDeadLettersAsync());
        }

        [Fact]
        public async Task ProcessDeadLetterAsync_ReturnsMessage()
        {
            var id = Guid.NewGuid();
            var msg = new DeadLetterMessage { Id = id };
            _repoMock.Setup(r => r.MarkAsProcessedAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(msg);
            var result = await _service.ProcessDeadLetterAsync(id);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task ProcessDeadLetterAsync_ThrowsKeyNotFound()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.MarkAsProcessedAsync(id, It.IsAny<CancellationToken>())).ThrowsAsync(new KeyNotFoundException());
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ProcessDeadLetterAsync(id));
        }

        [Fact]
        public async Task ProcessDeadLetterAsync_Throws_WhenRepoThrows()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.MarkAsProcessedAsync(id, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fail"));
            await Assert.ThrowsAsync<Exception>(() => _service.ProcessDeadLetterAsync(id));
        }
    }
} 