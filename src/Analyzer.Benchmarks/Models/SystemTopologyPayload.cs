namespace Analyzer.Benchmarks.Models;

using Analyzer.Shared.DTO.Common;

public record SystemTopologyPayload(
    Guid SystemId,
    string SystemName,
    List<ComponentDto> Components,
    List<LinkDto> Links
);