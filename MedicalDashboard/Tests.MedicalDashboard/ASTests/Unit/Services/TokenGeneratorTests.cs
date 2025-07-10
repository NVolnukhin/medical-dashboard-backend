using AuthService.Services.RecoveryToken;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Services
{
    public class TokenGeneratorTests
    {
        private readonly TokenGenerator _tokenGenerator;

        public TokenGeneratorTests()
        {
            _tokenGenerator = new TokenGenerator();
        }

        [Fact]
        public void GenerateToken_ShouldReturnValidToken()
        {
            // Act
            var token = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.True(token.Length >= 32); // Minimum length for security
        }

        [Fact]
        public void GenerateToken_MultipleCalls_ShouldReturnDifferentTokens()
        {
            // Act
            var token1 = _tokenGenerator.GenerateToken();
            var token2 = _tokenGenerator.GenerateToken();
            var token3 = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotEqual(token1, token2);
            Assert.NotEqual(token2, token3);
            Assert.NotEqual(token1, token3);
        }

        [Fact]
        public void GenerateToken_ShouldReturnUrlSafeToken()
        {
            // Act
            var token = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotNull(token);
            Assert.DoesNotContain("+", token);
            Assert.DoesNotContain("/", token);
            Assert.DoesNotContain("=", token);
        }

        [Fact]
        public void GenerateToken_ShouldReturnBase64EncodedToken()
        {
            // Act
            var token = _tokenGenerator.GenerateToken();

            // Assert
            // URL-safe Base64: A-Z, a-z, 0-9, -, _ (no +, /, or =)
            Assert.Matches(@"^[A-Za-z0-9\-_]+$", token);
        }

        [Fact]
        public void GenerateToken_ShouldReturnConsistentLength()
        {
            // Act
            var token1 = _tokenGenerator.GenerateToken();
            var token2 = _tokenGenerator.GenerateToken();
            var token3 = _tokenGenerator.GenerateToken();

            // Assert
            Assert.Equal(token1.Length, token2.Length);
            Assert.Equal(token2.Length, token3.Length);
        }

        [Fact]
        public void GenerateToken_ShouldNotContainSpecialCharacters()
        {
            // Act
            var token = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotNull(token);
            Assert.DoesNotContain("!", token);
            Assert.DoesNotContain("@", token);
            Assert.DoesNotContain("#", token);
            Assert.DoesNotContain("$", token);
            Assert.DoesNotContain("%", token);
            Assert.DoesNotContain("^", token);
            Assert.DoesNotContain("&", token);
            Assert.DoesNotContain("*", token);
            Assert.DoesNotContain("(", token);
            Assert.DoesNotContain(")", token);
            Assert.DoesNotContain("=", token);
            // Note: URL-safe Base64 uses - and _ instead of + and /
        }

        [Fact]
        public void GenerateToken_ShouldNotContainSpaces()
        {
            // Act
            var token = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotNull(token);
            Assert.DoesNotContain(" ", token);
        }

        [Fact]
        public void GenerateToken_ShouldNotContainNewlines()
        {
            // Act
            var token = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotNull(token);
            Assert.DoesNotContain("\n", token);
            Assert.DoesNotContain("\r", token);
        }

        [Fact]
        public void GenerateToken_ShouldNotContainTabs()
        {
            // Act
            var token = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotNull(token);
            Assert.DoesNotContain("\t", token);
        }

        [Fact]
        public void GenerateToken_ShouldBeCaseSensitive()
        {
            // Act
            var token1 = _tokenGenerator.GenerateToken();
            var token2 = _tokenGenerator.GenerateToken();

            // Assert
            Assert.NotEqual(token1.ToLower(), token2.ToLower());
        }

        [Fact]
        public void GenerateToken_ShouldBeRandom()
        {
            // Act
            var tokens = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                tokens.Add(_tokenGenerator.GenerateToken());
            }

            // Assert
            var uniqueTokens = tokens.Distinct().Count();
            Assert.Equal(100, uniqueTokens); // All tokens should be unique
        }
    }
} 