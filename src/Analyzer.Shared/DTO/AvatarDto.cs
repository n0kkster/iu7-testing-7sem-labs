namespace Analyzer.Shared.DTO;

public record AvatarDto(Guid Id, 
                             DateTimeOffset CreatedAt, 
                             string ContentType, 
                             byte[] Data);
