namespace Analyzer.Infrastructure.Data.Entities;

public class AvatarEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public byte[] Data { get; set; } = [];
    public string Hash { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public UserEntity? User { get; set; }
}