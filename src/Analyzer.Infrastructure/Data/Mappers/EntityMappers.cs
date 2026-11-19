namespace Analyzer.Infrastructure.Data.Mappers;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Infrastructure.Data.Entities;

public static class EntityMappers
{
    #region User
    public static UserEntity ToEntity(this User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        PasswordHash = user.PasswordHash,
        Role = (int)user.Role,
        TeamId = user.TeamId,
        AvatarId = user.AvatarId,
        IsTwoFactorEnabled = user.IsTwoFactorEnabled,
        FailedLoginAttempts = user.FailedLoginAttempts,
        LockoutEnd = user.LockoutEnd,
        TwoFactorCode = user.TwoFactorCode,
        TwoFactorCodeExpiry = user.TwoFactorCodeExpiry,
        UnlockCode = user.UnlockCode,
        UnlockCodeExpiry = user.UnlockCodeExpiry,
    };

    public static User ToDomain(this UserEntity entity)
    {
        var user = User.Restore(
            entity.Id,
            entity.Username,
            entity.Email,
            entity.PasswordHash,
            (Role)entity.Role,
            entity.TeamId,
            entity.AvatarId);

        user.LoadAuthState(
            entity.IsTwoFactorEnabled,
            entity.FailedLoginAttempts,
            entity.LockoutEnd,
            entity.TwoFactorCode,
            entity.TwoFactorCodeExpiry,
            entity.UnlockCode,
            entity.UnlockCodeExpiry);

        return user;
    }
    #endregion

    #region Team
    public static TeamEntity ToEntity(this Team team) => new()
    {
        Id = team.Id,
        Name = team.Name,
        Description = team.Description
    };

    public static Team ToDomain(this TeamEntity entity, IEnumerable<Guid>? memberIds = null) =>
        Team.Restore(
            entity.Id,
            entity.Name,
            entity.Description,
            memberIds);
    #endregion

    #region ITSystem
    public static ITSystemEntity ToEntity(this ITSystem system) => new()
    {
        Id = system.Id,
        Name = system.Name,
        Description = system.Description,
        CreatedAt = system.CreatedAt,
        UpdatedAt = system.UpdatedAt,
        TeamId = system.TeamId
    };

    public static ITSystem ToDomain(this ITSystemEntity entity) =>
        ITSystem.Restore(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.TeamId,
            entity.CreatedAt,
            entity.UpdatedAt);
    #endregion

    #region Invite
    public static InviteEntity ToEntity(this Invite invite) => new()
    {
        Id = invite.Id,
        Status = (int)invite.Status,
        TargetEmail = invite.TargetEmail,
        Role = (int)invite.Role,
        Code = invite.Code,
        ExpirationDate = invite.ExpirationDate,
        TeamId = invite.TeamId,
        ActivatedByUserId = invite.ActivatedByUserId
    };

    public static Invite ToDomain(this InviteEntity entity) =>
        Invite.Restore(
            entity.Id,
            entity.TargetEmail,
            entity.Code,
            (Role)entity.Role,
            (InviteStatus)entity.Status,
            entity.ExpirationDate,
            entity.TeamId,
            entity.ActivatedByUserId);
    #endregion

    #region Avatar
    public static AvatarEntity ToEntity(this Avatar avatar) => new()
    {
        Id = avatar.Id,
        UserId = avatar.UserId,
        Data = avatar.Data,
        Hash = avatar.Hash,
        ContentType = avatar.ContentType,
        CreatedAt = avatar.CreatedAt
    };

    public static Avatar ToDomain(this AvatarEntity entity) =>
        Avatar.Restore(
            entity.Id,
            entity.UserId,
            entity.Data,
            entity.Hash,
            entity.ContentType,
            entity.CreatedAt);
    #endregion
}