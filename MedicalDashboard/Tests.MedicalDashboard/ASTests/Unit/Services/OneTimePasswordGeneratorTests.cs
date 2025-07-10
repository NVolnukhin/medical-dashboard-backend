using AuthService.Services.Password;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Services
{
    public class OneTimePasswordGeneratorTests
    {
        private readonly OneTimePasswordGenerator _otpGenerator;

        public OneTimePasswordGeneratorTests()
        {
            _otpGenerator = new OneTimePasswordGenerator();
        }

        [Fact]
        public void GenerateOtp_WithDefaultLength_ShouldReturnEightCharacterPassword()
        {
            // Act
            var otp = _otpGenerator.GeneratePassword(8);

            // Assert
            Assert.NotNull(otp);
            Assert.Equal(8, otp.Length);
            Assert.Contains(otp, c => char.IsUpper(c));
            Assert.Contains(otp, c => char.IsLower(c));
            Assert.Contains(otp, c => char.IsDigit(c));
            Assert.Contains(otp, c => "!@#$%^&*".Contains(c));
        }

        [Fact]
        public void GenerateOtp_WithCustomLength_ShouldReturnPasswordWithSpecifiedLength()
        {
            // Arrange
            var length = 12;

            // Act
            var otp = _otpGenerator.GeneratePassword(length);

            // Assert
            Assert.NotNull(otp);
            Assert.Equal(length, otp.Length);
            Assert.Contains(otp, c => char.IsUpper(c));
            Assert.Contains(otp, c => char.IsLower(c));
            Assert.Contains(otp, c => char.IsDigit(c));
            Assert.Contains(otp, c => "!@#$%^&*".Contains(c));
        }

        [Fact]
        public void GenerateOtp_WithLengthEight_ShouldReturnEightCharacterPassword()
        {
            // Act
            var otp = _otpGenerator.GeneratePassword(8);

            // Assert
            Assert.NotNull(otp);
            Assert.Equal(8, otp.Length);
            Assert.Contains(otp, c => char.IsUpper(c));
            Assert.Contains(otp, c => char.IsLower(c));
            Assert.Contains(otp, c => char.IsDigit(c));
            Assert.Contains(otp, c => "!@#$%^&*".Contains(c));
        }

        [Fact]
        public void GenerateOtp_WithLengthTen_ShouldReturnTenCharacterPassword()
        {
            // Act
            var otp = _otpGenerator.GeneratePassword(10);

            // Assert
            Assert.NotNull(otp);
            Assert.Equal(10, otp.Length);
            Assert.Contains(otp, c => char.IsUpper(c));
            Assert.Contains(otp, c => char.IsLower(c));
            Assert.Contains(otp, c => char.IsDigit(c));
            Assert.Contains(otp, c => "!@#$%^&*".Contains(c));
        }

        [Fact]
        public void GenerateOtp_WithZeroLength_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _otpGenerator.GeneratePassword(0));
        }

        [Fact]
        public void GenerateOtp_WithNegativeLength_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _otpGenerator.GeneratePassword(-1));
        }

        [Fact]
        public void GenerateOtp_WithLengthLessThanEight_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _otpGenerator.GeneratePassword(7));
            Assert.Throws<ArgumentException>(() => _otpGenerator.GeneratePassword(1));
        }

        [Fact]
        public void GenerateOtp_WithVeryLongLength_ShouldReturnPasswordWithSpecifiedLength()
        {
            // Arrange
            var length = 20;

            // Act
            var otp = _otpGenerator.GeneratePassword(length);

            // Assert
            Assert.NotNull(otp);
            Assert.Equal(length, otp.Length);
            Assert.Contains(otp, c => char.IsUpper(c));
            Assert.Contains(otp, c => char.IsLower(c));
            Assert.Contains(otp, c => char.IsDigit(c));
            Assert.Contains(otp, c => "!@#$%^&*".Contains(c));
        }

        [Fact]
        public void GenerateOtp_MultipleCalls_ShouldReturnDifferentPasswords()
        {
            // Act
            var otp1 = _otpGenerator.GeneratePassword(8);
            var otp2 = _otpGenerator.GeneratePassword(9);
            var otp3 = _otpGenerator.GeneratePassword(10);

            // Assert
            Assert.NotEqual(otp1, otp2);
            Assert.NotEqual(otp2, otp3);
            Assert.NotEqual(otp1, otp3);
        }

        [Fact]
        public void GenerateOtp_WithSameLength_ShouldReturnDifferentPasswords()
        {
            // Act
            var otp1 = _otpGenerator.GeneratePassword(8);
            var otp2 = _otpGenerator.GeneratePassword(8);

            // Assert
            Assert.NotEqual(otp1, otp2);
            Assert.Equal(8, otp1.Length);
            Assert.Equal(8, otp2.Length);
        }

        [Fact]
        public void GenerateOtp_ShouldContainAllCharacterTypes()
        {
            // Act
            var otp = _otpGenerator.GeneratePassword(10);

            // Assert
            Assert.Contains(otp, c => char.IsUpper(c));
            Assert.Contains(otp, c => char.IsLower(c));
            Assert.Contains(otp, c => char.IsDigit(c));
            Assert.Contains(otp, c => "!@#$%^&*".Contains(c));
        }

        [Fact]
        public void GenerateOtp_ShouldContainOnlyValidCharacters()
        {
            // Act
            var otp = _otpGenerator.GeneratePassword(8);

            // Assert
            Assert.True(otp.All(c => char.IsLetterOrDigit(c) || "!@#$%^&*".Contains(c)));
        }

        [Theory]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(15)]
        [InlineData(20)]
        public void GenerateOtp_WithVariousLengths_ShouldReturnCorrectLength(int length)
        {
            // Act
            var otp = _otpGenerator.GeneratePassword(length);

            // Assert
            Assert.Equal(length, otp.Length);
            Assert.Contains(otp, c => char.IsUpper(c));
            Assert.Contains(otp, c => char.IsLower(c));
            Assert.Contains(otp, c => char.IsDigit(c));
            Assert.Contains(otp, c => "!@#$%^&*".Contains(c));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(7)]
        public void GenerateOtp_WithInvalidLengths_ShouldThrowArgumentException(int length)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _otpGenerator.GeneratePassword(length));
        }
    }
}