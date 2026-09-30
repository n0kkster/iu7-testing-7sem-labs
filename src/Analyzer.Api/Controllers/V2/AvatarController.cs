namespace Analyzer.Api.Controllers.V2;

using System.Security.Claims;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v2/avatars")]
public class AvatarController(IAvatarService avatarService) : ControllerBase
{
    private readonly IAvatarService _avatarService = avatarService;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [ActionName("uploadAvatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации данных",
                Detail = "Файл аватара не предоставлен или пуст",
                Instance = HttpContext.Request.Path
            });
        }

        await using var stream = file.OpenReadStream();
        var avatarId = await _avatarService.UploadNewAvatarAsync(UserId, stream);

        return CreatedAtAction(
            nameof(GetById), 
            new { id = avatarId }, 
            new { id = avatarId });
    }

    [HttpGet("history")]
    [ActionName("getAvatarHistory")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvatarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<AvatarDto>>> GetHistory()
    {
        var history = await _avatarService.GetAvatarHistoryAsync(UserId);
        return Ok(history);
    }

    [HttpGet("{id:guid}")]
    [ActionName("getAvatarById")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var avatar = await _avatarService.GetAvatarFileAsync(id);

        if (avatar is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ресурс не найден",
                Detail = $"Аватар с ID {id} не найден",
                Instance = HttpContext.Request.Path
            });
        }

        if (avatar.UserId != UserId)
        {
            return Forbid();
        }

        return File(avatar.Data, avatar.ContentType);
    }

    [HttpDelete("{id:guid}")]
    [ActionName("deleteAvatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var avatar = await _avatarService.GetAvatarFileAsync(id);

        if (avatar is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ресурс не найден",
                Detail = $"Аватар с ID {id} не существует",
                Instance = HttpContext.Request.Path
            });
        }

        await _avatarService.DeleteAvatarAsync(UserId, id);
        return NoContent();
    }
}