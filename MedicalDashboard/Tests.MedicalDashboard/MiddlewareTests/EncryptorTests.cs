using Middleware;
using Xunit;

namespace Tests.MedicalDashboard.MiddlewareTests
{
    public class EncryptorTests
    {
        private readonly Encryptor _encryptor;

        public EncryptorTests()
        {
            _encryptor = new Encryptor();
        }

        [Fact]
        public void GetSalt_ShouldReturnValidSalt()
        {
            // Act
            var salt = _encryptor.GetSalt();

            // Assert
            Assert.NotNull(salt);
            Assert.NotEmpty(salt);
            Assert.True(salt.Length > 0);
        }

        [Fact]
        public void GetSalt_MultipleCalls_ShouldReturnDifferentSalts()
        {
            // Act
            var salt1 = _encryptor.GetSalt();
            var salt2 = _encryptor.GetSalt();
            var salt3 = _encryptor.GetSalt();

            // Assert
            Assert.NotEqual(salt1, salt2);
            Assert.NotEqual(salt2, salt3);
            Assert.NotEqual(salt1, salt3);
        }

        [Fact]
        public void GetHash_WithValidInput_ShouldReturnValidHash()
        {
            // Arrange
            var value = "testPassword";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.True(hash.Length > 0);
        }

        [Fact]
        public void GetHash_WithSameInput_ShouldReturnSameHash()
        {
            // Arrange
            var value = "testPassword";
            var salt = _encryptor.GetSalt();

            // Act
            var hash1 = _encryptor.GetHash(value, salt);
            var hash2 = _encryptor.GetHash(value, salt);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void GetHash_WithDifferentValues_ShouldReturnDifferentHashes()
        {
            // Arrange
            var value1 = "testPassword1";
            var value2 = "testPassword2";
            var salt = _encryptor.GetSalt();

            // Act
            var hash1 = _encryptor.GetHash(value1, salt);
            var hash2 = _encryptor.GetHash(value2, salt);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void GetHash_WithDifferentSalts_ShouldReturnDifferentHashes()
        {
            // Arrange
            var value = "testPassword";
            var salt1 = _encryptor.GetSalt();
            var salt2 = _encryptor.GetSalt();

            // Act
            var hash1 = _encryptor.GetHash(value, salt1);
            var hash2 = _encryptor.GetHash(value, salt2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void GetHash_WithEmptyValue_ShouldReturnValidHash()
        {
            // Arrange
            var value = "";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithNullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? value = null;
            var salt = _encryptor.GetSalt();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _encryptor.GetHash(value, salt));
        }

        [Fact]
        public void GetHash_WithEmptySalt_ShouldThrowArgumentException()
        {
            // Arrange
            var value = "testPassword";
            var salt = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _encryptor.GetHash(value, salt));
        }

        [Fact]
        public void GetHash_WithNullSalt_ShouldThrowNullReferenceException()
        {
            // Arrange
            var value = "testPassword";
            string? salt = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _encryptor.GetHash(value, salt));
        }

        [Fact]
        public void GetHash_WithLongValue_ShouldReturnValidHash()
        {
            // Arrange
            var value = new string('a', 1000);
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithLongSalt_ShouldReturnValidHash()
        {
            // Arrange
            var value = "testPassword";
            var salt = new string('s', 1000);

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithSpecialCharacters_ShouldReturnValidHash()
        {
            // Arrange
            var value = "test!@#$%^&*()Password";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithUnicodeCharacters_ShouldReturnValidHash()
        {
            // Arrange
            var value = "тестПароль";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithNumbers_ShouldReturnValidHash()
        {
            // Arrange
            var value = "password123";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithMixedCharacters_ShouldReturnValidHash()
        {
            // Arrange
            var value = "P@ssw0rd!123";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetSalt_ShouldReturnBase64String()
        {
            // Act
            var salt = _encryptor.GetSalt();

            // Assert
            // Base64 strings should only contain A-Z, a-z, 0-9, +, /, and = for padding
            Assert.Matches(@"^[A-Za-z0-9+/]*={0,2}$", salt);
        }

        [Fact]
        public void GetHash_ShouldReturnBase64String()
        {
            // Arrange
            var value = "testPassword";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            // Base64 strings should only contain A-Z, a-z, 0-9, +, /, and = for padding
            Assert.Matches(@"^[A-Za-z0-9+/]*={0,2}$", hash);
        }

        [Fact]
        public void GetSalt_ShouldReturnConsistentLength()
        {
            // Act
            var salt1 = _encryptor.GetSalt();
            var salt2 = _encryptor.GetSalt();
            var salt3 = _encryptor.GetSalt();

            // Assert
            Assert.Equal(salt1.Length, salt2.Length);
            Assert.Equal(salt2.Length, salt3.Length);
        }

        [Fact]
        public void GetHash_ShouldReturnConsistentLength()
        {
            // Arrange
            var value = "testPassword";
            var salt = _encryptor.GetSalt();

            // Act
            var hash1 = _encryptor.GetHash(value, salt);
            var hash2 = _encryptor.GetHash(value, salt);
            var hash3 = _encryptor.GetHash(value, salt);

            // Assert
            Assert.Equal(hash1.Length, hash2.Length);
            Assert.Equal(hash2.Length, hash3.Length);
        }

        [Fact]
        public void GetHash_WithWhitespace_ShouldReturnValidHash()
        {
            // Arrange
            var value = "  testPassword  ";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithWhitespaceInSalt_ShouldReturnValidHash()
        {
            // Arrange
            var value = "testPassword";
            var salt = "  salt  ";

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithNewlines_ShouldReturnValidHash()
        {
            // Arrange
            var value = "test\nPassword";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithTabs_ShouldReturnValidHash()
        {
            // Arrange
            var value = "test\tPassword";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithSingleCharacter_ShouldReturnValidHash()
        {
            // Arrange
            var value = "a";
            var salt = _encryptor.GetSalt();

            // Act
            var hash = _encryptor.GetHash(value, salt);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void GetHash_WithSingleCharacterSalt_ShouldThrowArgumentException()
        {
            // Arrange
            var value = "testPassword";
            var salt = "a";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _encryptor.GetHash(value, salt));
        }
    }
} 