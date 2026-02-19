using System.Text.Json;
using DataAnalysisService.DTOs;
using DataAnalysisService.Services.Redis;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Tests.MedicalDashboard.DASTests.Unit.Services.Redis;

public class RedisServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly RedisService _service;

    public RedisServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _service = new RedisService(_redisMock.Object);
    }

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsDeserializedValue()
    {
        // Arrange
        var expectedValue = 80.5;
        var serializedValue = JsonSerializer.Serialize(expectedValue);
        
        _databaseMock.Setup(x => x.StringGetAsync("test-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _service.GetAsync<double>("test-key");

        // Assert
        Assert.Equal(expectedValue, result);
        _databaseMock.Verify(x => x.StringGetAsync("test-key", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_NonExistingKey_ReturnsDefault()
    {
        // Arrange
        _databaseMock.Setup(x => x.StringGetAsync("non-existing-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _service.GetAsync<double>("non-existing-key");

        // Assert
        Assert.Equal(default(double), result);
        _databaseMock.Verify(x => x.StringGetAsync("non-existing-key", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ComplexObject_DeserializesCorrectly()
    {
        // Arrange
        var expectedValue = new LastAlertInfo
        {
            AlertType = "alert",
            Timestamp = DateTime.UtcNow
        };
        var serializedValue = JsonSerializer.Serialize(expectedValue);
        
        _databaseMock.Setup(x => x.StringGetAsync("alert-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _service.GetAsync<LastAlertInfo>("alert-key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue.AlertType, result!.AlertType);
        Assert.Equal(expectedValue.Timestamp, result.Timestamp);
    }

    [Fact]
    public async Task GetAsync_InvalidJson_ReturnsDefault()
    {
        // Arrange
        var invalidJson = "invalid json";
        _databaseMock.Setup(x => x.StringGetAsync("invalid-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(invalidJson);

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _service.GetAsync<double>("invalid-key"));
    }

    [Fact]
    public async Task DeleteAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        _databaseMock.Setup(x => x.KeyDeleteAsync("test-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync("test-key");

        // Assert
        Assert.True(result);
        _databaseMock.Verify(x => x.KeyDeleteAsync("test-key", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        _databaseMock.Setup(x => x.KeyDeleteAsync("non-existing-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync("non-existing-key");

        // Assert
        Assert.False(result);
        _databaseMock.Verify(x => x.KeyDeleteAsync("non-existing-key", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        _databaseMock.Setup(x => x.KeyExistsAsync("test-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsAsync("test-key");

        // Assert
        Assert.True(result);
        _databaseMock.Verify(x => x.KeyExistsAsync("test-key", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        _databaseMock.Setup(x => x.KeyExistsAsync("non-existing-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ExistsAsync("non-existing-key");

        // Assert
        Assert.False(result);
        _databaseMock.Verify(x => x.KeyExistsAsync("non-existing-key", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_NullableType_HandlesNullCorrectly()
    {
        // Arrange
        _databaseMock.Setup(x => x.StringGetAsync("nullable-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _service.GetAsync<double?>("nullable-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_NullableType_HandlesValueCorrectly()
    {
        // Arrange
        var expectedValue = 75.0;
        var serializedValue = JsonSerializer.Serialize(expectedValue);
        
        _databaseMock.Setup(x => x.StringGetAsync("nullable-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _service.GetAsync<double?>("nullable-key");

        // Assert
        Assert.Equal(expectedValue, result);
    }
    
    [Fact]
    public async Task GetAsync_StringType_ReturnsCorrectly()
    {
        // Arrange
        var expectedValue = "test string";
        var serializedValue = JsonSerializer.Serialize(expectedValue);
        
        _databaseMock.Setup(x => x.StringGetAsync("string-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _service.GetAsync<string>("string-key");

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task GetAsync_IntType_ReturnsCorrectly()
    {
        // Arrange
        var expectedValue = 42;
        var serializedValue = JsonSerializer.Serialize(expectedValue);
        
        _databaseMock.Setup(x => x.StringGetAsync("int-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _service.GetAsync<int>("int-key");

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task GetAsync_BoolType_ReturnsCorrectly()
    {
        // Arrange
        var expectedValue = true;
        var serializedValue = JsonSerializer.Serialize(expectedValue);
        
        _databaseMock.Setup(x => x.StringGetAsync("bool-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _service.GetAsync<bool>("bool-key");

        // Assert
        Assert.Equal(expectedValue, result);
    }
} 