using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Имя обязателено")]
        public string FirstName { get; init; } = default!;

        [Required(ErrorMessage = "Фамилия обязателено")]
        public string LastName { get; init; } = default!;

        [Required(ErrorMessage = "Отчество обязателено")]
        public string MiddleName { get; init; } = default!;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; init; } = default!;

        [Required(ErrorMessage = "Номер телефона обязателен")]
        [Phone]
        public string PhoneNumber { get; init; } = default!;
    }
}
