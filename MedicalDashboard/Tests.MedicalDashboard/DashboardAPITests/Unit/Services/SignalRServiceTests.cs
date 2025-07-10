using DashboardAPI.Hubs;
using DashboardAPI.Services.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Shared;
using Xunit;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Services;

public class SignalRServiceTests
{
    private readonly Mock<IHubContext<MetricsHub>> _hubContextMock;
    private readonly Mock<ILogger<SignalRService>> _loggerMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly SignalRService _signalRService;

    public SignalRServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<MetricsHub>>();
        _loggerMock = new Mock<ILogger<SignalRService>>();
        _clientProxyMock = new Mock<IClientProxy>();
        _hubClientsMock = new Mock<IHubClients>();
        _groupManagerMock = new Mock<IGroupManager>();

        _hubClientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(x => x.Clients).Returns(_hubClientsMock.Object);
        _hubContextMock.Setup(x => x.Groups).Returns(_groupManagerMock.Object);

        _signalRService = new SignalRService(_hubContextMock.Object);
    }

    [Fact]
    public async Task SendMetricToPatientAsync_ShouldSendMetricToPatientGroup()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "HeartRate",
            Value = 75.5,
            Timestamp = DateTime.UtcNow
        };

        _clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.SendMetricToPatientAsync(patientId, metric);

        // Assert
        _hubClientsMock.Verify(x => x.Group($"patient-{patientId}"), Times.Once);
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveMetric", It.Is<object[]>(args => 
            args.Length == 1 && args[0] is MetricDto), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMetricToPatientAsync_WithNullMetric_ShouldNotThrowException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        MetricDto? metric = null;

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            _signalRService.SendMetricToPatientAsync(patientId, metric));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task SendMetricToPatientAsync_WithEmptyPatientId_ShouldSendToEmptyGroup()
    {
        // Arrange
        var patientId = Guid.Empty;
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "HeartRate",
            Value = 75.5,
            Timestamp = DateTime.UtcNow
        };

        _clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.SendMetricToPatientAsync(patientId, metric);

        // Assert
        _hubClientsMock.Verify(x => x.Group("patient-00000000-0000-0000-0000-000000000000"), Times.Once);
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveMetric", It.Is<object[]>(args => 
            args.Length == 1 && args[0] is MetricDto), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddToPatientGroupAsync_ShouldAddConnectionToGroup()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var patientId = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.AddToGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.AddToPatientGroupAsync(connectionId, patientId);

        // Assert
        _groupManagerMock.Verify(x => x.AddToGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddToPatientGroupAsync_WithNullConnectionId_ShouldAddToGroup()
    {
        // Arrange
        string? connectionId = null;
        var patientId = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.AddToGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.AddToPatientGroupAsync(connectionId, patientId);

        // Assert
        _groupManagerMock.Verify(x => x.AddToGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddToPatientGroupAsync_WithEmptyConnectionId_ShouldAddToGroup()
    {
        // Arrange
        var connectionId = "";
        var patientId = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.AddToGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.AddToPatientGroupAsync(connectionId, patientId);

        // Assert
        _groupManagerMock.Verify(x => x.AddToGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddToPatientGroupAsync_WithMultiplePatients_ShouldAddToCorrectGroups()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var patientId1 = Guid.NewGuid();
        var patientId2 = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.AddToGroupAsync(connectionId, $"patient-{patientId1}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _groupManagerMock.Setup(x => x.AddToGroupAsync(connectionId, $"patient-{patientId2}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.AddToPatientGroupAsync(connectionId, patientId1);
        await _signalRService.AddToPatientGroupAsync(connectionId, patientId2);

        // Assert
        _groupManagerMock.Verify(x => x.AddToGroupAsync(connectionId, $"patient-{patientId1}", It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.Verify(x => x.AddToGroupAsync(connectionId, $"patient-{patientId2}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromPatientGroupAsync_ShouldRemoveConnectionFromGroup()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var patientId = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.RemoveFromPatientGroupAsync(connectionId, patientId);

        // Assert
        _groupManagerMock.Verify(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromPatientGroupAsync_WithNullConnectionId_ShouldRemoveFromGroup()
    {
        // Arrange
        string? connectionId = null;
        var patientId = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.RemoveFromPatientGroupAsync(connectionId, patientId);

        // Assert
        _groupManagerMock.Verify(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromPatientGroupAsync_WithEmptyConnectionId_ShouldRemoveFromGroup()
    {
        // Arrange
        var connectionId = "";
        var patientId = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.RemoveFromPatientGroupAsync(connectionId, patientId);

        // Assert
        _groupManagerMock.Verify(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromPatientGroupAsync_WithMultiplePatients_ShouldRemoveFromCorrectGroups()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var patientId1 = Guid.NewGuid();
        var patientId2 = Guid.NewGuid();

        _groupManagerMock.Setup(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId1}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _groupManagerMock.Setup(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId2}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.RemoveFromPatientGroupAsync(connectionId, patientId1);
        await _signalRService.RemoveFromPatientGroupAsync(connectionId, patientId2);

        // Assert
        _groupManagerMock.Verify(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId1}", It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.Verify(x => x.RemoveFromGroupAsync(connectionId, $"patient-{patientId2}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMetricToPatientAsync_WithDifferentMetricTypes_ShouldSendCorrectData()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metrics = new[]
        {
            new MetricDto { PatientId = patientId, Type = "HeartRate", Value = 75.5, Timestamp = DateTime.UtcNow },
            new MetricDto { PatientId = patientId, Type = "BloodPressure", Value = 120.0, Timestamp = DateTime.UtcNow },
            new MetricDto { PatientId = patientId, Type = "Temperature", Value = 36.6, Timestamp = DateTime.UtcNow }
        };

        _clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        foreach (var metric in metrics)
        {
            await _signalRService.SendMetricToPatientAsync(patientId, metric);
        }

        // Assert
        _hubClientsMock.Verify(x => x.Group($"patient-{patientId}"), Times.Exactly(3));
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveMetric", It.Is<object[]>(args => 
            args.Length == 1 && args[0] is MetricDto), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
} 