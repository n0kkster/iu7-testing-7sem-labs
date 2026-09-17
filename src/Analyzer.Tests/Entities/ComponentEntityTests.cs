using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Domain.Exceptions;

namespace Analyzer.Tests.Entities;

public class ComponentEntityTests
{
    [Fact]
    public void Component_SetName_EmptyString_ThrowsInvalidComponentPropertyException()
    {
        // Arrange
        var component = new Component 
        { 
            Type = ComponentType.Database, 
            Name = "Valid Name", 
            Description = "Desc", 
            SystemId = Guid.NewGuid() 
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidComponentPropertyException>(() => component.Name = "  ");
        Assert.Contains("не может быть пустой строкой", ex.Message);
    }

    [Fact]
    public void Component_SetDescription_EmptyString_ThrowsInvalidComponentPropertyException()
    {
        // Arrange
        var component = new Component 
        { 
            Type = ComponentType.Microservice, 
            Name = "Name", 
            Description = "Valid Desc", 
            SystemId = Guid.NewGuid() 
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidComponentPropertyException>(() => component.Description = "");
        Assert.Contains("не может быть пустой строкой", ex.Message);
    }
}