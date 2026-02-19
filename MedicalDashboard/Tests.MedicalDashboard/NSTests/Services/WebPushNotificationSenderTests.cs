using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Enums;
using NotificationService.Hubs;
using NotificationService.WebPush.Sender;
using Xunit;

namespace Tests.MedicalDashboard.NSTests.Services;

public class WebPushNotificationSenderTests
{
    private readonly Mock<IHubContext<AlertsHub>> _hubContextMock;
    private readonly Mock<ILogger<WebPushNotificationSender>> _loggerMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly WebPushNotificationSender _sender;

    public WebPushNotificationSenderTests()
    {
        _hubContextMock = new Mock<IHubContext<AlertsHub>>();
        _loggerMock = new Mock<ILogger<WebPushNotificationSender>>();
        _clientProxyMock = new Mock<IClientProxy>();
        _hubClientsMock = new Mock<IHubClients>();

        _hubClientsMock.Setup(x => x.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(x => x.Clients).Returns(_hubClientsMock.Object);

        _sender = new WebPushNotificationSender(_hubContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Type_ShouldReturnWebPush()
    {
        // Act
        var result = _sender.Type;

        // Assert
        Assert.Equal(NotificationType.WebPush, result);
    }

    [Fact]
    public async Task SendAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithInvalidEndpoint_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "invalid-endpoint";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithSpecialCharacters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = "Test Subject with special chars: !@#$%^&*()";
        var body = "Test Body with special chars: !@#$%^&*()";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithUnicodeCharacters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = "Тестовый заголовок с кириллицей";
        var body = "Тестовое сообщение с кириллицей и эмодзи 🚀";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithLongSubject_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = new string('A', 1000);
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithLongBody_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = "Test Subject";
        var body = new string('B', 1000);

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithHttpsEndpoint_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://secure.example.com/push";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithHttpEndpoint_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "http://example.com/push";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithFcmEndpoint_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://fcm.googleapis.com/fcm/send";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithVeryLongSubject_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = new string('A', 10000);
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithVeryLongBody_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = "Test Subject";
        var body = new string('B', 10000);

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithVeryLongEndpoint_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://" + new string('a', 1000) + ".example.com/push";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = "Test Subject";
        var body = "Test Body";
        var cancellationToken = new CancellationToken(true); // Already cancelled

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _sender.SendAsync(recipient, subject, body, cancellationToken));
    }

    [Fact]
    public async Task SendAsync_WithSignalRException_ShouldThrowException()
    {
        // Arrange
        var recipient = "https://example.com/push";
        var subject = "Test Subject";
        var body = "Test Body";
        var expectedException = new InvalidOperationException("SignalR error");

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sender.SendAsync(recipient, subject, body));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task SendAsync_WithQueryParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push?param1=value1&param2=value2";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithFragment_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/push#fragment";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithSubdomain_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://sub.example.com/push";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithPort_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com:8080/push";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithPath_ShouldCompleteSuccessfully()
    {
        // Arrange
        var recipient = "https://example.com/deep/path/to/push";
        var subject = "Test Subject";
        var body = "Test Body";

        _clientProxyMock.Setup(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sender.SendAsync(recipient, subject, body);

        // Assert
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveAlert", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == body), It.IsAny<CancellationToken>()), Times.Once);
    }
} 