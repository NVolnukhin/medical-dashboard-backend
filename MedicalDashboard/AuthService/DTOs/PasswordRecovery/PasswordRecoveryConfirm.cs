using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.PasswordRecovery;

public sealed record PasswordRecoveryConfirm
{
    [Required(ErrorMessage = "Токен обязателен")]
    public string Token { get; init; }
    
    [Required(ErrorMessage = "Пароль не может быть пустым")]
    [MinLength(8, ErrorMessage = "Слишком короткий пароль")]
    public string NewPassword { get; init; }
    
    [Required(ErrorMessage = "Пароль не может быть пустым")]
    [MinLength(8, ErrorMessage = "Слишком короткий пароль")]
    public string ConfirmPassword { get; init; }

    public PasswordRecoveryConfirm(
        string token,
        string newPassword,
        string confirmPassword)
    {
        Token = token;
        NewPassword = newPassword;
        ConfirmPassword = confirmPassword;
    }
}