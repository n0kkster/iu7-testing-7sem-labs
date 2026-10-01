namespace Analyzer.Infrastructure.Data.Entities;

public class TeamEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<UserEntity> Members { get; set; } = [];
    public List<ITSystemEntity> Systems { get; set; } = [];
    public List<InviteEntity> Invites { get; set; } = [];
}