using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Analyzer.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Persistence.Postgres;

public class TeamRepository(AnalyzerDbContext context) : ITeamRepository
{
    private readonly AnalyzerDbContext _context = context;
    public async Task<Team?> GetByIdAsync(Guid teamId)
    {
        var team = (await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId))?.ToDomain();
        
        if (team is not null)
        {
            var userIds = await _context.Users
                .Where(u => u.TeamId == teamId)
                .Select(u => u.Id)
                .ToListAsync();

            team.LoadMembers(userIds);
        }

        return team;
    }

    public async Task<IReadOnlyCollection<Team>> GetAllTeamsAsync()
    {
        var teams = await _context.Teams.Select(t => t.ToDomain()).ToListAsync();
        
        var teamUserMap = await _context.Users
            .Where(u => u.TeamId != null)
            .GroupBy(u => u.TeamId ?? Guid.Empty)
            .ToDictionaryAsync(g => g.Key, g => g.Select(u => u.Id).ToList());

        foreach (var team in teams)
        {
            if (teamUserMap.TryGetValue(team.Id, out var userIds))
            {
                team.LoadMembers(userIds);
            }
        }

        return teams;
    }

    public async Task AddAsync(Team team)
    {
        await _context.Teams.AddAsync(team.ToEntity());
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Team team)
    {
        var entity = await _context.Teams.FindAsync(team.Id);
        if (entity != null)
        {
            entity.Name = team.Name;
            entity.Description = team.Description;

            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid teamId)
    {
        var team = await _context.Teams.FindAsync(teamId);
        if (team is not null)
        {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
    }
}