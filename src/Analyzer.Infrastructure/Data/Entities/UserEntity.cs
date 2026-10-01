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

    public TeamEntity? Team { get; set; }
    public AvatarEntity? Avatar { get; set; }
    public List<InviteEntity> ActivatedInvites { get; set; } = [];
}