using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Persistence.Postgres;

public class SystemRepository(AnalyzerDbContext context) : ISystemRepository
{
    private readonly AnalyzerDbContext _context = context;
    public async Task<ITSystem?> GetByIdAsync(Guid systemId)
    {
        return await _context.ITSystems.FirstOrDefaultAsync(s => s.Id == systemId);
    }

    public async Task<IReadOnlyCollection<ITSystem>> GetByTeamIdAsync(Guid teamId)
    {
        return await _context.ITSystems
            .Where(s => s.TeamId == teamId)
            .ToListAsync();
    }

    public async Task AddAsync(ITSystem system)
    {
        await _context.ITSystems.AddAsync(system);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ITSystem system)
    {
        _context.ITSystems.Update(system);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid systemId)
    {
        var system = await _context.ITSystems.FindAsync(systemId);
        if (system is not null)
        {
            _context.ITSystems.Remove(system);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsWithNameAsync(Guid teamId, string name)
    {
        return await _context.ITSystems
            .AnyAsync(s => s.TeamId == teamId && s.Name.ToLower() == name.ToLower());
    }
}