using DashboardAPI.Services.Kafka.Retry;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Services
{
    public class KafkaRetryServiceTests
    {
        private readonly Mock<ILogger<KafkaRetryService>> _loggerMock;
        private readonly KafkaRetryService _kafkaRetryService;

        public KafkaRetryServiceTests()
        {
            _loggerMock = new Mock<ILogger<KafkaRetryService>>();
            _kafkaRetryService = new KafkaRetryService(_loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithSuccessfulOperation_ShouldReturnResult()
        {
            // Arrange
            var expectedResult = "test result";
            var operationName = "TestOperation";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedResult), operationName);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithSuccessfulVoidOperation_ShouldComplete()
        {
            // Arrange
            var operationName = "TestOperation";
            var operationCompleted = false;

            // Act
            await _kafkaRetryService.ExecuteWithRetryAsync(async () => 
            {
                operationCompleted = true;
                await Task.CompletedTask;
            }, operationName);

            // Assert
            Assert.True(operationCompleted);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationThatFailsThenSucceeds_ShouldRetryAndSucceed()
        {
            // Arrange
            var operationName = "TestOperation";
            var attemptCount = 0;
            var expectedResult = "success";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new Exception("Temporary failure");
                }
                return expectedResult;
            }, operationName);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(3, attemptCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationThatAlwaysFails_ShouldThrowException()
        {
            // Arrange
            var operationName = "TestOperation";
            var expectedException = new Exception("Permanent failure");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _kafkaRetryService.ExecuteWithRetryAsync(async () =>
                {
                    throw expectedException;
                }, operationName));

            Assert.Equal(expectedException, exception);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithNullOperationName_ShouldExecute()
        {
            // Arrange
            string? operationName = null;
            var expectedResult = "test result";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedResult), operationName);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithEmptyOperationName_ShouldExecute()
        {
            // Arrange
            var operationName = "";
            var expectedResult = "test result";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedResult), operationName);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithLongOperationName_ShouldExecute()
        {
            // Arrange
            var operationName = new string('a', 1000);
            var expectedResult = "test result";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedResult), operationName);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithSpecialCharactersInOperationName_ShouldExecute()
        {
            // Arrange
            var operationName = "Test!@#$%^&*()Operation";
            var expectedResult = "test result";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedResult), operationName);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithUnicodeCharactersInOperationName_ShouldExecute()
        {
            // Arrange
            var operationName = "ТестОперация";
            var expectedResult = "test result";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedResult), operationName);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithDifferentExceptionTypes_ShouldRetry()
        {
            // Arrange
            var operationName = "TestOperation";
            var attemptCount = 0;
            var expectedResult = "success";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new InvalidOperationException("Invalid operation");
                }
                if (attemptCount == 2)
                {
                    throw new ArgumentException("Invalid argument");
                }
                return expectedResult;
            }, operationName);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(3, attemptCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithCancelledOperation_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var operationName = "TestOperation";
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _kafkaRetryService.ExecuteWithRetryAsync(async () =>
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    return "result";
                }, operationName));
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithAsyncOperation_ShouldHandleAsyncCorrectly()
        {
            // Arrange
            var operationName = "TestOperation";
            var expectedResult = "async result";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(async () =>
            {
                await Task.Delay(10); // Simulate async work
                return expectedResult;
            }, operationName);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithVoidAsyncOperation_ShouldHandleAsyncCorrectly()
        {
            // Arrange
            var operationName = "TestOperation";
            var operationCompleted = false;

            // Act
            await _kafkaRetryService.ExecuteWithRetryAsync(async () =>
            {
                await Task.Delay(10); // Simulate async work
                operationCompleted = true;
            }, operationName);

            // Assert
            Assert.True(operationCompleted);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningNull_ShouldReturnNull()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult<string?>(null), operationName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(""), operationName);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningZero_ShouldReturnZero()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(0), operationName);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningFalse_ShouldReturnFalse()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(false), operationName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningTrue_ShouldReturnTrue()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(true), operationName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningComplexObject_ShouldReturnObject()
        {
            // Arrange
            var operationName = "TestOperation";
            var expectedObject = new { Id = 1, Name = "Test", Value = 123.45 };

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedObject), operationName);

            // Assert
            Assert.Equal(expectedObject, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningArray_ShouldReturnArray()
        {
            // Arrange
            var operationName = "TestOperation";
            var expectedArray = new[] { 1, 2, 3, 4, 5 };

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedArray), operationName);

            // Assert
            Assert.Equal(expectedArray, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningList_ShouldReturnList()
        {
            // Arrange
            var operationName = "TestOperation";
            var expectedList = new List<string> { "item1", "item2", "item3" };

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedList), operationName);

            // Assert
            Assert.Equal(expectedList, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOperationReturningDictionary_ShouldReturnDictionary()
        {
            // Arrange
            var operationName = "TestOperation";
            var expectedDictionary = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 },
                { "key3", true }
            };

            // Act
            var result = await _kafkaRetryService.ExecuteWithRetryAsync(() => Task.FromResult(expectedDictionary), operationName);

            // Assert
            Assert.Equal(expectedDictionary, result);
        }
    }
} 