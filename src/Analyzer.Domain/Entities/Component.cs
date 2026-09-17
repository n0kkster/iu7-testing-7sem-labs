namespace Analyzer.Domain.Entities;

using Analyzer.Domain.Enums;
using Analyzer.Domain.Exceptions;

public class Component
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required ComponentType Type { get; init; }
    public required string Name
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidComponentPropertyException("Имя компонента не может быть пустой строкой");
            field = value;
        }
    }

    public required string Description
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidComponentPropertyException("Описание компонента не может быть пустой строкой");
            field = value;
        }
    }

    public required Guid SystemId { get; init; }
}