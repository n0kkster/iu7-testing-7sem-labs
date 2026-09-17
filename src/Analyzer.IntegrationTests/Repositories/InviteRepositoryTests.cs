using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Analyzer.Application.Interfaces.Repositories;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class InviteRepositoryTests : IDisposable 
{
    private readonly IServiceScope _scope;    
    private readonly IInviteRepository _inviteRepository;
    private readonly ITeamRepository _teamRepository;

    public InviteRepositoryTests(SharedDatabaseFixture fixture)
    {
        _scope = fixture.ServiceProvider.CreateScope();

        _inviteRepository = _scope.ServiceProvider.GetRequiredService<IInviteRepository>();
        _teamRepository = _scope.ServiceProvider.GetRequiredService<ITeamRepository>();
    }
    
    public void Dispose()
    {
        _scope.Dispose(); 
    }

    private async Task<Team> CreateTestTeamAsync()
    {
        var team = new Team($"Team_{Guid.NewGuid()}", "Test Team");
        await _teamRepository.AddAsync(team);

        return team;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveInviteToDatabase()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var targetEmail = $"invite_{Guid.NewGuid()}@test.com";
        var invite = new Invite(targetEmail, 7, team.Id, Role.Developer);

        // Act
        await _inviteRepository.AddAsync(invite);

        // Assert
        var savedInvite = await _inviteRepository.GetByIdAsync(invite.Id);

        savedInvite.Should().NotBeNull();
        savedInvite!.Code.Should().Be(invite.Code);
        savedInvite.TeamId.Should().Be(team.Id);
        savedInvite.Role.Should().Be(Role.Developer);
        savedInvite.Status.Should().Be(InviteStatus.Pending);
    }

    [Fact]
    public async Task GetByCodeAsync_ShouldReturnInvite()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var invite = new Invite($"code_{Guid.NewGuid()}@test.com", 7, team.Id, Role.SRE);

        await _inviteRepository.AddAsync(invite);

        // Act
        var result = await _inviteRepository.GetByCodeAsync(invite.Code);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(invite.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyInviteStatus()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var invite = new Invite($"update_{Guid.NewGuid()}@test.com", 7, team.Id, Role.Admin);
        
        await _inviteRepository.AddAsync(invite);

        // Act
        var inviteToUpdate = await _inviteRepository.GetByIdAsync(invite.Id);

        inviteToUpdate!.Revoke();
        await _inviteRepository.UpdateAsync(inviteToUpdate);

        // Assert
        var updatedInvite = await _inviteRepository.GetByIdAsync(invite.Id);

        updatedInvite!.Status.Should().Be(InviteStatus.Revoked);
    }
}