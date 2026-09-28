namespace Analyzer.Tests.Common.Fakes;

using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;

public class InMemoryTeamRepository : ITeamRepository
{
    private readonly List<Team> _teams = [];

    public Task<Team?> GetByIdAsync(Guid teamId)
    {
        var team = _teams.FirstOrDefault(t => t.Id == teamId);
        return Task.FromResult(team);
    }

    public Task<IReadOnlyCollection<Team>> GetAllTeamsAsync()
    {
        return Task.FromResult<IReadOnlyCollection<Team>>(_teams.ToList());
    }

    public Task AddAsync(Team team)
    {
        _teams.Add(team);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Team team)
    {
        var index = _teams.FindIndex(t => t.Id == team.Id);
        if (index != -1)
        {
            _teams[index] = team;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid teamId)
    {
        _teams.RemoveAll(t => t.Id == teamId);
        return Task.CompletedTask;
    }
}