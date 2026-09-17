using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;

namespace Analyzer.Application.Interfaces.Services;

public interface IInviteService
{
    Task<InviteDto> GenerateInviteAsync(GenerateInviteDto dto);    
    Task RevokeInviteAsync(Guid inviteId);
    Task<IReadOnlyCollection<InviteDto>> GetTeamInvitesAsync(Guid teamId);

    Task<(Role Role, Guid TeamId)> GetValidatedInviteDetailsAsync(string code, string email);
    Task ConsumeInviteAsync(string code, User user);
}