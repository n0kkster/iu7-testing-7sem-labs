using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
using Moq;
using Xunit;

namespace Analyzer.Tests.Services;

public class TeamServiceTests
{
    private readonly Mock<ITeamRepository> _teamRepoMock;
    private readonly Mock<ISystemRepository> _systemRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly TeamService _teamService;

    public TeamServiceTests()
    {
        _teamRepoMock = new Mock<ITeamRepository>();
        _systemRepoMock = new Mock<ISystemRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _teamService = new TeamService(_teamRepoMock.Object, _systemRepoMock.Object, _userRepoMock.Object);
    }

    #region Create, Update, Delete, Exists

    [Fact]
    public async Task CreateTeamAsync_AddsToRepository_AndReturnsId()
    {
        // Arrange
        var dto = new CreateTeamDto { Name = "Backend Team", Description = "Go/C# Devs" };

        // Act
        var team = await _teamService.CreateTeamAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, team.Id);
        _teamRepoMock.Verify(r => r.AddAsync(It.Is<Team>(t => t.Name == "Backend Team" && t.Description == "Go/C# Devs")), Times.Once);
    }

    [Fact]
    public async Task UpdateTeamAsync_TeamExists_UpdatesProfile()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new Team("Old Name", "Old Desc");
        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);

        var dto = new CreateTeamDto { Name = "New Name", Description = "New Desc" };

        // Act
        await _teamService.UpdateTeamAsync(teamId, dto);

        // Assert
        Assert.Equal("New Name", team.Name);
        Assert.Equal("New Desc", team.Description);
    }

    [Fact]
    public async Task UpdateTeamAsync_TeamNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _teamRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Team?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _teamService.UpdateTeamAsync(Guid.NewGuid(), new CreateTeamDto()));
    }

    [Fact]
    public async Task DeleteTeamAsync_NoSystemsOwned_DeletesTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _systemRepoMock.Setup(r => r.GetByTeamIdAsync(teamId)).ReturnsAsync(Array.Empty<ITSystem>());

        // Act
        await _teamService.DeleteTeamAsync(teamId);

        // Assert
        _teamRepoMock.Verify(r => r.DeleteAsync(teamId), Times.Once);
    }

    [Fact]
    public async Task DeleteTeamAsync_OwnsSystems_ThrowsInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var system = new ITSystem("Sys1", "Desc", teamId);
        _systemRepoMock.Setup(r => r.GetByTeamIdAsync(teamId)).ReturnsAsync([system]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.DeleteTeamAsync(teamId));
        Assert.Contains("Невозможно удалить команду, владеющую хоть одной системой", ex.Message);
        _teamRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistsAsync_ReturnsCorrectStatus(bool exists)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = exists ? new Team("Name", "Desc") : null;
        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);

        // Act
        var result = await _teamService.ExistsAsync(teamId);

        // Assert
        Assert.Equal(exists, result);
    }

    #endregion

    #region Member Management

    [Fact]
    public async Task AddMemberAsync_TeamExists_AddsUserToTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new Team("Name", "Desc");
        var user = User.CreateInvitedUser(
            "dev", 
            "dev@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );
        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);

        // Act
        await _teamService.AddMemberAsync(teamId, user);

        // Assert
        Assert.Contains(user.Id, team.MemberIds);
    }

    [Fact]
    public async Task RemoveMemberAsync_TeamExists_RemovesUserFromTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new Team("Name", "Desc");
        var userId = Guid.NewGuid();

        team.AddMember(userId);

        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);

        // Act
        await _teamService.RemoveMemberAsync(teamId, userId);

        // Assert
        Assert.DoesNotContain(userId, team.MemberIds);
    }
    [Fact]
    public async Task GetTeamMembersAsync_ReturnsMappedUserDtos()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new Team("Name", "Desc");

        var user = User.CreateInvitedUser(
            "username", 
            "test@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );
        team.AddMember(user.Id);

        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        // Act
        var result = await _teamService.GetTeamMembersAsync(teamId);

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Username, dto.Username);
        Assert.Equal(user.Email, dto.Email);
    }

    [Fact]
    public async Task GetTeamMembersAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new Team("Name", "Desc");
        var ghostUserId = Guid.NewGuid();
        team.AddMember(ghostUserId);

        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
        _userRepoMock.Setup(r => r.GetByIdAsync(ghostUserId)).ReturnsAsync((User?)null); // Пользователь пропал из БД

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _teamService.GetTeamMembersAsync(teamId));
        Assert.Contains("не существует", ex.Message);
    }

    #endregion
}