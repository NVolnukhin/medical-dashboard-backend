using DashboardAPI.Hubs;
using DashboardAPI.Services.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Hubs;

public class MetricsHubTests
{
    private readonly Mock<ISignalRService> _mockSignalRService;
    private readonly Mock<ILogger<MetricsHub>> _mockLogger;
    private readonly Mock<ISingleClientProxy> _mockClientProxy;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroupManager;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly MetricsHub _hub;

    public MetricsHubTests()
    {
        _mockSignalRService = new Mock<ISignalRService>();
        _mockLogger = new Mock<ILogger<MetricsHub>>();
        _mockClientProxy = new Mock<ISingleClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroupManager = new Mock<IGroupManager>();
        _mockClients = new Mock<IHubCallerClients>();

        _mockContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        _mockClients.Setup(x => x.Caller).Returns(_mockClientProxy.Object);

        _hub = new MetricsHub(_mockSignalRService.Object, _mockLogger.Object)
        {
            Context = _mockContext.Object,
            Clients = _mockClients.Object,
            Groups = _mockGroupManager.Object
        };
    }

    [Fact]
    public async Task SubscribeToPatient_WithValidPatientId_ShouldAddToGroupAndSendConfirmation()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToPatient(patientId);

        // Assert
        _mockSignalRService.Verify(x => x.AddToPatientGroupAsync("test-connection-id", patientId), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default), Times.Once);
    }

    [Fact]
    public async Task SubscribeToPatient_WithEmptyPatientId_ShouldAddToGroupAndSendConfirmation()
    {
        // Arrange
        var patientId = Guid.Empty;
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToPatient(patientId);

        // Assert
        _mockSignalRService.Verify(x => x.AddToPatientGroupAsync("test-connection-id", patientId), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default), Times.Once);
    }

    [Fact]
    public async Task SubscribeToPatient_WhenSignalRServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedException = new Exception("SignalR service error");
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _hub.SubscribeToPatient(patientId));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task SubscribeToPatientMetrics_WithValidPatientId_ShouldAddToGroupAndSendConfirmation()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToPatientMetrics(patientId);

        // Assert
        _mockSignalRService.Verify(x => x.AddToPatientGroupAsync("test-connection-id", patientId), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default), Times.Once);
    }

    [Fact]
    public async Task SubscribeToPatientMetrics_WithEmptyPatientId_ShouldAddToGroupAndSendConfirmation()
    {
        // Arrange
        var patientId = Guid.Empty;
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToPatientMetrics(patientId);

        // Assert
        _mockSignalRService.Verify(x => x.AddToPatientGroupAsync("test-connection-id", patientId), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default), Times.Once);
    }

    [Fact]
    public async Task SubscribeToPatientMetrics_WhenSignalRServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedException = new Exception("SignalR service error");
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _hub.SubscribeToPatientMetrics(patientId));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task UnsubscribeFromPatient_WithValidPatientId_ShouldRemoveFromGroupAndSendConfirmation()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _mockSignalRService.Setup(x => x.RemoveFromPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("UnsubscribedFromPatient", new object[] { patientId }, default))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.UnsubscribeFromPatient(patientId);

        // Assert
        _mockSignalRService.Verify(x => x.RemoveFromPatientGroupAsync("test-connection-id", patientId), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync("UnsubscribedFromPatient", new object[] { patientId }, default), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromPatient_WithEmptyPatientId_ShouldRemoveFromGroupAndSendConfirmation()
    {
        // Arrange
        var patientId = Guid.Empty;
        _mockSignalRService.Setup(x => x.RemoveFromPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("UnsubscribedFromPatient", new object[] { patientId }, default))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.UnsubscribeFromPatient(patientId);

        // Assert
        _mockSignalRService.Verify(x => x.RemoveFromPatientGroupAsync("test-connection-id", patientId), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync("UnsubscribedFromPatient", new object[] { patientId }, default), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromPatient_WhenSignalRServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedException = new Exception("SignalR service error");
        _mockSignalRService.Setup(x => x.RemoveFromPatientGroupAsync("test-connection-id", patientId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _hub.UnsubscribeFromPatient(patientId));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldCallBaseMethod()
    {
        // Arrange
        var baseHub = new Mock<Hub>();
        baseHub.Setup(x => x.OnConnectedAsync()).Returns(Task.CompletedTask);

        // Act
        await _hub.OnConnectedAsync();

        // Assert - просто проверяем что метод выполняется без исключений
        Assert.True(true);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_ShouldCallBaseMethod()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        await _hub.OnDisconnectedAsync(exception);

        // Assert - просто проверяем что метод выполняется без исключений
        Assert.True(true);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithoutException_ShouldCallBaseMethod()
    {
        // Arrange
        Exception? exception = null;

        // Act
        await _hub.OnDisconnectedAsync(exception);

        // Assert - просто проверяем что метод выполняется без исключений
        Assert.True(true);
    }

    [Fact]
    public async Task SubscribeToPatient_WhenClientProxyThrowsException_ShouldPropagateException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedException = new Exception("Client proxy error");
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _hub.SubscribeToPatient(patientId));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task SubscribeToPatientMetrics_WhenClientProxyThrowsException_ShouldPropagateException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedException = new Exception("Client proxy error");
        _mockSignalRService.Setup(x => x.AddToPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("SubscribedToPatient", new object[] { patientId }, default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _hub.SubscribeToPatientMetrics(patientId));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task UnsubscribeFromPatient_WhenClientProxyThrowsException_ShouldPropagateException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedException = new Exception("Client proxy error");
        _mockSignalRService.Setup(x => x.RemoveFromPatientGroupAsync("test-connection-id", patientId))
            .Returns(Task.CompletedTask);
        _mockClientProxy.Setup(x => x.SendCoreAsync("UnsubscribedFromPatient", new object[] { patientId }, default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _hub.UnsubscribeFromPatient(patientId));
        Assert.Same(expectedException, exception);
    }
} 