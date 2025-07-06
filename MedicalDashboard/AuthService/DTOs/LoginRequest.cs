using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; init; } = default!;

        [Required(ErrorMessage = "Password обязателен")]
        public string Password { get; init; } = default!;
    }
}

