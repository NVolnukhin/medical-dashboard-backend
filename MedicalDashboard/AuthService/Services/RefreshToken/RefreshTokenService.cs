using AuthService.Repository.RefreshToken;

namespace AuthService.Services.RefreshToken
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private const int TokenValidityInDays = 7;

        public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<Models.RefreshToken> GenerateRefreshTokenAsync(Models.User user, string? ipAddress)
        {
            // Отзываем все существующие токены пользователя
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, "Generated new token", ipAddress ?? "IP not found");

            var token = new Models.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(TokenValidityInDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                IsRevoked = false
            };

            return await _refreshTokenRepository.CreateAsync(token);
        }

        public async Task<Models.RefreshToken> RotateRefreshTokenAsync(string token, string? ipAddress)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
            if (refreshToken == null)
                throw new InvalidOperationException("Неверный refresh token");

            if (refreshToken.IsRevoked)
                throw new InvalidOperationException("Токен уже был отозван");

            // Отзываем текущий токен
            await _refreshTokenRepository.RevokeTokenAsync(token, "Replacing with another token", ipAddress ?? "IP not found");

            // Создаем новый токен
            var newToken = new Models.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = refreshToken.UserId,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(TokenValidityInDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                IsRevoked = false,
                ReplacedByToken = token
            };

            return await _refreshTokenRepository.CreateAsync(newToken);
        }

        public async Task RevokeRefreshTokenAsync(string token, string? ipAddress)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
            if (refreshToken == null)
                throw new InvalidOperationException("Неверный refresh token");

            if (refreshToken.IsRevoked)
                throw new InvalidOperationException("Токен уже был отозван");

            await _refreshTokenRepository.RevokeTokenAsync(token, "logged out", ipAddress ?? "IP not found");
        }

        public async Task<bool> ValidateRefreshTokenAsync(string token)
        {
            return await _refreshTokenRepository.IsTokenValidAsync(token);
        }
    }
} 