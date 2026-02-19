namespace AuthService.DTOs
{
    public class LoginResponse
    {
        public string? AccessToken { get; init; } = string.Empty;
        public string? RefreshToken { get; init; } = string.Empty;

        public string? Status { get; init; } = string.Empty;
        public string? Role { get; init; } = string.Empty;
    }
}