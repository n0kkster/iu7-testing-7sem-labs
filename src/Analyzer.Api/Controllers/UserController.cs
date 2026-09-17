using System.Security.Claims;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Analyzer.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _userService.RegisterAsync(dto);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _userService.LoginAsync(dto);
        return Ok(new { Token = token });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
        if (string.IsNullOrEmpty(userIdString) || 
            !Guid.TryParse(userIdString, out var userId))
        {
            Log.Error("Не найден Claim, содержащий User Id.");
            return Unauthorized("Ошибка получения информации о профиле.");
        }

        var profile = await _userService.GetProfileAsync(userId);
        if (profile is null)
        {
            Log.Error($"Профиль пользователя {userId} не найден.");
            return NotFound("Ошибка получения информации о профиле.");
        }

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.UpdateProfileAsync(userId, dto);
        return Ok();
        
    }

    [Authorize]
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _userService.DeleteAsync(id);
        return Ok();
    }

    [HttpPost("swagger-login")]
    [Consumes("application/x-www-form-urlencoded")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> SwaggerLogin([FromForm] string username, [FromForm] string password)
    {
        try
        {
            var token = await _userService.LoginAsync(new()
            {
                Username = username,
                Password = password
            });
            
            return Ok(new 
            { 
                access_token = token, 
                token_type = "Bearer" 
            });
        }
        catch (Exception)
        {
            return BadRequest(new 
            { 
                error = "invalid_grant", 
                error_description = "Неверный логин или пароль" 
            });
        }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}