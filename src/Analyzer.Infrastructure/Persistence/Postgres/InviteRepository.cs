using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Analyzer.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Persistence.Postgres;

public class InviteRepository(AnalyzerDbContext context) : IInviteRepository
{
    private readonly AnalyzerDbContext _context = context;
    public async Task<Invite?> GetByIdAsync(Guid id)
    {
        return (await _context.Invites.FirstOrDefaultAsync(i => i.Id == id))?.ToDomain();
        
    }

    public async Task<Invite?> GetByCodeAsync(string code)
    {
        return (await _context.Invites.FirstOrDefaultAsync(i => i.Code == code))?.ToDomain();
    }

    public async Task<IReadOnlyCollection<Invite>> GetByTeamIdAsync(Guid teamId)
    {
        return await _context.Invites
            .Where(i => i.TeamId == teamId)
            .Select(i => i.ToDomain())
            .ToListAsync();
    }

    public async Task AddAsync(Invite invite)
    {
        await _context.Invites.AddAsync(invite.ToEntity());
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Invite invite)
    {
        var entity = await _context.Invites.FindAsync(invite.Id);
        if (entity != null)
        {
            entity.Status = (int)invite.Status;
            entity.ActivatedByUserId = invite.ActivatedByUserId;

            await _context.SaveChangesAsync();
        }
    }
}