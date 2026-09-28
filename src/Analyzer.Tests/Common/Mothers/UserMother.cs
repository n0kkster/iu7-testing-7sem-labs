namespace Analyzer.Tests.Common.Mothers;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Tests.Common.Builders;

public static class UserMother
{
    public static User CreateAdmin() =>
        new UserBuilder()
            .WithUsername("global_admin")
            .WithEmail("admin@analyzer.local")
            .BuildAdmin();

    public static User CreateDeveloper(Guid? teamId = null) =>
        new UserBuilder()
            .WithUsername("dev_user")
            .WithEmail("dev@company.com")
            .WithRole(Role.Developer)
            .WithTeamId(teamId ?? Guid.NewGuid())
            .BuildInvited();

    public static User CreateArchitect(Guid? teamId = null) =>
        new UserBuilder()
            .WithUsername("arch_user")
            .WithEmail("arch@company.com")
            .WithRole(Role.Architect)
            .WithTeamId(teamId ?? Guid.NewGuid())
            .BuildInvited();

    public static User CreateSre(Guid? teamId = null) =>
        new UserBuilder()
            .WithUsername("sre_user")
            .WithEmail("sre@company.com")
            .WithRole(Role.SRE)
            .WithTeamId(teamId ?? Guid.NewGuid())
            .BuildInvited();
}