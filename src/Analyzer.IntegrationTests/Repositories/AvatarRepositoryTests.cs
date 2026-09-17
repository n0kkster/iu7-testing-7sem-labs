using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class AvatarRepositoryTests : IDisposable 
{
    private readonly IServiceScope _scope;    
    private readonly IUserRepository _userRepository;
    private readonly IAvatarRepository _avatarRepository;

    public AvatarRepositoryTests(SharedDatabaseFixture fixture)
    {
        _scope = fixture.ServiceProvider.CreateScope();

        _userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        _avatarRepository = _scope.ServiceProvider.GetRequiredService<IAvatarRepository>();
    }

    public void Dispose()
    {
        _scope.Dispose(); 
    }
    
    private async Task<User> CreateTestUserAsync()
    {
        var user = User.CreateAdmin($"user_{Guid.NewGuid()}", $"test_{Guid.NewGuid()}@test.com", "hash");
        await _userRepository.AddAsync(user);

        return user;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveAvatarToDatabase()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var data = new byte[] { 1, 2, 3, 4 };
        var avatar = new Avatar(user.Id, data, "image/webp");


        // Act
        await _avatarRepository.AddAsync(avatar);

        // Assert
        var savedAvatar = await _avatarRepository.GetByIdAsync(avatar.Id);

        savedAvatar.Should().NotBeNull();
        savedAvatar!.UserId.Should().Be(user.Id);
        savedAvatar.Hash.Should().Be(avatar.Hash);
        savedAvatar.Data.Should().Equal(data);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCorrectAvatar()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var avatar = new Avatar(user.Id, [13, 37], "image/png");

        await _avatarRepository.AddAsync(avatar);

        // Act
        var result = await _avatarRepository.GetByIdAsync(avatar.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(avatar.Id);
        result.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task GetByHashAsync_ShouldReturnAvatar_WhenUserAndHashMatch()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var avatar = new Avatar(user.Id, [1, 1, 1], "image/webp");

        await _avatarRepository.AddAsync(avatar);

        // Act
        var result = await _avatarRepository.GetByHashAsync(user.Id, avatar.Hash);

        // Assert
        result.Should().NotBeNull();
        result!.Hash.Should().Be(avatar.Hash);
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetHistoryByUserIdAsync_ShouldReturnOrderedDtos()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        var avatar1 = new Avatar(user.Id, [1], "image/webp");
        var avatar2 = new Avatar(user.Id, [2], "image/webp");
        var avatar3 = new Avatar(user.Id, [3], "image/webp");

        await _avatarRepository.AddAsync(avatar1);
        await _avatarRepository.AddAsync(avatar2);
        await _avatarRepository.AddAsync(avatar3);

        // Act
        var history = await _avatarRepository.GetHistoryByUserIdAsync(user.Id);

        // Assert
        history.Should().HaveCount(3);
        history.First().Id.Should().Be(avatar3.Id);
        history.Last().Id.Should().Be(avatar1.Id);
        history.First().Data.Should().Equal([3]);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAvatar()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var avatar = new Avatar(user.Id, [0], "image/webp");

        await _avatarRepository.AddAsync(avatar);

        // Act
        await _avatarRepository.DeleteAsync(avatar.Id);

        // Assert
        var deletedAvatar = await _avatarRepository.GetByIdAsync(avatar.Id);
        deletedAvatar.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithDuplicateHashForSameUser_ShouldThrowException()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var data = new byte[] { 5, 5, 5 };
        var avatar1 = new Avatar(user.Id, data, "image/webp");
        var avatar2 = new Avatar(user.Id, data, "image/webp");

        await _avatarRepository.AddAsync(avatar1);

        // Act
        var act = async () => await _avatarRepository.AddAsync(avatar2);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}