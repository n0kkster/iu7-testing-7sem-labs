using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;

namespace Analyzer.Application.Services;

public class InviteService(IInviteRepository inviteRepository, ITeamService teamService) : IInviteService
{
    private readonly IInviteRepository _inviteRepository = inviteRepository;
    private readonly ITeamService _teamService = teamService;

    public async Task<InviteDto> GenerateInviteAsync(GenerateInviteDto dto)
    {
        if (!await _teamService.ExistsAsync(dto.TeamId)) 
            throw new KeyNotFoundException("Команда не найдена");

        var invite = new Invite(dto.Email, dto.ValidForDays, dto.TeamId, dto.Role);
        await _inviteRepository.AddAsync(invite);

        return MapToDto(invite);
    }

    public async Task<(Role Role, Guid TeamId)> GetValidatedInviteDetailsAsync(string code, string email)
    {
        var invite = await _inviteRepository.GetByCodeAsync(code) 
            ?? throw new KeyNotFoundException("Приглашение не найдено или код неверен");

        invite.ValidateCanBeConsumedBy(email);

        return invite.GetDetails();
    }

    public async Task ConsumeInviteAsync(string code, User user)
    {
        var invite = await _inviteRepository.GetByCodeAsync(code) 
            ?? throw new KeyNotFoundException("Приглашение не найдено");

        invite.Consume(user.Id); 
        
        await _teamService.AddMemberAsync(invite.TeamId, user);
        await _inviteRepository.UpdateAsync(invite);
    }


    public async Task RevokeInviteAsync(Guid inviteId)
    {
        var invite = await _inviteRepository.GetByIdAsync(inviteId) 
            ?? throw new KeyNotFoundException("Приглашение не найдено");

        invite.Revoke();

        await _inviteRepository.UpdateAsync(invite);
    }

    public async Task<IReadOnlyCollection<InviteDto>> GetTeamInvitesAsync(Guid teamId)
    {
        var invites = await _inviteRepository.GetByTeamIdAsync(teamId);
        
        return invites.Select(MapToDto).ToList();
    }

    private InviteDto MapToDto(Invite invite)
    {
        return new InviteDto(
            invite.Id, 
            invite.Role,
            invite.TargetEmail,
            invite.Code,
            invite.ExpirationDate,
            invite.Status
        );
    }
}