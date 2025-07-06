namespace AuthService.DTOs
{
    public class LoginResponse
    {
        public string? AccessToken { get; init; } = string.Empty;
        public string? Status { get; init; } = string.Empty;
        public string? Message { get; init; } = string.Empty;
    }
}