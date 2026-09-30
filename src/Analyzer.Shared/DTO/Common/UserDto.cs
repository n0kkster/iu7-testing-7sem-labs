namespace Analyzer.Shared.DTO.Common;

using Analyzer.Domain.Enums;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; }
    public Guid TeamId { get; set; }
    public Guid? AvatarId { get; set; }
}

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? AvatarId { get; set; } = null;
}

public record PatchUserDto(
    string? Username = null,
    string? Email = null,
    Guid? AvatarId = null,
    string? OldPassword = null,
    string? NewPassword = null
);