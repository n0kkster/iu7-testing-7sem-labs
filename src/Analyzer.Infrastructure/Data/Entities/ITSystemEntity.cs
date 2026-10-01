namespace Analyzer.Infrastructure.Data.Entities;

public class ITSystemEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid TeamId { get; set; }

    public TeamEntity? Team { get; set; }
}