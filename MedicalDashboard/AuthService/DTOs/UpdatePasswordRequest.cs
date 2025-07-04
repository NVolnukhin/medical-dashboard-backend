using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public record UpdatePasswordRequest(
[Required] // [Length(64, 64)]
string CurrentPassword,

[Required] // [Length(64, 64)]
string NewPassword,
    
[Required] // [Length(64, 64)]
string ConfirmPassword);