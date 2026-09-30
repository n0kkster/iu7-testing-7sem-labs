namespace Analyzer.Api.Controllers.V1;

using System.Security.Claims;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v1/avatars")]
public class AvatarController(IAvatarService avatarService) : ControllerBase
{
    private readonly IAvatarService _avatarService = avatarService;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Ошибка получения файла");

        using var stream = file.OpenReadStream();
        var avatarId = await _avatarService.UploadNewAvatarAsync(UserId, stream);

        return CreatedAtAction(nameof(GetById), new { id = avatarId }, new { id = avatarId });
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyCollection<AvatarDto>>> GetHistory()
    {
        var history = await _avatarService.GetAvatarHistoryAsync(UserId);
        return Ok(history);
    }

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)] 
    public async Task<IActionResult> GetById(Guid id)
    {
        var avatar = await _avatarService.GetAvatarFileAsync(id);

        if (avatar is null)
            return NotFound();

        if (avatar.UserId != UserId)
            return Forbid();

        return File(avatar.Data, avatar.ContentType);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _avatarService.DeleteAvatarAsync(UserId, id);
        return NoContent();
    }
}