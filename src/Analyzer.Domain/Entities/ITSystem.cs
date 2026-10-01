namespace Analyzer.Domain.Entities;

public class ITSystem
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid TeamId { get; init; }

    public ITSystem(string name, string description, Guid teamId)
    {
        Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Имя системы обязательно") : name;
        Description = description;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        TeamId = teamId;
    }

    private ITSystem(Guid id, string name, string description, Guid teamId, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        Description = description;
        TeamId = teamId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static ITSystem Restore(Guid id, string name, string description, Guid teamId, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        return new ITSystem(id, name, description, teamId, createdAt, updatedAt);
    }

    public void UpdateDetails(string name, string description)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Имя системы обязательно") : name;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}