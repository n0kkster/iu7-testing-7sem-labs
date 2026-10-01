namespace Analyzer.Infrastructure.Data.Entities;

public class InviteEntity
{
    public Guid Id { get; set; }
    public int Status { get; set; }
    public string TargetEmail { get; set; } = string.Empty;
    public int Role { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset ExpirationDate { get; set; }
    public Guid TeamId { get; set; }
    public Guid? ActivatedByUserId { get; set; }

    public TeamEntity? Team { get; set; }
    public UserEntity? ActivatedByUser { get; set; }
}