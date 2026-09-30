using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO.Common;

namespace Analyzer.Application.Interfaces.Repositories;

public interface IAvatarRepository
{
    Task<Avatar?> GetByIdAsync(Guid id);
    public Task<Avatar?> GetByHashAsync(Guid userId, string hash);
    Task<IReadOnlyCollection<AvatarDto>> GetHistoryByUserIdAsync(Guid userId);
    Task AddAsync(Avatar avatar);
    Task DeleteAsync(Guid id);
}