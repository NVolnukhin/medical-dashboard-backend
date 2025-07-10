using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Config;
using NotificationService.Email.Sender;
using NotificationService.Enums;
using NotificationService.Services.Retry;
using Xunit;

namespace Tests.MedicalDashboard.NSTests.Services;

public class EmailNotificationSenderTests
{
    private readonly Mock<IOptions<SmtpSettings>> _mockSmtpSettings;
    private readonly Mock<ILogger<EmailNotificationSender>> _mockLogger;
    private readonly Mock<IRetryService> _mockRetryService;
    private readonly EmailNotificationSender _sender;

    public EmailNotificationSenderTests()
    {
        _mockSmtpSettings = new Mock<IOptions<SmtpSettings>>();
        _mockLogger = new Mock<ILogger<EmailNotificationSender>>();
        _mockRetryService = new Mock<IRetryService>();

        var smtpSettings = new SmtpSettings
        {
            Host = "smtp.example.com",
            Port = 587,
            Username = "test@example.com",
            Password = "password",
            From = "noreply@example.com",
            EnableSsl = true
        };

        _mockSmtpSettings.Setup(x => x.Value).Returns(smtpSettings);

        _sender = new EmailNotificationSender(_mockSmtpSettings.Object, _mockLogger.Object, _mockRetryService.Object);
    }

    [Fact]
    public void Type_ShouldReturnEmail()
    {
        // Act & Assert
        Assert.Equal(NotificationType.Email, _sender.Type);
    }

    [Fact]
    public async Task SendAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithInvalidEmailFormat_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "invalid-email-format";
        var subject = "Test Subject";
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithSpecialCharacters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test+tag@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithUnicodeCharacters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "тест@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithLongSubject_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = new string('A', 1000);
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithLongBody_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = new string('A', 10000);

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNewlinesInBody_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Line 1\nLine 2\r\nLine 3";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithTabsInBody_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Column1\tColumn2\tColumn3";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithHtmlBody_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "<html><body><h1>Test</h1><p>This is a test email.</p></body></html>";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMultipleRecipients_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "recipient1@example.com,recipient2@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithVeryLongEmail_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = new string('A', 100000);

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithVeryLongRecipient_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = new string('a', 1000) + "@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithVeryLongSubject_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = new string('A', 10000);
        var body = "Test Body";

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _mockRetryService.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), $"Отправка письма на почту {recipient}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithRetryServiceException_ShouldThrowException()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";
        var expectedException = new Exception("Retry service error");

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _sender.SendAsync(recipient, subject, body));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";
        var cancellationToken = new CancellationToken(true); // Already cancelled

        _mockRetryService.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Task>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _sender.SendAsync(recipient, subject, body, cancellationToken));
    }
} 