using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analyzer.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/invites")]
public class InvitesController(IInviteService inviteService) : ControllerBase
{
    readonly IInviteService _inviteService = inviteService;

    [HttpPost]
    public async Task<IActionResult> GenerateInvite([FromBody] GenerateInviteDto dto)
    {
        var invite = await _inviteService.GenerateInviteAsync(dto);
        return Ok(invite);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeInvite(Guid id)
    {
        await _inviteService.RevokeInviteAsync(id);
        return Ok();
    }

    [HttpGet("{teamId}")]
    public async Task<IActionResult> GetTeamInvites(Guid teamId)
    {
        var invites = await _inviteService.GetTeamInvitesAsync(teamId);
        return Ok(invites);
    }
}