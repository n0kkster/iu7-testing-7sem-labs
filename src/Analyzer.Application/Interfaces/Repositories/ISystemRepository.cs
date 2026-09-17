using Analyzer.Domain.Entities;

namespace Analyzer.Application.Interfaces.Repositories;

public interface ISystemRepository
{
    Task<ITSystem?> GetByIdAsync(Guid systemId);
    
    Task<IReadOnlyCollection<ITSystem>> GetByTeamIdAsync(Guid teamId);
    
    Task AddAsync(ITSystem system);
    Task UpdateAsync(ITSystem system);
    Task DeleteAsync(Guid systemId);
    
    Task<bool> ExistsWithNameAsync(Guid teamId, string name);
}