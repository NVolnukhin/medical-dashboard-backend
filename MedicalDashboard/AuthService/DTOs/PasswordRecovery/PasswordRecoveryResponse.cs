namespace AuthService.DTOs.PasswordRecovery;

public record PasswordRecoveryResponse(
    bool Success,
    string Message, 
    string? Token = null);