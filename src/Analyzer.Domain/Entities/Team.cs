namespace Analyzer.Domain.Entities;

public class Team
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    private readonly List<Guid> _memberIds = [];
    public IReadOnlyCollection<Guid> MemberIds => _memberIds.AsReadOnly();

    public Team(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Имя команды обязательно") : name;
        Description = description ?? string.Empty;
    }

    private Team(Guid id, string name, string description, IEnumerable<Guid>? memberIds)
    {
        Id = id;
        Name = name;
        Description = description;
        if (memberIds != null)
            _memberIds.AddRange(memberIds);
    }

    public static Team Restore(Guid id, string name, string description, IEnumerable<Guid>? memberIds = null)
    {
        return new Team(id, name, description, memberIds);
    }

    public void UpdateProfile(string name, string description)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name;
        Description = description ?? Description;
    }

    public void AddMember(Guid userId)
    {
        if (!_memberIds.Contains(userId))
            _memberIds.Add(userId);
    }

    public void RemoveMember(Guid userId)
    {
        _memberIds.Remove(userId);
    }

    internal void LoadMembers(IEnumerable<Guid> userIds)
    {
        _memberIds.Clear();
        _memberIds.AddRange(userIds);
    }
}