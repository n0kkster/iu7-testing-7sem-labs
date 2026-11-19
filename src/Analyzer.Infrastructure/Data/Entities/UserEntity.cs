namespace Analyzer.Infrastructure.Data.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Role { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? AvatarId { get; set; }

    public bool IsTwoFactorEnabled { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public string? TwoFactorCode { get; set; }
    public DateTimeOffset? TwoFactorCodeExpiry { get; set; }
    public string? UnlockCode { get; set; }
    public DateTimeOffset? UnlockCodeExpiry { get; set; }

    public TeamEntity? Team { get; set; }
    public AvatarEntity? Avatar { get; set; }
    public List<InviteEntity> ActivatedInvites { get; set; } = [];
}