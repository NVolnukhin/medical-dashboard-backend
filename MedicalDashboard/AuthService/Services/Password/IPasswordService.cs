namespace AuthService.Services.Password
{
    public interface IPasswordService
    {
        (string PasswordHash, string Salt) CreatePasswordHash(string password);
        bool ValidatePassword(string password, string salt, string storedHash);
    }
}
