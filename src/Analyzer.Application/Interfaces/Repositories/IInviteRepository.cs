using Analyzer.Domain.Entities;

namespace Analyzer.Application.Interfaces.Repositories;

public interface IInviteRepository
{
    Task<Invite?> GetByIdAsync(Guid id);
    Task<Invite?> GetByCodeAsync(string code);
    Task<IReadOnlyCollection<Invite>> GetByTeamIdAsync(Guid teamId);
    Task AddAsync(Invite invite);
    Task UpdateAsync(Invite invite);
}