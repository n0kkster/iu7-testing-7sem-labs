using System.Security.Cryptography;

namespace Analyzer.Domain.Entities;

public class Avatar
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public byte[] Data { get; private set; }
    public string Hash { get; private set; } 
    public string ContentType { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Для EF Core
#pragma warning disable CS8618
    private Avatar() { }
#pragma warning restore CS8618

    public Avatar(Guid userId, byte[] data, string contentType)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Data = data;
        ContentType = contentType;
        CreatedAt = DateTime.UtcNow;
        Hash = Convert.ToHexString(SHA256.HashData(Data));
    }
}