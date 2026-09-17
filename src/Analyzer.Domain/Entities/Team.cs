namespace Analyzer.Domain.Entities;

public class Team(string name, string description)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.IsNullOrWhiteSpace(name) ?
            throw new ArgumentException("Имя команды обязательно") :
            name;
    public string Description { get; private set; } = description ?? string.Empty;

    private readonly List<Guid> _memberIds = [];
    public IReadOnlyCollection<Guid> MemberIds => _memberIds.AsReadOnly();

    public void UpdateProfile(string name, string description)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name;
        Description = description ?? Description;
    }

    public void AddMember(Guid userId)
    {
        if (!_memberIds.Contains(userId))
        {
            _memberIds.Add(userId);
        }
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