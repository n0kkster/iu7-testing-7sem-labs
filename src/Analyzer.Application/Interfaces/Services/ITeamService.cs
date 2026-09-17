using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

namespace Analyzer.Application.Interfaces.Services;

public interface ITeamService
{
    public Task<IReadOnlyCollection<TeamDto>> GetAllTeamsAsync();

    public Task<TeamDto> CreateTeamAsync(CreateTeamDto dto);
    public Task UpdateTeamAsync(Guid teamId, CreateTeamDto dto);
    public Task DeleteTeamAsync(Guid teamId);
    public Task<bool> ExistsAsync(Guid teamId); 

    public Task<IReadOnlyCollection<UserDto>> GetTeamMembersAsync(Guid teamId);
    public Task AddMemberAsync(Guid teamId, User user);
    public Task RemoveMemberAsync(Guid teamId, Guid targetUserId);
}