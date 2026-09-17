namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

[Authorize]
[ApiController]
[Route("api/v1/systems")]
public class SystemsController(ISystemService systemsService) : ControllerBase
{
    readonly ISystemService _systemsService = systemsService;

    public record SystemStorage(IReadOnlyCollection<ComponentDto> Components,
                                 IReadOnlyCollection<LinkDto> Links);

    [HttpGet]
    public async Task<IActionResult> ListSystemsByTeam([FromQuery] Guid teamId)
    {
        var systems = await _systemsService.GetSystemsByTeamIdAsync(teamId);
        return Ok(systems);
    }

    [HttpPost]
    public async Task<IActionResult> AddSystem([FromBody] CreateITSystemDto dto)
    {
        var createdId = await _systemsService.CreateSystemAsync(dto);
        return Ok(createdId);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSystem(Guid id)
    {
        await _systemsService.DeleteSystemAsync(id);
        return Ok();
    }


    [HttpGet("export")]
    public async Task<IActionResult> ExportSystem([FromQuery] Guid id)
    {
        Console.WriteLine("export called");
        var (components, links) = await _systemsService.ExportSystemAsync(id);
        var jsonString = JsonSerializer.Serialize(new SystemStorage(components, links));
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var fileName = $"system-backup-{DateTime.Now:yyyy-MM-dd_HH-mm}.json";

        return File(bytes, "application/json", fileName);
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportSystem(IFormFile file, [FromForm] string importData)
    {
        try
        {
            if (file is null || file.Length == 0)
                return BadRequest("Ошибка получения файла");

            var dto = JsonSerializer.Deserialize<CreateITSystemDto>(
                importData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (dto is null)
                return BadRequest("Некорректные данные импорта");

            using var stream = file.OpenReadStream();
            var result = await JsonSerializer.DeserializeAsync<SystemStorage>(stream);

            if (result is null)
                return Problem("Ошибка десереализации файла", statusCode: 500);

            var (components, links) = result;

            var newSystemId = await _systemsService.ImportSystemAsync(components, links, dto);

            return Ok(newSystemId);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }
}