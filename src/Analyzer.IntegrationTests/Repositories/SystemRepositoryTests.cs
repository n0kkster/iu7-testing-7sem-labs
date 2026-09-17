using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class SystemRepositoryTests: IDisposable 
{
    private readonly IServiceScope _scope;    
    private readonly ISystemRepository _systemRepository;
    private readonly ITeamRepository _teamRepository;

    public SystemRepositoryTests(SharedDatabaseFixture fixture)
    {
        _scope = fixture.ServiceProvider.CreateScope();

        _systemRepository = _scope.ServiceProvider.GetRequiredService<ISystemRepository>();
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
    public async Task AddAsync_ShouldSaveSystemToDatabase()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system = new ITSystem($"System_{Guid.NewGuid()}", "Main billing system", team.Id);

        // Act
        await _systemRepository.AddAsync(system);

        // Assert
        var savedSystem = await _systemRepository.GetByIdAsync(system.Id);

        savedSystem.Should().NotBeNull();
        savedSystem!.Name.Should().Be(system.Name);
        savedSystem.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task GetByTeamIdAsync_ShouldReturnAllTeamSystems()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system1 = new ITSystem($"Sys1_{Guid.NewGuid()}", "Desc1", team.Id);
        var system2 = new ITSystem($"Sys2_{Guid.NewGuid()}", "Desc2", team.Id);

        await _systemRepository.AddAsync(system1);
        await _systemRepository.AddAsync(system2);

        // Act
        var systems = await _systemRepository.GetByTeamIdAsync(team.Id);

        // Assert
        systems.Should().HaveCountGreaterThanOrEqualTo(2);
        systems.Select(s => s.Id).Should().Contain([system1.Id, system2.Id]);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySystemDetails()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system = new ITSystem("Old Name", "Old Desc", team.Id);

        await _systemRepository.AddAsync(system);

        // Act
        var systemToUpdate = await _systemRepository.GetByIdAsync(system.Id);
        var newName = $"New Name {Guid.NewGuid()}";
        systemToUpdate!.UpdateDetails(newName, "New Desc");
        await _systemRepository.UpdateAsync(systemToUpdate);

        // Assert
        var updatedSystem = await _systemRepository.GetByIdAsync(system.Id);

        updatedSystem!.Name.Should().Be(newName);
        updatedSystem.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSystem()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system = new ITSystem($"To Delete {Guid.NewGuid()}", "Desc", team.Id);

        await _systemRepository.AddAsync(system);

        // Act
        await _systemRepository.DeleteAsync(system.Id);

        // Assert
        var deletedSystem = await _systemRepository.GetByIdAsync(system.Id);

        deletedSystem.Should().BeNull();
    }
}