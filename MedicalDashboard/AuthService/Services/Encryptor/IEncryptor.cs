namespace AuthService.Services.Encryptor;

public interface IEncryptor
{
    string GetSalt();
    string GetHash(string value, string salt);
}