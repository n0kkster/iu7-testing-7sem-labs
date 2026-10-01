using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO.Common;
using Analyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Analyzer.Infrastructure.Data.Mappers;

namespace Analyzer.Infrastructure.Persistence.Postgres;

public class AvatarRepository(AnalyzerDbContext context) : IAvatarRepository
{
    public async Task<Avatar?> GetByIdAsync(Guid id)
    {
        return (await context.Avatars.FirstOrDefaultAsync(a => a.Id == id))?.ToDomain();
    }

    public async Task<Avatar?> GetByHashAsync(Guid userId, string hash)
    {
        return (await context.Avatars
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Hash == hash))?.ToDomain();

    }

    public async Task<IReadOnlyCollection<AvatarDto>> GetHistoryByUserIdAsync(Guid userId)
    {
        return await context.Avatars
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AvatarDto(a.Id, a.CreatedAt, a.ContentType, a.Data))
            .ToListAsync();
    }

    public async Task AddAsync(Avatar avatar)
    {
        await context.Avatars.AddAsync(avatar.ToEntity());
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var avatar = await context.Avatars.FindAsync(id);
        if (avatar is not null)
        {
            context.Avatars.Remove(avatar);
            await context.SaveChangesAsync();
        }
    }
}