using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Analyzer.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Persistence.Postgres;

public class SystemRepository(AnalyzerDbContext context) : ISystemRepository
{
    private readonly AnalyzerDbContext _context = context;
    public async Task<ITSystem?> GetByIdAsync(Guid systemId)
    {
        return (await _context.ITSystems.FirstOrDefaultAsync(s => s.Id == systemId))?.ToDomain();
    }

    public async Task<IReadOnlyCollection<ITSystem>> GetByTeamIdAsync(Guid teamId)
    {
        return await _context.ITSystems
            .Where(s => s.TeamId == teamId)
            .Select(s => s.ToDomain())
            .ToListAsync();
    }

    public async Task AddAsync(ITSystem system)
    {
        await _context.ITSystems.AddAsync(system.ToEntity());
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ITSystem system)
    {
        var entity = await _context.ITSystems.FindAsync(system.Id);
        if (entity != null)
        {
            entity.Name = system.Name;
            entity.Description = system.Description;
            entity.UpdatedAt = system.UpdatedAt;

            await _context.SaveChangesAsync();
        }
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