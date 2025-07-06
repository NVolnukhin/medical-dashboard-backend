namespace AuthService.Services.Password
{
    public interface IOneTimePasswordGenerator
    {
        string GeneratePassword(int length);
    }
}