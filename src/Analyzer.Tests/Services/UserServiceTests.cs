using Analyzer.Application.Interfaces.Providers;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO.Common;
using Moq;

namespace Analyzer.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IInviteService> _inviteServiceMock;
    private readonly Mock<IAvatarRepository> _avatarRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _inviteServiceMock = new Mock<IInviteService>();
        _avatarRepositoryMock = new Mock<IAvatarRepository>();
        
        _userService = new UserService(
            _userRepoMock.Object, 
            _jwtProviderMock.Object, 
            _inviteServiceMock.Object,
            _avatarRepositoryMock.Object
        );
    }

    #region Registration & Login

    [Fact]
    public async Task RegisterAsync_ValidDto_SavesUserAndConsumesInvite()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "new_user",
            Email = "test@test.com",
            Password = "MySecretPass123!",
            InviteCode = "INVITE_CODE"
        };

        var teamId = Guid.NewGuid();
        var roleFromInvite = Role.Developer;

        _userRepoMock.Setup(r => r.ExistsByUsernameAsync(dto.Username)).ReturnsAsync(false);
        
        _inviteServiceMock.Setup(i => i.GetValidatedInviteDetailsAsync(dto.InviteCode, dto.Email))
            .ReturnsAsync((roleFromInvite, teamId));

        // Act
        var newUserId = await _userService.RegisterAsync(dto);

        // Assert
        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Username == "new_user" &&
            u.Role == roleFromInvite &&
            u.TeamId == teamId &&
            BCrypt.Net.BCrypt.EnhancedVerify(dto.Password, u.PasswordHash))),
            Times.Once);

        _inviteServiceMock.Verify(i => i.ConsumeInviteAsync("INVITE_CODE", It.IsAny<User>()), 
            Times.Once);

        Assert.NotEqual(Guid.Empty, newUserId);
    }

    [Fact]
    public async Task RegisterAsync_UsernameTaken_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "taken_user",
            Email = "test@test.com",
            Password = "password",
            InviteCode = "code"
        };
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync("taken_user")).ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.RegisterAsync(dto));
        Assert.Contains("уже зарегистрирован", ex.Message);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_InvalidPassword_ThrowsArgumentException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "taken_user",
            Email = "test@test.com",
            Password = "2shrt",
            InviteCode = "code"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync(dto));
        Assert.Contains("должен содержать не менее", ex.Message);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsJwt()
    {
        // Arrange
        var password = "CorrectPassword";
        var hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
        
        var user = User.CreateAdmin("test_user", "test@test.com", hash);

        _userRepoMock.Setup(r => r.GetByUsernameAsync("test_user")).ReturnsAsync(user);
        _jwtProviderMock.Setup(j => j.GenerateToken(user, It.IsAny<string>())).Returns("token_abc_123");

        var dto = new LoginDto { Username = "test_user", Password = password };

        // Act
        var result = await _userService.LoginAsync(dto);

        // Assert
        Assert.Equal("token_abc_123", result);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.EnhancedHashPassword("CorrectPassword");
        var user = User.CreateAdmin("test_user", "test@test.com", hash);
        _userRepoMock.Setup(r => r.GetByUsernameAsync("test_user")).ReturnsAsync(user);

        var dto = new LoginDto { Username = "test_user", Password = "WrongPassword" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByUsernameAsync("ghost")).ReturnsAsync((User?)null);
        var dto = new LoginDto { Username = "ghost", Password = "123" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.LoginAsync(dto));
    }

    #endregion

    #region Profile Update

    [Fact]
    public async Task UpdateProfileAsync_ChangeUsernameToAvailable_UpdatesSuccessfully()
    {
        // Arrange
        var user = User.CreateAdmin("old_name", "test@test.com", "hash");
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync("new_name")).ReturnsAsync(false);

        var dto = new UpdateProfileDto()
        {
            Username = "new_name", 
            Email = "new_email@test.com"
        };

        // Act
        await _userService.UpdateProfileAsync(user.Id, dto);

        // Assert
        Assert.Equal("new_name", user.Username);
        Assert.Equal("new_email@test.com", user.Email);
        _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_ChangeUsernameToTaken_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = User.CreateAdmin("old_name", "test@test.com", "hash");
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync("taken_name")).ReturnsAsync(true);

        var dto = new UpdateProfileDto()
        {
            Username = "taken_name", 
            Email = "test@test.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.UpdateProfileAsync(user.Id, dto));
        Assert.Contains("используется другим пользователем", ex.Message);
    }

    #endregion

    #region Password Change

    [Fact]
    public async Task ChangePasswordAsync_ValidOldPassword_UpdatesHash()
    {
        // Arrange
        var oldPass = "OldPass123!";
        var newPass = "NewPass456!";
        var oldHash = BCrypt.Net.BCrypt.EnhancedHashPassword(oldPass);
        var user = User.CreateAdmin("test_user", "test@test.com", oldHash);

        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        // Act
        await _userService.ChangePasswordAsync(user.Id, oldPass, newPass);

        // Assert
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify(newPass, user.PasswordHash));
        _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongOldPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var oldHash = BCrypt.Net.BCrypt.EnhancedHashPassword("RealOldPass");
        var user = User.CreateAdmin("test_user", "test@test.com", oldHash);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.ChangePasswordAsync(user.Id, "FakeOldPass", "NewPass"));

        Assert.Contains("Текущий пароль указан неверно", ex.Message);
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region Profile, All Users, Delete

    [Fact]
    public async Task GetProfileAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var user = User.CreateAdmin("profile_user", "profile@test.com", "hash");
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetProfileAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.GetProfileAsync(missingId));
    }

    [Fact]
    public async Task GetAllUsersAsync_UsersExist_ReturnsUserDtoList()
    {
        // Arrange
        var user1 = User.CreateAdmin("admin1", "a1@test.com", "hash");
        var user2 = User.CreateAdmin("admin2", "a2@test.com", "hash");
        _userRepoMock.Setup(r => r.GetAllUsersAsync()).ReturnsAsync([user1, user2]);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_NoUsers_ReturnsEmptyList()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAllUsersAsync()).ReturnsAsync([]);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_EmailAlreadyTaken_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = User.CreateAdmin("user1", "old@test.com", "hash");
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync("user1")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("taken@test.com")).ReturnsAsync(true);

        var dto = new UpdateProfileDto { Username = "user1", Email = "taken@test.com" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _userService.UpdateProfileAsync(user.Id, dto));
        Assert.Contains("почта уже используется", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_UserExists_DeletesFromRepository()
    {
        // Arrange
        var user = User.CreateAdmin("to_delete", "del@test.com", "hash");
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        // Act
        await _userService.DeleteAsync(user.Id);

        // Assert
        _userRepoMock.Verify(r => r.DeleteAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.DeleteAsync(missingId));
        _userRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion
}