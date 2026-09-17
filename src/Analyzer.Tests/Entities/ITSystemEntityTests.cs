using Analyzer.Domain.Entities;

namespace Analyzer.Tests.Entities;

public class ITSystemEntityTests
{
    #region ITSystem Logic Tests

    [Fact]
    public void ITSystem_Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ITSystem("  ", "Desc", Guid.NewGuid()));
    }

    [Fact]
    public void ITSystem_UpdateDetails_UpdatesPropertiesAndTimestamp()
    {
        // Arrange
        var system = new ITSystem("Old Name", "Old Desc", Guid.NewGuid());
        var oldDate = system.UpdatedAt;

        Thread.Sleep(50); 

        // Act
        system.UpdateDetails("New Name", "New Desc");

        // Assert
        Assert.Equal("New Name", system.Name);
        Assert.Equal("New Desc", system.Description);
        Assert.True(system.UpdatedAt > oldDate); 
    }

    [Fact]
    public void ITSystem_UpdateDetails_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var system = new ITSystem("Valid Name", "Desc", Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => system.UpdateDetails("", "New Desc"));
    }

    #endregion
}