using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
using Moq;

namespace Analyzer.Tests.Services;

public class SystemServiceTests
{
    private readonly Mock<IGraphService> _graphServiceMock;
    private readonly Mock<ISystemRepository> _systemsRepoMock;
    private readonly SystemService _systemService;

    public SystemServiceTests()
    {
        _graphServiceMock = new Mock<IGraphService>();
        _systemsRepoMock = new Mock<ISystemRepository>();
        _systemService = new SystemService(_graphServiceMock.Object, _systemsRepoMock.Object);
    }

    #region CRUD Systems

    [Fact]
    public async Task GetSystemsByTeamIdAsync_ReturnsMappedDtos()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var system = new ITSystem("Billing", "Payment processing", teamId);

        _systemsRepoMock.Setup(r => r.GetByTeamIdAsync(teamId)).ReturnsAsync([system]);
        _graphServiceMock.Setup(r => r.GetComponentsBySystemIdAsync(system.Id)).ReturnsAsync([]);

        // Act
        var result = await _systemService.GetSystemsByTeamIdAsync(teamId);

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.Equal(system.Id, dto.Id);
        Assert.Equal("Billing", dto.Name);
        Assert.Equal(teamId, dto.TeamId);
        Assert.Equal(0, dto.ComponentsCount);
    }

    [Fact]
    public async Task CreateSystemAsync_AddsToRepo_AndReturnsId()
    {
        // Arrange
        var dto = new CreateITSystemDto()
        {
            Name = "CRM",
            Description = "Customer relations",
            TeamId = Guid.NewGuid()
        };

        // Act
        var resultId = await _systemService.CreateSystemAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
        _systemsRepoMock.Verify(r => r.AddAsync(It.Is<ITSystem>(s => s.Name == "CRM" && s.TeamId == dto.TeamId)), Times.Once);
    }

    [Fact]
    public async Task UpdateSystemAsync_SystemExists_UpdatesDetails()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var system = new ITSystem("Old Name", "Old Desc", Guid.NewGuid());
        _systemsRepoMock.Setup(r => r.GetByIdAsync(systemId)).ReturnsAsync(system);

        var dto = new ITSystemDto(systemId, "New Name", "New Desc", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), 0);

        // Act
        await _systemService.UpdateSystemAsync(dto);

        // Assert
        Assert.Equal("New Name", system.Name);
        Assert.Equal("New Desc", system.Description);
    }

    [Fact]
    public async Task UpdateSystemAsync_SystemNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _systemsRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ITSystem?)null);
        var dto = new ITSystemDto(Guid.NewGuid(), "Name", "Desc", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), 0);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _systemService.UpdateSystemAsync(dto));
    }

    [Fact]
    public async Task DeleteSystemAsync_CascadesToComponents()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var comp1 = new ComponentDto { Id = Guid.NewGuid(), SystemId = systemId, Type = ComponentType.Database, Name = "DB", Description = "" };
        var comp2 = new ComponentDto { Id = Guid.NewGuid(), SystemId = systemId, Type = ComponentType.Microservice, Name = "API", Description = "" };

        _graphServiceMock.Setup(s => s.GetComponentsBySystemIdAsync(systemId))
            .ReturnsAsync([comp1, comp2]);

        // Act
        await _systemService.DeleteSystemAsync(systemId);

        // Assert
        // Проверяем, что GraphService.DeleteComponentAsync был вызван для КАЖДОГО компонента
        _graphServiceMock.Verify(s => s.DeleteComponentAsync(comp1.Id), Times.Once);
        _graphServiceMock.Verify(s => s.DeleteComponentAsync(comp2.Id), Times.Once);

        // Проверяем, что сама система тоже была удалена
        _systemsRepoMock.Verify(r => r.DeleteAsync(systemId), Times.Once);
    }

    #endregion

    #region Export / Import

    [Fact]
    public async Task ExportSystem_ReturnsComponentsAndLinks()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var components = new List<ComponentDto> { new() { Id = Guid.NewGuid(), SystemId = systemId, Type = ComponentType.Database, Name = "DB", Description = "" } };
        var links = new List<LinkDto> { new() { Id = Guid.NewGuid(), SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid(), Severity = LinkSeverity.High, Protocol = ProtocolType.gRPC } };

        _graphServiceMock.Setup(s => s.GetComponentsBySystemIdAsync(systemId)).ReturnsAsync(components);
        _graphServiceMock.Setup(s => s.GetLinksBySystemIdAsync(systemId)).ReturnsAsync(links);

        // Act
        var (resultComponents, resultLinks) = await _systemService.ExportSystemAsync(systemId);

        // Assert
        Assert.Single(resultComponents);
        Assert.Single(resultLinks);
        Assert.Equal(components, resultComponents);
        Assert.Equal(links, resultLinks);
    }

    [Fact]
    public async Task ImportSystem_RebuildsGraphWithNewGuids()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var oldCompId1 = Guid.NewGuid();
        var oldCompId2 = Guid.NewGuid();

        var components = new List<ComponentDto>
        {
            new() { Id = oldCompId1, SystemId = Guid.NewGuid(), Type = ComponentType.Microservice, Name = "App", Description = "" },
            new() { Id = oldCompId2, SystemId = Guid.NewGuid(), Type = ComponentType.Database, Name = "DB", Description = "" }
        };

        var links = new List<LinkDto>
        {
            new() { Id = Guid.NewGuid(), SourceId = oldCompId1, TargetId = oldCompId2, Severity = LinkSeverity.High, Protocol = ProtocolType.TCP }
        };

        var dto = new CreateITSystemDto()
        {
            Name = "Imported System", 
            Description = "Imported Desc", 
            TeamId = teamId
        };

        var newCompId1 = Guid.NewGuid();
        var newCompId2 = Guid.NewGuid();

        _graphServiceMock.SetupSequence(s => s.CreateComponentAsync(It.IsAny<CreateComponentDto>()))
            .ReturnsAsync(newCompId1)
            .ReturnsAsync(newCompId2);

        // Act
        var resultSystemId = await _systemService.ImportSystemAsync(components, links, dto);

        // Assert
        Assert.NotEqual(Guid.Empty, resultSystemId);

        _systemsRepoMock.Verify(r => r.AddAsync(It.Is<ITSystem>(s => s.Name == "Imported System")), Times.Once);

        _graphServiceMock.Verify(s => s.CreateLinkAsync(It.Is<CreateLinkDto>(l =>
            l.SourceId == newCompId1 &&
            l.TargetId == newCompId2 &&
            l.Severity == LinkSeverity.High &&
            l.Protocol == ProtocolType.TCP)), Times.Once);
    }

    #endregion
}