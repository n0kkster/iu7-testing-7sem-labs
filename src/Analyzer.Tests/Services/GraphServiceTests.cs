using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
using Moq;

namespace Analyzer.Tests.Services;

public class GraphServiceTests
{
    private readonly Mock<IGraphRepository> _graphRepoMock;
    private readonly GraphService _graphService;

    public GraphServiceTests()
    {
        _graphRepoMock = new Mock<IGraphRepository>();
        _graphService = new GraphService(_graphRepoMock.Object);
    }

    #region Component Tests

    [Fact]
    public async Task GetComponentsBySystemIdAsync_ReturnsMappedDtos()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var componentId = Guid.NewGuid();
        
        var components = new List<Component>
        {
            new() 
            { 
                Id = componentId, 
                SystemId = systemId, 
                Type = ComponentType.Database, 
                Name = "DB Node", 
                Description = "Main DB" 
            }
        };

        _graphRepoMock.Setup(r => r.GetComponentsBySystemIdAsync(systemId))
            .ReturnsAsync(components);

        // Act
        var result = await _graphService.GetComponentsBySystemIdAsync(systemId);

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.Equal(componentId, dto.Id);
        Assert.Equal(systemId, dto.SystemId);
        Assert.Equal(ComponentType.Database, dto.Type);
        Assert.Equal("DB Node", dto.Name);
        Assert.Equal("Main DB", dto.Description);
    }

    [Fact]
    public async Task GetComponentDetailsAsync_ReturnsMappedDto()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var component = new Component 
        { 
            Id = componentId, 
            SystemId = Guid.NewGuid(), 
            Type = ComponentType.Microservice, 
            Name = "Auth Service", 
            Description = "Handles Auth" 
        };

        _graphRepoMock.Setup(r => r.GetComponentAsync(componentId))
            .ReturnsAsync(component);

        // Act
        var result = await _graphService.GetComponentDetailsAsync(componentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(componentId, result.Id);
        Assert.Equal(component.Name, result.Name);
        Assert.Equal(component.Type, result.Type);
    }

    [Fact]
    public async Task CreateComponentAsync_AddsToRepositoryAndReturnsId()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var dto = new CreateComponentDto(systemId, ComponentType.MessageBroker, "Kafka", "Event Bus");

        // Act
        var newId = await _graphService.CreateComponentAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, newId);
        
        // Проверяем, что маппинг в Entity прошел корректно перед сохранением
        _graphRepoMock.Verify(r => r.AddComponentAsync(It.Is<Component>(c => 
            c.SystemId == systemId &&
            c.Type == ComponentType.MessageBroker &&
            c.Name == "Kafka" &&
            c.Description == "Event Bus")), 
            Times.Once);
    }

    [Fact]
    public async Task UpdateComponentAsync_UpdatesRepository()
    {
        // Arrange
        var dto = new ComponentDto 
        { 
            Id = Guid.NewGuid(), 
            SystemId = Guid.NewGuid(), 
            Type = ComponentType.ExternalAPI, 
            Name = "Stripe", 
            Description = "Payments" 
        };

        // Act
        await _graphService.UpdateComponentAsync(dto);

        // Assert
        _graphRepoMock.Verify(r => r.UpdateComponentAsync(It.Is<Component>(c => 
            c.Id == dto.Id &&
            c.Name == dto.Name &&
            c.Description == dto.Description &&
            c.Type == dto.Type)), 
            Times.Once);
    }

    [Fact]
    public async Task DeleteComponentAsync_DeletesFromRepository()
    {
        // Arrange
        var componentId = Guid.NewGuid();

        // Act
        await _graphService.DeleteComponentAsync(componentId);

        // Assert
        _graphRepoMock.Verify(r => r.DeleteComponentAsync(componentId), Times.Once);
    }

    #endregion

    #region Link Tests

    [Fact]
    public async Task GetLinksBySystemIdAsync_ReturnsMappedDtos()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var links = new List<Link>
        {
            new Link 
            { 
                Id = linkId, 
                SourceId = sourceId, 
                TargetId = targetId, 
                Severity = LinkSeverity.High, 
                Protocol = ProtocolType.gRPC 
            }
        };

        _graphRepoMock.Setup(r => r.GetLinksBySystemIdAsync(systemId))
            .ReturnsAsync(links);

        // Act
        var result = await _graphService.GetLinksBySystemIdAsync(systemId);

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.Equal(linkId, dto.Id);
        Assert.Equal(sourceId, dto.SourceId);
        Assert.Equal(targetId, dto.TargetId);
        Assert.Equal(LinkSeverity.High, dto.Severity);
        Assert.Equal(ProtocolType.gRPC, dto.Protocol);
    }

    [Fact]
    public async Task CreateLinkAsync_AddsToRepositoryAndReturnsId()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var dto = new CreateLinkDto(sourceId, targetId, LinkSeverity.Mid, ProtocolType.REST);

        // Act
        var newId = await _graphService.CreateLinkAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, newId);

        // Проверяем, что маппинг связи в Entity прошел без потерь
        _graphRepoMock.Verify(r => r.AddLinkAsync(It.Is<Link>(l => 
            l.SourceId == sourceId &&
            l.TargetId == targetId &&
            l.Severity == LinkSeverity.Mid &&
            l.Protocol == ProtocolType.REST)), 
            Times.Once);
    }

    [Fact]
    public async Task DeleteLinkAsync_DeletesFromRepository()
    {
        // Arrange
        var linkId = Guid.NewGuid();

        // Act
        await _graphService.DeleteLinkAsync(linkId);

        // Assert
        _graphRepoMock.Verify(r => r.DeleteLinkAsync(linkId), Times.Once);
    }

    #endregion
}