using AuthService.Services.Jwt;
using Microsoft.Extensions.Options;
using Middleware;
using Xunit;
using Moq;

namespace Tests.MedicalDashboard.ASTests.Unit.Services;

public class JwtBuilderTests
{
    private readonly JwtConfig _validConfig;
    private readonly JwtBuilder _jwtBuilder;

    public JwtBuilderTests()
    {
        _validConfig = new JwtConfig
        {
            SecretKey = "your-super-secret-key-with-at-least-32-characters",
            Issuer = "MedicalDashboard",
            Audience = "MedicalDashboardUsers"
        };

        var options = Options.Create(_validConfig);
        _jwtBuilder = new JwtBuilder(options);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowNullReferenceException()
    {
        // Arrange
        IOptions<JwtConfig>? options = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => new JwtBuilder(options));
    }

    [Fact]
    public void Constructor_WithNullConfigValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<JwtConfig>>();
        mockOptions.Setup(x => x.Value).Returns((JwtConfig)null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JwtBuilder(mockOptions.Object));
    }

    [Fact]
    public void Constructor_WithEmptySecretKey_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new JwtConfig
        {
            SecretKey = "",
            Issuer = "test-issuer",
            Audience = "test-audience"
        };
        var mockOptions = new Mock<IOptions<JwtConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new JwtBuilder(mockOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullSecretKey_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new JwtConfig
        {
            SecretKey = null,
            Issuer = "test-issuer",
            Audience = "test-audience"
        };
        var mockOptions = new Mock<IOptions<JwtConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new JwtBuilder(mockOptions.Object));
    }

    [Fact]
    public void Constructor_WithValidConfig_ShouldNotThrowException()
    {
        // Arrange
        var config = new JwtConfig
        {
            SecretKey = "test-secret-key",
            Issuer = "test-issuer",
            Audience = "test-audience"
        };
        var mockOptions = new Mock<IOptions<JwtConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        // Act & Assert
        var exception = Record.Exception(() => new JwtBuilder(mockOptions.Object));
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetTokenAsync_WithValidParameters_ShouldReturnValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var role = "Admin";

        // Act
        var token = await _jwtBuilder.GetTokenAsync(userId, role);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task GetTokenAsync_WithEmptyGuid_ShouldReturnValidToken()
    {
        // Arrange
        var userId = Guid.Empty;
        var role = "User";

        // Act
        var token = await _jwtBuilder.GetTokenAsync(userId, role);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task GetTokenAsync_WithEmptyRole_ShouldReturnValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var role = "";

        // Act
        var token = await _jwtBuilder.GetTokenAsync(userId, role);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task GetTokenAsync_WithNullRole_ShouldThrowNullReferenceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        string? role = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _jwtBuilder.GetTokenAsync(userId, role));
    }

    [Fact]
    public async Task GetTokenAsync_WithDifferentUserIds_ShouldReturnDifferentTokens()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var role = "User";

        // Act
        var token1 = await _jwtBuilder.GetTokenAsync(userId1, role);
        var token2 = await _jwtBuilder.GetTokenAsync(userId2, role);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public async Task GetTokenAsync_WithDifferentRoles_ShouldReturnValidTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var role1 = "Admin";
        var role2 = "User";

        // Act
        var token1 = await _jwtBuilder.GetTokenAsync(userId, role1);
        var token2 = await _jwtBuilder.GetTokenAsync(userId, role2);

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
    }

    [Fact]
    public async Task GetTokenAsync_WithSameUserIdAndRole_ShouldReturnSameToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var role = "Admin";

        // Act
        var token1 = await _jwtBuilder.GetTokenAsync(userId, role);
        var token2 = await _jwtBuilder.GetTokenAsync(userId, role);

        // Assert
        Assert.Equal(token1, token2);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var role = "Admin";

        // Act
        var token = _jwtBuilder.GetTokenAsync(userId, role).Result;
        var extractedUserId = _jwtBuilder.ValidateToken(token);

        // Assert
        Assert.Equal(userId.ToString(), extractedUserId);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnEmptyString()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = _jwtBuilder.ValidateToken(invalidToken);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldReturnEmptyString()
    {
        // Act
        var result = _jwtBuilder.ValidateToken("");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ValidateToken_WithNullToken_ShouldReturnEmptyString()
    {
        // Act
        var result = _jwtBuilder.ValidateToken(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ValidateToken_WithTokenFromDifferentSecret_ShouldReturnEmptyString()
    {
        // Arrange
        var config1 = new JwtConfig
        {
            SecretKey = "first-secret-key-with-at-least-32-characters",
            Issuer = "MedicalDashboard",
            Audience = "MedicalDashboardUsers"
        };
        var config2 = new JwtConfig
        {
            SecretKey = "second-secret-key-with-at-least-32-characters",
            Issuer = "MedicalDashboard",
            Audience = "MedicalDashboardUsers"
        };

        var options1 = Options.Create(config1);
        var options2 = Options.Create(config2);
        
        var jwtBuilder1 = new JwtBuilder(options1);
        var jwtBuilder2 = new JwtBuilder(options2);

        var userId = Guid.NewGuid();
        var role = "Admin";

        // Act
        var token = jwtBuilder1.GetTokenAsync(userId, role).Result;
        var result = jwtBuilder2.ValidateToken(token);

        // Assert
        Assert.Equal(string.Empty, result);
    }
} 