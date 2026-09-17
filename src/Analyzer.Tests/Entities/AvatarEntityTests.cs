using System.Security.Cryptography;
using System.Text;
using Analyzer.Domain.Entities;

namespace Analyzer.Tests.Entities;

public class AvatarEntityTests
{
    #region Constructor & Initialization

    [Fact]
    public void Avatar_Constructor_ValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var data = new byte[] { 0x1, 0x2, 0x3 };
        var contentType = "image/webp";

        // Act
        var avatar = new Avatar(userId, data, contentType);

        // Assert
        Assert.NotEqual(Guid.Empty, avatar.Id);
        Assert.Equal(userId, avatar.UserId);
        Assert.Equal(data, avatar.Data);
        Assert.Equal(contentType, avatar.ContentType);
        Assert.True((DateTimeOffset.UtcNow - avatar.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void Avatar_Constructor_GeneratesValidHash()
    {
        // Arrange
        var inputString = "test-data";
        var data = Encoding.UTF8.GetBytes(inputString);
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        var expectedHash = Convert.ToHexString(hashBytes);

        // Act
        var avatar = new Avatar(Guid.NewGuid(), data, "image/webp");

        // Assert
        Assert.Equal(expectedHash, avatar.Hash); 
        Assert.Equal(64, avatar.Hash.Length);
    }

        #endregion

    #region Hashing Logic

    [Fact]
    public void Avatar_SameData_ProducesSameHash()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 1, 2, 3 };

        // Act
        var avatar1 = new Avatar(userId, data1, "image/webp");
        var avatar2 = new Avatar(userId, data2, "image/webp");

        // Assert
        Assert.Equal(avatar1.Hash, avatar2.Hash);
    }

    [Fact]
    public void Avatar_DifferentData_ProducesDifferentHash()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 3, 2, 1 };

        // Act
        var avatar1 = new Avatar(Guid.NewGuid(), data1, "image/webp");
        var avatar2 = new Avatar(Guid.NewGuid(), data2, "image/webp");

        // Assert
        Assert.NotEqual(avatar1.Hash, avatar2.Hash);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Avatar_EmptyData_StillGeneratesHash()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act
        var avatar = new Avatar(Guid.NewGuid(), emptyData, "image/webp");

        // Assert
        Assert.NotNull(avatar.Hash);
        Assert.NotEmpty(avatar.Hash);
        Assert.Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", avatar.Hash);
    }

    #endregion
}