using AuthService.DTOs;
using AuthService.Repository.User;
using AuthService.Services.Jwt;
using AuthService.Services.Password;
using AuthService.Services.RefreshToken;
using Shared;

namespace AuthService.Services.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly IJwtBuilder _jwtBuilder;
        private readonly IRefreshTokenService _refreshTokenService;

        public IdentityService(
            IUserRepository userRepository,
            IPasswordService passwordService,
            IJwtBuilder jwtBuilder,
            IRefreshTokenService refreshTokenService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _jwtBuilder = jwtBuilder;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<LoginResponse> LoginAsync(string email, string password, string? ipAddress)
        {
            var user = await _userRepository.GetByEmail(email);
            if (user == null)
                throw new InvalidOperationException("Invalid email or password");

            if (!_passwordService.ValidatePassword(password, user.Salt, user.Password))
                throw new InvalidOperationException("Invalid email or password");

            
            var accessToken = await _jwtBuilder.GetTokenAsync(user.Id, user.Role);
            
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user, ipAddress ?? "IP not found");
            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                Status = "SUCCESS", 
                Role = user.Role.ToLower()
            };
        }
        
        public async Task<TokensResponse> RefreshTokenAsync(string refreshToken, string? ipAddress)
        {
            if (!await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken))
                throw new InvalidOperationException("Invalid refresh token");

            var token = await _refreshTokenService.RotateRefreshTokenAsync(refreshToken, ipAddress ?? "IP not found");
            var user = await _userRepository.GetById(token.UserId);
            
            var accessToken = await _jwtBuilder.GetTokenAsync(user.Id, user.Role);
            return new TokensResponse 
            {
                AccessToken = accessToken, 
                RefreshToken = token.Token
            };
        }

        public async Task RevokeTokenAsync(string refreshToken, string? ipAddress)
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken, ipAddress ?? "IP not found");
        }

        public async Task<bool> ValidateTokenAsync(string refreshToken)
        {
            return await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
        }

        public async Task<AuthService.Models.User?> GetUserAsync(string email)
        {
            var result = _userRepository.Users
                .FirstOrDefault(u => u.Email == email);

            return result;
        }
        
        public async Task<AuthService.Models.User?> GetUserByIdAsync(Guid userId)
        {
            var result = _userRepository.Users
                .FirstOrDefault(u => u.Id == userId);

            return result;
        }

        public async Task InsertUserAsync(AuthService.Models.User user)
        {
            if (!Enum.TryParse<Role>(user.Role, ignoreCase: true, out _))
                throw new InvalidOperationException($"Invalid role. Allowed values: {string.Join(", ", Enum.GetNames(typeof(Role)))}");
            
            await _userRepository.Users.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }
    }
}
