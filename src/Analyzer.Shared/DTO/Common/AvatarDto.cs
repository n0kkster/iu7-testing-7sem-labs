namespace Analyzer.Shared.DTO.Common;

public record AvatarDto(Guid Id, 
                             DateTimeOffset CreatedAt, 
                             string ContentType, 
                             byte[] Data);
