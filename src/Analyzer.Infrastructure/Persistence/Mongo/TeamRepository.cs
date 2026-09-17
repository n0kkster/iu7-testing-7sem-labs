using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using MongoDB.Driver;

namespace Analyzer.Infrastructure.Persistence.Mongo;

public class TeamRepository(IMongoDatabase database) : ITeamRepository
{
    private readonly IMongoCollection<Team> _teams = 
        database.GetCollection<Team>("teams");

    private readonly IMongoCollection<User> _users = 
        database.GetCollection<User>("users");

    public async Task<Team?> GetByIdAsync(Guid teamId)
    {
        var team = await _teams.Find(x => x.Id == teamId).FirstOrDefaultAsync();
        
        if (team is not null)
        {
            var usersInTeam = await _users.Find(x => x.TeamId == teamId).ToListAsync();
            team.LoadMembers(usersInTeam.Select(u => u.Id).ToList()); 
        }

        return team;
    }

    public async Task<IReadOnlyCollection<Team>> GetAllTeamsAsync() =>
        await _teams.Find(_ => true).ToListAsync();

    public async Task AddAsync(Team team) =>
        await _teams.InsertOneAsync(team);

    public async Task UpdateAsync(Team team) =>
        await _teams.ReplaceOneAsync(x => x.Id == team.Id, team);

    public async Task DeleteAsync(Guid teamId) =>
        await _teams.DeleteOneAsync(x => x.Id == teamId);
}