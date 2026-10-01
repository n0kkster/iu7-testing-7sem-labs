namespace Analyzer.IntegrationTests.Services;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Shared.DTO.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection("Database collection")]
public class UserLifecycleITCase : IDisposable
{
    private readonly IServiceScope _scope;
    private readonly IUserService _userService;
    private readonly ITeamService _teamService;
    private readonly IInviteService _inviteService;
    private readonly IUserRepository _userRepository;

    public UserLifecycleITCase(SharedDatabaseFixture fixture)
    {
        _scope = fixture.ServiceProvider.CreateScope();

        _userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
        _teamService = _scope.ServiceProvider.GetRequiredService<ITeamService>();
        _inviteService = _scope.ServiceProvider.GetRequiredService<IInviteService>();
        _userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Fact]
    public async Task ExecuteCompleteUserLifecycle_ShouldPersistAndMutateDataCorrectly()
    {
        // ====================================================================
        // ARRANGE
        // ====================================================================
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var initialPassword = "StrongPassword123!";
        var updatedPassword = "EvenStrongerPassword456!";
        var userEmail = $"dev_{uniqueSuffix}@analyzer.local";
        var initialUsername = $"user_{uniqueSuffix}";
        var updatedUsername = $"renamed_{uniqueSuffix}";

        // Создаем реальную команду в базе данных через сервис
        var createdTeam = await _teamService.CreateTeamAsync(new CreateTeamDto
        {
            Name = $"Team_{uniqueSuffix}",
            Description = "Integration Test Team"
        });

        // Генерируем реальный инвайт для вступления
        var generatedInvite = await _inviteService.GenerateInviteAsync(new GenerateInviteDto
        {
            TeamId = createdTeam.Id,
            Email = userEmail,
            Role = Role.Developer,
            ValidForDays = 3
        });

        // ====================================================================
        // ACT & ASSERT: ШАГ 1 — Регистрация пользователя по инвайту
        // ====================================================================
        var registerDto = new RegisterDto
        {
            Username = initialUsername,
            Email = userEmail,
            Password = initialPassword,
            InviteCode = generatedInvite.Code
        };

        var registeredUserId = await _userService.RegisterAsync(registerDto);

        // Проверяем состояние в БД: пользователь создан, инвайт погашен, команда пополнена
        registeredUserId.Should().NotBeEmpty();

        var persistedUser = await _userRepository.GetByIdAsync(registeredUserId);
        persistedUser.Should().NotBeNull();
        persistedUser!.Email.Should().Be(userEmail);
        persistedUser.Role.Should().Be(Role.Developer);
        persistedUser.TeamId.Should().Be(createdTeam.Id);

        var teamMembers = await _teamService.GetTeamMembersAsync(createdTeam.Id);
        teamMembers.Should().Contain(u => u.Id == registeredUserId);

        // ====================================================================
        // ACT & ASSERT: ШАГ 2 — Аутентификация (Логин)
        // ====================================================================
        var loginDto = new LoginDto
        {
            Username = initialUsername,
            Password = initialPassword
        };

        var jwtToken = await _userService.LoginAsync(loginDto);

        jwtToken.Should().NotBeNullOrWhiteSpace();
        jwtToken.Split('.').Should().HaveCount(3);

        // ====================================================================
        // ACT & ASSERT: ШАГ 3 — Обновление данных профиля
        // ====================================================================
        var updateProfileDto = new UpdateProfileDto
        {
            Username = updatedUsername,
            Email = userEmail
        };

        await _userService.UpdateProfileAsync(registeredUserId, updateProfileDto);

        var userAfterUpdate = await _userService.GetProfileAsync(registeredUserId);
        userAfterUpdate.Username.Should().Be(updatedUsername);

        // ====================================================================
        // ACT & ASSERT: ШАГ 4 — Смена пароля и проверка входа со старым/новым
        // ====================================================================
        await _userService.ChangePasswordAsync(registeredUserId, initialPassword, updatedPassword);

        // Старый пароль больше не должен подходить
        var failedLoginAct = async () => await _userService.LoginAsync(new LoginDto
        {
            Username = updatedUsername,
            Password = initialPassword
        });
        await failedLoginAct.Should().ThrowAsync<InvalidOperationException>();

        // Новый пароль генерирует валидный токен
        var newJwtToken = await _userService.LoginAsync(new LoginDto
        {
            Username = updatedUsername,
            Password = updatedPassword
        });
        newJwtToken.Should().NotBeNullOrWhiteSpace();

        // ====================================================================
        // ACT & ASSERT: ШАГ 5 — Удаление пользователя
        // ====================================================================
        await _userService.DeleteAsync(registeredUserId);

        var deletedUser = await _userRepository.GetByIdAsync(registeredUserId);
        deletedUser.Should().BeNull();

        var finalTeamMembers = await _teamService.GetTeamMembersAsync(createdTeam.Id);
        finalTeamMembers.Should().NotContain(u => u.Id == registeredUserId);
    }
}