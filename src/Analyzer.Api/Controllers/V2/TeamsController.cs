namespace Analyzer.Api.Controllers.V2;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v2/teams")]
public class TeamsController(ITeamService teamService, IInviteService inviteService) : ControllerBase
{
    private readonly ITeamService _teamService = teamService;
    private readonly IInviteService _inviteService = inviteService;

    #region Команды

    [HttpGet]
    [ActionName("getAllTeams")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<TeamDto>>> GetAllTeams()
    {
        var teams = await _teamService.GetAllTeamsAsync();
        return Ok(teams);
    }

    [HttpPost]
    [ActionName("createTeam")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
    {
        var team = await _teamService.CreateTeamAsync(dto);
        return StatusCode(StatusCodes.Status201Created, team);
    }

    [HttpDelete("{id:guid}")]
    [ActionName("deleteTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        await _teamService.DeleteTeamAsync(id);
        return NoContent();
    }

    #endregion

    #region Инвайты команды

    [HttpGet("{teamId:guid}/invites")]
    [ActionName("getTeamInvites")]
    [ProducesResponseType(typeof(IReadOnlyCollection<InviteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<InviteDto>>> GetTeamInvites(Guid teamId)
    {
        if (!await _teamService.ExistsAsync(teamId))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ресурс не найден",
                Detail = $"Команда с ID {teamId} не найдена",
                Instance = HttpContext.Request.Path
            });
        }

        var invites = await _inviteService.GetTeamInvitesAsync(teamId);
        return Ok(invites);
    }

    [HttpPost("{teamId:guid}/invites")]
    [ActionName("createTeamInvite")]
    [ProducesResponseType(typeof(InviteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTeamInvite(Guid teamId, [FromBody] CreateInviteDto dto)
    {
        if (!await _teamService.ExistsAsync(teamId))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ресурс не найден",
                Detail = $"Команда с ID {teamId} не найдена",
                Instance = HttpContext.Request.Path
            });
        }

        var generateDto = new GenerateInviteDto
        {
            TeamId = teamId,
            Email = dto.Email,
            Role = dto.Role,
            ValidForDays = dto.ValidForDays
        };

        var invite = await _inviteService.GenerateInviteAsync(generateDto);
        return StatusCode(StatusCodes.Status201Created, invite);
    }

    [HttpDelete("{teamId:guid}/invites/{inviteId:guid}")]
    [ActionName("revokeTeamInvite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeTeamInvite(Guid teamId, Guid inviteId)
    {
        if (!await _teamService.ExistsAsync(teamId))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ресурс не найден",
                Detail = $"Команда с ID {teamId} не найдена",
                Instance = HttpContext.Request.Path
            });
        }

        await _inviteService.RevokeInviteAsync(inviteId);
        return NoContent();
    }

    #endregion
}