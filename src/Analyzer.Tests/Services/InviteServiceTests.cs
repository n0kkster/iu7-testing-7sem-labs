using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
using Moq;

namespace Analyzer.Tests.Services;

public class InviteServiceTests
{
    private readonly Mock<IInviteRepository> _inviteRepoMock;
    private readonly Mock<ITeamService> _teamServiceMock;
    private readonly InviteService _inviteService;

    public InviteServiceTests()
    {
        _inviteRepoMock = new Mock<IInviteRepository>();
        _teamServiceMock = new Mock<ITeamService>();
        _inviteService = new InviteService(_inviteRepoMock.Object, _teamServiceMock.Object);
    }

    #region Generate Invite

    [Fact]
    public async Task GenerateInviteAsync_TeamExists_AddsInviteAndReturnsCode()
    {
        // Arrange
        var dto = new GenerateInviteDto 
        { 
            Email = "new@test.com", 
            TeamId = Guid.NewGuid(), 
            ValidForDays = 3, 
            Role = Role.Developer 
        };
        _teamServiceMock.Setup(t => t.ExistsAsync(dto.TeamId)).ReturnsAsync(true);

        // Act
        var inviteDto = await _inviteService.GenerateInviteAsync(dto);

        // Assert
        Assert.NotEmpty(inviteDto.Code);
        _inviteRepoMock.Verify(r => r.AddAsync(It.Is<Invite>(i => 
            i.TeamId == dto.TeamId && 
            i.Role == dto.Role && 
            i.TargetEmail == dto.Email)), Times.Once);
    }

    [Fact]
    public async Task GenerateInviteAsync_TeamNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new GenerateInviteDto { TeamId = Guid.NewGuid() };
        _teamServiceMock.Setup(t => t.ExistsAsync(dto.TeamId)).ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _inviteService.GenerateInviteAsync(dto));
        Assert.Equal("Команда не найдена", ex.Message);
    }

    #endregion

    #region Invite Consumption Flow

    [Fact]
    public async Task GetValidatedInviteDetailsAsync_ValidCode_ReturnsRoleAndTeam()
    {
        // Arrange
        var email = "test@test.com";
        var teamId = Guid.NewGuid();
        var role = Role.Developer;
        var invite = new Invite(email, 7, teamId, role);

        _inviteRepoMock.Setup(r => r.GetByCodeAsync(invite.Code)).ReturnsAsync(invite);

        // Act
        var (returnedRole, returnedTeamId) = await _inviteService.GetValidatedInviteDetailsAsync(invite.Code, email);

        // Assert
        Assert.Equal(role, returnedRole);
        Assert.Equal(teamId, returnedTeamId);
    }

    [Fact]
    public async Task GetValidatedInviteDetailsAsync_WrongEmail_ThrowsArgumentException()
    {
        // Arrange
        var invite = new Invite("real@test.com", 7, Guid.NewGuid(), Role.Developer);
        _inviteRepoMock.Setup(r => r.GetByCodeAsync(invite.Code)).ReturnsAsync(invite);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _inviteService.GetValidatedInviteDetailsAsync(invite.Code, "wrong@test.com"));
    }

    [Fact]
    public async Task ConsumeInviteAsync_ValidFlow_UpdatesStatusAndAddsToTeam()
    {
        // Arrange
        var email = "test@test.com";
        var teamId = Guid.NewGuid();
        var role = Role.Developer;
        var invite = new Invite(email, 7, teamId, role);
        
        var user = User.CreateInvitedUser("username", email, "hash", role, teamId);

        _inviteRepoMock.Setup(r => r.GetByCodeAsync(invite.Code)).ReturnsAsync(invite);

        // Act
        await _inviteService.ConsumeInviteAsync(invite.Code, user);

        // Assert
        Assert.Equal(InviteStatus.Activated, invite.Status);
        Assert.Equal(user.Id, invite.ActivatedByUserId);
        
        _teamServiceMock.Verify(t => t.AddMemberAsync(teamId, user), Times.Once);
        _inviteRepoMock.Verify(r => r.UpdateAsync(invite), Times.Once);
    }

    [Fact]
    public async Task ConsumeInviteAsync_InvalidCode_ThrowsKeyNotFoundException()
    {
        // Arrange
        var user = User.CreateAdmin("admin", "admin@test.com", "hash");
        _inviteRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Invite?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _inviteService.ConsumeInviteAsync("wrong_code", user));
    }

    #endregion

    #region Revoke

    [Fact]
    public async Task RevokeInviteAsync_ValidInvite_CallsRevokeAndUpdate()
    {
        // Arrange
        var invite = new Invite("test@test.com", 7, Guid.NewGuid(), Role.Developer);
        _inviteRepoMock.Setup(r => r.GetByIdAsync(invite.Id)).ReturnsAsync(invite);

        // Act
        await _inviteService.RevokeInviteAsync(invite.Id);

        // Assert
        Assert.Equal(InviteStatus.Revoked, invite.Status);
        _inviteRepoMock.Verify(r => r.UpdateAsync(invite), Times.Once);
    }

    #endregion
}