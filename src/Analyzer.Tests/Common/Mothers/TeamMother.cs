namespace Analyzer.Tests.Common.Mothers;

using Analyzer.Domain.Entities;

public static class TeamMother
{
    public static Team CreateEngineeringTeam() =>
        new("Engineering Team", "Core backend and infrastructure team");

    public static Team CreateFrontendTeam() =>
        new("Frontend Team", "Web UI and mobile client team");

    public static Team CreateTeamWithMembers(params Guid[] memberIds)
    {
        var team = new Team("Staffed Team", "Team with predefined members");
        foreach (var id in memberIds)
        {
            team.AddMember(id);
        }
        return team;
    }
}