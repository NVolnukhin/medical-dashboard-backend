using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Config;
using NotificationService.Services.Retry;

namespace Tests.MedicalDashboard.NSTests.Services
{
    public class RetryServiceTests
    {
        private readonly Mock<ILogger<RetryService>> _loggerMock = new();
        private readonly Mock<IOptions<RetrySettings>> _settingsMock = new();
        private readonly RetryService _service;

        public RetryServiceTests()
        {
            _settingsMock.Setup(s => s.Value).Returns(new RetrySettings { MaxRetryAttempts = 2, OperationTimeoutSeconds = 1 });
            _service = new RetryService(_loggerMock.Object, _settingsMock.Object);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_ReturnsResult_WhenSuccess()
        {
            var result = await _service.ExecuteWithRetryAsync(async () => 42, "op");
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_RetriesOnTimeout()
        {
            int count = 0;
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _service.ExecuteWithRetryAsync<int>(async () => { count++; await Task.CompletedTask; throw new TimeoutException(); }, "op");
            });
            Assert.Equal(_settingsMock.Object.Value.MaxRetryAttempts, count);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_Throws_WhenMaxAttemptsExceeded()
        {
            await Assert.ThrowsAsync<Exception>(() => _service.ExecuteWithRetryAsync<int>(async () => { await Task.CompletedTask; throw new Exception("fail"); }, "op"));
        }
    }
} 