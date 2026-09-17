using Analyzer.Domain.Entities;

namespace Analyzer.Application.Interfaces.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid teamId);
    Task<IReadOnlyCollection<Team>> GetAllTeamsAsync();
    
    Task AddAsync(Team team);
    Task UpdateAsync(Team team);
    Task DeleteAsync(Guid teamId);
}