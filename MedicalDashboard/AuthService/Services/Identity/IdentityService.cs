using AuthService.DTOs;
using AuthService.Repository.User;
using AuthService.Services.Jwt;
using AuthService.Services.Password;

namespace AuthService.Services.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly IJwtBuilder _jwtBuilder;

        public IdentityService(
            IUserRepository userRepository,
            IPasswordService passwordService,
            IJwtBuilder jwtBuilder)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _jwtBuilder = jwtBuilder;
        }

        public async Task<LoginResponse> LoginAsync(string email, string password, string? ipAddress)
        {
            var user = await _userRepository.GetByEmail(email);
            if (user == null)
                throw new InvalidOperationException("Invalid email or password");

            if (!_passwordService.ValidatePassword(password, user.Salt, user.Password))
                throw new InvalidOperationException("Invalid email or password");

            var accessToken = await _jwtBuilder.GetTokenAsync(user.Id);
            return new LoginResponse
            {
                AccessToken = accessToken,
                Status = "SUCCESS", 
                Message = null
            };
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
            var result = await _userRepository.Users.AddAsync(user);

            await _userRepository.SaveChangesAsync();
        }
    }
}
