namespace Analyzer.Tests.Common.Builders;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

public class ComponentBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _systemId = Guid.NewGuid();
    private ComponentType _type = ComponentType.Microservice;
    private string _name = "Default Service";
    private string _description = "Service description";

    public ComponentBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ComponentBuilder WithSystemId(Guid systemId)
    {
        _systemId = systemId;
        return this;
    }

    public ComponentBuilder WithType(ComponentType type)
    {
        _type = type;
        return this;
    }

    public ComponentBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ComponentBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public Component Build()
    {
        return new Component
        {
            Id = _id,
            SystemId = _systemId,
            Type = _type,
            Name = _name,
            Description = _description
        };
    }
}