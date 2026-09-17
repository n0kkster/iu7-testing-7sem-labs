using Analyzer.Domain.Entities;

namespace Analyzer.Tests.Entities;

public class TeamEntityTests
{
    [Fact]
    public void Team_Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Team("", "description"));
    }

    [Fact]
    public void Team_UpdateProfile_WithEmptyName_PreservesOldName()
    {
        // Arrange
        var team = new Team("Old Team", "Old Desc");

        // Act
        team.UpdateProfile("   ", "New Desc");

        // Assert
        Assert.Equal("Old Team", team.Name); // Имя не изменилось
        Assert.Equal("New Desc", team.Description); // Описание обновилось
    }

    [Fact]
    public void Team_AddMember_PreventsDuplicates()
    {
        // Arrange
        var team = new Team("Dev", "Desc");
        var userId = Guid.NewGuid();

        // Act
        team.AddMember(userId);
        team.AddMember(userId); // Пытаемся добавить того же пользователя второй раз

        // Assert
        Assert.Single(team.MemberIds); // В списке должен быть только 1 элемент
    }

    [Fact]
    public void Team_RemoveMember_RemovesSuccessfully()
    {
        // Arrange
        var team = new Team("Dev", "Desc");
        var userId = Guid.NewGuid();
        team.AddMember(userId);

        // Act
        team.RemoveMember(userId);

        // Assert
        Assert.Empty(team.MemberIds);
    }
}