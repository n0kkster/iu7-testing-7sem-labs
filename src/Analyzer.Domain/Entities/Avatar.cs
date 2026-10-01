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

    public Avatar(Guid userId, byte[] data, string contentType)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Data = data;
        ContentType = contentType;
        CreatedAt = DateTime.UtcNow;
        Hash = Convert.ToHexString(SHA256.HashData(Data));
    }
    private Avatar(Guid id, Guid userId, byte[] data, string hash, string contentType, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Data = data;
        Hash = hash;
        ContentType = contentType;
        CreatedAt = createdAt;
    }

    public static Avatar Restore(Guid id, Guid userId, byte[] data, string hash, string contentType, DateTimeOffset createdAt)
    {
        return new Avatar(id, userId, data, hash, contentType, createdAt);
    }
}