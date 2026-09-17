using Analyzer.Domain.Entities;
using Analyzer.IntegrationTests.Fixtures;
using FluentAssertions;
using Analyzer.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Analyzer.Application.Interfaces.Repositories;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class TeamRepositoryTests: IDisposable 
{
    private readonly IServiceScope _scope;    
    private readonly IUserRepository _userRepository;
    private readonly ITeamRepository _teamRepository;

    public TeamRepositoryTests(SharedDatabaseFixture fixture)
    {
        _scope = fixture.ServiceProvider.CreateScope();

        _userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        _teamRepository = _scope.ServiceProvider.GetRequiredService<ITeamRepository>();
    }
    
    public void Dispose()
    {
        _scope.Dispose(); 
    }

    [Fact]
    public async Task AddAsync_ShouldSaveTeamToDatabase()
    {
        // Arrange
        var team = new Team("Alpha Team", "Core backend developers");

        // Act
        await _teamRepository.AddAsync(team);

        // Assert
        var savedTeam = await _teamRepository.GetByIdAsync(team.Id);

        savedTeam.Should().NotBeNull();
        savedTeam!.Name.Should().Be("Alpha Team");
        savedTeam.Description.Should().Be("Core backend developers");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTeam_WithLoadMembers()
    {
        // Arrange
        var team = new Team("Beta Team", "Frontend developers");

        var user1 = User.CreateInvitedUser(
            "user1", 
            "user1@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );
        var user2 = User.CreateInvitedUser(
            "user2", 
            "user2@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );

        await _teamRepository.AddAsync(team);
        await _userRepository.AddAsync(user1);
        await _userRepository.AddAsync(user2);

        // Act
        var result = await _teamRepository.GetByIdAsync(team.Id);

        // Assert
        result.Should().NotBeNull();
        result!.MemberIds.Should().HaveCount(2);
        result.MemberIds.Should().Contain([user1.Id, user2.Id]);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyTeamDetails()
    {
        // Arrange
        var team = new Team("Old Name", "Old Desc");
        await _teamRepository.AddAsync(team);

        // Act
        var teamToUpdate = await _teamRepository.GetByIdAsync(team.Id);
        teamToUpdate!.UpdateProfile("New Name", "New Desc");
        await _teamRepository.UpdateAsync(teamToUpdate);

        // Assert
        var updatedTeam = await _teamRepository.GetByIdAsync(team.Id);

        updatedTeam!.Name.Should().Be("New Name");
        updatedTeam.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTeamFromDatabase()
    {
        // Arrange
        var team = new Team("Team to delete", "Will be deleted");
        await _teamRepository.AddAsync(team);

        // Act
        await _teamRepository.DeleteAsync(team.Id);

        // Assert
        var deletedTeam = await _teamRepository.GetByIdAsync(team.Id);

        deletedTeam.Should().BeNull();
    }
}