using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class UserRepositoryTests: IDisposable 
{
    private readonly IServiceScope _scope;    
    private readonly IUserRepository _userRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IAvatarRepository _avatarRepository;

    public UserRepositoryTests(SharedDatabaseFixture fixture)
    {
        _scope = fixture.ServiceProvider.CreateScope();

        _userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        _teamRepository = _scope.ServiceProvider.GetRequiredService<ITeamRepository>();
        _inviteRepository = _scope.ServiceProvider.GetRequiredService<IInviteRepository>();
        _avatarRepository = _scope.ServiceProvider.GetRequiredService<IAvatarRepository>();
    }

    public void Dispose()
    {
        _scope.Dispose(); 
    }
    
    [Fact]
    public async Task AddAsync_ShouldSaveUserToDatabase()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await _teamRepository.AddAsync(team);

        var username = $"user_{Guid.NewGuid()}";
        var email = $"{Guid.NewGuid()}@test.com";
        var user = User.CreateInvitedUser(
            username, 
            email, 
            "hash123", 
            Role.Developer, 
            team.Id
        );

        // Act
        await _userRepository.AddAsync(user);

        // Assert
        var savedUser = await _userRepository.GetByIdAsync(user.Id);

        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be(username);
        savedUser.Email.Should().Be(email);
        savedUser.Role.Should().Be(Role.Developer);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await _teamRepository.AddAsync(team);

        var username = $"findme_{Guid.NewGuid()}";
        var user = User.CreateInvitedUser(
            username, 
            $"{Guid.NewGuid()}@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );

        await _userRepository.AddAsync(user);

        // Act
        var result = await _userRepository.GetByUsernameAsync(username);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyUserDetails()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await _teamRepository.AddAsync(team);

        var invite = new Invite(
            $"invite_{Guid.NewGuid()}@test.com", 
            7, 
            team.Id, 
            Role.Developer
        );
        await _inviteRepository.AddAsync(invite);

        var user = User.CreateInvitedUser(
            $"old_{Guid.NewGuid()}", 
            $"{Guid.NewGuid()}@test.com", 
            "oldHash",
            Role.Developer,
            team.Id
        );
        await _userRepository.AddAsync(user);

        var avatar = new Avatar(user.Id, [1, 2, 3], "image/webp");
        await _avatarRepository.AddAsync(avatar);

        // Act

        var userToUpdate = await _userRepository.GetByIdAsync(user.Id);
        var newUsername = $"new_{Guid.NewGuid()}";
        
        userToUpdate!.UpdateProfile(newUsername, "new@test.com", avatar.Id);
        userToUpdate.ChangePassword("newHash");

        await _userRepository.UpdateAsync(userToUpdate);

        // Assert
        var updatedUser = await _userRepository.GetByIdAsync(user.Id);

        updatedUser!.Username.Should().Be(newUsername);
        updatedUser.Email.Should().Be("new@test.com");
        updatedUser.PasswordHash.Should().Be("newHash");
        updatedUser.AvatarId.Should().Be(avatar.Id);
        updatedUser.Role.Should().Be(Role.Developer);
        updatedUser.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task ExistsByUsernameAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await _teamRepository.AddAsync(team);

        var username = $"exists_{Guid.NewGuid()}";
        var user = User.CreateInvitedUser(
            username, 
            $"{Guid.NewGuid()}@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );

        await _userRepository.AddAsync(user);

        // Act
        var exists = await _userRepository.ExistsByUsernameAsync(username);

        // Assert
        exists.Should().BeTrue();
    }
}