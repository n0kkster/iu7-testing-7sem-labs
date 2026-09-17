namespace Analyzer.Domain.Entities;

public class ITSystem(string name, string description, Guid teamId)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Имя системы обязательно")
            : name;
    public string Description { get; private set; } = description;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Guid TeamId { get; init; } = teamId;

    public void UpdateDetails(string name, string description)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Имя системы обязательно")
            : name;
        Description = description;

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}