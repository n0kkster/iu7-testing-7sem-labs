using Analyzer.Application.Interfaces.Providers;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;
using Moq;

namespace Analyzer.Tests.Services;

public class AvatarServiceTests
{
    private readonly Mock<IAvatarRepository> _avatarRepoMock;
    private readonly Mock<IImageProvider> _imageProviderMock;
    private readonly AvatarService _avatarService;

    public AvatarServiceTests()
    {
        _avatarRepoMock = new Mock<IAvatarRepository>();
        _imageProviderMock = new Mock<IImageProvider>();
        _avatarService = new AvatarService(_avatarRepoMock.Object, _imageProviderMock.Object);
    }

    #region 1. UploadNewAvatarAsync

    [Fact]
    public async Task UploadNewAvatarAsync_NewImage_AddsToRepoAndReturnsId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var imageData = new byte[] { 1, 2, 3 };
        var stream = new MemoryStream(imageData);
        var processResult = new ProcessedImageResult(imageData, "image/webp");

        _imageProviderMock.Setup(p => p.CreateWebpAsync(It.IsAny<Stream>()))
            .ReturnsAsync(processResult);

        _avatarRepoMock.Setup(r => r.GetByHashAsync(userId, It.IsAny<string>()))
            .ReturnsAsync((Avatar?)null);

        // Act
        var result = await _avatarService.UploadNewAvatarAsync(userId, stream);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _avatarRepoMock.Verify(r => r.AddAsync(It.IsAny<Avatar>()), Times.Once);
    }

    [Fact]
    public async Task UploadNewAvatarAsync_DuplicateImage_ReturnsExistingIdWithoutAdding()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var imageData = new byte[] { 1, 2, 3 };
        var stream = new MemoryStream(imageData);
        var processResult = new ProcessedImageResult(imageData, "image/webp");

        var existingAvatar = new Avatar(userId, imageData, "image/webp");

        _imageProviderMock.Setup(p => p.CreateWebpAsync(It.IsAny<Stream>()))
            .ReturnsAsync(processResult);

        _avatarRepoMock.Setup(r => r.GetByHashAsync(userId, It.IsAny<string>()))
            .ReturnsAsync(existingAvatar);

        // Act
        var result = await _avatarService.UploadNewAvatarAsync(userId, stream);

        // Assert
        Assert.Equal(existingAvatar.Id, result);
        _avatarRepoMock.Verify(r => r.AddAsync(It.IsAny<Avatar>()), Times.Never);
    }

    #endregion

    #region 2. GetAvatarHistoryAsync

    [Fact]
    public async Task GetAvatarHistoryAsync_ReturnsHistoryFromRepo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var history = new List<AvatarDto>
        {
            new(Guid.NewGuid(), DateTimeOffset.UtcNow, "image/webp", [1]),
            new(Guid.NewGuid(), DateTimeOffset.UtcNow, "image/webp", [2])
        };

        _avatarRepoMock.Setup(r => r.GetHistoryByUserIdAsync(userId))
            .ReturnsAsync(history);

        // Act
        var result = await _avatarService.GetAvatarHistoryAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        _avatarRepoMock.Verify(r => r.GetHistoryByUserIdAsync(userId), Times.Once);
    }

    #endregion

    #region 3. GetAvatarFileAsync

    [Fact]
    public async Task GetAvatarFileAsync_AvatarExists_ReturnsAvatar()
    {
        // Arrange
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar(Guid.NewGuid(), [1, 2], "image/webp");

        _avatarRepoMock.Setup(r => r.GetByIdAsync(avatarId))
            .ReturnsAsync(avatar);

        // Act
        var result = await _avatarService.GetAvatarFileAsync(avatarId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(avatar.Id, result.Id);
    }

    [Fact]
    public async Task GetAvatarFileAsync_NotExists_ReturnsNull()
    {
        // Arrange
        _avatarRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Avatar?)null);

        // Act
        var result = await _avatarService.GetAvatarFileAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 4. DeleteAvatarAsync

    [Fact]
    public async Task DeleteAvatarAsync_OwnAvatar_CallsDelete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar(userId, [1], "image/webp");

        _avatarRepoMock.Setup(r => r.GetByIdAsync(avatarId))
            .ReturnsAsync(avatar);

        // Act
        await _avatarService.DeleteAvatarAsync(userId, avatarId);

        // Assert
        _avatarRepoMock.Verify(r => r.DeleteAsync(avatarId), Times.Once);
    }

    [Fact]
    public async Task DeleteAvatarAsync_NotOwnAvatar_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var someoneElseId = Guid.NewGuid();
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar(someoneElseId, [1], "image/webp");

        _avatarRepoMock.Setup(r => r.GetByIdAsync(avatarId))
            .ReturnsAsync(avatar);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _avatarService.DeleteAvatarAsync(userId, avatarId));
        
        _avatarRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAvatarAsync_AvatarNotFound_DoesNothing()
    {
        // Arrange
        _avatarRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Avatar?)null);

        // Act
        await _avatarService.DeleteAvatarAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        _avatarRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion
}