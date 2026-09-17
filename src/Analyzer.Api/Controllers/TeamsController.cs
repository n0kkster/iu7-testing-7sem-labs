using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analyzer.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/teams")]
public class TeamsController(ITeamService teamService) : ControllerBase
{
    readonly ITeamService _teamService = teamService;

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllTeams()
    {
        var teams = await _teamService.GetAllTeamsAsync();
        return Ok(teams);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
    {
        var team = await _teamService.CreateTeamAsync(dto);
        return Ok(team);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        await _teamService.DeleteTeamAsync(id);
        return Ok();
    }
}