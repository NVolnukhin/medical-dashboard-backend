using Middleware;

namespace AuthService.Services.Password
{
    public class PasswordService : IPasswordService
    {
        private readonly IEncryptor _encryptor;

        public PasswordService(IEncryptor encryptor)
        {
            _encryptor = encryptor;
        }

        public (string PasswordHash, string Salt) CreatePasswordHash(string password)
        {
            var salt = _encryptor.GetSalt();
            var passwordHash = _encryptor.GetHash(password, salt);
            return (passwordHash, salt);
        }

        public bool ValidatePassword(string password, string salt, string storedHash)
        {
            var hash = _encryptor.GetHash(password, salt);
            return hash == storedHash;
        }
    }
}