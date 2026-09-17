using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Persistence.Postgres;

public class InviteRepository(AnalyzerDbContext context) : IInviteRepository
{
    private readonly AnalyzerDbContext _context = context;
    public async Task<Invite?> GetByIdAsync(Guid id)
    {
        return await _context.Invites.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invite?> GetByCodeAsync(string code)
    {
        return await _context.Invites.FirstOrDefaultAsync(i => i.Code == code);
    }

    public async Task<IReadOnlyCollection<Invite>> GetByTeamIdAsync(Guid teamId)
    {
        return await _context.Invites
            .Where(i => i.TeamId == teamId)
            .ToListAsync();
    }

    public async Task AddAsync(Invite invite)
    {
        await _context.Invites.AddAsync(invite);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Invite invite)
    {
        _context.Invites.Update(invite);
        await _context.SaveChangesAsync();
    }
}