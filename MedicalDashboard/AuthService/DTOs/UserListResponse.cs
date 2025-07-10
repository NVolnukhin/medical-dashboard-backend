namespace AuthService.DTOs
{
    public class UserListResponse
    {
        public Guid Id { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? MiddleName { get; init; }
        public string Email { get; init; } = default!;
        public string PhoneNumber { get; init; } = default!;
        public bool IsActive { get; init; }
        public string Role { get; init; } = default!;
    }
} 