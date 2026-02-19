using Microsoft.Extensions.Logging;
using Moq;
using Shared.Extensions.Logging;
using Xunit;

namespace Tests.MedicalDashboard.SharedTests
{
    public class LoggerExtensionsTests
    {
        private readonly Mock<ILogger> _loggerMock;

        public LoggerExtensionsTests()
        {
            _loggerMock = new Mock<ILogger>();
        }

        [Fact]
        public void LogSuccess_ShouldLogInformationWithSuccessFormat()
        {
            // Arrange
            var message = "Test success message";

            // Act
            _loggerMock.Object.LogSuccess(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SUCCESS") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogSuccess_WithEmptyMessage_ShouldLogInformation()
        {
            // Arrange
            var message = "";

            // Act
            _loggerMock.Object.LogSuccess(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SUCCESS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogSuccess_WithNullMessage_ShouldLogInformation()
        {
            // Arrange
            string? message = null;

            // Act
            _loggerMock.Object.LogSuccess(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SUCCESS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogSuccess_WithSpecialCharacters_ShouldLogInformation()
        {
            // Arrange
            var message = "Test!@#$%^&*()message";

            // Act
            _loggerMock.Object.LogSuccess(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SUCCESS") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogSuccess_WithUnicodeCharacters_ShouldLogInformation()
        {
            // Arrange
            var message = "Тестовое сообщение";

            // Act
            _loggerMock.Object.LogSuccess(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SUCCESS") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogWarning_ShouldLogInformationWithWarningFormat()
        {
            // Arrange
            var message = "Test warning message";

            // Act
            _loggerMock.Object.LogWarning(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[WARN") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogWarning_WithEmptyMessage_ShouldLogInformation()
        {
            // Arrange
            var message = "";

            // Act
            _loggerMock.Object.LogWarning(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[WARN")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogWarning_WithNullMessage_ShouldLogInformation()
        {
            // Arrange
            string? message = null;

            // Act
            _loggerMock.Object.LogWarning(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[WARN")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithMessageOnly_ShouldLogErrorWithFailureFormat()
        {
            // Arrange
            var message = "Test failure message";

            // Act
            _loggerMock.Object.LogFailure(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithException_ShouldLogErrorWithExceptionMessage()
        {
            // Arrange
            var message = "Test failure message";
            var exception = new Exception("Test exception message");

            // Act
            _loggerMock.Object.LogFailure(message, exception);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED") && v.ToString().Contains(exception.Message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithNullException_ShouldLogErrorWithMessage()
        {
            // Arrange
            var message = "Test failure message";
            Exception? exception = null;

            // Act
            _loggerMock.Object.LogFailure(message, exception);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithEmptyMessage_ShouldLogError()
        {
            // Arrange
            var message = "";

            // Act
            _loggerMock.Object.LogFailure(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithNullMessage_ShouldLogError()
        {
            // Arrange
            string? message = null;

            // Act
            _loggerMock.Object.LogFailure(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithExceptionAndEmptyMessage_ShouldLogErrorWithExceptionMessage()
        {
            // Arrange
            var message = "";
            var exception = new Exception("Test exception message");

            // Act
            _loggerMock.Object.LogFailure(message, exception);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED") && v.ToString().Contains(exception.Message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogInfo_ShouldLogInformationWithInfoFormat()
        {
            // Arrange
            var message = "Test info message";

            // Act
            _loggerMock.Object.LogInfo(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[INFO") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogInfo_WithEmptyMessage_ShouldLogInformation()
        {
            // Arrange
            var message = "";

            // Act
            _loggerMock.Object.LogInfo(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[INFO")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogInfo_WithNullMessage_ShouldLogInformation()
        {
            // Arrange
            string? message = null;

            // Act
            _loggerMock.Object.LogInfo(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[INFO")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogInfo_WithSpecialCharacters_ShouldLogInformation()
        {
            // Arrange
            var message = "Test!@#$%^&*()info";

            // Act
            _loggerMock.Object.LogInfo(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[INFO") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogInfo_WithUnicodeCharacters_ShouldLogInformation()
        {
            // Arrange
            var message = "Тестовая информация";

            // Act
            _loggerMock.Object.LogInfo(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[INFO") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithComplexException_ShouldLogErrorWithExceptionMessage()
        {
            // Arrange
            var message = "Test failure message";
            var innerException = new InvalidOperationException("Inner exception");
            var exception = new Exception("Outer exception", innerException);

            // Act
            _loggerMock.Object.LogFailure(message, exception);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED") && v.ToString().Contains(exception.Message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithExceptionHavingNullMessage_ShouldLogError()
        {
            // Arrange
            var message = "Test failure message";
            var exception = new Exception(null);

            // Act
            _loggerMock.Object.LogFailure(message, exception);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogSuccess_WithLongMessage_ShouldLogInformation()
        {
            // Arrange
            var message = new string('a', 1000);

            // Act
            _loggerMock.Object.LogSuccess(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SUCCESS") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogWarning_WithLongMessage_ShouldLogInformation()
        {
            // Arrange
            var message = new string('w', 1000);

            // Act
            _loggerMock.Object.LogWarning(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[WARN") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogFailure_WithLongMessage_ShouldLogError()
        {
            // Arrange
            var message = new string('f', 1000);

            // Act
            _loggerMock.Object.LogFailure(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[FAILED") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void LogInfo_WithLongMessage_ShouldLogInformation()
        {
            // Arrange
            var message = new string('i', 1000);

            // Act
            _loggerMock.Object.LogInfo(message);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[INFO") && v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }
} 