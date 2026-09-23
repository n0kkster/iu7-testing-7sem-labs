namespace Analyzer.Tests.Services;

using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;
using Analyzer.Tests.Common.Fakes;
using Analyzer.Tests.Common.Mothers;
using Moq;

public class SchoolsComparisonTests
{
    #region Создание команды

    // ЛОНДОНСКАЯ ШКОЛА:
    // Тестируемый сервис полностью изолирован. Репозиторий подменен моком.
    // Проверяем факт вызова метода AddAsync с нужными параметрами через Verify.
    [Fact]
    public async Task CreateTeam_LondonSchool_VerifiesInteractionWithRepository()
    {
        // Arrange
        var teamRepoMock = new Mock<ITeamRepository>();
        var systemRepoMock = new Mock<ISystemRepository>();
        var userRepoMock = new Mock<IUserRepository>();

        var service = new TeamService(
            teamRepoMock.Object, 
            systemRepoMock.Object, 
            userRepoMock.Object);

        var dto = new CreateTeamDto
        {
            Name = "Platform Team",
            Description = "Infrastructure support"
        };

        // Act
        var result = await service.CreateTeamAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);

        // Ключевое отличие: проверяем контракт взаимодействия с зависимостью
        teamRepoMock.Verify(r => r.AddAsync(It.Is<Team>(t => 
            t.Name == dto.Name && 
            t.Description == dto.Description)), 
            Times.Once);
    }

    // КЛАССИЧЕСКАЯ ШКОЛА:
    // Вместо мока используется Fake-хранилище (InMemoryTeamRepository).
    // Нас не интересует, какие внутренние методы вызывались — проверяем только итоговое состояние хранилища.
    [Fact]
    public async Task CreateTeam_ClassicalSchool_VerifiesResultingStateInStorage()
    {
        // Arrange
        var fakeTeamRepo = new InMemoryTeamRepository();
        var stubSystemRepo = new Mock<ISystemRepository>().Object;
        var stubUserRepo = new Mock<IUserRepository>().Object;

        var service = new TeamService(fakeTeamRepo, stubSystemRepo, stubUserRepo);

        var dto = new CreateTeamDto
        {
            Name = "Platform Team",
            Description = "Infrastructure support"
        };

        // Act
        var result = await service.CreateTeamAsync(dto);

        // Assert
        Assert.NotNull(result);

        // Ключевое отличие: запрашиваем состояние репозитория напрямую
        var savedTeam = await fakeTeamRepo.GetByIdAsync(result.Id);
        Assert.NotNull(savedTeam);
        Assert.Equal("Platform Team", savedTeam.Name);
    }

    #endregion

    #region Сценарий 2: Удаление команды без систем (DeleteTeamAsync)

    // ЛОНДОНСКАЯ ШКОЛА:
    // Проверяем, что при пустом списке систем был вызван DeleteAsync именно с переданным Id.
    [Fact]
    public async Task DeleteTeam_LondonSchool_VerifiesDeleteCallWhenNoSystemsOwned()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamRepoMock = new Mock<ITeamRepository>();
        var systemRepoMock = new Mock<ISystemRepository>();

        systemRepoMock.Setup(r => r.GetByTeamIdAsync(teamId))
            .ReturnsAsync([]);

        var service = new TeamService(
            teamRepoMock.Object, 
            systemRepoMock.Object, 
            Mock.Of<IUserRepository>());

        // Act
        await service.DeleteTeamAsync(teamId);

        // Assert
        teamRepoMock.Verify(r => r.DeleteAsync(teamId), Times.Once);
    }

    // КЛАССИЧЕСКАЯ ШКОЛА:
    // Заранее наполняем Fake реальным объектом и проверяем, что после удаления объект исчез из коллекции.
    [Fact]
    public async Task DeleteTeam_ClassicalSchool_VerifiesItemIsRemovedFromStorage()
    {
        // Arrange
        var fakeTeamRepo = new InMemoryTeamRepository();
        var team = TeamMother.CreateEngineeringTeam();
        await fakeTeamRepo.AddAsync(team);

        var systemRepoMock = new Mock<ISystemRepository>();
        systemRepoMock.Setup(r => r.GetByTeamIdAsync(team.Id))
            .ReturnsAsync([]);

        var service = new TeamService(
            fakeTeamRepo, 
            systemRepoMock.Object, 
            Mock.Of<IUserRepository>());

        // Act
        await service.DeleteTeamAsync(team.Id);

        // Assert
        var deletedTeam = await fakeTeamRepo.GetByIdAsync(team.Id);
        Assert.Null(deletedTeam);
        Assert.Empty(await fakeTeamRepo.GetAllTeamsAsync());
    }

    #endregion
}