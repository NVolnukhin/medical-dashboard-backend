namespace AuthService.Models
{
    public class User
    {
        public Guid Id { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? MiddleName { get; init; }

        public required string Email { get; init; }
        public required string PhoneNumber { get; init; }
        public required string Password { get; set; }
        public string Salt { get; set; }
        public bool IsActive { get; init; }
    }
}