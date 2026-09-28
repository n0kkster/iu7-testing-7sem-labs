namespace Analyzer.Tests.Common.Builders;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

public class UserBuilder
{
    private string _username = "test_user";
    private string _email = "valid_user@analyzer.local";
    private string _passwordHash = "superpupermegahash";
    private Role _role = Role.Developer;
    private Guid _teamId = Guid.NewGuid();

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithRole(Role role)
    {
        _role = role;
        return this;
    }

    public UserBuilder WithTeamId(Guid teamId)
    {
        _teamId = teamId;
        return this;
    }

    public User BuildInvited()
    {
        return User.CreateInvitedUser(_username, _email, _passwordHash, _role, _teamId);
    }

    public User BuildAdmin()
    {
        return User.CreateAdmin(_username, _email, _passwordHash);
    }
}