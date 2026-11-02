namespace Analyzer.Benchmarks.Data;

using Analyzer.Benchmarks.Models;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO.Common;

public static class TopologyGenerator
{
    public static SystemTopologyPayload Generate(int componentCount = 100, int linkCount = 150)
    {
        var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var components = new List<ComponentDto>(componentCount);

        for (int i = 0; i < componentCount; i++)
        {
            components.Add(new ComponentDto
            {
                Id = Guid.Parse($"10000000-0000-0000-0000-{i:D12}"),
                SystemId = systemId,
                Type = (ComponentType)(i % 4),
                Name = $"Microservice-{i}",
                Description = $"Enterprise core business component number {i} with metadata payload"
            });
        }

        var links = new List<LinkDto>(linkCount);
        var random = new Random(1337);

        for (int i = 0; i < linkCount; i++)
        {
            var source = components[random.Next(componentCount)];
            var target = components[random.Next(componentCount)];

            links.Add(new LinkDto
            {
                Id = Guid.Parse($"20000000-0000-0000-0000-{i:D12}"),
                SourceId = source.Id,
                TargetId = target.Id,
                Severity = (LinkSeverity)(i % 3),
                Protocol = (ProtocolType)(i % 5)
            });
        }

        return new SystemTopologyPayload(systemId, "Enterprise-Core-System", components, links);
    }
}