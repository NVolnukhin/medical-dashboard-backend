using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.PasswordRecovery;

public sealed record PasswordRecoveryRequest(
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    string Email
);