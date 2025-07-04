namespace AuthService.DTOs;

public class UserInfo
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }

    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
}