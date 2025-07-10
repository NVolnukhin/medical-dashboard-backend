using AuthService.Services.Password;
using Middleware;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Services;

public class PasswordServiceTests
{
    private readonly Mock<IEncryptor> _mockEncryptor;
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _mockEncryptor = new Mock<IEncryptor>();
        _passwordService = new PasswordService(_mockEncryptor.Object);
    }

    [Fact]
    public void CreatePasswordHash_WithValidPassword_ShouldReturnHashAndSalt()
    {
        // Arrange
        var password = "testpassword";
        var expectedSalt = "testsalt";
        var expectedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetSalt()).Returns(expectedSalt);
        _mockEncryptor.Setup(x => x.GetHash(password, expectedSalt)).Returns(expectedHash);

        // Act
        var result = _passwordService.CreatePasswordHash(password);

        // Assert
        Assert.Equal(expectedHash, result.PasswordHash);
        Assert.Equal(expectedSalt, result.Salt);
        _mockEncryptor.Verify(x => x.GetSalt(), Times.Once);
        _mockEncryptor.Verify(x => x.GetHash(password, expectedSalt), Times.Once);
    }

    [Fact]
    public void CreatePasswordHash_WithEmptyPassword_ShouldReturnHashAndSalt()
    {
        // Arrange
        var password = "";
        var expectedSalt = "testsalt";
        var expectedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetSalt()).Returns(expectedSalt);
        _mockEncryptor.Setup(x => x.GetHash(password, expectedSalt)).Returns(expectedHash);

        // Act
        var result = _passwordService.CreatePasswordHash(password);

        // Assert
        Assert.Equal(expectedHash, result.PasswordHash);
        Assert.Equal(expectedSalt, result.Salt);
        _mockEncryptor.Verify(x => x.GetSalt(), Times.Once);
        _mockEncryptor.Setup(x => x.GetHash(password, expectedSalt)).Returns(expectedHash);
    }

    [Fact]
    public void CreatePasswordHash_WithNullPassword_ShouldReturnHashAndSalt()
    {
        // Arrange
        string? password = null;
        var expectedSalt = "testsalt";
        var expectedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetSalt()).Returns(expectedSalt);
        _mockEncryptor.Setup(x => x.GetHash(password!, expectedSalt)).Returns(expectedHash);

        // Act
        var result = _passwordService.CreatePasswordHash(password!);

        // Assert
        Assert.Equal(expectedHash, result.PasswordHash);
        Assert.Equal(expectedSalt, result.Salt);
        _mockEncryptor.Verify(x => x.GetSalt(), Times.Once);
        _mockEncryptor.Verify(x => x.GetHash(password!, expectedSalt), Times.Once);
    }

    [Fact]
    public void CreatePasswordHash_WithSpecialCharacters_ShouldReturnHashAndSalt()
    {
        // Arrange
        var password = "test@#$%^&*()password";
        var expectedSalt = "testsalt";
        var expectedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetSalt()).Returns(expectedSalt);
        _mockEncryptor.Setup(x => x.GetHash(password, expectedSalt)).Returns(expectedHash);

        // Act
        var result = _passwordService.CreatePasswordHash(password);

        // Assert
        Assert.Equal(expectedHash, result.PasswordHash);
        Assert.Equal(expectedSalt, result.Salt);
        _mockEncryptor.Verify(x => x.GetSalt(), Times.Once);
        _mockEncryptor.Verify(x => x.GetHash(password, expectedSalt), Times.Once);
    }

    [Fact]
    public void CreatePasswordHash_WithLongPassword_ShouldReturnHashAndSalt()
    {
        // Arrange
        var password = new string('A', 1000);
        var expectedSalt = "testsalt";
        var expectedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetSalt()).Returns(expectedSalt);
        _mockEncryptor.Setup(x => x.GetHash(password, expectedSalt)).Returns(expectedHash);

        // Act
        var result = _passwordService.CreatePasswordHash(password);

        // Assert
        Assert.Equal(expectedHash, result.PasswordHash);
        Assert.Equal(expectedSalt, result.Salt);
        _mockEncryptor.Verify(x => x.GetSalt(), Times.Once);
        _mockEncryptor.Verify(x => x.GetHash(password, expectedSalt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithValidPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "testpassword";
        var salt = "testsalt";
        var storedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetHash(password, salt)).Returns(storedHash);

        // Act
        var result = _passwordService.ValidatePassword(password, salt, storedHash);

        // Assert
        Assert.True(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithInvalidPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "wrongpassword";
        var salt = "testsalt";
        var storedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetHash(password, salt)).Returns("wronghash");

        // Act
        var result = _passwordService.ValidatePassword(password, salt, storedHash);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithWrongSalt_ShouldReturnFalse()
    {
        // Arrange
        var password = "testpassword";
        var salt = "wrongsalt";
        var storedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetHash(password, salt)).Returns("wronghash");

        // Act
        var result = _passwordService.ValidatePassword(password, salt, storedHash);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithEmptyPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "";
        var salt = "testsalt";
        var storedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetHash(password, salt)).Returns("wronghash");

        // Act
        var result = _passwordService.ValidatePassword(password, salt, storedHash);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithNullPassword_ShouldReturnFalse()
    {
        // Arrange
        string? password = null;
        var salt = "testsalt";
        var storedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetHash(password!, salt)).Returns("wronghash");

        // Act
        var result = _passwordService.ValidatePassword(password!, salt, storedHash);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password!, salt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithEmptySalt_ShouldReturnFalse()
    {
        // Arrange
        var password = "testpassword";
        var salt = "";
        var storedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetHash(password, salt)).Returns("wronghash");

        // Act
        var result = _passwordService.ValidatePassword(password, salt, storedHash);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithNullSalt_ShouldReturnFalse()
    {
        // Arrange
        var password = "testpassword";
        string? salt = null;
        var storedHash = "testhash";

        _mockEncryptor.Setup(x => x.GetHash(password, salt!)).Returns("wronghash");

        // Act
        var result = _passwordService.ValidatePassword(password, salt!, storedHash);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt!), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithEmptyStoredHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "testpassword";
        var salt = "testsalt";
        var storedHash = "";

        _mockEncryptor.Setup(x => x.GetHash(password, salt)).Returns("testhash");

        // Act
        var result = _passwordService.ValidatePassword(password, salt, storedHash);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt), Times.Once);
    }

    [Fact]
    public void ValidatePassword_WithNullStoredHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "testpassword";
        var salt = "testsalt";
        string? storedHash = null;

        _mockEncryptor.Setup(x => x.GetHash(password, salt)).Returns("testhash");

        // Act
        var result = _passwordService.ValidatePassword(password, salt, storedHash!);

        // Assert
        Assert.False(result);
        _mockEncryptor.Verify(x => x.GetHash(password, salt), Times.Once);
    }
} 